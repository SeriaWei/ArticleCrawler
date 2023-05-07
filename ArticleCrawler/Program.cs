using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using ArticleCrawler.Api;
using ArticleCrawler.Filters;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArticleCrawler
{
    internal class Program
    {
        static HttpClient _httpClient;
        static Dictionary<string, IFilter> _filters;
        static Type _articleType = typeof(ArticleEntity);
        static Regex _urlPatten = new Regex(@"\[([ ]+)?(\d+)([ ]+)?-([ ]+)?(\d+)([ ]+)?\]", RegexOptions.Compiled);
        static string _host;
        static string _historyFile = "history.txt";
        static async Task Main(string[] args)
        {
            _httpClient = new HttpClient();
            _filters = new Dictionary<string, IFilter>
            {
                {"Trim",new TrimFilter()},
                {"Substring",new SubstringFilter()},
                {"Replace",new ReplaceFilter()}
            };
            string json = File.ReadAllText("Mapping.json");
            var mapping = JsonSerializer.Deserialize<Mapping>(json);
            HashSet<string> crawled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var fs = new FileStream(_historyFile, FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (var reader = new StreamReader(fs))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        crawled.Add(line);
                    }
                }
            }
            //http://www.tjch.com.cn/234/244/index_614_[1-309].html
            Console.WriteLine("Input a Url:");
            string baseUrl = Console.ReadLine();
            _host = new Uri(baseUrl).Host;
            var apiService = new ApiService();
            var historyStream = new FileStream(_historyFile, FileMode.Append, FileAccess.Write);
            var historyWriter = new StreamWriter(historyStream);
            foreach (var url in ParseUrl(baseUrl))
            {
                var doc = await GetHtmlDocument(url);
                var articles = doc.QuerySelectorAll(mapping.ArticleItemsSelector);
                foreach (var articleElement in articles)
                {
                    var article = new ArticleEntity();
                    HashSet<string> files = new HashSet<string>();
                    foreach (var propertyMapping in mapping.IndexPage)
                    {
                        var downloadFiles = await SetPropertyValueTo(article, articleElement, propertyMapping);
                        foreach (var file in downloadFiles)
                        {
                            files.Add(file.Replace('/', Path.DirectorySeparatorChar));
                        }
                    }
                    string href = (articleElement.QuerySelector(mapping.ArticleLinkSelector) as IHtmlAnchorElement).Href;
                    if (crawled.Contains(href)) continue;

                    var articleDoc = await GetHtmlDocument(href);
                    foreach (var propertyMapping in mapping.DetailPage)
                    {
                        var downloadFiles = await SetPropertyValueTo(article, articleDoc.DocumentElement, propertyMapping);
                        foreach (var file in downloadFiles)
                        {
                            files.Add(file.Replace('/', Path.DirectorySeparatorChar));
                        }
                    }
                    var articleNew = await apiService.Create(article, files);
                    Console.WriteLine("Complete: {0} -> ID:{1}", href, articleNew?.ID);
                    crawled.Add(href);
                    historyWriter.WriteLine(href);
                    await historyWriter.FlushAsync();
                }
            }
            historyWriter.Dispose();
            historyStream.Dispose();
        }

        private static async Task<List<string>> SetPropertyValueTo(ArticleEntity article, IElement articleElement, PropertyMapping item)
        {
            var files = new List<string>();
            var property = _articleType.GetProperty(item.Property);
            if (!string.IsNullOrEmpty(item.Selector))
            {
                var ele = articleElement.QuerySelector(item.Selector);
                if (ele == null) return files;

                var valueFile = await PopulateValue(ele, item);
                string value = valueFile.Value;
                files = valueFile.Files;
                value = FilterValue(item.Filters, value);
                property.SetValue(article, ConvertTo(value, property.PropertyType));
            }
            else if (item.Value != null)
            {
                property.SetValue(article, ((JsonElement)item.Value).Deserialize(property.PropertyType));
            }
            return files;
        }

        private static string FilterValue(Filter[] filters, string value)
        {
            if (filters == null || filters.Length == 0) return value;

            string result = value;
            foreach (var filter in filters)
            {
                result = _filters[filter.Name].Process(result, filter.Args);
            }

            return result;
        }

        private static async Task<(string Value, List<string> Files)> PopulateValue(IElement ele, PropertyMapping item)
        {
            var files = new List<string>();
            if (item.IsHtml)
            {
                foreach (var image in ele.QuerySelectorAll("img"))
                {
                    string path = await DownloadFile((image as IHtmlImageElement).Source);
                    if (!string.IsNullOrEmpty(path))
                    {
                        files.Add(path);
                        image.SetAttribute("src", path);
                    }
                }
                foreach (var image in ele.QuerySelectorAll("a"))
                {
                    string href = (image as IHtmlAnchorElement).Href;
                    if (string.IsNullOrEmpty(href)) continue;

                    if (".pdf".Equals(Path.GetExtension(new Uri(href).AbsolutePath), StringComparison.OrdinalIgnoreCase))
                    {
                        string path = await DownloadFile(href);
                        if (!string.IsNullOrEmpty(path))
                        {
                            files.Add(path);
                            image.SetAttribute("href", path);
                        }
                    }
                }
                return (ele.InnerHtml, files);
            }
            else if (!string.IsNullOrEmpty(item.Attr))
            {
                return (ele.GetAttribute(item.Attr), files);
            }
            else
            {
                if (ele.TagName.Equals("IMG", StringComparison.OrdinalIgnoreCase))
                {
                    string value = (ele as IHtmlImageElement).Source;
                    var path = await DownloadFile(value);
                    if (!string.IsNullOrEmpty(path))
                    {
                        files.Add(path);
                        return (path, files);
                    }
                    return (value, files);
                }
                else
                {
                    return (ele.TextContent, files);
                }
            }
        }

        public static async Task<IDocument> ParseHtml(string html, string baseUrl)
        {
            var configuration = Configuration.Default;
            var context = BrowsingContext.New(configuration);
            var document = await context.OpenAsync(res => res.Content(html).Address(baseUrl));
            return document;
        }
        public static async Task<IDocument> GetHtmlDocument(string url)
        {
            Console.WriteLine("Url: {0}", url);
            string html = await _httpClient.GetStringAsync(url);
            return await ParseHtml(html, url);
        }
        public static async Task<string> DownloadFile(string url)
        {
            var uri = new Uri(url);
            string filePath = ToFilePath(url);
            if (File.Exists(filePath)) return uri.AbsolutePath;

            if (!uri.Host.Equals(_host, StringComparison.OrdinalIgnoreCase)) return null;

            Console.WriteLine("Download file: {0}", url);
            using (var responseStream = await _httpClient.GetStreamAsync(url))
            {
                EnsureDirectoryExists(Path.GetDirectoryName(filePath));
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    responseStream.CopyTo(fs);
                }
            }
            return uri.AbsolutePath;
        }
        public static object ConvertTo(object obj, Type targetType)
        {
            if (obj == null) return null;

            var realType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return Convert.ChangeType(obj, realType);
        }
        public static string ToFilePath(string url)
        {
            return "Files" + new Uri(url).AbsolutePath.Replace('/', Path.DirectorySeparatorChar);
        }
        public static void EnsureDirectoryExists(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }
        static IEnumerable<string> ParseUrl(string url)
        {
            var matchResult = _urlPatten.Match(url);
            if (!matchResult.Success)
            {
                yield return url;
            }
            else
            {
                int start = int.Parse(matchResult.Groups[2].Value);
                int end = int.Parse(matchResult.Groups[5].Value);
                string format = _urlPatten.Replace(url, "{0}");
                for (int i = start; i <= end; i++)
                {
                    yield return string.Format(format, i);
                }
            }
        }
    }
}
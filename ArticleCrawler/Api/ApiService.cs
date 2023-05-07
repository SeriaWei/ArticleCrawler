using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ArticleCrawler.Api
{
    public class ApiService
    {
        private HttpClient _httpClient;
        private AuthToken _authToken;
        private ConfigOption _configOption;

        public ApiService()
        {
            string configText = File.ReadAllText("apiconfig.json");
            _configOption = JsonSerializer.Deserialize<ConfigOption>(configText);
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_configOption.Host);
        }

        public async Task<ArticleEntity> Create(ArticleEntity article, HashSet<string> files)
        {
            if (_authToken == null || _authToken.Expires < DateTime.UtcNow)
            {
                string text = JsonSerializer.Serialize(new { userID = _configOption.UserID, passWord = _configOption.PassWord });
                var response = await _httpClient.PostAsync("/api/account/createtoken", new StringContent(text, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                string authJson = await response.Content.ReadAsStringAsync();
                _authToken = JsonSerializer.Deserialize<AuthToken>(authJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            var articleContext = new ArticleApiRequestContext();
            articleContext.Article = article;
            articleContext.Files = new List<ApiFile>();
            foreach (var item in files)
            {
                articleContext.Files.Add(new ApiFile { Path = item, FileBytes = File.ReadAllBytes("Files" + item) });
            }
            string articleJson = JsonSerializer.Serialize(articleContext, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/article/create");
            requestMessage.Content = new StringContent(articleJson, Encoding.UTF8, "application/json");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken.Token);
            var articleResponse = await _httpClient.SendAsync(requestMessage);
            string responseJson = await articleResponse.Content.ReadAsStringAsync();
            if (!articleResponse.IsSuccessStatusCode)
            {
                throw new Exception(responseJson);
            }
            var result = JsonSerializer.Deserialize<ApiResult<ArticleEntity>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine(result.ErrorMessage);
            }
            return result.Result;
        }
    }
}

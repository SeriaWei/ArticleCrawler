using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticleCrawler.Api
{
    public class ArticleApiRequestContext
    {
        public ArticleEntity Article { get; set; }
        public List<ApiFile> Files { get; set; }
    }
}

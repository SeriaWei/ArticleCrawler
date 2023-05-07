using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticleCrawler
{
    public class Mapping
    {
        public string ArticleItemsSelector { get; set; }
        public string ArticleLinkSelector { get; set; }
        public PropertyMapping[] IndexPage { get; set; }
        public PropertyMapping[] DetailPage { get; set; }
    }
}

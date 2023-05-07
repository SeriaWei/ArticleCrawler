using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArticleCrawler.Filters;

namespace ArticleCrawler
{
    public class PropertyMapping
    {
        public string Property { get; set; }
        public string Selector { get; set; }
        public object Value { get; set; }
        public bool IsHtml { get; set; }
        public string Attr { get; set; }
        public Filter[] Filters { get; set; }
    }
}

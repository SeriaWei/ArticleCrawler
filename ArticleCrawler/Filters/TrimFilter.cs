using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticleCrawler.Filters
{
    public class TrimFilter : IFilter
    {
        public string Process(string value, string[] args)
        {
            return value?.Trim();
        }
    }
}

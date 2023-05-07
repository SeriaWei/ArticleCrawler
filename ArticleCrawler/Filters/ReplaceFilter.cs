using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticleCrawler.Filters
{
    public class ReplaceFilter : IFilter
    {
        public string Process(string value, string[] args)
        {
            return value.Replace(args[0], args[1]);
        }
    }
}

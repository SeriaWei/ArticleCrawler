using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticleCrawler.Filters
{
    public class SubstringFilter : IFilter
    {
        public string Process(string value, string[] args)
        {
            if (args.Length == 1)
            {
                return value.Substring(int.Parse(args[0]));
            }
            return value.Substring(int.Parse(args[0]), int.Parse(args[1]));
        }
    }
}

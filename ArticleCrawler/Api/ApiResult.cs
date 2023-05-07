using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticleCrawler.Api
{
    public class ApiResult<T>
    {
        public T Result { get; set; }
        public string ErrorMessage { get; set; }
    }
}

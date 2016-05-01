using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib
{
    public class OTSDBException
    {
        public  int code { get; set;}
        public DateTime time { get; set; }
        public string message  { get; set; }
        public string details { get; set; }
        public string trace { get; set; }
         

    }

    class ErrorContainer
    {
        public OTSDBException error { get; set; }
    }
}

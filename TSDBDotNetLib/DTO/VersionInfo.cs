using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib
{
   public  class VersionInfo
    {
    
            public string timestamp { get; set; }
            public string host { get; set; }
            public string repo { get; set; }
            public string full_revision { get; set; }
            public string short_revision { get; set; }
            public string user { get; set; }
            public string repo_status { get; set; }
            public string version { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(
           this, Formatting.Indented);
        }
    }
}

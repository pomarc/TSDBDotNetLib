using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib
{
    public class Metric
    {
    

        public string Key { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public Metric(Metric baseMetric, string key)
        {
            Key = baseMetric.Key + "." + key;
            Tags = new Dictionary<string, string>(baseMetric.Tags);

        }

        public Metric (string key)
        {
            Tags = new Dictionary<string, string>();
            Key = key;
        }
        public Metric()
        {
            Tags = new Dictionary<string, string>();
        }

        public Metric(Metric baseMetric)
        {
            Key = baseMetric.Key ;
            Tags = new Dictionary<string, string>(baseMetric.Tags);
        }
    }
}

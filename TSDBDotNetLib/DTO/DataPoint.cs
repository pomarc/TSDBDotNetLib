using System;
using System.Collections.Generic;
using System.Text;

namespace TSDBDotNetLib
{
    public class DataPoint
    {
     

        
        //ctors
        public DataPoint()
        {
            Tags = new Dictionary<string, string>();
        }
        public DataPoint(string Metric)
        {
            Tags = new Dictionary<string, string>();
            this.Metric = Metric;
            this.Timestamp = DateTime.Now;
        }
        public DataPoint(string Metric, double Value) 
        {
            Tags = new Dictionary<string, string>();
            this.Metric = Metric;
            this.Value = Value;
            this.Timestamp = DateTime.Now;
        }
        public DataPoint(string Metric, double Value, System.DateTime TimeStamp ) 
        {
            Tags = new Dictionary<string, string>();
            this.Metric = Metric;
            this.Value = Value;
            this.Timestamp = TimeStamp;
        }
        public DataPoint(Metric metric, double value)
        {
            this.Metric = metric.Key;
            this.Tags = new Dictionary<string, string>(metric.Tags);
            this.Timestamp = DateTime.Now;
            this.Value = value;

        }
        public DataPoint(Metric metric, double value, DateTime timestamp)
        {
            this.Metric = metric.Key;
            this.Tags = new Dictionary<string, string>(metric.Tags);
            this.Timestamp = timestamp;
            this.Value = value;

        }

        //props
        public string Metric { get; set; }
        public Dictionary<string,string> Tags { get; set; }
        public double Value { get; set; }
        public ulong JTimestamp {
           get {
                    var epoch = new DateTime(1970, 1, 1);
                    var ret= (ulong) Timestamp.ToUniversalTime().Subtract(epoch).TotalMilliseconds;
                    return ret;
                }
        }
        public DateTime Timestamp { get; set; }
        public string TagsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in Tags.Keys)
                {
                    sb.AppendFormat("\"{0}\":\"{1}\",", item, Tags[item]);

                }
                var o=sb.ToString().TrimEnd(',');
          
                return o;
            }
        }

        //public object TagsStringTelnet {
        //    get
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        foreach (var item in Tags.Keys)
        //        {
        //            sb.AppendFormat(" {0} ={1} ", item, Tags[item]);

        //        }

        //        var o = sb.ToString();
        //        return o;
        //    }
        //}

        //mets

        public string ToJson()
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{{\"metric\":\"{0}\",\"timestamp\":{1},\"value\":{2},\"tags\":{{ {3} }}}}",
                Metric,
                JTimestamp.ToString(),
                Value.ToString().Replace(',','.'),
                TagsString);
            var o= sb.ToString();
            return o;
        }

        //public string toTelnet()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendFormat("put {0} {1} {2}  {3} ",
        //       this.Metric,
        //       this.JTimestamp.ToString(),
        //       this.Value,
        //       this.TagsStringTelnet);
        //    var o = sb.ToString();
        //    return o;
        //}
    }
}
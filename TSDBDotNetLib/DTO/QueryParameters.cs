using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib
{
    public enum Aggregators
    {
        
        min,
        sum,
        max,
        avg,
        dev
 
    }
 
    class QueryItem
    {
        public string aggregator { get; set; }
        public string metric { get; set; }
        public string rate { get; set; }
        public string downsample { get; set; }
        public Dictionary<string,string> tags { get; set; }
    }   

    class QueryDefinition
    {
        QueryParameters _queryParams;
        public QueryDefinition(QueryParameters queryParameters)
        {
            _queryParams = queryParameters;
        }

       
 
        public object start
        {
            get
            {
                if (_queryParams.StartTimeStamp != -1) return _queryParams.StartTimeStamp;
                var epoch = new DateTime(1970, 1, 1);
                if (_queryParams.StartDate != null) return _queryParams.StartDate.Value.ToUniversalTime().Subtract(epoch).TotalMilliseconds;
                if (_queryParams.StartString != null) return _queryParams.StartString;
                return "";
            }

        }
     
        public object end
        {
            get
            {
                if (_queryParams.EndTimeStamp != -1) return _queryParams.EndTimeStamp;
                var epoch = new DateTime(1970, 1, 1);
                if (_queryParams.EndDate != null) return _queryParams.EndDate.Value.ToUniversalTime().Subtract(epoch).TotalMilliseconds;
                if (_queryParams.EndString != null) return _queryParams.EndString;
                return "";
            }

        }


        public bool msResolution { get { return _queryParams.MillisecondResolution; } }


        public bool showTSUIDs { get  { return _queryParams.ShowTSUIDs; } }


      //  public bool delete { get { return _queryParams.Delete; } }




       public QueryItem[] queries
        {
            get
            {
                QueryItem queryItem = new QueryItem();
                queryItem.aggregator = _queryParams.Aggregator.ToString();
                queryItem.metric = _queryParams.Metric;
                queryItem.tags = new Dictionary<string, string>(_queryParams.Tags);
                return new QueryItem[] { queryItem };
            }
        }

        public string ToJson()
        {
          return   JsonConvert.SerializeObject(this);
        }
    }




    public  class QueryParameters
    {
        
        public QueryParameters()
        {
            Tags = new Dictionary<string, string>();
        }

        public long StartTimeStamp { get; set; } = -1;
        public DateTime? StartDate  { get; set; } = null;
        public string StartString { get; set; } = null;


        [JsonProperty(propertyName:"start")]
        public object Start
        {
            get
            {
                if (StartTimeStamp != -1) return StartTimeStamp;
                var epoch = new DateTime(1970, 1, 1);
                if (StartDate != null) return StartDate.Value.ToUniversalTime().Subtract(epoch).TotalMilliseconds;
                if (StartString != null) return StartString;
                return "";
            }
           
        }

        public long EndTimeStamp { get; set; } = -1;
        public DateTime? EndDate { get; set; } = null;
        public string EndString { get; set; } = null;

        [JsonProperty(propertyName: "end")]
        public object End
        {
            get
            {
                if (EndTimeStamp != -1) return EndTimeStamp;
                var epoch = new DateTime(1970, 1, 1);
                if (EndDate != null) return EndDate.Value.ToUniversalTime().Subtract(epoch).TotalMilliseconds;
                if (EndString != null) return EndString;
                return "";
            }

        }

         
        public bool MillisecondResolution { get; set; }

       
        public bool ShowTSUIDs { get; set; }


        //only in 2.2+
        public bool Delete { get; set; }

         

        public Aggregators Aggregator {get;set;}

        public string Metric { get; set; }
        public bool Rate { get; set; }
        public string DownSample { get; set; }
        public Dictionary<string,string> Tags { get; set; }

        public string ToJson()
        {
            return (new QueryDefinition(this).ToJson());
        }
        
    }
}

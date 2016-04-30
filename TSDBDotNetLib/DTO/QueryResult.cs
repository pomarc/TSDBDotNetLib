using System.Collections.Generic;

namespace TSDBDotNetLib
{

 
    
    public class QueryResult
    {
        public string metric { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public List<string> aggregatedTags { get; set; }
        //public List<Annotation> annotations { get; set; }
        //public List<GlobalAnnotation> globalAnnotations { get; set; }
        public List<string> tsuids { get; set; }
        public  Dictionary<string,double> dps { get; set; }
    }
}
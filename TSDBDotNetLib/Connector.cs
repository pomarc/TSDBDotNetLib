using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace TSDBDotNetLib
{
    public class Connector
    {
        private Uri _ServerUri = new Uri("http://127.0.0.1:4242");     
        private bool _useDiagnostics = true;


        private Queue<DataPoint> _datapointsQueue { get; set; }
        public VersionInfo VersionInfos { get; set; }

        public PutOptions           HttpPutOptions { get; set; }
        public bool                 EnqueueFailedDatapoints { get; set; }
        public Diagnostics          DiagnosticCounters { get; set; }
        internal OTSDBException LastException { get; private set; }

        public delegate void OTSDBErrorDelegate(OTSDBException exception, Exception originalException);

        public event OTSDBErrorDelegate OnOTSDBError;
 

        public Connector(string url, bool enableDiagnostics=true )
        {
            _ServerUri = new Uri(url);
            DiagnosticCounters = new Diagnostics();
            _useDiagnostics = enableDiagnostics;
            HttpPutOptions = new PutOptions();
            VersionInfos = new VersionInfo();

        }


        public   async Task<bool> TestConnection()
        {

            try
            {
                if (_useDiagnostics)
                {
                    DiagnosticCounters.StartDate = DateTime.Now;
                }
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(_ServerUri + "api/version");
                http.SendChunked = false;
                http.Method = "GET";
 
                http.ContentType = "application/json";
                WebResponse response = await http.GetResponseAsync();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                
              
                var stream = response.GetResponseStream();
                sw.Stop();
                DiagnosticCounters.LastQueryElapsedMs = sw.ElapsedMilliseconds;
                StreamReader sr = new StreamReader(stream);
                string content = sr.ReadToEnd();
                DiagnosticCounters.LastQueryResponseSize = content.Count();
                VersionInfos = JsonConvert.DeserializeObject<VersionInfo>(content);

               
                return true;
            }
            catch (WebException exc)
            {
                
                if (exc.Response != null)
                {
                    StreamReader reader = new StreamReader(exc.Response.GetResponseStream());
                    var content = reader.ReadToEnd();
                    var error = JsonConvert.DeserializeObject<ErrorContainer>(content); ;
                    if (error != null)
                    {
                        LastException = error.error;
                        LastException.time = DateTime.Now;
                    }
                    else
                    {
                        LastException = null;
                    }

                    if (OnOTSDBError != null)
                    {
                        OnOTSDBError(LastException, exc);
                    }

                }
                 
                return false;
            }
            catch (Exception ex)
            {
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(null, ex);
                }
                return false;
            }

          

        }

        public async Task<PutResult> PutAsyncHttp(DataPoint[] dataPoints)
        {
            PutResult result = new PutResult();
            try
            {
                if (_useDiagnostics)
                {
                    DiagnosticCounters.SentDatapoints+=(ulong)dataPoints.Count();
                }



                var uri = new Uri(_ServerUri + "api/put"+HttpPutOptions.ToString());
                var spm = ServicePointManager.FindServicePoint(uri);
                spm.Expect100Continue = false;
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(uri);
                http.SendChunked = false;

                http.Method = "POST";

                http.ContentType = "application/json";

                Encoding encoder = Encoding.UTF8;

                var dataPointCount = dataPoints.Count();
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
            

                foreach (var item in dataPoints)
                {
                    dataPointCount--;
                    sb.Append(item.ToJson());
                    if (dataPointCount > 0)
                    {
                        sb.Append(",");
                    }
                }

                sb.Append("]");

             //   Console.WriteLine(sb.ToString());

                byte[] data = encoder.GetBytes(sb.ToString());

                http.Method = "POST";
                http.ContentType = "application/json; charset=utf-8";
                http.ContentLength = data.Length;
                using (Stream stream = http.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }

                this.DiagnosticCounters.PendingPutRequests++;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                //request -------------
                WebResponse response = await http.GetResponseAsync();

                sw.Stop();
                DiagnosticCounters.LastQueryElapsedMs = sw.ElapsedMilliseconds;

                var streamOutput = response.GetResponseStream();

                StreamReader sr = new StreamReader(streamOutput);
                string content = sr.ReadToEnd();

                DiagnosticCounters.LastQueryResponseSize = response.ContentLength;
                PutDetails details = null;
                if (HttpPutOptions.Details || HttpPutOptions.Summary)
                {
                    //try to parse the response

                    details = Newtonsoft.Json.JsonConvert.DeserializeObject<PutDetails>(content);
                    result.details = details;
                }


                    if (_useDiagnostics && details!=null)
                {
                    
                        DiagnosticCounters.SuccessfulSentDatapoints += (ulong) details.success;
                        DiagnosticCounters.FailedSentDatapoints += (ulong)details.failed;
                        if (details.success > 0)
                        {
                            DiagnosticCounters.LastSuccessfulPut = DateTime.Now;
                        }
                       


                    }
                    else
                    {
                        DiagnosticCounters.SuccessfulSentDatapoints++;
                        DiagnosticCounters.LastSuccessfulPut = DateTime.Now;
                    }
                 
                 
            }
            catch (WebException exc)
            {
                
                LastException = null;
                if (exc.Response != null)
                {
                    StreamReader reader = new StreamReader(exc.Response.GetResponseStream());
                    var content = reader.ReadToEnd();
                    var error = JsonConvert.DeserializeObject<ErrorContainer>(content); ;
                    if (error != null)
                    {
                        LastException = error.error;
                        LastException.time = DateTime.Now;
                    }
                    
                }
              
                if (_useDiagnostics)
                {
                    DiagnosticCounters.FailedSentDatapoints+=(ulong)dataPoints.Count();
                }
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(LastException, exc);
                }

                result.OtsbException = LastException;
                result.originalException = exc;
            }
            catch (Exception exc)
            {

            
                if (_useDiagnostics)
                {

                    DiagnosticCounters.FailedSentDatapoints += (ulong)dataPoints.Count();
                }
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(null, exc);
                }
                result.originalException = exc;
            }
            finally
            {
                this.DiagnosticCounters.PendingPutRequests--;
            }

            return result;
        }
        public async Task<PutResult>  PutAsyncHttp(DataPoint dataPoint)
        {
            PutResult result = new PutResult();
            try
            {
               

                if (_useDiagnostics)
                {
                    DiagnosticCounters.SentDatapoints++;
                }

                    
                 var uri = new Uri(_ServerUri+"/api/put"+HttpPutOptions.ToString());
                var spm = ServicePointManager.FindServicePoint(uri);
                spm.Expect100Continue = false;
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(uri); 
                http.SendChunked = false;

                http.Method = "POST";
                
                http.ContentType = "application/json";
                 
                Encoding encoder = Encoding.UTF8;
                byte[] data = encoder.GetBytes(dataPoint.ToJson());

                http.Method = "POST";
                http.ContentType = "application/json; charset=utf-8";
                http.ContentLength = data.Length;
                using (Stream stream = http.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }

                this.DiagnosticCounters.PendingPutRequests++;
                WebResponse response = await http.GetResponseAsync();

                var streamOutput = response.GetResponseStream();
                StreamReader sr = new StreamReader(streamOutput);
                string content = sr.ReadToEnd();

                PutDetails details = null;
                if (HttpPutOptions.Details || HttpPutOptions.Summary)
                {
                    //try to parse the response

                    details = Newtonsoft.Json.JsonConvert.DeserializeObject<PutDetails>(content);
                    result.details = details;
                }
                if (_useDiagnostics)
                {
                    DiagnosticCounters.SuccessfulSentDatapoints++;
                    DiagnosticCounters.LastSuccessfulPut = DateTime.Now;
                }
                
            }
            catch   (WebException exc)
            {
                LastException = null;
                if (exc.Response != null)
                {
                    StreamReader reader = new StreamReader(exc.Response.GetResponseStream());
                    var content = reader.ReadToEnd();
                    var error = JsonConvert.DeserializeObject<ErrorContainer>(content); ;
                    if (error != null)
                    {
                        LastException = error.error;
                        LastException.time = DateTime.Now;
                    }
                   
                   

                }
                if (_useDiagnostics)
                {
                    DiagnosticCounters.FailedSentDatapoints++;
                }
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(LastException, exc);
                }
                result.OtsbException = LastException;
                result.originalException = exc;
            }
            catch (Exception exc)
            { 

                if (_useDiagnostics)
                {
                    DiagnosticCounters.FailedSentDatapoints--;
                }
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(null, exc);
                }
                
                result.originalException = exc;
            }
            

            finally
            {
                this.DiagnosticCounters.PendingPutRequests--;
            }

            return result; ;
        }

        public async Task<QueryResult> QueryAsyncHttp(QueryParameters queryParameters)
        {
            QueryResult result = new QueryResult();
            try
            {
                
                var uri = new Uri(_ServerUri + "api/query"  );
                var spm = ServicePointManager.FindServicePoint(uri);
                spm.Expect100Continue = false;
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(uri);
                http.SendChunked = false;

                http.Method = "POST";

                http.ContentType = "application/json";

                Encoding encoder = Encoding.UTF8;


                var JsonQuery = queryParameters.ToJson();
                byte[] data = encoder.GetBytes(JsonQuery);

                http.Method = "POST";
                http.ContentType = "application/json; charset=utf-8";
                http.ContentLength = data.Length;
                using (Stream stream = http.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }
                var sw = System.Diagnostics.Stopwatch.StartNew();
                WebResponse response = await http.GetResponseAsync();
                sw.Stop();
                DiagnosticCounters.LastQueryElapsedMs = sw.ElapsedMilliseconds;

                var streamOutput = response.GetResponseStream();
                StreamReader sr = new StreamReader(streamOutput);
                string content = sr.ReadToEnd();

                DiagnosticCounters.LastQueryResponseSize = content.Count();

                var results = JsonConvert.DeserializeObject<QueryResult[]>(content);
                if (results==null|| results.Count() == 0)
                {
                    return new QueryResult();
                }
                return results[0];

            }
            catch (WebException exc)
            {
                if (exc.Response != null)
                {
                    StreamReader reader = new StreamReader(exc.Response.GetResponseStream());
                    var content = reader.ReadToEnd();
                    var error = JsonConvert.DeserializeObject<ErrorContainer>(content); ;
                    if (error != null)
                    {
                        LastException = error.error;
                        LastException.time = DateTime.Now;
                    }
                    else
                    {
                        LastException = null;
                    }

                    if (OnOTSDBError != null)
                    {
                        OnOTSDBError(LastException, exc);
                    }
                    return null;
                }
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(LastException, exc);
                }
                return null;
                
            }
            catch (Exception exc)
            {
                if (OnOTSDBError != null)
                {
                    OnOTSDBError(null, exc);
                }
                return null;

            }
            finally
            {
                
            }

             
          

        }

        

    }
}

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
        public Diagnostics          DIagnosticCounters { get; set; }
        internal OTSDBException LastException { get; private set; }

        

        public Connector(string url, bool enableDiagnostics=true )
        {
            _ServerUri = new Uri(url);
            DIagnosticCounters = new Diagnostics();
            _useDiagnostics = enableDiagnostics;
            HttpPutOptions = new PutOptions();
            VersionInfos = new VersionInfo();

        }


        public   async Task<bool> Connect()
        {

            try
            {
                if (_useDiagnostics)
                {
                    DIagnosticCounters.StartDate = DateTime.Now;
                }
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(_ServerUri + "api/version");
                http.SendChunked = false;
                http.Method = "GET";
                // http.Headers.Clear();
                //http.Headers.Add("Content-Type", "application/json");
                http.ContentType = "application/json";
                WebResponse response = await http.GetResponseAsync();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                
              
                var stream = response.GetResponseStream();
                sw.Stop();
                DIagnosticCounters.LastQueryElapsedMs = sw.ElapsedMilliseconds;
                StreamReader sr = new StreamReader(stream);
                string content = sr.ReadToEnd();
                DIagnosticCounters.LastQueryResponseSize = content.Count();
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

                }
                 
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }

          

        }

        public async Task PutAsyncHttp(DataPoint[] dataPoints)
        {
            try
            {
                if (_useDiagnostics)
                {
                    DIagnosticCounters.SentDatapoints+=(ulong)dataPoints.Count();
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

                this.DIagnosticCounters.PendingPutRequests++;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                //request -------------
                WebResponse response = await http.GetResponseAsync();

                sw.Stop();
                DIagnosticCounters.LastQueryElapsedMs = sw.ElapsedMilliseconds;

                var streamOutput = response.GetResponseStream();

                StreamReader sr = new StreamReader(streamOutput);
                string content = sr.ReadToEnd();

                DIagnosticCounters.LastQueryResponseSize = response.ContentLength;

                if (_useDiagnostics)
                {
                    if (HttpPutOptions.Details|| HttpPutOptions.Summary)
                    {
                        //try to parse the response

                        dynamic converter = Newtonsoft.Json.JsonConvert.DeserializeObject<PutDetails>(content);

                        DIagnosticCounters.SuccessfulSentDatapoints += (ulong) converter.success;
                        DIagnosticCounters.FailedSentDatapoints += (ulong)converter.failed;
                        if (converter.success > 0)
                        {
                            DIagnosticCounters.LastSuccessfulPut = DateTime.Now;
                        }
                       


                    }
                    else
                    {
                        DIagnosticCounters.SuccessfulSentDatapoints++;
                        DIagnosticCounters.LastSuccessfulPut = DateTime.Now;
                    }
                 
                }

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
                }
              
                if (_useDiagnostics)
                {
                    DIagnosticCounters.FailedSentDatapoints+=(ulong)dataPoints.Count();
                }
            }
            catch (Exception exc)
            {
            
                if (_useDiagnostics)
                {
                    DIagnosticCounters.FailedSentDatapoints += (ulong)dataPoints.Count();
                }
            }
            finally
            {
                this.DIagnosticCounters.PendingPutRequests--;
            }

            return;
        }
        public async Task  PutAsyncHttp(DataPoint dataPoint)
        {
            try
            {
                if (_useDiagnostics)
                {
                    DIagnosticCounters.SentDatapoints++;
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

                this.DIagnosticCounters.PendingPutRequests++;
                WebResponse response = await http.GetResponseAsync();

                var streamOutput = response.GetResponseStream();
                StreamReader sr = new StreamReader(streamOutput);
                string content = sr.ReadToEnd();

                if (_useDiagnostics)
                {
                    DIagnosticCounters.SuccessfulSentDatapoints++;
                    DIagnosticCounters.LastSuccessfulPut = DateTime.Now;
                }
                
            }
            catch   (WebException exc)
            {
                if(exc.Response != null)
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

                }
                if (_useDiagnostics)
                {
                    DIagnosticCounters.FailedSentDatapoints++;
                }
            }
            catch (Exception exc)
            { 

                if (_useDiagnostics)
                {
                    DIagnosticCounters.FailedSentDatapoints--;
                }
            }
            

            finally
            {
                this.DIagnosticCounters.PendingPutRequests--;
            }

            return    ;
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
                DIagnosticCounters.LastQueryElapsedMs = sw.ElapsedMilliseconds;

                var streamOutput = response.GetResponseStream();
                StreamReader sr = new StreamReader(streamOutput);
                string content = sr.ReadToEnd();

                DIagnosticCounters.LastQueryResponseSize = content.Count();

                var results = JsonConvert.DeserializeObject<QueryResult[]>(content);
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

                }
                return null;
                
            }
            catch (Exception exc)
            {
                return null;

            }
            finally
            {
                
            }

             
          

        }

        

    }
}

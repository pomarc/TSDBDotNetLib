using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSDBDotNetLib;

namespace ConsoleTester
{
    class Program
    {

        static Connector _connector;
        private static Connector _diagnosticConnector;
        private static Metric _diagnosticMetric;

        static async Task GoPut()
        {

            try
            {


                var connectResult = await _connector.Connect();
                if (connectResult)
                {
                    ulong counter = 0;

                    Console.WriteLine("connect successful.");
                    Console.WriteLine(_connector.VersionInfos);
                    try
                    {
                        while (true)
                        {

                            counter = counter > 10000000 ? 0 : ++counter;

                            var rnd = new Random();

                            _connector.HttpPutOptions.Summary = true;
                            DataPoint datapoint = new DataPoint("metrica4.sottometrica.subsub", counter, DateTime.Now);

                            datapoint.Tags.Add("site", "rome");


                            DataPoint datapoint1 = new DataPoint("metrica4.sottometrica.subsub" , counter, DateTime.Now);

                            datapoint1.Tags.Add("site", "perugia");
                            DataPoint datapoint3 = new DataPoint("metrica4.sottometrica.subsub" , counter, DateTime.Now);

                            datapoint3.Tags.Add("site", "milan");


                            Metric systemEnv = new Metric() { Key = "system.env" };

                            Metric systemEnvRome = new Metric(systemEnv);
                            systemEnvRome.Tags.Add("site", "rome");

                            Metric systemEnvTemperatureRome = new Metric(systemEnvRome, "temperature");
                            Metric systemEnvHumidityRome = new Metric(systemEnvRome, "humidity");

                            Metric SystemEnvTemperatureParis = new Metric(systemEnv, "temperature");
                            SystemEnvTemperatureParis.Tags.Add("site", "paris");


                            DataPoint datapoint4 = new DataPoint(systemEnvTemperatureRome, Math.Cos(counter / 50.0) * Math.Sin(counter / 50.0));
                            DataPoint datapoint5 = new DataPoint(systemEnvHumidityRome, Math.Cos(counter / 30.0));
                            DataPoint datapoint6 = new DataPoint(SystemEnvTemperatureParis, Math.Cos(Math.Sin(counter / 60.0)));

                            DataPoint[] datapoints = new DataPoint[] { datapoint1, datapoint, datapoint3, datapoint4, datapoint5, datapoint6 };


                            await _connector.PutAsyncHttp(datapoints);
                            DataPoint[] points = new DataPoint[1];
                            points[0] = new DataPoint(_diagnosticMetric, _connector.DiagnosticCounters.LastQueryElapsedMs);

                            await _diagnosticConnector.PutAsyncHttp(points);
                             System.Threading.Thread.Sleep(200);
                        }
                    }
                   catch    (Exception ex)
                    {
                        Console.WriteLine("aborting put, exception: " + ex.Message);
                       
                    }


                }
                else
                {

                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine("couldn't connect. aborting.");
                        Console.ResetColor();
                    

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("eccezione " + ex.Message);
            }

        }


        public static async Task GoQuery()
        {

            var connectResult = await _connector.Connect();
            if (connectResult)
            {
                ulong counter = 0;
                Console.WriteLine("connect successful.");
                Console.WriteLine(_connector.VersionInfos);
                // query
                Console.WriteLine("query...");
                QueryParameters queryParams = new QueryParameters();
                queryParams.StartString = "12h-ago";
                queryParams.EndString = "1m-ago";
                queryParams.Metric = "system.env.temperature";
                queryParams.Tags.Add("site", "rome");
                queryParams.MillisecondResolution = true;
                //queryParams.Delete = true;
                ;
                var results = _connector.QueryAsyncHttp(queryParams).Result;
           
                /* foreach (var i in results.dps)
                 {
                     Console.WriteLine(counter++ + ")" + UnixTimeStampToDateTime(Double.Parse(i.Key)).ToString("yyyyMMdd HH:mm:ss.fff") + ": " + i.Value);
                 }
                 */
                if (results == null)
                {
                    Console.WriteLine("error");
                }
                else
                {
                    if (results.dps == null)
                    {
                        Alert("no results.");
                    }
                    else
                    {
                        Console.WriteLine("found " + results.dps.Count()+" results");
                    }
                    
                }

                Console.WriteLine("elapsed: " + _connector.DiagnosticCounters.LastQueryElapsedMs + "ms");

                Console.WriteLine("size: " + _connector.DiagnosticCounters.LastQueryResponseSize + "chars");

                DataPoint[] points = new DataPoint[1];
                points[0] = new DataPoint(_diagnosticMetric, _connector.DiagnosticCounters.LastQueryElapsedMs);

                await _diagnosticConnector.PutAsyncHttp(points);
                   


            }
            else
            {

                Console.ForegroundColor = ConsoleColor.Red;
                
                Console.WriteLine("couldn't connect. aborting.");
                Console.ResetColor();
            }


        }



        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        static void Main(string[] args)
        {
            //you should put your server name in a server.txt text file inside the /bin/debug dir
            string serverName = "http://localhost:4242";
            try
            {
                 serverName = System.IO.File.ReadAllText("server.txt");
            }
            catch(Exception exc)
            {
                Alert("You should put your test server name in a server.txt text file inside the /bin/debug dir. Defaulting to http://localhost:4242");

            }

            _connector = new Connector(serverName);
            _connector.OnOTSDBError += _connector_OnOTSDBError;

            _diagnosticConnector = new Connector("http://localhost:4242");
            _diagnosticMetric = new Metric("diagnostics.elapsed");
            _diagnosticMetric.Tags.Add("server", "dockercloud");

            Console.WriteLine("press 1 for put, 2 for query then enter.");
            var c = Console.ReadLine();
            if (c.StartsWith("1"))
            {
                GoPut();
                while (true)
                {
                    Console.WriteLine(_connector.DiagnosticCounters.ToString());
                    System.Threading.Thread.Sleep(5000);
                }
                
            } else
            if (c.StartsWith("2"))
            {
                  GoQuery().Wait();
            }
            else
            {
                Alert("Wrong choice, quitting.");
            }

            Console.WriteLine("press any key");
            Console.ReadKey();


        }

        private static void _connector_OnOTSDBError(OTSDBException exc, Exception originalException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("exception. ");
            if (exc != null)
            {
                Console.Write(exc.time.ToLongTimeString()+" "+ exc.code+": "+exc.message );
                
            }
            else
            {
                Console.Write( originalException.Message);
            }
            Console.WriteLine("-");
            Console.ResetColor();
        }


        private static void Alert(string message )
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
          
            Console.WriteLine(DateTime.Now.ToLongTimeString()+"- "+message);
            Console.ResetColor();
        }
        private static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(DateTime.Now.ToLongTimeString() + "- " + message);
            Console.ResetColor();
        }

    }
}

[](#tsdbnetlib)TSDBDotNetLib
=========================

TSDBNetLib is a .Net library written in c\# to connect .NET applications to an OpenTSDB (open time series data base) server through the
http API. TSDBNetLib provides simple and efficient ways to save and
retrieve time series data points in a type safe manner.

#### [](#what-is-opentsdb)What is OpenTSDB?

> OpenTSDB is "The Scalable Time Series Database", that lets you "Store
> and serve massive amounts of time series data without losing
> granularity." Please visit [otsdb.net](otsdb.net) to learn more.

[](#version)Version
-------------------

Current version is `0.1.0`  The library is under *heavy*
development.
[](#installation-and-testing)Installation and testing
-----------------------------------------------------

You can git clone the repository and compile it to get the unit tests, a
simple console tester and the library itself. You can also add the
library project to your solution and add it to your project references.

The project TSDBTests is a Visual Studio 2015 Unit test program. These
tests should run and end all green, provided you set up a local OTSD
server listening on `http://localhost:4242`

If you need an OpenTSDB instance working in no time and do not want to
learn the intricacies of installing it (and installing Hadoop and HBase,
too) you can use one of the convenient Docker images you can find
online. A useful docker image of otsdb 2.2 can be found here:
[https://hub.docker.com/r/petergrace/opentsdb-docker/](https://hub.docker.com/r/petergrace/opentsdb-docker/)

A really useful tool to visualize the data you store in an OpenTSDB
database is [Grafana](http://grafana.org). It can use OTSDB as a data
source, and is really simple and powerful. It has been of great help in
the developing of this library.

[](#dependencies)Dependencies
-----------------------------

This library uses the nice [Newtonsoft
JSON.NET](https://github.com/JamesNK/Newtonsoft.Json) library.

[](#quick-start)QUICK start
---------------------------

The library can be used in various ways. The simplest way to send a
datapoint with the current timestamp, a `system.environment.temperature`
metric, the `server=host` tag and a value of 23 is this:

        var _connector = new TSDBDotNetLib.Connector("http://localhost:4242");
        var dataPoint = new TSDBDotNetLib.DataPoint("system.environment.temperature",23);
        datapoint.Tags.Add("server","host1");
        await _connector.PutHttpAsync(dataPoint);

a little more sophisticated use of the library can be seen in the
following example, where some reusable metric objects are created, three
datapoints are stored and a query is made:

            int _exceptioncounter = 0;
            string serverName = "http://localhost:4242";
            Connector connector = new Connector(serverName);
            var connectResult = await connector.TestConnection();
            if (connectResult)
            {
                ulong counter = 0;
                Console.WriteLine("connect successful.");
                Console.WriteLine(connector.VersionInfos);
                try
                {

                    connector.OnOTSDBError += (ex1, exo) =>
                    {
                        _exceptioncounter++;
                    };

                    connector.HttpPutOptions.Summary = true;

                    //let's define a base metric we can reuse
                    //these should be define elsewhere and reused
                    Metric systemEnv = new Metric() { Key = "system.dc.diagnostics" };

                    //let's make a base metric for the Rome site
                    Metric systemEnvRome = new Metric(systemEnv);
                    systemEnvRome.Tags.Add("site", "rome");

                    //let's define two metrics for the Rome site
                    Metric systemEnvTemperatureRome = new Metric(systemEnvRome, "temperature");
                    Metric systemEnvHumidityRome = new Metric(systemEnvRome, "humidity");

                    //same for Paris, just temp
                    Metric SystemEnvTemperatureParis = new Metric(systemEnv, "temperature");
                    SystemEnvTemperatureParis.Tags.Add("site", "paris");

                    //let's use the metrics for three new datapoints
                    DataPoint datapoint1 = new DataPoint(systemEnvTemperatureRome, tRome);
                    DataPoint datapoint2 = new DataPoint(systemEnvHumidityRome, hRome);
                    DataPoint datapoint3 = new DataPoint(SystemEnvTemperatureParis, tParis);

                    DataPoint[] datapoints = new DataPoint[] { datapoint1, datapoint2, datapoint3 };

                    //let's send the datapoints to the server
                    var results= await connector.PutHttpAsync(datapoints);

                    if (results.HasErrors)
                    {
                        Console.Write("an error has occourred: ");
                        if (results.OtsbException!= null)
                        {
                            Console.Write(results.OtsbException.message);
                        }
                        else
                        {
                            Console.Write(results.originalException.Message);
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("ok." +results.details.success+" points saved");
                    }

                    //let's wtite out the statistics:
                    Console.WriteLine(connector.DiagnosticCounters.ToString());

                    //let's ask back the temperature for the Rome datacenter and print it to the console:
                    QueryParameters queryParams = new QueryParameters();
                    queryParams.StartString = "12h-ago";
                    queryParams.EndString = "1m-ago";
                    queryParams.Metric = "system.dc.diagnostics.temperature";
                    queryParams.Tags.Add("site", "rome");
                    queryParams.MillisecondResolution = true;
                    queryParams.Aggregator = Aggregators.avg;

                    var queryResults = connector.QueryHttpAsync(queryParams).Result;
                    if (!queryResults.HasErrors){

                        foreach (var i in queryResults.dps)
                        {
                            Console.WriteLine(  UnixTimeStampToDateTime(Double.Parse(i.Key)).ToString("yyyyMMdd HH:mm:ss.fff") + ": " + i.Value);
                        }
                    }
                    else
                    {
                         Console.WriteLine("an error has occourred");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("aborting put, exception: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("couldn't connect. aborting.");
            }

[](#usage)Usage
---------------

TSDBNetLib uses the OTSD http API. You can get all the information you
need about this API from the [opentsdb http API
docs](http://opentsdb.net/docs/build/html/api_http/index.html).

In the next paragraphs the classes that can be used to store and query
datapoints are shown.

### [](#the-metric-class)The `Metric` class

This class is useful to encapsulate information about a metric and some
tags that can be used (and reused) to initalize datapoints. You can
easily assign the key on creation:

        Metric metric = new Metric("system.key.test");
        metric.Tags.Add("test", "value");

`metric.key` holds the string key defining the metric `metric.Tags` is a
`Dictionary<string,string>` holding the tags collection.

You can define a Metric and add its key and tags later:

        Metric metric = new Metric();
        metric.key=system.key.test;
        metric.Tags.Add("test", "value");

In OTSDB metrics are hyìierachical, so you could find it useful initializing
a metric from another:

        Metric metriSET = new Metric("system.environment.temperature");
        metriSET.Tags.Add("test", "value");
        Metric metricSETC = new Metric(metriSET,"cpu");
        metricSETC.Tags.Add("test2", "value2");
        //metricSETC.key is now "system.environment.temperature.cpu" and metricSETC.Tags holds both test and test2 tags

### [](#the-datapoint-class)The `DataPoint` class

A `DataPoint` object represents (guess what?) a data point. In OpenTSDB
a data point has a metric, a value and a timestamp. OTSDBNetLib offers
many constructors that you can use to initialize a datapoint:

        DataPoint dataPoint1= new DataPoint();
        dataPoint1.Metric="sys.speed";
        dataPoint1.TimeStamp=DateTime.Now; //this particular value can be omitted since DateTime.Now is the default value
        dataPoint1.Value=23.3;

or

        //specify the metric as a string
        DataPoint dataPoint1= new DataPoint("system.environment.temperature");
        dataPoint1.Tags.Add("site","rome");

        //specify the metric and value
        DataPoint dataPoint2= new DataPoint("system.environment.temperature",94);
        dataPoint2.Tags.Add("site","milan");

        //specify the metric, value and timestamp as DateTime
        DataPoint dataPoint3= new DataPoint("system.environment.temperature",94, timeVar); 
        dataPoint3.Tags.Add("site","perugia");

more quickly, you can initialize the datapoint using a `Metric` object
like this:

        Metric metriSET = new Metric("system.environment.temperature");
        metriSET.Tags.Add("site", "logsvalleybridge");

        DataPoint dataPoint5= new DataPoint(metriSET,12);
        DataPoint dataPoint4= new DataPoint(metriSET,94, timeVar);
        dataPoint5.Tags.Add("rack","1");

### [](#the-connector-class)The `Connector` class

The Connector class is the most important class of this library since it
has the responsibility of interacting with the database server. It has
methods to push data to the server, make queries, and check the version
of the server.

#### [](#the-constructor)The Constructor

The constructor for the connector class has this signature:

        public Connector(string url, bool enableDiagnostics=true )

where `url` is a string with the full Uri to the server, e.g.
`http://localhost:4242` and `enableDiagnostics` sets if performance and
diagnostics statistics must be collected for the active session.

> Note: the constructor does NOT perform any connection to the database.

#### [](#testing-the-connection-testconnection)Testing the connection: `TestConnection()`

Before trying to put or query datapoints, it is fine to check if the
connection to the server can be established. It is useful to get the
version info of the server too. This can be done using the following
method

    public   async Task<bool> TestConnection()

if successful, the Method asynchronously returns `true` and the
`Connector.VersionInfos` property is filled up. This is an instance of
the `VersionInfo` class:

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

The `ToString()` method is overridden to return an easy-to-read JSON
representation of the class.

> Note: the TestConnection() method does NOT keep the connection open.
> Each following put or query request open and close their own
> connections.

#### [](#sending-data-to-the-server-puthttpasync)Sending data to the server: `PutHttpAsync()`

There are two methods that send data to the OpenTsdb Server: one for a
single data point:

``` {lang="chsarp"}
    public async Task<PutResult> PutHttpAsync(DataPoint dataPoint)
```

and one for an array of data points:

``` {lang="chsarp"}
    public async Task<PutResult> PutHttpAsync(DataPoint[] dataPoints)
```

each method gives in return an instance of the `PutResult` class.

Before using `PutHttpAsync` you may want to set the options for the put
operation, setting the `Connector.HttpPutOptions` property members (you
can get more detailed info  about these options on the OpenTSDB API http
put page):

  Member      tpye   Meaning
  ----------- ------ ----------------------------------------------------------------------------------------------------------------------------
  `Details`   bool   tells the server to send detailed info back. It is recommended if you enable statistics to get precise results and errors
  `Summary`   bool   tells the server to send summary info back. It is a lighter version of `Details`
  `Sync`      bool   tells the server to send the response back only when the datapoint is safely written to disk.

##### [](#the-putresults-class)the `PutResults` class

Each call to `PutHttpAsync` returns an instance of this class:

``` {lang="chsarp"}
 public class PutResult
    {
        public bool HasErrors { get; set; } //true if an exception has occourred
        public OTSDBException OtsbException { get; set; }  //a decoded (when possible) error code from OpenTSDB
        public Exception originalException { get; set; } //the raise .NET exception, if any
        public PutDetails details { get; set; } //the decoded Summary or Details object returned from OpenTSDB
    }
```

whose members are instance of `System.Exception`, bool and

``` {lang="chsarp"}
 public class OTSDBException
    {
        public  int code { get; set;}
        public DateTime time { get; set; }
        public string message  { get; set; }
        public string details { get; set; }
        public string trace { get; set; }
    }

     public class PutDetails
    {
        public int success { get; set; }
        public int failed { get; set; }

    }
```

#### [](#querying-data-queryhttpasync)Querying data: `QueryHttpAsync()`

You can use the `Connector` object to make queries to the OpenTSDB
Server using the following method

``` {lang="chsarp"}
    public async Task<QueryResult> QueryHttpAsync(QueryParameters queryParameters)
```

The query parameters are well explained on the [http query API
page](http://opentsdb.net/docs/build/html/api_http/query/index.html#query-api-endpoints)
of the OpenTSDB Docs site.

`QueryHttpAsync` implements a single result set query. At this stage of
development OTSDBNetLib only implements single result sets.

The following code implements a simple query:

``` {lang="chsarp"}
    QueryParameters queryParams = new QueryParameters();
    queryParams.StartString = "12h-ago";
    queryParams.EndString = "1m-ago";
    queryParams.Metric = "system.env.temperature";
    queryParams.Tags.Add("site", "rome");
    queryParams.MillisecondResolution = true;
    queryParams.Delete = false;
    queryParams.Aggregator = Aggregators.avg;
    var results = _connector.QueryHttpAsync(queryParams).Result;
```

as you can see, the start and end date are here expressed as string
`     queryParams.StartString = "12h-ago";` in the relative time
language of OTSDB. There are two other ways that you can specify start
or end date in: as .NET DateTime object
`queryParams.StartDate=DateTime.Now;` or as Javascript timestamp
`queryParams.StartTimeStamp=1462369786;`.

> CAUTION: Setting the `Delete` property of the `QueryParameters` class
> to `true` deletes from the server all the datapoints selected by the
> query. This option is available only on OTSB 2.2+. Using it on a
> server with a lower version results in an exception.

##### [](#the-queryresult-class)The `QueryResult` class

The Query method asynchronously returns an instance of this class:

``` {lang="chsarp"}
 public class QueryResult
    {

        public bool HasErrors { get; set; } //true if an exception has occourred
        public OTSDBException OtsbException { get; set; }  //a decoded (when possible) error code from OpenTSDB
        public Exception originalException { get; set; } //the raise .NET exception, if any
        public string metric { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public List<string> aggregatedTags { get; set; }
        public List<string> tsuids { get; set; }
        public  Dictionary<string,double> dps { get; set; }
    }
```

which is a subset of the standard OTSDB result JSON object plus the
properties useful in order to check if any errors has occourred.

> (hint: the returned datapoints are in the `dps` Dictionary)

#### [](#the-onotsdberror-event)The `OnOTSDBError` event

You can attach an event handler to the `Connector.OnOTSDBError` event
handler to get informed of any error occourrence in one of the operations
performed e.g.:

``` {lang="chsarp"}
    _connector = new Connector(serverName);
    _connector.OnOTSDBError += _connector_OnOTSDBError;
    (...)
     private static void _connector_OnOTSDBError(OTSDBException exc, Exception originalException)
        {
            if (exc != null)
            {
                Console.WriteLine(exc.time.ToLongTimeString()+" "+ exc.code+": "+exc.message );
            }
        else
            {
                Console.WriteLine( originalException.Message);
            }
        }
```

#### [](#retrieving-statistics-and-diagnostics-data-on-the-current-session-the-diagnosticcounters-property)Retrieving Statistics and Diagnostics data on the current session: the `DiagnosticCounters` property

If you started the `Connector` object with the diagnostics enabled, you
can periodically obtain info on the current session.

This class exposes the following information:

  Property                     Use
  ---------------------------- -------------------------------------------
  `StartDate`                  DateTime of first connection
  `LastSuccessfulPut`          DateTime of last successful put operation
  `SentDatapoints`             Number of tentatively sent data points
  `SuccessfulSentDatapoints`   Number of data points successfully sent
  `FailedSentDatapoints`       Number of data points unsuccessfully sent
  `PendingPutRequests`         Number of put http requests still pending
  `LastQueryElapsedMs`         duration of last query in milliseconds
  `LastQueryResponseSize`      length of last response in chars

and the following Methods:

``` {lang="chsarp"}
 public void Reset() //resets all counters
```

and

``` {lang="chsarp"}
 override public  string  ToString();  
```

which returns a string in the form:

    Start Date: 04/05/2016 16:16:32
    Last Successful Put:04/05/2016 16:47:03
    Sent data points: 4218   Successful: 4218        sent-successful 0  Failed: 0    Pending put rqs:0
    Rate: 0,00230234997753106 put/ms
    Last query elapsed: 184ms       Last successful query response size:24 chars

[](#acknowldegments)Acknowldegments
-----------------------------------

Thanks to [http://opentsdb.net](http://opentsdb.net) for all the work
they've done. Thanks to [http://grafana.org](http://grafana.org) for the
wonderful tool. Thanks to [http://dillinger.io](http://dillinger.io) for
the nice md editor.


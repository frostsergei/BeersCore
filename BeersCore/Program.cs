using Apache.Ignite.Core;
using CsvHelper;
using System;
using System.Diagnostics;
using System.IO;

namespace BeersCore
{
    class Program
    {
        public const string VECTORS = "Vectors";
        public const string ALL = "All";

        static Stopwatch stopwatch = new Stopwatch();
        static Stopwatch stopwatchCalc = new Stopwatch();

        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Must be 5 arguments!");
                return;
            }
            string PATH = args[0].ToString();

            uint LIMIT; //Size is 4096 clustered!!!!
            bool checkLimit = uint.TryParse(args[1], out LIMIT);
            if (LIMIT < 4097) { Console.WriteLine("Minimal Size is 4096 elements!"); return; }

            int TOTAL_CORR;
            bool checkCorr = int.TryParse(args[2], out TOTAL_CORR);

            int DEBUG;
            bool checkDebug = int.TryParse(args[3], out DEBUG);

            string CONF = string.Empty;

            if (args.Length==5)
            CONF = args[4].ToString();

            stopwatch.Restart();
            using (var stream = new StreamReader(PATH))
            using (var csv = new CsvReader(stream))
            {
                var cfg = new IgniteConfiguration
                {
                    ClientMode = false,

                    /* DiscoverySpi = new TcpDiscoverySpi
                     {                                               
                         LocalPort = 51,
                         LocalPortRange = 4,

                         IpFinder = new TcpDiscoveryStaticIpFinder
                         {
                             // Addresses and port range of the nodes from the first cluster.
                             // 127.0.0.1 can be replaced with actual IP addresses or host names.
                             // The port range is optional.
                             Endpoints = new[] { "10.146.3.51" }
                         }
                     },*/
                };

                using (var ignite = (CONF==string.Empty)?(Ignition.Start()):(Ignition.Start(CONF)) )
                {
                    csv.Configuration.Delimiter = ",";

                    var records = csv.GetRecords<BeerReview>();

                    var cacheVectors = ignite.GetOrCreateCache<int, BeerReview>(VECTORS);
                    cacheVectors.RemoveAll();

                    var cacheAll = ignite.GetOrCreateCache<int, BeerReview>(ALL);
                    cacheAll.RemoveAll();

                    var streamerVector = ignite.GetDataStreamer<int, BeerReview>(cacheVectors.Name);
                    streamerVector.AllowOverwrite = true;

                    var streamerAll = ignite.GetDataStreamer<int, BeerReview>(cacheAll.Name);
                    streamerAll.AllowOverwrite = true;

                    int num = 0;
                    int numAll = 0;

                    foreach (var record in records)
                    {
                        if (record.valid())
                        {
                            streamerVector.AddData(num, record);
                            ++num;
                            if (num >= LIMIT) break;
                            //cacheVectors.Put(num,record);
                            //++num;
                            //if (num >= LIMIT) break;

                        }
                    }

                    if (TOTAL_CORR == 0)
                        foreach (var record in records)
                        {
                            if (record.valid())
                            {
                                streamerAll.AddData(numAll, record);
                                ++numAll;
                                //cacheAll.Put(numAll, record);
                                //++numAll;

                            }
                        }

                    //Calcualte
                    stopwatchCalc.Restart();

                    var solution = ignite.GetCompute().Call(new LinearRegressionCalculation(ignite, TOTAL_CORR));

                    stopwatchCalc.Stop();
                    stopwatch.Stop();

                    Console.WriteLine("{0} {1} {2}", args[1], args[2], args[3]); //Limit, TotalCorrelation, DebugMode
                    Console.WriteLine("Calculated up to {0} elements.", cacheVectors.GetSize());
                    Console.WriteLine("Result = {0} + {1} * aroma + {2} * appearance + {3} * palate + {4} * taste", solution.bias, solution.aroma, solution.appearance, solution.palate, solution.taste);
                    Console.WriteLine("Elapsed {0} miliseconds. Calculated in {1} miliseconds.", stopwatch.ElapsedMilliseconds, stopwatchCalc.ElapsedMilliseconds);
                    Console.WriteLine("Correlation = {0}\n", solution.correlation);

                    if (DEBUG != 0)
                        Console.ReadLine();
                }
            }
        }
    }
}

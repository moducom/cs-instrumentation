using Moducom.Instrumentation.Abstract;
using Prometheus.Client.Collectors.Abstractions;
using Prometheus.Client.MetricServer;
using System;
using System.Threading;

namespace Moducom.Instrumentation.Prometheus.TestServer
{
    class Program
    {
        static void NativeExporter()
        {

        }


        static void WrappedExporter(ICollectorRegistry registry)
        {
            // TODO: Utilize what minimal IoC tricks we have here
            var repo = new Repository(registry, "testexporter");
            var metricName = "test/metric1";

            Node testNode = repo[metricName];

            testNode.Description = "Service Provider+Facade test";

            INode node = testNode;

            var counter = node.GetMetric<ICounter>(new { delineator = 1 });
            var histogram = repo["test/metric3"].GetHistogram();

            var t = new Timer(delegate 
            {
                counter.Increment();

                histogram.Value = 5;

                // Once every 5 times, record also a histogram value of 1 and 5.1 just for visual
                // observation/testing
                if (counter.Value % 5 == 0)
                {
                    histogram.Value = 5.1;
                    histogram.Value = 1;
                }

            }, null, 0, 1000);

            repo["test/metric2"].GetGauge(new { delinerator = 2 }).Value = 77;
        }

        static void Main(string[] args)
        {
            var registry = new global::Prometheus.Client.Collectors.CollectorRegistry();
            // Be careful, explicit reference to "localhost" here binds us to ipv6
            //CollectorRegistry
            IMetricServer metricServer = new MetricServer(
                registry, 
                new MetricServerOptions() { Port = 9100 });

            metricServer.Start();

            WrappedExporter(registry);

            Console.WriteLine("Metric server running");
            Console.ReadLine();

            metricServer.Stop();
        }
    }
}

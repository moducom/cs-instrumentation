using Moducom.Instrumentation.Abstract;
using Prometheus.Client.MetricServer;
using System;

namespace Moducom.Instrumentation.Prometheus.TestServer
{
    class Program
    {
        static void NativeExporter()
        {

        }


        static void WrappedExporter()
        {
            // TODO: Utilize what minimal IoC tricks we have here
            var repo = new Moducom.Instrumentation.Prometheus.Repository();
            var metricName = "test/metric1";

            var testNode = repo[metricName];

            ((Node)testNode).Description = "Service Provider+Facade test";

            var counter = testNode.GetMetric<ICounter>(new { delineator = 1 });

            counter.Increment();
        }

        static void Main(string[] args)
        {
            // Be careful, explicit reference to "localhost" here binds us to ipv6
            IMetricServer metricServer = new MetricServer(9100);

            metricServer.Start();

            WrappedExporter();

            Console.WriteLine("Metric server running");
            Console.ReadLine();

            metricServer.Stop();
        }
    }
}

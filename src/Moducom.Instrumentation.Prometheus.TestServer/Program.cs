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

            var counter = testNode.GetMetric<ICounter>(new { instance = 1 });

            counter.Increment();
        }

        static void Main(string[] args)
        {
            IMetricServer metricServer = new MetricServer("localhost", 9100);

            metricServer.Start();

            WrappedExporter();

            Console.WriteLine("Metric server running");
            Console.ReadLine();

            metricServer.Stop();
        }
    }
}

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

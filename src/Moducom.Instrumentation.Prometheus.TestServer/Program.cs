using Prometheus.Client.MetricServer;
using System;

namespace Moducom.Instrumentation.Prometheus.TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IMetricServer metricServer = new MetricServer("localhost", 9100);

            metricServer.Start();

            Console.WriteLine("Metric server running");
            Console.ReadLine();

            metricServer.Stop();
        }
    }
}

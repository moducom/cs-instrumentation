using Prometheus.Client;
using Prometheus.Client.MetricsWriter.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Test
{
    // Prometheus specific
    internal class SyntheticMetricsWriter : IMetricsWriter, ISampleWriter
    {
        public Task CloseWriterAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }

        public IMetricsWriter EndMetric()
        {
            return this;
        }

        public IMetricsWriter EndSample()
        {
            return this;
        }

        public Task FlushAsync() => Task.CompletedTask;

        public ILabelWriter StartLabels()
        {
            throw new NotImplementedException();
        }

        public IMetricsWriter StartMetric(string metricName)
        {
            return this;
        }

        public ISampleWriter StartSample(string suffix = "")
        {
            return this;
        }

        public IMetricsWriter WriteHelp(string help)
        {
            return this;
        }

        public ISampleWriter WriteTimestamp(long timestamp)
        {
            return this;
        }

        public IMetricsWriter WriteType(MetricType metricType)
        {
            return this;
        }

        public ISampleWriter WriteValue(double value)
        {
            return this;
        }
    }
}

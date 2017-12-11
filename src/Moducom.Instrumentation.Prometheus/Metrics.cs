using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Prometheus
{
    public class CounterMetric : IMetricBase, ICounter
    {
        readonly global::Prometheus.Client.ICounter nativeCounter;

        public CounterMetric(global::Prometheus.Client.ICounter nativeCounter)
        {
            this.nativeCounter = nativeCounter;
        }

        public double Value => throw new NotImplementedException();

        public bool GetLabelValue(string label, out object value)
        {
            throw new NotImplementedException();
        }

        public void Increment(double byAmount)
        {
            nativeCounter.Inc(byAmount);
        }

        public void SetLabels(object labels)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> Labels => throw new NotImplementedException();
    }
}

using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Prometheus
{
    public class GauageMetric : IMetricBase, IGauge
    {
        readonly global::Prometheus.Client.IGauge nativeGauge;

        public GauageMetric(global::Prometheus.Client.IGauge nativeGauge)
        {
            this.nativeGauge = nativeGauge;
        }

        public double Value
        {
            get => nativeGauge.Value;
            set => nativeGauge.Set(value);
        }

        public IEnumerable<string> Labels => throw new NotImplementedException();

        public void Decrement(double byAmount)
        {
            nativeGauge.Dec(byAmount);
        }

        public bool GetLabelValue(string label, out object value)
        {
            throw new NotImplementedException();
        }

        public void Increment(double byAmount)
        {
            nativeGauge.Inc(byAmount);
        }

        public void SetLabels(object labels)
        {
            throw new NotImplementedException();
        }
    }


    public class CounterMetric : IMetricBase, ICounter
    {
        readonly global::Prometheus.Client.ICounter nativeCounter;

        public CounterMetric(global::Prometheus.Client.ICounter nativeCounter)
        {
            this.nativeCounter = nativeCounter;
        }

        public double Value => nativeCounter.Value;

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

    public class CounterMetric2 : IMetricBase, ICounter
    {
        readonly global::Prometheus.Client.Counter parent;
        readonly global::Prometheus.Client.ICounter child;

        public double Value => child.Value;

        public CounterMetric2(global::Prometheus.Client.Counter nativeCounter, string[] labelValues)
        {
            this.parent = nativeCounter;
            this.child = nativeCounter.Labels(labelValues);
        }


        public bool GetLabelValue(string label, out object value)
        {
            throw new NotImplementedException();
        }

        public void Increment(double byAmount)
        {
            child.Inc(byAmount);
        }

        public void SetLabels(object labels)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> Labels => parent.LabelNames;
    }

    public class CounterMetric3 : IMetricBase, ICounter
    {
        global::Prometheus.Client.Contracts.Counter counter;

        public CounterMetric3(global::Prometheus.Client.Contracts.Counter counter)
        {
            this.counter = counter;
        }

        public IEnumerable<string> Labels => throw new NotImplementedException();

        public double Value => throw new NotImplementedException();

        public bool GetLabelValue(string label, out object value)
        {
            throw new NotImplementedException();
        }

        public void Increment(double byAmount)
        {
            throw new NotImplementedException();
        }

        public void SetLabels(object labels)
        {
            throw new NotImplementedException();
        }
    }

    public class CounterChildMetric : IMetricBase, ICounter
    {
        readonly global::Prometheus.Client.Counter.ThisChild nativeCounterChild;

        public IEnumerable<string> Labels => throw new NotImplementedException();

        public double Value => throw new NotImplementedException();

        public bool GetLabelValue(string label, out object value)
        {
            throw new NotImplementedException();
        }

        public void Increment(double byAmount)
        {
            throw new NotImplementedException();
        }

        public void SetLabels(object labels)
        {
            throw new NotImplementedException();
        }
    }
}

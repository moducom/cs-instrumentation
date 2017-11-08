using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IMetric
    {
    }


    public interface IGauge<T> : IMetric
    {
        void Set(T value);
    }


    public interface IGauge : IGauge<double> { }


    public interface ICounter<T> : IMetric
        where T: IComparable
    {
        void Increment(T byAmount);
        void Decrement(T byAmount);
    }

    public interface ICounter : ICounter<double> { }


    public interface IDescription : IMetric
    {
        string Description { set; }
    }

    public class Metric : IMetric
    {
        protected readonly IMetricValue underlyingValue;

        public Metric(IMetricValue metricValue) { underlyingValue = metricValue; }
    }


    public class GaugeMetric<T> : Metric, IGauge<T>
    {
        public GaugeMetric(IMetricValue metricValue) : base(metricValue) { }

        public void Set(T value)
        {
            underlyingValue.Value = value;
        }
    }

    public class CounterMetric : Metric, ICounter
    {
        public CounterMetric(IMetricValue metricValue) : base(metricValue) { }

        public void Decrement(double byAmount)
        {
            underlyingValue.Value = ((double)underlyingValue.Value) + byAmount;
        }

        public void Increment(double byAmount)
        {
            underlyingValue.Value = ((double)underlyingValue.Value) - byAmount;
        }
    }

    public class DescriptionMetric : Metric, IDescription
    {
        public DescriptionMetric(IMetricValue metricValue) : base(metricValue) { }

        public string Description { set => underlyingValue.Value = value; }
    }
}

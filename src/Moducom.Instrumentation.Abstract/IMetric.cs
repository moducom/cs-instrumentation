using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IMetricBase<T> : IMetricBase
    {
        T Value { get; }
    }


    public interface IMetric<T> : IMetricBase<T>
    {
        T Value { get; set; }
    }


    public interface ICounter<T> : IMetricBase<T>
        where T: IComparable
    {
        void Increment(T byAmount);
        void Decrement(T byAmount);
    }

    public interface ICounter : ICounter<double> { }

    public static class IMetricExtensions
    {
        public static object GetLabelValue(this Experimental.ILabelsProvider labelsProvider, string label)
        {
            if (!labelsProvider.GetLabelValue(label, out object value))
                throw new KeyNotFoundException(label);

            return value;
        }
    }
}

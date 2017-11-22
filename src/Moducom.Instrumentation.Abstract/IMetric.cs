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

    public interface IMetricSetter<T> : IMetricBase
    {
        T Value { set; }
    }


    public interface IMetric<T> : IMetricBase<T>
    {
        T Value { get; set; }
    }


    public interface ICounter<T> : IMetricBase<T>
        where T: IComparable
    {
        // FIX: Kind of a flaw, you can throw a negative number into here
        // maybe a counter should really just be an integer counter and save the
        // decimal behaviors for IGauge
        void Increment(T byAmount);
    }


    /// <summary>
    /// Increment-only metric
    /// </summary>
    public interface ICounter : ICounter<double> { }

    public interface IGauge<T> : ICounter<T>
        where T : IComparable
    {
        // Mimicking prometheus approach, but I still feel an "adjust" might be more appropriate
        // rather than Increment/Decrement (again because it's all signed operations anyway)
        void Decrement(T byAmount);
    }

    public interface IGauge : IGauge<double> { }


    public interface IHistogram<T> : IMetricSetter<T> { }

    public static class IMetricExtensions
    {
        public static object GetLabelValue(this Experimental.ILabelsProvider labelsProvider, string label)
        {
            if (!labelsProvider.GetLabelValue(label, out object value))
                throw new KeyNotFoundException(label);

            return value;
        }


        public static void Increment(this ICounter counter)
        {
            counter.Increment(1);
        }
    }
}

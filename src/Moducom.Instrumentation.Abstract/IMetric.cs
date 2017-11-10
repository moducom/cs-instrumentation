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
        // FIX: Kind of a flaw, you can throw a negative number into here
        // maybe a counter should really just be an integer counter and save the
        // decimal behaviors for IGauge
        void Increment(T byAmount);
    }


    /// <summary>
    /// Increment-only metric
    /// </summary>
    public interface ICounter : ICounter<double> { }

    public interface IGauge<T> : IMetric<T>
    {
        void Decrement(T byAmount);
    }

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

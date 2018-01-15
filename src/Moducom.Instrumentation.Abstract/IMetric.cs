using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IValueGetter { }

    public interface IValueGetter<T> : IValueGetter
    {
        T Value { get; }
    }

    public interface IMetricBase<T> :
        IValueGetter<T>,
        IMetricBase
    {
    }

    public interface IMetricSetter<T> : IMetricBase
    {
        T Value { set; }
    }


    public interface IMetric<T> : IMetricBase<T>
    {
        new T Value { get; set; }
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

    public interface IGauge<T> : ICounter<T>, 
        IMetric<T>
        //IMetricSetter<T> // would prefer this approach but haven't quite cracked the nut on .NET get/set property in different interfaces
        where T : IComparable
    {
        // Mimicking prometheus approach, but I still feel an "adjust" might be more appropriate
        // rather than Increment/Decrement (again because it's all signed operations anyway)
        void Decrement(T byAmount);
    }

    public interface IGauge : IGauge<double> { }


    public interface IHistogram<T> : IMetricSetter<T>
    {
        IEnumerable<IHistogramNode<T>> Values { get; }
    }

    public interface IHistogramNode<T>
    {
        T Value { get; }
        DateTime TimeStamp { get; }
    }

    public static class IMetricExtensions
    {
        /// <summary>
        /// Get or create a counter metric qualified by the given labels
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static ICounter GetCounter(this Experimental.IMetricProvider provider, object labels = null) =>
            provider.GetMetric<ICounter>(labels);

        /// <summary>
        /// Get or create a gauge metric qualified by the given labels
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static IGauge GetGauge(this Experimental.IMetricProvider provider, object labels = null) =>
            provider.GetMetric<IGauge>(labels);

        public static object GetLabelValue(this Experimental.ILabelValueProvider labelsProvider, string label)
        {
            if (!labelsProvider.GetLabelValue(label, out object value))
                throw new KeyNotFoundException(label);

            return value;
        }


        public static void Increment(this ICounter counter)
        {
            counter.Increment(1);
        }


        //const DateTime _minValue = DateTime.MinValue;


        /// <summary>
        /// Get sum of all values, starting from the specified timestamp
        /// TODO: *might* want a binning version, or might just wait until we plug into proper metrics codebase
        /// </summary>
        /// <param name="histogram"></param>
        /// <param name="startFrom"></param>
        /// <returns></returns>
        /// <remarks>
        /// NOTE: Beware, this code probably won't work with a plugin provider like prometheus - pretty sure it won't
        /// expose the raw histogram data for us
        /// </remarks>
        public static double GetSum(this IHistogram<double> histogram, DateTime startFrom)
        {
            return histogram.Values.SkipWhile(x => x.TimeStamp < startFrom).Sum(x => x.Value);
        }


        /// <summary>
        /// Get count of all values, starting from the specified timestamp
        /// TODO: *might* want a binning version, or might just wait until we plug into proper metrics codebase
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="histogram"></param>
        /// <param name="startFrom"></param>
        /// <returns></returns>
        public static double GetCount<T>(this IHistogram<T> histogram, DateTime startFrom)
        {
            return histogram.Values.SkipWhile(x => x.TimeStamp < startFrom).Count();
        }


        /// <summary>
        /// Get average of all values starting from specified timestamp - no binning
        /// TODO: *might* want a binning version, or might just wait until we plug into proper metrics codebase
        /// </summary>
        /// <param name="histogram"></param>
        /// <param name="startFrom"></param>
        /// <returns></returns>
        public static double GetAverage(this IHistogram<double> histogram, DateTime startFrom)
        {
            var count = histogram.GetCount(startFrom);
            double average = histogram.GetSum(startFrom) / count;
            return average;
        }
    }
}

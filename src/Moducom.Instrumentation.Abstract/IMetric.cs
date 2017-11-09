using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IMetric<T> : IMetricBase
    {
        T Value { get; }
    }


    public interface ICounter<T> : IMetric<T>
        where T: IComparable
    {
        void Increment(T byAmount);
        void Decrement(T byAmount);
    }

    public interface ICounter : ICounter<double> { }
}

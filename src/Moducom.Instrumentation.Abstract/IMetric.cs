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
}

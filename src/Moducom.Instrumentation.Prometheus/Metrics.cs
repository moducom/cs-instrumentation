using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// These are all essentially wrappers around native prometheus metrics
/// </summary>
namespace Moducom.Instrumentation.Prometheus
{
    internal class Gauge : IGauge
    {
        readonly global::Prometheus.Client.IGauge nativeGauge;

        public Gauge(global::Prometheus.Client.IGauge nativeGauge)
        {
            this.nativeGauge = nativeGauge;
        }

        public double Value
        {
            get => nativeGauge.Value;
            set => nativeGauge.Set(value);
        }

        public void Decrement(double byAmount)
        {
            nativeGauge.Dec(byAmount);
        }

        public void Increment(double byAmount)
        {
            nativeGauge.Inc(byAmount);
        }
    }


    internal class Counter : ICounter
    {
        readonly global::Prometheus.Client.ICounter nativeCounter;

        public Counter(global::Prometheus.Client.ICounter nativeCounter)
        {
            this.nativeCounter = nativeCounter;
        }

        public double Value => nativeCounter.Value;

        public void Increment(double byAmount)
        {
            nativeCounter.Inc(byAmount);
        }
    }

}

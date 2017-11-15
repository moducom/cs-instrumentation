using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PRO = global::Prometheus;

namespace Moducom.Instrumentation.Prometheus
{
    internal class Node : INode
    {
        PRO.Contracts.MetricFamily metricsFamily;
        PRO.Client.Collectors.ICollector collector;
        Repository repository;
        PRO.Client.MetricFactory metricFactory = PRO.Client.Metrics.DefaultFactory;

        public IEnumerable<INode> Children => throw new NotImplementedException();

        // so that we can get fully-qualified name
        INode parent;

        string name;

        public string Name => name;

        protected string GetFullName()
        {
            INode node = this.parent;
            string fullname = name;

            while(node != null)
            {
                fullname = node.Name + "/" + fullname;
            }

            return fullname;
        }

        public void AddChild(INode child)
        {
            throw new NotImplementedException();

        }

        public void AddMetric(IMetricBase metric)
        {
            throw new NotImplementedException();

            var _metric = new PRO.Contracts.Metric();

            if (metric is ICounter)
            {
                var _c = new PRO.Contracts.Counter();
                //new CounterMetric(_c);
                // FIX: experimental and untested code
                _metric.counter = _c;
            }

            metricsFamily.metric.Add(_metric);
        }

        public T AddMetric<T>(string key = null) where T : IMetricBase
        {
            if(typeof(T) == typeof(ICounter))
            {
                if (metricsFamily != null)
                {
                    PRO.Client.Counter counter = metricFactory.CreateCounter(GetFullName(), "TBD");
                    collector = counter;
                    metricsFamily = counter.Collect();
                    return (T)(ICounter)new CounterMetric(counter);
                }
                else
                {
                    return default(T);
                }
            }
            else if(typeof(T) == typeof(IGauge<double>))
            {
                PRO.Client.Gauge gauge = metricFactory.CreateGauge(GetFullName(), "TBD");
            }
            throw new NotImplementedException();
        }

        public INode GetChild(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMetricBase> GetMetrics(object labels = null)
        {
            throw new NotImplementedException();
        }
    }


    public class MetricNode<T> : Abstract.Experimental.IMetricNode<T>
        where T: IMetricBase
    {
        public void AddMetric(IMetricBase metric)
        {
            throw new NotImplementedException();
        }

        public T1 AddMetric<T1>(string key = null) where T1 : IMetricBase
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMetricBase> GetMetrics(object labels = null)
        {
            throw new NotImplementedException();
        }

        public T Labels(object labels)
        {
            throw new NotImplementedException();
        }
    }
}

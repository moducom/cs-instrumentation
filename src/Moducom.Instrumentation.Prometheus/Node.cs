using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Prometheus
{
    internal class Node : INode
    {
        public IEnumerable<INode> Children => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public void AddChild(INode child)
        {
            throw new NotImplementedException();
        }

        public void AddMetric(IMetricBase metric)
        {
            throw new NotImplementedException();
        }

        public T AddMetric<T>(string key = null) where T : IMetricBase
        {
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

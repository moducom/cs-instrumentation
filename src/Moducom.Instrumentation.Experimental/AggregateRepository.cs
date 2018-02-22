using Moducom.Instrumentation.Abstract;
using Moducom.Instrumentation.Abstract.Experimental;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Experimental
{
    class AggregateRepository : IRepository
    {
        LinkedList<IRepository> repositories = new LinkedList<IRepository>();

        class Metric : ILabelsProvider, IValueGetter
        {
            public IEnumerable<string> Labels => throw new NotImplementedException();

            public bool GetLabelValue(string label, out object value)
            {
                throw new NotImplementedException();
            }
        }


        class Counter : Metric, ICounter
        {
            public double Value => throw new NotImplementedException();

            public void Increment(double byAmount)
            {
                throw new NotImplementedException();
            }
        }

        class Node : INode
        {
            public IEnumerable<INode> Children => throw new NotImplementedException();

            public IEnumerable<IMetric> Metrics => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public INode GetChild(string key)
            {
                throw new NotImplementedException();
            }

            public T GetMetric<T>(object labels = null, object options = null) where T : IValueGetter
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IMetric> GetMetrics(object labels)
            {
                throw new NotImplementedException();
            }
        }

        public INode this[string key] => throw new NotImplementedException();

        public INode RootNode => throw new NotImplementedException();
    }
}

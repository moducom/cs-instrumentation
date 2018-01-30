using Fact.Extensions.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Experimental
{
    class AggregateChildProvider<TKey, TChild> : IChildProvider<TKey, TChild>
    {
        LinkedList<IChildProvider<TKey, TChild>> providers = new LinkedList<IChildProvider<TKey, TChild>>();

        public IEnumerable<TChild> Children => throw new NotImplementedException();

        public TChild GetChild(TKey key)
        {
            throw new NotImplementedException();
        }


        public void AddAggregated(IChildProvider<TKey, TChild> aggregated)
        {
            providers.AddLast(aggregated);
        }
    }
}

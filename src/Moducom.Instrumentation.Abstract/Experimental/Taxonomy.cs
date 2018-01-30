using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Fact.Extensions.Collection;

namespace Moducom.Instrumentation.Experimental
{
    public abstract class TaxonomyBase<TNode, TINode> : TaxonomyBase<TNode>, ITaxonomy<TINode>
        where TNode: INamedChildProvider<TNode>, TINode
        where TINode: INamedChildProvider<TINode>, INamed
    {
        TINode IAccessor<string, TINode>.this[string key] => base[key];

        TINode ITaxonomy<TINode>.RootNode => RootNode;
    }


    public abstract class NamedChildCollection<TNode, TINode> :
        NamedChildCollection<TNode>,
        INamedChildCollection<TINode>
        where TNode: TINode
        where TINode: INamed
    {
        IEnumerable<TINode> IChildProvider<TINode>.Children => Children.Cast<TINode>();

        public NamedChildCollection(string name) : base(name) { }

        event Action<object, TINode> IChildCollection<TINode>.ChildAdded
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        TINode IChildProvider<string, TINode>.GetChild(string key) => GetChild(key);

        void IChildCollection<TINode>.AddChild(TINode child)
        {
            throw new NotImplementedException();
        }
    }
}

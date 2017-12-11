using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Moducom.Instrumentation.Abstract;

namespace Moducom.Instrumentation.Experimental
{
    public interface ITaxonomy<TNode>
        where TNode: Abstract.Experimental.INamed, Abstract.Experimental.IChildProvider<TNode>
    {
        TNode RootNode { get; }

        TNode this[string path] { get; }
    }

    public class Taxonomy
    {
        public class NodeBase<TNode> : 
            Abstract.Experimental.INamed,
            Abstract.Experimental.IChildCollection<TNode>
            where TNode: Abstract.Experimental.INamed
        {
            SparseDictionary<string, TNode> children;
            readonly string name;

            public string Name => name;

            public IEnumerable<TNode> Children => children.Values;

            public NodeBase(string name)
            {
                this.name = name;
            }

            /// <summary>
            /// TODO: Very likely would prefer a null back if no child, not an exception
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public TNode GetChild(string name)
            {
                children.TryGetValue(name, out TNode value);
                return value;
            }

            public void AddChild(TNode node) => children.Add(node.Name, node);
        }
    }

    public abstract class Taxonomy<TNode> : Taxonomy
        where TNode : Abstract.Experimental.INamed, Abstract.Experimental.IChildProvider<TNode>
    {
        public abstract TNode RootNode { get; }

        public virtual TNode CreateNode(string name) { return default(TNode); }

        TNode this[string path]
        {
            get
            {
                string[] splitPaths = path.Split('/');

                return RootNode.FindChildByPath(splitPaths, CreateNode);
            }
        }
    }
}

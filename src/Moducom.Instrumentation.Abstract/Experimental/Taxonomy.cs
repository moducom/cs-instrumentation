using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Moducom.Instrumentation.Abstract;

namespace Moducom.Instrumentation.Experimental
{
    public interface ITaxonomy<TNode, TINode>
        where TINode: Abstract.Experimental.INamed, Abstract.Experimental.IChildProvider<TINode>
        where TNode: TINode
    {
        TINode RootNode { get; }

        TINode this[string path] { get; }
    }

    public class Taxonomy
    {
        public class NodeBase<TNode, TINode> : 
            Abstract.Experimental.INamed,
            Abstract.Experimental.IChildCollection<TINode>
            where TINode: Abstract.Experimental.INamed
            where TNode: TINode
        {
            SparseDictionary<string, TINode> children;
            readonly string name;

            public string Name => name;

            public IEnumerable<TINode> Children => children.Values;

            public NodeBase(string name)
            {
                this.name = name;
            }

            /// <summary>
            /// TODO: Very likely would prefer a null back if no child, not an exception
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public TINode GetChild(string name)
            {
                children.TryGetValue(name, out TINode value);
                return value;
            }

            public void AddChild(TINode node) => children.Add(node.Name, node);
        }
    }

    public abstract class Taxonomy<TNode, TINode> : Taxonomy, ITaxonomy<TNode, TINode>
        where TINode : Abstract.Experimental.INamed, Abstract.Experimental.IChildProvider<TINode>
        where TNode : TINode
    {
        public abstract TINode RootNode { get; }

        protected virtual TNode CreateNode(string name) { return default(TNode); }

        /// <summary>
        /// Helper since cast didn't automatically happen via FindChildByPath
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private TINode _CreateNode(string name) => CreateNode(name);

        public TINode this[string path]
        {
            get
            {
                string[] splitPaths = path.Split('/');

                return RootNode.FindChildByPath(splitPaths, _CreateNode);
            }
        }
    }
}

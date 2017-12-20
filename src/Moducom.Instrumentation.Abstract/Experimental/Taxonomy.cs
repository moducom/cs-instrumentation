using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Moducom.Instrumentation.Abstract;

namespace Moducom.Instrumentation.Experimental
{
    public interface ITaxonomy<TNode, TINode>
        where TINode: Abstract.Experimental.INamed, IChildProvider<TINode>
        where TNode: TINode
    {
        /// <summary>
        /// Occurs only when a brand new node has been detected as created
        /// NOTE: wrapped taxonomies might not reliably fire this event
        /// </summary>
        event Action<object, TINode> NodeCreated;

        TINode RootNode { get; }

        TINode this[string path] { get; }
    }

    /// <summary>
    /// TODO: move this to a non-instrumentation-specific place (maybe fact.extensions.collections)
    /// TODO: removed INamed requirement (AddChild will need to take name as part of parameter)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IChildProvider<T>
    {
        IEnumerable<T> Children { get; }

        T GetChild(string name);
    }


    public interface IChildCollection<T> : IChildProvider<T>
    {
        /// <summary>
        /// Param #1 is sender
        /// Param #2 is added node
        /// </summary>
        event Action<object, T> ChildAdded;

        void AddChild(T child);
    }


    public interface IChild<T>
    {
        T Parent { get; }
    }


    public class Taxonomy
    {
        public class NodeBase<TNode, TINode> : 
            Abstract.Experimental.INamed,
            IChildCollection<TINode>
            where TINode: Abstract.Experimental.INamed
            where TNode: TINode
        {
            SparseDictionary<string, TINode> children;
            readonly string name;

            public string Name => name;

            public IEnumerable<TINode> Children => children.Values;

            public event Action<object, TINode> ChildAdded;

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

            public void AddChild(TINode node)
            {
                children.Add(node.Name, node);
                ChildAdded?.Invoke(this, node);
            }
        }
    }

    public abstract class Taxonomy<TNode, TINode> : Taxonomy, ITaxonomy<TNode, TINode>
        where TINode : Abstract.Experimental.INamed, IChildProvider<TINode>
        where TNode : TINode
    {
        public abstract TINode RootNode { get; }

        protected virtual TNode CreateNode(TINode parent, string name) { return default(TNode); }

        public event Action<object, TINode> NodeCreated;

        /// <summary>
        /// Helper since cast didn't automatically happen via FindChildByPath
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private TINode _CreateNode(TINode parent, string name)
        {
            var createdNode = CreateNode(parent, name);

            NodeCreated?.Invoke(this, createdNode);

            return createdNode;
        }

        public TINode this[string path]
        {
            get
            {
                string[] splitPaths = path.Split('/');

                return RootNode.FindChildByPath(splitPaths, _CreateNode);
            }
        }
    }


    public static class IChildExtensions
    {
        static bool DetectNullNameParent<T>(T node)
            where T : Abstract.Experimental.INamed, IChild<T>
        {
            return node.Parent.Name == null;
        }

        /// <summary>
        /// String child nodes together to produce something similar to a FQDN
        /// (fully qualified domain name)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="delimiter"></param>
        /// <param name="experimentalAbortProcessor"></param>
        /// <returns></returns>
        public static string GetFullName<T>(this T node, char delimiter = '/', 
            Func<T, bool> experimentalAbortProcessor = null)
            where T: Abstract.Experimental.INamed, IChild<T>
        {
            var fullName = node.Name;

            while(node.Parent != null)
            {
                if (experimentalAbortProcessor != null && experimentalAbortProcessor(node)) return fullName;

                node = node.Parent;

                // TODO: Ideally this would be more configurable, but will do
                // we skip path building/delimiter concatination if the node has no name
                if (node.Name == null) continue;

                fullName = node.Name + delimiter + fullName;
            }

            return fullName;
        }
    }


    public static class ITaxonomyExtensions
    {
        public static void Visit<TNode, TContext>(this TNode node, Action<TNode, TContext> visitor, TContext context, int level = 0)
            where TNode : IChildProvider<TNode>
            where TContext: new()
        {
            visitor(node, context);

            foreach (TNode childNode in node.Children)
            {
                context = new TContext();

                Visit(childNode, visitor, context, level + 1);
            }
        }


        public static void Visit<TNode>(this TNode node, Action<TNode> visitor, int level = 0)
            where TNode : IChildProvider<TNode>
        {
            visitor(node);

            foreach (TNode childNode in node.Children)
            {
                Visit(childNode, visitor, level + 1);
            }
        }


        public static void Visit<TNode, TINode>(this ITaxonomy<TNode, TINode> taxonomy, Action<TINode> visitor)
            where TINode : IChildProvider<TINode>, Abstract.Experimental.INamed
            where TNode : TINode
        {
            Visit(taxonomy.RootNode, visitor);
        }

        public static void Visit(this IRepository repository, Action<INode> visitor)
        {
            Visit(repository.RootNode, visitor);
        }
    }

}

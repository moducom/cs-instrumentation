using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    namespace Experimental
    {
        public interface IWithLabels
        {
            object GetLabelValue(string label);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="labels">Can be either an anonymous object or an IDictionary of string and object</param>
            void SetLabels(object labels);
        }

        public interface IWithValue
        {
            object Value { get; set; }
        }

        /// <summary>
        /// TODO: move this to a non-instrumentation-specific place (maybe fact.extensions.collections)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface IChildProvider<T>
            where T: INamed
        {
            IEnumerable<T> Children { get; }

            T GetChild(string name);

            void AddChild(T child);
        }

        public interface IWithChildren : IChildProvider<INode> { }

        /// <summary>
        /// TODO: Use Fact.Extensions version of this
        /// </summary>
        public interface INamed
        {
            string Name { get; }
        }
    }

    public interface INode :
        Experimental.IWithLabels,
        Experimental.IWithValue,
        Experimental.IWithChildren,
        Experimental.INamed
    {

    }


    public static class INodeExtensions
    {
        /// <summary>
        /// Stock standard tree traversal
        /// </summary>
        /// <param name="startNode">top of tree to search from</param>
        /// <param name="splitPaths">broken out path components</param>
        /// <param name="nodeFactory"></param>
        /// <returns></returns>
        public static INode FindNodeByPath(this INode startNode, IEnumerable<string> splitPaths, Func<string, INode> nodeFactory)
        {
            INode currentNode = startNode;

            foreach (var name in splitPaths)
            {
                INode node = currentNode.GetChild(name);

                if (node == null)
                {
                    // If no way to create a new node, then we basically abort (node not found)
                    if (nodeFactory == null) return null;

                    // TODO: have a configuration flag to determine auto add
                    node = nodeFactory(name);
                    currentNode.AddChild(node);
                }

                currentNode = node;
            }

            return currentNode;
        }
    }
}

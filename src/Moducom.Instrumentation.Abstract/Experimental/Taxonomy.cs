using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Moducom.Instrumentation.Abstract;
using Fact.Extensions.Collection;

namespace Moducom.Instrumentation.Experimental
{
#if !NET40
    using Fact.Extensions.Experimental;

    public abstract class TaxonomyBase<TNode, TINode> : TaxonomyBase<TNode>, ITaxonomy<TINode>
        where TNode: IChildCollection<TNode>, TINode
        where TINode: IChildCollection<TINode>, INamed
    {
        TINode IAccessor<string, TINode>.this[string key] => base[key];

        TINode ITaxonomy<TINode>.RootNode => RootNode;
    }
#endif

    public static class IChildExtensions
    {
        static bool DetectNullNameParent<T>(T node)
            where T : INamed, IChild<T>
        {
            return node.Parent.Name == null;
        }

        /// <summary>
        /// TODO: Put this into Collection code area too
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
            where T: INamed, IChild<T>
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
}

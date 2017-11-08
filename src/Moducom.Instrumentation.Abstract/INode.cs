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
        {
            IEnumerable<T> Children { get; }

            T GetChild(string name);
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
}

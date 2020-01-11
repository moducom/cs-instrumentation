#if NETSTANDARD1_1
#define USE_RUNTIMEPROPERTIES
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Moducom.Instrumentation.Abstract
{
    public class Utility
    {
        /// <summary>
        /// Kludgey DBNull-like mechanism for us to detect initialization mode vs acquisition mode
        /// </summary>
        /// <remarks>
        /// We depend on
        /// https://stackoverflow.com/questions/2778827/why-does-the-string-type-have-a-tostring-method
        /// Behavior to treat this particular string like a DBNull even if ToString() was run on it
        /// FIX: Due to https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-test-for-reference-equality-identity
        /// actually this string approach won't work
        /// </remarks>
        public static readonly string InitializerDefunct = "";

        public static readonly object Initializer = new object();

        /// <summary>
        /// Turn from either anonymous object or dictionary into a key/value label list
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        /// <remarks>TODO:Move this to a better location</remarks>
        public static IEnumerable<KeyValuePair<string, object>> LabelHelper(object labels)
        {
            if (labels == null) return Enumerable.Empty<KeyValuePair<string, object>>();

            if (labels is IDictionary<string, object> dictionaryLabels)
                return dictionaryLabels;
            else if (labels is string[] labelArray)
                return labelArray.Select(x => new KeyValuePair<string, object>(x, Initializer));
            else
                return from n in labels.GetType().
#if USE_RUNTIMEPROPERTIES
                       GetRuntimeProperties()
#else
                       GetProperties()
#endif
                       select new KeyValuePair<string, object>(n.Name, n.GetValue(labels, null));
            // NOTE: This was working, but doesnt now.  Not sure what circumstances this is OK for
            //select KeyValuePair.Create(n.Name, n.GetValue(labels, null));
        }
    }
}

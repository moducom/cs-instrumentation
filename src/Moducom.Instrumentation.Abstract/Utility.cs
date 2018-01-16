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

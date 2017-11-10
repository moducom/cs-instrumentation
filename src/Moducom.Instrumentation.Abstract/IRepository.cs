using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IRepository
    {
        INode RootNode { get; }

        INode this[string path] { get; }
    }

    public static class IRepositoryExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="path"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static ICounter GetCounterExperimental(this IRepository repository, string path, object labels = null)
        {
            return (ICounter) repository[path].GetMetric(null);
        }

        public static Experimental.ICounterNode GetCounterExperimental(this IRepository repository, string path)
        {
            return (Experimental.ICounterNode)repository[path];
        }
    }
}

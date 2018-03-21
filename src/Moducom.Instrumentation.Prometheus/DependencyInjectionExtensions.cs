using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="rootName"></param>
        public static void AddPrometheus(this IServiceCollection collection, string rootName)
        {
            collection.AddSingleton<Abstract.IRepository>(x => new Prometheus.Repository(rootName));
        }
    }
}

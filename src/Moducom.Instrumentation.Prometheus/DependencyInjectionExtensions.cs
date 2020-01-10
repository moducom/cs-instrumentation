using Microsoft.Extensions.DependencyInjection;
using Prometheus.Client.Collectors.Abstractions;
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
            // Prometheus.Repository itself is rather lightweight, so a transient is fine
            // It's the underlying ICollectorRegistry which does all the heavy lifting
            collection.AddTransient<Abstract.IRepository>(sp =>
            {
                var registry = sp.GetService<ICollectorRegistry>() ??
                    global::Prometheus.Client.Metrics.DefaultCollectorRegistry;

                return new Prometheus.Repository(registry, rootName);
            });
        }
    }
}

using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Moducom.Instrumentation.Experimental
{
    // TODO: Replace this with Fact.Extensions.Collection version
    public static class EnumerableExtensions
    {
        public static string ToString(this IEnumerable<string> strings, string delimiter)
        {
            string returnValue = null;

            foreach(var s in strings)
            {
                if (returnValue == null)
                    returnValue = s;
                else
                    returnValue += delimiter + s;
            }

            return returnValue;
        }
    }

    // TODO: Rename this to WriterDump or similar since it isnt text file specific
    public class TextFileDump
    {
        readonly IRepository repository;

        public TextFileDump(IRepository repository)
        {
            this.repository = repository;
        }


        protected void Dump(TextWriter writer, IMetricBase metric)
        {
            // FIX: have to compare if it's a gauge first, because gauge currently extends counter...
            if (metric is IGauge g)
            {
                writer.Write($"gauge = {g.Value}");
            }
            else if (metric is ICounter c)
            {
                writer.Write($"counter = {c.Value}");
            }

            writer.Write("  ");

            writer.WriteLine(metric.Labels.Select(n => n + '=' + metric.GetLabelValue(n)).ToString(","));
        }

        protected void Dump(TextWriter writer, INode node, int level)
        {
            //string indent = Enumerable.Repeat(' ', level).ToString();
            var indent = new string(' ', level * 2);

            writer.WriteLine(indent + "+ " + node.Name);

            foreach (IMetricBase metric in node.GetMetrics())
            {
                writer.Write(indent + "  - ");

                Dump(writer, metric);
            }

            foreach (INode childNode in node.Children)
            {
                Dump(writer, childNode, level + 1);
            }
        }

        public void Dump(TextWriter writer)
        {
            Dump(writer, repository.RootNode, 0);
        }
    }

#if !NETSTANDARD1_6
    public class TextFileDumpDaemon : IDisposable
    {
        Timer timer;
        readonly string filepath;
        readonly TextFileDump dump;
        readonly IRepository repository;

        public event Action<Exception> Error;
        public event Action Heartbeat;
        public event Action Shutdown;

        public TextFileDumpDaemon(string filepath, IRepository repository)
        {
            timer = new Timer(TimerCallback, null, 20000, 5000);
            this.filepath = filepath;
            dump = new TextFileDump(repository);
            this.repository = repository;
        }

        // Test empty call just to ensure we don't get optimized away
        // 100% unsure if it makes a difference
        public void DummyBegin()
        {
            // Would do proper logger but alas CLR issues still are present
            System.Diagnostics.Debug.WriteLine("DummyBegin called");
        }

        public void Dispose()
        {
            Shutdown?.Invoke();
            timer.Dispose();
        }

        protected void TimerCallback(object state)
        {
            try
            {
                Heartbeat?.Invoke();

                repository.GetCounterExperimental("internal/timer_callback").Increment();

                using (var writer = new StreamWriter(filepath, false))
                {
                    dump.Dump(writer);
                }
            }
            catch(Exception e)
            {
                Error?.Invoke(e);
            }
        }
    }
#endif
}

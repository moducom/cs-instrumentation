using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    public class TextFileDump
    {
        readonly IRepository repository;

        public TextFileDump(IRepository repository)
        {
            this.repository = repository;
        }

        protected void Dump(TextWriter writer, INode node, int level)
        {
            //string indent = Enumerable.Repeat(' ', level).ToString();
            var indent = new string(' ', level * 2);

            writer.WriteLine(indent + "+ " + node.Name);

            foreach (IMetricBase metric in node.GetMetrics())
            {
                writer.Write(indent + "  - ");

                if (metric is ICounter)
                {
                    writer.Write("counter = ");
                    writer.Write(((ICounter)metric).Value);
                }

                writer.Write("  ");

                writer.WriteLine(metric.Labels.Select(n => n + '=' + metric.GetLabelValue(n)).ToString(","));
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
}

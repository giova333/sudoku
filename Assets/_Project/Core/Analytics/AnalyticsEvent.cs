using System.Collections.Generic;
using System.Text;

namespace Sudoku.Core.Analytics
{
    /// <summary>
    /// One thing worth recording: a snake_case name and the parameters that
    /// describe it, common ones included. This is the whole vocabulary an
    /// <see cref="IAnalyticsService"/> has to understand.
    /// </summary>
    public readonly struct AnalyticsEvent
    {
        static readonly AnalyticsParameter[] None = new AnalyticsParameter[0];

        readonly AnalyticsParameter[] _parameters;

        public AnalyticsEvent(string name, AnalyticsParameter[] parameters)
        {
            Name = name;
            _parameters = parameters ?? None;
        }

        public string Name { get; }

        /// <summary>
        /// The common parameters first, then the ones particular to this event.
        /// The array is built for this event and handed over, so a backend that
        /// buffers can keep it.
        /// </summary>
        public IReadOnlyList<AnalyticsParameter> Parameters => _parameters ?? None;

        /// <summary>Looks a parameter up by key. For the console and for tests -
        /// a backend walks the list instead.</summary>
        public bool TryGet(string key, out AnalyticsParameter parameter)
        {
            foreach (var candidate in Parameters)
                if (candidate.Key == key)
                {
                    parameter = candidate;
                    return true;
                }

            parameter = default;
            return false;
        }

        public override string ToString()
        {
            var text = new StringBuilder(Name);
            text.Append(" {");

            var first = true;
            foreach (var parameter in Parameters)
            {
                if (!first) text.Append(", ");
                text.Append(parameter);
                first = false;
            }

            return text.Append('}').ToString();
        }
    }
}

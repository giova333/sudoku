using System;
using System.Globalization;

namespace Sudoku.Game.Settings
{
    /// <summary>
    /// One persisted preference: a value that can be read, written and watched.
    ///
    /// The value round-trips through invariant text, so bools, ints and enums
    /// all persist without the store knowing anything about them. That is what
    /// makes adding a preference a single declaration rather than a new case in
    /// a serializer and a new method on a service.
    /// </summary>
    public sealed class Preference<T> : IPreference
    {
        readonly IPreferenceStore _store;
        T _value;

        public Preference(IPreferenceStore store, string key, T fallback)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _value = Parse(store.Read(key, Format(fallback)), fallback);
        }

        public string Key { get; }

        public string ValueText => Format(_value);

        public event Action<IPreference> Changed;

        /// <summary>
        /// Assigning stores the value and announces it. Assigning the value it
        /// already holds is silent, so a screen that repaints itself cannot
        /// spam the change stream.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;

                _value = value;
                _store.Write(Key, Format(value));
                _store.Flush();
                Changed?.Invoke(this);
            }
        }

        /// <summary>
        /// Applies the preference now and on every later change.
        ///
        /// Almost every listener wants exactly this - the alternative is calling
        /// the same apply method twice, once at wire-up and once in a handler,
        /// which is where the two halves drift apart.
        /// </summary>
        public void Observe(Action<T> onValue)
        {
            if (onValue == null) throw new ArgumentNullException(nameof(onValue));

            onValue(_value);
            Changed += _ => onValue(_value);
        }

        static string Format(T value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        static T Parse(string text, T fallback)
        {
            if (string.IsNullOrEmpty(text)) return fallback;

            try
            {
                var type = typeof(T);
                if (type.IsEnum) return (T)Enum.Parse(type, text, true);
                return (T)Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                // A value written by an older build - or by a build that spelled
                // this preference differently - must never stop the game from
                // starting. The default is always a safe answer.
                return fallback;
            }
        }
    }
}

using Common.Extensions;
using Common.Utilities;

using System;
using System.Collections.Generic;

namespace Common.Values
{
    public class RecordedValue<TValue> : Disposable, IValue<TValue>
    {
        private bool _hasCapacity;

        private List<TValue> _allValues;

        private TValue _current;
        private TValue _previous;

        public event Action<TValue, TValue, IReadOnlyList<TValue>> OnValueChanged;

        public TValue Value
        {
            get => _current;
            set
            {
                _previous = _current;
                _current = value;

                if (_hasCapacity && (_allValues.Count + 1) < _allValues.Capacity)
                    _allValues.Add(_current);

                OnValueChanged.Call(_current, _previous, _allValues);
            }
        }

        public TValue Previous => _previous;

        public IReadOnlyList<TValue> AllValues => _allValues;

        public RecordedValue()
            => _allValues = new List<TValue>();

        public RecordedValue(TValue value)
        {
            _allValues = new List<TValue>();

            Value = value;
        }

        public RecordedValue(int size)
        {
            _allValues = new List<TValue>(size);
            _hasCapacity = true;
        }

        public RecordedValue(TValue value, int size)
        {
            _allValues = new List<TValue>(size);
            _hasCapacity = true;

            Value = value;
        }
    }
}

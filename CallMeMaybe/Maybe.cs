﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CallMeMaybe
{
    public struct Maybe<T> : IEquatable<Maybe<T>>, IEnumerable<T>, IMaybe
    {
        private readonly T _value;
        private readonly bool _hasValue;

        public bool HasValue
        {
            get { return _hasValue; }
        }

        public Maybe(T value)
        {
            // Since the whole purpose of this class is to avoid null values,
            // We're going to fail fast if someone tries providing one to us.
            if (ReferenceEquals(null, value))
            {
                throw new ArgumentNullException(
                    "value", "Cannot create a Maybe from a null value. Try Maybe.Empty<T>() instead.");
            }

            _hasValue = true;
            _value = value;
        }

        bool IMaybe.TryGetValue(out object value)
        {
            value = _value;
            return _hasValue;
        }

        public static implicit operator Maybe<T>(T value)
        {
            return new Maybe<T>(value);
        }

        public override string ToString()
        {
            return _hasValue ? _value.ToString() : "";
        }

        #region IEnumerable

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var collection = _hasValue ? new[] { _value } : new T[0];
            return collection.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }


        #endregion

        #region Equality

        bool IEquatable<Maybe<T>>.Equals(Maybe<T> other)
        {
            return Equals(other);
        }


        public override bool Equals(object obj)
        {
            // Maybe values never equal `null`--that's sort of the point.
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            // Maybe.From(1) == 1
            if (obj is T)
            {
                return _value.Equals((T) obj);
            }
            // Otherwise, Maybe values can only be compared with other Maybe values.
            var maybe = obj as IMaybe;
            if (maybe == null) return false;
            object value;
            if (!maybe.TryGetValue(out value))
            {
                // If the other one doesn't have a value, then we're
                // only "equal" if this one doesn't either.
                return !HasValue;
            }
            return Equals(_value, value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_hasValue.GetHashCode()*397) ^ EqualityComparer<T>.Default.GetHashCode(_value);
            }
        }

        public static bool operator ==(Maybe<T> left, Maybe<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Maybe<T> left, Maybe<T> right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    internal interface IMaybe
    {
        bool HasValue { get; }
        bool TryGetValue(out object value);
    }

    public static class Maybe
    {
        public static Maybe<T> From<T>(T value)
        {
            return new Maybe<T>(value);
        }

        public static Maybe<T> Empty<T>()
        {
            return new Maybe<T>();
        }
    }
}
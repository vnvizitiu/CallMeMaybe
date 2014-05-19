﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace CallMeMaybe
{

    // TODO: Fail-fast checks on all methods (specifically watch for lambdas)
    // TODO: Comment all methods and types
    // TODO: Investigate [EditorBrowsable] and [DebuggerDisplay] attributes
    // TODO: Investigate Resharper annotations: Pure, InstantHandle, and NotNull
    /// <summary>
    /// Container for an optional value that may or may not exist.
    /// </summary>
    /// <remarks>
    /// Using a <see cref="Maybe{T}"/> for optional values will help you to catch
    /// problems at compile-time that may otherwise have created
    /// <see cref="NullReferenceException"/>s at runtime.
    /// </remarks>
    /// <typeparam name="T">The type of value that this may or may not contain.</typeparam>
    public struct Maybe<T> : IEquatable<Maybe<T>>, IMaybe
    {
        private readonly T _value;
        private readonly bool _hasValue;

        /// <summary>
        /// Gets whether or not this <see cref="Maybe{T}"/> contains a value.
        /// </summary>
        public bool HasValue
        {
            get { return _hasValue; }
        }

        /// <summary>
        /// Constructs a <see cref="Maybe{T}"/> that contains the given value, or
        /// an empty <see cref="Maybe{T}"/> if the value is null.
        /// </summary>
        /// <param name="value">
        /// The value the <see cref="Maybe{T}"/> should contain. If null, the
        /// <see cref="Maybe{T}"/> will not contain a value.
        /// </param>
        public Maybe(T value)
        {
            // Since the whole purpose of this class is to avoid null values,
            // we're going to treat them as if they are "Not".
            _hasValue = !ReferenceEquals(null, value);
            _value = value;
        }

        /// <summary>
        /// Attempts to get the value.
        /// </summary>
        /// <param name="value">
        /// An out parameter that will be set to the value inside this <see cref="Maybe{T}"/>
        /// if it has one, or the default value for type <see cref="T"/> if not.
        /// </param>
        /// <returns>True if this <see cref="Maybe{T}"/> has a value, false otherwise.</returns>
        bool IMaybe.TryGetValue(out object value)
        {
            value = _value;
            return _hasValue;
        }

        /// <summary>
        /// Implicitly casts a value of type <see cref="T"/> to a <see cref="Maybe{T}"/>
        /// </summary>
        /// <param name="value">
        /// The value the <see cref="Maybe{T}"/> should contain. If null, the
        /// <see cref="Maybe{T}"/> will not contain a value.
        /// </param>
        /// <returns>
        /// A <see cref="Maybe{T}"/> that contains the given value, or
        /// an empty <see cref="Maybe{T}"/> if the value is null.
        /// </returns>
        public static implicit operator Maybe<T>(T value)
        {
            // Slight corner case: since Maybes are objects themselves,
            // C# will try to do an implicit conversion from a Maybe into
            // a Maybe<object>.
            // We need to unwrap the inner value of the other Maybe, and
            // use that as the value that this Maybe intends to be.
            if (typeof (T) == typeof (object) && value is IMaybe)
            {
                var otherMaybe = (IMaybe) value;
                object otherValue;

                if (otherMaybe.TryGetValue(out otherValue))
                {
                    value = (T) otherValue;
                }
            }

            return new Maybe<T>(value);
        }

        /// <summary>
        /// Implicitly casts a <see cref="MaybeNot"/> to a <see cref="Maybe{T}"/>
        /// </summary>
        /// <remarks>The existence of this operator allows consumers to use <see cref="Maybe.Not"/>
        /// in many cases where they would otherwise have needed to explicitly (and needlessly) 
        /// specify the type via <see cref="Maybe{T}.Not"/></remarks>
        /// <param name="maybeNot">A <see cref="MaybeNot"/> value.</param>
        /// <returns>A <see cref="Maybe{T}"/> without a value.</returns>
        public static implicit operator Maybe<T>(MaybeNot maybeNot)
        {
            return default(Maybe<T>);
        }
        // TODO: Consider implicit conversion from Maybe<Maybe<T>>
        // Note: I tried the conversion mentioned above, but it broke Resharper. Let's wait until that
        // bug gets fixed before working on it again.

        /// <summary>
        /// Gets a <see cref="Maybe{T}"/> without a value.
        /// </summary>
        public static Maybe<T> Not
        {
            get { return default (Maybe<T>); }
        }

        #region LINQ Methods

        [Pure]
        public Maybe<TValue> Select<TValue>(Func<T, TValue> selector)
        {
            return _hasValue ? selector(_value) : Maybe<TValue>.Not;
        }

        public Maybe<T> Where(Func<T, bool> criteria)
        {
            return _hasValue && criteria(_value) ? this : default(Maybe<T>);
        }

        public Maybe<TResult> SelectMany<TResult>(
            Func<T, Maybe<TResult>> resultSelector)
        {
            return _hasValue ? resultSelector(_value) : default(Maybe<TResult>);
        }

        public Maybe<TResult> SelectMany<TOther, TResult>(Func<T, Maybe<TOther>> otherSelector,
            Func<T, TOther, TResult> resultSelector)
        {
            if (_hasValue)
            {
                var otherMaybe = otherSelector(_value);
                if (otherMaybe._hasValue)
                {
                    return resultSelector(_value, otherMaybe._value);
                }
            }
            return default(Maybe<TResult>);
        }

        public T Single()
        {
            return ToList().Single();
        }

        public List<T> ToList()
        {
            return _hasValue ? new List<T>(1) { _value } : new List<T>(0);
        }

        // TODO: Any() method

        #endregion

        // TODO: Consider making this method return `this` so that we can chain calls.
        /// <summary>
        /// Performs the given action if this <see cref="Maybe{T}"/> has a value.
        /// Otherwise, this will do nothing at all.
        /// </summary>
        /// <param name="action">
        /// The action to perform if this <see cref="Maybe{T}"/> has a value.
        /// (The value will be given as the action's parameter).
        /// </param>
        public void Do(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            if (_hasValue)
            {
                action(_value);
            }
        }

        // TODO: Consider an ElseDo() method: would this really add anything?

        // TODO: Consider an Or() method (switch to another Maybe<> value if no value). Are there any real use cases for this?

        // TODO: Consider an ElseIf() method, so we can chain Maybe.If(...).ElseIf(...).Else(...);

        public T Else(Func<T> getValueIfNot)
        {
            if (getValueIfNot == null)
            {
                throw new ArgumentNullException("getValueIfNot");
            }
            return _hasValue ? _value : getValueIfNot();
        }

        public T Else(T valueIfNot)
        {
            return _hasValue ? _value : valueIfNot;
        }

        public override string ToString()
        {
            return _hasValue ? _value.ToString() : "";
        }

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
            // Maybe values can be compared with other Maybe values.
            var maybe = obj as IMaybe;
            if (maybe != null)
            {
                object value;
                if (!maybe.TryGetValue(out value))
                {
                    // If the other one doesn't have a value, then we're
                    // only "equal" if this one doesn't either.
                    return !HasValue;
                }
                return Equals(_value, value);
            }
            // If it's not a Maybe, then maybe it's a T
            // Maybe.From(1) == 1
            if (obj is T)
            {
                return _hasValue && _value.Equals((T)obj);
            }
            return false;
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

    public struct MaybeNot
    {
        public bool HasValue
        {
            get { return false; }
        }

        public override string ToString()
        {
            return "";
        }
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

        // TODO: Figure out how to do the equivalent of Not, with anonymous types?

        // TODO: Decide if we want Maybe.Not<T>() or Maybe<T>.Not, or both?
        public static readonly MaybeNot Not = new MaybeNot();

        public static Maybe<T> If<T>(bool condition, T valueIfTrue)
        {
            return condition ? valueIfTrue : Maybe<T>.Not;
        }

        public static Maybe<T> If<T>(bool condition, Func<T> valueIfTrue)
        {
            return condition ? valueIfTrue() : Maybe<T>.Not;
        }
    }
}

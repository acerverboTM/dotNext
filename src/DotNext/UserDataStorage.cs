﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNext
{
    using static Collections.Generic.Collection;
    using ReaderWriterSpinLock = Threading.ReaderWriterSpinLock;

    /// <summary>
    /// Provides access to user data associated with the object.
    /// </summary>
	/// <remarks>
    /// This is by-ref struct because user data should have
    /// the same lifetime as its owner.
	/// </remarks>
    [SuppressMessage("Design", "CA1066", Justification = "By-ref value type cannot implement interfaces")]
    public readonly ref struct UserDataStorage
    {
        /// <summary>
        /// Implementation of this interface allows to customize behavior of
        /// <see cref="ObjectExtensions.GetUserData{T}(T)"/> method.
        /// </summary>
        /// <remarks>
        /// If runtime type of object passed to <see cref="ObjectExtensions.GetUserData{T}(T)"/> method
        /// provides implementation of this interface then actual <see cref="UserDataStorage"/>
        /// depends on the <see cref="Source"/> implementation.
        /// It is recommended to implement this interface explicitly.
        /// </remarks>
        public interface IContainer
        {
            /// <summary>
            /// Gets the actual source of user data for this object.
            /// </summary>
            /// <remarks>
            /// If this property returns <c>this</c> object then user data has to be attached to the object itself;
            /// otherwise, use the data attached to the returned object.
            /// Additionally, you can store user data explicitly in the backing field which is initialized
            /// with real user data storage using <see cref="CreateStorage"/> method.
            /// </remarks>
            /// <value>The source of user data for this object.</value>
            object Source { get; }

            /// <summary>
            /// Creates a storage of user data that can be saved into field
            /// and returned via <see cref="Source"/> property.
            /// </summary>
            /// <returns>The object representing storage for user data.</returns>
            protected static object CreateStorage() => new BackingStorage();
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Supplier<T, V> : ISupplier<V>
        {
            private readonly T arg;
            private readonly ValueFunc<T, V> factory;

            internal Supplier(T arg, in ValueFunc<T, V> factory)
            {
                this.arg = arg;
                this.factory = factory;
            }

            V ISupplier<V>.Invoke() => factory.Invoke(arg);
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Supplier<T1, T2, V> : ISupplier<V>
        {
            private readonly T1 arg1;
            private readonly T2 arg2;
            private readonly ValueFunc<T1, T2, V> factory;

            internal Supplier(T1 arg1, T2 arg2, in ValueFunc<T1, T2, V> factory)
            {
                this.arg1 = arg1;
                this.arg2 = arg2;
                this.factory = factory;
            }

            V ISupplier<V>.Invoke() => factory.Invoke(arg1, arg2);
        }

        private sealed class BackingStorage : Dictionary<long, object?>
        {

            //ReaderWriterLockSlim is not used because it is heavyweight
            //spin-based lock is used instead because it is very low probability of concurrent
            //updates of the same backing storage.

            private ReaderWriterSpinLock lockState;

            //should be public because called through Activator by ConditionalWeakTable
            public BackingStorage()
                : base(3)
            {
            }

            private BackingStorage(IDictionary<long, object?> source)
                : base(source)
            {
            }

            internal BackingStorage Copy()
            {
                BackingStorage copy;
                lockState.EnterReadLock();
                copy = new BackingStorage(this);
                lockState.ExitReadLock();
                return copy;
            }

            internal void CopyTo(BackingStorage dest)
            {
                lockState.EnterReadLock();
                dest.lockState.EnterWriteLock();
                dest.Clear();
                dest.AddAll(this);
                dest.lockState.ExitWriteLock();
                lockState.ExitReadLock();
            }

            [return: NotNullIfNotNull("defaultValue")]
            [return: MaybeNull]
            internal V Get<V>(UserDataSlot<V> slot, [AllowNull]V defaultValue)
            {
                lockState.EnterReadLock();
                var result = slot.GetUserData(this, defaultValue);
                lockState.ExitReadLock();
                return result;
            }

            internal V GetOrSet<V, S>(UserDataSlot<V> slot, ref S valueFactory)
                where S : struct, ISupplier<V>
            {
                //fast path - read lock is required
                lockState.EnterReadLock();
                var exists = slot.GetUserData(this, out var userData);
                lockState.ExitReadLock();
                if (exists)
                    goto exit;
                //non-fast path: factory should be called
                lockState.EnterWriteLock();
                if (slot.GetUserData(this, out userData))
                    lockState.ExitWriteLock();
                else
                    try
                    {
                        userData = valueFactory.Invoke();
                        if (userData != null)
                            slot.SetUserData(this, userData);
                    }
                    finally
                    {
                        lockState.ExitWriteLock();
                    }
                exit:
                return userData;
            }

            internal bool Get<V>(UserDataSlot<V> slot, out V userData)
            {
                lockState.EnterReadLock();
                var result = slot.GetUserData(this, out userData);
                lockState.ExitReadLock();
                return result;
            }

            internal void Set<V>(UserDataSlot<V> slot, V userData)
            {
                lockState.EnterWriteLock();
                try
                {
                    slot.SetUserData(this, userData);
                }
                finally
                {
                    lockState.ExitWriteLock();
                }
            }

            internal bool Remove<V>(UserDataSlot<V> slot)
            {
                lockState.EnterWriteLock();
                var result = slot.RemoveUserData(this);
                lockState.ExitWriteLock();
                return result;
            }

            internal bool Remove<V>(UserDataSlot<V> slot, out V userData)
            {
                lockState.EnterWriteLock();
                var result = slot.GetUserData(this, out userData) && slot.RemoveUserData(this);
                lockState.ExitWriteLock();
                return result;
            }
        }

        private static readonly ConditionalWeakTable<object, BackingStorage> UserData = new ConditionalWeakTable<object, BackingStorage>();

        private readonly object source;

        internal UserDataStorage(object source) => this.source = source switch
        {
            null => throw new ArgumentNullException(nameof(UserDataStorage.source)),
            IContainer support => support.Source,
            _ => source,
        };

        private BackingStorage? GetStorage()
        {
            if (source is BackingStorage storage)
                return storage;
            return UserData.TryGetValue(source, out storage) ? storage : null;
        }

        private BackingStorage GetOrCreateStorage()
            => source is BackingStorage storage ? storage : UserData.GetOrCreateValue(source);

        /// <summary>
		/// Gets user data.
		/// </summary>
		/// <typeparam name="V">Type of data.</typeparam>
		/// <param name="slot">The slot identifying user data.</param>
		/// <param name="defaultValue">Default value to be returned if no user data contained in this collection.</param>
		/// <returns>User data.</returns>
        [return: NotNullIfNotNull("defaultValue")]
        [return: MaybeNull]
        public V Get<V>(UserDataSlot<V> slot, [AllowNull]V defaultValue)
        {
            var storage = GetStorage();
            return storage is null ? defaultValue : storage.Get(slot, defaultValue);
        }

        /// <summary>
		/// Gets user data.
		/// </summary>
		/// <typeparam name="V">Type of data.</typeparam>
		/// <param name="slot">The slot identifying user data.</param>
		/// <returns>User data; or <c>default(V)</c> if there is no user data associated with <paramref name="slot"/>.</returns>
        [return: MaybeNull]
        public V Get<V>(UserDataSlot<V> slot)
        {
            var storage = GetStorage();
            return storage is null ? default : storage.Get(slot, default);
        }

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <returns>The data associated with the slot.</returns>
        public V GetOrSet<V>(UserDataSlot<V> slot)
            where V : notnull, new()
        {
            var activator = ValueFunc<V>.Activator;
            return GetOrCreateStorage().GetOrSet(slot, ref activator);
        }

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="B">The type of user data associated with arbitrary object.</typeparam>
        /// <typeparam name="D">The derived type with public parameterless constructor.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <returns>The data associated with the slot.</returns>
        public B GetOrSet<B, D>(UserDataSlot<B> slot)
            where D : class, B, new()
        {
            var activator = ValueFunc<D>.Activator;
            return GetOrCreateStorage().GetOrSet(slot, ref activator);
        }

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="valueFactory">The value supplier which is called when no user data exists.</param>
        /// <returns>The data associated with the slot.</returns>
        [return: NotNull]
        public V GetOrSet<V>(UserDataSlot<V> slot, Func<V> valueFactory) => GetOrSet(slot, new ValueFunc<V>(valueFactory, true));

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="T">The type of the argument to be passed into factory.</typeparam>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="arg">The argument to be passed into factory.</param>
        /// <param name="valueFactory">The value supplier which is called when no user data exists.</param>
        /// <returns>The data associated with the slot.</returns>
        [return: NotNull]
        public V GetOrSet<T, V>(UserDataSlot<V> slot, T arg, Func<T, V> valueFactory)
            => GetOrSet(slot, arg, new ValueFunc<T, V>(valueFactory, true));

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument to be passed into factory.</typeparam>
        /// <typeparam name="T2">The type of the first argument to be passed into factory.</typeparam>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="arg1">The first argument to be passed into factory.</param>
        /// <param name="arg2">The second argument to be passed into factory.</param>
        /// <param name="valueFactory">The value supplier which is called when no user data exists.</param>
        /// <returns>The data associated with the slot.</returns>
        [return: NotNull]
        public V GetOrSet<T1, T2, V>(UserDataSlot<V> slot, T1 arg1, T2 arg2, Func<T1, T2, V> valueFactory)
            => GetOrSet(slot, arg1, arg2, new ValueFunc<T1, T2, V>(valueFactory, true));

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="valueFactory">The value supplier which is called when no user data exists.</param>
        /// <returns>The data associated with the slot.</returns>
        [return: NotNull]
        public V GetOrSet<V>(UserDataSlot<V> slot, in ValueFunc<V> valueFactory)
            => GetOrCreateStorage().GetOrSet(slot, ref Unsafe.AsRef(valueFactory));

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="T">The type of the argument to be passed into factory.</typeparam>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="arg">The argument to be passed into factory.</param>
        /// <param name="valueFactory">The value supplier which is called when no user data exists.</param>
        /// <returns>The data associated with the slot.</returns>
        [return: NotNull]
        public V GetOrSet<T, V>(UserDataSlot<V> slot, T arg, in ValueFunc<T, V> valueFactory)
        {
            var supplier = new Supplier<T, V>(arg, valueFactory);
            return GetOrCreateStorage().GetOrSet(slot, ref supplier);
        }

        /// <summary>
        /// Gets existing user data or creates a new data and return it.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument to be passed into factory.</typeparam>
        /// <typeparam name="T2">The type of the first argument to be passed into factory.</typeparam>
        /// <typeparam name="V">The type of user data associated with arbitrary object.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="arg1">The first argument to be passed into factory.</param>
        /// <param name="arg2">The second argument to be passed into factory.</param>
        /// <param name="valueFactory">The value supplier which is called when no user data exists.</param>
        /// <returns>The data associated with the slot.</returns>
        [return: NotNull]
        public V GetOrSet<T1, T2, V>(UserDataSlot<V> slot, T1 arg1, T2 arg2, in ValueFunc<T1, T2, V> valueFactory)
        {
            var supplier = new Supplier<T1, T2, V>(arg1, arg2, valueFactory);
            return GetOrCreateStorage().GetOrSet(slot, ref supplier);
        }

        /// <summary>
        /// Tries to get user data.
        /// </summary>
        /// <typeparam name="V">Type of data.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="userData">User data.</param>
        /// <returns><see langword="true"/>, if user data slot exists in this collection.</returns>
        public bool TryGet<V>(UserDataSlot<V> slot, [NotNullWhen(true)]out V userData)
        {
            var storage = GetStorage();
            if (storage is null)
            {
                userData = default!;
                return false;
            }
            else
                return storage.Get(slot, out userData);
        }

        /// <summary>
        /// Sets user data.
        /// </summary>
        /// <typeparam name="V">Type of data.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="userData">User data to be saved in this collection.</param>
        public void Set<V>(UserDataSlot<V> slot, [DisallowNull]V userData)
            => GetOrCreateStorage().Set(slot, userData);

        /// <summary>
        /// Removes user data slot.
        /// </summary>
        /// <typeparam name="V">The type of user data.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <returns><see langword="true"/>, if data is removed from this collection.</returns>
        public bool Remove<V>(UserDataSlot<V> slot)
        {
            var storage = GetStorage();
            return storage?.Remove(slot) ?? false;
        }

        /// <summary>
        /// Removes user data slot.
        /// </summary>
        /// <typeparam name="V">The type of user data.</typeparam>
        /// <param name="slot">The slot identifying user data.</param>
        /// <param name="userData">Remove user data.</param>
        /// <returns><see langword="true"/>, if data is removed from this collection.</returns>
        public bool Remove<V>(UserDataSlot<V> slot, [NotNullWhen(true)]out V userData)
        {
            var storage = GetStorage();
            if (storage is null)
            {
                userData = default!;
                return false;
            }
            else
                return storage.Remove(slot, out userData);
        }

        /// <summary>
        /// Replaces user data of the object with the copy of the current one.
        /// </summary>
        /// <param name="obj">The object which user data has to be replaced with the copy of the current one.</param>
        public void CopyTo(object obj)
        {
            if (obj is IContainer support)
                obj = support.Source;
            var source = GetStorage();
            if (source != null)
                if (obj is BackingStorage destination)
                    source.CopyTo(destination);
                else
                    UserData.Add(obj, source.Copy());
        }

        /// <summary>
        /// Computes identity hash code for this storage.
        /// </summary>
        /// <returns>The identity hash code for this storage.</returns>
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(source);

        /// <summary>
        /// Determines whether this storage is attached to
        /// the given object.
        /// </summary>
        /// <param name="other">Other object to check.</param>
        /// <returns><see langword="true"/>, if this storage is attached to <paramref name="other"/> object; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? other) => ReferenceEquals(source, other);

        /// <summary>
        /// Returns textual representation of this storage.
        /// </summary>
        /// <returns>The textual representation of this storage.</returns>
        public override string ToString() => source.ToString();

        /// <summary>
        /// Determines whether two stores are for the same object.
        /// </summary>
        /// <param name="first">The first storage to compare.</param>
        /// <param name="second">The second storage to compare.</param>
        /// <returns><see langword="true"/>, if two stores are for the same object; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(UserDataStorage first, UserDataStorage second)
            => ReferenceEquals(first.source, second.source);

        /// <summary>
        /// Determines whether two stores are not for the same object.
        /// </summary>
        /// <param name="first">The first storage to compare.</param>
        /// <param name="second">The second storage to compare.</param>
        /// <returns><see langword="true"/>, if two stores are not for the same object; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(UserDataStorage first, UserDataStorage second)
            => !ReferenceEquals(first.source, second.source);
    }
}
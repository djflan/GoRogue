﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GoRogue.Pooling
{
    /// <summary>
    /// A pseudo-implementation of the IListPool interface which doesn't actually do any pooling; eg. its functions
    /// simply return a new list or discard the one they receive.  Useful for specifying "don't pool" to a function
    /// or algorithm which takes a list pool.
    /// </summary>
    /// <typeparam name="T">Type of items in the list.</typeparam>
    public sealed class NoPoolingListPool<T> : IListPool<T>
    {
        /// <summary>
        /// Settable, but ignored.
        /// </summary>
        public int MaxLists { get; set; }

        /// <summary>
        /// Creates a new list and returns it.
        /// </summary>
        /// <returns>A newly allocated list.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> Rent() => new List<T>();

        /// <summary>
        /// Does nothing, simply allowing the given list to be GCed.
        /// </summary>
        /// <param name="list">List to "return".</param>
        /// <param name="clear">Ignored.</param>
        public void Return(List<T> list, bool clear = true)
        { }
    }
}

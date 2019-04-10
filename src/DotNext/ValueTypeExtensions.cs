using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNext
{
	/// <summary>
	/// Various extensions for value types.
	/// </summary>
    public static class ValueTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte ToByte<T>(T value) where T : struct, IConvertible => value.ToByte(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static sbyte ToSByte<T>(T value) where T : struct, IConvertible => value.ToSByte(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short ToInt16<T>(T value) where T : struct, IConvertible => value.ToInt16(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort ToUInt16<T>(T value) where T : struct, IConvertible => value.ToUInt16(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ToInt32<T>(T value) where T : struct, IConvertible => value.ToInt32(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ToUInt32<T>(T value) where T : struct, IConvertible => value.ToUInt32(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ToInt64<T>(T value) where T : struct, IConvertible => value.ToInt64(null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ToUInt64<T>(T value) where T : struct, IConvertible => value.ToUInt64(null);

        /// <summary>
        /// Obtain a value of type <typeparamref name="TO"/> by 
        /// reinterpreting the object representation of <typeparamref name="FROM"/>.
        /// </summary>
        /// <param name="input">A value to convert.</param>
        /// <param name="output">Conversion result.</param>
        /// <typeparam name="FROM">The type of input struct.</typeparam>
        /// <typeparam name="TO">The type of output struct.</typeparam>
        /// <seealso cref="ValueType{T}.BitCast{TO}(ref T, out TO)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BitCast<FROM, TO>(this FROM input, out TO output)
            where FROM : unmanaged
            where TO : unmanaged
            => ValueType<FROM>.BitCast(ref input, out output);

        /// <summary>
        /// Obtain a value of type <typeparamref name="TO"/> by 
        /// reinterpreting the object representation of <typeparamref name="FROM"/>. 
        /// </summary>
        /// <remarks>
        /// Every bit in the value representation of the returned <typeparamref name="TO"/> object 
        /// is equal to the corresponding bit in the object representation of <typeparamref name="FROM"/>. 
        /// The values of padding bits in the returned <typeparamref name="TO"/> object are unspecified. 
        /// The method takes into account size of <typeparamref name="FROM"/> and <typeparamref name="TO"/> types
        /// and able to provide conversion between types of different size.
        /// </remarks>
        /// <param name="input">A value to convert.</param>
        /// <typeparam name="FROM">The type of input struct.</typeparam>
        /// <typeparam name="TO">The type of output struct.</typeparam>
        /// <returns>Conversion result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TO BitCast<FROM, TO>(this FROM input)
            where FROM : unmanaged
            where TO : unmanaged
        {
            ValueType<FROM>.BitCast<TO>(ref input, out var output);
            return output;
        }

        /// <summary>
		/// Checks whether the specified value is equal to one
		/// of the specified values.
		/// </summary>
		/// <remarks>
		/// This method uses <see cref="IEquatable{T}.Equals(T)"/>
		/// to check equality between two values.
		/// </remarks>
		/// <typeparam name="T">The type of object to compare.</typeparam>
		/// <param name="value">The value to compare with other.</param>
		/// <param name="values">Candidate objects.</param>
		/// <returns><see langword="true"/>, if <paramref name="value"/> is equal to one of <paramref name="values"/>.</returns>
		public static bool IsOneOf<T>(this T value, IEnumerable<T> values)
            where T: struct, IEquatable<T>
        {
            foreach (var v in values)
				if (v.Equals(value))
					return true;
			return false;
        }

        /// <summary>
		/// Checks whether the specified value is equal to one
		/// of the specified values.
		/// </summary>
		/// <remarks>
		/// This method uses <see cref="IEquatable{T}.Equals(T)"/>
		/// to check equality between two values.
		/// </remarks>
		/// <typeparam name="T">The type of object to compare.</typeparam>
		/// <param name="value">The value to compare with other.</param>
		/// <param name="values">Candidate objects.</param>
		/// <returns><see langword="true"/>, if <paramref name="value"/> is equal to one of <paramref name="values"/>.</returns>
		public static bool IsOneOf<T>(this T value, params T[] values)
			where T: struct, IEquatable<T>
            => value.IsOneOf((IEnumerable<T>)values);

		/// <summary>
		/// Create boxed representation of the value type.
		/// </summary>
		/// <param name="value">Value to be placed into heap.</param>
		/// <typeparam name="T">Value type.</typeparam>
		/// <returns>Boxed representation of value type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueType<T> Box<T>(this T value)
			where T: struct
			=> new ValueType<T>(value);
    }
}
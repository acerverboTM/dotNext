﻿using System;
using System.Reflection;
using System.Collections.Generic;
using static System.Diagnostics.Debug;
using System.Linq.Expressions;

namespace DotNext.Reflection 
{
	internal static class Operator 
	{
		internal static ExpressionType ToExpressionType (this UnaryOperator @operator) => (ExpressionType) @operator;
		internal static ExpressionType ToExpressionType (this BinaryOperator @operator) => (ExpressionType) @operator;

		internal readonly struct Kind: IEquatable<Kind>
		{
			private sealed class EqualityComparer: IEqualityComparer<Kind>
			{
				public int GetHashCode(Kind key) => key.GetHashCode();
				public bool Equals(Kind first, Kind second) => first.Equals(second);
			}

			internal static IEqualityComparer<Kind> Comparer = new EqualityComparer();

			private readonly bool overloaded;
			private readonly ExpressionType operatorType;

			internal Kind(UnaryOperator operatorType, bool overloaded)
			{
				this.operatorType = operatorType.ToExpressionType();
				this.overloaded = overloaded;
			}

			public static implicit operator UnaryOperator(in Kind key) => (UnaryOperator)key.operatorType;
			public static implicit operator BinaryOperator(in Kind key) => (BinaryOperator)key.operatorType;

			public static implicit operator ExpressionType(in Kind key) => key.operatorType;

			internal Kind(BinaryOperator operatorType, bool overloaded)
			{
				this.operatorType = operatorType.ToExpressionType();
				this.overloaded = overloaded;
			}

			public bool Equals(Kind other) => operatorType == other.operatorType && overloaded == other.overloaded;
			public override bool Equals(object other)
				=> other is Kind key && Equals(key);
			public override int GetHashCode() => overloaded ? (int)operatorType + 100: (int)operatorType;

            private InvalidOperationException OperatorNotExists()
                => new InvalidOperationException(ExceptionMessages.MissingOperator(operatorType));

			internal UnaryExpression MakeUnary<R>(in Operand operand)
			{
				var result = Expression.MakeUnary(operatorType, operand.Argument, typeof(R));
				return result.Method is null ^ overloaded ? result : throw OperatorNotExists();
			}

			internal BinaryExpression MakeBinary(in Operand first, in Operand second)
			{
				var result = Expression.MakeBinary(operatorType, first.Argument, second.Argument);
				return result.Method is null ^ overloaded ? result : throw OperatorNotExists();
			}
		}

		internal readonly ref struct Operand 
		{
			internal readonly Expression Argument;
			internal readonly ParameterExpression Source;

			private Operand (ParameterExpression operand) => this.Argument = Source = operand;

			internal Operand (ParameterExpression supplier, Type expectedType) => Argument = Expression.Convert (this.Source = supplier, expectedType);

			public static implicit operator Operand (ParameterExpression operand) => new Operand (operand);
		}

		internal static bool Upcast (this ref Operand operand) 
		{
			var baseType = operand.Argument.Type.BaseType;
			//do not walk through inheritance hierarchy for value types
			if (baseType is null || operand.Argument.Type.IsValueType)
				return false;
			else {
				operand = new Operand (operand.Source, baseType);
				return true;
			}
		}

		internal static bool NormalizePrimitive(this ref Operand operand)
			{
				switch(operand.Argument.Type.GetTypeCode())
				{
					case TypeCode.Byte:
					case TypeCode.UInt16:
					case TypeCode.SByte:
					case TypeCode.Int16:
						operand = new Operand(operand.Source, typeof(int));
						return true;
					default:
						return false;
				}
			}
	}

	/// <summary>
	/// Abstract class for all reflected operators.
	/// </summary>
	/// <typeparam name="D">Type of delegate representing operator signature.</typeparam>
	public abstract class Operator<D> : IOperator<D>
		where D : Delegate 
	{
		private protected readonly D invoker;

		private protected Operator (D invoker, ExpressionType type, MethodInfo overloaded) {
			Type = type;
			this.invoker = invoker;
		}

		/// <summary>
		/// Gets the implementing method for the operator.
		/// </summary>
		public MethodInfo Method { get; }

		D IOperator<D>.Invoker => invoker;

		public static implicit operator D (Operator<D> @operator) => @operator?.invoker;

		/// <summary>
		/// Gets type of operator.
		/// </summary>
		public ExpressionType Type { get; }

		private static Expression<D> Convert (ParameterExpression parameter, Expression operand, Type conversionType, bool @checked) {
				try {
					return Expression.Lambda<D> (@checked ? Expression.ConvertChecked (operand, conversionType) : Expression.Convert (operand, conversionType), parameter);
				} catch (ArgumentException e) {
					WriteLine (e);
					return null;
				} catch (InvalidOperationException) {
					//do not walk through inheritance hierarchy for value types
					if (parameter.Type.IsValueType) return null;
					var lookup = operand.Type.BaseType;
					return lookup is null ? null : Convert (parameter, Expression.Convert (parameter, lookup), conversionType, @checked);
				}
		}

		private protected static Expression<D> MakeConvert<T> (ParameterExpression parameter, bool @checked) => Convert (parameter, parameter, typeof (T), @checked);
	}

	/// <summary>
	/// A delegate representing unary operator.
	/// </summary>
	/// <param name="operand">Operand.</param>
	/// <typeparam name="T">Type of operand.</typeparam>
	/// <typeparam name="R">Type of operator result.</typeparam>
	/// <returns>Result of unary operation.</returns>
	public delegate R Operator<T, R> ( in T operand);

	/// <summary>
	/// Represents binary operator.
	/// </summary>
	/// <param name="first">First operand.</param>
	/// <param name="second">Second operand.</param>
	/// <typeparam name="T1">Type of first operand.</typeparam>
	/// <typeparam name="T2">Type of second operand.</typeparam>
	/// <typeparam name="R">Type of operator result.</typeparam>
	/// <returns>Result of binary operator.</returns>
	public delegate R Operator<T1, T2, R> ( in T1 first, in T2 second);
}
﻿using System;

namespace DotNext.Reflection
{
	/// <summary>
	/// Indicates that requested event doesn't exist.
	/// </summary>
	public sealed class MissingEventException : ConstraintViolationException
	{
		private MissingEventException(Type declaringType, string eventName, Type handlerType)
			: base(declaringType, ExceptionMessages.MissingEvent(eventName, handlerType, declaringType))
		{
			HandlerType = handlerType;
			EventName = eventName;
		}

		internal static MissingEventException Create<T, E>(string eventName)
			where E: MulticastDelegate
			=> new MissingEventException(typeof(T), eventName, typeof(E));

		public Type HandlerType { get; }
		public string EventName { get; }
	}
}

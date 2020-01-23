using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNext.Reflection
{
    /// <summary>
    /// Represents reflected method.
    /// </summary>
    /// <typeparam name="D">Type of delegate describing signature of the reflected method.</typeparam>
    public sealed class Method<D> : MethodInfo, IMethod<D>, IEquatable<MethodInfo>
        where D : MulticastDelegate
    {
        private sealed class Cache : MemberCache<MethodInfo, Method<D>>
        {
            private protected override Method<D> Create(string methodName, bool nonPublic) => Reflect(methodName, nonPublic);
        }

        private sealed class Cache<T> : MemberCache<MethodInfo, Method<D>>
        {
            private protected override Method<D> Create(string methodName, bool nonPublic) => Reflect(typeof(T), methodName, nonPublic);
        }

        private static readonly UserDataSlot<Method<D>> CacheSlot = UserDataSlot<Method<D>>.Allocate();
        private const BindingFlags StaticPublicFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        private const BindingFlags StaticNonPublicFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags InstancePublicFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        private const BindingFlags InstanceNonPublicFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly MethodInfo method;
        internal readonly D Invoker;

        private Method(MethodInfo method, Expression<D> lambda)
        {
            this.method = method;
            Invoker = lambda.Compile();
        }

        internal Method(MethodInfo method, IEnumerable<Expression> args, IEnumerable<ParameterExpression> parameters)
            : this(method, Expression.Lambda<D>(Expression.Call(method, args), true, parameters))
        {
        }

        internal Method(MethodInfo method, ParameterExpression instance, IEnumerable<Expression> args, IEnumerable<ParameterExpression> parameters)
            : this(method, Expression.Lambda<D>(Expression.Call(instance, method, args), true, parameters.Prepend(instance)))
        {
        }

        private Method(MethodInfo method)
        {
            this.method = method;
            Invoker = method.CreateDelegate<D>();
        }

        /// <summary>
        /// Gets the attributes associated with this method.
        /// </summary>
        public override MethodAttributes Attributes => method.Attributes;

        /// <summary>
        /// Gets a value indicating the calling conventions for this constructor.
        /// </summary>
        public override CallingConventions CallingConvention => method.CallingConvention;

        /// <summary>
        /// Gets a value indicating whether the generic method contains unassigned generic type parameters.
        /// </summary>
        public override bool ContainsGenericParameters => method.ContainsGenericParameters;

        /// <summary>
        /// Creates a delegate of the specified type from this method.
        /// </summary>
        /// <param name="delegateType">The type of the delegate to create.</param>
        /// <returns>The delegate for this method.</returns>
        public override Delegate CreateDelegate(Type delegateType) => method.CreateDelegate(delegateType);

        /// <summary>
        /// Creates a delegate of the specified type from this method.
        /// </summary>
        /// <param name="delegateType">The type of the delegate to create.</param>
        /// <param name="target">The object targeted by the delegate.</param>
        /// <returns>The delegate for this method.</returns>
        public override Delegate CreateDelegate(Type delegateType, object target) => method.CreateDelegate(delegateType, target);

        /// <summary>
        /// Gets a collection that contains this member's custom attributes.
        /// </summary>
        public override IEnumerable<CustomAttributeData> CustomAttributes => method.CustomAttributes;

        /// <summary>
        /// Gets the class that declares this method.
        /// </summary>
        public override Type DeclaringType => method.DeclaringType;

        /// <summary>
        /// Returns the method on the direct or indirect base class in which the method represented by this instance was first declared.
        /// </summary>
        /// <returns>The first implementation of this method.</returns>
        public override MethodInfo GetBaseDefinition() => method.GetBaseDefinition();

        /// <summary>
        /// Returns an array of all custom attributes applied to this method.
        /// </summary>
        /// <param name="inherit"><see langword="true"/> to search this member's inheritance chain to find the attributes; otherwise, <see langword="false"/>.</param>
        /// <returns>An array that contains all the custom attributes applied to this method.</returns>
        public override object[] GetCustomAttributes(bool inherit) => method.GetCustomAttributes(inherit);

        /// <summary>
        /// Returns an array of all custom attributes applied to this method.
        /// </summary>
        /// <param name="attributeType">The type of attribute to search for. Only attributes that are assignable to this type are returned.</param>
        /// <param name="inherit"><see langword="true"/> to search this member's inheritance chain to find the attributes; otherwise, <see langword="false"/>.</param>
        /// <returns>An array that contains all the custom attributes applied to this method.</returns>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => method.GetCustomAttributes(attributeType, inherit);

        /// <summary>
        /// Returns a list of custom attributes that have been applied to the target method.
        /// </summary>
        /// <returns>The data about the attributes that have been applied to the target method.</returns>
        public override IList<CustomAttributeData> GetCustomAttributesData() => method.GetCustomAttributesData();

        /// <summary>
        /// Returns the type arguments of a generic method or the type parameters of a generic method definition.
        /// </summary>
        /// <returns>The list of generic arguments.</returns>
        public override Type[] GetGenericArguments() => method.GetGenericArguments();

        /// <summary>
        /// Returns generic method definition from which the current method can be constructed.
        /// </summary>
        /// <returns>Generic method definition from which the current method can be constructed.</returns>
        public override MethodInfo GetGenericMethodDefinition() => method.GetGenericMethodDefinition();

        /// <summary>
        /// Provides access to the MSIL stream, local variables, and exceptions for the current method.
        /// </summary>
        /// <returns>An object that provides access to the MSIL stream, local variables, and exceptions for the current method.</returns>
        public override MethodBody GetMethodBody() => method.GetMethodBody();

        /// <summary>
        /// Gets method implementation attributes.
        /// </summary>
        /// <returns>Implementation attributes.</returns>
        public override MethodImplAttributes GetMethodImplementationFlags() => method.GetMethodImplementationFlags();

        /// <summary>
        /// Gets method parameters.
        /// </summary>
        /// <returns>The array of method parameters.</returns>
        public override ParameterInfo[] GetParameters() => method.GetParameters();

        /// <summary>
        /// Invokes this method.
        /// </summary>
        /// <param name="obj">The object on which to invoke the method.</param>
        /// <param name="invokeAttr">Specifies the type of binding.</param>
        /// <param name="binder">Defines a set of properties and enables the binding, coercion of argument types, and invocation of members using reflection.</param>
        /// <param name="parameters">A list of method arguments.</param>
        /// <param name="culture">Used to govern the coercion of types.</param>
        /// <returns>The return value of the invoked method.</returns>
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
            => method.Invoke(obj, invokeAttr, binder, parameters, culture);

        /// <summary>
        /// Determines whether one or more attributes of the specified type or of its derived types is applied to this method.
        /// </summary>
        /// <param name="attributeType">The type of custom attribute to search for. The search includes derived types.</param>
        /// <param name="inherit"><see langword="true"/> to search this member's inheritance chain to find the attributes; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if one or more instances of <paramref name="attributeType"/> or any of its derived types is applied to this method; otherwise, <see langword="false"/>.</returns>
        public override bool IsDefined(Type attributeType, bool inherit)
            => method.IsDefined(attributeType, inherit);

        /// <summary>
        /// Gets a value indicating whether the method is generic.
        /// </summary>
        public override bool IsGenericMethod => method.IsGenericMethod;

        /// <summary>
        /// Gets a value indicating whether the method is a generic method definition.
        /// </summary>
        public override bool IsGenericMethodDefinition => method.IsGenericMethodDefinition;

        /// <summary>
        /// Gets a value that indicates whether the method is security-critical or security-safe-critical at the current trust level, 
        /// and therefore can perform critical operations.
        /// </summary>
        public override bool IsSecurityCritical => method.IsSecurityCritical;

        /// <summary>
        /// Gets a value that indicates whether the method is security-safe-critical at the current trust level; that is, 
        /// whether it can perform critical operations and can be accessed by transparent code.
        /// </summary>
        public override bool IsSecuritySafeCritical => method.IsSecuritySafeCritical;

        /// <summary>
        /// Gets a value that indicates whether the current method is transparent at the current trust level, 
        /// and therefore cannot perform critical operations.
        /// </summary>
        public override bool IsSecurityTransparent => method.IsSecurityTransparent;

        /// <summary>
        /// Substitutes the elements of an array of types for the type parameters of the current generic method definition, 
        /// and returns  the resulting constructed method.
        /// </summary>
        /// <param name="typeArguments">An array of types to be substituted for the type parameters of the current generic method definition.</param>
        /// <returns>The constructed method formed by substituting the elements of <paramref name="typeArguments"/> for the type parameters of the current generic method definition.</returns>
        public override MethodInfo MakeGenericMethod(params Type[] typeArguments) => method.MakeGenericMethod(typeArguments);

        /// <summary>
        /// Always returns <see cref="MemberTypes.Method"/>.
        /// </summary>
        public override MemberTypes MemberType => MemberTypes.Method;

        /// <summary>
        /// Gets a value that identifies a metadata element.
        /// </summary>
        public override int MetadataToken => method.MetadataToken;

        /// <summary>
        /// Gets a handle to the internal metadata representation of a method.
        /// </summary>
        public override RuntimeMethodHandle MethodHandle => method.MethodHandle;

        /// <summary>
        /// Gets method implementation attributes.
        /// </summary>
        public override MethodImplAttributes MethodImplementationFlags => method.MethodImplementationFlags;

        /// <summary>
        /// Gets the module in which the type that declares the method represented by the current instance is defined.
        /// </summary>
        public override Module Module => method.Module;

        /// <summary>
        /// Gets name of this method.
        /// </summary>
        public override string Name => method.Name;

        /// <summary>
        /// Gets the class object that was used to obtain this instance.
        /// </summary>
        public override Type ReflectedType => method.ReflectedType;

        /// <summary>
        /// Gets information about the return type of the method, such as whether the return type has custom modifiers.
        /// </summary>
        public override ParameterInfo ReturnParameter => method.ReturnParameter;

        /// <summary>
        /// Gets the return type of this method.
        /// </summary>
        public override Type ReturnType => method.ReturnType;

        /// <summary>
        /// Gets the custom attributes for the return type.
        /// </summary>
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => method.ReturnTypeCustomAttributes;

        /// <summary>
        /// Determines whether this method is equal to the given method.
        /// </summary>
        /// <param name="other">Other constructor to compare.</param>
        /// <returns><see langword="true"/> if this object reflects the same method as the specified object; otherwise, <see langword="false"/>.</returns>
        public bool Equals(MethodInfo other) => other is Method<D> method ? this.method == method.method : this.method == other;

        /// <summary>
        /// Determines whether this method is equal to the given method.
        /// </summary>
        /// <param name="other">Other constructor to compare.</param>
        /// <returns><see langword="true"/> if this object reflects the same method as the specified object; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object other)
        {
            switch (other)
            {
                case Method<D> method:
                    return this.method == method.method;
                case MethodInfo method:
                    return this.method == method;
                case D invoker:
                    return Equals(Invoker, invoker);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Computes hash code uniquely identifies the reflected method.
        /// </summary>
        /// <returns>The hash code of the method.</returns>
        public override int GetHashCode() => method.GetHashCode();

        /// <summary>
        /// Gets a delegate representing this method.
        /// </summary>
        /// <param name="method">The reflected method.</param>
        public static implicit operator D(Method<D> method) => method?.Invoker;

        MethodInfo IMember<MethodInfo>.RuntimeMember => method;
        D IMember<MethodInfo, D>.Invoker => Invoker;

        /// <summary>
        /// Returns textual representation of this method.
        /// </summary>
        /// <returns>The textual representation of this method.</returns>
        public override string ToString() => method.ToString();

        private static Method<D> ReflectStatic(Type declaringType, Type[] parameters, Type returnType,
            string methodName, bool nonPublic)
        {
            //lookup in declaring type
            var targetMethod = declaringType.GetMethod(methodName,
                nonPublic ? StaticNonPublicFlags : StaticPublicFlags,
                Type.DefaultBinder,
                parameters,
                Array.Empty<ParameterModifier>());
            //lookup in extension methods
            if (targetMethod is null || returnType != targetMethod.ReturnType)
                targetMethod = ExtensionRegistry.GetMethods(declaringType, MethodLookup.Static)
                    .FirstOrDefault(candidate =>
                        candidate.Name == methodName && candidate.SignatureEquals(parameters) &&
                        candidate.ReturnType == returnType);
            return targetMethod is null ? null : new Method<D>(targetMethod);
        }

        private static Method<D> ReflectStatic(Type declaringType, Type argumentsType, Type returnType,
            string methodName, bool nonPublic)
        {
            var (parameters, arglist, input) = Signature.Reflect(argumentsType);
            //lookup in declaring type
            var targetMethod = declaringType.GetMethod(methodName,
                nonPublic ? StaticNonPublicFlags : StaticPublicFlags,
                Type.DefaultBinder,
                parameters,
                Array.Empty<ParameterModifier>());
            //lookup in extension methods
            if (targetMethod is null || returnType != targetMethod.ReturnType)
                targetMethod = ExtensionRegistry.GetMethods(declaringType, MethodLookup.Static)
                    .FirstOrDefault(candidate =>
                        candidate.Name == methodName && candidate.SignatureEquals(parameters) &&
                        candidate.ReturnType == returnType);
            return targetMethod is null ? null : new Method<D>(targetMethod, arglist, new[] { input });
        }

        private static Type NonRefType(Type type) => type.IsByRef ? type.GetElementType() : type;

        private static Method<D> ReflectInstance(Type thisParam, Type[] parameters, Type returnType, string methodName, bool nonPublic)
        {
            //lookup in declaring type
            var targetMethod = NonRefType(thisParam).GetMethod(methodName,
                nonPublic ? InstanceNonPublicFlags : InstancePublicFlags,
                Type.DefaultBinder,
                parameters,
                Array.Empty<ParameterModifier>());
            //lookup in extension methods
            if (targetMethod is null || returnType != targetMethod.ReturnType)
                targetMethod = ExtensionRegistry.GetMethods(thisParam, MethodLookup.Instance).FirstOrDefault(candidate => candidate.Name == methodName && Enumerable.SequenceEqual(candidate.GetParameterTypes().RemoveFirst(1), parameters) && candidate.ReturnType == returnType);
            //this parameter can be passed as REF so handle this situation
            //first parameter should be passed by REF for structure types
            if (targetMethod is null)
                return null;
            else if (thisParam.IsByRef ^ NonRefType(thisParam).IsValueType)
            {
                ParameterExpression[] parametersDeclaration;
                if (targetMethod.IsStatic)
                {
                    parametersDeclaration = Array.ConvertAll(targetMethod.GetParameterTypes(), Expression.Parameter);
                    return new Method<D>(targetMethod, parametersDeclaration, parametersDeclaration);
                }
                else
                {
                    var thisParamDeclaration = Expression.Parameter(thisParam);
                    parametersDeclaration = Array.ConvertAll(parameters, Expression.Parameter);
                    return new Method<D>(targetMethod, thisParamDeclaration, parametersDeclaration, parametersDeclaration);
                }
            }
            else
                return new Method<D>(targetMethod);
        }

        private static Method<D> ReflectInstance(Type thisParam, Type argumentsType, Type returnType, string methodName, bool nonPublic)
        {
            var (parameters, arglist, input) = Signature.Reflect(argumentsType);
            var thisParamDeclaration = Expression.Parameter(thisParam.MakeByRefType());
            //lookup in declaring type
            var targetMethod = thisParam.GetMethod(methodName,
                nonPublic ? InstanceNonPublicFlags : InstancePublicFlags,
                Type.DefaultBinder,
                parameters,
                Array.Empty<ParameterModifier>());
            //lookup in extension methods
            if (targetMethod is null || returnType != targetMethod.ReturnType)
            {
                targetMethod = null;
                foreach (var candidate in ExtensionRegistry.GetMethods(thisParam, MethodLookup.Instance))
                    if (candidate.Name == methodName && Enumerable.SequenceEqual(candidate.GetParameterTypes().RemoveFirst(1), parameters) && candidate.ReturnType == returnType)
                    {
                        targetMethod = candidate;
                        break;
                    }
            }
            return targetMethod is null ? null : new Method<D>(targetMethod, thisParamDeclaration, arglist, new[] { input });
        }

        /// <summary>
        /// Reflects instance method.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="nonPublic"></param>
        /// <returns></returns>
        private static Method<D> Reflect(string methodName, bool nonPublic)
        {
            var delegateType = typeof(D);
            if (delegateType.IsAbstract)
                throw new AbstractDelegateException<D>();
            else if (delegateType.IsGenericInstanceOf(typeof(Function<,,>)) && delegateType.GetGenericArguments().Take(out var thisParam, out var argumentsType, out var returnType) == 3L)
                return ReflectInstance(thisParam, argumentsType, returnType, methodName, nonPublic);
            else if (delegateType.IsGenericInstanceOf(typeof(Procedure<,>)) && delegateType.GetGenericArguments().Take(out thisParam, out argumentsType) == 2L)
                return ReflectInstance(thisParam, argumentsType, typeof(void), methodName, nonPublic);
            else
            {
                DelegateType.GetInvokeMethod<D>().Decompose(MethodExtensions.GetParameterTypes, method => method.ReturnType, out var parameters, out returnType);
                thisParam = parameters.FirstOrDefault() ?? throw new ArgumentException(ExceptionMessages.ThisParamExpected);
                return ReflectInstance(thisParam, parameters.RemoveFirst(1), returnType, methodName, nonPublic);
            }
        }

        /// <summary>
        /// Reflects static method.
        /// </summary>
        /// <param name="declaringType">Declaring type.</param>
        /// <param name="methodName">Name of method.</param>
        /// <param name="nonPublic">True to reflect non-public static method.</param>
        /// <returns>Reflected static method.</returns>
        private static Method<D> Reflect(Type declaringType, string methodName, bool nonPublic)
        {
            var delegateType = typeof(D);
            if (delegateType.IsAbstract)
                throw new AbstractDelegateException<D>();
            else if (delegateType.IsGenericInstanceOf(typeof(Function<,>)) && delegateType.GetGenericArguments().Take(out var argumentsType, out var returnType) == 2L)
                return ReflectStatic(declaringType, argumentsType, returnType, methodName, nonPublic);
            else if (delegateType.IsGenericInstanceOf(typeof(Procedure<>)))
                return ReflectStatic(declaringType, delegateType.GetGenericArguments()[0], typeof(void), methodName, nonPublic);
            else
            {
                DelegateType.GetInvokeMethod<D>().Decompose(MethodExtensions.GetParameterTypes, method => method.ReturnType, out var parameters, out returnType);
                return ReflectStatic(declaringType, parameters, returnType, methodName, nonPublic);
            }
        }

        private static Method<D> Unreflect(MethodInfo method, ParameterExpression thisParam, Type argumentsType, Type returnType)
        {
            var (_, arglist, input) = Signature.Reflect(argumentsType);
            var prologue = new LinkedList<Expression>();
            var epilogue = new LinkedList<Expression>();
            var locals = new LinkedList<ParameterExpression>();
            //adjust THIS
            Expression thisArg;
            if (thisParam is null || method.DeclaringType is null)
                thisArg = null;
            else if (method.DeclaringType.IsAssignableFromWithoutBoxing(thisParam.Type))
                thisArg = thisParam;
            else if (thisParam.Type == typeof(object))
                thisArg = Expression.Convert(thisParam, method.DeclaringType);
            else
                return null;
            //adjust arguments
            if (!Signature.NormalizeArguments(method.GetParameterTypes(), arglist, locals, prologue, epilogue))
                return null;
            Expression body;
            //adjust return type
            if (returnType == typeof(void) || returnType.IsAssignableFromWithoutBoxing(method.ReturnType))
                body = Expression.Call(thisArg, method, arglist);
            else if (returnType == typeof(object))
                body = Expression.Convert(Expression.Call(thisArg, method, arglist), returnType);
            else
                return null;
            if (epilogue.Count == 0)
                epilogue.AddFirst(body);
            else if (method.ReturnType != typeof(void))
            {
                var returnArg = Expression.Parameter(returnType);
                locals.AddFirst(returnArg);
                body = Expression.Assign(returnArg, body);
                epilogue.AddFirst(body);
                epilogue.AddLast(returnArg);
            }
            body = prologue.Count == 0 && epilogue.Count == 1 ? epilogue.First.Value : Expression.Block(locals, prologue.Concat(epilogue));
            return new Method<D>(method, thisParam is null ? Expression.Lambda<D>(body, input) : Expression.Lambda<D>(body, thisParam, input));
        }

        private static Method<D> UnreflectStatic(MethodInfo method)
        {
            var delegateType = typeof(D);
            if (delegateType.IsGenericInstanceOf(typeof(Function<,>)) && delegateType.GetGenericArguments().Take(out var argumentsType, out var returnType) == 2L)
                return Unreflect(method, null, argumentsType, returnType);
            else if (delegateType.IsGenericInstanceOf(typeof(Procedure<>)))
                return Unreflect(method, null, delegateType.GetGenericArguments()[0], typeof(void));
            else if (DelegateType.GetInvokeMethod<D>().SignatureEquals(method))
                return new Method<D>(method);
            else
                return null;
        }

        private static Method<D> UnreflectInstance(MethodInfo method)
        {
            var delegateType = typeof(D);
            if (delegateType.IsGenericInstanceOf(typeof(Function<,,>)) && delegateType.GetGenericArguments().Take(out var thisParam, out var argumentsType, out var returnType) == 3L)
                return Unreflect(method, Expression.Parameter(thisParam.MakeByRefType()), argumentsType, returnType);
            else if (delegateType.IsGenericInstanceOf(typeof(Procedure<,>)) && delegateType.GetGenericArguments().Take(out thisParam, out argumentsType) == 2L)
                return Unreflect(method, Expression.Parameter(thisParam.MakeByRefType()), argumentsType, typeof(void));
            else
            {
                DelegateType.GetInvokeMethod<D>().Decompose(MethodExtensions.GetParameterTypes, m => m.ReturnType, out var parameters, out returnType);
                thisParam = parameters.FirstOrDefault() ?? throw new ArgumentException(ExceptionMessages.ThisParamExpected);
                parameters = parameters.RemoveFirst(1);
                if (method.SignatureEquals(parameters) && method.ReturnType == returnType)
                    if (thisParam.IsByRef ^ method.DeclaringType?.IsValueType ?? false)
                    {
                        var arguments = Array.ConvertAll(parameters, Expression.Parameter);
                        return new Method<D>(method, Expression.Parameter(thisParam), arguments, arguments);
                    }
                    else
                        return new Method<D>(method);
                return null;
            }
        }

        private static Method<D> Unreflect(MethodInfo method)
        {
            var delegateType = typeof(D);
            if (delegateType.IsAbstract)
                throw new AbstractDelegateException<D>();
            else if (method is Method<D> existing)
                return existing;
            else if (method.IsGenericMethodDefinition || method.IsAbstract || method.IsConstructor)
                return null;
            else if (method.IsStatic)
                return UnreflectStatic(method);
            else
                return UnreflectInstance(method);
        }

        internal static Method<D> GetOrCreate(MethodInfo method)
            => method.GetUserData().GetOrSet(CacheSlot, method, new ValueFunc<MethodInfo, Method<D>>(Unreflect));

        internal static Method<D> GetOrCreate<T>(string methodName, bool nonPublic, MethodLookup lookup)
        {
            MemberCache<MethodInfo, Method<D>> cache;
            switch (lookup)
            {
                case MethodLookup.Instance:
                    cache = Cache.Of<Cache>(typeof(T));
                    break;
                case MethodLookup.Static:
                    cache = Cache.Of<Cache<T>>(typeof(T));
                    break;
                default:
                    cache = null;
                    break;
            }
            return cache?.GetOrCreate(methodName, nonPublic);
        }
    }
}
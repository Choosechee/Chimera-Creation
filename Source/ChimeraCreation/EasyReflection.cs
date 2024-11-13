using HarmonyLib;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AnomalyAllies
{
    internal static class EasyReflection
    {
        internal const BindingFlags allInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        internal const BindingFlags allStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private class EquatableArray<T> : IEquatable<EquatableArray<T>>
        {
            public readonly T[] values;

            public T this[int index]
            {
                get => values[index];
                set => values[index] = value;
            }

            public EquatableArray(int length) => values = new T[length];

            public EquatableArray(T[] array) : this(array.Length)
            {
                Array.Copy(array, values, array.Length);
            }
            
            public bool Equals(EquatableArray<T> other)
            {
                return Enumerable.SequenceEqual(values, other.values);
            }

            public override bool Equals(object obj)
            {
                return obj is EquatableArray<T> eqArray && Equals(eqArray);
            }

            public override int GetHashCode()
            {
                if (this is null || values.Length == 0) return 0;
                
                int hash = values[0].GetHashCode();
                for (int i = 1; i < values.Length; i++)
                    hash ^= values[i].GetHashCode();

                return hash;
            }

            public static bool operator ==(EquatableArray<T> first, EquatableArray<T> second) => first.Equals(second);
            public static bool operator !=(EquatableArray<T> first, EquatableArray<T> second) => !first.Equals(second);
        }

        private record MemberCacheKey(Type ParentType, string Name, MemberTypes MemberType, bool Static)
        {
            public EquatableArray<Type> ParameterTypes { get; init; }
            
            public MemberCacheKey(Type ParentType, string Name, MemberTypes MemberType, bool Static, Type[] ParameterTypes) : this(ParentType, Name, MemberType, Static)
            {
                this.ParameterTypes = new EquatableArray<Type>(ParameterTypes);
            }
        }

        private static readonly Dictionary<MemberCacheKey, MemberInfo> memberCache = new();
        private static readonly Dictionary<Pair<Type, string>, bool> ambiguousMethodCache = new();

        public static T ForceGetField<T>(this object obj, string fieldName)
        {
            Type objType = obj.GetType();
            MemberCacheKey cacheKey = new MemberCacheKey(objType, fieldName, MemberTypes.Field, false);
            FieldInfo field;
            if (memberCache.TryGetValue(cacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Field cache hit!");
                field = member as FieldInfo;
            }
            else
            {
                field = objType.GetField(fieldName, allInstance);
                if (field is null)
                    field = objType.Field(fieldName);
                if (field is /*still*/ null || field.IsStatic)
                    throw new ArgumentException($"Could not find field {fieldName} in type {objType.Name}", "fieldName");

                memberCache[cacheKey] = field;
            }
            
            object fieldValue = field.GetValue(obj);
            return (T)fieldValue;
        }

        public static T ForceGetStaticField<T>(this Type type, string fieldName)
        {
            MemberCacheKey cacheKey = new MemberCacheKey(type, fieldName, MemberTypes.Field, true);
            FieldInfo field;
            if (memberCache.TryGetValue(cacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Field cache hit!");
                field = member as FieldInfo;
            }
            else
            {
                field = type.GetField(fieldName, allStatic);
                if (field is null)
                    field = type.Field(fieldName);
                if (field is /*still*/ null || !field.IsStatic)
                    throw new ArgumentException($"Could not find field {fieldName} in type {type.Name}", "fieldName");

                memberCache[cacheKey] = field;
            }

            object fieldValue = field.GetValue(null);
            return (T)fieldValue;
        }

        public static T ForceGetProperty<T>(this object obj, string propertyName)
        {
            Type objType = obj.GetType();
            MemberCacheKey cacheKey = new MemberCacheKey(objType, propertyName, MemberTypes.Property, false);
            PropertyInfo property;
            if (memberCache.TryGetValue(cacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Property cache hit!");
                property = member as PropertyInfo;
            }
            else
            {
                property = objType.GetProperty(propertyName, allInstance);
                if (property is null)
                    property = objType.Property(propertyName);
                if (property is /*still*/ null || property.GetMethod.IsStatic)
                    throw new ArgumentException($"Could not find property {propertyName} in type {objType.Name}", "propertyName");

                memberCache[cacheKey] = property;
            }

            object propertyValue = property.GetValue(obj);
            return (T)propertyValue;
        }

        public static T ForceGetStaticProperty<T>(this Type type, string propertyName)
        {
            MemberCacheKey cacheKey = new MemberCacheKey(type, propertyName, MemberTypes.Property, true);
            PropertyInfo property;
            if (memberCache.TryGetValue(cacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Property cache hit!");
                property = member as PropertyInfo;
            }
            else
            {
                property = type.GetProperty(propertyName, allStatic);
                if (property is null)
                    property = type.Property(propertyName);
                if (property is /*still*/ null || !property.GetMethod.IsStatic)
                    throw new ArgumentException($"Could not find property {propertyName} in type {type.Name}", "propertyName");

                memberCache[cacheKey] = property;
            }

            object propertyValue = property.GetValue(null);
            return (T)propertyValue;
        }

        internal static Type[] ObjectArrayToTypeArray(object[] objects)
        {
            Type[] types = new Type[objects.Length];
            for (int i = 0; i < objects.Length; i++)
                types[i] = objects[i].GetType();

            return types;
        }

        public static void ForceInvokeMethod(this object obj, string methodName, params object[] args) => ForceInvokeMethod<object>(obj, methodName, args);

        public static T ForceInvokeMethod<T>(this object obj, string methodName, params object[] args)
        {
            Type objType = obj.GetType();
            Pair<Type, string> ambiguousMethodCacheKey = new Pair<Type, string>(objType, methodName);
            MemberCacheKey memberCacheKey;
            if (ambiguousMethodCache.TryGetValue(ambiguousMethodCacheKey, out bool ambiguous) && ambiguous)
            {
                Type[] parameterTypes = ObjectArrayToTypeArray(args);
                memberCacheKey = new MemberCacheKey(objType, methodName, MemberTypes.Method, false, parameterTypes);
            }
            else
                memberCacheKey = new MemberCacheKey(objType, methodName, MemberTypes.Method, false);

            MethodInfo method;
            if (memberCache.TryGetValue(memberCacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Method cache hit!");
                method = member as MethodInfo;
            }
            else
            {
                try
                {
                    method = objType.GetMethod(methodName, allInstance);
                    if (method is null)
                        method = objType.Method(methodName);
                    if (method is /*still*/ null || method.IsStatic)
                        throw new ArgumentException($"Could not find method {methodName} in type {objType.Name}", "methodName");
                }
                catch (AmbiguousMatchException)
                {
                    Type[] parameterTypes = ObjectArrayToTypeArray(args);
                    method = objType.GetMethod(methodName, allInstance, null, parameterTypes, null);
                    if (method is null)
                        method = objType.Method(methodName, parameterTypes);
                    if (method is /*still*/ null || method.IsStatic)
                        throw new ArgumentException($"Could not find method {methodName} with parameters {string.Join<Type>(", ", parameterTypes)} in type {objType.Name}", "methodName, args");

                    memberCacheKey = memberCacheKey with { ParameterTypes = new EquatableArray<Type>(parameterTypes) };
                }

                memberCache[memberCacheKey] = method;
            }

            return (T)method.Invoke(obj, args);
        }

        public static void ForceInvokeStaticMethod(this Type type, string methodName, params object[] args) => ForceInvokeStaticMethod<object>(type, methodName, args);

        public static T ForceInvokeStaticMethod<T>(this Type type, string methodName, params object[] args)
        {
            Pair<Type, string> ambiguousMethodCacheKey = new Pair<Type, string>(type, methodName);
            MemberCacheKey memberCacheKey;
            if (ambiguousMethodCache.TryGetValue(ambiguousMethodCacheKey, out bool ambiguous) && ambiguous)
            {
                Type[] parameterTypes = ObjectArrayToTypeArray(args);
                memberCacheKey = new MemberCacheKey(type, methodName, MemberTypes.Method, true, parameterTypes);
            }
            else
                memberCacheKey = new MemberCacheKey(type, methodName, MemberTypes.Method, true);

            MethodInfo method;
            if (memberCache.TryGetValue(memberCacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Method cache hit!");
                method = member as MethodInfo;
            }
            else
            {
                try
                {
                    method = type.GetMethod(methodName, allStatic);
                    if (method is null)
                        method = type.Method(methodName);
                    if (method is /*still*/ null || !method.IsStatic)
                        throw new ArgumentException($"Could not find method {methodName} in type {type.Name}", "methodName");
                }
                catch (AmbiguousMatchException)
                {
                    Type[] parameterTypes = ObjectArrayToTypeArray(args);
                    method = type.GetMethod(methodName, allStatic, null, parameterTypes, null);
                    if (method is null)
                        method = type.Method(methodName, parameterTypes);
                    if (method is /*still*/ null || !method.IsStatic)
                        throw new ArgumentException($"Could not find method {methodName} with parameters {string.Join<Type>(", ", parameterTypes)} in type {type.Name}", "methodName, args");

                    memberCacheKey = memberCacheKey with { ParameterTypes = new EquatableArray<Type>(parameterTypes) };
                }

                memberCache[memberCacheKey] = method;
            }

            return (T)method.Invoke(null, args);
        }
    }
}

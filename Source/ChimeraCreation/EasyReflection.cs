using HarmonyLib;
using System;
using System.Collections.Generic;
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

        private static Dictionary<Pair<Type, string>, MemberInfo> cachedMembers = new();

        //private static Dictionary<Pair<Type, string>, MethodInfo> cachedMethodInfos = new();
        //private static Dictionary<Pair<MethodInfo, object>, Delegate> cachedInstanceDelegates = new();

        public static T ForceGetField<T>(this object obj, string fieldName)
        {
            Type objType = obj.GetType();
            Pair<Type, string> cacheKey = new Pair<Type, string>(objType, fieldName);
            FieldInfo field;
            if (cachedMembers.TryGetValue(cacheKey, out MemberInfo member))
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

                cachedMembers[cacheKey] = field;
            }
            
            object fieldValue = field.GetValue(obj);
            return (T)fieldValue;
        }

        public static T ForceGetStaticField<T>(this Type type, string fieldName)
        {
            Pair<Type, string> cacheKey = new Pair<Type, string>(type, fieldName);
            FieldInfo field;
            if (cachedMembers.TryGetValue(cacheKey, out MemberInfo member))
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

                cachedMembers[cacheKey] = field;
            }

            object fieldValue = field.GetValue(null);
            return (T)fieldValue;
        }

        public static T ForceGetProperty<T>(this object obj, string propertyName)
        {
            Type objType = obj.GetType();
            Pair<Type, string> cacheKey = new Pair<Type, string>(objType, propertyName);
            PropertyInfo property;
            if (cachedMembers.TryGetValue(cacheKey, out MemberInfo member))
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

                cachedMembers[cacheKey] = property;
            }

            object propertyValue = property.GetValue(obj);
            return (T)propertyValue;
        }

        public static T ForceGetStaticProperty<T>(this Type type, string propertyName)
        {
            Pair<Type, string> cacheKey = new Pair<Type, string>(type, propertyName);
            PropertyInfo property;
            if (cachedMembers.TryGetValue(cacheKey, out MemberInfo member))
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

                cachedMembers[cacheKey] = property;
            }

            object propertyValue = property.GetValue(null);
            return (T)propertyValue;
        }

        public static void ForceInvokeMethod(this object obj, string methodName, params object[] args) => ForceInvokeMethod<object>(obj, methodName, args);

        public static T ForceInvokeMethod<T>(this object obj, string methodName, params object[] args)
        {
            Type objType = obj.GetType();
            Pair<Type, string> cacheKey = new Pair<Type, string>(objType, methodName);
            MethodInfo method;
            if (cachedMembers.TryGetValue(cacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Method cache hit!");
                method = member as MethodInfo;
            }
            else
            {
                method = objType.GetMethod(methodName, allInstance);
                if (method is null)
                    method = objType.Method(methodName);
                if (method is /*still*/ null || method.IsStatic)
                    throw new ArgumentException($"Could not find method {methodName} in type {objType.Name}", "methodName");

                cachedMembers[cacheKey] = method;
            }

            return (T)method.Invoke(obj, args);
        }

        public static void ForceInvokeStaticMethod(this Type type, string methodName, params object[] args) => ForceInvokeStaticMethod<object>(type, methodName, args);

        public static T ForceInvokeStaticMethod<T>(this Type type, string methodName, params object[] args)
        {
            Pair<Type, string> cacheKey = new Pair<Type, string>(type, methodName);
            MethodInfo method;
            if (cachedMembers.TryGetValue(cacheKey, out MemberInfo member))
            {
                //AnomalyAlliesMod.Logger.Message("Method cache hit!");
                method = member as MethodInfo;
            }
            else
            {
                method = type.GetMethod(methodName, allStatic);
                if (method is null)
                    method = type.Method(methodName);
                if (method is /*still*/ null || !method.IsStatic)
                    throw new ArgumentException($"Could not find method {methodName} in type {type.Name}", "methodName");

                cachedMembers[cacheKey] = method;
            }

            return (T)method.Invoke(null, args);
        }
    }
}

using HarmonyLib;
using System;
using System.Reflection;

namespace AnomalyAllies
{
    internal static class EasyReflection
    {
        internal const BindingFlags allInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        internal const BindingFlags allStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static T ForceGetField<T>(this object obj, string fieldName)
        {
            object fieldValue = obj.GetType().GetField(fieldName, allInstance).GetValue(obj);
            return (T)fieldValue;
        }

        public static T ForceGetStaticField<T>(this Type type, string fieldName)
        {
            object fieldValue = type.GetField(fieldName, allStatic).GetValue(null);
            return (T)fieldValue;
        }

        public static T ForceGetProperty<T>(this object obj, string propertyName)
        {
            object propertyValue = obj.GetType().GetProperty(propertyName, allInstance).GetValue(obj);
            return (T)propertyValue;
        }

        public static T ForceGetStaticProperty<T>(this Type type, string propertyName)
        {
            object propertyValue = type.GetProperty(propertyName, allStatic).GetValue(null);
            return (T)propertyValue;
        }

        public static void ForceInvokeMethod(this object obj, string methodName, params object[] args)
        {
            MethodInfo method = obj.GetType().GetMethod(methodName, allInstance);
            if (method is null)
                method = obj.GetType().Method(methodName);

            method.Invoke(obj, args);
        }

        public static T ForceInvokeMethod<T>(this object obj, string methodName, params object[] args)
        {
            MethodInfo method = obj.GetType().GetMethod(methodName, allInstance);
            if (method is null)
                method = obj.GetType().Method(methodName);

            object methodReturn = method.Invoke(obj, args);
            return (T)methodReturn;
        }

        public static void ForceInvokeStaticMethod(this Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, allStatic);
            method.Invoke(null, args);
        }

        public static T ForceInvokeStaticMethod<T>(this Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, allStatic);
            object methodReturn = method.Invoke(null, args);
            return (T)methodReturn;
        }
    }
}

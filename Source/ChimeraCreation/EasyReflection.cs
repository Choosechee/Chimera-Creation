using System;
using System.Reflection;

namespace AnomalyAllies
{
    internal static class EasyReflection
    {
        static BindingFlags allInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static BindingFlags allStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static T ForceGetField<T>(this object obj, string fieldName)
        {
            object fieldValue = obj.GetType().GetField(fieldName, allInstance).GetValue(obj);
            if (fieldValue is T)
                return (T)fieldValue;
            else
                throw new InvalidCastException($"{fieldName} is not of the specified type.");
        }

        public static T ForceGetStaticField<T>(this Type type, string fieldName)
        {
            object fieldValue = type.GetField(fieldName, allStatic).GetValue(null);
            if (fieldValue is T)
                return (T)fieldValue;
            else
                throw new InvalidCastException($"{fieldName} is not of the specified type.");
        }

        public static T ForceGetProperty<T>(this object obj, string propertyName)
        {
            object propertyValue = obj.GetType().GetProperty(propertyName, allInstance).GetValue(obj);
            if (propertyValue is T)
                return (T)propertyValue;
            else
                throw new InvalidCastException($"{propertyName} is not of the specified type.");
        }

        public static T ForceGetStaticProperty<T>(this Type type, string propertyName)
        {
            object propertyValue = type.GetProperty(propertyName, allStatic).GetValue(null);
            if (propertyValue is T)
                return (T)propertyValue;
            else
                throw new InvalidCastException($"{propertyName} is not of the specified type.");
        }

        public static void ForceInvokeMethod(this object obj, string methodName, params object[] args)
        {
            MethodInfo method = obj.GetType().GetMethod(methodName, allInstance);
            method.Invoke(obj, args);
        }

        public static T ForceInvokeMethod<T>(this object obj, string methodName, params object[] args)
        {
            MethodInfo method = obj.GetType().GetMethod(methodName, allInstance);
            object methodReturn = method.Invoke(obj, args);
            if (methodReturn is T)
                return (T)methodReturn;
            else
                throw new InvalidCastException($"{methodReturn} is not of the specified type");
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
            if (methodReturn is T)
                return (T)methodReturn;
            else
                throw new InvalidCastException($"{methodReturn} is not of the specified type");
        }
    }
}

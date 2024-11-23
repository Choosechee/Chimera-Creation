using System;
using System.Reflection;
using Verse;

namespace AnomalyAllies.Misc
{
    internal static class NameExtensions
    {
        public static Name GetCopy(this Name name)
        {
            if (name is null) return null;

            if (name is NameSingle nameSingle)
                return new NameSingle(nameSingle.Name, nameSingle.Numerical);
            else if (name is NameTriple nameTriple)
                return new NameTriple(nameTriple.First, nameTriple.Nick, nameTriple.Last);
            else
            {
                Type nameType = name.GetType();
                Name nameCopy = (Name)Activator.CreateInstance(nameType);

                foreach (FieldInfo field in nameType.GetFields(EasyReflection.allInstance))
                {
                    object value = field.GetValue(name);
                    field.SetValue(nameCopy, value);
                }

                return nameCopy;
            }
        }
    }
}

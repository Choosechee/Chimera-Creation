using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace AnomalyAllies.Misc
{
    public class GameComponent_DefChanges : GameComponent, IDisposable
    {
        protected interface IDefFieldChange : IExposable
        {
            Def Def { get; }
            List<string> FieldPath { get; }

            object OldValue { get; }
            object NewValue { get; }

            void InitializeOldValueAndType();
        }

        protected record DefFieldChange<DefT> : IDefFieldChange where DefT: Def, new()
        {
            private DefT def;
            public Def Def => def;

            private List<string> fieldPath;
            public List<string> FieldPath => fieldPath;

            private object oldValue;
            public object OldValue => oldValue;

            private object newValue;
            public object NewValue => newValue;

            private Type valueType;
            private LookMode valueLookMode;

            public DefFieldChange(DefT def, IEnumerable<string> fieldPath, object newValue)
            {
                this.def = def;

                if (fieldPath is not null)
                    this.fieldPath = fieldPath.ToList();

                this.newValue = newValue;

                InitializeOldValueAndType();
            }

            public DefFieldChange(DefT def, IEnumerable<string> fieldPath) : this(def, fieldPath, null)
            {
            }

            public DefFieldChange() : this(null, null)
            {
            }

            public void InitializeOldValueAndType()
            {
                if (def is null || fieldPath is null)
                    return;
                
                FieldInfo currentField = null;
                object currentObject = def;
                Type currentType = currentObject.GetType();

                for (int i = 0; i < fieldPath.Count - 1; i++)
                {
                    string fieldName = fieldPath[i];

                    currentField = currentType.GetField(fieldName, EasyReflection.allInstance);
                    currentObject = currentField.GetValue(currentObject);
                    currentType = currentObject.GetType();
                }
                currentField = currentType.GetField(fieldPath[fieldPath.Count - 1], EasyReflection.allInstance);

                valueType = currentField.FieldType;
                if (newValue is not null && !valueType.IsAssignableFrom(newValue.GetType()))
                    throw new InvalidOperationException($"Field {currentField.Name} cannot hold {newValue}");

                oldValue = currentField.GetValue(currentObject);
                Scribe_Universal.TryResolveLookMode(valueType, out valueLookMode);
            }

            public virtual void ExposeData()
            {
                Scribe_Defs.Look(ref def, "def");
                Scribe_Collections.Look(ref fieldPath, "fieldPath", LookMode.Value);

                if (Scribe.mode == LoadSaveMode.LoadingVars)
                    InitializeOldValueAndType();

                Scribe_Universal.Look(ref newValue, "newValue", valueLookMode, ref valueType);
            }

            public virtual bool Equals(DefFieldChange<DefT> other)
            {
                if (other is null) return false;
                
                return def == other.def && Enumerable.SequenceEqual(fieldPath, other.fieldPath);
            }

            public override int GetHashCode()
            {
                int hashCode = def.GetHashCode();

                foreach (string fieldName in fieldPath)
                    hashCode ^= fieldName.GetHashCode();

                return hashCode;
            }
        }
        
        protected List<IDefFieldChange> defFieldChanges = new List<IDefFieldChange>();

        public static GameComponent_DefChanges CurrentInstance { get; private set; }

        public GameComponent_DefChanges()
        {
            CurrentInstance = this;
        }

        public GameComponent_DefChanges(Game game) : this()
        {
        }

        public virtual void AddNewDefChange<DefT>(DefT def, IEnumerable<string> fieldPath, object newValue)
        where DefT : Def, new()
        {
            IDefFieldChange defFieldChange = new DefFieldChange<DefT>(def, fieldPath, newValue);
            if (defFieldChanges.Contains(defFieldChange))
                defFieldChanges.Remove(defFieldChange);

            SetNewDefValue(defFieldChange);
            defFieldChanges.Add(defFieldChange);
        }

        public virtual bool RemoveDefChange(Def def, IEnumerable<string> fieldPath)
        {
            IDefFieldChange defFieldChange = defFieldChanges.Find((dfc) => dfc.Def == def && Enumerable.SequenceEqual(dfc.FieldPath, fieldPath));
            if (defFieldChange is null)
                return false;

            ResetToOldDefValue(defFieldChange);
            defFieldChanges.Remove(defFieldChange);
            return true;
        }

        protected virtual void SetNewDefValue(IDefFieldChange change) => ChangeDefValue(change, change.NewValue);
        protected virtual void ResetToOldDefValue(IDefFieldChange change) => ChangeDefValue(change, change.OldValue);

        private void ChangeDefValue(IDefFieldChange change, object value)
        {
            FieldInfo currentField = null;
            object currentObject = change.Def;
            Type currentType = currentObject.GetType();

            for (int i = 0; i < change.FieldPath.Count - 1; i++)
            {
                string fieldName = change.FieldPath[i];

                currentField = currentType.GetField(fieldName, EasyReflection.allInstance);
                currentObject = currentField.GetValue(currentObject);
                currentType = currentObject.GetType();
            }
            currentField = currentType.GetField(change.FieldPath[change.FieldPath.Count - 1], EasyReflection.allInstance);

            currentField.SetValue(currentObject, value);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            foreach (IDefFieldChange change in defFieldChanges)
                SetNewDefValue(change);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref defFieldChanges, "defFieldChanges", LookMode.Deep);
        }

        protected bool disposed;
        public void Dispose()
        {
            if (disposed) return;

            foreach (IDefFieldChange change in defFieldChanges)
                ResetToOldDefValue(change);

            GC.SuppressFinalize(this);
            disposed = true;
        }
    }
}

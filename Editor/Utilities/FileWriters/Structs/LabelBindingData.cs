using System;

namespace UIToolkit.Editor.Utilities.FileWriters
{
    public struct LabelBindingData
    {
        public Type ParentType;
        public string ParentName;
        public string BindingPath;
        public bool IsField;

        public override string ToString()
        {
            return $"<{ParentType.Name}>[{ParentName}] BIND TO -> {BindingPath}";
        }
    }
}
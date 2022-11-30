using System;
using UIToolkit.Attributes;

namespace UIToolkit.Editor.Utilities.FileWriters
{
    public struct ConditionalData
    {
        public Type ParentType;
        public string ParentName;

        public ConditionalBaseAttribute conditionalAttribute;
    }
}
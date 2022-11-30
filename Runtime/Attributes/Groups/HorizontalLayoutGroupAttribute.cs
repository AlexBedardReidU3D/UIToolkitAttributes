using System;
using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class HorizontalLayoutGroupAttribute : GroupBaseAttribute
    {
        public HorizontalLayoutGroupAttribute(string path) : base(path) { }
    }
}
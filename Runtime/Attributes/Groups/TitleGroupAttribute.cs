using System;
using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class TitleGroupAttribute : GroupBaseAttribute
    {
        private readonly string m_Label;  
        
        public TitleGroupAttribute(string path, string label = "") : base(path)
        {
            m_Label = label;
        }

        public string GetLabel() => string.IsNullOrWhiteSpace(m_Label) ? Name : m_Label;
    }
}
using System;
using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    [Conditional("UNITY_EDITOR")]
    public class InfoBoxAttribute : Attribute
    {
        public readonly string InfoText;
        
        public InfoBoxAttribute(string infoText)
        {
            InfoText = infoText;
        }
    }
}
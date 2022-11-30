using System;
using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]  
    [Conditional("UNITY_EDITOR")]
    public class DisableInEditorModeAttribute : ConditionalBaseAttribute  
    {  
        public DisableInEditorModeAttribute()  
        {  
        }
    }
}
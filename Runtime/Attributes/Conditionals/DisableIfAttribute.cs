using System;
using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]  
    [Conditional("UNITY_EDITOR")]
    public class DisableIfAttribute : ConditionalBaseAttribute
    {
        public readonly string Condition;
        public readonly object ExpectedValue;
        
        public DisableIfAttribute(string condition)
        {
            Condition = condition;
            ExpectedValue = null;
        }
        
        public DisableIfAttribute(string condition, object expectedValue)
        {
            Condition = condition;
            ExpectedValue = expectedValue;
        }
    }
}
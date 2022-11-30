using System;
using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]  
    [Conditional("UNITY_EDITOR")]
    public class EnableIfAttribute : ConditionalBaseAttribute
    {
        public readonly string Condition;
        public readonly object ExpectedValue;
        
        public EnableIfAttribute(string condition)
        {
            Condition = condition;
            ExpectedValue = null;
        }
        
        public EnableIfAttribute(string condition, object expectedValue)
        {
            Condition = condition;
            ExpectedValue = expectedValue;
        }
    }
}
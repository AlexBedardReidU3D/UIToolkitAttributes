using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Field)]  
    [Conditional("UNITY_EDITOR")]
    public class CustomLabelAttribute : System.Attribute  
    {  
        private readonly string m_Label;  
    
        public CustomLabelAttribute(string label)  
        {  
            m_Label = label;  
        }

        public string GetText() => m_Label;
    } 
}
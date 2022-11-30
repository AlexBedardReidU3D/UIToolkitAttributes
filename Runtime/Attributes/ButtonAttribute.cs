using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Method)]  
    [Conditional("UNITY_EDITOR")]
    public class ButtonAttribute : System.Attribute  
    {  
        private readonly string m_Text;  
    
        public ButtonAttribute(string text)  
        {  
            m_Text = text;  
        }
    
        public ButtonAttribute()  
        {  
        }

        public string GetText() => m_Text;
    } 
}
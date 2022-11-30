using System.Diagnostics;

namespace UIToolkit.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Field)]  
    [Conditional("UNITY_EDITOR")]
    public class DisplayAsStringAttribute : System.Attribute  
    {  
        public DisplayAsStringAttribute()  
        {  
        }
    } 
}
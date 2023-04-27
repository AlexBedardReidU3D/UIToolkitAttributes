﻿#define GENERATE_SCRIPTS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UIToolkit.Attributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkit.Editor.Utilities.FileWriters
{
    public static class ScriptGenerator
    {
        private static readonly string PATH = Path.Combine(Application.dataPath, "Editor", "Custom Inspectors");

        //ScriptGenerator Functions
        //================================================================================================================//

        public static void CreateCustomEditor(in Type type, 
            in IEnumerable<MethodInfo> methodButtons, 
            in IEnumerable<LabelBindingData> labelBindingDatas,
            in IEnumerable<ConditionalData> conditionalDatas)
        {
#if GENERATE_SCRIPTS
            var directoryInfo = new DirectoryInfo(Path.Combine(PATH, type.Name));
            
            if(directoryInfo.Exists == false)
                directoryInfo.Create();

            string className;

            string code;
            //If this uses a UnityEngine.Component, we can use a Custom Inspector
            if (type.IsSubclassOf(typeof(Component)))
            {
                className = $"{type.Name}CustomInspector";
                code = GenerateCustomEditorCode(type, className, methodButtons, labelBindingDatas, conditionalDatas);
            }
            //Otherwise Assume that we just need a property drawer
            else
            {
                className = $"{type.Name}PropertyDrawer";
                code = GenerateCustomPropertyDrawerCode(type, className, methodButtons, labelBindingDatas, conditionalDatas);
            }

            var filename = $"{className}.cs";
            var path = Path.Combine(directoryInfo.FullName, filename);
            File.WriteAllText(path, code);
#endif
        }

        //Code Generation Functions
        //================================================================================================================//

        private static string GenerateCustomEditorCode(in Type type, in string className, 
            in IEnumerable<MethodInfo> buttons, 
            in IEnumerable<LabelBindingData> labelBindingDatas,
            in IEnumerable<ConditionalData> conditionalDatas)
        {
            var writer = new Writer
            {
                buffer = new StringBuilder()
            };
            
            var objectInstanceName = $"{type.Name}Instance";


            // Header.
            writer.WriteLine(WriterHelper.MakeAutoGeneratedCodeHeader("UXML Generator",
                new Version(0,0,1).ToString(),
                nameof(ScriptGenerator)));
            // Usings.
            GetUsings(ref writer);
            
            writer.WriteLine($"[CustomEditor(typeof({type.GetSafeName()}))]");
            
            // Begin class.
            writer.WriteLine($"public class @{className} : UnityEditor.Editor");
            writer.BeginBlock();
            writer.WriteLine($"private List<{nameof(SimpleBinding)}> savedBindings;");
            writer.WriteLine($"private List<{nameof(ConditionalBinding)}> savedConditionalBindings;");
            writer.WriteLine();
            
            // Default CreateInspectorGUI.
            writer.WriteLine("public override VisualElement CreateInspectorGUI()");
            writer.BeginBlock();
            writer.WriteLine($"var {objectInstanceName}= ({type.GetSafeName()})target;");
            writer.WriteLine($"var classType = {objectInstanceName}.GetType();");
            writer.WriteLine();
            SetupInspector(type, ref writer);

            //Button callbacks
            GetButtonsCode(objectInstanceName, buttons, ref writer);

            //Custom Label Bindings
            var needsEventMethod = GetLabelBindsCode(labelBindingDatas, ref writer);

            //Custom Conditionals for Editing
            var needClickEventMethod = GetConditionalsCode(conditionalDatas, ref writer);
            
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine();
            
            writer.WriteLine("// Return the finished inspector UI");
            writer.WriteLine("return myInspector;");
            //End Function
            writer.EndBlock();

            //Add Other Functions
            //----------------------------------------------------------//
            
            //See if we should be adding the function to setup conditionals
            TryAddSetEnabledMethod(conditionalDatas, ref writer);
            
            if(needsEventMethod)
                TryAddKeyUpEventMethod(type, ref writer);
            if (needClickEventMethod)
                TryAddConditionalEventMethod(ref writer);
            
            //----------------------------------------------------------//

            //End Class
            writer.EndBlock();
            
            return writer.buffer.ToString();
        }

        private static string GenerateCustomPropertyDrawerCode(in Type type, 
            in string className,
            in IEnumerable<MethodInfo> buttons, 
            in IEnumerable<LabelBindingData> labelBindingDatas,
            in IEnumerable<ConditionalData> conditionalDatas)
        {
            var writer = new Writer
            {
                buffer = new StringBuilder()
            };

            // Header.
            writer.WriteLine(WriterHelper.MakeAutoGeneratedCodeHeader("UXML Generator",
                new Version(0,0,1).ToString(),
                nameof(ScriptGenerator)));
            
            // Usings.
            GetUsings(ref writer);
            
            writer.WriteLine($"[CustomPropertyDrawer(typeof({type.GetSafeName()}))]");
            
            // Begin class.
            writer.WriteLine($"public class @{className} : PropertyDrawer");
            writer.BeginBlock();
            
            writer.WriteLine($"private List<{nameof(ConditionalBinding)}> savedBindings;");
            writer.WriteLine($"private List<{nameof(ConditionalBinding)}> savedConditionalBindings;");
            writer.WriteLine();
            
            // Default CreateInspectorGUI.
            writer.WriteLine("public override VisualElement CreatePropertyGUI(SerializedProperty property)");
            writer.BeginBlock();

            SetupInspector(type, ref writer);
            
            //Button callbacks
            if (buttons.Any())
            {
                /*var valueTarget = fieldInfo.GetValue(property.serializedObject.targetObject);
        var classType = fieldInfo.FieldType;

        //TestButton Action Callback
        var TestButtonMethod = classType.GetMethod("TestButton", BindingFlags.NonPublic | BindingFlags.Instance);
        myInspector.Q<UnityEngine.UIElements.Button>("TestButton").clickable.clicked += () =>
        {
            TestButtonMethod.Invoke(valueTarget, default);
        };*/
                writer.WriteLine("//----------------------------------------------------------//");
                writer.WriteLine("//Button Attribute Calls");
                writer.WriteLine("//----------------------------------------------------------//");

                writer.WriteLine();
                writer.WriteLine("var valueTarget = fieldInfo.GetValue(property.serializedObject.targetObject);");
                writer.WriteLine("var classType = fieldInfo.FieldType;");
                writer.WriteLine();
                
                foreach (var methodInfo in buttons)
                {
                    var methodVarName = $"{methodInfo.Name}Method";
                    
                    writer.WriteLine($"//{methodInfo.Name} Action Callback");
                    writer.WriteLine($"var {methodVarName} = classType.GetMethod(\"{methodInfo.Name}\", BindingFlags.NonPublic | BindingFlags.Instance);");
                    writer.WriteLine($"myInspector.Q<UnityEngine.UIElements.Button>(\"{methodInfo.Name}\").clickable.clicked += () =>");
                    writer.BeginBlock();
                    writer.WriteLine($"{methodVarName}.Invoke(valueTarget, default);");
                    writer.EndBlock(';');
                    writer.WriteLine();
                }
                
                writer.WriteLine("//----------------------------------------------------------//");
                writer.WriteLine();

            }
            
            //Custom Label Bindings
            var needsEventMethod = GetLabelBindsCode(labelBindingDatas, ref writer);
            
            //Custom Conditionals for Editing
            var needConditionalEvent = GetConditionalsCode(conditionalDatas, ref writer);
            
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine();
            
            writer.WriteLine("// Return the finished inspector UI");
            writer.WriteLine("return myInspector;");
            //End Function
            writer.EndBlock();

            //Add Other Functions
            //----------------------------------------------------------//

            //See if we should be adding the function to setup conditionals
            TryAddSetEnabledMethod(conditionalDatas, ref writer);

            if(needsEventMethod)
                TryAddKeyUpEventMethod(type, ref writer);
            
            if (needConditionalEvent)
                TryAddConditionalEventMethod(ref writer);

            //----------------------------------------------------------//
            
            //End Class
            writer.EndBlock();
            
            return writer.buffer.ToString();
        }


        //Generate Code
        //================================================================================================================//
        
        private static void GetUsings(ref Writer writer)
        {
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine("using UIToolkit.Editor.Utilities;");
            writer.WriteLine("using UIToolkit.Editor;");
            
            writer.WriteLine("using System.Reflection;");
            writer.WriteLine("using UnityEditor;");
            writer.WriteLine("using UnityEngine;");
            writer.WriteLine("using UnityEditor.UIElements;");
            writer.WriteLine("using UnityEngine.UIElements;");
            writer.WriteLine();
        }

        //Generate Body Code
        //================================================================================================================//

        #region Generate Body Code

        private static void SetupInspector(in Type type, ref Writer writer)
        {
            writer.WriteLine("// Create a new VisualElement to be the root of our inspector UI");
            writer.WriteLine("VisualElement myInspector = new VisualElement();");
            writer.WriteLine("myInspector.Add(new Label(\"This is a custom inspector\"));");
            writer.WriteLine("// Load and clone a visual tree from UXML");
            writer.WriteLine($"VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(\"Assets/Editor/Custom Inspectors/{type.Name}/{type.Name}UXML.uxml\");");
            writer.WriteLine("visualTree.CloneTree(myInspector);");
            writer.WriteLine();
        }

        private static void GetButtonsCode(in string objectInstanceName, in IEnumerable<MethodInfo> buttons, ref Writer writer)
        {
            //Button callbacks
            if (buttons.Any() == false) 
                return;
            
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine("//Button Attribute Calls");
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine();
                
            foreach (var methodInfo in buttons)
            {
                var methodVarName = $"{methodInfo.Name}Method";
                    
                writer.WriteLine($"//{methodInfo.Name} Action Callback");
                writer.WriteLine($"var {methodVarName} = classType.GetMethod(\"{methodInfo.Name}\", BindingFlags.NonPublic | BindingFlags.Instance);");
                writer.WriteLine($"myInspector.Q<UnityEngine.UIElements.Button>(\"{methodInfo.Name}\").clickable.clicked += () =>");
                writer.BeginBlock();
                writer.WriteLine($"{methodVarName}.Invoke({objectInstanceName}, default);");
                writer.EndBlock(';');
                writer.WriteLine();
            }
                
            writer.WriteLine();
        }
        
        private static bool GetLabelBindsCode(in IEnumerable<LabelBindingData> labelBindingDatas, ref Writer writer)
        {
            //Custom Label Bindings
            if (labelBindingDatas.Any() == false) 
                return false;
            
            /*var labelClass = GroupBox.labelUssClassName;
                    myInspector.Q<GroupBox>("Dynamic Group")
                            .Q<Label>(null, labelClass).bindingPath = "myDynamicLabel";*/
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine("//Custom Label Bindings");
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine();

            var needsEventMethod = false;
            var nonFieldLabelBindings = labelBindingDatas.Where(x => x.IsField == false);
            if (nonFieldLabelBindings.Any())
            {
                needsEventMethod = true;
                
                writer.WriteLine($"savedBindings = new List<{nameof(SimpleBinding)}>();");
                writer.WriteLine("VisualElement savedElement;");
                writer.WriteLine("MemberInfo savedMemberInfo;");
                writer.WriteLine();
                writer.WriteLine("myInspector.RegisterCallback<UnityEngine.UIElements.KeyUpEvent>(new EventCallback<KeyUpEvent>(OnKeyUpEvent));");
                writer.WriteLine();
                foreach (var labelBinding in nonFieldLabelBindings)
                {
                    writer.WriteLine($"savedElement = myInspector.Q<GroupBox>(\"{labelBinding.ParentName}\").Q<Label>(null, GroupBox.labelUssClassName);");
                    writer.WriteLine($"savedMemberInfo = classType.GetFirstMember(\"{labelBinding.BindingPath}\");");
                    writer.WriteLine($"savedBindings.Add(new {nameof(SimpleBinding)}(savedElement, savedMemberInfo));");
                    writer.WriteLine();
                }
                
                writer.WriteLine("//Force Update the Labels once all connected");
                writer.WriteLine("OnKeyUpEvent(default);");
                writer.WriteLine();
                writer.WriteLine("//----------------------------------------------------------//");
                writer.WriteLine();
            }


            var fieldLabelBindings = labelBindingDatas.Where(x => x.IsField);
            foreach (var labelBinding in fieldLabelBindings)
            {
                   
                writer.WriteLine($"//{labelBinding.ToString()}");
                if (labelBinding.ParentType == typeof(Foldout))
                {
                    writer.WriteLine($"myInspector.Q<{labelBinding.ParentType.Name}>(\"{labelBinding.ParentName}\")");
                    writer.WriteLine($"\t.Q<{nameof(Label)}>(null, {nameof(Foldout)}.{nameof(Foldout.textUssClassName)}).bindingPath = \"{labelBinding.BindingPath}\";"); 
                }
                else
                {
                    writer.WriteLine($"myInspector.Q<{labelBinding.ParentType.Name}>(\"{labelBinding.ParentName}\")");
                    writer.WriteLine($"\t.Q<{nameof(Label)}>(null, {labelBinding.ParentType.Name}.labelUssClassName).bindingPath = \"{labelBinding.BindingPath}\";"); 
                }

                writer.WriteLine();

            }
                
            writer.WriteLine();

            return needsEventMethod;
        }
        
        private static bool GetConditionalsCode(in IEnumerable<ConditionalData> conditionalDatas, ref Writer writer)
        {
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine("//Conditional Editors");
            writer.WriteLine("//----------------------------------------------------------//");
            writer.WriteLine();

            //----------------------------------------------------------//

            var needConditionalEvent = conditionalDatas.Any(x => x.conditionalAttribute is EnableIfAttribute ||
                                                                 x.conditionalAttribute is DisableIfAttribute);
            if (needConditionalEvent)
            {
                writer.WriteLine($"savedConditionalBindings = new List<{nameof(ConditionalBinding)}>();");
                writer.WriteLine("VisualElement item;");
                writer.WriteLine("MemberInfo memberInfo;");
                writer.WriteLine();
                writer.WriteLine("myInspector.RegisterCallback<UnityEngine.UIElements.PointerMoveEvent>(new EventCallback<PointerMoveEvent>(UpdateConditionalEvent));");
                writer.WriteLine("myInspector.RegisterCallback<UnityEngine.UIElements.ClickEvent>(new EventCallback<ClickEvent>(UpdateConditionalEvent));");
                writer.WriteLine();
            }
            
            if (conditionalDatas.Any(x => x.conditionalAttribute is DisableInEditorModeAttribute ||
                                          x.conditionalAttribute is DisableInPlayModeAttribute))
            {
                writer.WriteLine("var applicationIsPlaying = Application.isPlaying;");
            }
            //----------------------------------------------------------//

            
            //Just Sort/Order by conditional type (name)
            var groupedConditionals = conditionalDatas
                .OrderBy(x => x.conditionalAttribute.GetType().Name)
                .ToArray();

            Type lastType = null;
            bool addLabel = true;
            foreach (var conditionalData in groupedConditionals)
            {
                if (lastType != conditionalData.conditionalAttribute.GetType())
                {
                    lastType = conditionalData.conditionalAttribute.GetType();
                    addLabel = true;
                    writer.WriteLine();
                }
                
                switch (conditionalData.conditionalAttribute)
                {
                    case DisableInEditorModeAttribute _ :
                        if(addLabel)
                            writer.WriteLine("//Disable in Editor");
                        writer.WriteLine($"SetEnabled(myInspector.Q<{conditionalData.ParentType.Name}>(\"{conditionalData.ParentName}\"), applicationIsPlaying, true);");
                        break;
                    case DisableInPlayModeAttribute _ :
                        if(addLabel)
                            writer.WriteLine("//Disable in Play mode");
                        writer.WriteLine($"SetEnabled(myInspector.Q<{conditionalData.ParentType.Name}>(\"{conditionalData.ParentName}\"), applicationIsPlaying, false);");
                        break;
                    case EnableIfAttribute enableIfAttribute when enableIfAttribute.ExpectedValue != null:
                        if(addLabel)
                            writer.WriteLine("//Enable If");
                        writer.WriteLine($"item = myInspector.Q<VisualElement>(\"{conditionalData.ParentName}\");");
                        writer.WriteLine($"memberInfo = classType.GetFirstMember(\"{enableIfAttribute.Condition}\");");
                        writer.WriteLine($"savedConditionalBindings.Add(new {nameof(ConditionalBinding)}(item, memberInfo,  {enableIfAttribute.ExpectedValue.ToStaticString()}, true));");
                        writer.WriteLine();
                        break;
                    case DisableIfAttribute disableIfAttribute when disableIfAttribute.ExpectedValue != null:
                        if(addLabel)
                            writer.WriteLine("//Disable If");
                        writer.WriteLine($"item = myInspector.Q<VisualElement>(\"{conditionalData.ParentName}\");");
                        writer.WriteLine($"memberInfo = classType.GetFirstMember(\"{disableIfAttribute.Condition}\");");
                        writer.WriteLine($"savedConditionalBindings.Add(new {nameof(ConditionalBinding)}(item, memberInfo,  {disableIfAttribute.ExpectedValue.ToStaticString()}, false));");
                        writer.WriteLine();
                        break;
                    case EnableIfAttribute enableIfAttribute :
                        if(addLabel)
                            writer.WriteLine("//Enable If");
                        writer.WriteLine($"item = myInspector.Q<VisualElement>(\"{conditionalData.ParentName}\");");
                        writer.WriteLine($"memberInfo = classType.GetFirstMember(\"{enableIfAttribute.Condition}\");");
                        writer.WriteLine($"savedConditionalBindings.Add(new {nameof(ConditionalBinding)}(item, memberInfo, true));");
                        writer.WriteLine();
                        break;
                    case DisableIfAttribute disableIfAttribute:
                        if(addLabel)
                            writer.WriteLine("//Disable If");
                        writer.WriteLine($"item = myInspector.Q<VisualElement>(\"{conditionalData.ParentName}\");");
                        writer.WriteLine($"memberInfo = classType.GetFirstMember(\"{disableIfAttribute.Condition}\");");
                        writer.WriteLine($"savedConditionalBindings.Add(new {nameof(ConditionalBinding)}(item, memberInfo, false));");
                        writer.WriteLine();
                        break;
                    default:
                        throw new NotImplementedException();
                }


                if (addLabel == false)
                    continue;
                
                //writer.WriteLine();
                addLabel = false;
            }
            
            if(needConditionalEvent)
                writer.WriteLine("UpdateConditionalEvent(default);");
            
            writer.WriteLine();

            return needConditionalEvent;
        }

        #endregion //Generate Body Code

        //Add new Methods
        //================================================================================================================//

        #region Add New Methods

        private static void TryAddSetEnabledMethod(in IEnumerable<ConditionalData> conditionalDatas, ref Writer writer)
        {
            if (conditionalDatas.Any() == false)
                return;
            
            writer.WriteLine();
            writer.WriteLine("private void SetEnabled(in VisualElement visualElement, in bool currentResult, in bool expectedResult)");
            writer.BeginBlock();
            
            writer.WriteLine("var setState = currentResult.Equals(expectedResult);");
            writer.WriteLine();
            writer.WriteLine("visualElement.focusable = setState;");
            writer.WriteLine("visualElement.SetEnabled(setState);");
            writer.WriteLine();

            writer.WriteLine("if(setState == false)");
            writer.WriteLine("\tvisualElement.AddToClassList(\"read-only\");");
            writer.WriteLine("else");
            writer.WriteLine("\tvisualElement.RemoveFromClassList(\"read-only\");");
            writer.EndBlock();
            
            
        }

        private static void TryAddKeyUpEventMethod(Type type, ref Writer writer)
        {
            writer.WriteLine();
            writer.WriteLine("private void OnKeyUpEvent(KeyUpEvent evt)");
            writer.BeginBlock();
            writer.WriteLine($"var MyClassInstance = ({type.Name})target;");
            writer.WriteLine();
            writer.WriteLine("foreach (var bindingData in savedBindings)");
            writer.BeginBlock();
            writer.WriteLine("var newValue = bindingData.MemberInfo.GetValue(MyClassInstance);");
            writer.WriteLine();
            writer.WriteLine("if (bindingData.RequiresUpdate(newValue) == false)");
            writer.WriteLine("\tcontinue;");
            writer.WriteLine();
            writer.WriteLine("switch (bindingData.SavedElement)");
            writer.BeginBlock();
            writer.WriteLine("case Label label:");
            writer.WriteLine("\tlabel.text = (string)newValue;");
            writer.WriteLine("\tbreak;");
            writer.WriteLine("default:");
            writer.WriteLine("\tthrow new NotImplementedException($\"{bindingData.SavedElement.GetType().Name} not yet supported\");");
            //End Switch
            writer.EndBlock();
            //End Foreach
            writer.EndBlock();
            //End Method
            writer.EndBlock();
            writer.WriteLine();
        }
        
        private static void TryAddConditionalEventMethod(ref Writer writer)
        {
            writer.WriteLine();
            writer.WriteLine("private void UpdateConditionalEvent(EventBase _)");
            writer.BeginBlock();
            writer.WriteLine("var MyClassInstance = (MyClass)target;");
            writer.WriteLine();
            writer.WriteLine("foreach (var bindingData in savedConditionalBindings)");
            writer.BeginBlock();
            writer.WriteLine("var newValue = bindingData.MemberInfo.GetValue(MyClassInstance);");
            writer.WriteLine();
            writer.WriteLine("if (bindingData.RequiresUpdate(newValue) == false)");
            writer.WriteLine("\tcontinue;");
            writer.WriteLine();
            writer.WriteLine("bindingData.CurrentValue = newValue;");
            writer.WriteLine("SetEnabled(bindingData.SavedElement,newValue.Equals(bindingData.TargetValue), bindingData.ExpectedResult);");
            //End Foreach
            writer.EndBlock();
            //End Method
            writer.EndBlock();
            writer.WriteLine();
        }

        #endregion //Add New Methods
        
        //================================================================================================================//

    }
}
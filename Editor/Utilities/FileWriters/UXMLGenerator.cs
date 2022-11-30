using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UIToolkit.Attributes;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

using Label = UnityEngine.UIElements.Label;
using Object = UnityEngine.Object;

namespace UIToolkit.Editor.Utilities.FileWriters
{
    //TODO Take a look here: https://docs.unity3d.com/Manual/roslyn-analyzers.html
    public static class UXMLGenerator
    {
        private static readonly string PATH = Path.Combine(Application.dataPath, "Editor", "Custom Inspectors");

        public enum UITYPE
        {
            UI,
            UIE
        }

        //Classes
        //================================================================================================================//
        /// <summary>
        /// A collection of elements meant to be grouped together in the Inspector
        /// </summary>
        private class MemberGroupInfo
        {
            public GroupBaseAttribute myGroupBaseAttribute;
            public List<object> Objects;
        }

        //Static Properties
        //================================================================================================================//

        private static Dictionary<Type, List<MethodInfo>> s_ButtonFunctions;
        private static Dictionary<Type, List<LabelBindingData>> s_LabelBindingData;
        private static Dictionary<Type, List<ConditionalData>> s_ConditionalFieldData;

        //================================================================================================================//

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            //----------------------------------------------------------//

            Type[] GetAllScriptsWithGenerateUXMLAttribute()
            {
                return (from a in AppDomain.CurrentDomain.GetAssemblies()
                    from t in a.GetTypes()
                    let attributes = t.GetCustomAttributes(typeof(GenerateUXMLAttribute), true)
                    where attributes != null && attributes.Length > 0
                    select t).ToArray();
            }

            //----------------------------------------------------------//

            //Based on: https://stackoverflow.com/a/607204
            var typesWithGenerateUxml = GetAllScriptsWithGenerateUXMLAttribute();

            //Setup dictionaries used to pass information to the Script Generator
            s_ButtonFunctions = new Dictionary<Type, List<MethodInfo>>();
            s_LabelBindingData = new Dictionary<Type, List<LabelBindingData>>();
            s_ConditionalFieldData = new Dictionary<Type, List<ConditionalData>>();

            //Iterate through all of the scripts that use the [GenerateUXML] attribute
            for (int i = 0; i < typesWithGenerateUxml.Length; i++)
            {
                var type = typesWithGenerateUxml[i];

                //Add a new entry on the dictionaries for the current type
                s_ButtonFunctions.Add(type, new List<MethodInfo>());
                s_LabelBindingData.Add(type, new List<LabelBindingData>());
                s_ConditionalFieldData.Add(type, new List<ConditionalData>());
                
                var generatedCode = GenerateUXMLCode(type);
                TryCreateUxmlFile(type, generatedCode);
                ScriptGenerator.CreateCustomEditor(type,
                    s_ButtonFunctions[type],
                    s_LabelBindingData[type],
                    s_ConditionalFieldData[type]);
            }

            Debug.Log("Generated Custom Editor Scripts & UXML for:<b>\n\t- " +
                      $"{string.Join("\n\t- ", typesWithGenerateUxml.Select(x => x.Name))}</b>");

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Writes all UXML information to a directory(Which will be created if it hasn't already) under
        /// Application.dataPath/Editor/Custom Inspectors/CLASS_NAME/
        /// </summary>
        /// <param name="type"></param>
        private static void TryCreateUxmlFile(in Type type, in string uxmlCode)
        {
            DirectoryInfo directoryInfo =
                new DirectoryInfo(Path.Combine(Application.dataPath, "Editor", "Custom Inspectors", type.Name));

            if (directoryInfo.Exists == false)
                directoryInfo.Create();

            //Write new or overwrite existing
            var filePath = Path.Combine(PATH, type.Name, $"{type.Name}UXML.uxml");
            File.WriteAllText(filePath, uxmlCode);
        }

        //Generate UXML
        //================================================================================================================//

        private static string GenerateUXMLCode(in Type type)
        {
            var writer = new UXMLWriter
            {
                buffer = new StringBuilder()
            };

            //Write Header
            writer.WriteLine(WriterHelper.MakeAutoGeneratedCodeXMLHeader(
                "UXML Generator",
                new Version(0, 0, 1).ToString(),
                nameof(UXMLGenerator)));

            writer.WriteLine();

            //Write UXML starter
            writer.WriteLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\" editor-extension-mode=\"True\">");
            writer.Indent();
            //Add custom Style Sheet
            writer.WriteLine("<Style src=\"project://database/Assets/Editor/Prototyping/NewUSSFile.uss?fileID=7433441132597879392&amp;guid=6900da3c74504df48882b65fbc801786&amp;type=3#NewUSSFile\" />");
            //Get type members that are not constructors, ordered by their metadata token
            var memberInfos = type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(x => (x is ConstructorInfo) == false)
                .OrderBy(x => x.MetadataToken)
                .ToArray();

            //Group members together by the specified header name
            var groupedMembers = TryGetGroupedMembers(memberInfos);

            for (var i = 0; i < groupedMembers.Count; i++)
            {
                switch (groupedMembers[i])
                {
                    case FieldInfo fieldInfo:
                        if (fieldInfo.IsPrivate &&
                            fieldInfo.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
                            continue;
                        //Write fields
                        GetFieldAsUXML(type, ref writer, fieldInfo);
                        break;
                    case MemberGroupInfo memberGroupInfo:
                        GetGroupUXML(type, ref writer, memberGroupInfo);
                        break;
                    case MethodInfo methodInfo:
                        GetMethodAsUxml(type, methodInfo, ref writer);
                        break;
                }
            }

            //End Field Block
            writer.Outdent();

            //Close File
            writer.WriteLine("</ui:UXML>");

            return writer.buffer.ToString();
        }

        //UXML Member Functions
        //================================================================================================================//

        #region UXML Member Functions

        private static void GetFieldAsUXML(in Type type, ref UXMLWriter writer, in FieldInfo fieldInfo)
        {
            var conditional = fieldInfo.GetCustomAttribute<ConditionalBaseAttribute>();
            var infoBox = fieldInfo.GetCustomAttribute<InfoBoxAttribute>();
            var customLabel = fieldInfo.GetCustomAttribute<CustomLabelAttribute>();
            var readOnly = fieldInfo.GetCustomAttribute<ReadOnlyAttribute>() != null;
            var displayAsString = fieldInfo.GetCustomAttribute<DisplayAsStringAttribute>() != null;

            var label = customLabel != null ? customLabel.GetText() : fieldInfo.Name;

            if (infoBox != null)
            {
                AddInfoBox(type, ref writer, infoBox);
            }

            //Display As String
            //----------------------------------------------------------//

            if (displayAsString)
            {
                DisplayAsString(type, ref writer, label, fieldInfo);
                return;
            }

            var hasCustomEditor = fieldInfo.FieldType.HasCustomEditor();

            //Custom Editor Drawer, for non-unity objects
            //----------------------------------------------------------//
            if (hasCustomEditor && fieldInfo.FieldType.IsSubclassOf(typeof(Object)) == false &&
                fieldInfo.GetCustomAttribute<BoxGroupAttribute>() == null)
            {
                BeginFoldoutGroup(ref writer, label);
                ElementBuilder(type, ref writer, UITYPE.UIE, "PropertyField", fieldInfo.Name, conditional, label: label,
                    bindingPath: fieldInfo.Name, isReadonly: readOnly);
                EndFoldoutGroup(ref writer);
                return;
            }


            //String field element
            //----------------------------------------------------------//

            if (fieldInfo.FieldType == typeof(String))
            {
                ElementBuilder(type, ref writer, UITYPE.UI, "TextField", fieldInfo.Name, conditional, label: label,
                    bindingPath: fieldInfo.Name, pickingMode: "ignore", isReadonly: readOnly);

                return;
            }

            //Class Drawing
            //----------------------------------------------------------//
            if (fieldInfo.FieldType.IsClass)
            {
                switch (fieldInfo.FieldType.Namespace)
                {
                    case "UnityEngine":
                        ElementBuilder(type, ref writer, UITYPE.UIE, "ObjectField", fieldInfo.Name, conditional,
                            label: label, bindingPath: fieldInfo.Name, types: new[]
                            {
                                fieldInfo.FieldType.FullName,
                                "UnityEngine.CoreModule"
                            }, isReadonly: readOnly);

                        return;
                    default:
                        ElementBuilder(type, ref writer, UITYPE.UIE, "PropertyField", fieldInfo.Name, conditional,
                            label: label, bindingPath: fieldInfo.Name, isReadonly: readOnly);
                        return;
                }
            }
            //Enum Type
            //----------------------------------------------------------//

            if (fieldInfo.FieldType.IsSubclassOf(typeof(Enum)))
            {
                //<uie:EnumField label="MyEnum" type="MyNamespace.MyEnum, AssemblyName" />
                ElementBuilder(type, ref writer,
                    UITYPE.UIE,
                    "EnumField",
                    fieldInfo.Name,
                    conditional,
                    types: new[]
                    {
                        fieldInfo.FieldType.FullName,
                        fieldInfo.FieldType.Assembly.GetName().Name
                    },
                    label: label,
                    bindingPath: fieldInfo.Name,
                    isReadonly: readOnly);
                return;
            }

            //Bool Type
            //----------------------------------------------------------//
            if (fieldInfo.FieldType == typeof(bool))
            {
                //<uie:EnumField label="MyEnum" type="MyNamespace.MyEnum, AssemblyName" />
                ElementBuilder(type, ref writer,
                    UITYPE.UI,
                    "Toggle",
                    fieldInfo.Name,
                    conditional,
                    label: label,
                    bindingPath: fieldInfo.Name,
                    isReadonly: readOnly);
                return;
            }

            //Value Type
            //----------------------------------------------------------//

            string uieType;
            switch (fieldInfo.FieldType.Name)
            {
                case nameof(Int32):
                    uieType = "IntegerField";
                    break;
                case nameof(Single):
                    uieType = "FloatField";
                    break;
                case nameof(Int64):
                    uieType = "LongField";
                    break;
                case nameof(Vector2):
                case nameof(Vector3):
                case nameof(Vector4):
                case nameof(Rect):
                case nameof(Bounds):
                case nameof(Vector2Int):
                case nameof(Vector3Int):
                    uieType = $"{fieldInfo.FieldType.Name}Field";
                    break;
                default:
                    ElementBuilder(type, ref writer, UITYPE.UIE, "PropertyField", fieldInfo.Name, conditional,
                        label: label, bindingPath: fieldInfo.Name, isReadonly: readOnly);
                    return;

            }

            //Default Return
            //----------------------------------------------------------//

            ElementBuilder(type, ref writer, UITYPE.UIE, uieType, fieldInfo.Name, conditional, label: label,
                value: string.Empty, bindingPath: fieldInfo.Name, isReadonly: readOnly);
        }

        /// <summary>
        /// When a method has a Button Attribute, we include the element here
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodInfo"></param>
        /// <param name="writer"></param>
        private static void GetMethodAsUxml(in Type type, in MethodInfo methodInfo, ref UXMLWriter writer)
        {
            var button = methodInfo.GetCustomAttribute<ButtonAttribute>();

            if (button == null)
                return;

            var conditional = methodInfo.GetCustomAttribute<ConditionalBaseAttribute>();

            var infoBox = methodInfo.GetCustomAttribute<InfoBoxAttribute>();
            if (infoBox != null)
            {
                AddInfoBox(type, ref writer, infoBox);
            }

            s_ButtonFunctions[type].Add(methodInfo);

            var buttonText = button.GetText();

            var label = string.IsNullOrEmpty(buttonText) ? methodInfo.Name : buttonText;

            //FIXME Binding Path gets a string for the label, and is not the call for the method
            ElementBuilder(type, ref writer, UITYPE.UI, "Button", methodInfo.Name, conditional, text: label,
                elidedTooltip: true);
        }

        #endregion //UXML Member Functions

        //UXML Group Functions
        //================================================================================================================//

        #region UXML Group Functions

        /// <summary>
        /// This returns a mixed list of elements, that can contain both raw members, as well as the GroupInfo.
        /// </summary>
        /// <param name="memberInfos"></param>
        /// <returns></returns>
        /// <exception cref="MissingMemberException"></exception>
        /// <exception cref="Exception"></exception>
        private static List<object> TryGetGroupedMembers(IReadOnlyList<MemberInfo> memberInfos)
        {
            var outList = new List<object>();
            var groupKeys = new Dictionary<string, MemberGroupInfo>();

            //----------------------------------------------------------//

            bool ContainsPathGroups(in GroupBaseAttribute groupBaseAttribute,
                in Dictionary<string, MemberGroupInfo> groups)
            {
                //To maintain the integrity/unique sub groups this is the path structure for a group key:
                // - VerticalLayout <- Root
                // - VerticalLayout/HorizontalGroup <- Sub Group
                // - VerticalLayout/HorizontalGroup/BoxGroup2 <- Sub Group
                var groupPaths = groupBaseAttribute.Path.Split('/');

                for (var g = 0; g < groupPaths.Length - 1; g++)
                {
                    var pathCheck = string.Join('/', groupPaths.Take(g + 1));
                    if (groups.ContainsKey(pathCheck) == false)
                        throw new MissingMemberException($"No group has been created with name {pathCheck}");
                }

                return true;
            }

            void AddElementToGroup(in string groupName, in string groupPath, in GroupBaseAttribute groupsBase,
                object toAdd, string groupParentPath = null)
            {
                //See if we've already created the group, if not we'll have to add it
                if (groupKeys.TryGetValue(groupPath, out var foundGroup) == false)
                {
                    var groupData = new MemberGroupInfo
                    {
                        myGroupBaseAttribute = groupsBase,
                        Objects = new List<object>()
                    };

                    //We only add to the group if we've specified an item
                    if (toAdd != null)
                        groupData.Objects.Add(toAdd);

                    groupKeys.Add(groupPath, groupData);
                    //If this is a sub group we want to add the group to its parent, otherwise a root group can be added
                    // to the list of elements getting returned
                    if (groupParentPath == null) outList.Add(groupData);
                    else groupKeys[groupParentPath].Objects.Add(groupData);
                }
                else
                {
                    //If we have a group already, but it doesn't match the one we're trying to make not, something is wrong
                    if (groupsBase.GetType() != foundGroup.myGroupBaseAttribute.GetType())
                    {
                        throw new Exception(
                            $"Group {groupName} Cannot use {groupsBase.GetType()} as it already exists as {foundGroup.myGroupBaseAttribute.GetType()}");
                    }

                    if (toAdd != null)
                        foundGroup.Objects.Add(toAdd);
                }
            }

            //----------------------------------------------------------//

            for (var i = 0; i < memberInfos.Count; i++)
            {
                //Don't want to store the constructor
                if (memberInfos[i] is ConstructorInfo)
                    continue;

                //Check to see if we want to use a group for this member
                var allGroupAttributes = memberInfos[i].GetCustomAttributes<GroupBaseAttribute>()
                    .ToArray();

                //If there's no group, store it so we can retain the order
                if (allGroupAttributes.Length == 0)
                {
                    outList.Add(memberInfos[i]);
                    continue;
                }

                //Iterate over all the groups, looking for stacks of attributes or a single group
                for (var g = 0; g < allGroupAttributes.Length; g++)
                {
                    //We need this check in the event we have a stack of groups, we only want to add the member to the last mentioned
                    var isLastGroup = g == allGroupAttributes.Length - 1;

                    var groupsBase = allGroupAttributes[g];
                    var groupName = groupsBase.Name;
                    var groupPath = groupsBase.Path;

                    //The first check determines if there are parent groups, and checks if they exist
                    //================================================================================================================//
                    if (groupsBase.HasSubGroups && ContainsPathGroups(groupsBase, groupKeys))
                    {
                        AddElementToGroup(groupName, groupPath, groupsBase, isLastGroup ? memberInfos[i] : null,
                            groupsBase.ParentPath);
                    }
                    //================================================================================================================//
                    else
                    {
                        AddElementToGroup(groupName, groupPath, groupsBase, isLastGroup ? memberInfos[i] : null);
                    }
                    //================================================================================================================//
                }
            }

            return outList;
        }

        /// <summary>
        /// Iterates through the MemberGroupInfo objects, auto populating the UXML information
        /// </summary>
        /// <param name="type"></param>
        /// <param name="writer"></param>
        /// <param name="memberGroupInfo"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void GetGroupUXML(in Type type, ref UXMLWriter writer, in MemberGroupInfo memberGroupInfo)
        {
            BeginGroup(type, memberGroupInfo.myGroupBaseAttribute, ref writer);
            foreach (var memberObject in memberGroupInfo.Objects)
            {
                switch (memberObject)
                {
                    //If the elements shouldn't be drawn, just ignore it
                    case FieldInfo fieldInfo:
                        if (fieldInfo.IsPrivate &&
                            fieldInfo.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
                            continue;
                        //Write fields
                        GetFieldAsUXML(type, ref writer, fieldInfo);
                        break;
                    case MethodInfo methodInfo:
                        GetMethodAsUxml(type, methodInfo, ref writer);
                        break;
                    case MemberGroupInfo subGroupMemberInfo:
                        GetGroupUXML(type, ref writer, subGroupMemberInfo);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(memberObject), memberObject, null);
                }
            }

            EndGroup(memberGroupInfo.myGroupBaseAttribute, ref writer);
        }

        private static void BeginGroup(Type type, in GroupBaseAttribute groupBaseAttribute, ref UXMLWriter writer)
        {
            string CheckHasBindingPath(in string elementType, in string elementName, in string label)
            {
                if (LabelIsDynamic(label, out var bindingPath) == false)
                {
                    return $"text=\"{label}\"";
                }

                s_LabelBindingData[type].Add(new LabelBindingData
                {
                    ParentType = GetElementAsType(elementType),
                    ParentName = elementName,
                    BindingPath = bindingPath,
                    IsField = type.IsField(bindingPath)
                });

                //We need to force add text here, otherwise a Groupbox will not add the label element
                return "text=\"$CUSTOM BIND$\"";
            }

            string labelText;
            switch (groupBaseAttribute)
            {
                case VerticalLayoutGroupAttribute _:
                    writer.WriteLine($"<ui:GroupBox name=\"{groupBaseAttribute.Name}\">");
                    break;
                case HorizontalLayoutGroupAttribute _:
                    writer.WriteLine($"<ui:GroupBox name=\"{groupBaseAttribute.Name}\" class=\"horizontal-group\">");
                    break;
                case TitleGroupAttribute titleGroup:
                    labelText = CheckHasBindingPath(nameof(GroupBox), groupBaseAttribute.Name, titleGroup.GetLabel());
                    writer.WriteLine(
                        $"<ui:GroupBox name=\"{groupBaseAttribute.Name}\" {labelText} class=\"title-group\">");
                    break;
                case FoldoutGroupAttribute foldoutGroup:
                    labelText = CheckHasBindingPath(nameof(Foldout), groupBaseAttribute.Name, foldoutGroup.GetLabel());
                    writer.WriteLine(
                        $"<ui:Foldout name=\"{groupBaseAttribute.Name}\" {labelText} value=\"true\" class=\"foldout-group\">");
                    break;
                case BoxGroupAttribute boxGroup:
                    labelText = CheckHasBindingPath(nameof(GroupBox), groupBaseAttribute.Name, boxGroup.GetLabel());
                    writer.WriteLine(
                        $"<ui:GroupBox name=\"{groupBaseAttribute.Name}\" {labelText} class=\"box-group\">");
                    break;
                default:
                    throw new NotImplementedException();
            }

            writer.Indent();

        }

        private static void EndGroup(in GroupBaseAttribute groupBaseAttribute, ref UXMLWriter writer)
        {
            writer.Outdent();
            switch (groupBaseAttribute)
            {
                case VerticalLayoutGroupAttribute _:
                case HorizontalLayoutGroupAttribute _:
                case TitleGroupAttribute _:
                case BoxGroupAttribute _:
                    writer.WriteLine("</ui:GroupBox>");
                    break;
                case FoldoutGroupAttribute _:
                    writer.WriteLine("</ui:Foldout>");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion //UXML Group Functions

        //ElementBuilder
        //================================================================================================================//

        #region ElementBuilder

        /// <summary>
        /// Based on the passed parameters a UXML element is created, and automatically added to the writer buffer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="uxmlWriter"></param>
        /// <param name="uiType"></param>
        /// <param name="fieldType"></param>
        /// <param name="name"></param>
        /// <param name="conditional"></param>
        /// <param name="label"></param>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="bindingPath"></param>
        /// <param name="types"></param>
        /// <param name="pickingMode"></param>
        /// <param name="style"></param>
        /// <param name="classes"></param>
        /// <param name="isReadonly"></param>
        /// <param name="elidedTooltip"></param>
        /// <exception cref="Exception"></exception>
        private static void ElementBuilder(in Type type,
            ref UXMLWriter uxmlWriter,
            in UITYPE uiType,
            in string fieldType,
            in string name,
            in ConditionalBaseAttribute conditional,
            in string label = null,
            in string text = null,
            in string value = null,
            in string bindingPath = null,
            in string[] types = null,
            in string pickingMode = null,
            in string style = null,
            string[] classes = null,
            in bool isReadonly = false,
            in bool elidedTooltip = false)
        {
            var assembly = new List<string>
            {
                fieldType,
            };
            if (name != null)
                assembly.Add($"name=\"{name}\"");
            if (label != null)
            {
                if (LabelIsDynamic(label, out var customBindingPath) == false)
                    assembly.Add($"label=\"{label}\"");
                else
                {
                    if (string.IsNullOrWhiteSpace(name))
                        throw new Exception("Missing element name");
                    s_LabelBindingData[type].Add(new LabelBindingData
                    {
                        ParentType = GetElementAsType(fieldType),
                        ParentName = name,
                        BindingPath = customBindingPath,
                        IsField = type.IsField(customBindingPath)
                    });
                    assembly.Add($"label=\"{label}\"");
                }
            }

            if (text != null)
                assembly.Add($"text=\"{text}\"");
            if (value != null)
                assembly.Add($"value=\"{value}\"");
            if (bindingPath != null)
                assembly.Add($"binding-path=\"{bindingPath}\"");
            if (types != null && types.Length > 0)
                assembly.Add($"type=\"{string.Join(", ", types)}\"");
            if (pickingMode != null)
                assembly.Add($"pickingMode=\"{pickingMode}\"");
            if (isReadonly)
            {
                assembly.Add(GetReadonlyString(true));
                if (classes == null)
                    classes = new[]
                    {
                        "read-only"
                    };
                else
                {
                    var classList = classes.ToList();
                    classList.Add("read-only");
                    classes = classList.ToArray();
                }
            }

            if (elidedTooltip)
                assembly.Add("display-tooltip-when-elided=\"true\"");

            if (classes != null && classes.Length > 0)
                assembly.Add($"class=\"{string.Join(' ', classes)}\"");
            if (style != null)
                assembly.Add($"style=\"{style}\"");

            uxmlWriter.WriteLine($"<{uiType.GetAsString()}:{string.Join(' ', assembly)}/>");

            if (conditional != null)
            {
                s_ConditionalFieldData[type].Add(new ConditionalData
                {
                    ParentType = GetElementAsType(fieldType),
                    ParentName = name,
                    conditionalAttribute = conditional
                });
            }
        }

        #endregion //ElementBuilder

        //Helper Functions
        //================================================================================================================//

        #region Helper Functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddInfoBox(in Type type, ref UXMLWriter writer, in InfoBoxAttribute infoBoxAttribute)
        {
            writer.WriteLine("<ui:GroupBox class=\"info-box\">");
            writer.Indent();
            ElementBuilder(type, ref writer, UITYPE.UI, "VisualElement", null, null);
            ElementBuilder(type, ref writer, UITYPE.UI, "Label", null, null, text: infoBoxAttribute.InfoText,
                elidedTooltip: true);
            writer.Outdent();
            writer.WriteLine("</ui:GroupBox>");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BeginFoldoutGroup(ref UXMLWriter writer, in string text)
        {
            writer.WriteLine($"<ui:Foldout text=\"{text}\">");
            writer.Indent();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EndFoldoutGroup(ref UXMLWriter writer)
        {
            writer.Outdent();
            writer.WriteLine("</ui:Foldout>");
        }

        private static void DisplayAsString(in Type type, ref UXMLWriter writer, in string label,
            in FieldInfo fieldInfo)
        {
            //Example of a horizontal layout group used with 2 labels
            /*
        <ui:GroupBox style="justify-content: flex-start; flex-direction: row; align-items: auto;">
            <ui:Label text="Label" display-tooltip-when-elided="true" />
            <ui:Label text="Label" display-tooltip-when-elided="true" />
        </ui:GroupBox>
     */
            writer.WriteLine("<ui:GroupBox class=\"display-as-string\">");
            writer.Indent();
            ElementBuilder(type, ref writer, UITYPE.UI, "Label", fieldInfo.Name, null, text: label,
                elidedTooltip: true);
            ElementBuilder(type, ref writer, UITYPE.UI, "Label", fieldInfo.Name, null, text: string.Empty,
                bindingPath: fieldInfo.Name, elidedTooltip: true);
            writer.Outdent();
            writer.WriteLine("</ui:GroupBox>");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetReadonlyString(in bool readOnly)
        {
            return (readOnly ? "focusable=\"false\" readonly=\"true\"" : string.Empty);
        }

        private static bool LabelIsDynamic(in string label, out string bindingPath)
        {
            bindingPath = null;

            if (label[0].Equals('$') == false)
                return false;

            bindingPath = label.Remove(0, 1);

            return true;
        }

        private static Type GetElementAsType(in string elementName)
        {
            switch (elementName)
            {
                case nameof(GroupBox):
                    return typeof(GroupBox);
                case nameof(TextField):
                    return typeof(TextField);
                case nameof(IntegerField):
                    return typeof(IntegerField);
                case nameof(ObjectField):
                    return typeof(ObjectField);
                case nameof(UnityEngine.UIElements.Button):
                    return typeof(UnityEngine.UIElements.Button);

                case nameof(Foldout):
                    return typeof(Foldout);
                case nameof(FloatField):
                    return typeof(FloatField);
                case nameof(PropertyField):
                    return typeof(PropertyField);
                case nameof(VisualElement):
                    return typeof(VisualElement);
                case nameof(Label):
                    return typeof(Label);
                case nameof(LongField):
                    return typeof(LongField);

                case nameof(Vector2Field):
                    return typeof(Vector2Field);
                case nameof(Vector3Field):
                    return typeof(Vector3Field);
                case nameof(Vector4Field):
                    return typeof(Vector4Field);

                default:
                    throw new NotImplementedException(
                        $"{elementName} has not been implemented into {nameof(GetElementAsType)}");
            }
        }
        
        #endregion //Helper Functions

        //================================================================================================================//


    }
}
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using UnityEngine.Animations;

using System.Collections.Generic;
using System.Linq;
using System;

using System.IO;
using Vector3 = UnityEngine.Vector3;

using UnityEditor.AnimatedValues;
using UnityEngine.Events;

using System.Text.RegularExpressions;



namespace GabSith.WFT
{
    public class MenuControlEditor : EditorWindow
    {
        bool avatarAvailable;
        Vector2 scrollPos;

        public VRCExpressionsMenu menu;
        public int menuIndex;

        public VRCExpressionsMenu.Control control;

        bool richTextFold = false;
        Color selectedColor = Color.white;

        List<VRCExpressionParameters.ValueType>  valueTypes = new List<VRCExpressionParameters.ValueType> { };
        int selectedParameter;

        public VRCExpressionParameters expressionParameters;
        public VRCAvatarDescriptor avatarDescriptor;

        List<string> expressionParametersNames = new List<string> { };
        List<int> subParametersInts = new List<int> { };

        bool createMode;
        string newMenuName;


        int selectedControl = 0;
        readonly string[] controlOptions = new string[6] { "Button", "Toggle", "Sub Menu", "Two Axis Puppet", "Four Axis Puppet", "Radial Puppet" };

        Color defaultColor;

        bool[] containsNewOne = new bool[5] { true, true, true, true, true };

        bool[] newParameterBools = new bool[5] { false, false, false, false, false };

        VRCExpressionParameters.Parameter newParameter = new VRCExpressionParameters.Parameter { };

        string[] selectedConvModes = new string[6] { "Expression", "FX", "Gesture", "Action", "Base", "Additive" };

        bool[] globalCreationBools = new bool[6] { true, true, false, false, false, false };

        string[] valueTypeOptions = new string[3] { "Int", "Float", "Bool" };


        private const string MenuControlFolderKey = "MenuControlFolderKey";
        private const string MenuControlUseGlobalKey = "MenuControlUseGlobalKey";
        private const string MenuControlFolderSuffixKey = "MenuControlFolderSuffixKey";
        string suffix;




        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(MenuControlEditor), false, "Menu Control Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Audio Mixer@2x").image, text = "Menu Control Editor", tooltip = "♥" };
            w.minSize = new Vector2(330, 310);
        }

        private void OnEnable()
        {
            defaultColor = GUI.color;

            suffix = ProjectSettingsManager.GetString(MenuControlFolderSuffixKey);

            if (avatarDescriptor == null)
            {
                avatarAvailable = false;
            }
            else
            {
                avatarAvailable = true;
            }
        }

        private void OnDestroy()
        {
            //MakeSureItDoesTheThing(menu);
            EditorUtility.SetDirty(menu);
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            CommonActions.GenerateTitle("Menu Control Editor");

            if (avatarDescriptor == null)
            {
                avatarAvailable = false;
            }
            else
            {
                avatarAvailable = true;
            }



            if (AssetDatabase.GetAssetPath(menu) != null)
            {
                menu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(AssetDatabase.GetAssetPath(menu));

                control = menu.controls[menuIndex];


                if (control != null)
                {
                    // Name and Icon
                    EditorGUILayout.BeginHorizontal();
                    control.name = EditorGUILayout.TextField("", control.name, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.Height(60));
                    control.icon = EditorGUILayout.ObjectField("", control.icon, typeof(Texture2D), true, GUILayout.Width(60f), GUILayout.Height(60f)) as Texture2D;
                    EditorGUILayout.EndHorizontal();


                    // Rich Text
                    GUIStyle rtLabel = new GUIStyle(GUI.skin.label)
                    {
                        richText = true
                    };
                    GUIStyle rtButton = new GUIStyle(GUI.skin.button)
                    {
                        richText = true
                    };
                    GUI.SetNextControlName("NotText");
                    richTextFold = EditorGUILayout.Foldout(richTextFold, "Rich Text");
                    if (richTextFold)
                    {
                        using (var h = new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Preview: ", GUILayout.Height(30f));
                            GUILayout.Label(control.name, rtLabel, GUILayout.Height(30f));
                        }

                        // Begin Horizontal Layout for Bold and Italic Buttons
                        EditorGUILayout.BeginHorizontal();

                        // Bold Button
                        if (GUILayout.Button("<b>Bold</b>", rtButton))
                        {
                            control.name = ToggleRichTextTag(control.name, "<b>", "</b>");
                            GUI.FocusControl("NotText");
                        }

                        // Italic Button
                        if (GUILayout.Button("<i>Italic</i>", rtButton))
                        {
                            control.name = ToggleRichTextTag(control.name, "<i>", "</i>");
                            GUI.FocusControl("NotText");
                        }

                        // Color Button
                        if (GUILayout.Button("Color"))
                        {
                            control.name = ToggleColorTag(control.name, selectedColor, true);
                            GUI.FocusControl("NotText");
                        }

                        EditorGUILayout.EndHorizontal();

                        // Color Selection
                        EditorGUI.BeginChangeCheck();
                        selectedColor = EditorGUILayout.ColorField(new GUIContent("Text Color"), selectedColor, true, false, false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            control.name = ToggleColorTag(control.name, selectedColor);
                            GUI.FocusControl("NotText");
                        }

                    }

                    EditorGUILayout.Space(20f);

                    //control.icon = EditorGUILayout.ObjectField("Menu Icon", control.icon, typeof(Texture2D), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;

                    EditorGUI.BeginChangeCheck();
                    selectedControl = GetIntFromControl(control.type);
                    selectedControl = EditorGUILayout.Popup("Type: ", selectedControl, controlOptions);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //Debug.Log(controlOptions[selectedControl]);
                    }
                    control.type = GetControlFromInt(selectedControl);

                    EditorGUILayout.Space();
                    
                    EditorGUILayout.BeginHorizontal();

                    control.parameter.name = EditorGUILayout.TextField("Parameter: ", control.parameter.name);
                    if (avatarAvailable)
                    {
                        EditorGUI.BeginChangeCheck();
                        int valParam = ParameterList("", control.parameter.name, CreateParameterList(expressionParameters), 0);
                        selectedParameter = valParam;


                        List<string> parameterNames = new List<string> { };

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (valParam != 0)
                            {
                                string parameterName = CreateParameterList(expressionParameters)[valParam - 1];
                                control.parameter.name = parameterName;

                            }
                            else
                                control.parameter.name = "";
                        }


                        // New Parameter
                        NewParameter(0, ref control.parameter.name);

                    }
                    else
                    {
                        EditorGUILayout.EndHorizontal();
                    }


                    if (avatarAvailable && valueTypes.Count != expressionParameters.parameters.Length)
                    {
                        if (valueTypes[selectedParameter] == VRCExpressionParameters.ValueType.Int)
                        {
                            control.value = EditorGUILayout.IntField("Value", (int)control.value);
                        }
                        else if (valueTypes[selectedParameter] == VRCExpressionParameters.ValueType.Float)
                        {
                            //control.value = EditorGUILayout.FloatField("Value", control.value);
                            control.value = EditorGUILayout.Slider("Value", control.value, -1, 1);
                        }
                    }



                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    {

                        EditorGUILayout.BeginHorizontal();

                        control.subMenu = EditorGUILayout.ObjectField("Sub Menu", control.subMenu, typeof(VRCExpressionsMenu), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as VRCExpressionsMenu;

                        // OPTION TO CREATE SUBMENU
                        if (CommonActions.ToggleButton("Create", createMode, GUILayout.Width(100)))
                        {
                            createMode = !createMode;
                        }






                        EditorGUILayout.EndHorizontal();

                        if (createMode)
                        {
                            EditorGUILayout.Space();

                            newMenuName = EditorGUILayout.TextField("Menu Name: ", newMenuName);


                            CommonActions.SelectFolder(MenuControlUseGlobalKey, MenuControlFolderKey, MenuControlFolderSuffixKey, ref suffix);

                            if (string.IsNullOrEmpty(newMenuName))
                                GUI.enabled = false;

                            if (GUILayout.Button("Create", GUILayout.Height(30f)))
                            {
                                control.subMenu = CreateExpressionMenu(newMenuName);
                            }
                            GUI.enabled = true;

                        }



                    }
                    else if (control.type == VRCExpressionsMenu.Control.ControlType.RadialPuppet)
                    {
                        //Debug.Log(control.subParameters.Length);
                        //Debug.Log(control.subParameters[0].name);

                        if (control.subParameters == null || control.subParameters.Length == 0)
                        {
                            control.subParameters = new VRCExpressionsMenu.Control.Parameter[1] { new VRCExpressionsMenu.Control.Parameter { } };
                        }


                        {
                            EditorGUILayout.BeginHorizontal();


                            control.subParameters[0].name = EditorGUILayout.TextField("Parameter Rotation: ", control.subParameters[0].name);
                            if (avatarAvailable)
                            {
                                EditorGUI.BeginChangeCheck();
                                int val = ParameterList("", control.subParameters[0].name, CreateParameterList(expressionParameters, true, true), 1);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (val != 0)
                                        control.subParameters[0].name = CreateParameterList(expressionParameters, true, true)[val - 1];
                                    else
                                        control.subParameters[0].name = "";
                                }

                                // New Parameter
                                NewParameter(1, ref control.subParameters[0].name);

                            }
                            else
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                        }

                    }
                    else if (control.type == VRCExpressionsMenu.Control.ControlType.Toggle || control.type == VRCExpressionsMenu.Control.ControlType.Button)
                    {
                        //Debug.Log(valueTypes.Count);
                        /*
                        if (valueTypes.Count != expressionParameters.parameters.Length)
                        {
                            if (valueTypes[selectedParameter] == VRCExpressionParameters.ValueType.Int)
                            {
                                control.value = EditorGUILayout.IntField("Value", (int)control.value);
                            }
                            else if (valueTypes[selectedParameter] == VRCExpressionParameters.ValueType.Float)
                            {
                                //control.value = EditorGUILayout.FloatField("Value", control.value);
                                control.value = EditorGUILayout.Slider("Value", control.value, 0, 1);
                            }
                        }*/
                    }
                    else if (control.type == VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet)
                    {

                        if (control.subParameters.Length < 2)
                        {
                            control.subParameters = new VRCExpressionsMenu.Control.Parameter[4] { new VRCExpressionsMenu.Control.Parameter { },
                                new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { } };
                        }

                        EditorGUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        control.subParameters[0].name = EditorGUILayout.TextField("Parameter Horizontal: ", control.subParameters[0].name);
                        if (avatarAvailable)
                        {
                            EditorGUI.BeginChangeCheck();
                            int val = ParameterList("", control.subParameters[0].name, CreateParameterList(expressionParameters, true, false), 1);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (val != 0)
                                    control.subParameters[0].name = CreateParameterList(expressionParameters, true, false)[val - 1];
                                else
                                    control.subParameters[0].name = "";
                            }
                            // New Parameter
                            NewParameter(1, ref control.subParameters[0].name);
                        }
                        else
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.BeginHorizontal();
                        control.subParameters[1].name = EditorGUILayout.TextField("Parameter Vertical: ", control.subParameters[1].name);
                        if (avatarAvailable)
                        {
                            EditorGUI.BeginChangeCheck();
                            int val1 = ParameterList("", control.subParameters[1].name, CreateParameterList(expressionParameters, true, false), 2);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (val1 != 0)
                                    control.subParameters[1].name = CreateParameterList(expressionParameters, true, false)[val1 - 1];
                                else
                                    control.subParameters[1].name = "";
                            }

                            // New Parameter
                            NewParameter(2, ref control.subParameters[1].name);
                        }
                        else
                        {
                            EditorGUILayout.EndHorizontal();
                        }

                    }

                    else if (control.type == VRCExpressionsMenu.Control.ControlType.FourAxisPuppet)
                    {
                        if (control.subParameters.Length < 4)
                        {
                            control.subParameters = new VRCExpressionsMenu.Control.Parameter[4] { new VRCExpressionsMenu.Control.Parameter { },
                                new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { } };
                        }

                        EditorGUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        control.subParameters[0].name = EditorGUILayout.TextField("Parameter Up: ", control.subParameters[0].name);
                        if (avatarAvailable)
                        {
                            EditorGUI.BeginChangeCheck();
                            int val = ParameterList("", control.subParameters[0].name, CreateParameterList(expressionParameters, true, false), 1);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (val != 0)
                                    control.subParameters[0].name = CreateParameterList(expressionParameters, true, false)[val - 1];
                                else
                                    control.subParameters[0].name = "";
                            }


                            // New Parameter
                            NewParameter(1, ref control.subParameters[0].name);

                        }
                        else
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.BeginHorizontal();
                        control.subParameters[1].name = EditorGUILayout.TextField("Parameter Right: ", control.subParameters[1].name);
                        if (avatarAvailable)
                        {
                            EditorGUI.BeginChangeCheck();
                            int val1 = ParameterList("", control.subParameters[1].name, CreateParameterList(expressionParameters, true, false), 2);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (val1 != 0)
                                    control.subParameters[1].name = CreateParameterList(expressionParameters, true, false)[val1 - 1];
                                else
                                    control.subParameters[1].name = "";
                            }

                            // New Parameter
                            NewParameter(2, ref control.subParameters[1].name);
                        }
                        else
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.BeginHorizontal();
                        control.subParameters[2].name = EditorGUILayout.TextField("Parameter Down: ", control.subParameters[2].name);
                        if (avatarAvailable)
                        {
                            EditorGUI.BeginChangeCheck();
                            int val2 = ParameterList("", control.subParameters[2].name, CreateParameterList(expressionParameters, true, false), 3);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (val2 != 0)
                                    control.subParameters[2].name = CreateParameterList(expressionParameters, true, false)[val2 - 1];
                                else
                                    control.subParameters[2].name = "";
                            }


                            // New Parameter
                            NewParameter(3, ref control.subParameters[2].name);

                        }
                        else
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.BeginHorizontal();
                        control.subParameters[3].name = EditorGUILayout.TextField("Parameter Left: ", control.subParameters[3].name);
                        if (avatarAvailable)
                        {
                            EditorGUI.BeginChangeCheck();
                            int val3 = ParameterList("", control.subParameters[3].name, CreateParameterList(expressionParameters, true, false), 4);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (val3 != 0)
                                    control.subParameters[3].name = CreateParameterList(expressionParameters, true, false)[val3 - 1];
                                else
                                    control.subParameters[3].name = "";
                            }


                            // New Parameter
                            NewParameter(4, ref control.subParameters[3].name);
                        }
                        else
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                }
                else
                {
                    Debug.LogError("Control is null");
                }
            }


            if (!avatarAvailable)
            {
                EditorGUILayout.HelpBox("No avatar was selected, so parameters can't be listed or added", MessageType.Info);
            }

            EditorGUILayout.Space(25);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        string ToggleRichTextTag(string text, string openTag, string closeTag)
        {
            if (text.Contains(openTag) && text.Contains(closeTag))
            {
                text = text.Replace(openTag, "").Replace(closeTag, "");
            }
            else
            {
                text = openTag + text + closeTag;
            }
            return text;
        }

        string ToggleColorTag(string text, Color color, bool toogle = false)
        {
            string colorHex = ColorUtility.ToHtmlStringRGBA(color);
            string openTag = $"<color=#{colorHex}>";
            string closeTag = "</color>";

            // Regex pattern to find existing color tags
            string pattern = @"<color=#\w{6,8}>(.*?)<\/color>";

            if (Regex.IsMatch(text, pattern))
            {
                if (toogle)
                {
                    text = Regex.Replace(text, pattern, $"$1");
                    return text;
                }

                // Replace the existing color tag with the new one
                text = Regex.Replace(text, pattern, $"{openTag}$1{closeTag}");
            }
            else
            {
                // Add new color tag
                text = openTag + text + closeTag;
            }

            return text;
        }
        /*
        string ToggleColorTag(string text, Color color, bool remove = false)
        {
            string colorHex = ColorUtility.ToHtmlStringRGBA(color);
            string openTag = $"<color=#{colorHex}>";
            string closeTag = "</color>";

            // Check if there is an existing color tag
            int colorTagStart = text.IndexOf("<color=");
            int colorTagEnd = text.IndexOf("</color>");


            if (colorTagStart != -1 && colorTagEnd != -1)
            {
                if (remove)
                {
                    // Remove the existing color tag
                    string beforeColor1 = text.Substring(0, colorTagStart);
                    string insideColor1 = text.Substring(colorTagStart, colorTagEnd - colorTagStart + closeTag.Length);
                    string afterColor1 = text.Substring(colorTagEnd + closeTag.Length);

                    text = beforeColor1 + insideColor1.Substring(insideColor1.IndexOf(">") + 1) + afterColor1;
                    return text;
                }


                // Remove the existing color tag
                string beforeColor = text.Substring(0, colorTagStart);
                string insideColor = text.Substring(colorTagStart, colorTagEnd - colorTagStart + closeTag.Length);
                string afterColor = text.Substring(colorTagEnd + closeTag.Length);

                // Replace existing color tag with the new one
                text = beforeColor + openTag + insideColor.Substring(insideColor.IndexOf(">") + 1) + afterColor;
            }
            else
            {
                // Add new color tag
                text = openTag + text + closeTag;
            }


            return text;
        }
        */


        VRCExpressionsMenu.Control.ControlType GetControlFromInt(int controlIndex)
        {
            if (controlIndex == 0)
            {
                return VRCExpressionsMenu.Control.ControlType.Button;
            }
            else if (controlIndex == 1)
            {
                return VRCExpressionsMenu.Control.ControlType.Toggle;
            }
            else if (controlIndex == 2)
            {
                return VRCExpressionsMenu.Control.ControlType.SubMenu;
            }
            else if (controlIndex == 3)
            {
                return VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet;
            }
            else if (controlIndex == 4)
            {
                return VRCExpressionsMenu.Control.ControlType.FourAxisPuppet;
            }
            else
            {
                return VRCExpressionsMenu.Control.ControlType.RadialPuppet;
            }

            //return VRCExpressionsMenu.Control.ControlType.Button;
        }
        int GetIntFromControl(VRCExpressionsMenu.Control.ControlType controlType)
        {
            if (controlType == VRCExpressionsMenu.Control.ControlType.Button)
            {
                return 0;
            }
            else if (controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
            {
                return 1;
            }
            else if (controlType == VRCExpressionsMenu.Control.ControlType.SubMenu)
            {
                return 2;
            }
            else if (controlType == VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet)
            {
                return 3;
            }
            else if (controlType == VRCExpressionsMenu.Control.ControlType.FourAxisPuppet)
            {
                return 4;
            }
            else
            {
                return 5;
            }

        }



        bool CreateGlobal()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                for (int i = 0; i < selectedConvModes.Length; i++)
                {
                    globalCreationBools[i] = EditorGUILayout.ToggleLeft(selectedConvModes[i], globalCreationBools[i], GUILayout.Width(Screen.width / 6f - 11f));
                }
                globalCreationBools[0] = true;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                // Header
                {
                    EditorGUILayout.BeginHorizontal("box");
                    // Name
                    GUILayout.Label("Name");
                    GUILayout.Space(10);

                    // Value Type
                    GUILayout.Label("Value Type", GUILayout.Width(70));
                    GUILayout.Space(10);

                    // Default
                    GUILayout.Label("Default", GUILayout.Width(50));
                    GUILayout.Space(10);

                    // Saved
                    GUILayout.Label("Saved", GUILayout.Width(50));

                    // Synced
                    GUILayout.Label("Synced", GUILayout.Width(50));

                    EditorGUILayout.EndHorizontal();
                }

                // New Parameter

                EditorGUILayout.BeginHorizontal("box");

                // Name
                newParameter.name = EditorGUILayout.TextField(newParameter.name);
                GUILayout.Space(10);

                // Value Type
                int valueTypeSelectedIndex = ValueToIndex(newParameter.valueType);
                valueTypeSelectedIndex = EditorGUILayout.Popup("", valueTypeSelectedIndex, valueTypeOptions, GUILayout.Width(70));
                newParameter.valueType = IndexToValue(valueTypeSelectedIndex);
                GUILayout.Space(10);

                // Default
                switch (newParameter.valueType)
                {
                    case VRCExpressionParameters.ValueType.Int:
                        newParameter.defaultValue = EditorGUILayout.IntField(Convert.ToInt32(newParameter.defaultValue), GUILayout.Width(50));
                        break;
                    case VRCExpressionParameters.ValueType.Float:
                        newParameter.defaultValue = EditorGUILayout.FloatField(newParameter.defaultValue, GUILayout.Width(50));
                        break;
                    case VRCExpressionParameters.ValueType.Bool:
                        newParameter.defaultValue = Convert.ToSingle(EditorGUILayout.Toggle(Convert.ToBoolean(newParameter.defaultValue), GUILayout.Width(50)));
                        break;
                    default:
                        break;
                }
                GUILayout.Space(10);

                // Saved
                newParameter.saved = EditorGUILayout.Toggle(newParameter.saved, GUILayout.Width(50));

                // Synced
                newParameter.networkSynced = EditorGUILayout.Toggle(newParameter.networkSynced, GUILayout.Width(50));


                EditorGUILayout.EndHorizontal();

                // Check if it exists
                List<string> parameterNames = new List<string> { };
                if (globalCreationBools[0]) // Expression Selected
                {
                    foreach (var item in avatarDescriptor.expressionParameters.parameters)
                    {
                        parameterNames.Add(item.name);
                    }
                }
                if (globalCreationBools[1]) // FX Selected
                {
                    RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(1)].animatorController;

                    if (controllerRuntime != null)
                    {
                        foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                        {
                            parameterNames.Add(item.name);
                        }
                    }
                }
                if (globalCreationBools[2]) // Gesture Selected
                {
                    RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(2)].animatorController;

                    if (controllerRuntime != null)
                    {
                        foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                        {
                            parameterNames.Add(item.name);
                        }
                    }
                }
                if (globalCreationBools[3]) // Action Selected
                {
                    RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(3)].animatorController;

                    if (controllerRuntime != null)
                    {
                        foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                        {
                            parameterNames.Add(item.name);
                        }
                    }
                }
                if (globalCreationBools[4]) // Base Selected
                {
                    RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(4)].animatorController;

                    if (controllerRuntime != null)
                    {
                        foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                        {
                            parameterNames.Add(item.name);
                        }
                    }
                }
                if (globalCreationBools[5]) // Add Selected
                {
                    RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(5)].animatorController;

                    if (controllerRuntime != null)
                    {
                        foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                        {
                            parameterNames.Add(item.name);
                        }
                    }
                }

                if (parameterNames.Contains(newParameter.name))
                {
                    EditorGUILayout.HelpBox(newParameter.name + " already exists! It'll be replaced", MessageType.Warning);
                }


                if (GUILayout.Button("Add to selected"))
                {
                    List<UnityEngine.Object> objectsToCheck = new List<UnityEngine.Object> { avatarDescriptor.expressionParameters,
                avatarDescriptor.baseAnimationLayers[0].animatorController,
                avatarDescriptor.baseAnimationLayers[1].animatorController,
                avatarDescriptor.baseAnimationLayers[2].animatorController,
                avatarDescriptor.baseAnimationLayers[3].animatorController,
                avatarDescriptor.baseAnimationLayers[4].animatorController};

                    objectsToCheck.RemoveAll(item => item == null);

                    Undo.RecordObjects(objectsToCheck.ToArray(), "Global Parameter");


                    if (globalCreationBools[0]) // Expressions Parameters Selected
                    {
                        VRCExpressionParameters.Parameter copy = new VRCExpressionParameters.Parameter
                        {
                            name = newParameter.name,
                            defaultValue = newParameter.defaultValue,
                            networkSynced = newParameter.networkSynced,
                            saved = newParameter.saved,
                            valueType = newParameter.valueType
                        };

                        List<VRCExpressionParameters.Parameter> items = new List<VRCExpressionParameters.Parameter>();
                        foreach (var item in avatarDescriptor.expressionParameters.parameters)
                        {
                            items.Add(item);
                        }

                        // Remove the parameter if it exists
                        for (int i = 0; i < items.Count; i++)
                        {
                            if (items[i].name == copy.name)
                            {
                                items.RemoveAt(i);
                            }
                        }

                        items.Add(copy);
                        avatarDescriptor.expressionParameters.parameters = items.ToArray();
                    }

                    if (globalCreationBools[1]) // FX Selected
                    {
                        GlobalControllerAdd(4, valueTypeSelectedIndex);
                    }
                    if (globalCreationBools[2]) // Gesture Selected
                    {
                        GlobalControllerAdd(2, valueTypeSelectedIndex);
                    }
                    if (globalCreationBools[3]) // Action Selected
                    {
                        GlobalControllerAdd(3, valueTypeSelectedIndex);
                    }
                    if (globalCreationBools[4]) // Base Selected
                    {
                        GlobalControllerAdd(0, valueTypeSelectedIndex);
                    }
                    if (globalCreationBools[5]) // Add Selected
                    {
                        GlobalControllerAdd(1, valueTypeSelectedIndex);
                    }

                    return true;

                }
                return false;


            }
        }

        int SelectedParameterMapping(int selectedParameter)
        {
            switch (selectedParameter)
            {
                case 0:
                    break;
                case 1: // FX
                    return 4;
                case 2: // Gesture
                    return 2;
                case 3: // Action
                    return 3;
                case 4: // Base
                    return 0;
                case 5: // Additive
                    return 1;
                default:
                    break;
            }

            return 4;
        }

        void NewParameter(int boolsIndex, ref string subParameter)
        {

            if (CommonActions.ToggleButton("New", newParameterBools[boolsIndex], GUILayout.Width(50)))
            {
                newParameterBools[boolsIndex] = !newParameterBools[boolsIndex];
            }

            EditorGUILayout.EndHorizontal();

            if (newParameterBools[boolsIndex])
            {
                if (CreateGlobal())
                {
                    subParameter = expressionParameters.parameters.Last().name;
                }
                EditorGUILayout.Space();
            }

            // Not Found Warning
            if (!containsNewOne[boolsIndex])
            {
                EditorGUILayout.HelpBox(subParameter + " was not found in the expression parameters", MessageType.Warning);
            }

        }


        void GlobalControllerAdd(int controllerIndex, int valueTypeSelectedIndex)
        {
            if (avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController)
            {
                AnimatorControllerParameter newControllerParameter = new AnimatorControllerParameter
                {
                    name = newParameter.name,
                    defaultBool = Convert.ToBoolean(newParameter.defaultValue),
                    defaultFloat = newParameter.defaultValue,
                    defaultInt = Convert.ToInt32(newParameter.defaultValue),
                    type = IndexToType(valueTypeSelectedIndex)
                };

                List<AnimatorControllerParameter> itemsController = new List<AnimatorControllerParameter>();

                AnimatorController animatorController = (AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController;
                foreach (var item in animatorController.parameters)
                {
                    itemsController.Add(item);
                }

                // Remove the parameter if it exists
                for (int i = 0; i < itemsController.Count; i++)
                {
                    if (itemsController[i].name == newControllerParameter.name)
                    {
                        itemsController.RemoveAt(i);
                    }
                }

                itemsController.Add(newControllerParameter);
                ((AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController).parameters = itemsController.ToArray();
            }
            else
            {
                Debug.Log("The selected animator does not exist");
            }
        }

        int TypeToIndex(AnimatorControllerParameterType type)
        {
            switch (type)
            {
                case AnimatorControllerParameterType.Int:
                    return 0;
                case AnimatorControllerParameterType.Float:
                    return 1;
                case AnimatorControllerParameterType.Bool:
                    return 2;
                case AnimatorControllerParameterType.Trigger:
                    return 2;
                default:
                    return 2;
            }
        }

        AnimatorControllerParameterType IndexToType(int index)
        {
            switch (index)
            {
                case 0:
                    return AnimatorControllerParameterType.Int;
                case 1:
                    return AnimatorControllerParameterType.Float;
                case 2:
                    return AnimatorControllerParameterType.Bool;
                default:
                    return AnimatorControllerParameterType.Bool;
            }

        }


        int ValueToIndex(VRCExpressionParameters.ValueType valueType)
        {
            switch (valueType)
            {
                case VRCExpressionParameters.ValueType.Int:
                    return 0;
                case VRCExpressionParameters.ValueType.Float:
                    return 1;
                case VRCExpressionParameters.ValueType.Bool:
                    return 2;
                default:
                    return 2;
            }
        }

        public VRCExpressionParameters.ValueType IndexToValue(int index)
        {
            switch (index)
            {
                case 0:
                    return VRCExpressionParameters.ValueType.Int;
                case 1:
                    return VRCExpressionParameters.ValueType.Float;
                case 2:
                    return VRCExpressionParameters.ValueType.Bool;
                default:
                    return VRCExpressionParameters.ValueType.Bool;
            }

        }

        void ParameterList(string label, string parameterName, bool noBools = false)
        {

            EditorGUI.BeginChangeCheck();
            int val = ParameterList(label, parameterName, CreateParameterList(expressionParameters, noBools));
            if (EditorGUI.EndChangeCheck())
            {
                if (val != 0)
                    parameterName = CreateParameterList(expressionParameters, noBools)[val - 1];
                else
                    parameterName = "";
            }
        }

        VRCExpressionsMenu CreateExpressionMenu(string name)
        {
            VRCExpressionsMenu expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(expressionsMenu, GetFolder() + "/" + name + ".asset");

            MakeSureItDoesTheThing(expressionsMenu);

            EditorGUIUtility.PingObject(expressionsMenu);


            return expressionsMenu;
        }

        List<string> CreateParameterList(VRCExpressionParameters expressionParameters, bool noBools = false, bool noInts = false)
        {
            List<string> expressionParametersNames = new List<string> { };
            valueTypes = new List<VRCExpressionParameters.ValueType> { };


            if (noInts && !noBools)
            {
                foreach (var parameter in expressionParameters.parameters)
                {
                    if (parameter.valueType != VRCExpressionParameters.ValueType.Int)
                    {
                        expressionParametersNames.Add(parameter.name);
                        valueTypes.Add(parameter.valueType);
                    }
                }
            }

            else if (noBools && !noInts)
            {
                foreach (var parameter in expressionParameters.parameters)
                {
                    if (parameter.valueType != VRCExpressionParameters.ValueType.Bool)
                    {
                        expressionParametersNames.Add(parameter.name);
                        valueTypes.Add(parameter.valueType);
                    }
                }
            }

            else if (noBools && noInts)
            {
                foreach (var parameter in expressionParameters.parameters)
                {
                    if ((parameter.valueType != VRCExpressionParameters.ValueType.Bool) && (parameter.valueType != VRCExpressionParameters.ValueType.Int))
                    {
                        expressionParametersNames.Add(parameter.name);
                        valueTypes.Add(parameter.valueType);
                    }
                }
            }

            else
            {
                foreach (var parameter in expressionParameters.parameters)
                {
                    //expressionParametersNames.Add(parameter.name + " [" + parameter.valueType + "]");
                    expressionParametersNames.Add(parameter.name);

                    valueTypes.Add(parameter.valueType);

                }
            }


            return expressionParametersNames;
        }

        int ParameterList(string parameterType, string currentValue, List<string> list, int parameterOrSubIndex = 69)
        {
            list.Insert(0, "[None]");
            int selectedElement = 0;

            if (list.Contains(currentValue))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == currentValue)
                    {
                        selectedElement = i;
                    }
                }

                if (parameterOrSubIndex != 69)
                    containsNewOne[parameterOrSubIndex] = true;
            }
            else if (parameterOrSubIndex != 69)
            {
                if (!string.IsNullOrEmpty(currentValue))
                    containsNewOne[parameterOrSubIndex] = false;
                else
                    containsNewOne[parameterOrSubIndex] = true;
            }

            List<string> listWithType = new List<string> { };
            //string[] listWithType;

            valueTypes.Insert(0, VRCExpressionParameters.ValueType.Bool);
            for (int i = 0; i < list.Count; i++)
            {
                if (i == 0)
                    listWithType.Add(list[i]);
                else
                    listWithType.Add(list[i] + " [" + valueTypes[i] + "]");

            }

            selectedElement = EditorGUILayout.Popup(parameterType, selectedElement, listWithType.ToArray(), GUILayout.Width(100));


            return selectedElement;
        }

        string GetFolder()
        {
            return CommonActions.GetFolder(MenuControlUseGlobalKey, MenuControlFolderKey) + "/" + suffix;
        }

        void MakeSureItDoesTheThing(UnityEngine.Object dirtyBoy = null)
        {
            if (dirtyBoy != null)
                EditorUtility.SetDirty(dirtyBoy);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }
}

    #endif
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




namespace GabSith.WFT
{
    public class HueShiftEditor : EditorWindow
    {
        Editor gameObjectEditor;

        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        VRCAvatarDescriptor avatarDescriptor;
        private Renderer gameObject;
        private List<Renderer> extraGameObjects = new List<Renderer> { };

        private List<Material> selectedMaterials = new List<Material> { };

        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;
        AnimatorController FXLayer;
        Texture2D icon;
        AnimationClip existingAnimation;

        string toggleName;
        string animationName;
        string parameterName;

        bool saved = true;
        float defaultState = 0;
        float defaultStateFloat;
        bool useWriteDefaults = false;

        bool useExistingAnimation = false;

        bool fold = false;
        bool advancedFold = false;
        Vector2 scrollPosExtraObjects;
        Vector2 scrollPosDescriptors;
        Vector2 scrollPosMaterials;



        //private string defaultPath = "Assets/WF Tools - GabSith/Generated";
        //private string folderPath = "Assets/WF Tools - GabSith/Generated";

        private const string HueShiftFolderKey = "HueShiftFolderKey";
        private const string HueShiftUseGlobalKey = "HueShiftUseGlobalKey";
        //private const string GlobalFolderKey = "GlobalFolderKey";

        [MenuItem("GabSith/Hue Shift Creator", false, 50)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(HueShiftEditor), false, "Hue Shift Creator");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_ColorPicker.CycleSlider").image, text = "Hue Shift Creator", tooltip = "♥" };

        }



        private void OnEnable()
        {
            if (avatarDescriptor == null)
            {
                //avatarDescriptor = SceneAsset.FindObjectOfType<VRCAvatarDescriptor>();

                CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

                if (avatarDescriptorsFromScene.Length == 1)
                {
                    avatarDescriptor = avatarDescriptorsFromScene[0];
                }
            }


        }

        void OnGUI()
        {
            // Use a vertical layout group to organize the fields
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Hue Shift Creator");

            //RefreshDescriptors();


            // Avatar Selection
            CommonActions.FindAvatars(ref avatarDescriptor, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);


            if (useExistingAnimation)
            {
                GUI.enabled = false;
                gameObject = null;
                extraGameObjects.Clear();
            }

            scrollPosExtraObjects = EditorGUILayout.BeginScrollView(scrollPosExtraObjects, GUILayout.ExpandHeight(false));

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("+", GUILayout.Width(20f)))
            {
                extraGameObjects.Add(null);
            }

            EditorGUILayout.LabelField("Object to Hue Shift", GUILayout.MaxWidth(125f));

            gameObject = (Renderer)EditorGUILayout.ObjectField("", gameObject, typeof(Renderer), true, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                if ((gameObject != null && avatarDescriptor != null) && (!gameObject.transform.IsChildOf(avatarDescriptor.transform) || gameObject.transform == avatarDescriptor.transform))
                {
                    gameObject = null;
                }

                toggleName = gameObject != null ? gameObject.name + " Hue Shift" : null ?? "";

                if (gameObject != null)
                {
                    animationName = GetPathToObject(gameObject.transform).Replace("/", " ") + " Hue Shift";
                }
                parameterName = toggleName;
            }
            EditorGUILayout.EndHorizontal();



            // Extra objects to toggle
            if (extraGameObjects != null && extraGameObjects.Count > 0)
            {
                for (int i = 0; i < extraGameObjects.Count; i++)
                {
                    if ((extraGameObjects[i] != null && avatarDescriptor != null) && ((!extraGameObjects[i].transform.IsChildOf(avatarDescriptor.transform)) || (extraGameObjects[i].transform == avatarDescriptor.transform)))
                    {
                        extraGameObjects[i] = null;
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("-", GUILayout.Width(20f)))
                    {
                        extraGameObjects.RemoveAt(i);
                        continue;
                    }
                    EditorGUILayout.LabelField("Object to Hue Shift #" + (i+2), GUILayout.MaxWidth(125f));
                    extraGameObjects[i] = (Renderer)EditorGUILayout.ObjectField("", extraGameObjects[i], typeof(Renderer), true, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                    /*
                    if (extraGameObjects[i] != null && extraGameObjects[i].sharedMaterials.Length > 1)
                    {
                        scrollPosMaterials = EditorGUILayout.BeginScrollView(scrollPosMaterials, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                        EditorGUILayout.BeginHorizontal();
                        foreach (var item in extraGameObjects[i].sharedMaterials)
                        {
                            if (item != null)
                            {
                                if (selectedMaterials.Contains(item))
                                {
                                    Color def = GUI.backgroundColor;
                                    GUI.backgroundColor = new Color { r = 0.3f, g = 1f, b = 0.3f, a = 1 };

                                    if (GUILayout.Button(item.name))
                                    {
                                        selectedMaterials.Remove(item);
                                    }

                                    GUI.backgroundColor = def;
                                }
                                else
                                {
                                    if (GUILayout.Button(item.name))
                                    {
                                        selectedMaterials.Add(item);
                                    }
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.Space();
                    }

                    */
                }
            }

            EditorGUILayout.EndScrollView();

            GUI.enabled = true;


            menu = (VRCExpressionsMenu)EditorGUILayout.ObjectField("Menu", menu, typeof(VRCExpressionsMenu), true);


            if (avatarDescriptor != null)
            {
                FXLayer = (AnimatorController)avatarDescriptor.baseAnimationLayers[4].animatorController;
                parameters = avatarDescriptor.expressionParameters;
                if (menu == null)
                    menu = avatarDescriptor.expressionsMenu;
            }


            EditorGUILayout.Space();

            // Use a text field to enter the toggle name
            toggleName = EditorGUILayout.TextField("Hue Shift Name", toggleName);

            EditorGUILayout.Space();

            // Use toggle fields to set the saved and default states with a custom style
            GUIStyle toggleStyle = new GUIStyle(EditorStyles.toggle)
            {
                fontSize = 15 // Increase the font size
            };
            saved = EditorGUILayout.Toggle("Saved", saved, toggleStyle);
            //defaultState = EditorGUILayout.FloatField("Default value", Mathf.Clamp(defaultState, 0, 1));
            defaultState = EditorGUILayout.Slider("Default Value", defaultState, 0, 1);


            defaultStateFloat = defaultState;

            EditorGUILayout.Space();



            // Crate fold for advanced settings
            advancedFold = EditorGUILayout.Foldout(advancedFold, "More Settings");

            if (advancedFold) {

                icon = EditorGUILayout.ObjectField("Menu Icon", icon, typeof(Texture2D), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;

                useWriteDefaults = EditorGUILayout.Toggle("Use Write Defaults", useWriteDefaults, toggleStyle);

                useExistingAnimation = EditorGUILayout.Toggle("Use Existing Animation", useExistingAnimation, toggleStyle);

                if (useExistingAnimation)
                {
                    existingAnimation = (AnimationClip)EditorGUILayout.ObjectField("Existing Animation Clip", existingAnimation, typeof(AnimationClip), true);
                }

                // Use a space to separate the fields
                EditorGUILayout.Space();

                // Use a text field for the animation name
                if (useExistingAnimation)
                    GUI.enabled = false;
                animationName = EditorGUILayout.TextField("Animation Name", animationName);
                GUI.enabled = true;

                if ((animationName == null || animationName == "") && gameObject != null && avatarDescriptor != null) {
                    animationName = GetPathToObject(gameObject.transform).Replace("/", " ") + " Hue Shift";
                }
                if (existingAnimation != null && useExistingAnimation)
                {
                    animationName = existingAnimation.name;
                }

                // Use a text field for the parameter name
                parameterName = EditorGUILayout.TextField("Parameter Name", parameterName);

                if (parameterName == null || parameterName == "") {
                    parameterName = toggleName;
                }
            }
            else
            {
                if (gameObject != null && avatarDescriptor != null)
                    animationName = GetPathToObject(gameObject.transform).Replace("/", " ") + " Hue Shift";
                parameterName = toggleName;
            }

            // Use a space to separate the fields
            EditorGUILayout.Space();



            if (useExistingAnimation)
            {
                GUI.enabled = false;
            }


            CommonActions.SelectFolder(HueShiftUseGlobalKey, HueShiftFolderKey);


            // Use a space to separate the fields
            EditorGUILayout.Space();

            // Use a conditional statement to check if the requirements are met
            if (!RequirementsMet())
                GUI.enabled = false;


            // Warning
            EditorGUILayout.HelpBox("This script only creates the parameter, layer, animation and menu control. The material still needs to be manually set up to function", MessageType.Warning);


            // Custom style for the Create Toggle button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15, // Increase the font size
                fixedHeight = 35,
                
            };
            // Use buttons to create the toggle and the animation clip with a custom style
            buttonStyle.fixedHeight = 35; // Increase the button height

            if (GUILayout.Button("Create Hue Shift", buttonStyle))
            {
                CreateToggle(toggleName, parameterName, animationName, saved, defaultStateFloat, menu, parameters, icon);
            }

            // Use a space to separate the fields
            EditorGUILayout.Space();
            GUI.enabled = true;
            // Use a fold to separate the extra buttons
            fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, "Debug");
            if (fold)
            {
                if (GUILayout.Button("Create Animation Clip"))
                {
                    CreateAnimations(animationName);
                }

                if (GUILayout.Button("Create Menu Control"))
                {
                    CreateMenu(toggleName, parameterName, menu, icon);
                }

                if (GUILayout.Button("Test Path To Object"))
                {
                    //Debug.Log(VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject.transform, avatarDescriptor.transform));

                    Debug.Log(GetPathToObject(gameObject.transform));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Use a space to separate the fields
            EditorGUILayout.Space();

            // End the vertical layout group
            EditorGUILayout.EndVertical();

            // Use a space to separate the preview
            EditorGUILayout.Space(10);

        }



        string GetPathToObject(Transform gameObject)
        {
            /*
            //string objectPath = gameObject.GetHierarchyPath();
            string objectPath = VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject);
            string avatarName = "";
            if (avatarDescriptor != null)
                avatarName = avatarDescriptor.name;
            int index = objectPath.IndexOf(avatarName);
            string localObjectPath = objectPath.Substring(index + avatarName.Length + 1);


            return localObjectPath;*/
            return (VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject, avatarDescriptor.transform));
        }

        

        void CreateToggle(string name, string parameterName, string animationName, bool saved, float defaultFloatState, VRCExpressionsMenu menu, VRCExpressionParameters expressionParameters, Texture2D icon)
        {
            // MENU

            CreateMenu(name, parameterName, menu, icon);


            // EXPRESSION PARAMETERS

            CreateExpressionParameter(parameterName, saved, defaultFloatState, expressionParameters);


            // FX LAYER

                // CONTROLLER PARAMETERS

            CreateControllerParameters(parameterName, defaultState);


                // LAYERS

            CreateLayer(name);


                // ANIMATION

            AnimationClip clip = CreateAnimations(animationName);

                // LOGIC

            CreateLogic(name, clip, parameterName);

            MakeSureItDoesTheThing(FXLayer);
            
        }

        void CreateMenu(string name, string parameterName, VRCExpressionsMenu menu, Texture2D icon = null)
        {
            VRCExpressionsMenu toggleSubmenu = menu;
            VRCExpressionsMenu.Control.Parameter conParameter = new VRCExpressionsMenu.Control.Parameter { name = parameterName };

            VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control { name = name, type = VRCExpressionsMenu.Control.ControlType.RadialPuppet, subParameters = new VRCExpressionsMenu.Control.Parameter[] { conParameter }, icon = icon };

            for (int i = 0; i < toggleSubmenu.controls.Count; i++)
            {
                if (toggleSubmenu.controls[i].name == control.name)
                    toggleSubmenu.controls.RemoveAt(i);
            }

            toggleSubmenu.controls.Add(control);
            MakeSureItDoesTheThing(menu);
        }

        void CreateExpressionParameter(string parameterName, bool saved, float defaultFloatState, VRCExpressionParameters expressionParameters)
        {
            // Create the expression parameter with the name, type and default settings
            VRCExpressionParameters.Parameter objectToggle = new VRCExpressionParameters.Parameter { name = parameterName, valueType = VRCExpressionParameters.ValueType.Float, saved = saved, defaultValue = defaultFloatState };

            VRCExpressionParameters.Parameter[] parameterArray = expressionParameters.parameters;
            parameterArray = parameterArray.Where(x => !x.name.Equals(parameterName)).ToArray();

            int count = parameterArray.Length;

            // Resize the parameterArray to have one more element
            Array.Resize(ref parameterArray, count + 1);

            // Assign the new parameter to the last element
            parameterArray[count] = objectToggle;


            expressionParameters.parameters = parameterArray;

            MakeSureItDoesTheThing(expressionParameters);
        }

        void CreateControllerParameters(string parameterName, float defaultState)
        {
            // Create the controller parameter with the name and type
            AnimatorControllerParameter controllerParameter = new AnimatorControllerParameter { type = AnimatorControllerParameterType.Float, name = parameterName, defaultFloat = defaultState };

            // Loop through the existing parameters
            for (int i = 0; i < FXLayer.parameters.Length; i++)
            {
                // If the name is the same, remove the parameter from the controller
                if (parameterName == FXLayer.parameters[i].name)
                {
                    FXLayer.RemoveParameter(i);
                }
            }

            // Add the new parameter to the controller
            FXLayer.AddParameter(controllerParameter);
        }

        void CreateLayer(string name)
        {
            // Loop through the existing layers
            for (int i = 0; i < FXLayer.layers.Length; i++)
            {
                // If the name is the same, remove the layer from the controller
                if (name == FXLayer.layers[i].name)
                {
                    FXLayer.RemoveLayer(i);
                }
            }


            AnimatorControllerLayer newLayer = new AnimatorControllerLayer
            {
                name = name,
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine { name = name, hideFlags = HideFlags.HideInHierarchy }
            };
            newLayer.stateMachine.exitPosition = new Vector3(50f, 70f, 0f);


            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetDatabase.GetAssetPath(FXLayer));

            FXLayer.AddLayer(newLayer);
        }

        void CreateLogic(string name, AnimationClip animationClip, string parameterName)
        {
            var root = FXLayer.layers[FXLayer.layers.Length - 1].stateMachine;

            List<AnimatorState> theAnimations = new List<AnimatorState>
            {
            root.AddState("Hue Shift", new Vector3(30, 200, 0)),
            };

            if (useWriteDefaults)
                theAnimations[0].writeDefaultValues = true;
            else
                theAnimations[0].writeDefaultValues = false;
            theAnimations[0].motion = animationClip;
            theAnimations[0].timeParameterActive = true;
            theAnimations[0].timeParameter = parameterName;

        }

        AnimationClip CreateAnimations(string animationName)
        {
            if (!useExistingAnimation)
            {
                AnimationClip clip = new AnimationClip();

                Type type = gameObject.GetComponent<Renderer>().GetType();

                AnimationCurve curve1 = AnimationCurve.Linear(0, 0, 0.1666667f, 1);
                clip.SetCurve(GetPathToObject(gameObject.transform), type, "material._MainHueShift", curve1);

                if (extraGameObjects != null && extraGameObjects.Count > 0)
                {
                    for (int i = 0; i < extraGameObjects.Count; i++)
                    {
                        if (extraGameObjects[i] == null)
                        {
                            continue;
                        }
                        clip.SetCurve(GetPathToObject(extraGameObjects[i].transform), type, "material._MainHueShift", curve1);
                    }
                }

                Directory.CreateDirectory(CommonActions.GetFolder(HueShiftUseGlobalKey, HueShiftFolderKey));
                AssetDatabase.CreateAsset(clip, CommonActions.GetFolder(HueShiftUseGlobalKey, HueShiftFolderKey) + "/" + animationName.Replace("/", " ") + ".anim");

                clip = AssetDatabase.LoadAssetAtPath(CommonActions.GetFolder(HueShiftUseGlobalKey, HueShiftFolderKey) + "/" + animationName.Replace("/", " ") + ".anim", typeof(AnimationClip)) as AnimationClip;

                MakeSureItDoesTheThing(clip);

                return clip;
            }
            else
                return existingAnimation;
        }

        bool RequirementsMet()
        {
            if (avatarDescriptor == null)
            {
                EditorGUILayout.HelpBox("Avatar descriptor cannot be null", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (gameObject == null && !useExistingAnimation)
            {
                EditorGUILayout.HelpBox("Object to hue shift cannot be null", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (useExistingAnimation && existingAnimation == null)
            {
                EditorGUILayout.HelpBox("Existing animation cannot be null", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (menu != null && menu.controls.Count >= 8)
            {
                EditorGUILayout.HelpBox("The selected menu is full", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (string.IsNullOrEmpty(toggleName))
            {
                EditorGUILayout.HelpBox("Toggle name cannot be empty", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (string.IsNullOrEmpty(parameterName))
            {
                EditorGUILayout.HelpBox("Parameter name cannot be empty", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }

            return true;
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
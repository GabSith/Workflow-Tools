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
    public class ToggleCreatorEditor : EditorWindow
    {
        //bool refreshedAvatars = false;
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        VRCAvatarDescriptor avatarDescriptor;
        private GameObject gameObject;
        private List<GameObject> extraGameObjects = new List<GameObject> { };

        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;
        AnimatorController FXLayer;
        Texture2D icon;
        AnimationClip existingAnimation;

        string toggleName;
        string animationName;
        string parameterName;

        bool saved = true;
        bool defaultState = true;
        float defaultStateFloat;
        float transitionDuration = 0.1f;
        bool useWriteDefaults = false;

        bool useExistingAnimation = false;
        bool slowTrasition = false;

        bool fold = false;
        bool advancedFold = false;
        Vector2 scrollPosExtraObjects;
        Vector2 scrollPosDescriptors;
        Vector2 scrollPos;


        bool parameterAlreadyExists;
        bool layerAlreadyExists;
        bool animationAlreadyExists;

        AnimationClip proxy;
        private const string ProxyClipGuidKey = "ProxyClipSaver_ClipGuid";

        private const string ToggleCreatorFolderKey = "ToggleCreatorFolderKey";
        private const string ToggleCreatorUseGlobalKey = "ToggleCreatorUseGlobalKey";
        private const string ToggleCreatorFolderSuffixKey = "ToggleCreatorFolderSuffixKey";
        string suffix;



        [MenuItem("GabSith/Toggle Creator", false, 101)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(ToggleCreatorEditor), false, "Toggle Creator");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Audio Mixer@2x").image, text = "Toggle Creator", tooltip = "♥" };
        }



        private void OnEnable()
        {

            if (avatarDescriptor == null)
            {
                CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

                if (avatarDescriptorsFromScene.Length == 1)
                {
                    avatarDescriptor = avatarDescriptorsFromScene[0];
                }
            }

            LoadProxyClip();

            suffix = ProjectSettingsManager.GetString(ToggleCreatorFolderSuffixKey);

        }



        private void OnDisable()
        {
            SaveProxyClip();
        }



        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));

            // Title
            CommonActions.GenerateTitle("Toggle Creator");

            // Avatar Selection
            CommonActions.FindAvatars(ref avatarDescriptor, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);
            if (avatarDescriptor == null)
            {
                CommonActions.RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);
            }

            if (useExistingAnimation)
            {
                GUI.enabled = false;
                gameObject = null;
                extraGameObjects.Clear();
            }

            scrollPosExtraObjects = EditorGUILayout.BeginScrollView(scrollPosExtraObjects, GUIStyle.none, GUI.skin.verticalScrollbar,
                GUILayout.ExpandHeight(false));
            //scrollPosExtraObjects = EditorGUILayout.BeginScrollView(scrollPosExtraObjects, GUILayout.ExpandHeight(false), GUILayout.);


            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("+", GUILayout.Width(20f)))
            {
                extraGameObjects.Add(null);
            }
            EditorGUILayout.LabelField("Object to toggle", GUILayout.MaxWidth(125f));
            gameObject = (GameObject)EditorGUILayout.ObjectField("", gameObject, typeof(GameObject), true, GUILayout.ExpandWidth(true));
            if (gameObject != null && avatarDescriptor == null)
            {
                avatarDescriptor = gameObject.GetComponentInParent<VRCAvatarDescriptor>();
            }

            if ((gameObject != null && avatarDescriptor != null) && (!gameObject.transform.IsChildOf(avatarDescriptor.transform) || gameObject.transform == avatarDescriptor.transform))
            {
                gameObject = null;
            }
            if (EditorGUI.EndChangeCheck())
            {
                toggleName = gameObject != null ? gameObject.name : null ?? "";

                if (gameObject != null && avatarDescriptor != null)
                    animationName = GetPathToObject(gameObject.transform).Replace("/", " ");
                parameterName = toggleName;

                CheckRepeatedParameter(parameterName);
                CheckRepeatedAnimation();
                CheckRepeatedLayer(toggleName);

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
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.LabelField("Object to toggle #" + (i+2), GUILayout.MaxWidth(125f));
                    extraGameObjects[i] = (GameObject)EditorGUILayout.ObjectField("", extraGameObjects[i], typeof(GameObject), true, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();


            GUI.enabled = true;


            menu = (VRCExpressionsMenu)EditorGUILayout.ObjectField("Menu", menu, typeof(VRCExpressionsMenu), true);


            if (avatarDescriptor != null)
            {
                if (avatarDescriptor.baseAnimationLayers[4].animatorController != null)
                    FXLayer = (AnimatorController)avatarDescriptor.baseAnimationLayers[4].animatorController;
                parameters = avatarDescriptor.expressionParameters;
                if (menu == null)
                    menu = avatarDescriptor.expressionsMenu;
            }


            // Use a space to separate the fields
            EditorGUILayout.Space();

            // Use a text field to enter the toggle name
            EditorGUI.BeginChangeCheck();
            toggleName = EditorGUILayout.TextField("Toggle Name", toggleName);
            if (EditorGUI.EndChangeCheck())
            {
                CheckRepeatedLayer(toggleName);
            }
            // Use a space to separate the fields
            EditorGUILayout.Space();

            // Use toggle fields to set the saved and default states with a custom style
            GUIStyle toggleStyle = new GUIStyle(EditorStyles.toggle)
            {
                fontSize = 15 // Increase the font size
            };
            saved = EditorGUILayout.Toggle("Saved", saved, toggleStyle);
            defaultState = EditorGUILayout.Toggle("Default", defaultState, toggleStyle);

            // Use a conditional statement to assign the default state float
            defaultStateFloat = defaultState ? 1f : 0f;


            // Use a space to separate the fields
            EditorGUILayout.Space();


            // Crate fold for advanced settings
            advancedFold = EditorGUILayout.Foldout(advancedFold, "More Settings");

            if (advancedFold) {

                // Change proxy
                EditorGUI.BeginChangeCheck();
                proxy = EditorGUILayout.ObjectField("Proxy Animation", proxy, typeof(AnimationClip), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as AnimationClip;
                if (EditorGUI.EndChangeCheck())
                {
                    SaveProxyClip();
                }
                if (proxy == null)
                {
                    EditorGUILayout.HelpBox("A new proxy animation will be created", MessageType.Info);
                }

                // Use an image field to select a menu icon
                //icon = (Texture2D)EditorGUILayout.ObjectField("Menu Icon", icon, typeof(Texture2D), true);
                icon = EditorGUILayout.ObjectField("Menu Icon", icon, typeof(Texture2D), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;


                // Use a toggle field to set whether to use an existing animation
                useExistingAnimation = EditorGUILayout.Toggle("Use Existing Animation", useExistingAnimation, toggleStyle);

                if (useExistingAnimation)
                {
                    existingAnimation = (AnimationClip)EditorGUILayout.ObjectField("Existing Animation Clip", existingAnimation, typeof(AnimationClip), true);
                    EditorGUILayout.Space();
                }



                // Use a toggle for slow transition
                slowTrasition = EditorGUILayout.Toggle("Slow Trasition", slowTrasition, toggleStyle);

                if (slowTrasition)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        transitionDuration = EditorGUILayout.Slider("Transition Duration", transitionDuration, 0f, 5f);
                        if (GUILayout.Button("Reset", GUILayout.Width(50f)))
                        {
                            transitionDuration = 0.1f;
                        }
                    }

                }

                useWriteDefaults = EditorGUILayout.Toggle("Use Write Defaults", useWriteDefaults, toggleStyle);


                EditorGUILayout.Space();

                // Use a text field for the animation name
                EditorGUI.BeginChangeCheck();
                if (useExistingAnimation)
                    GUI.enabled = false;
                animationName = EditorGUILayout.TextField("Animation Name", animationName);
                GUI.enabled = true;
                if ((animationName == null || animationName == "") && gameObject != null && avatarDescriptor != null) {
                    animationName = GetPathToObject(gameObject.transform).Replace("/", " ");
                }
                if (existingAnimation != null && useExistingAnimation)
                {
                    animationName = existingAnimation.name;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    CheckRepeatedAnimation();
                }




                // Use a text field for the parameter name
                EditorGUI.BeginChangeCheck();
                parameterName = EditorGUILayout.TextField("Parameter Name", parameterName);
                if (EditorGUI.EndChangeCheck())
                {
                    CheckRepeatedParameter(parameterName);
                }

                if (string.IsNullOrEmpty(parameterName)) {
                    parameterName = toggleName;
                }
            }
            else
            {
                if (gameObject != null && avatarDescriptor != null)
                    animationName = GetPathToObject(gameObject.transform).Replace("/", " ");
                parameterName = toggleName;
                CheckRepeatedParameter(parameterName);
                CheckRepeatedAnimation();
            }

            // Use a space to separate the fields
            EditorGUILayout.Space();



            // Select Folder

            if (!useExistingAnimation)
            {
                if (CommonActions.SelectFolder(ToggleCreatorUseGlobalKey, ToggleCreatorFolderKey, ToggleCreatorFolderSuffixKey, ref suffix))
                {
                    CheckRepeatedAnimation();
                }
            }

            EditorGUILayout.Space();

            if (!RequirementsMet())
                GUI.enabled = false;


            if (GUILayout.Button("Create Toggle", new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fixedHeight = 35,
            }))
            {
                CreateToggle(toggleName, parameterName, animationName, saved, defaultStateFloat, menu, parameters, icon);
            }


            // Use a space to separate the fields
            EditorGUILayout.Space();

            GUI.enabled = true;



            // Use a fold to separate the extra buttons
            fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, "Extras");
            if (fold)
            {
                if (string.IsNullOrEmpty(animationName))
                    GUI.enabled = false;
                if (GUILayout.Button("Create Animation Clip"))
                {
                    CreateAnimations(animationName);
                }
                GUI.enabled = true;

                if (string.IsNullOrEmpty(parameterName) || parameters == null)
                    GUI.enabled = false;
                if (GUILayout.Button("Add Parameters"))
                {
                    CreateExpressionParameter(parameterName, saved, defaultStateFloat, parameters);
                    CreateControllerParameters(parameterName, defaultState);
                }
                GUI.enabled = true;

                if (string.IsNullOrEmpty(toggleName) || string.IsNullOrEmpty(parameterName) || menu == null)
                    GUI.enabled = false;
                if (GUILayout.Button("Create Menu Control"))
                {
                    CreateMenu(toggleName, parameterName, menu, icon);
                }
                GUI.enabled = true;

            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

        }



        string GetPathToObject(Transform gameObject)
        {
            return (VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject, avatarDescriptor.transform));
        }

        

        void CreateToggle(string name, string parameterName, string animationName, bool saved, float defaultFloatState, VRCExpressionsMenu menu, VRCExpressionParameters expressionParameters, Texture2D icon)
        {
            // PROXY

            if (proxy == null)
            {
                CreateProxy();
            }

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

            CreateLogic(name, clip);

            MakeSureItDoesTheThing(FXLayer);
            
        }

        void CreateMenu(string name, string parameterName, VRCExpressionsMenu menu, Texture2D icon = null)
        {
            VRCExpressionsMenu toggleSubmenu = menu;
            VRCExpressionsMenu.Control.Parameter conParameter = new VRCExpressionsMenu.Control.Parameter { name = parameterName };

            VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control { name = name, type = VRCExpressionsMenu.Control.ControlType.Toggle, parameter = conParameter, icon = icon };

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
            VRCExpressionParameters.Parameter objectToggle = new VRCExpressionParameters.Parameter { name = parameterName, valueType = VRCExpressionParameters.ValueType.Bool, saved = saved, defaultValue = defaultFloatState };

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

        void CreateControllerParameters(string parameterName, bool defaultState)
        {
            // Create the controller parameter with the name and type
            AnimatorControllerParameter controllerParameter = new AnimatorControllerParameter { type = AnimatorControllerParameterType.Bool, name = parameterName, defaultBool = defaultState };

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

        void CreateLogic(string name, AnimationClip animationClip)
        {
            var root = FXLayer.layers[FXLayer.layers.Length - 1].stateMachine;

            List<AnimatorState> theAnimations = new List<AnimatorState>
            {
            root.AddState("proxy", new Vector3(30, 200, 0)),
            root.AddState(name + " On", new Vector3(-90, 290, 0)),
            root.AddState(name + " Off", new Vector3(150, 290, 0))
            };

            for (int i = 0; i < theAnimations.Count; i++)
            {
                if (useWriteDefaults)
                    theAnimations[i].writeDefaultValues = true;
                else
                    theAnimations[i].writeDefaultValues = false;

                theAnimations[i].motion = proxy;
            }

            theAnimations[2].speed = -1f;
            //AnimationClip clip = AssetDatabase.LoadAssetAtPath(folderPath + "/" + animationName.Replace("/", " ") + ".anim", typeof(AnimationClip)) as AnimationClip;
            AnimationClip clip = animationClip;
            theAnimations[2].motion = clip;
            theAnimations[1].motion = clip;


            // TRANSITIONS

            List<AnimatorStateTransition> theTransitions = new List<AnimatorStateTransition> {
            theAnimations[1].AddTransition(theAnimations[2]),
            theAnimations[2].AddTransition(theAnimations[1]),

            theAnimations[0].AddTransition(theAnimations[1]),
            theAnimations[0].AddTransition(theAnimations[2])
            };

            theTransitions[0].AddCondition(AnimatorConditionMode.IfNot, 0, name);
            theTransitions[1].AddCondition(AnimatorConditionMode.If, 0, name);

            theTransitions[2].AddCondition(AnimatorConditionMode.If, 0, name);
            theTransitions[3].AddCondition(AnimatorConditionMode.IfNot, 0, name);

            for (int i = 0; i < theTransitions.Count; i++)
            {
                theTransitions[i].duration = 0;
                theTransitions[i].hasExitTime = false;
                theTransitions[i].exitTime = 0f;
                theTransitions[i].offset = 999;
            }

            if (slowTrasition)
            {
                theTransitions[0].duration = transitionDuration;
                theTransitions[0].offset = 0;

                theTransitions[1].duration = transitionDuration;
                theTransitions[1].offset = 0;
            }

        }

        void CreateProxy()
        {
            AnimationClip clip = new AnimationClip();

            AnimationCurve curve1 = AnimationCurve.Linear(0, 0, 0.016666f, 0);
            clip.SetCurve("___proxy___", typeof(GameObject), "m_IsActive", curve1);


            Directory.CreateDirectory(GetFolder());
            string clipPath = GetFolder() + "/proxy.anim";
            AssetDatabase.CreateAsset(clip, clipPath);

            clip = AssetDatabase.LoadAssetAtPath(clipPath, typeof(AnimationClip)) as AnimationClip;

            MakeSureItDoesTheThing(clip);

            proxy = clip;
            SaveProxyClip();
        }

        AnimationClip CreateAnimations(string animationName)
        {
            if (!useExistingAnimation)
            {
                AnimationClip clip = new AnimationClip();

                AnimationCurve curve1 = AnimationCurve.Linear(0, 0, 0.016666f, 1);
                clip.SetCurve(GetPathToObject(gameObject.transform), typeof(GameObject), "m_IsActive", curve1);

                if (extraGameObjects != null && extraGameObjects.Count > 0)
                {
                    for (int i = 0; i < extraGameObjects.Count; i++)
                    {
                        if (extraGameObjects[i] == null)
                        {
                            continue;
                        }
                        clip.SetCurve(GetPathToObject(extraGameObjects[i].transform), typeof(GameObject), "m_IsActive", curve1);
                    }
                }


                Directory.CreateDirectory(GetFolder());
                string clipPath = GetFolder() + "/" + animationName.Replace("/", " ") + ".anim";
                AssetDatabase.CreateAsset(clip, clipPath);

                clip = AssetDatabase.LoadAssetAtPath(clipPath, typeof(AnimationClip)) as AnimationClip;

                MakeSureItDoesTheThing(clip);

                EditorGUIUtility.PingObject(clip);

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
            if (avatarDescriptor.baseAnimationLayers[4].animatorController == null)
            {
                //Debug.Log("AAAAAAAAAA");
                EditorGUILayout.HelpBox("The avatar must have an FX Layer", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (avatarDescriptor.expressionParameters == null)
            {
                //Debug.Log("AAAAAAAAAA");
                EditorGUILayout.HelpBox("The avatar must have Expression Parameters", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }
            if (avatarDescriptor.expressionsMenu == null)
            {
                //Debug.Log("AAAAAAAAAA");
                EditorGUILayout.HelpBox("The avatar must have an Expression Menu", MessageType.Error);
                EditorGUILayout.Space();
                return false;
            }


            if (gameObject == null && !useExistingAnimation)
            {
                EditorGUILayout.HelpBox("Object to toggle cannot be null", MessageType.Error);
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
            if (!useExistingAnimation && animationAlreadyExists)
            {
                EditorGUILayout.HelpBox("An animation with this name already exists. The old animation will be replaced.", MessageType.Warning);
            }
            if (layerAlreadyExists)
            {
                EditorGUILayout.HelpBox("A layer in the FX controller with this name already exists. The old layer will be replaced.", MessageType.Warning);
            }
            if (parameterAlreadyExists)
            {
                EditorGUILayout.HelpBox("A parameter with this name already exists. The old parameter will be replaced.", MessageType.Warning);
            }

            /*
            if (proxy == null)
            {
                EditorGUILayout.HelpBox("Proxy animation cannot be null", MessageType.Error);
                EditorGUILayout.Space();
                return false;

            }*/


            return true;
        }

        string GetFolder()
        {
            return CommonActions.GetFolder(ToggleCreatorUseGlobalKey, ToggleCreatorFolderKey) + "/" + ProjectSettingsManager.GetString(ToggleCreatorFolderSuffixKey);
        }


        void CheckRepeatedParameter(string parameter)
        {
            parameterAlreadyExists = false;
            if (avatarDescriptor != null && avatarDescriptor.expressionParameters != null && !string.IsNullOrEmpty(parameter))
            {
                foreach (var item in avatarDescriptor.expressionParameters.parameters)
                {
                    if (item.name == parameter)
                    {
                        // Parameter already exists
                        parameterAlreadyExists = true;
                    }
                }
            }
        }

        void CheckRepeatedAnimation()
        {
            animationAlreadyExists = false;
            if (!string.IsNullOrEmpty(animationName))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath(GetFolder() + "/" + 
                    animationName.Replace("/", " ") + ".anim", typeof(AnimationClip)) as AnimationClip;
                if (clip != null)
                {
                    // Animation already exists
                    animationAlreadyExists = true;
                }
            }
        }

        void CheckRepeatedLayer(string layerName)
        {
            layerAlreadyExists = false;
            if (FXLayer != null && !string.IsNullOrEmpty(layerName))
            {
                for (int i = 0; i < FXLayer.layers.Length; i++)
                {
                    if (layerName == FXLayer.layers[i].name)
                    {
                        // Layer already exists
                        layerAlreadyExists = true;
                    }
                }
            }
        }

        private void SaveProxyClip()
        {
            if (proxy != null)
            {
                string path = AssetDatabase.GetAssetPath(proxy);
                string guid = AssetDatabase.AssetPathToGUID(path);
                //EditorPrefs.SetString(ProxyClipGuidKey, guid);
                ProjectSettingsManager.SetString(ProxyClipGuidKey, guid);
            }
            else
            {
                //EditorPrefs.DeleteKey(ProxyClipGuidKey);
                ProjectSettingsManager.DeleteKey(ProxyClipGuidKey);
            }
        }

        private void LoadProxyClip()
        {
            //string guid = EditorPrefs.GetString(ProxyClipGuidKey, string.Empty);
            string guid = ProjectSettingsManager.GetString(ProxyClipGuidKey, string.Empty);

            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                proxy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
            else
            {
                proxy = null;
            }
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
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
using System.Reflection;

namespace GabSith.WFT
{
    public class AvatarSetupEditor : EditorWindow
    {
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        GameObject avatar;
        VRCAvatarDescriptor avatarDescriptor;

        ModelImporter model;


        string menuName = "Menu";
        string parametersName = "Parameters";
        string FXLayerName = "FX";
        string GestureLayerName = "Gesture";
        string BaseLayerName = "Base";
        string AdditiveLayerName = "Additive";
        string ActionLayerName = "Action";


        bool expParameterAlreadyExists;
        bool expMenuAlreadyExists;
        bool LayerAlreadyExists;
        /*
        bool GestureLayerAlreadyExists;
        bool BaseLayerAlreadyExists;
        bool AdditiveLayerAlreadyExists;
        bool ActionLayerAlreadyExists;*/


        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;
        AnimatorController FXLayer;
        AnimatorController GestureLayer;
        AnimatorController BaseLayer;
        AnimatorController AdditiveLayer;
        AnimatorController ActionLayer;


        string FXGUID = "d40be620cf6c698439a2f0a5144919fe";
        string GestureGUID = "404d228aeae421f4590305bc4cdaba16";
        string BaseGUID = "4e4e1a372a526074884b7311d6fc686b";
        string AdditiveGUID = "573a1373059632b4d820876efe2d277f";
        string ActionGUID = "3e479eeb9db24704a828bffb15406520";


        AnimBool foldLayers = new AnimBool(false);
        AnimBool foldExpressions = new AnimBool(false);
        AnimBool foldModel = new AnimBool(false);
        AnimBool foldDebug = new AnimBool(false);

        bool modelIssues = false;

        //bool foldLayers = false;
        //bool foldExpressions = false;
        //bool foldModel = true;


        //bool fold = false;
        //bool advancedFold = false;
        Vector2 scrollPosDescriptors;
        Vector2 scrollPos;

        private const string AvatarSetupFolderKey = "AvatarSetupFolderKey";
        private const string AvatarSetupUseGlobalKey = "AvatarSetupUseGlobalKey";
        private const string AvatarSetupFolderSuffixKey = "AvatarSetupFolderSuffixKey";
        string suffix;


        //private string defaultPath = "Assets/WF Tools - GabSith/Generated";
        //private string folderPath = "Assets/WF Tools - GabSith/Generated";

        [MenuItem("GabSith/Niche/Avatar Setup", false, 1000)]


        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(AvatarSetupEditor), false, "Avatar Setup");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Audio Mixer@2x").image, text = "Avatar Setup", tooltip = "♥" };
            w.minSize = new Vector2(320, 350);
        }


        private void OnEnable()
        {
            if (avatar == null)
            {
                CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

                if (avatarDescriptorsFromScene.Length == 1)
                {
                    avatar = avatarDescriptorsFromScene[0].gameObject;
                }
            }

            suffix = ProjectSettingsManager.GetString(AvatarSetupFolderSuffixKey);

            foldLayers.valueChanged.AddListener(new UnityAction(Repaint));
            foldExpressions.valueChanged.AddListener(new UnityAction(Repaint));
            foldModel.valueChanged.AddListener(new UnityAction(Repaint));
            foldDebug.valueChanged.AddListener(new UnityAction(Repaint));
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            CommonActions.GenerateTitle("Avatar Setup");

            // Avatar Selection
            CommonActions.FindAvatarsAsObjects(ref avatar, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);

            float height = 38f;

            GUIStyle buttons = new GUIStyle(GUI.skin.button)
            {
                wordWrap = true,
                fixedHeight = height,
                fixedWidth = 140f
            };

            EditorGUILayout.Space(15);



            if (avatar != null && avatar.GetComponent<VRCAvatarDescriptor>() != null)
            {
                avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();


                FXLayer = CheckLayers(4);
                GestureLayer = CheckLayers(2);
                BaseLayer = CheckLayers(0);
                AdditiveLayer = CheckLayers(1);
                ActionLayer = CheckLayers(3);


                parameters = avatarDescriptor.expressionParameters;

                menu = avatarDescriptor.expressionsMenu;
            }
            else
            {
                avatarDescriptor = null;
                FXLayer = null;
                parameters = null;
                menu = null;
            }


            //scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));


            if (avatar != null)
            {

                UnityEngine.Object originalObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(avatar);
                model = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(originalObject)) as ModelImporter;


                if (avatar.activeInHierarchy)
                {
                    EditorGUILayout.HelpBox("Avatar is part of the scene.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Avatar is not instantiated.", MessageType.Warning);
                    if (GUILayout.Button("Instantiate avatar", buttons))
                    {
                        avatar = (GameObject)PrefabUtility.InstantiatePrefab(avatar);

                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (avatarDescriptor != null)
            {
                EditorGUILayout.HelpBox("Avatar Descriptor found.", MessageType.Info);

                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foldLayers.target = EditorGUILayout.BeginFoldoutHeaderGroup(foldLayers.target, "Playable Layers");
                using (var group = new EditorGUILayout.FadeGroupScope(foldLayers.faded))
                {
                    if (group.visible)
                    {
                        //FX Layer
                        ReallyCheckLayers(FXLayer, ref FXLayerName, "FX", FXGUID, 4, "FX Layer");
                        //Gesture Layer
                        ReallyCheckLayers(GestureLayer, ref GestureLayerName, "Gesture", GestureGUID, 2, "Gesture Layer");
                        //Base Layer
                        ReallyCheckLayers(BaseLayer, ref BaseLayerName, "Base", BaseGUID, 0, "Base Layer");
                        //Action Layer
                        ReallyCheckLayers(ActionLayer, ref ActionLayerName, "Action", ActionGUID, 3, "Action Layer");
                        //Additive Layer
                        ReallyCheckLayers(AdditiveLayer, ref AdditiveLayerName, "Additive", AdditiveGUID, 1, "Additive Layer");

                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();


                EditorGUILayout.Space();
                foldExpressions.target = EditorGUILayout.BeginFoldoutHeaderGroup(foldExpressions.target, "Expressions");
                using (var group = new EditorGUILayout.FadeGroupScope(foldExpressions.faded))
                {
                    if (group.visible)
                    {
                        // Expression Menu
                        if (avatarDescriptor.expressionsMenu != null)
                        {
                            EditorGUILayout.HelpBox("Expression Menu found.", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Expression Menu not found.", MessageType.Warning);

                            EditorGUILayout.BeginHorizontal();
                            menuName = EditorGUILayout.TextField("Menu Name", menuName);
                            if (string.IsNullOrEmpty(menuName))
                            {
                                menuName = "Menu";
                            }
                            if (GUILayout.Button("Create Expression Menu", GUILayout.Width(200)))
                            {
                                avatarDescriptor.expressionsMenu = CreateExpressionMenu(menuName);
                            }
                            EditorGUILayout.EndHorizontal();
                            if (CheckRepeatedMenu(GetFolder() + "/" + menuName + ".asset"))
                            {
                                EditorGUILayout.HelpBox("An asset with that name already exists in the selected path. If a new Menu is created, the old one will be replaced.", MessageType.Warning);
                            }
                            EditorGUILayout.Space();
                        }

                        // Expression Parameters
                        if (avatarDescriptor.expressionParameters != null)
                        {
                            EditorGUILayout.HelpBox("Expression Parameters found.", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Expression Parameters not found.", MessageType.Warning);

                            EditorGUILayout.BeginHorizontal();
                            parametersName = EditorGUILayout.TextField("Parameters Name", parametersName);
                            if (string.IsNullOrEmpty(parametersName))
                            {
                                parametersName = "Parameters";
                            }
                            if (GUILayout.Button("Create Expression Parameters", GUILayout.Width(200)))
                            {
                                avatarDescriptor.expressionParameters = CreateExpressionParameters(parametersName);
                            }
                            EditorGUILayout.EndHorizontal();
                            if (CheckRepeatedParameters(GetFolder() + "/" + parametersName + ".asset"))
                            {
                                EditorGUILayout.HelpBox("An asset with that name already exists in the selected path. If a new Parameters asset is created, the old one will be replaced.", MessageType.Warning);
                            }
                            EditorGUILayout.Space();
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.EndVertical();

            }
            else if (avatar != null && avatarDescriptor == null && avatar.activeInHierarchy)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Avatar Descriptor not found.", MessageType.Warning);

                if (GUILayout.Button("Add avatar descriptor component", buttons))
                {
                    Selection.activeGameObject = avatar;

                    avatar.AddComponent<VRCAvatarDescriptor>();
                    MakeSureItDoesTheThing(avatar);

                }
                EditorGUILayout.EndHorizontal();
            }

            if (avatar != null)
            {
                EditorGUILayout.Space(15);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                foldModel.target = EditorGUILayout.BeginFoldoutHeaderGroup(foldModel.target, "Model Settings");
                if (modelIssues && GUILayout.Button("Auto Fix", GUILayout.Width(140f)))
                {
                    if (model != null)
                    {
                        model.animationType = ModelImporterAnimationType.Human;
                        model.isReadable = true;
                        string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
                        PropertyInfo prop = model.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        prop.GetValue(model); prop.SetValue(model, true);

                        model.SaveAndReimport();
                        MakeSureItDoesTheThing(model);
                    }
                }
                EditorGUILayout.EndHorizontal();
                modelIssues = false;

                using (var group = new EditorGUILayout.FadeGroupScope(foldModel.faded))
                {
                    if (group.visible)
                    {
                        if (model == null)
                        {
                            EditorGUILayout.HelpBox("Can't access the original model file.", MessageType.Warning);
                        }

                        else
                        {
                            if (model.animationType != ModelImporterAnimationType.Human)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.HelpBox("Model rig type is not set to humanoid.", MessageType.Warning);
                                modelIssues = true;
                                if (GUILayout.Button("Set model rig as human", buttons))
                                {
                                    model.animationType = ModelImporterAnimationType.Human;
                                    model.SaveAndReimport();
                                }
                                EditorGUILayout.EndHorizontal();

                            }
                            else
                            {
                                EditorGUILayout.HelpBox("Model rig type is set to humanoid.", MessageType.Info);
                            }

                            if (model.isReadable)
                            {
                                EditorGUILayout.HelpBox("Model has Read/Write enabled.", MessageType.Info);
                            }
                            else
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.HelpBox("Model doesn't have Read/Write enabled.", MessageType.Warning);
                                modelIssues = true;
                                if (GUILayout.Button("Enable Read/Write", buttons))
                                {
                                    model.isReadable = true;
                                    model.SaveAndReimport();
                                    MakeSureItDoesTheThing(model);
                                }
                                EditorGUILayout.EndHorizontal();

                            }

                            if (model.importBlendShapeNormals == ModelImporterNormals.Calculate && !CheckBlendshapeNormalsLegacy())
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.HelpBox("Blendshape Normals are set to Calculate and Legacy Blendshape Normals are off.", MessageType.Warning);
                                modelIssues = true;
                                if (GUILayout.Button("Use Legacy Blendshape Normals", buttons))
                                {
                                    string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
                                    PropertyInfo prop = model.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                    prop.GetValue(model); prop.SetValue(model, true);

                                    model.SaveAndReimport();
                                    MakeSureItDoesTheThing(model);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("Model Blendshape Normals are set up correctly.", MessageType.Info);
                            }

                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.EndVertical();



            }


            EditorGUILayout.Space(15);

            CommonActions.SelectFolder(AvatarSetupUseGlobalKey, AvatarSetupFolderKey, AvatarSetupFolderSuffixKey, ref suffix);



            EditorGUILayout.Space();

            GUI.enabled = true;


            foldDebug.target = EditorGUILayout.BeginFoldoutHeaderGroup(foldDebug.target, "Debug");
            using (var group = new EditorGUILayout.FadeGroupScope(foldDebug.faded))
            {
                if (group.visible)
                {

                    if (GUILayout.Button("Check Model"))
                    {
                        UnityEngine.Object originalObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(avatar);
                        Debug.Log(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(originalObject)) as ModelImporter);
                    }

                    if (GUILayout.Button("Check Avatar"))
                    {
                        Debug.Log(avatar.scene.name);
                        Debug.Log(avatar.activeInHierarchy);
                    }

                    if (GUILayout.Button("Check Menu"))
                    {
                        Debug.Log(avatarDescriptor.expressionsMenu);
                    }

                    if (GUILayout.Button("Check FX"))
                    {

                        Debug.Log("Length: " + avatarDescriptor.baseAnimationLayers.Length);
                        Debug.Log(avatarDescriptor);

                        Debug.Log("Controller: " + avatarDescriptor.baseAnimationLayers[4].animatorController);

                        Debug.Log("isEnabled: " + avatarDescriptor.baseAnimationLayers[4].isEnabled);
                        Debug.Log("isDefault: " + avatarDescriptor.baseAnimationLayers[4].isDefault);

                    }

                    if (GUILayout.Button("Check Parameters"))
                    {
                        Debug.Log(avatarDescriptor.expressionParameters);
                    }

                    if (GUILayout.Button("Check Model"))
                    {
                        Debug.Log(model.normalCalculationMode);
                        Debug.Log(model.importBlendShapeNormals);
                        Debug.Log(model.importNormals);

                    }

                    if (GUILayout.Button("Check Normals"))
                    {
                        string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
                        PropertyInfo prop = model.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        Debug.Log(prop.GetValue(model));
                    }

                    if (GUILayout.Button("Check Controller"))
                    {
                        AnimatorController animator = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/256fes 1/VRC/New Animator Controller.controller");

                        Debug.Log(animator.hideFlags);
                        Debug.Log(animator.GetHashCode());
                    }
                }

            }

            //EditorGUILayout.EndScrollView();

            EditorGUILayout.EndFoldoutHeaderGroup();


            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();


        }
        
        bool CheckBlendshapeNormalsLegacy()
        {
            string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
            PropertyInfo prop = model.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (bool)prop.GetValue(model);
        }

        void ReallyCheckLayers(AnimatorController layer, ref string layerName, string defaultName, string guid, int index, string officialName)
        {
            if (layer != null)
            {
                EditorGUILayout.HelpBox(officialName + " found.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(officialName + " not found.", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();
                layerName = EditorGUILayout.TextField(officialName + " Name", layerName);
                if (string.IsNullOrEmpty(layerName))
                {
                    layerName = defaultName;
                }
                if (GUILayout.Button("Create Empty", GUILayout.Width(100)))
                {
                    avatarDescriptor.baseAnimationLayers[index].animatorController = CreateLayer(layerName, index);
                }
                if (GUILayout.Button("Clone Default", GUILayout.Width(100)))
                {
                    avatarDescriptor.baseAnimationLayers[index].animatorController = CloneDefaultLayer(layerName, index, guid);
                }
                EditorGUILayout.EndHorizontal();

                if (CheckRepeatedLayer(GetFolder() + "/" + layerName + ".controller"))
                {
                    EditorGUILayout.HelpBox("A controller with that name already exists in the selected path. If a new " + officialName + " is created, the old one will be replaced.", MessageType.Warning);
                }
                EditorGUILayout.Space();
            }
        }

        AnimatorController CheckLayers(int index)
        {
            if (avatarDescriptor.baseAnimationLayers != null && avatarDescriptor.baseAnimationLayers.Length != 0 
                && !avatarDescriptor.baseAnimationLayers[index].isDefault && avatarDescriptor.baseAnimationLayers[index].animatorController != null)
                return (AnimatorController)avatarDescriptor.baseAnimationLayers[index].animatorController;
            else
            {
                return null;
            }
        }
        

        AnimatorController CreateLayer(string name, int index)
        {
            avatarDescriptor.customizeAnimationLayers = true;
            //avatarDescriptor.baseAnimationLayers[4].isEnabled = true;
            avatarDescriptor.baseAnimationLayers[index].isDefault = false;

            AnimatorControllerLayer newLayer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine { name = "Base Layer", hideFlags = HideFlags.HideInHierarchy }
            };
            newLayer.stateMachine.exitPosition = new Vector3(50f, 70f, 0f);


            AnimatorController animatorController = new AnimatorController 
            { 
                name = name
            };

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(animatorController, GetFolder() + "/" + name + ".controller");

            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
            animatorController.AddLayer(newLayer);


            MakeSureItDoesTheThing(animatorController);

            EditorGUIUtility.PingObject(animatorController);

            return animatorController;
        }

        VRCExpressionsMenu CreateExpressionMenu(string name)
        {
            avatarDescriptor.customExpressions = true;

            VRCExpressionsMenu expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(expressionsMenu, GetFolder() + "/" + name + ".asset");

            MakeSureItDoesTheThing(expressionsMenu);
            EditorGUIUtility.PingObject(expressionsMenu);


            return expressionsMenu;
        }

        VRCExpressionParameters CreateExpressionParameters(string name)
        {
            avatarDescriptor.customExpressions = true;

            VRCExpressionParameters expressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();

            expressionParameters.name = "name";
            expressionParameters.parameters = new VRCExpressionParameters.Parameter[] { };

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(expressionParameters, GetFolder() + "/" + name + ".asset");

            MakeSureItDoesTheThing(expressionParameters);

            EditorGUIUtility.PingObject(expressionParameters);


            return expressionParameters;
        }

        AnimatorController CloneDefaultLayer(string name, int layerIndex, string guid)
        {
            avatarDescriptor.customizeAnimationLayers = true;
            avatarDescriptor.baseAnimationLayers[layerIndex].isDefault = false;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Object not found for the given GUID.");
                return null;
            }

            AnimatorController asset = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (asset == null)
            {
                Debug.LogError("Failed to load object.");
                return null;
            }

            //string fileName = Path.GetFileName(assetPath);
            string fileName = name + ".controller";
            string newPath = Path.Combine(GetFolder(), fileName);

            Directory.CreateDirectory(GetFolder());


            if (AssetDatabase.CopyAsset(assetPath, newPath))
            {
                Debug.Log("Asset copied successfully to: " + newPath);
                AssetDatabase.Refresh();
            }

            AnimatorController copiedAsset = AssetDatabase.LoadAssetAtPath<AnimatorController>(newPath);


            /*
            AnimatorControllerLayer newLayer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine { name = "Base Layer", hideFlags = HideFlags.HideInHierarchy }
            };
            newLayer.stateMachine.exitPosition = new Vector3(50f, 70f, 0f);


            AnimatorController animatorController = new AnimatorController
            {
                name = name,
                //layers = new AnimatorControllerLayer[] { newLayer } 
            };

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(animatorController, GetFolder() + "/" + name + ".controller");

            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
            animatorController.AddLayer(newLayer);
            */

            MakeSureItDoesTheThing(copiedAsset);

            EditorGUIUtility.PingObject(copiedAsset);

            return copiedAsset;
        }


        string GetPathToObject(Transform gameObject)
        {
            return (VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject, avatar.transform));
        }


        bool CheckRepeatedMenu(string path)
        {
            VRCExpressionsMenu tempMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
            if (tempMenu != null)
            {
                expMenuAlreadyExists = true;
            }
            else
            {
                expMenuAlreadyExists = false;
            }
            return expMenuAlreadyExists;
        }
        bool CheckRepeatedParameters(string path)
        {
            VRCExpressionParameters tempParameters = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(path);
            if (tempParameters != null)
            {
                expParameterAlreadyExists = true;
            }
            else
            {
                expParameterAlreadyExists = false;
            }
            return expParameterAlreadyExists;
        }
        bool CheckRepeatedLayer(string path)
        {
            AnimatorController tempController = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (tempController != null)
            {
                LayerAlreadyExists = true;
            }
            else
            {
                LayerAlreadyExists = false;
            }
            return LayerAlreadyExists;
        }


        private string GetFolder()
        {
            return CommonActions.GetFolder(AvatarSetupUseGlobalKey, AvatarSetupFolderKey) + "/" + suffix;
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
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
    public class InitialSetupEditor : EditorWindow
    {
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        GameObject avatar;
        VRCAvatarDescriptor avatarDescriptor;

        ModelImporter model;


        string menuName = "Menu";
        string parametersName = "Parameters";
        string FXLayerName = "FX";

        bool expParameterAlreadyExists;
        bool expMenuAlreadyExists;
        bool FXLayerAlreadyExists;


        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;
        AnimatorController FXLayer;


        bool fold = false;
        //bool advancedFold = false;
        Vector2 scrollPosDescriptors;
        Vector2 scrollPosList;

        private const string InitialSetupFolderKey = "InitialSetupFolderKey";
        private const string InitialSetupUseGlobalKey = "InitialSetupUseGlobalKey";
        //private const string GlobalFolderKey = "GlobalFolderKey";

        //private string defaultPath = "Assets/WF Tools - GabSith/Generated";
        //private string folderPath = "Assets/WF Tools - GabSith/Generated";

        [MenuItem("GabSith/Initial Setup", false, 50)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(InitialSetupEditor), false, "Initial Setup");
            //GUIContent titleContent = new GUIContent("Test", (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Bon/Assets/Materials/Textures/KannaSip.png", typeof(Texture2D)));
            //EditorGUIUtility.IconContent("d_Audio Mixer@2x");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Audio Mixer@2x").image, text = "Initial Setup", tooltip = "♥" };
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


        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Initial Setup");

            // Avatar Selection
            CommonActions.FindAvatarsAsObjects(ref avatar, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);


            EditorGUILayout.Space(15);

            {

                if (avatar != null && avatar.GetComponent<VRCAvatarDescriptor>() != null)
                {
                    avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();

                    if (avatarDescriptor.baseAnimationLayers == null)
                    {
                        //Debug.Log("AAAAAAAA");
                        //avatarDescriptor.baseAnimationLayers = new VRCAvatarDescriptor.CustomAnimLayer[5];
                        Debug.Log(avatarDescriptor.baseAnimationLayers);

                    }
                    else if (avatarDescriptor.baseAnimationLayers.Length == 0)
                    {
                        
                        //Debug.Log("EEEEEEEE");

                    }

                    if (avatarDescriptor.baseAnimationLayers != null && avatarDescriptor.baseAnimationLayers.Length != 0 && !avatarDescriptor.baseAnimationLayers[4].isDefault && avatarDescriptor.baseAnimationLayers[4].animatorController != null)
                        FXLayer = (AnimatorController)avatarDescriptor.baseAnimationLayers[4].animatorController;
                    else
                    {
                        FXLayer = null;
                    }

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


                scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));



                if (avatar != null)
                {

                    UnityEngine.Object originalObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(avatar);
                    model = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(originalObject)) as ModelImporter;



                    if (model == null)
                    {
                        EditorGUILayout.HelpBox("This is not a valid model.", MessageType.Warning);
                        //EditorGUILayout.EndVertical();
                        //EditorGUILayout.EndScrollView();
                    }

                    else
                    {
                        if (model.animationType != ModelImporterAnimationType.Human)
                        {
                            EditorGUILayout.HelpBox("Model rig type is not set to humanoid.", MessageType.Warning);
                            if (GUILayout.Button("Set model rig as human"))
                            {

                                model.animationType = ModelImporterAnimationType.Human;
                                model.SaveAndReimport();
                            }

                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Model rig type is set to humanoid.", MessageType.Info);
                        }
                    }


                    if (avatar.activeInHierarchy)
                    {
                        EditorGUILayout.HelpBox("Avatar is part of the scene.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Avatar is not instantiated.", MessageType.Warning);
                        if (GUILayout.Button("Instantiate avatar"))
                        {
                            avatar = (GameObject)PrefabUtility.InstantiatePrefab(avatar);

                        }
                    }
                }

                if (avatarDescriptor != null)
                {
                    EditorGUILayout.HelpBox("Avatar Descriptor found.", MessageType.Info);

                    //FX Layer
                    if (FXLayer != null)
                    {
                        EditorGUILayout.HelpBox("FX Layer found.", MessageType.Info);

                    }
                    else
                    {
                        EditorGUILayout.HelpBox("FX Layer not found.", MessageType.Warning);

                        EditorGUILayout.BeginHorizontal();
                        FXLayerName = EditorGUILayout.TextField("FX Layer Name", FXLayerName);
                        if (string.IsNullOrEmpty(FXLayerName))
                        {
                            FXLayerName = "FX";
                        }
                        if (GUILayout.Button("Create FX Layer", GUILayout.Width(200)))
                        {
                            avatarDescriptor.baseAnimationLayers[4].animatorController = CreateFXLayer(FXLayerName);
                        }
                        EditorGUILayout.EndHorizontal();

                        if (CheckRepeatedFX(GetFolder() + "/" + FXLayerName + ".controller"))
                        {
                            EditorGUILayout.HelpBox("A controller with that name already exists in the selected path. If a new FX Layer is created, the old one will be replaced.", MessageType.Warning);
                        }
                        EditorGUILayout.Space();

                    }

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
                else if (avatar != null && avatarDescriptor == null && avatar.activeInHierarchy)
                {
                    EditorGUILayout.HelpBox("Avatar Descriptor not found.", MessageType.Warning);

                    if (GUILayout.Button("Add avatar descriptor component"))
                    {
                        Selection.activeGameObject = avatar;

                        avatar.AddComponent<VRCAvatarDescriptor>();
                        MakeSureItDoesTheThing(avatar);

                    }

                }

                if (avatar != null && model != null)
                {
                    if (model.isReadable)
                    {
                        EditorGUILayout.HelpBox("Model has Read/Write enabled.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Model doesn't have Read/Write enabled.", MessageType.Warning);
                        if (GUILayout.Button("Enable Read/Write"))
                        {
                            model.isReadable = true;
                            model.SaveAndReimport();
                            MakeSureItDoesTheThing(model);
                        }

                    }
                    if (model.importBlendShapeNormals == ModelImporterNormals.Calculate && !CheckBlendshapeNormalsLegacy())
                    {
                        EditorGUILayout.HelpBox("Blendshape Normals are set to Calculate and Legacy Blendshape Normals are off.", MessageType.Warning);


                        if (GUILayout.Button("Use Legacy Blendshape Normals"))
                        {
                            string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
                            PropertyInfo prop = model.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            prop.GetValue(model); prop.SetValue(model, true);

                            model.SaveAndReimport();
                            MakeSureItDoesTheThing(model);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Model Blendshape Normals are set up correctly.", MessageType.Info);
                    }

                }

            }



            // Use a space to separate the fields
            EditorGUILayout.Space();


            /*
            // Use a button to select the folder path 
            if (GUILayout.Button("Select Folder"))
            {
                folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");

                if (folderPath == null || folderPath == "")
                {
                    folderPath = defaultPath;
                }

                int index = folderPath.IndexOf("Assets/");

                folderPath = folderPath.Substring(index);

                CheckRepeatedFX(GetFolder() + "/" + FXLayerName + ".controller");
                CheckRepeatedMenu(GetFolder() + "/" + menuName + ".asset");
                CheckRepeatedParameters(GetFolder() + "/" + parametersName + ".asset");

            }
            GUI.enabled = true;
            */

            /*
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Folder"))
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");

                if (folderPath == null || folderPath == "")
                {
                    folderPath = defaultPath;
                }

                int index = folderPath.IndexOf("Assets/");

                folderPath = folderPath.Substring(index);

                if (ProjectSettingsManager.GetBool(InitialSetupUseGlobalKey, true))
                    ProjectSettingsManager.SetString(GlobalFolderKey, folderPath);
                else
                    ProjectSettingsManager.SetString(InitialSetupFolderKey, folderPath);
            }
            // Global Folder
            Color def = GUI.backgroundColor;
            if (ProjectSettingsManager.GetBool(InitialSetupUseGlobalKey, true))
            {
                GUI.backgroundColor = new Color { r = 0.5f, g = 1f, b = 0.5f, a = 1 };
            }
            if (GUILayout.Button("Use Global", GUILayout.Width(100f)))
            {
                //useGlobal = !useGlobal;
                ProjectSettingsManager.SetBool(InitialSetupUseGlobalKey, !ProjectSettingsManager.GetBool(InitialSetupUseGlobalKey, true));
            }
            GUI.backgroundColor = def;

            EditorGUILayout.EndHorizontal();
            */

            CommonActions.SelectFolder(InitialSetupUseGlobalKey, InitialSetupFolderKey);



            EditorGUILayout.Space();

            GUI.enabled = true;

            // Use a fold to separate the extra buttons
            fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, "Debug");
            if (fold)
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




                /*
                if (GUILayout.Button("Test Path To Object"))
                {
                    //Debug.Log(VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject.transform, avatarDescriptor.transform));

                    Debug.Log(GetPathToObject(gameObject.transform));
                }*/
            }


            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Use a space to separate the fields
            EditorGUILayout.Space();

            // End the vertical layout group
            EditorGUILayout.EndVertical();

            // Use a space to separate the preview
            EditorGUILayout.Space(10);

        }

        
        bool CheckBlendshapeNormalsLegacy()
        {
            string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
            PropertyInfo prop = model.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (bool)prop.GetValue(model);
        }

        

        AnimatorController CreateFXLayer(string name)
        {
            avatarDescriptor.customizeAnimationLayers = true;
            //avatarDescriptor.baseAnimationLayers[4].isEnabled = true;
            avatarDescriptor.baseAnimationLayers[4].isDefault = false;

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


            MakeSureItDoesTheThing(animatorController);

            return animatorController;
        }

        VRCExpressionsMenu CreateExpressionMenu(string name)
        {
            avatarDescriptor.customExpressions = true;

            VRCExpressionsMenu expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(expressionsMenu, GetFolder() + "/" + name + ".asset");

            MakeSureItDoesTheThing(expressionsMenu);

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

            return expressionParameters;
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
        bool CheckRepeatedFX(string path)
        {
            AnimatorController tempFX = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (tempFX != null)
            {
                FXLayerAlreadyExists = true;
            }
            else
            {
                FXLayerAlreadyExists = false;
            }
            return FXLayerAlreadyExists;
        }


        private string GetFolder()
        {
            return CommonActions.GetFolder(InitialSetupUseGlobalKey, InitialSetupFolderKey);
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
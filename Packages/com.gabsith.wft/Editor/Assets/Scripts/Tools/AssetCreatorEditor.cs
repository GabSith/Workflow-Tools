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
    /// <summary>
    /// Editor window for creating various assets commonly used in VRChat avatar development.
    /// </summary>
    public class AssetCreatorShortcuts : EditorWindow
    {
        // Scroll position for the GUI
        Vector2 scrollPos;

        // Constants for saved keys
        private const string AssetCreatorSelectedShaderKey = "AssetCreatorSelectedShaderKey";

        private const string AssetCreatorFolderKey = "AssetCreatorFolderKey";
        private const string AssetCreatorUseGlobalKey = "AssetCreatorUseGlobalKey";
        private const string AssetCreatorFolderSuffixKey = "AssetCreatorFolderSuffixKey";
        string suffix;

        // Array of shader names and the selected shader index
        string[] shaders = new string[4] { "Standard", "VRChat/Mobile/Toon Lit", "VRChat/Mobile/Standard Lite", ".poiyomi/Poiyomi Toon" };
        int selectedShader = 0;

        // AnimBool for smooth UI transitions of the material mode
        AnimBool materialMode = new AnimBool(false);

        /// <summary>
        /// Creates and shows the Asset Creator Shortcuts window.
        /// </summary>
        [MenuItem("GabSith/Niche/Asset Creator Shortcuts %#&S", false, 800)]
        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(AssetCreatorShortcuts), false, "Asset Creator Shortcuts");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("scene-template-dark").image, text = "Asset Creator Shortcuts", tooltip = "♥" };
            w.minSize = new Vector2(300, 200);
        }

        /// <summary>
        /// Called when the window is enabled. Sets up the material mode animation listener.
        /// </summary>
        private void OnEnable()
        {
            int.TryParse(ProjectSettingsManager.GetString(AssetCreatorSelectedShaderKey), out selectedShader);
            materialMode.valueChanged.AddListener(new UnityAction(Repaint));
        }

        /// <summary>
        /// Draws the GUI for the Asset Creator Shortcuts window.
        /// </summary>
        void OnGUI()
        {
            float height = 25f;
            GUIStyle buttons = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = height,
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

            CommonActions.GenerateTitle("Asset Creator Shortcuts");

            // Material creation section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Material", buttons))
            {
                CreateMaterial("Material", shaders[selectedShader]);
            }

            if (CommonActions.ToggleButton("▼ Shader ▼", materialMode.target, GUILayout.Height(25f), GUILayout.Width(100f)))
            {
                materialMode.target = !materialMode.target;
            }

            EditorGUILayout.EndHorizontal();

            // Shader selection dropdown
            using (var group = new EditorGUILayout.FadeGroupScope(materialMode.faded))
            {
                if (group.visible)
                {
                    EditorGUI.BeginChangeCheck();
                    selectedShader = EditorGUILayout.Popup("Shader", selectedShader, shaders);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ProjectSettingsManager.SetString(AssetCreatorSelectedShaderKey, selectedShader.ToString());
                    }
                }
            }
            EditorGUILayout.EndVertical();

            // Buttons for creating various assets
            if (GUILayout.Button("Animation Clip", buttons))
            {
                CreateAnimationClip("Clip");
            }

            if (GUILayout.Button("Animation Controller", buttons))
            {
                CreateController("Controller");
            }

            if (GUILayout.Button("Menu", buttons))
            {
                CreateExpressionMenu("Menu");
            }

            EditorGUILayout.Space(20f);

            // Folder selection
            CommonActions.SelectFolder(AssetCreatorUseGlobalKey, AssetCreatorFolderKey, AssetCreatorFolderSuffixKey, ref suffix);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Creates a new material with the specified name and shader.
        /// </summary>
        Material CreateMaterial(string name, string shader)
        {
            Shader matShader = Shader.Find(shader);

            if (matShader == null)
            {
                Debug.LogError("Shader not found!");
                return null;
            }

            Material material = new Material(Shader.Find("Standard"));
            material.shader = matShader;

            CreateAssetExt(material, name, ".mat");

            MakeSureItDoesTheThing(material);

            EditorGUIUtility.PingObject(material);

            return material;
        }

        /// <summary>
        /// Creates a new VRCExpressionsMenu asset.
        /// </summary>
        VRCExpressionsMenu CreateExpressionMenu(string name)
        {
            VRCExpressionsMenu expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

            CreateAssetExt(expressionsMenu, name, ".asset");

            MakeSureItDoesTheThing(expressionsMenu);

            EditorGUIUtility.PingObject(expressionsMenu);

            return expressionsMenu;
        }

        /// <summary>
        /// Creates a new AnimationClip asset.
        /// </summary>
        AnimationClip CreateAnimationClip(string name)
        {
            AnimationClip clip = new AnimationClip();

            CreateAssetExt(clip, name, ".anim");

            MakeSureItDoesTheThing(clip);

            EditorGUIUtility.PingObject(clip);

            return clip;
        }

        /// <summary>
        /// Creates a new AnimatorController asset.
        /// </summary>
        AnimatorController CreateController(string name)
        {
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

            CreateAssetExt(animatorController, name, ".controller");

            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
            animatorController.AddLayer(newLayer);

            MakeSureItDoesTheThing(animatorController);

            EditorGUIUtility.PingObject(animatorController);

            return animatorController;
        }

        /// <summary>
        /// Creates an asset file with a unique name in the specified folder.
        /// </summary>
        void CreateAssetExt(UnityEngine.Object asset, string name, string extension)
        {
            string folder = GetFolder();
            Directory.CreateDirectory(folder);

            string uniquePath = GetUniqueFileName(folder, name, extension);

            AssetDatabase.CreateAsset(asset, uniquePath);
        }

        /// <summary>
        /// Generates a unique file name to avoid overwriting existing files.
        /// </summary>
        string GetUniqueFileName(string basePath, string name, string extension)
        {
            string fullPath = basePath + "/" + name + extension;
            int counter = 1;

            while (File.Exists(fullPath))
            {
                fullPath = basePath + "/" + name + " " + counter + extension;
                counter++;
            }

            return fullPath;
        }

        /// <summary>
        /// Gets the folder path for asset creation.
        /// </summary>
        string GetFolder()
        {
            return CommonActions.GetFolder(AssetCreatorUseGlobalKey, AssetCreatorFolderKey, AssetCreatorFolderSuffixKey);
        }

        /// <summary>
        /// Marks the specified object as dirty, saves all assets, and refreshes the AssetDatabase.
        /// </summary>
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
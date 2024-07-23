#if UNITY_EDITOR


using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using System;


namespace GabSith.WFT
{
    public class CommonActions
    {

        public static void GenerateTitle(string name)
        {
            Color tempColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.95f, 0.9f, 0.9f, 1f);
            EditorGUILayout.BeginVertical(GUI.skin.window, GUILayout.MaxHeight(25));
            GUI.backgroundColor = tempColor;


            EditorGUILayout.LabelField(name, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 18,
                fixedHeight = 17
            });
            EditorGUILayout.LabelField("by GabSith", new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerCenter,
                fixedHeight = 10
            });

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(15);
        }

        public static bool FindAvatars(ref VRCAvatarDescriptor avatarDescriptor, ref Vector2 scrollPosDescriptors, ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            bool changesMade = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (avatarDescriptorsFromScene == null)
                {
                    RefreshDescriptors(ref avatarDescriptorsFromScene);
                }

                EditorGUI.BeginChangeCheck();
                avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
                if (EditorGUI.EndChangeCheck())
                {
                    changesMade = true;
                }

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Select From Scene" : "Refresh", avatarDescriptorsFromScene.Length < 2 ? GUILayout.Width(130f) : GUILayout.Width(70f)))
                {
                    RefreshDescriptors(ref avatarDescriptorsFromScene);

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                        changesMade = true;
                    }
                }
            }
            

            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EditorGUILayout.BeginHorizontal();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        RefreshDescriptors(ref avatarDescriptorsFromScene);
                    }
                    else if (GUILayout.Button(item != null ? item.name : ""))
                    {
                        avatarDescriptor = item;
                        changesMade = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
            }
            return changesMade;
        }

        public static bool FindAvatarsAsObjects(ref GameObject avatarDescriptor, ref Vector2 scrollPosDescriptors, ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            bool changesMade = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (avatarDescriptorsFromScene == null)
                {
                    RefreshDescriptors(ref avatarDescriptorsFromScene);
                }

                EditorGUI.BeginChangeCheck();
                avatarDescriptor = (GameObject)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    changesMade = true;
                }

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Select From Scene" : "Refresh", avatarDescriptorsFromScene.Length < 2 ? GUILayout.Width(130f) : GUILayout.Width(70f)))
                {
                    RefreshDescriptors(ref avatarDescriptorsFromScene);

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0].gameObject;
                    }
                }
            }


            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EditorGUILayout.BeginHorizontal();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        RefreshDescriptors(ref avatarDescriptorsFromScene);
                    }
                    else if (GUILayout.Button(item != null ? item.name : ""))
                    {
                        avatarDescriptor = item.gameObject;
                        changesMade = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
            }

            return changesMade;
        }

        public static void RefreshDescriptors(ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);
        }

        public static bool SelectFolder(string LocalUseGlobalKey, string LocalFolderKey, string LocalSuffixKey, ref string suffix)
        {
            bool changed = false;
            //string defaultPath = "Assets/WF Tools - GabSith/Generated";
            string globalFolderKey = "GlobalFolderKey";


            EditorGUILayout.BeginHorizontal();

            // Use a button to select the folder path 
            if (GUILayout.Button("Select Folder"))
            {
                string tempPath = CommonActions.GetFolder(LocalUseGlobalKey, LocalFolderKey);
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");

                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = tempPath;
                }

                int index = folderPath.IndexOf("Assets/");

                folderPath = folderPath.Substring(index);

                if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
                    ProjectSettingsManager.SetString(globalFolderKey, folderPath);
                else
                    ProjectSettingsManager.SetString(LocalFolderKey, folderPath);

                changed = true;
            }

            // Global Folder
            Color def = GUI.backgroundColor;
            if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
            {
                GUI.backgroundColor = new Color { r = 0.5f, g = 1f, b = 0.5f, a = 1 };
            }
            if (GUILayout.Button("Use Global", GUILayout.Width(100f)))
            {
                //useGlobal = !useGlobal;
                ProjectSettingsManager.SetBool(LocalUseGlobalKey, !ProjectSettingsManager.GetBool(LocalUseGlobalKey, true));
                changed = true;
            }
            GUI.backgroundColor = def;

            EditorGUILayout.EndHorizontal();


            GUI.enabled = true;

            GUIStyle pathLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Folder: " + CommonActions.GetFolder(LocalUseGlobalKey, LocalFolderKey), pathLabelStyle);
            // Suffix
            EditorGUILayout.LabelField("/", GUILayout.Width(7f));
            EditorGUI.BeginChangeCheck();
            suffix = EditorGUILayout.TextField("", suffix, GUILayout.MaxWidth(100f));
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettingsManager.SetString(LocalSuffixKey, suffix);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();

            return changed;
        }


        public static bool SelectFolder(string LocalUseGlobalKey, string LocalFolderKey)
        {
            bool changed = false;
            //string defaultPath = "Assets/WF Tools - GabSith/Generated";
            string globalFolderKey = "GlobalFolderKey";


            EditorGUILayout.BeginHorizontal();

            // Use a button to select the folder path 
            if (GUILayout.Button("Select Folder"))
            {
                string tempPath = CommonActions.GetFolder(LocalUseGlobalKey, LocalFolderKey);
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");

                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = tempPath;
                }

                int index = folderPath.IndexOf("Assets/");

                folderPath = folderPath.Substring(index);

                if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
                    ProjectSettingsManager.SetString(globalFolderKey, folderPath);
                else
                    ProjectSettingsManager.SetString(LocalFolderKey, folderPath);

                changed = true;
            }

            // Global Folder
            Color def = GUI.backgroundColor;
            if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
            {
                GUI.backgroundColor = new Color { r = 0.5f, g = 1f, b = 0.5f, a = 1 };
            }
            if (GUILayout.Button("Use Global", GUILayout.Width(100f)))
            {
                //useGlobal = !useGlobal;
                ProjectSettingsManager.SetBool(LocalUseGlobalKey, !ProjectSettingsManager.GetBool(LocalUseGlobalKey, true));
                changed = true;
            }
            GUI.backgroundColor = def;

            EditorGUILayout.EndHorizontal();


            GUI.enabled = true;

            GUIStyle pathLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            EditorGUILayout.LabelField("Selected Folder: " + CommonActions.GetFolder(LocalUseGlobalKey, LocalFolderKey), pathLabelStyle);

            return changed;
        }



        public static string GetFolder(string LocalUseGlobalKey, string localFolderKey)
        {
            string defaultPath = "Assets/WF Tools - GabSith/Generated";
            string globalFolderKey = "GlobalFolderKey";

            if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
            {
                return ProjectSettingsManager.GetString(globalFolderKey, defaultPath);
            }
            else
            {
                return ProjectSettingsManager.GetString(localFolderKey, defaultPath);
            }
        }

    }
}    
#endif
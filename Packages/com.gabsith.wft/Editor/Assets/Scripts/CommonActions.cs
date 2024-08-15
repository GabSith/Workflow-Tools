#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using System;
using System.Text.RegularExpressions;


namespace GabSith.WFT
{
    /// <summary>
    /// Provides common utility actions for my Unity editor scripts.
    /// </summary>
    public class CommonActions
    {
        // Color used for toggle buttons
        public static Color selectionColor = new Color(0.9f, 1.15f, 1.45f);

        // Default color for headers
        static Color defaultHeaderColor = new Color(0.95f, 0.9f, 0.9f, 1f);

        // Keys for storing settings
        static string globalFolderKey = "GlobalFolderKey";
        private const string DefaultFolderKey = "DefaultFolderKey";
        private const string SettingsUseHeaderKey = "SettingsUseHeaderKey";
        private const string HeaderColorKey = "HeaderColorKey";


        /// <summary>
        /// Generates a title header for the editor window.
        /// </summary>
        /// <param name="name">The name to display in the title.</param>
        public static void GenerateTitle(string name)
        {
            if (ProjectSettingsManager.GetBool(SettingsUseHeaderKey, true))
            {
                Color defaultColor = GUI.backgroundColor;
                GUI.backgroundColor = ProjectSettingsManager.GetColor(HeaderColorKey, defaultHeaderColor);

                EditorGUILayout.BeginVertical(GUI.skin.window, GUILayout.MaxHeight(25));
                GUI.backgroundColor = defaultColor;

                // Display the main title
                EditorGUILayout.LabelField(name, new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = 18,
                    fixedHeight = 17
                });

                // Display the author credit
                EditorGUILayout.LabelField("by GabSith", new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.LowerCenter,
                    fixedHeight = 10
                });

                EditorGUILayout.Space(5);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(15);
                GUI.backgroundColor = defaultColor;
            }
            else
            {
                EditorGUILayout.Space(10);
            }
        }

        /// <summary>
        /// Creates a toggle button that changes color when toggled on.
        /// </summary>
        /// <param name="buttonText">The text to display on the button.</param>
        /// <param name="toggleValue">The current toggle state.</param>
        /// <param name="options">Additional GUILayout options.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public static bool ToggleButton(string buttonText, bool toggleValue, params GUILayoutOption[] options)
        {
            Color defaultColor = GUI.backgroundColor;
            if (toggleValue)
                GUI.backgroundColor = ProjectSettingsManager.GetColor("ButtonsColorKey", CommonActions.selectionColor);
            if (GUILayout.Button(buttonText, options))
            {
                return true;
            }
            GUI.backgroundColor = defaultColor;
            return false;
        }

        /// <summary>
        /// Creates a toggle button with a GUIContent that changes color when toggled on.
        /// </summary>
        /// <param name="gUIContent">The GUIContent to display on the button.</param>
        /// <param name="toggleValue">The current toggle state.</param>
        /// <param name="options">Additional GUILayout options.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public static bool ToggleButton(GUIContent gUIContent, bool toggleValue, params GUILayoutOption[] options)
        {
            Color defaultColor = GUI.backgroundColor;
            if (toggleValue)
                GUI.backgroundColor = ProjectSettingsManager.GetColor("ButtonsColorKey", CommonActions.selectionColor);
            if (GUILayout.Button(gUIContent, options))
            {
                return true;
            }
            GUI.backgroundColor = defaultColor;
            return false;
        }

        /// <summary>
        /// Finds and displays VRCAvatarDescriptors in the scene.
        /// </summary>
        /// <param name="avatarDescriptor">The currently selected avatar descriptor.</param>
        /// <param name="scrollPosDescriptors">The scroll position for the descriptor list.</param>
        /// <param name="avatarDescriptorsFromScene">An array of all avatar descriptors in the scene.</param>
        /// <returns>True if changes were made, false otherwise.</returns>
        public static bool FindAvatars(ref VRCAvatarDescriptor avatarDescriptor, ref Vector2 scrollPosDescriptors, ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            bool changesMade = false;

            // Refresh descriptors if needed
            if (avatarDescriptorsFromScene == null || avatarDescriptor == null)
            {
                RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);
            }

            // Display avatar selection field and refresh button
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
                if (EditorGUI.EndChangeCheck())
                {
                    changesMade = true;
                }

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Select From Scene" : "Refresh", avatarDescriptorsFromScene.Length < 2 ? GUILayout.Width(130f) : GUILayout.Width(70f)))
                {
                    RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                        changesMade = true;
                    }
                }
            }

            // Display scrollable list of avatars if there are multiple in the scene
            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EditorGUILayout.BeginHorizontal();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);
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

        /// <summary>
        /// Finds and displays VRCAvatarDescriptors in the scene, returning GameObjects.
        /// </summary>
        /// <param name="avatarDescriptor">The currently selected avatar GameObject.</param>
        /// <param name="scrollPosDescriptors">The scroll position for the descriptor list.</param>
        /// <param name="avatarDescriptorsFromScene">An array of all avatar descriptors in the scene.</param>
        /// <returns>True if changes were made, false otherwise.</returns>
        public static bool FindAvatarsAsObjects(ref GameObject avatarDescriptor, ref Vector2 scrollPosDescriptors, ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            bool changesMade = false;

            // Refresh descriptors if needed
            if (avatarDescriptorsFromScene == null)
            {
                RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);
            }

            // Display avatar selection field and refresh button
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                avatarDescriptor = (GameObject)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    changesMade = true;
                }

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Select From Scene" : "Refresh", avatarDescriptorsFromScene.Length < 2 ? GUILayout.Width(130f) : GUILayout.Width(70f)))
                {
                    RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0].gameObject;
                    }
                }
            }

            // Display scrollable list of avatars if there are multiple in the scene
            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EditorGUILayout.BeginHorizontal();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);
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

        /// <summary>
        /// Refreshes the list of VRCAvatarDescriptors in the scene.
        /// </summary>
        /// <param name="avatarDescriptor">Reference to the current avatar descriptor.</param>
        /// <param name="avatarDescriptorsFromScene">Array to store found avatar descriptors.</param>
        public static void RefreshDescriptors(ref VRCAvatarDescriptor avatarDescriptor, ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);

            if (avatarDescriptorsFromScene.Length == 1)
            {
                avatarDescriptor = avatarDescriptorsFromScene[0];
            }
        }

        /// <summary>
        /// Refreshes the list of VRCAvatarDescriptors in the scene, using GameObject reference.
        /// </summary>
        /// <param name="avatarDescriptor">Reference to the current avatar GameObject.</param>
        /// <param name="avatarDescriptorsFromScene">Array to store found avatar descriptors.</param>
        public static void RefreshDescriptors(ref GameObject avatarDescriptor, ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);

            if (avatarDescriptorsFromScene.Length == 1)
            {
                avatarDescriptor = avatarDescriptorsFromScene[0].gameObject;
            }
        }

        /// <summary>
        /// Refreshes the list of VRCAvatarDescriptors in the scene.
        /// </summary>
        /// <param name="avatarDescriptorsFromScene">Array to store found avatar descriptors.</param>
        public static void RefreshDescriptors(ref VRCAvatarDescriptor[] avatarDescriptorsFromScene)
        {
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);
        }

        /// <summary>
        /// Displays a folder selection interface in the editor.
        /// </summary>
        /// <param name="LocalUseGlobalKey">Key for the setting to use global folder.</param>
        /// <param name="LocalFolderKey">Key for the local folder setting.</param>
        /// <param name="LocalSuffixKey">Key for the folder suffix setting.</param>
        /// <param name="suffix">Reference to the current suffix string.</param>
        /// <returns>True if changes were made, false otherwise.</returns>
        public static bool SelectFolder(string LocalUseGlobalKey, string LocalFolderKey, string LocalSuffixKey, ref string suffix)
        {
            bool changed = false;
            float buttonHeight = 22f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Button to select folder path
            if (GUILayout.Button("Select Folder", GUILayout.Height(buttonHeight)))
            {
                string tempPath = CommonActions.GetFolder(LocalUseGlobalKey, LocalFolderKey, LocalSuffixKey);
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

            // Toggle for using global folder
            Color def = GUI.backgroundColor;
            if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
            {
                GUI.backgroundColor = ProjectSettingsManager.GetColor("ButtonsColorKey", selectionColor);
            }
            if (GUILayout.Button("Use Global", GUILayout.Width(100f), GUILayout.Height(buttonHeight)))
            {
                ProjectSettingsManager.SetBool(LocalUseGlobalKey, !ProjectSettingsManager.GetBool(LocalUseGlobalKey, true));
                changed = true;
            }
            GUI.backgroundColor = def;

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            // Display selected folder path
            GUIStyle pathLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Folder: " + CommonActions.GetFolder(LocalUseGlobalKey, LocalFolderKey, ""), pathLabelStyle);

            // Suffix input field
            EditorGUILayout.LabelField("/", GUILayout.Width(7f));
            EditorGUI.BeginChangeCheck();
            suffix = EditorGUILayout.TextField("", suffix, GUILayout.MaxWidth(100f));
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettingsManager.SetString(LocalSuffixKey, suffix);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            return changed;
        }

        /// <summary>
        /// Gets the current folder path based on settings.
        /// </summary>
        /// <param name="LocalUseGlobalKey">Key for the setting to use global folder.</param>
        /// <param name="localFolderKey">Key for the local folder setting.</param>
        /// <param name="localSuffixKey">Key for the folder suffix setting.</param>
        /// <returns>The current folder path.</returns>
        public static string GetFolder(string LocalUseGlobalKey, string localFolderKey, string localSuffixKey)
        {
            string defaultPath = ProjectSettingsManager.GetString(DefaultFolderKey, ProjectSettingsManager.defaultPath);

            if (ProjectSettingsManager.GetBool(LocalUseGlobalKey, true))
            {
                string globalFolder = ProjectSettingsManager.GetString(globalFolderKey, defaultPath);
                if (!string.IsNullOrEmpty(localSuffixKey))
                    globalFolder += "/" + ProjectSettingsManager.GetString(localSuffixKey, "");
                return (globalFolder);
            }
            else
            {
                string localFolder = ProjectSettingsManager.GetString(localFolderKey, defaultPath);
                if (!string.IsNullOrEmpty(localSuffixKey))
                    localFolder += "/" + ProjectSettingsManager.GetString(localSuffixKey, "");
                return (localFolder);
            }
        }




        public static string CleanRichText(string text)
        {
            // Define the pattern to match all tags
            string pattern = @"<[^>]+>";

            // Use Regex.Replace with a MatchEvaluator to selectively keep or remove tags
            string cleanText = Regex.Replace(text, pattern, match =>
            {
                string tag = match.Value.ToLower();

            // Keep opening and closing tags for bold, italics, and color
            if (tag.StartsWith("<b>") || tag.StartsWith("</b>") ||
                    tag.StartsWith("<i>") || tag.StartsWith("</i>") ||
                    tag.StartsWith("<color=") || tag.StartsWith("</color>"))
                {
                    return match.Value;
                }

            // Remove all other tags
            return string.Empty;
            });

            return (CompleteTags(cleanText));
        } 


        public static string CompleteTags(string text)
        {
            Stack<string> tagStack = new Stack<string>();
            string pattern = @"</?(\w+)(?:\s+[^>]*)?>";

            string cleanedText = Regex.Replace(text, pattern, match =>
            {
                string fullTag = match.Value.ToLower();
                string tagName = match.Groups[1].Value.ToLower();
                bool isClosingTag = fullTag.StartsWith("</");

                if (tagName == "b" || tagName == "i" || tagName == "color")
                {
                    if (isClosingTag)
                    {
                        if (tagStack.Count > 0 && tagStack.Peek() == tagName)
                        {
                            tagStack.Pop();
                        }
                        return fullTag;
                    }
                    else
                    {
                        tagStack.Push(tagName);
                        return fullTag;
                    }
                }

                return ""; // Remove all other tags
            });

            // Close any remaining open tags
            while (tagStack.Count > 0)
            {
                string tag = tagStack.Pop();
                cleanedText += $"</{tag}>";
            }

            return cleanedText;
        }

        /*
        public static string CleanRichText(string text)
        {
            Stack<string> tagStack = new Stack<string>();
            string pattern = @"<(/?)(\w+)(?:=[^>]*)?>";

            string cleanedText = Regex.Replace(text, pattern, match =>
            {
                string fullTag = match.Value;
                bool isClosingTag = match.Groups[1].Value == "/";
                string tagName = match.Groups[2].Value.ToLower();

                if (tagName == "b" || tagName == "i" || tagName == "size" || tagName == "color" ||
                    tagName == "material" || tagName == "quad" || tagName == "line-height" || tagName == "voffset")
                {
                    if (isClosingTag)
                    {
                        if (tagStack.Count > 0 && tagStack.Peek() == tagName)
                        {
                            tagStack.Pop();
                        }
                        return fullTag;
                    }
                    else
                    {
                        tagStack.Push(tagName);
                        return fullTag;
                    }
                }

                return fullTag;
            });

            while (tagStack.Count > 0)
            {
                string tag = tagStack.Pop();
                cleanedText += $"</{tag}>";
            }

            return cleanedText;
        }
        */

        public static string RemoveRichText(string text)
        {
            string pattern = @"<[^>]+>";

            if (Regex.IsMatch(text, pattern))
            {
                return Regex.Replace(text, pattern, "");
            }

            return text;
        }
    }
}
#endif
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using UnityEditor.AnimatedValues;
using UnityEngine.Events;


namespace GabSith.WFT
{
    public class WFTEditor : EditorWindow
    {
        private const string SettingsUseHeaderKey = "SettingsUseHeaderKey";
        private const string GlobalFolderKey = "GlobalFolderKey";
        private const string DefaultFolderKey = "DefaultFolderKey";

        private const string HeaderColorKey = "HeaderColorKey";
        private const string ButtonsColorKey = "ButtonsColorKey";


        Color defaultHeaderColor = new Color(0.95f, 0.9f, 0.9f, 1f);
        Color defautButtonsColor = new Color(0.9f, 1.15f, 1.45f);


        //bool customizationFold.target = false;

        //bool folderFold = false;
        AnimBool folderFold = new AnimBool(false);
        AnimBool customizationFold = new AnimBool(false);
        bool menuEditorFold = false;
        private AnimBool myFoldout = new AnimBool(false);
        AnimBool drop = new AnimBool(false);

        Vector2 scrollPos;

        [MenuItem("GabSith/WFT Editor (Settings)", false, 2000)]


        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(WFTEditor), false, "WFT Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_SettingsIcon").image, text = "WFT Editor", tooltip = "♥" };
            w.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            drop.valueChanged.AddListener(new UnityAction(Repaint));
            customizationFold.valueChanged.AddListener(new UnityAction(Repaint));
            folderFold.valueChanged.AddListener(new UnityAction(Repaint));

        }


        void OnGUI()
        {
            GUIStyle label = new GUIStyle(EditorStyles.boldLabel) { };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true)) ;

            // Tittle
            CommonActions.GenerateTitle("WFT Editor");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);



            // Use Header
            bool useHeader = ProjectSettingsManager.EditorGUIBool(SettingsUseHeaderKey, "Use Header", true);
            /*
            EditorGUILayout.LabelField("Header", label);
            EditorGUI.BeginChangeCheck();
            ProjectSettingsManager.useHeader = EditorGUILayout.ToggleLeft("Use Header", ProjectSettingsManager.useHeader);
            // Or something with ProjectSettingsManager.GetBool(SettingsUseHeaderKey, true) ?
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettingsManager.SetBool(SettingsUseHeaderKey, ProjectSettingsManager.useHeader);
            }*/
            EditorGUILayout.Space(10);



            // Customization
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            customizationFold.target = EditorGUILayout.Foldout(customizationFold.target, "Customization");
            using (var group1 = new EditorGUILayout.FadeGroupScope(customizationFold.faded))
            {
                if (group1.visible)
                {
                    EditorGUILayout.BeginHorizontal();
                    ProjectSettingsManager.EditorGUIColor(HeaderColorKey, "Header", defaultHeaderColor);
                    if (GUILayout.Button("Reset", GUILayout.Width(70f)))
                    {
                        ProjectSettingsManager.SetColor(HeaderColorKey, defaultHeaderColor);
                    }
                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.ColorField(new GUIContent("Header"), Color.white, true, false, true);

                    EditorGUILayout.BeginHorizontal();
                    ProjectSettingsManager.EditorGUIColor(ButtonsColorKey, "Toggle Buttons", defautButtonsColor);
                    if (GUILayout.Button("Reset", GUILayout.Width(70f)))
                    {
                        ProjectSettingsManager.SetColor(ButtonsColorKey, defautButtonsColor);
                    }
                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.ColorField(new GUIContent("Buttons"), defautButtonsColor, true, false, true);

                    EditorGUILayout.Space();


                    /*
                    //drop.target = menuEditorFold;
                    drop.target = EditorGUILayout.Foldout(drop.target, "Menu Editor Colors");
                    using (var group = new EditorGUILayout.FadeGroupScope(drop.faded))
                    {
                        if (group.visible)
                        {
                            EditorGUILayout.ColorField(new GUIContent("Submenu"), Color.white, true, false, true);
                            EditorGUILayout.ColorField(new GUIContent("Toggle"), Color.white, true, false, true);
                            EditorGUILayout.ColorField(new GUIContent("Button"), Color.white, true, false, true);
                            EditorGUILayout.ColorField(new GUIContent("Radial"), Color.white, true, false, true);
                            EditorGUILayout.ColorField(new GUIContent("Puppets"), Color.white, true, false, true);

                            EditorGUILayout.Space();
                            EditorGUILayout.ColorField(new GUIContent("Create New"), Color.white, true, false, true);
                        }
                    }

                    
                    menuEditorFold = EditorGUILayout.Foldout(menuEditorFold, "Menu Editor Colors");
                    if (menuEditorFold)
                    {
                        EditorGUILayout.ColorField(new GUIContent("Submenu"), Color.white, true, false, true);
                        EditorGUILayout.ColorField(new GUIContent("Toggle"), Color.white, true, false, true);
                        EditorGUILayout.ColorField(new GUIContent("Button"), Color.white, true, false, true);
                        EditorGUILayout.ColorField(new GUIContent("Radial"), Color.white, true, false, true);
                        EditorGUILayout.ColorField(new GUIContent("Puppets"), Color.white, true, false, true);

                        EditorGUILayout.Space();
                        EditorGUILayout.ColorField(new GUIContent("Create New"), Color.white, true, false, true);
                    }*/
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);


            // Folders
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            folderFold.target = EditorGUILayout.Foldout(folderFold.target, "Folders");
            using (var group = new EditorGUILayout.FadeGroupScope(folderFold.faded))
            {
                if (group.visible)
                {
                    // Default Folder
                    EditorGUILayout.LabelField("Default Folder", label);
                    if (SelectFolder("Select Default Folder", DefaultFolderKey))
                    {
                        ProjectSettingsManager.defaultPath = ProjectSettingsManager.GetString(DefaultFolderKey, ProjectSettingsManager.defaultPath);
                    }
                    EditorGUILayout.Space(10);

                    // Global Folder
                    EditorGUILayout.LabelField("Global Folder", label);
                    SelectFolder("Select Global Folder", GlobalFolderKey);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);



            float height = 25f;
            
            /*
            // Save To System
            EditorGUILayout.LabelField("Save To System", label);
            GUILayout.Button("Save Settings To System", GUILayout.Height(height));
            EditorGUILayout.Space(10);*/


            // Delete Saved Settings
            EditorGUILayout.LabelField("Danger Zone", label);
            if (GUILayout.Button("Delete All Saved Settings", GUILayout.Height(height)))
            {
                ProjectSettingsManager.DeleteAllData();
            }
            //GUILayout.Button("Delete Saved To System Settings", GUILayout.Height(height));


            EditorGUILayout.Space(15f);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }



        bool SelectFolder(string buttonText, string key)
        {
            EditorGUILayout.BeginHorizontal();
            bool changed = false;

            if (GUILayout.Button(buttonText))
            {
                string tempPath = GetFolder(key);
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");

                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = tempPath;
                }

                int index = folderPath.IndexOf("Assets/");

                folderPath = folderPath.Substring(index);

                ProjectSettingsManager.SetString(key, folderPath);
                changed = true;
            }

            EditorGUILayout.EndHorizontal();


            GUIStyle pathLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Folder: " + GetFolder(key), pathLabelStyle);

            EditorGUILayout.EndHorizontal();
            return changed;
        }


        string GetFolder(string key)
        {
            return ProjectSettingsManager.GetString(key, ProjectSettingsManager.defaultPath);
        }

    }

}

#endif
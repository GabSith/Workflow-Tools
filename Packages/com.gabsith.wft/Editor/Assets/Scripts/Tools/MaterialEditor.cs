#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using VRC.SDK3.Avatars.Components;
//using VRC.SDK3.Avatars.ScriptableObjects;

using System.IO;
using System.Collections.Generic;



namespace GabSith.WFT
{
    public class MaterialEditor : EditorWindow
    {

        GameObject parent;

        bool converterMode;
        int selectedConvMode = 0;
        int selectedQuestShader = 0;

        string[] presets = new string[4] { "Off", "Active", "Quest", "VRM" };
        int preset;

        Vector2 scrollPosDescriptors;
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;


        Vector2 scrollPos;

        //private string defaultPath = "Assets/WF Tools - GabSith/Generated";
        //private string folderPath = "Assets/WF Tools - GabSith/Generated";

        private const string MaterialEditorFolderKey = "MaterialEditorFolderKey";
        private const string MaterialEditorUseGlobalKey = "MaterialEditorUseGlobalKey";
        private const string MaterialEditorFolderSuffixKey = "MaterialEditorFolderSuffixKey";
        string suffix;


        Color defaultColor;
        Color highlightHide = new Color(0.7f, 0.5f, 0.5f, 0.8f);
        Color selectedOption = new Color(0.5f, 0.8f, 0.5f);


        [MenuItem("GabSith/Niche/Material Editor", false, 1000)]


        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(MaterialEditor), false, "Material Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Material On Icon").image, text = "Material Editor", tooltip = "♥" };

        }

        private void OnEnable()
        {
            CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);
            suffix = ProjectSettingsManager.GetString(MaterialEditorFolderSuffixKey);

        }


        void OnGUI()
        {
            defaultColor = GUI.color;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Material Editor");

            CommonActions.FindAvatarsAsObjects(ref parent, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);

            //parent = EditorGUILayout.ObjectField("Avatar", parent, typeof(GameObject), true) as GameObject;

            if (parent != null)
            {
                List<Renderer> renderers = new List<Renderer> { };

                foreach (var item in parent.GetComponentsInChildren<Renderer>(true))
                {
                    renderers.Add(item);
                }

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);


                foreach (var item in renderers)
                {

                    List<Material> materials = new List<Material> { };


                    for (int i = 0; i < item.sharedMaterials.Length; i++)
                    {

                        switch (preset)
                        {
                            case 1:
                                if (!item.gameObject.activeInHierarchy)
                                    GUI.color = highlightHide;
                                break;
                            case 2:
                                if (!item.sharedMaterials[i].shader.name.StartsWith("VRChat/Mobile"))
                                    GUI.color = highlightHide;
                                break;
                            case 3:
                                if (!item.sharedMaterials[i].shader.name.StartsWith("VRM"))
                                    GUI.color = highlightHide;
                                break;

                            default:
                                break;
                        }

                        materials.Add(item.sharedMaterials[i]);

                        EditorGUILayout.BeginHorizontal(new GUIStyle(GUI.skin.box));

                        Material tempMaterial = materials[i];
                        tempMaterial = EditorGUILayout.ObjectField(materials[i], typeof(Material), true) as Material;
                        if (tempMaterial != materials[i])
                        {
                            materials[i] = tempMaterial;

                            if (materials.Count == item.sharedMaterials.Length)
                            {
                                item.sharedMaterials = materials.ToArray();
                                EditorUtility.SetDirty(item);
                            }

                        }
                        GUILayout.Label(" is found in ", GUILayout.Width(70));

                        EditorGUILayout.ObjectField(item, typeof(Renderer), true, GUILayout.Width(100));

                        EditorGUILayout.EndHorizontal();
                    }


                    GUI.color = defaultColor;
                }


                EditorGUILayout.EndScrollView();

            }


            EditorGUILayout.Space(10);

            GUILayout.Label("Highlight:");

            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < presets.Length; i++)
            {
                switch (preset)
                {
                    case 1:
                        if (presets[i] == "Active")
                            GUI.color = selectedOption;
                        break;
                    case 2:
                        if (presets[i] == "Quest")
                            GUI.color = selectedOption;
                        break;
                    case 3:
                        if (presets[i] == "VRM")
                            GUI.color = selectedOption;
                        break;

                    default:
                        break;
                }



                if (GUILayout.Button(presets[i]))
                    preset = i;

                GUI.color = defaultColor;
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            converterMode = EditorGUILayout.ToggleLeft("Material Conversion", converterMode);

            if (converterMode)
            {

                //Debug.Log(Shader.Find("VRM/MToon"));
                //Debug.Log(Shader.Find("VRChat/Mobile/Toon Lit"));
                //Debug.Log(Shader.Find("VRChat/Mobile/Standard Lite"));

                string[] selectedConvModes = new string[2] { "Quest", "VRM" };
                Shader selectedShader = null;
                selectedConvMode = GUILayout.Toolbar(selectedConvMode, selectedConvModes);

                switch (selectedConvMode)
                {
                    case 0:
                        EditorGUILayout.Space();
                        selectedQuestShader = EditorGUILayout.Popup(selectedQuestShader, new string[2] { "Toon Lit", "Standard Lite" });
                        switch (selectedQuestShader)
                        {
                            case 0:
                                selectedShader = Shader.Find("VRChat/Mobile/Toon Lit");
                                break;
                            case 1:
                                selectedShader = Shader.Find("VRChat/Mobile/Standard Lite");
                                break;
                            default:
                                break;
                        }
                        break;
                    case 1:
                        EditorGUILayout.Space();
                        selectedQuestShader = EditorGUILayout.Popup(selectedQuestShader, new string[2] { "MToon", "MToon10" });
                        switch (selectedQuestShader)
                        {
                            case 0:
                                selectedShader = Shader.Find("VRM/MToon");
                                break;
                            case 1:
                                selectedShader = Shader.Find("VRM10/MToon10");
                                break;
                            default:
                                break;
                        }


                        break;

                    default:
                        break;
                }

                // TODO: Give the option to select which shader per material. Have a button to set all materials to the same shader
                // Duplicate the material to the set folder, then change the shader to the selected one
                // Create a copy of the avatar in the hierarchy. get the path to the material from the original one and change them to the new one
                // For VRM specially, use "if (Shader.Find("VRM/MToon") != null" first)


                // Folder


                EditorGUILayout.Space();

                CommonActions.SelectFolder(MaterialEditorUseGlobalKey, MaterialEditorFolderKey, MaterialEditorFolderSuffixKey, ref suffix);

                EditorGUILayout.Space(10);
                if (GUILayout.Button("Create Materials", new GUIStyle(GUI.skin.button) { fixedHeight = 35, fontSize = 13 }))
                {
                    if (selectedShader != null)
                        CreateMaterials(parent, selectedConvModes[selectedConvMode], selectedShader);
                    else
                    {
                        Debug.LogError("Shader not found!");
                    }

                }

            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();
        }


        void CreateMaterials(GameObject original, string mode, Shader shader)
        {
            GameObject avatarClone = GameObject.Instantiate(original);
            avatarClone.transform.position += new Vector3(-1, 0, 0);
            avatarClone.name = parent.name + " " + mode;

            List<Renderer> renderers = new List<Renderer> { };

            foreach (var item in avatarClone.GetComponentsInChildren<Renderer>(true))
            {
                renderers.Add(item);
            }

            foreach (var item in renderers)
            {
                List<Material> materials = new List<Material> { };

                for (int i = 0; i < item.sharedMaterials.Length; i++)
                {
                    if (item.sharedMaterials[i].shader != shader)
                    {
                        string path = GetFolder() + "/" + item.sharedMaterials[i].name + " " + mode + ".mat";
                        Directory.CreateDirectory(GetFolder());
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(item.sharedMaterials[i]), path);
                        //AssetDatabase.CreateAsset(item.sharedMaterials[i], path);
                        AssetDatabase.ImportAsset(path);

                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                        mat.shader = shader;

                        materials.Add(mat);

                        if (materials.Count == item.sharedMaterials.Length)
                        {
                            item.sharedMaterials = materials.ToArray();
                            MakeSureItDoesTheThing(item);
                        }
                    }
                }
            }
        }

        string GetFolder()
        {
            return CommonActions.GetFolder(MaterialEditorUseGlobalKey, MaterialEditorFolderKey) + "/" + suffix;
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
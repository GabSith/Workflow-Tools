#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


using System.IO;
using System.Collections.Generic;




namespace GabSith.WFT
{
    public class BlendshapeAnimationEditor : EditorWindow
    {
        string animationName;

        bool useExistingAnimation = false;
        AnimationClip existingAnimation;

        //private string defaultPath = "Assets/WF Tools - GabSith/Generated";
        //private string folderPath = "Assets/WF Tools - GabSith/Generated";

        private const string BlendshapeAnimFolderKey = "BlendshapeAnimFolderKey";
        private const string BlendshapeAnimUseGlobalKey = "BlendshapeAnimUseGlobalKey";
        private const string BlendshapeAnimFolderSuffixKey = "BlendshapeAnimFolderSuffixKey";
        string suffix;


        GameObject parent;
        SkinnedMeshRenderer skinnedMeshRenderer;
        Mesh skinnedMesh;
        int blendShapeCount;
        string[] blendShapeNames;
        float[] blendShapeWeights;
        bool[] isBlendShapeActive = new bool[0];

        Vector2 scrollPosExtraBlendshapes;


        private List<SkinnedMeshRenderer> extraSkinnedMeshRenderers = new List<SkinnedMeshRenderer> { };
        private List<string[]> blendShapeNamesList = new List<string[]> { };
        private List<float[]> blendShapeWeightsList = new List<float[]> { };
        private List<bool[]> isBlendShapeActiveList = new List<bool[]> { };
        private List<Vector2> scrollPosList = new List<Vector2> { };


        Vector2 scrollPos;


        [MenuItem("GabSith/Niche/Blendshape Animation Creator", false, 900)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(BlendshapeAnimationEditor), false, "Blendshape Animation Creator");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("editcollision_16@2x").image, text = "Blendshape Animations", tooltip = "♥" };
        }

        private void OnEnable()
        {
            suffix = ProjectSettingsManager.GetString(BlendshapeAnimFolderSuffixKey);
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Blendshape Animation Creator");


            parent = EditorGUILayout.ObjectField("Parent Game Object", parent, typeof(GameObject), true) as GameObject;


            scrollPosExtraBlendshapes = EditorGUILayout.BeginScrollView(scrollPosExtraBlendshapes, GUILayout.ExpandHeight(false));

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("+", GUILayout.Width(20f)))
            {
                extraSkinnedMeshRenderers.Add(null);
                blendShapeNamesList.Add(new string[] { });
                blendShapeWeightsList.Add(new float[] { });
                isBlendShapeActiveList.Add(new bool[] { });
                scrollPosList.Add(new Vector2 { });
            }
            EditorGUILayout.LabelField("Mesh", GUILayout.MaxWidth(125f));
            skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                if (parent == null && skinnedMeshRenderer != null)
                {
                    parent = skinnedMeshRenderer.transform.root.gameObject;
                }
            }
            EditorGUILayout.EndHorizontal();






            //skinnedMeshRenderer = EditorGUILayout.ObjectField("Mesh", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;

            if (skinnedMeshRenderer != null)
            {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                skinnedMesh = skinnedMeshRenderer.sharedMesh;
                blendShapeCount = skinnedMesh.blendShapeCount;

                if (EditorGUI.EndChangeCheck() || (blendShapeCount != isBlendShapeActive.Length))
                {
                    isBlendShapeActive = new bool[blendShapeCount];
                }


                if (blendShapeNames == null || blendShapeNames.Length != blendShapeCount)
                {
                    blendShapeNames = new string[blendShapeCount];
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        blendShapeNames[i] = skinnedMesh.GetBlendShapeName(i);
                    }
                }

                if (blendShapeWeights == null || blendShapeWeights.Length != blendShapeCount)
                {
                    blendShapeWeights = new float[blendShapeCount];
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        blendShapeWeights[i] = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    }
                }
                if (blendShapeNames.Length != 0)
                {
                    EditorGUILayout.LabelField("Blend Shapes", EditorStyles.boldLabel);


                    //EditorGUI.BeginChangeCheck();

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(false));

                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            isBlendShapeActive[i] = EditorGUILayout.Toggle(isBlendShapeActive[i], GUILayout.MaxWidth(20f));
                            EditorGUILayout.LabelField(blendShapeNames[i], GUILayout.MaxWidth(130f));
                            blendShapeWeights[i] = EditorGUILayout.Slider(blendShapeWeights[i], 0f, 100f);
                        }
                    }


                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Toggle All"))
                        {
                            bool isOneOff = false;
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                if (isBlendShapeActive[i] == false)
                                {
                                    isOneOff = true;
                                    break;
                                }
                            }
                            if (isOneOff)
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    isBlendShapeActive[i] = true;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    isBlendShapeActive[i] = false;
                                }
                            }
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                isBlendShapeActive[i] = false;
                                //skinnedMeshRenderer.SetBlendShapeWeight(i, 0);
                                //blendShapeWeights[i] = skinnedMeshRenderer.GetBlendShapeWeight(i);
                                blendShapeWeights[i] = 0;
                            }
                        }
                    }

                }
            }


            // Extra meshes
            if (extraSkinnedMeshRenderers != null && extraSkinnedMeshRenderers.Count > 0)
            {
                for (int j = 0; j < extraSkinnedMeshRenderers.Count; j++)
                {
                    if ((extraSkinnedMeshRenderers[j] != null && parent != null) && ((!extraSkinnedMeshRenderers[j].transform.IsChildOf(parent.transform)) || (extraSkinnedMeshRenderers[j].transform == parent.transform)))
                    {
                        extraSkinnedMeshRenderers[j] = null;
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("-", GUILayout.Width(20f)))
                    {
                        extraSkinnedMeshRenderers.RemoveAt(j);
                        blendShapeNamesList.RemoveAt(j);
                        blendShapeWeightsList.RemoveAt(j);
                        isBlendShapeActiveList.RemoveAt(j);
                        scrollPosList.RemoveAt(j);
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                    EditorGUILayout.LabelField("Mesh #" + (j + 2), GUILayout.MaxWidth(125f));
                    extraSkinnedMeshRenderers[j] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("", extraSkinnedMeshRenderers[j], typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();



                    if (extraSkinnedMeshRenderers[j] != null)
                    {
                        int num = j;

                        EditorGUILayout.Space();
                        Mesh skinnedMesh = extraSkinnedMeshRenderers[j].sharedMesh;
                        int blendShapeCount = skinnedMesh.blendShapeCount;

                        //Debug.Log(isBlendShapeActiveList.Count);


                        if ((blendShapeCount != isBlendShapeActiveList[num].Length))
                        {
                            isBlendShapeActiveList[num] = new bool[blendShapeCount];
                        }


                        if (blendShapeNamesList[num] == null || blendShapeNamesList[num].Length != blendShapeCount)
                        {
                            blendShapeNamesList[num] = new string[blendShapeCount];
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                blendShapeNamesList[num][i] = skinnedMesh.GetBlendShapeName(i);
                            }
                        }

                        if (blendShapeWeightsList[num] == null || blendShapeWeightsList[num].Length != blendShapeCount)
                        {
                            blendShapeWeightsList[num] = new float[blendShapeCount];
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                blendShapeWeightsList[num][i] = extraSkinnedMeshRenderers[j].GetBlendShapeWeight(i);
                            }
                        }
                        if (blendShapeNamesList[num].Length != 0)
                        {
                            EditorGUILayout.LabelField("Blend Shapes", EditorStyles.boldLabel);


                            //EditorGUI.BeginChangeCheck();

                            scrollPosList[num] = EditorGUILayout.BeginScrollView(scrollPosList[num], GUILayout.ExpandHeight(false));

                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    isBlendShapeActiveList[num][i] = EditorGUILayout.Toggle(isBlendShapeActiveList[num][i], GUILayout.MaxWidth(20f));
                                    EditorGUILayout.LabelField(blendShapeNamesList[num][i], GUILayout.MaxWidth(130f));
                                    blendShapeWeightsList[num][i] = EditorGUILayout.Slider(blendShapeWeightsList[num][i], 0f, 100f);
                                }
                            }


                            EditorGUILayout.EndScrollView();

                            EditorGUILayout.Space();

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Toggle All"))
                                {
                                    bool isOneOff = false;
                                    for (int i = 0; i < blendShapeCount; i++)
                                    {
                                        if (isBlendShapeActiveList[num][i] == false)
                                        {
                                            isOneOff = true;
                                            break;
                                        }
                                    }
                                    if (isOneOff)
                                    {
                                        for (int i = 0; i < blendShapeCount; i++)
                                        {
                                            isBlendShapeActiveList[num][i] = true;
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < blendShapeCount; i++)
                                        {
                                            isBlendShapeActiveList[num][i] = false;
                                        }
                                    }
                                }

                                if (GUILayout.Button("Reset"))
                                {
                                    for (int i = 0; i < blendShapeCount; i++)
                                    {
                                        isBlendShapeActiveList[num][i] = false;
                                        //extraSkinnedMeshRenderers[num].SetBlendShapeWeight(i, 0);
                                        blendShapeWeightsList[num][i] = extraSkinnedMeshRenderers[num].GetBlendShapeWeight(i);
                                    }
                                }
                            }
                        }
                    }





                }
            }

            EditorGUILayout.EndScrollView();


            EditorGUILayout.Space(20);

            useExistingAnimation = EditorGUILayout.ToggleLeft("Add to existing animation", useExistingAnimation);
            EditorGUILayout.Space();
            if (useExistingAnimation)
            {
                existingAnimation = EditorGUILayout.ObjectField("Animation", existingAnimation, typeof(AnimationClip), true, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as AnimationClip;

            }
            else
            {
                animationName = EditorGUILayout.TextField("Animation Name", animationName);




                EditorGUILayout.Space(10);

                // Select Folder
                if (!useExistingAnimation)
                {
                    CommonActions.SelectFolder(BlendshapeAnimUseGlobalKey, BlendshapeAnimFolderKey, BlendshapeAnimFolderSuffixKey, ref suffix);
                }

            }
            EditorGUILayout.Space();


            if (!RequirementsMet())
                GUI.enabled = false;

            // Custom style for the Create Toggle button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15, // Increase the font size
                fixedHeight = 35,

            };
            // Use buttons to create the toggle and the animation clip with a custom style
            buttonStyle.fixedHeight = 35; // Increase the button height

            //if (GUILayout.Button("Create Animation", buttonStyle))
            if (GUILayout.Button(useExistingAnimation ? "Add To Animation" : "Create Animation", buttonStyle))
            {
                if (useExistingAnimation)
                    AddToAnimationClip(existingAnimation);

                else
                    CreateAnimationClip();
            }

            // Use a space to separate the fields
            EditorGUILayout.Space();

            // End the vertical layout group
            EditorGUILayout.EndVertical();

        }

        void AddToAnimationClip(AnimationClip clip)
        {
            AddAnimationCurve(clip, skinnedMeshRenderer, isBlendShapeActive, blendShapeWeights);

            if (extraSkinnedMeshRenderers.Count > 0)
            {
                for (int i = 0; i < extraSkinnedMeshRenderers.Count; i++)
                {
                    AddAnimationCurve(clip, extraSkinnedMeshRenderers[i], isBlendShapeActiveList[i], blendShapeWeightsList[i]);
                }
            }

            //AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GetAssetPath(clip));

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
            MakeSureItDoesTheThing(clip);
        }

        void CreateAnimationClip()
        {
            AnimationClip clip = new AnimationClip { name = animationName };

            AddAnimationCurve(clip, skinnedMeshRenderer, isBlendShapeActive, blendShapeWeights);

            if (extraSkinnedMeshRenderers.Count > 0)
            {
                for (int i = 0; i < extraSkinnedMeshRenderers.Count; i++)
                {
                    AddAnimationCurve(clip, extraSkinnedMeshRenderers[i], isBlendShapeActiveList[i], blendShapeWeightsList[i]);
                }
            }

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(clip, GetFolder() + "/" + clip.name + ".anim");

            EditorGUIUtility.PingObject(clip);

            MakeSureItDoesTheThing(clip);
        }
        void AddAnimationCurve(AnimationClip clip, SkinnedMeshRenderer skinnedMeshRenderer, bool[] isBlendShapeActive, float[] blendShapeWeights)
        {
            if (skinnedMeshRenderer == null) return;
            if (string.IsNullOrEmpty(animationName) && !useExistingAnimation) return;


            Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
            int blendShapeCount = skinnedMesh.blendShapeCount;



            for (int i = 0; i < blendShapeCount; i++)
            {
                if (isBlendShapeActive[i])
                {
                    string blendShapeName = skinnedMesh.GetBlendShapeName(i);
                    string propertyName = "blendShape." + blendShapeName;
                    AnimationCurve curve = new AnimationCurve();

                    float time = 0f;

                    Keyframe keyframe = new Keyframe(time, blendShapeWeights[i]);
                    curve.AddKey(keyframe);

                    clip.SetCurve(GetPathToObject(skinnedMeshRenderer.transform), typeof(SkinnedMeshRenderer), propertyName, curve);

                }
            }
        }

        string GetPathToObject(Transform gameObject)
        {
            if (parent != null)
                return (VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject, parent.transform));
            else
                return "";
        }




        void RestOfUI(SkinnedMeshRenderer skinnedMeshRenderer, int num)
        {
            if (skinnedMeshRenderer != null)
            {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                skinnedMesh = skinnedMeshRenderer.sharedMesh;
                blendShapeCount = skinnedMesh.blendShapeCount;

                if (EditorGUI.EndChangeCheck() || (blendShapeCount != isBlendShapeActive.Length))
                {
                    isBlendShapeActive = new bool[blendShapeCount];
                }


                if (blendShapeNames == null || blendShapeNames.Length != blendShapeCount)
                {
                    blendShapeNames = new string[blendShapeCount];
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        blendShapeNames[i] = skinnedMesh.GetBlendShapeName(i);
                    }
                }

                if (blendShapeWeights == null || blendShapeWeights.Length != blendShapeCount)
                {
                    blendShapeWeights = new float[blendShapeCount];
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        blendShapeWeights[i] = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    }
                }
                if (blendShapeNames.Length != 0)
                {
                    EditorGUILayout.LabelField("Blend Shapes", EditorStyles.boldLabel);


                    //EditorGUI.BeginChangeCheck();

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(false));

                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            isBlendShapeActive[i] = EditorGUILayout.Toggle(isBlendShapeActive[i], GUILayout.MaxWidth(20f));
                            EditorGUILayout.LabelField(blendShapeNames[i], GUILayout.MaxWidth(130f));
                            blendShapeWeights[i] = EditorGUILayout.Slider(blendShapeWeights[i], 0f, 100f);
                        }
                    }


                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Toggle All"))
                        {
                            bool isOneOff = false;
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                if (isBlendShapeActive[i] == false)
                                {
                                    isOneOff = true;
                                    break;
                                }
                            }
                            if (isOneOff)
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    isBlendShapeActive[i] = true;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    isBlendShapeActive[i] = false;
                                }
                            }
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            for (int i = 0; i < blendShapeCount; i++)
                            {
                                isBlendShapeActive[i] = false;
                                //skinnedMeshRenderer.SetBlendShapeWeight(i, 0);
                                blendShapeWeights[i] = skinnedMeshRenderer.GetBlendShapeWeight(i);
                            }
                        }
                    }

                }
            }

        }

        bool RequirementsMet()
        {

            if (skinnedMeshRenderer == null)
            {
                //EditorGUILayout.HelpBox("Object to toggle cannot be null", MessageType.Error);
                //EditorGUILayout.Space();
                return false;
            }

            if (string.IsNullOrEmpty(animationName) && !useExistingAnimation)
            {
                return false;
            }
            else if (useExistingAnimation && existingAnimation == null)
            {
                return false;
            }

            bool isOneOn = false;
            for (int i = 0; i < blendShapeCount; i++)
            {
                if (isBlendShapeActive[i] == true)
                {
                    isOneOn = true;
                    break;
                }
            }
            if (!isOneOn)
            {
                return false;
            }


            return true;
        }

        string GetFolder()
        {
            return CommonActions.GetFolder(BlendshapeAnimUseGlobalKey, BlendshapeAnimFolderKey) + "/" + suffix;
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
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;

using System.IO;
using System.Collections.Generic;

using UnityEditor.AnimatedValues;
using UnityEngine.Events;


namespace GabSith.WFT
{
    public class BlendshapeAnimationEditor : EditorWindow
    {
        string animationName;

        bool useExistingAnimation = false;
        AnimationClip existingAnimation;

        //bool secondKeyframeFold = false;
        AnimBool secondKeyframeFold = new AnimBool(false);

        int selectedFrame = 0;
        int keyframePosition = 1;
        bool useSingleFrame = false;
        bool equalKeyframes = true;
        float[] secondBlendShapeWeights;
        private List<float[]> secondBlendShapeWeightsList = new List<float[]> { };

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

        string find = "";
        private List<string> findList = new List<string> { };


        Vector2 scrollPosBlends;

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
            secondKeyframeFold.valueChanged.AddListener(new UnityAction(Repaint));
        }

        void OnGUI()
        {
            GUI.SetNextControlName("NotText");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

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
                secondBlendShapeWeightsList.Add(new float[] { });

                isBlendShapeActiveList.Add(new bool[] { });
                scrollPosList.Add(new Vector2 { });

                findList.Add("");
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


            BlendshapeListUI(ref skinnedMesh, ref skinnedMeshRenderer, ref blendShapeCount,
                ref isBlendShapeActive, ref blendShapeNames, ref blendShapeWeights, ref secondBlendShapeWeights, ref scrollPosBlends, ref find);


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
                        secondBlendShapeWeightsList.RemoveAt(j);
                        isBlendShapeActiveList.RemoveAt(j);
                        scrollPosList.RemoveAt(j);

                        findList.RemoveAt(j);

                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                    EditorGUILayout.LabelField("Mesh #" + (j + 2), GUILayout.MaxWidth(125f));
                    extraSkinnedMeshRenderers[j] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("", extraSkinnedMeshRenderers[j], typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    if (extraSkinnedMeshRenderers[j] != null)
                    {
                        if (extraSkinnedMeshRenderers.Count != isBlendShapeActiveList.Count)
                        {
                            Debug.Log("Extra meshes discrepancy");
                            blendShapeNamesList.Add(new string[] { });
                            blendShapeWeightsList.Add(new float[] { });
                            secondBlendShapeWeightsList.Add(new float[] { });

                            isBlendShapeActiveList.Add(new bool[] { });
                            scrollPosList.Add(new Vector2 { });

                            findList.Add(""); 
                        }

                        int num = j;
                        Mesh skinnedMesh = extraSkinnedMeshRenderers[num].sharedMesh;
                        int blendShapeCount = skinnedMesh.blendShapeCount;
                        SkinnedMeshRenderer skinnedMeshRenderer = extraSkinnedMeshRenderers[num];
                        bool[] isBlendShapeActive = isBlendShapeActiveList[num];
                        string[] blenshapeNames = blendShapeNamesList[num];
                        float[] blendShapeWeights = blendShapeWeightsList[num];
                        float[] secondBlendShapeWeights = secondBlendShapeWeightsList[num];
                        Vector2 scrollPos = scrollPosList[num];
                        string find = findList[num];

                        BlendshapeListUI(ref skinnedMesh, ref skinnedMeshRenderer, ref blendShapeCount, ref isBlendShapeActive,
                            ref blenshapeNames, ref blendShapeWeights, ref secondBlendShapeWeights, ref scrollPos, ref find);

                        extraSkinnedMeshRenderers[j].sharedMesh = skinnedMesh;
                        extraSkinnedMeshRenderers[num] = skinnedMeshRenderer;
                        isBlendShapeActiveList[num] = isBlendShapeActive;
                        blendShapeWeightsList[num] = blendShapeWeights;
                        secondBlendShapeWeightsList[num] = secondBlendShapeWeights;
                        scrollPosList[num] = scrollPos;
                        findList[num] = find;
                    }
                }
            }

            EditorGUILayout.EndScrollView();


            // Second Frame
            EditorGUILayout.Space(15);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                //secondKeyframeFold.target = EditorGUILayout.BeginFoldoutHeaderGroup(secondKeyframeFold.target, "Second Keyframe");

                secondKeyframeFold.target = EditorGUILayout.Foldout(secondKeyframeFold.target, "Second Keyframe");
                using var group = new EditorGUILayout.FadeGroupScope(secondKeyframeFold.faded);
                if (group.visible)
                {
                    EditorGUILayout.Space();
                    if (useSingleFrame || equalKeyframes)
                    {
                        selectedFrame = 0;
                        GUI.enabled = false;
                    }
                    EditorGUI.BeginChangeCheck();
                    selectedFrame = GUILayout.Toolbar(selectedFrame, new string[2] { "First Keyframe", "Second Keyframe" }, GUILayout.Height(25f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        GUI.FocusControl("NotText");
                    }
                    GUI.enabled = true;


                    useSingleFrame = EditorGUILayout.ToggleLeft("Use single keyframe", useSingleFrame);
                    if (!useSingleFrame)
                    {

                        equalKeyframes = EditorGUILayout.ToggleLeft("Both keyframes are the same", equalKeyframes);

                        EditorGUILayout.Space(5);

                        keyframePosition = EditorGUILayout.IntField("Second key on frame", keyframePosition);
                        if (keyframePosition < 1)
                            keyframePosition = 1;

                        EditorGUILayout.Space();

                        if (!equalKeyframes)
                        {
                            if (GUILayout.Button("Copy values from first to second key", GUILayout.Height(25f)))
                            {
                                GUI.FocusControl("NotText");
                                CopyValues();
                            }
                        }
                    }
                    EditorGUILayout.Space();
                }

            }

            // Add to existing animation
            EditorGUILayout.Space(15);

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


            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fixedHeight = 35,
            };

            if (GUILayout.Button(useExistingAnimation ? "Add To Animation" : "Create Animation", buttonStyle))
            {
                if (useExistingAnimation)
                    AddToAnimationClip(existingAnimation);

                else
                    CreateAnimationClip();
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();


            EditorGUILayout.EndVertical();
        }

        void BlendshapeListUI(ref Mesh skinnedMesh, ref SkinnedMeshRenderer skinnedMeshRenderer, ref int blendShapeCount, ref bool[] isBlendShapeActive, ref string[] blendShapeNames,
            ref float[] blendShapeWeights, ref float[] secondBlendShapeWeights, ref Vector2 scrollPos, ref string find)
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
                    secondBlendShapeWeights = new float[blendShapeCount];

                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        blendShapeWeights[i] = skinnedMeshRenderer.GetBlendShapeWeight(i);
                        secondBlendShapeWeights[i] = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    }
                }
                if (blendShapeNames.Length != 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Blend Shapes", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(15f));
                        find = EditorGUILayout.TextField(find, GUILayout.MinWidth(25));
                    }
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(false));

                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        if (Search(blendShapeNames[i], find))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                isBlendShapeActive[i] = EditorGUILayout.Toggle(isBlendShapeActive[i], GUILayout.MaxWidth(20f));
                                EditorGUILayout.LabelField(blendShapeNames[i], GUILayout.MaxWidth(130f));

                                if (selectedFrame == 0 || equalKeyframes)
                                    blendShapeWeights[i] = EditorGUILayout.Slider(blendShapeWeights[i], 0f, 100f);
                                else
                                {
                                    secondBlendShapeWeights[i] = EditorGUILayout.Slider(secondBlendShapeWeights[i], 0f, 100f);
                                }
                            }
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

                        if (GUILayout.Button("Set all to 100"))
                        {
                            if (selectedFrame == 0 || equalKeyframes)
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    blendShapeWeights[i] = 100;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    secondBlendShapeWeights[i] = 100;
                                }
                            }
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            if (selectedFrame == 0 || equalKeyframes)
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    isBlendShapeActive[i] = false;
                                    blendShapeWeights[i] = 0;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    isBlendShapeActive[i] = false;
                                    secondBlendShapeWeights[i] = 0;
                                }
                            }
                        }
                    }
                }
            }

        }

        void AddToAnimationClip(AnimationClip clip)
        {
            AddAnimationCurve(clip, skinnedMeshRenderer, isBlendShapeActive, blendShapeWeights, secondBlendShapeWeights);

            if (extraSkinnedMeshRenderers.Count > 0)
            {
                for (int i = 0; i < extraSkinnedMeshRenderers.Count; i++)
                {
                    AddAnimationCurve(clip, extraSkinnedMeshRenderers[i], isBlendShapeActiveList[i], blendShapeWeightsList[i], secondBlendShapeWeightsList[i]);
                }
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
            MakeSureItDoesTheThing(clip);
        }

        void CreateAnimationClip()
        {
            AnimationClip clip = new AnimationClip { name = animationName };

            AddAnimationCurve(clip, skinnedMeshRenderer, isBlendShapeActive, blendShapeWeights, secondBlendShapeWeights);

            if (extraSkinnedMeshRenderers.Count > 0)
            {
                for (int i = 0; i < extraSkinnedMeshRenderers.Count; i++)
                {
                    AddAnimationCurve(clip, extraSkinnedMeshRenderers[i], isBlendShapeActiveList[i], blendShapeWeightsList[i], secondBlendShapeWeightsList[i]);
                }
            }

            Directory.CreateDirectory(GetFolder());
            AssetDatabase.CreateAsset(clip, GetFolder() + "/" + clip.name + ".anim");

            EditorGUIUtility.PingObject(clip);

            MakeSureItDoesTheThing(clip);
        }
        void AddAnimationCurve(AnimationClip clip, SkinnedMeshRenderer skinnedMeshRenderer, bool[] isBlendShapeActive, float[] blendShapeWeights, float[] secondBlendShapeWeights)
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
                    float timeSF = keyframePosition / 60f;

                    Keyframe keyframe = new Keyframe(time, blendShapeWeights[i]);
                    curve.AddKey(keyframe);

                    // Second Keyframe
                    if (!useSingleFrame)
                    {
                        if (equalKeyframes)
                        {
                            Keyframe secondKeyframe = new Keyframe(timeSF, blendShapeWeights[i]);
                            curve.AddKey(secondKeyframe);
                        }
                        else
                        {
                            Keyframe secondKeyframe = new Keyframe(timeSF, secondBlendShapeWeights[i]);
                            curve.AddKey(secondKeyframe);
                        }
                    }

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

        bool Search(string name, string find)
        {
            if (!string.IsNullOrEmpty(find) && find != " " && !(name.IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return false;
            }
            return true;

        }

        void CopyValues()
        {
            for (int i = 0; i < blendShapeWeights.Length; i++)
            {
                secondBlendShapeWeights[i] = blendShapeWeights[i]; 
            }

            // Extra Meshes

            if (extraSkinnedMeshRenderers != null && extraSkinnedMeshRenderers.Count > 0)
            {
                for (int j = 0; j < extraSkinnedMeshRenderers.Count; j++)
                {
                    if (extraSkinnedMeshRenderers[j] != null)
                    {
                        for (int i = 0; i < blendShapeWeightsList[j].Length; i++)
                        {
                            secondBlendShapeWeightsList[j][i] = blendShapeWeightsList[j][i];
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
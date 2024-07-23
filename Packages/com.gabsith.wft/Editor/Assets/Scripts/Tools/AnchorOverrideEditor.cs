#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

//using VRC.SDK3.Avatars.Components;
//using VRC.SDK3.Avatars.ScriptableObjects;

using System.IO;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;


namespace GabSith.WFT
{
    public class AnchorOverrideEditor : EditorWindow
    {
        SerializedObject so;
        SerializedProperty _parent;
        SerializedProperty _newAnchor;


        Vector2 scrollPosDescriptors;
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        [SerializeField]
        GameObject parent;
        [SerializeField]
        Transform newAnchor = null;

        Vector2 scrollPos;

        [MenuItem("GabSith/Anchor Override Editor", false, 2)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(AnchorOverrideEditor), false, "Anchor Override Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_editconstraints_16@2x").image, text = "Anchor Override Editor", tooltip = "♥" };

        }

        private void OnEnable()
        {
            so = new SerializedObject(this);
            InitializeSerializedProperties();

            CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

            if (avatarDescriptorsFromScene.Length == 1)
            {
                parent = avatarDescriptorsFromScene[0].gameObject;
            }
        }

        private void InitializeSerializedProperties()
        {
            _parent = so.FindProperty("parent");
            _newAnchor = so.FindProperty("newAnchor");


        }
        void OnGUI()
        {
            so.Update();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Anchor Override Editor");


            //parent = EditorGUILayout.ObjectField("Avatar", parent, typeof(GameObject), true) as GameObject;

            // Avatar Selection
            CommonActions.FindAvatarsAsObjects(ref parent, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);



            if (parent != null)
            {
                List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer> { };

                foreach (var item in parent.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderers.Add(item);
                }

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(false));


                foreach (var item in renderers)
                {

                    EditorGUILayout.BeginHorizontal(new GUIStyle(GUI.skin.box));

                    Transform tempItem = item.probeAnchor;
                    tempItem = EditorGUILayout.ObjectField(item.probeAnchor, typeof(Transform), true) as Transform;
                    if (tempItem != item.probeAnchor)
                    {
                        item.probeAnchor = tempItem;
                        EditorUtility.SetDirty(item);
                    }

                    GUILayout.Label(" is found in ", GUILayout.Width(70));

                    EditorGUILayout.ObjectField(item, typeof(SkinnedMeshRenderer), true, GUILayout.Width(100));

                    EditorGUILayout.EndHorizontal();

                }


                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(20);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Set all to:", GUILayout.Width(70));
                //newAnchor = EditorGUILayout.ObjectField(newAnchor, typeof(Transform), true) as Transform;

                EditorGUILayout.PropertyField(_newAnchor, new GUIContent(""));



                if (GUILayout.Button("Set"))
                {
                    foreach (var item in renderers)
                    {
                        item.probeAnchor = newAnchor;

                        EditorUtility.SetDirty(item);
                    }

                }

                EditorGUILayout.EndHorizontal();

                Animator animator = parent.GetComponent<Animator>();
                if (animator != null && animator.isHuman)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Presets:", GUILayout.Width(70));
                    if (GUILayout.Button("Hips"))
                    {
                        _newAnchor.objectReferenceValue = animator.GetBoneTransform(HumanBodyBones.Hips);
                    }
                    if (GUILayout.Button("Spine"))
                    {
                        _newAnchor.objectReferenceValue = animator.GetBoneTransform(HumanBodyBones.Spine);
                    }
                    if (GUILayout.Button("Chest"))
                    {
                        _newAnchor.objectReferenceValue = animator.GetBoneTransform(HumanBodyBones.Chest);
                    }
                    if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
                    {
                        if (GUILayout.Button("U. Chest"))
                        {
                            _newAnchor.objectReferenceValue = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                so.ApplyModifiedProperties();

                EditorGUILayout.EndVertical();


            }

            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();

        }


    }

}

    #endif
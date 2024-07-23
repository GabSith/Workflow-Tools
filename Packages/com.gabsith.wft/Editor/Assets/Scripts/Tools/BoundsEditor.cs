#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

using System.IO;
using System.Collections.Generic;



namespace GabSith.WFT
{
    public class BoundsEditor : EditorWindow
    {
        GameObject parent;
        //Transform newAnchor = null;
        Vector2 scrollPosDescriptors;
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        Vector3 newBoundsCenter;
        Vector3 newBoundsExtent = new Vector3(1, 1, 1);

        Vector2 scrollPos;


        [MenuItem("GabSith/Bounds Editor", false, 2)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(BoundsEditor), false, "Bounds Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Grid.BoxTool@2x").image, text = "Bounds Editor", tooltip = "♥" };

        }

        private void OnEnable()
        {
            CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

            if (avatarDescriptorsFromScene.Length == 1)
            {
                parent = avatarDescriptorsFromScene[0].gameObject;
            }
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Bounds Editor");

            // Avatar Selection
            CommonActions.FindAvatarsAsObjects(ref parent, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);
            //parent = EditorGUILayout.ObjectField("Avatar", parent, typeof(GameObject), true) as GameObject;

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
                    

                    EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));

                    //EditorGUILayout.ObjectField(item, typeof(SkinnedMeshRenderer), true, GUILayout.Width(100));
                    EditorGUILayout.ObjectField(item, typeof(SkinnedMeshRenderer), true);


                    EditorGUILayout.BeginHorizontal();
                    Vector3 center = item.localBounds.center;
                    GUILayout.Label("Center", GUILayout.Width(70f));
                    center = EditorGUILayout.Vector3Field("", center, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    Vector3 extent = item.localBounds.extents;
                    GUILayout.Label("Extent", GUILayout.Width(70f));
                    extent = EditorGUILayout.Vector3Field("", extent, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.EndHorizontal();

                    item.localBounds = new Bounds { center = center, extents = extent };


                    EditorGUILayout.EndVertical();
                }


                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);


                EditorGUILayout.Space(10);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("Set all to:", GUILayout.Width(70));

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Center", GUILayout.Width(70f));
                newBoundsCenter = EditorGUILayout.Vector3Field("", newBoundsCenter);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Extent", GUILayout.Width(70f));
                newBoundsExtent = EditorGUILayout.Vector3Field("", newBoundsExtent);
                EditorGUILayout.EndHorizontal();



                //newAnchor = EditorGUILayout.ObjectField(newAnchor, typeof(Transform), true) as Transform;
                if (GUILayout.Button("Set"))
                {
                    foreach (var item in renderers)
                    {

                        item.localBounds = new Bounds { center = newBoundsCenter, extents = newBoundsExtent };

                        //EditorGUILayout.BeginHorizontal(new GUIStyle(GUI.skin.box));


                        //EditorGUILayout.EndHorizontal();
                        EditorUtility.SetDirty(item);
                    }

                }

                EditorGUILayout.EndVertical();

            }

            EditorGUILayout.Space(10);

            // End the vertical layout group
            EditorGUILayout.EndVertical();

        }


    }

}

    #endif
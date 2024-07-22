#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

//using VRC.SDK3.Avatars.Components;
//using VRC.SDK3.Avatars.ScriptableObjects;

using System.IO;
using System.Collections.Generic;





namespace GabSith.WFT
{
    public class BoundsEditor : EditorWindow
    {

        GameObject parent;
        //Transform newAnchor = null;


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


        void OnGUI()
        {

            // Use a vertical layout group to organize the fields
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Use a label field to display the title of the tool with a custom style
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 40
            };


            EditorGUILayout.LabelField("Bounds Editor", titleStyle);
            EditorGUILayout.LabelField("by GabSith", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fixedHeight = 35 });



            EditorGUILayout.Space(25);


            parent = EditorGUILayout.ObjectField("Avatar", parent, typeof(GameObject), true) as GameObject;

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
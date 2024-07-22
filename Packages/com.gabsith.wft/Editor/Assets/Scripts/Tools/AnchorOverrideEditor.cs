#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

//using VRC.SDK3.Avatars.Components;
//using VRC.SDK3.Avatars.ScriptableObjects;

using System.IO;
using System.Collections.Generic;





namespace GabSith.WFT
{
    public class AnchorOverrideEditor : EditorWindow
    {

        GameObject parent;
        Transform newAnchor = null;


        Vector2 scrollPos;


        [MenuItem("GabSith/Anchor Override Editor", false, 2)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(AnchorOverrideEditor), false, "Anchor Override Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_editconstraints_16@2x").image, text = "Anchor Override Editor", tooltip = "♥" };

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


            EditorGUILayout.LabelField("Anchor Override Editor", titleStyle);
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
                    //List<Transform> anchorOverride = new List<Transform> { };


                    //anchorOverride.Add(item.probeAnchor);

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

                EditorGUILayout.Space(10);


                EditorGUILayout.Space(10);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUILayout.Label("Set all to:", GUILayout.Width(70));
                newAnchor = EditorGUILayout.ObjectField(newAnchor, typeof(Transform), true) as Transform;
                if (GUILayout.Button("Set"))
                {
                    foreach (var item in renderers)
                    {

                        item.probeAnchor = newAnchor;


                        //EditorGUILayout.BeginHorizontal(new GUIStyle(GUI.skin.box));


                        //EditorGUILayout.EndHorizontal();
                        EditorUtility.SetDirty(item);
                    }

                }

                EditorGUILayout.EndHorizontal();

            }

            EditorGUILayout.Space(10);

            // End the vertical layout group
            EditorGUILayout.EndVertical();

        }


    }

}

    #endif
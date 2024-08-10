#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using VRC.SDK3.Avatars.Components;
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
        Vector2 scrollPosition;

        bool centerLink = false;
        bool extentLink = true;
        private int lastChangedAxis = -1;

        List<bool> individualLinks = new List<bool>();


        [MenuItem("GabSith/Bounds Editor", false, 503)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(BoundsEditor), false, "Bounds Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Grid.BoxTool@2x").image, text = "Bounds Editor", tooltip = "♥" };

        }

        private void OnEnable()
        {
            CommonActions.RefreshDescriptors(ref parent, ref avatarDescriptorsFromScene);
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
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

                if (individualLinks.Count != renderers.Count * 2)
                    individualLinks.Clear();


                for (int i = 0; i < renderers.Count; i++)
                {
                    EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));

                    EditorGUILayout.ObjectField(renderers[i], typeof(SkinnedMeshRenderer), true);

                    if (individualLinks.Count != renderers.Count * 2)
                    {
                        individualLinks.Add(false);
                        individualLinks.Add(true);
                    }

                    Vector3 center = renderers[i].localBounds.center;
                    Vector3 extent = renderers[i].localBounds.extents;

                    Vector3WithLinkForLoop("Center", ref center, i * 2);
                    Vector3WithLinkForLoop("Extent", ref extent, i * 2+1);

                    renderers[i].localBounds = new Bounds { center = center, extents = extent };

                    EditorGUILayout.EndVertical();
                }

                /*
                foreach (var item in renderers)
                {
                    EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));

                    EditorGUILayout.ObjectField(item, typeof(SkinnedMeshRenderer), true);


                    //individualLinks.Add()


                    EditorGUILayout.BeginHorizontal();
                    Vector3 center = item.localBounds.center;
                    GUILayout.Label("Center", GUILayout.Width(70f));
                    center = EditorGUILayout.Vector3Field("", center, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    Vector3 extent = item.localBounds.extents;
                    GUILayout.Label(new GUIContent("Extent"), GUILayout.Width(70f));
                    extent = EditorGUILayout.Vector3Field("", extent, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.EndHorizontal();

                    item.localBounds = new Bounds { center = center, extents = extent };


                    EditorGUILayout.EndVertical();
                }
                */

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(20);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("Set all to:", GUILayout.Width(70));

                Vector3WithLink("Center", ref newBoundsCenter, ref centerLink);
                Vector3WithLink("Extent", ref newBoundsExtent, ref extentLink);



                if (GUILayout.Button("Set"))
                {
                    foreach (var item in renderers)
                    {

                        item.localBounds = new Bounds { center = newBoundsCenter, extents = newBoundsExtent };

                        EditorUtility.SetDirty(item);
                    }

                }

                EditorGUILayout.EndVertical();

            }

            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        void Vector3WithLink(string name, ref Vector3 vector, ref bool useLink)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(name, GUILayout.Width(50f));

            // Toggle button for linking/unlinking
            string iconName = useLink ? "d_Linked" : "d_Unlinked";
            GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
            if (GUILayout.Button(iconContent, EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(20f)))
            {
                useLink = !useLink;
            }

            EditorGUI.BeginChangeCheck();

            Vector3 oldValue = vector;
            vector = EditorGUILayout.Vector3Field("", vector);

            if (EditorGUI.EndChangeCheck())
            {
                if (useLink)
                {
                    // Determine which axis changed
                    if (vector.x != oldValue.x) lastChangedAxis = 0;
                    else if (vector.y != oldValue.y) lastChangedAxis = 1;
                    else if (vector.z != oldValue.z) lastChangedAxis = 2;

                    // Update all axes based on the changed axis
                    if (lastChangedAxis != -1)
                    {
                        float newValue = vector[lastChangedAxis];
                        vector = new Vector3(newValue, newValue, newValue);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        void Vector3WithLinkForLoop(string name, ref Vector3 vector, int i)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(name, GUILayout.Width(50f));

            // Toggle button for linking/unlinking
            string iconName = individualLinks[i] ? "d_Linked" : "d_Unlinked";
            GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
            if (GUILayout.Button(iconContent, EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(20f)))
            {
                individualLinks[i] = !individualLinks[i];
            }

            EditorGUI.BeginChangeCheck();

            Vector3 oldValue = vector;
            vector = EditorGUILayout.Vector3Field("", vector);

            if (EditorGUI.EndChangeCheck())
            {
                if (individualLinks[i])
                {
                    // Determine which axis changed
                    if (vector.x != oldValue.x) lastChangedAxis = 0;
                    else if (vector.y != oldValue.y) lastChangedAxis = 1;
                    else if (vector.z != oldValue.z) lastChangedAxis = 2;

                    // Update all axes based on the changed axis
                    if (lastChangedAxis != -1)
                    {
                        float newValue = vector[lastChangedAxis];
                        vector = new Vector3(newValue, newValue, newValue);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

    }

}

    #endif
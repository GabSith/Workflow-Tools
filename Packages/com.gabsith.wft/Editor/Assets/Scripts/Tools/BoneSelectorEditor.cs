#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

using System.IO;
using System;




namespace GabSith.WFT
{
    public class BoneSelectorEditor : EditorWindow
    {

        float offsetY = 0;
        float offsetX = 0;
        float scale;
        Rect smallModeRect;
        bool smallMode;

        Animator avatar;
        Vector2 scrollPos;
        Vector2 scrollPosDescriptors;
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        [MenuItem("GabSith/Bone Selector", false, 2)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(BoneSelectorEditor), false, "Bone Selector");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_AvatarSelector").image, text = "Bone Selector", tooltip = "♥" };
            w.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            if (avatar == null)
            {
                //avatarDescriptor = SceneAsset.FindObjectOfType<VRCAvatarDescriptor>();

                RefreshDescriptors();

                if (avatarDescriptorsFromScene.Length == 1)
                {
                    avatar = avatarDescriptorsFromScene[0].GetComponent<Animator>();
                }
            }

            scale = 1.4f;
            offsetY = -80 * scale;
            offsetX = -7;
            smallModeRect = new Rect(0, 0, 400, 540);
            smallMode = false;
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


            EditorGUILayout.LabelField("Bone Selector", titleStyle);
            EditorGUILayout.LabelField("by GabSith", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fixedHeight = 35 });


            EditorGUILayout.Space(25);


            //avatar = EditorGUILayout.ObjectField("Humanoid Avatar", avatar, typeof(Animator), true) as Animator;

            using (new EditorGUILayout.HorizontalScope())
            {
                // Use object fields to assign the avatar, object, and menu
                avatar = (Animator)EditorGUILayout.ObjectField("Avatar", avatar, typeof(Animator), true);

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Find" : "Refresh", GUILayout.Width(70f)))
                {
                    RefreshDescriptors();

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatar = avatarDescriptorsFromScene[0].GetComponent<Animator>();
                        //Debug.Log(avatarDescriptorsFromScene[0].GetComponent<Animator>().avatar.name);
                    }
                }

            }
            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
                //offsetY += 20;
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                //using (new EditorGUILayout.HorizontalScope())
                EditorGUILayout.BeginHorizontal();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        RefreshDescriptors();
                    }
                    else if (GUILayout.Button(item != null ? item.name : ""))
                    {
                        avatar = item.GetComponent<Animator>();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                // Use a space to separate the fields
                EditorGUILayout.Space();
            }

            GUIStyle buttonStickman = new GUIStyle(GUI.skin.button) { fontSize = 7 };


            //scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

            if (avatar == null)
                GUI.enabled = false;
            else if (!avatar.isHuman)
                avatar = null;

            if (Screen.width < 385)
            {
                smallMode = true;
                //Debug.Log("smol");
            }
            else
            {
                smallMode = false;
            }

            EditorGUILayout.Space(320 * scale);

            //EditorGUILayout.BeginVertical();

            {
                if (GUI.Button(CalculateRect(0, 90, 60, 60), "Head", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Head));
                }
                if (GUI.Button(CalculateRect(0, 150, 25, 10), "Neck", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Neck));
                }
                if ((avatar != null) && avatar.GetBoneTransform(HumanBodyBones.UpperChest) != null)
                {
                    if (GUI.Button(CalculateRect(0, 160, 40, 20), " U. Chest", buttonStickman) && (avatar != null))
                    {
                        EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.UpperChest));
                    }
                    if (GUI.Button(CalculateRect(0, 180, 40, 40), "Chest", buttonStickman) && (avatar != null))
                    {
                        EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Chest));
                    }
                }
                else
                {
                    if (GUI.Button(CalculateRect(0, 160, 40, 60), "Chest", buttonStickman) && (avatar != null))
                    {
                        EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Chest));
                    }
                }
                if (GUI.Button(CalculateRect(-40, 160, 40, 25), "Arm.R", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightUpperArm));
                }
                if (GUI.Button(CalculateRect(-80, 160, 40, 25), "Arm.R", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightLowerArm));
                }
                if (GUI.Button(CalculateRect(-112.5f, 160, 25, 25), "Hand", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightHand));
                }
                if (GUI.Button(CalculateRect(40, 160, 40, 25), "Arm.L", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftUpperArm));
                }
                if (GUI.Button(CalculateRect(80, 160, 40, 25), "Arm.L", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftLowerArm));
                }
                if (GUI.Button(CalculateRect(112.5f, 160, 25, 25), "Hand", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftHand));
                }
                if (GUI.Button(CalculateRect(0, 220, 50, 30), "Hips", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Hips));
                }
                if (GUI.Button(CalculateRect(-15, 250, 30, 50), "Leg.R", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightUpperLeg));
                }
                if (GUI.Button(CalculateRect(-15, 300, 30, 50), "Leg.R", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightLowerLeg));
                }
                if (GUI.Button(CalculateRect(-15, 350, 30, 20), "Foot.R", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightFoot));
                }
                if (GUI.Button(CalculateRect(15, 250, 30, 50), "Leg.L", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
                }
                if (GUI.Button(CalculateRect(15, 300, 30, 50), "Leg.L", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
                }
                if (GUI.Button(CalculateRect(15, 350, 30, 20), "Foot.L", buttonStickman) && (avatar != null))
                {
                    EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftFoot));
                }


            }

            //EditorGUILayout.EndVertical();
            GUI.enabled = true;
            EditorGUILayout.EndScrollView();


            /*
            if (GUI.Button(new Rect((Screen.width / 2 - 30) * scale + offsetX, (90 + offsetY) * scale, 60 * scale, 60 * scale), "Head", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Head));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 12.5f) * scale + offsetX, (150 + offsetY) * scale, 25 * scale, 10 * scale), "Neck", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Neck));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 20) * scale + offsetX, (160 + offsetY) * scale, 40 * scale, 60 * scale), "Chest", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Chest));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 60) * scale + offsetX, (160 + offsetY) * scale, 40 * scale, 25 * scale), "Arm.R", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightUpperArm));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 100) * scale + offsetX, (160 + offsetY) * scale, 40 * scale, 25 * scale), "Arm.R", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightLowerArm));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 125) * scale + offsetX, (160 + offsetY) * scale, 25 * scale, 25 * scale), "Hand.R", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightHand));
            }
            if (GUI.Button(new Rect((Screen.width / 2 + 20) * scale + offsetX, (160 + offsetY) * scale, 40 * scale, 25 * scale), "Arm.L", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftUpperArm));
            }
            if (GUI.Button(new Rect((Screen.width / 2 + 60) * scale + offsetX, (160 + offsetY) * scale, 40 * scale, 25 * scale), "Arm.L", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftLowerArm));
            }
            if (GUI.Button(new Rect((Screen.width / 2 + 100) * scale + offsetX, (160 + offsetY) * scale, 25 * scale, 25 * scale), "Hand.L", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftHand));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 25) * scale + offsetX, (220 + offsetY) * scale, 50 * scale, 30 * scale), "Hips", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.Hips));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 30) * scale + offsetX, (250 + offsetY) * scale, 30 * scale, 50 * scale), "Leg.R", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightUpperLeg));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 30) * scale + offsetX, (300 + offsetY) * scale, 30 * scale, 50 * scale), "Leg.R", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightLowerLeg));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 30) * scale + offsetX, (350 + offsetY) * scale, 30 * scale, 20 * scale), "Foot.R", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.RightFoot));
            }
            if (GUI.Button(new Rect((Screen.width / 2 + 0) * scale + offsetX, (250 + offsetY) * scale, 30 * scale, 50 * scale), "Leg.L", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
            }
            if (GUI.Button(new Rect((Screen.width / 2 - 0) * scale + offsetX, (300 + offsetY) * scale, 30 * scale, 50 * scale), "Leg.L", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
            }
            if (GUI.Button(new Rect((Screen.width / 2 + 0) * scale + offsetX, (350 + offsetY) * scale, 30 * scale, 20 * scale), "Foot.L", buttonStickman))
            {
                EditorGUIUtility.PingObject(avatar.GetBoneTransform(HumanBodyBones.LeftFoot));
            }
            */
            


            // End the vertical layout group
            EditorGUILayout.EndVertical();

        }

        Rect CalculateRect(float offsetRectX, float offsetRectY, float width, float height)
        {

            float x = Screen.width / 2 - width * scale / 2 + offsetRectX * scale + offsetX;
            float y = offsetRectY * scale + offsetY;
            float newWidth = width * scale;
            float newHeight = height * scale;


            if (smallMode)
            {
                 x = smallModeRect.width / 2 - width * scale / 2 + offsetRectX * scale + offsetX;
            }


            return new Rect(x, y, newWidth, newHeight);       
        }


        void RefreshDescriptors()
        {
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);
        }

    }

}

    #endif
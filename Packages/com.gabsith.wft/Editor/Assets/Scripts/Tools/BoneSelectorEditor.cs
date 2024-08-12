#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;


namespace GabSith.WFT
{
    public class BoneSelectorEditor : EditorWindow
    {
        bool echoPing = false;
        Transform echoBone = null;

        float offsetY = 0;
        float offsetX = 0;
        float scale;
        Rect smallModeRect;
        bool smallMode;

        Animator avatar;
        Vector2 scrollPos;
        Vector2 scrollPosDescriptors;
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        private const string BoneSelectorUnfoldKey = "BoneSelectorUnfoldKey";

        Color defaultColor;

        [MenuItem("GabSith/Bone Selector %#&B", false, 501)]


        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(BoneSelectorEditor), false, "Bone Selector");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_AvatarSelector").image, text = "Bone Selector", tooltip = "♥" };
            w.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            if (avatar == null)
            {
                CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

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

            defaultColor = GUI.color;
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("Bone Selector");


            using (new EditorGUILayout.HorizontalScope())
            {
                avatar = (Animator)EditorGUILayout.ObjectField("Avatar", avatar, typeof(Animator), true);

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Find" : "Refresh", GUILayout.Width(70f)))
                {
                    CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatar = avatarDescriptorsFromScene[0].GetComponent<Animator>();
                    }
                }

            }

            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
                //offsetY += 20;
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EditorGUILayout.BeginHorizontal();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);
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



            // Unfold
            EditorGUILayout.Space();
            if (CommonActions.ToggleButton("Unfold Selected", ProjectSettingsManager.GetBool(BoneSelectorUnfoldKey, true), GUILayout.Height(25f)))
            {
                ProjectSettingsManager.SetBool(BoneSelectorUnfoldKey, !ProjectSettingsManager.GetBool(BoneSelectorUnfoldKey, true));
            }


            // Echo Previous Ping
            EchoPing();




            GUIStyle buttonStickman = new GUIStyle(GUI.skin.button) { fontSize = 7 };

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));

            if (avatar == null)
                GUI.enabled = false;
            else if (!avatar.isHuman)
                avatar = null;

            if (Screen.width < 385)
            {
                smallMode = true;
            }
            else
            {
                smallMode = false;
            }

            EditorGUILayout.Space(320 * scale);

            {
                if (GUI.Button(CalculateRect(0, 90, 60, 60), "Head", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.Head));

                }
                if (GUI.Button(CalculateRect(0, 150, 25, 10), "Neck", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.Neck));
                }
                if ((avatar != null) && avatar.GetBoneTransform(HumanBodyBones.UpperChest) != null)
                {
                    if (GUI.Button(CalculateRect(0, 160, 40, 20), " U. Chest", buttonStickman) && (avatar != null))
                    {
                        Ping(avatar.GetBoneTransform(HumanBodyBones.UpperChest));
                    }
                    if (GUI.Button(CalculateRect(0, 180, 40, 40), "Chest", buttonStickman) && (avatar != null))
                    {
                        Ping(avatar.GetBoneTransform(HumanBodyBones.Chest));
                    }
                }
                else
                {
                    if (GUI.Button(CalculateRect(0, 160, 40, 60), "Chest", buttonStickman) && (avatar != null))
                    {
                        Ping(avatar.GetBoneTransform(HumanBodyBones.Chest));
                    }
                }
                if (GUI.Button(CalculateRect(-40, 160, 40, 25), "Arm.R", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.RightUpperArm));
                }
                if (GUI.Button(CalculateRect(-80, 160, 40, 25), "Arm.R", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.RightLowerArm));
                }
                if (GUI.Button(CalculateRect(-112.5f, 160, 25, 25), "Hand", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.RightHand));
                }
                if (GUI.Button(CalculateRect(40, 160, 40, 25), "Arm.L", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.LeftUpperArm));
                }
                if (GUI.Button(CalculateRect(80, 160, 40, 25), "Arm.L", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.LeftLowerArm));
                }
                if (GUI.Button(CalculateRect(112.5f, 160, 25, 25), "Hand", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.LeftHand));
                }
                if (GUI.Button(CalculateRect(0, 220, 50, 30), "Hips", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.Hips));
                }
                if (GUI.Button(CalculateRect(-15, 250, 30, 50), "Leg.R", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.RightUpperLeg));
                }
                if (GUI.Button(CalculateRect(-15, 300, 30, 50), "Leg.R", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.RightLowerLeg));
                }
                if (GUI.Button(CalculateRect(-15, 350, 30, 20), "Foot.R", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.RightFoot));
                }
                if (GUI.Button(CalculateRect(15, 250, 30, 50), "Leg.L", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
                }
                if (GUI.Button(CalculateRect(15, 300, 30, 50), "Leg.L", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
                }
                if (GUI.Button(CalculateRect(15, 350, 30, 20), "Foot.L", buttonStickman) && (avatar != null))
                {
                    Ping(avatar.GetBoneTransform(HumanBodyBones.LeftFoot));
                }


            }

            GUI.enabled = true;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

        }

        void Ping(Transform bone)
        {
            EditorGUIUtility.PingObject(bone);

            if (ProjectSettingsManager.GetBool(BoneSelectorUnfoldKey, true))
            {
                if (bone.childCount > 0)
                {
                    for (int i = 0; i < bone.childCount; i++)
                    {
                        EditorGUIUtility.PingObject(bone.GetChild(i));
                    }
                    echoPing = true;
                    echoBone = bone;
                }
            }
        }


        void EchoPing()
        {
            if (echoPing)
            {
                EditorGUIUtility.PingObject(echoBone);
                echoPing = false;
            }
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
    }

}

#endif
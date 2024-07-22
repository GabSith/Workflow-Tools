#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using UnityEngine.Animations;

using System.Collections.Generic;
using System;

using System.IO;
using Vector3 = UnityEngine.Vector3;

using UnityEditor.AnimatedValues;
using UnityEngine.Events;




namespace GabSith.WFT
{
    public class MenuEditor : EditorWindow
    {
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        VRCAvatarDescriptor avatarDescriptor;
        VRCAvatarDescriptor lastAvatarDescriptor;

        private List<VRCExpressionsMenu> controlList = new List<VRCExpressionsMenu> { };


        //SerializedObject so;
        //SerializedProperty _control;


        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;
        //AnimatorController FXLayer;


        Vector2 scrollPosDescriptors;
        Vector2 scrollPosMenu;

        bool refreshedAvatars = false;


        VRCExpressionsMenu.Control copiedControl = new VRCExpressionsMenu.Control();

        bool isCopyActive = false;

        private int currentlyDraggingItemIndex = -1;
        Rect ghostRect;
        Color defaultColor;
        //public Texture2D copyTex, pasteTex, editTex, deleteTex;


        [MenuItem("GabSith/Menu Editor", false, 1)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(MenuEditor), false, "Menu Editor");
            //GUIContent titleContent = new GUIContent("Test", (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Bon/Assets/Materials/Textures/KannaSip.png", typeof(Texture2D)));
            //EditorGUIUtility.IconContent("d_Audio Mixer@2x");
            //w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_Audio Mixer@2x").image, text = "Menu Editor", tooltip = "♥" };
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image, text = "Menu Editor", tooltip = "♥" };
            w.minSize = new Vector2(350, 400);
        }

        private void OnEnable()
        {
            if (!EditorApplication.isPlaying)
            {
                if (avatarDescriptor == null)
                {
                    //avatarDescriptor = SceneAsset.FindObjectOfType<VRCAvatarDescriptor>();

                    RefreshDescriptors();

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                    }
                }
            }

            defaultColor = GUI.color;

        }


        void OnGUI()
        {
            Event e = Event.current;

            // Use a vertical layout group to organize the fields
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Use a label field to display the title of the tool with a custom style
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 40
            };


            EditorGUILayout.LabelField("Menu Editor", titleStyle);
            EditorGUILayout.LabelField("by GabSith", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fixedHeight = 35 });

            // Use a space to separate the fields
            EditorGUILayout.Space(25);


            using (new EditorGUILayout.HorizontalScope())
            {
                // Use object fields to assign the avatar, object, and menu
                avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
                if (avatarDescriptor != lastAvatarDescriptor)
                {
                    RefreshMenu();
                    lastAvatarDescriptor = avatarDescriptor;
                }

                if (GUILayout.Button(avatarDescriptorsFromScene.Length < 2 ? "Select From Scene" : "Refresh", avatarDescriptorsFromScene.Length < 2 ? GUILayout.Width(130f) : GUILayout.Width(70f)))
                {
                    RefreshDescriptors();

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                    }
                }

            }

            if (avatarDescriptorsFromScene != null && avatarDescriptorsFromScene.Length > 1)
            {
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
                        avatarDescriptor = item;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                // Use a space to separate the fields
                EditorGUILayout.Space();
            }


            if (avatarDescriptor != null)
            {
                refreshedAvatars = false;

                if (avatarDescriptor.customExpressions && avatarDescriptor.expressionParameters != null)
                    parameters = avatarDescriptor.expressionParameters;
                if (menu == null && avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
                    menu = avatarDescriptor.expressionsMenu;
            }
            else
            {
                if (!refreshedAvatars)
                {
                    //Debug.Log("avatarDescriptor is null!");
                    RefreshDescriptors();

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                    }
                    refreshedAvatars = true;
                }
            }


            GUIStyle menuButtons = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 13,
                fixedHeight = 40
            };

            GUIStyle menuButtonsEmpty = new GUIStyle(menuButtons)
            {
                fontSize = 10,
                fixedHeight = 40
            };

            EditorGUILayout.Space(20);

            controlList.RemoveAll(item => item == null);

            if (RequirementsMet())
            {
                scrollPosMenu = EditorGUILayout.BeginScrollView(scrollPosMenu, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));

                controlList.RemoveAll(item => item == null);

                if (controlList.Count == 0)
                    controlList.Add(menu);

                {
                    EditorGUILayout.BeginHorizontal();
                    //using (new EditorGUILayout.HorizontalScope())
                    {
                        //GUILayout.Label("Current Menu: " + controlList[controlList.Count - 1].name);
                        GUILayout.Label("Current Menu: ");
                        EditorGUILayout.ObjectField(controlList[controlList.Count - 1], typeof(VRCExpressionsMenu), false);

                        if (controlList.Count == 1)
                        {
                            GUI.enabled = false;
                        }

                        if (GUILayout.Button("Back", GUILayout.Width(123)))
                        {
                            controlList.RemoveAt(controlList.Count - 1);
                        }

                        GUI.enabled = true;


                        if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
                        {
                            RefreshMenu();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (controlList[controlList.Count - 1] != null)


                    for (int i = 0; i < VRCExpressionsMenu.MAX_CONTROLS; i++)
                    {
                        if (controlList[controlList.Count - 1].controls.Count > i)
                        {
                            if (i == currentlyDraggingItemIndex)
                            {
                                GUI.color = Color.grey; // Change the color of the item being dragged
                            }


                            // Actual Control
                            VRCExpressionsMenu.Control control = controlList[controlList.Count - 1].controls[i];
                            {
                                using (new EditorGUILayout.HorizontalScope(new GUIStyle() { fixedHeight = 45 }))
                                {
                                    int smallButtonsHeight = 19;


                                    GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                                    GUILayout.Label(EditorGUIUtility.IconContent("_Menu@2x"), style, GUILayout.Height(EditorGUIUtility.singleLineHeight), 
                                        GUILayout.Width(30), GUILayout.Height(40));


                                    // Move UP or DOWN
                                    /*
                                    EditorGUILayout.BeginVertical(new GUIStyle() { fixedWidth = 35, fontSize = 10, });

                                    if (i == 0)
                                        GUI.enabled = false;
                                    if (GUILayout.Button("▲", new GUIStyle(EditorStyles.miniButton) { fixedHeight = smallButtonsHeight }))
                                    {
                                        VRCExpressionsMenu.Control controlUp = controlList[controlList.Count - 1].controls[i-1];

                                        controlList[controlList.Count - 1].controls[i - 1] = control;
                                        controlList[controlList.Count - 1].controls[i] = controlUp;
                                        EditorUtility.SetDirty(controlList[controlList.Count - 1]);

                                    }
                                    GUI.enabled = true;

                                    if (i + 1 == VRCExpressionsMenu.MAX_CONTROLS || controlList[controlList.Count - 1].controls.Count == i + 1)
                                        GUI.enabled = false;
                                    if (GUILayout.Button("▼", new GUIStyle(EditorStyles.miniButton) { fixedHeight = smallButtonsHeight }))
                                    {
                                        VRCExpressionsMenu.Control controlDown = controlList[controlList.Count - 1].controls[i + 1];

                                        controlList[controlList.Count - 1].controls[i + 1] = control;
                                        controlList[controlList.Count - 1].controls[i] = controlDown;
                                        EditorUtility.SetDirty(controlList[controlList.Count - 1]);

                                    }
                                    GUI.enabled = true;
                                    EditorGUILayout.EndVertical();
                                    */

                                    // Menu or otherwise button

                                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu) // If the control is a menu
                                    {
                                        if (GUILayout.Button(new GUIContent(control.name + " ➔", control.icon), menuButtons, GUILayout.MaxWidth(Screen.width- 240)))
                                        {
                                            if (control.subMenu != null)
                                                controlList.Add(control.subMenu);
                                        }
                                        CheckDragAndDrop(ref control.icon, controlList[controlList.Count - 1]);
                                    }
                                    else
                                    {
                                        Color def = GUI.backgroundColor;

                                        switch (control.type)
                                        {
                                            case VRCExpressionsMenu.Control.ControlType.Button:
                                                GUI.backgroundColor = new Color { r = 1f, g = 1f, b = 0.7f, a = 1 };
                                                break;
                                            case VRCExpressionsMenu.Control.ControlType.Toggle:
                                                GUI.backgroundColor = new Color { r = 0.7f, g = 0.7f, b = 1f, a = 1 };
                                                break;
                                            case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                                                GUI.backgroundColor = new Color { r = 1f, g = 0.7f, b = 1f, a = 1 };
                                                break;
                                            default:
                                                GUI.backgroundColor = new Color { r = 1f, g = 0.7f, b = 0.7f, a = 1 };
                                                break;
                                        }


                                        GUILayout.Button(new GUIContent(control.name, control.icon), menuButtons, GUILayout.MaxWidth(Screen.width - 240));
                                        CheckDragAndDrop(ref control.icon, controlList[controlList.Count - 1]);


                                        GUI.backgroundColor = def;
                                        GUI.enabled = true;
                                    }


                                    // The rest

                                    // Edit
                                    if (GUILayout.Button(new GUIContent("Edit", "Opens the Menu Control Editor"), menuButtons, GUILayout.Width(60)))
                                    {

                                        OpenWindow(control, i, controlList[controlList.Count - 1], parameters);

                                    }

                                    /*
                                    // Clone
                                    if (controlList[controlList.Count - 1].controls.Count >= VRCExpressionsMenu.MAX_CONTROLS)
                                        GUI.enabled = false;
                                    if (GUILayout.Button("Clone", menuButtons, GUILayout.Width(60)))
                                    {
                                        controlList[controlList.Count - 1].controls.Add(Duplicate(i));
                                        EditorUtility.SetDirty(controlList[controlList.Count - 1]);
                                    }
                                    GUI.enabled = true;*/

                                    // Copy Paste

                                    // Copy
                                    EditorGUILayout.BeginVertical(new GUIStyle() { fixedWidth = 60, fontSize = 8, });
                                    if (GUILayout.Button(new GUIContent("Copy"), new GUIStyle(EditorStyles.miniButton) { fixedHeight = smallButtonsHeight, fontSize = 10 }))
                                    {
                                        isCopyActive = true;
                                        copiedControl = Duplicate(controlList[controlList.Count - 1].controls[i]);

                                    }

                                    // Paste
                                    if (!isCopyActive)
                                        GUI.enabled = false;
                                    if (GUILayout.Button(new GUIContent("Paste"), new GUIStyle(EditorStyles.miniButton) { fixedHeight = smallButtonsHeight, fontSize = 10 }))
                                    {
                                        controlList[controlList.Count - 1].controls[i] = Duplicate(copiedControl);
                                        //controlList[controlList.Count - 1].controls.RemoveAt(i);
                                        //controlList[controlList.Count - 1].controls.Add(Duplicate(copiedControl));
                                        EditorUtility.SetDirty(controlList[controlList.Count - 1]);
                                    }
                                    GUI.enabled = true;

                                    EditorGUILayout.EndVertical();




                                    // Delete
                                    if (GUILayout.Button(new GUIContent("Delete"), menuButtons, GUILayout.Width(60)))
                                    {
                                        controlList[controlList.Count - 1].controls.RemoveAt(i);
                                        EditorUtility.SetDirty(controlList[controlList.Count - 1]);
                                    }

                                }

                            }

                            GUI.color = defaultColor; // Reset color

                            Rect dropArea = GUILayoutUtility.GetLastRect();
                            if (e.type == EventType.MouseDown && dropArea.Contains(e.mousePosition))
                            {
                                currentlyDraggingItemIndex = i;
                                EditorGUIUtility.SetWantsMouseJumping(1);
                            }
                            else if (e.type == EventType.MouseDrag && currentlyDraggingItemIndex > -1)
                            {
                                e.Use();
                            }
                            else if (e.type == EventType.MouseUp && dropArea.Contains(e.mousePosition) && currentlyDraggingItemIndex > -1)
                            {
                                //controlList[controlList.Count - 1].controls = new List<VRCExpressionsMenu.Control>(items);
                                currentlyDraggingItemIndex = -1;
                                EditorGUIUtility.SetWantsMouseJumping(0);
                                Repaint(); // Force the window to repaint
                            }

                            // Draw a ghost item at the mouse position while dragging
                            if (currentlyDraggingItemIndex > -1)
                            {
                                ghostRect = new Rect(dropArea.x, e.mousePosition.y - (dropArea.height / 2), dropArea.width, dropArea.height);
                                //EditorGUI.DrawRect(ghostRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                            }

                            if (currentlyDraggingItemIndex > -1 && dropArea.Contains(e.mousePosition))
                            {
                                VRCExpressionsMenu.Control temp = controlList[controlList.Count - 1].controls[currentlyDraggingItemIndex];
                                controlList[controlList.Count - 1].controls.RemoveAt(currentlyDraggingItemIndex);
                                controlList[controlList.Count - 1].controls.Insert(i, temp);
                                currentlyDraggingItemIndex = i;
                                Repaint(); // Force the window to repaint
                            }

                        }

                        // New Control
                        else
                        {
                            Color def = GUI.backgroundColor;
                            GUI.backgroundColor = new Color { r = 0.4f, g = 1f, b = 0.4f, a = 1 };

                            using (new EditorGUILayout.HorizontalScope(new GUIStyle() { fixedHeight = 45 }))
                            {
                                if (GUILayout.Button("+ Create Control +", menuButtonsEmpty))
                                {
                                    VRCExpressionsMenu.Control newControl = new VRCExpressionsMenu.Control
                                    {
                                        name = "New Control",
                                        parameter = new VRCExpressionsMenu.Control.Parameter { name = "" },
                                        subParameters = new VRCExpressionsMenu.Control.Parameter[4] { new VRCExpressionsMenu.Control.Parameter { },
                                        new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { } },
                                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                                        icon = null,

                                    };
                                    controlList[controlList.Count - 1].controls.Add(newControl);
                                    EditorUtility.SetDirty(controlList[controlList.Count - 1]);

                                    OpenWindow(newControl, controlList[controlList.Count - 1].controls.Count - 1, controlList[controlList.Count - 1], parameters);

                                }

                                    CheckDragAndDropNewSubmenu(controlList[controlList.Count - 1]);


                                GUI.backgroundColor = def;

                                if (!isCopyActive)
                                    GUI.enabled = false;
                                if (GUILayout.Button("Paste", menuButtons, GUILayout.Width(60)))
                                {
                                    controlList[controlList.Count - 1].controls.Add(Duplicate(copiedControl));
                                    EditorUtility.SetDirty(controlList[controlList.Count - 1]);
                                }
                                GUI.enabled = true;

                            }

                        }
                    }

                    if (currentlyDraggingItemIndex > -1 && mouseOverWindow == GetWindow(typeof(MenuEditor)))
                    {
                        EditorGUI.DrawRect(ghostRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    }

                    // If the mouse button is released outside of any item
                    if ((e.type == EventType.Ignore || e.type == EventType.MouseUp) && currentlyDraggingItemIndex > -1)
                    {
                        currentlyDraggingItemIndex = -1;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        Repaint(); // Force the window to repaint
                    }

                    if (controlList[controlList.Count - 1] != null)

                        GUILayout.Label("Used Controls: " + controlList[controlList.Count - 1].controls.Count + " out of " + VRCExpressionsMenu.MAX_CONTROLS);
                    EditorGUILayout.Space(20);


                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                //EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("Expression Menu or Expression Parameters cannot be accessed", MessageType.Error);
                EditorGUILayout.Space(40);
            }


            // Use a space to separate the fields
            EditorGUILayout.Space();

            // End the vertical layout group
            EditorGUILayout.EndVertical();

            // Use a space to separate the preview
            EditorGUILayout.Space(10);

        }


        void OpenWindow(VRCExpressionsMenu.Control control, int index, VRCExpressionsMenu menu, VRCExpressionParameters parameters)
        {
            EditorWindow.GetWindow<MenuControlEditor>("Menu Control Editor").control = control;
            EditorWindow.GetWindow<MenuControlEditor>("Menu Control Editor").menuIndex = index;
            EditorWindow.GetWindow<MenuControlEditor>("Menu Control Editor").menu = menu;
            EditorWindow.GetWindow<MenuControlEditor>("Menu Control Editor").expressionParameters = parameters;
            EditorWindow.GetWindow<MenuControlEditor>("Menu Control Editor").minSize = new Vector2(330, 310);
            EditorWindow.GetWindow<MenuControlEditor>("Menu Control Editor").avatarDescriptor = avatarDescriptor;
        }

        private void CheckDragAndDrop(ref Texture2D texture, VRCExpressionsMenu cont)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (e.type == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences[0] as Texture2D != null)
                        {
                            DragAndDrop.AcceptDrag();
                            texture = DragAndDrop.objectReferences[0] as Texture2D;
                            EditorUtility.SetDirty(cont);
                        }
                    }
                    break;
            }
        }

        private void CheckDragAndDropNewSubmenu(VRCExpressionsMenu cont)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (e.type == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences[0] as VRCExpressionsMenu != null)
                        {
                            DragAndDrop.AcceptDrag();

                            // Create new submenu

                            VRCExpressionsMenu.Control newControl = new VRCExpressionsMenu.Control
                            {
                                name = DragAndDrop.objectReferences[0].name,
                                parameter = new VRCExpressionsMenu.Control.Parameter { name = "" },
                                subParameters = new VRCExpressionsMenu.Control.Parameter[4] { new VRCExpressionsMenu.Control.Parameter { },
                                        new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { }, new VRCExpressionsMenu.Control.Parameter { } },
                                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                icon = null,
                                subMenu = DragAndDrop.objectReferences[0] as VRCExpressionsMenu

                            };
                            cont.controls.Add(newControl);
                            EditorUtility.SetDirty(cont);
                        }
                    }
                    break;
            }
        }


        VRCExpressionsMenu.Control Duplicate(VRCExpressionsMenu.Control selectedControl)
        {

            VRCExpressionsMenu.Control.Parameter[] parameters;

            if (selectedControl.subParameters != null)
            {
                List<VRCExpressionsMenu.Control.Parameter> parametersList = new List<VRCExpressionsMenu.Control.Parameter> { };

                for (int i = 0; i < selectedControl.subParameters.Length; i++)
                {
                    parametersList.Add(new VRCExpressionsMenu.Control.Parameter { name = selectedControl.subParameters[i].name });
                }

                parameters = parametersList.ToArray();
            }
            else
                parameters = null;

            VRCExpressionsMenu.Control.Label[] labels;
            if (selectedControl.labels != null)
            {
                labels = new VRCExpressionsMenu.Control.Label[selectedControl.labels.Length];
                for (int j = 0; j < selectedControl.labels.Length; j++)
                {
                    labels[j] = new VRCExpressionsMenu.Control.Label
                    {
                        name = selectedControl.labels[j].name,
                        icon = selectedControl.labels[j].icon
                    };
                }
            }
            else
                labels = null;

            VRCExpressionsMenu.Control newControl = new VRCExpressionsMenu.Control
            {
                name = selectedControl.name,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = selectedControl.parameter.name },
                icon = selectedControl.icon,
                value = selectedControl.value,
                labels = labels,
                subMenu = selectedControl.subMenu,
                subParameters = parameters,
                type = selectedControl.type
            };

            return newControl;
        }


        void RefreshDescriptors()
        {
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);
        }

        void RefreshMenu()
        {
            menu = null;
            controlList.Clear();

            if (avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
            {
                menu = avatarDescriptor.expressionsMenu;
                controlList.Add(menu);
            }

        }

        bool RequirementsMet()
        {
            if (avatarDescriptor == null)
            {
                //Debug.Log("Avatar Descriptor is null");
                return false;
            }
            else if (!avatarDescriptor.customExpressions || avatarDescriptor.expressionParameters == null || avatarDescriptor.expressionsMenu == null) 
            {
                Debug.Log("Expressions Menu or Expressions Parameters are null");
                return false;
            }



            return true;
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
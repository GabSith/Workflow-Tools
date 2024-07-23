#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

using System.Collections.Generic;
using System;

using System.Text.RegularExpressions;


namespace GabSith.WFT
{
    public class MenuEditor : EditorWindow
    {
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        VRCAvatarDescriptor avatarDescriptor;

        private List<VRCExpressionsMenu> controlList = new List<VRCExpressionsMenu> { };


        //SerializedObject so;

        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;


        Vector2 scrollPosDescriptors;
        Vector2 scrollPosMenu;

        //bool refreshedAvatars = false;


        VRCExpressionsMenu.Control copiedControl = new VRCExpressionsMenu.Control();

        bool isCopyActive = false;

        private int currentlyDraggingItemIndex = -1;
        Rect ghostRect;
        Color defaultColor;


        [MenuItem("GabSith/Menu Editor", false, 1)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(MenuEditor), false, "Menu Editor");

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

                    CommonActions.RefreshDescriptors(ref avatarDescriptorsFromScene);

                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                        if (avatarDescriptor.customExpressions && avatarDescriptor.expressionParameters != null)
                            parameters = avatarDescriptor.expressionParameters;
                        if (menu == null && avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
                            menu = avatarDescriptor.expressionsMenu;
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

            CommonActions.GenerateTitle("Menu Editor");

            // Avatar Selection
            if (CommonActions.FindAvatars(ref avatarDescriptor, ref scrollPosDescriptors, ref avatarDescriptorsFromScene))
            {
                if (avatarDescriptor.customExpressions && avatarDescriptor.expressionParameters != null)
                    parameters = avatarDescriptor.expressionParameters;
                if (avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
                    menu = avatarDescriptor.expressionsMenu;
                RefreshMenu();
            }

            /*
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
            */

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

                    /*Color tempColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1, 1, 1, 0.1f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.backgroundColor = tempColor;
                    */
                    //EditorGUILayout.BeginVertical(GUI.skin.);


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


                                    // Menu or otherwise button

                                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu) // If the control is a menu
                                    {
                                        if (GUILayout.Button(new GUIContent(CleanMarkdownText(control.name) + " ➔", control.icon), menuButtons, GUILayout.MaxWidth(Screen.width- 240)))
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


                                        GUILayout.Button(new GUIContent(CleanMarkdownText(control.name), control.icon), menuButtons, GUILayout.MaxWidth(Screen.width - 240));
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

                    //EditorGUILayout.EndVertical();


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


        string CleanMarkdownText(string text)
        {
            // Pattern to match markdown-like tags
            string pattern = @"<[^>]+>";

            if (Regex.IsMatch(text, pattern))
            {
                return "[MD] " + Regex.Replace(text, pattern, "");
            }

            return text;
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
                //Debug.Log("Expressions Menu or Expressions Parameters are null");
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
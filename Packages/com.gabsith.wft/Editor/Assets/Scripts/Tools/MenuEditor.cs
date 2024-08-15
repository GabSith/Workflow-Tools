#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

using System.Collections.Generic;
using System;
using System.Linq;

using System.Text.RegularExpressions;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;


namespace GabSith.WFT
{
    public class MenuEditor : EditorWindow
    {
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;

        VRCAvatarDescriptor avatarDescriptor;

        private List<VRCExpressionsMenu> controlList = new List<VRCExpressionsMenu> { };


        bool useSelected = false;
        int previousSelection;

        VRCExpressionParameters parameters;
        VRCExpressionsMenu menu;


        Vector2 scrollPosDescriptors;
        Vector2 scrollPosMenu;
        Vector2 scrollPos;


        AnimBool preview = new AnimBool(false);
        bool showPreview = true;


        VRCExpressionsMenu.Control copiedControl = new VRCExpressionsMenu.Control();

        bool isCopyActive = false;

        private int currentlyDraggingItemIndex = -1;
        Rect ghostRect;
        Color defaultColor;

        Color newControlColor = new Color(0.4f, 1f, 0.4f);

        Color buttonColor = new Color { r = 1f, g = 1f, b = 0.7f, a = 1 };
        Color toggleColor = new Color { r = 0.7f, g = 0.7f, b = 1f, a = 1 };
        Color radialColor = new Color { r = 1f, g = 0.7f, b = 1f, a = 1 };
        Color otherColor = new Color { r = 1f, g = 0.7f, b = 0.7f, a = 1 };
        Color folderColor = new Color { r = 1f, g = 1f, b = 1f, a = 1 };

        GUIStyle menuButtons, menuButtonsIcon, menuButtonsLabel, menuButtonsParam, menuButtonsEmpty;

        VRCExpressionsMenu menuToDraw;
        bool isFirstMenu = false;

        [MenuItem("GabSith/Menu Editor", false, 1)]


        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow w = EditorWindow.GetWindow(typeof(MenuEditor), false, "Menu Editor");

            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image, text = "Menu Editor", tooltip = "♥" };
            w.minSize = new Vector2(350, 400);
            w.autoRepaintOnSceneChange = true;
        }

        private void OnEnable()
        {
            if (avatarDescriptor == null)
            {
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

            defaultColor = GUI.color;

            preview.valueChanged.AddListener(new UnityAction(Repaint));

        }

        void OnGUI()
        {
            Event e = Event.current;

            menuButtons = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 13,
                fixedHeight = 40,
                richText = true
            };
            menuButtonsIcon = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(),
                fixedHeight = 40,
            };
            menuButtonsLabel = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                margin = new RectOffset(),
                fontSize = 13,
                fixedHeight = 40,
                richText = true
            };
            menuButtonsParam = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = true,
                margin = new RectOffset(),
                fontSize = 10,
                fixedHeight = 40,
            };

            menuButtonsEmpty = new GUIStyle(menuButtons)
            {
                fontSize = 10,
                fixedHeight = 40
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);


            // Tittle
            CommonActions.GenerateTitle("Menu Editor");

            // Avatar Selection
            if (CommonActions.FindAvatars(ref avatarDescriptor, ref scrollPosDescriptors, ref avatarDescriptorsFromScene) && !useSelected)
            {
                if (avatarDescriptor != null)
                {
                    if (avatarDescriptor.customExpressions && avatarDescriptor.expressionParameters != null)
                        parameters = avatarDescriptor.expressionParameters;
                    if (avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
                        menu = avatarDescriptor.expressionsMenu;
                }

                if (useSelected)
                {
                    if (Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets).Length > 0)
                        menu = Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets)[0];
                }

                RefreshMenu();
            }

            EditorGUILayout.Space();

            // Use Selected
            //if (useSelected)
            //{
                //GUI.backgroundColor = CommonActions.selectionColor;
                //GUI.backgroundColor = ProjectSettingsManager.GetColor("ButtonsColorKey", CommonActions.selectionColor);

            //}
            //if (GUILayout.Button("Use Selected Menu", GUILayout.Height(25f)))
            if (CommonActions.ToggleButton("Use Selected Menu", useSelected, GUILayout.Height(25f)))
            {
                useSelected = !useSelected;
                previousSelection = 0;

                if (useSelected)
                {
                    if (Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets).Length > 0)
                        menu = Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets)[0];
                }
                else
                {
                    if (avatarDescriptor != null)
                    {
                        if (avatarDescriptor.customExpressions && avatarDescriptor.expressionParameters != null)
                            parameters = avatarDescriptor.expressionParameters;
                        if (avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
                            menu = avatarDescriptor.expressionsMenu;
                    }
                }
                RefreshMenu();

            }
            //GUI.backgroundColor = defaultColor;


            // Selection Changed
            if (useSelected)
            {
                VRCExpressionsMenu[] selection = Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets);
                if (selection.Length > 0)
                {
                    int selectionID = selection[0].GetInstanceID();
                    if (selectionID != previousSelection)
                    {
                        menu = selection[0];
                        RefreshMenu();
                        Repaint();

                        previousSelection = selectionID;
                    }
                }
            }



            EditorGUILayout.Space(20);

            controlList.RemoveAll(item => item == null);

            if (RequirementsMet())
            {
                scrollPosMenu = EditorGUILayout.BeginScrollView(scrollPosMenu, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));

                controlList.RemoveAll(item => item == null);

                if (controlList.Count == 0)
                    controlList.Add(menu);

                int lastIndex = controlList.Count - 1;
                VRCExpressionsMenu lastMenu = controlList[lastIndex];
                menuToDraw = lastMenu;

                {
                    EditorGUILayout.BeginHorizontal();
                    
                    GUILayout.Label("Current Menu: ");
                    EditorGUILayout.ObjectField(lastMenu, typeof(VRCExpressionsMenu), false);

                    isFirstMenu = controlList.Count == 1;

                    GUI.enabled = !isFirstMenu;


                    if (GUILayout.Button("Back", GUILayout.Width(123)))
                    {
                        controlList.RemoveAt(lastIndex);
                    }
                    GUI.enabled = true;


                    if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
                    {
                        RefreshMenu();
                    }

                    
                    EditorGUILayout.EndHorizontal();


                    if (lastMenu != null)
                    {
                        for (int i = 0; i < VRCExpressionsMenu.MAX_CONTROLS; i++)
                        {
                            if (lastMenu.controls.Count > i)
                            {
                                if (i == currentlyDraggingItemIndex)
                                {
                                    GUI.color = Color.grey; // Color of the item being dragged
                                }


                                // Actual Control
                                VRCExpressionsMenu.Control control = lastMenu.controls[i];
                                {
                                    using (new EditorGUILayout.HorizontalScope(new GUIStyle() { fixedHeight = 45 }))
                                    {
                                        int smallButtonsHeight = 19;


                                        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                                        GUILayout.Label(EditorGUIUtility.IconContent("_Menu@2x"), style, GUILayout.Height(EditorGUIUtility.singleLineHeight),
                                            GUILayout.Width(30), GUILayout.Height(40));


                                        // Menu or otherwise button
                                        //float width = Screen.width - 241f;

                                        if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu) // If the control is a menu
                                        {
                                            Color def = GUI.backgroundColor;
                                            GUI.backgroundColor = folderColor;
                                            /*
                                            using (var v = new EditorGUILayout.HorizontalScope("Button", GUILayout.MaxWidth(width), GUILayout.ExpandWidth(false)))
                                            {
                                                if (GUI.Button(v.rect, GUIContent.none) && control.subMenu != null)
                                                    controlList.Add(control.subMenu); 
                                                GUILayout.Label(control.icon, GUILayout.Height(35));
                                                GUILayout.Label(control.name + " ➔");
                                                GUILayout.Label(control.parameter.name);
                                            }*/
                                            MenuButton(control);
                                            /*
                                            using (var horizontalScope = new EditorGUILayout.HorizontalScope(menuButtons, GUILayout.MaxWidth(width), GUILayout.ExpandWidth(false)))
                                            {
                                                bool isClicked = GUI.Button(horizontalScope.rect, "", GUIStyle.none);

                                                GUILayout.Label(control.icon, menuButtonsIcon, GUILayout.Width(60), GUILayout.Height(35));
                                                GUILayout.Label(control.name + " ➔", menuButtonsLabel, GUILayout.ExpandWidth(true), GUILayout.MinWidth(10));

                                                if (!string.IsNullOrEmpty(control.parameter.name))
                                                {
                                                    // Flexible space to push parameter to the right
                                                    GUILayout.FlexibleSpace();

                                                    Color defCol = GUI.color;
                                                    GUI.color = new Color(1f, 1f, 1f, 0.6f);
                                                    GUILayout.Label(control.parameter.name, menuButtonsParam, GUILayout.Width(80));
                                                    GUI.color = defCol;
                                                }
                                                // Handle click event
                                                if (isClicked && control.subMenu != null)
                                                {
                                                    controlList.Add(control.subMenu);
                                                }
                                            }             */                             
                                            /*
                                            if (GUILayout.Button(new GUIContent(control.name + " ➔", control.icon), menuButtons, GUILayout.MaxWidth(width)))
                                            {
                                                if (control.subMenu != null)
                                                    controlList.Add(control.subMenu);
                                            }*/
                                            CheckDragAndDrop(ref control.icon, ref control.subMenu, lastMenu);
                                            GUI.backgroundColor = def;

                                        }
                                        else
                                        {
                                            Color def = GUI.backgroundColor;

                                            switch (control.type)
                                            {
                                                case VRCExpressionsMenu.Control.ControlType.Button:
                                                    GUI.backgroundColor = buttonColor;
                                                    break;
                                                case VRCExpressionsMenu.Control.ControlType.Toggle:
                                                    GUI.backgroundColor = toggleColor;
                                                    break;
                                                case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                                                    GUI.backgroundColor = radialColor;
                                                    break;
                                                default:
                                                    GUI.backgroundColor = otherColor;
                                                    break;
                                            }

                                            MenuButton(control);


                                            //GUILayout.Button(new GUIContent(control.name, control.icon), menuButtons, GUILayout.MaxWidth(width));

                                            CheckDragAndDrop(ref control.icon, lastMenu);


                                            GUI.backgroundColor = def;
                                            GUI.enabled = true;
                                        }


                                        // The rest

                                        // Edit
                                        if (GUILayout.Button(new GUIContent("Edit", "Opens the Menu Control Editor"), menuButtons, GUILayout.Width(60)))
                                        {
                                            OpenWindow(control, i, lastMenu, parameters);
                                        }


                                        // Copy Paste

                                        // Copy
                                        EditorGUILayout.BeginVertical(new GUIStyle() { fixedWidth = 60, fontSize = 8, });
                                        if (GUILayout.Button(new GUIContent("Copy"), new GUIStyle(EditorStyles.miniButton) { fixedHeight = smallButtonsHeight, fontSize = 10 }))
                                        {
                                            isCopyActive = true;
                                            copiedControl = Duplicate(lastMenu.controls[i]);

                                        }

                                        // Paste
                                        if (!isCopyActive)
                                            GUI.enabled = false;
                                        if (GUILayout.Button(new GUIContent("Paste"), new GUIStyle(EditorStyles.miniButton) { fixedHeight = smallButtonsHeight, fontSize = 10 }))
                                        {
                                            lastMenu.controls[i] = Duplicate(copiedControl);
                                            //controlList[controlList.Count - 1].controls.RemoveAt(i);
                                            //controlList[controlList.Count - 1].controls.Add(Duplicate(copiedControl));
                                            EditorUtility.SetDirty(lastMenu);
                                        }
                                        GUI.enabled = true;

                                        EditorGUILayout.EndVertical();


                                        // Delete
                                        if (GUILayout.Button(new GUIContent("Delete"), menuButtons, GUILayout.Width(60)))
                                        {
                                            lastMenu.controls.RemoveAt(i);
                                            EditorUtility.SetDirty(lastMenu);
                                        }
                                    }
                                }

                                GUI.color = defaultColor; // Reset color


                                // Drag Position
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
                                    currentlyDraggingItemIndex = -1;
                                    EditorGUIUtility.SetWantsMouseJumping(0);
                                    Repaint();
                                }

                                // Draw a ghost item at the mouse position while dragging
                                if (currentlyDraggingItemIndex > -1)
                                {
                                    ghostRect = new Rect(dropArea.x, e.mousePosition.y - (dropArea.height / 2), dropArea.width, dropArea.height);
                                }

                                if (currentlyDraggingItemIndex > -1 && dropArea.Contains(e.mousePosition))
                                {
                                    VRCExpressionsMenu.Control temp = lastMenu.controls[currentlyDraggingItemIndex];
                                    lastMenu.controls.RemoveAt(currentlyDraggingItemIndex);
                                    lastMenu.controls.Insert(i, temp);
                                    currentlyDraggingItemIndex = i;
                                    Repaint();
                                }
                            }

                            // New Control
                            else
                            {
                                Color def = GUI.backgroundColor;
                                GUI.backgroundColor = newControlColor;

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
                                        lastMenu.controls.Add(newControl);
                                        EditorUtility.SetDirty(lastMenu);

                                        OpenWindow(newControl, controlList[controlList.Count - 1].controls.Count - 1, controlList[controlList.Count - 1], parameters);

                                    }

                                    CheckDragAndDropNewSubmenu(lastMenu);


                                    GUI.backgroundColor = def;

                                    if (!isCopyActive)
                                        GUI.enabled = false;
                                    if (GUILayout.Button("Paste", menuButtons, GUILayout.Width(60)))
                                    {
                                        lastMenu.controls.Add(Duplicate(copiedControl));
                                        EditorUtility.SetDirty(lastMenu);
                                    }
                                    GUI.enabled = true;

                                }

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


                    if (lastMenu != null)

                        GUILayout.Label("Used Controls: " + lastMenu.controls.Count + " out of " + VRCExpressionsMenu.MAX_CONTROLS);
                    EditorGUILayout.Space(20);


                }
                EditorGUILayout.EndScrollView();
            }


            EditorGUILayout.Space();

            showPreview = EditorGUILayout.Foldout(showPreview, "Preview");
            preview.target = showPreview && RequirementsMet(false);
            using (var group = new EditorGUILayout.FadeGroupScope(preview.faded))

                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    RadialMenuDrawer.DrawRadialMenu(EditorGUILayout.GetControlRect(false, 320f), menuToDraw, isFirstMenu && !useSelected);
                    EditorGUILayout.EndVertical();
                }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

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

        void MenuButton(VRCExpressionsMenu.Control control)
        {
            float width = Screen.width - 241f;

            using (var horizontalScope = new EditorGUILayout.HorizontalScope(menuButtons, GUILayout.MaxWidth(width), GUILayout.ExpandWidth(false)))
            {
                bool isClicked = GUI.Button(horizontalScope.rect, "", GUIStyle.none);

                GUILayout.Label(control.icon, menuButtonsIcon, GUILayout.Width(60), GUILayout.Height(35));

                GUILayout.Label(control.name, menuButtonsLabel, GUILayout.ExpandWidth(true), GUILayout.MinWidth(10));



                if (!string.IsNullOrEmpty(control.parameter.name))
                {
                    // Flexible space to push parameter to the right
                    GUILayout.FlexibleSpace();

                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("d_FolderOpened Icon"), menuButtonsLabel, GUILayout.Width(30));
                    }

                    Color defCol = GUI.color;
                    GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
                    GUILayout.Label("|", menuButtonsParam, GUILayout.Width(10));

                    if (!ContainsParameter(control.parameter.name))
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("d_console.warnicon", "Parameter was not found in the avatar's Expression Paramaters"), 
                            menuButtonsParam, GUILayout.Width(20));
                        GUI.color = new Color(0.9f, 0.8f, 0.55f, 0.6f);
                        GUILayout.Label(control.parameter.name, menuButtonsParam, GUILayout.Width(60));
                    }
                    else
                        GUILayout.Label(control.parameter.name, menuButtonsParam, GUILayout.Width(80));
                    GUI.color = defCol;
                }
                else if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                        GUILayout.Label(EditorGUIUtility.IconContent("d_FolderOpened Icon"), menuButtonsLabel, GUILayout.Width(30));
                }



                // If a submenu is clicked, add it to the list
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    if (isClicked && control.subMenu != null)
                    {
                        controlList.Add(control.subMenu);
                    }
            }

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

                    if (DragAndDrop.objectReferences[0] as Texture2D != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }

                    if (e.type == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences[0] as Texture2D != null)
                        {
                            DragAndDrop.AcceptDrag();
                            texture = DragAndDrop.objectReferences[0] as Texture2D;
                            EditorUtility.SetDirty(cont);
                        }
                        else if (DragAndDrop.objectReferences[0] as VRCExpressionsMenu != null)
                        {
                            DragAndDrop.AcceptDrag();
                            texture = DragAndDrop.objectReferences[0] as Texture2D;
                            EditorUtility.SetDirty(cont);
                        }

                    }
                    break;
            }
        }

        private void CheckDragAndDrop(ref Texture2D texture, ref VRCExpressionsMenu submenu, VRCExpressionsMenu cont)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
                        return;

                    if (DragAndDrop.objectReferences[0] as Texture2D != null || DragAndDrop.objectReferences[0] as VRCExpressionsMenu != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }

                    if (e.type == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences[0] as Texture2D != null)
                        {

                            DragAndDrop.AcceptDrag();
                            texture = DragAndDrop.objectReferences[0] as Texture2D;
                            EditorUtility.SetDirty(cont);
                        }
                        else if (DragAndDrop.objectReferences[0] as VRCExpressionsMenu != null)
                        {
                            DragAndDrop.AcceptDrag();
                            submenu = DragAndDrop.objectReferences[0] as VRCExpressionsMenu;
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

                    if (DragAndDrop.objectReferences[0] as VRCExpressionsMenu != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }

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

            if (!useSelected && avatarDescriptor != null && avatarDescriptor.customExpressions && avatarDescriptor.expressionsMenu != null)
            {
                menu = avatarDescriptor.expressionsMenu;
                controlList.Add(menu);
            }
            else if (useSelected)
            {
                if (Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets).Length > 0)
                    menu = Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets)[0];
            }
        }

        bool ContainsParameter(string parameter)
        {
            if (avatarDescriptor != null && avatarDescriptor.expressionParameters != null)
            {
                List<string> parameterNames = new List<string> { };
                foreach (var item in avatarDescriptor.expressionParameters.parameters)
                {
                    parameterNames.Add(item.name);
                }
                return parameterNames.Contains(parameter);
            }
            else
            {
                return true;
            }
        }

        bool RequirementsMet(bool showWarning = true)
        {
            if (!useSelected && avatarDescriptor == null)
            {
                if (showWarning)
                {
                    EditorGUILayout.HelpBox("No avatar descriptor selected", MessageType.Error);
                    EditorGUILayout.Space(40);
                }
                return false;
            }
            else if (!useSelected && (!avatarDescriptor.customExpressions || avatarDescriptor.expressionParameters == null || avatarDescriptor.expressionsMenu == null)) 
            {
                if (showWarning)
                {
                    EditorGUILayout.HelpBox("Expression Menu or Expression Parameters cannot be accessed", MessageType.Error);
                    EditorGUILayout.Space(40);
                }
                return false;
            }
            else if (useSelected && Selection.GetFiltered<VRCExpressionsMenu>(SelectionMode.Assets).Length < 1)
            {
                if (showWarning)
                {
                    EditorGUILayout.HelpBox("No menu selected", MessageType.Error);
                    EditorGUILayout.Space(40);
                }
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
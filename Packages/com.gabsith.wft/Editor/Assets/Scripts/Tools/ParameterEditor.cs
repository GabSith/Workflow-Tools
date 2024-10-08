﻿#if UNITY_EDITOR


using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using System;

using UnityEditor.AnimatedValues;
using UnityEngine.Events;


namespace GabSith.WFT
{
    public class ParameterEditor : EditorWindow
    {
        private GameObject myGameObject;


        VRCAvatarDescriptor[] avatarDescriptorsFromScene;
        VRCAvatarDescriptor avatarDescriptor;
        Vector2 scrollPosDescriptors;

        string[] valueTypeOptions = new string[3] { "Int", "Float", "Bool" };
        //int valueTypeSelectedIndex = 2;

        VRCExpressionParameters.Parameter newParameter = new VRCExpressionParameters.Parameter { };

        string[] selectedConvModes = new string[6] { "Expression", "FX", "Gesture", "Action", "Base", "Additive" };

        //bool settingsMode = false;
        //bool searchMode = false;
        bool editingOptions = false;
        bool caseSensitive = false;
        string find = "";
        string replace = "";
        Vector2 scrollPosFoundNames;
        bool highlightMode = false;
        List<bool> highlightHide = new List<bool> { };

        AnimBool settingsMode = new AnimBool(false);
        AnimBool searchMode = new AnimBool(false);
        AnimBool globalCreationMode = new AnimBool(false);


        //bool globalCreationMode = false;
        bool[] globalCreationBools = new bool[6] { true, true, false, false, false, false };

        bool[] changesMadeGlobalDelay = new bool[5] { false, false, false, false, false };
        int selectedParamMode;
        Color defaultColor;

        Rect ghostRect;

        private List<VRCExpressionParameters.Parameter> items;
        List<AnimatorControllerParameter> itemsController = new List<AnimatorControllerParameter>();

        private int currentlyDraggingItemIndex = -1;
        private Vector2 scrollPos;

        bool showClone = true;
        bool showDelete = true;

        [MenuItem("GabSith/Parameter Editor", false, 2)]


        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(ParameterEditor), false, "Parameter Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image, text = "Parameter Editor", tooltip = "♥" };
            w.minSize = new Vector2(350, 400);
            w.autoRepaintOnSceneChange = true;
        }

        private void OnEnable()
        {
            defaultColor = GUI.backgroundColor;
            settingsMode.valueChanged.AddListener(new UnityAction(Repaint));
            searchMode.valueChanged.AddListener(new UnityAction(Repaint));
            globalCreationMode.valueChanged.AddListener(new UnityAction(Repaint));

        }

        private void OnBecameVisible()
        {
            if (avatarDescriptor == null)
            {
                CommonActions.RefreshDescriptors(ref avatarDescriptor, ref avatarDescriptorsFromScene);
            }
        }


        private void OnGUI()
        {
            GUI.SetNextControlName("NotText");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title
            CommonActions.GenerateTitle("Parameter Editor");

            // Avatar Selection
            CommonActions.FindAvatars(ref avatarDescriptor, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);


            // Mode Toolbar
            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            selectedParamMode = GUILayout.Toolbar(selectedParamMode, selectedConvModes);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl("NotText");
            }
            items = new List<VRCExpressionParameters.Parameter>();

            if (selectedParamMode == 0)
            {
                if (avatarDescriptor != null && avatarDescriptor.expressionParameters != null)
                {
                    foreach (var item in avatarDescriptor.expressionParameters.parameters)
                    {
                        items.Add(item);
                    }
                }
                ExpressionParametersEdit();
            }
            else
            {
                itemsController = new List<AnimatorControllerParameter>();

                if (avatarDescriptor != null && avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController != null)
                {
                    AnimatorController animatorController = (AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController;
                    foreach (var item in animatorController.parameters)
                    {
                        itemsController.Add(item);
                    }
                }
                ControllerParametersEdit(SelectedParameterMapping(selectedParamMode));
            }


            EditorGUILayout.EndVertical();

        }
        void ExtraBar(List<VRCExpressionParameters.Parameter> expressionParameters = null, List<AnimatorControllerParameter> controllerParameters = null)
        {

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New", GUILayout.Height(25)))
            {
                if (expressionParameters != null)
                {
                    scrollPos += new Vector2(0, 999999999);
                    expressionParameters.Add(new VRCExpressionParameters.Parameter { name = "New Parameter", valueType = VRCExpressionParameters.ValueType.Bool });

                }
                else if (controllerParameters != null)
                {
                    scrollPos += new Vector2(0, 999999999);
                    AnimatorControllerParameter newParam = new AnimatorControllerParameter { name = "New Parameter", type = AnimatorControllerParameterType.Bool };

                    itemsController.Add(newParam);
                }
            }

            // Create Global
            if (CommonActions.ToggleButton("▼ Create Global ▼", globalCreationMode.target, GUILayout.Height(25f)))
            {
                settingsMode.target = false;
                searchMode.target = false;
                globalCreationMode.target = !globalCreationMode.target;
            }

            // Backup Icon: CustomTool@2x
            // Search Mode
            if (CommonActions.ToggleButton(EditorGUIUtility.IconContent("d_Search Icon"), searchMode.target, GUILayout.Height(25f), GUILayout.Width(25)))
            {
                globalCreationMode.target = false;
                settingsMode.target = false;
                searchMode.target = !searchMode.target;
            }

            // Settings
            if (CommonActions.ToggleButton(EditorGUIUtility.IconContent("_Popup@2x"), settingsMode.target, GUILayout.Height(25f), GUILayout.Width(25)))
            {
                globalCreationMode.target = false;
                searchMode.target = false;
                settingsMode.target = !settingsMode.target;
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();

            using (var group = new EditorGUILayout.FadeGroupScope(globalCreationMode.faded))

                if (group.visible && CreateGlobal())
                {
                    for (int i = 0; i < changesMadeGlobalDelay.Length; i++)
                    {
                        if (changesMadeGlobalDelay[i])
                        {
                            changesMadeGlobalDelay[i] = false;
                        }
                    }
                }

            using (var group = new EditorGUILayout.FadeGroupScope(searchMode.faded))

                if (group.visible)
                {
                    List<string> names = new List<string> { };
                    List<string> newNames = new List<string> { };

                    if (expressionParameters != null)
                    {
                        foreach (var item in expressionParameters)
                        {
                            names.Add(item.name);
                        }
                    }
                    else
                    {
                        foreach (var item in controllerParameters)
                        {
                            names.Add(item.name);
                        }
                    }

                    if (expressionParameters != null)
                    {
                        newNames = EditMode(names, expressionParameters);
                    }
                    else
                    {
                        newNames = EditMode(names, null, controllerParameters);
                    }



                    if (newNames != null)
                    {
                        if (expressionParameters != null)
                        {
                            for (int i = 0; i < expressionParameters.Count; i++)
                            {
                                Debug.Log(expressionParameters[i].name + "=" + newNames[i]);

                                expressionParameters[i].name = newNames[i];
                            }
                            EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                        }
                        else if (controllerParameters != null)
                        {
                            for (int i = 0; i < controllerParameters.Count; i++)
                            {
                                controllerParameters[i].name = newNames[i];
                            }
                            EditorUtility.SetDirty((AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController);
                        }
                    }

                }

            using (var group = new EditorGUILayout.FadeGroupScope(settingsMode.faded))
                if (group.visible)
                {
                    showClone = EditorGUILayout.ToggleLeft("Show Clone", showClone);
                    showDelete = EditorGUILayout.ToggleLeft("Show Delete", showDelete);
                }

            EditorGUILayout.Space();

            for (int i = 0; i < changesMadeGlobalDelay.Length; i++)
            {
                if (!changesMadeGlobalDelay[i])
                {
                    changesMadeGlobalDelay[i] = true;
                    return;
                }
            }

            if (expressionParameters != null)
            {
                if (avatarDescriptor.expressionParameters.parameters != items.ToArray())
                {
                    avatarDescriptor.expressionParameters.parameters = items.ToArray();
                    if (changesMadeGlobalDelay[4])
                    {
                        Undo.RecordObject(avatarDescriptor.expressionParameters, "Changes To Expressions Parametes");
                    }
                    EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                }
            }
            else if (controllerParameters != null)
            {
                if (((AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController).parameters != itemsController.ToArray())
                {
                    ((AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController).parameters = itemsController.ToArray();

                    if (changesMadeGlobalDelay[4])
                    {
                        Undo.RecordObject((AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController, "Changes To Controller");
                    }
                    EditorUtility.SetDirty((AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController);
                }
            }

        }

        void ExpressionParametersEdit()
        {
            {
                if (avatarDescriptor == null || avatarDescriptor.expressionParameters == null)
                {
                    return;
                }

                Event e = Event.current;
                GUILayout.Space(10);

                EditorGUI.BeginChangeCheck();
                avatarDescriptor.expressionParameters = (VRCExpressionParameters)EditorGUILayout.ObjectField("", avatarDescriptor.expressionParameters, typeof(VRCExpressionParameters), false);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(avatarDescriptor);
                    return;
                }


                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                // Header
                {
                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Space(25);
                    // Name
                    GUILayout.Label("Name");
                    GUILayout.Space(10);

                    // Value Type
                    GUILayout.Label("Value Type", GUILayout.Width(70));
                    GUILayout.Space(10);

                    // Default
                    GUILayout.Label("Default", GUILayout.Width(50));
                    GUILayout.Space(10);

                    // Saved
                    GUILayout.Label("Saved", GUILayout.Width(50));

                    // Synced
                    GUILayout.Label("Synced", GUILayout.Width(50));

                    // Copy
                    if (showClone)
                        GUILayout.Label("Clone", GUILayout.Width(50));

                    // Delete
                    if (showDelete)
                        GUILayout.Label("Delete", GUILayout.Width(50));

                    EditorGUILayout.EndHorizontal();
                }


                for (int i = 0; i < items.Count; i++)
                {
                    if (searchMode.target && highlightMode && highlightHide.Count > i && !highlightHide[i])
                    {
                        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }



                    if (i == currentlyDraggingItemIndex)
                    {
                        GUI.color = Color.grey; // Change the color of the item being dragged
                        GUI.FocusControl("NotText");
   
                    }


                    {
                        EditorGUILayout.BeginHorizontal("box");

                        GUILayout.Label(EditorGUIUtility.IconContent("_Menu"), GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(25));

                        // Name
                        items[i].name = EditorGUILayout.TextField(items[i].name);
                        GUILayout.Space(10);

                        // Value Type
                        int valueTypeSelectedIndex = ValueToIndex(items[i].valueType);
                        valueTypeSelectedIndex = EditorGUILayout.Popup("", valueTypeSelectedIndex, valueTypeOptions, GUILayout.Width(70));
                        items[i].valueType = IndexToValue(valueTypeSelectedIndex);
                        GUILayout.Space(10);

                        // Default
                        switch (items[i].valueType)
                        {
                            case VRCExpressionParameters.ValueType.Int:
                                items[i].defaultValue = EditorGUILayout.IntField(Convert.ToInt32(items[i].defaultValue), GUILayout.Width(50));
                                break;
                            case VRCExpressionParameters.ValueType.Float:
                                items[i].defaultValue = EditorGUILayout.FloatField(items[i].defaultValue, GUILayout.Width(50));
                                break;
                            case VRCExpressionParameters.ValueType.Bool:
                                items[i].defaultValue = Convert.ToSingle(EditorGUILayout.Toggle(Convert.ToBoolean(items[i].defaultValue), GUILayout.Width(50)));
                                break;
                            default:
                                break;
                        }
                        GUILayout.Space(10);

                        // Saved
                        items[i].saved = EditorGUILayout.Toggle(items[i].saved, GUILayout.Width(50));

                        // Synced
                        items[i].networkSynced = EditorGUILayout.Toggle(items[i].networkSynced, GUILayout.Width(50));

                        // Copy
                        if (showClone)
                            if (GUILayout.Button("Clone", GUILayout.Width(50)))
                            {
                                items.Add(new VRCExpressionParameters.Parameter
                                {
                                    name = items[i].name,
                                    defaultValue = items[i].defaultValue,
                                    networkSynced = items[i].networkSynced,
                                    saved = items[i].saved,
                                    valueType = items[i].valueType
                                });
                                scrollPos += new Vector2(0, 999999);
                            }

                        // Delete
                        if (showDelete)
                            if (GUILayout.Button("Delete", GUILayout.Width(50)))
                            {
                                items.RemoveAt(i);
                            }

                        EditorGUILayout.EndHorizontal();

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
                        items = new List<VRCExpressionParameters.Parameter>(items);
                        currentlyDraggingItemIndex = -1;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        Repaint(); // Force the window to repaint
                    }

                    // Draw a ghost item at the mouse position while dragging
                    if (currentlyDraggingItemIndex > -1)
                    {
                        ghostRect = new Rect(dropArea.x, e.mousePosition.y - (dropArea.height / 2), dropArea.width, dropArea.height);
                    }

                    if (currentlyDraggingItemIndex > -1 && dropArea.Contains(e.mousePosition))
                    {
                        VRCExpressionParameters.Parameter temp = items[currentlyDraggingItemIndex];
                        items.RemoveAt(currentlyDraggingItemIndex);
                        items.Insert(i, temp);
                        currentlyDraggingItemIndex = i;
                        Repaint(); // Force the window to repaint
                    }

                }

                //Debug.Log(mouseOverWindow == EditorWindow.GetWindow(typeof(ParameterEditor), false, "Parameter Editor"));


                if (currentlyDraggingItemIndex > -1 && mouseOverWindow == GetWindow(typeof(ParameterEditor)))
                {
                    EditorGUI.DrawRect(ghostRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                }

                // If the mouse button is released outside of any item, reset currentlyDraggingItemIndex
                if ((e.type == EventType.Ignore || e.type == EventType.MouseUp) && currentlyDraggingItemIndex > -1)
                {
                    currentlyDraggingItemIndex = -1;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    Repaint(); // Force the window to repaint
                }

                EditorGUILayout.EndScrollView();

                if (avatarDescriptor.expressionParameters.CalcTotalCost() > VRCExpressionParameters.MAX_PARAMETER_COST)
                {
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                }

                EditorGUILayout.HelpBox("Total Memory: " + avatarDescriptor.expressionParameters.CalcTotalCost() + "/" + VRCExpressionParameters.MAX_PARAMETER_COST, MessageType.Info);
                GUI.color = defaultColor;


                ExtraBar(items, null);

                /*
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create New", GUILayout.Height(25)))
                {
                    scrollPos += new Vector2(0, 999999999);
                    items.Add(new VRCExpressionParameters.Parameter { name = "New Parameter", valueType = VRCExpressionParameters.ValueType.Bool });
                }

                if (globalCreationMode)
                {
                    GUI.color = new Color(0.5f, 0.8f, 0.5f);
                }

                if (GUILayout.Button("▼ Create Global ▼", GUILayout.Height(25)))
                {
                    settingsMode = false;
                    editMode = false;
                    globalCreationMode = !globalCreationMode;
                }
                GUI.color = defaultColor;

                if (editMode)
                {
                    GUI.color = new Color(0.5f, 0.8f, 0.5f);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("CustomTool@2x"), GUILayout.Height(25), GUILayout.Width(25))) // Edit Mode
                {
                    globalCreationMode = false;
                    settingsMode = false;
                    editMode = !editMode;
                }
                GUI.color = defaultColor;

                if (settingsMode)
                {
                    GUI.color = new Color(0.5f, 0.8f, 0.5f);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup@2x"), GUILayout.Height(25), GUILayout.Width(25))) // Settings
                {
                    globalCreationMode = false;
                    editMode = false;
                    settingsMode = !settingsMode;
                }
                GUI.color = defaultColor;

                EditorGUILayout.EndHorizontal();


                if (globalCreationMode && CreateGlobal())
                {
                    for (int i = 0; i < changesMadeGlobalDelay.Length; i++)
                    {
                        if (changesMadeGlobalDelay[i])
                        {
                            changesMadeGlobalDelay[i] = false;
                        }
                    }
                }

                if (editMode)
                {
                    List<string> names = new List<string> { };

                    foreach (var item in items)
                    {
                        names.Add(item.name);
                    }

                    EditMode(names);
                }

                if (settingsMode)
                {
                    showDelete = EditorGUILayout.ToggleLeft("Show Delete", showDelete);
                }

                EditorGUILayout.Space();

                for (int i = 0; i < changesMadeGlobalDelay.Length; i++)
                {
                    if (!changesMadeGlobalDelay[i])
                    {
                        changesMadeGlobalDelay[i] = true;
                        return;
                    }
                }

                avatarDescriptor.expressionParameters.parameters = items.ToArray();
                if (changesMadeGlobalDelay[4])
                    Undo.RecordObject(avatarDescriptor.expressionParameters, "Changes To Expressions Parametes");
                */
            }

        }

        void ControllerParametersEdit(int controllerIndex)
        {
            {
                if (avatarDescriptor == null || avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController == null)
                {
                    return;
                }



                Event e = Event.current;
                GUILayout.Space(10);

                EditorGUI.BeginChangeCheck();
                avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("", (AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController, typeof(AnimatorController), false);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(avatarDescriptor);
                    return;
                }

                EditorGUILayout.HelpBox("Modifying parameters here might break existing transitions/drivers", MessageType.Info);


                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                // Header
                {
                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Space(25);
                    // Name
                    GUILayout.Label("Name");
                    GUILayout.Space(10);

                    // Value Type
                    GUILayout.Label("Value Type", GUILayout.Width(70));
                    GUILayout.Space(10);

                    // Default
                    GUILayout.Label("Default", GUILayout.Width(50));
                    GUILayout.Space(10);

                    // Copy
                    if (showClone)
                    {
                        GUILayout.Label("Clone", GUILayout.Width(50));
                        GUILayout.Space(5);
                    }

                    // Delete
                    if (showDelete)
                        GUILayout.Label("Delete", GUILayout.Width(50));

                    EditorGUILayout.EndHorizontal();
                }


                for (int i = 0; i < itemsController.Count; i++)
                {
                    if (searchMode.target && highlightMode && highlightHide.Count > i && !highlightHide[i])
                    {
                        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }


                    if (i == currentlyDraggingItemIndex)
                    {
                        GUI.color = Color.grey; // Change the color of the item being dragged
                    }

                    EditorGUILayout.BeginHorizontal("box");

                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("_Menu"), GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(25));

                        // Name
                        itemsController[i].name = EditorGUILayout.TextField(itemsController[i].name);
                        GUILayout.Space(10);

                        // Value Type
                        int valueTypeSelectedIndex = TypeToIndex(itemsController[i].type);
                        valueTypeSelectedIndex = EditorGUILayout.Popup("", valueTypeSelectedIndex, valueTypeOptions, GUILayout.Width(70));
                        itemsController[i].type = IndexToType(valueTypeSelectedIndex);
                        GUILayout.Space(10);

                        // Default
                        switch (itemsController[i].type)
                        {
                            case AnimatorControllerParameterType.Float:
                                itemsController[i].defaultFloat = EditorGUILayout.FloatField(itemsController[i].defaultFloat, GUILayout.Width(50));
                                break;
                            case AnimatorControllerParameterType.Int:
                                itemsController[i].defaultInt = EditorGUILayout.IntField(itemsController[i].defaultInt, GUILayout.Width(50));
                                break;
                            case AnimatorControllerParameterType.Bool:
                                itemsController[i].defaultBool = EditorGUILayout.Toggle(itemsController[i].defaultBool, GUILayout.Width(50));
                                break;
                            case AnimatorControllerParameterType.Trigger:
                                break;
                            default:
                                break;
                        }
                        GUILayout.Space(10);

                        // Copy
                        if (showClone)
                        {
                            if (GUILayout.Button("Clone", GUILayout.Width(50)))
                            {
                                itemsController.Add(new AnimatorControllerParameter
                                {
                                    name = itemsController[i].name,
                                    defaultBool = itemsController[i].defaultBool,
                                    defaultFloat = itemsController[i].defaultFloat,
                                    defaultInt = itemsController[i].defaultInt,
                                    type = itemsController[i].type
                                });
                                scrollPos += new Vector2(0, 999999);
                            }
                            GUILayout.Space(5);
                        }

                        // Delete
                        if (showDelete)
                            if (GUILayout.Button("Delete", GUILayout.Width(50)))
                            {
                                itemsController.RemoveAt(i);
                            }

                        EditorGUILayout.EndHorizontal();

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
                        itemsController = new List<AnimatorControllerParameter>(itemsController);
                        currentlyDraggingItemIndex = -1;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        Repaint(); // Force the window to repaint
                    }


                    if (currentlyDraggingItemIndex > -1)
                    {
                        Rect ghostRect = new Rect(dropArea.x, e.mousePosition.y - (dropArea.height / 2), dropArea.width, dropArea.height);
                        EditorGUI.DrawRect(ghostRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                    }
                    // Draw a ghost item at the mouse position while dragging
                    if (currentlyDraggingItemIndex > -1 && dropArea.Contains(e.mousePosition))
                    {
                        AnimatorControllerParameter temp = itemsController[currentlyDraggingItemIndex];
                        itemsController.RemoveAt(currentlyDraggingItemIndex);
                        itemsController.Insert(i, temp);
                        currentlyDraggingItemIndex = i;
                        Repaint(); // Force the window to repaint
                    }
                }

                // If the mouse button is released outside of any item, reset currentlyDraggingItemIndex
                if ((e.type == EventType.Ignore || e.type == EventType.MouseUp) && currentlyDraggingItemIndex > -1)
                {
                    //previewItems = new List<string>(items);
                    currentlyDraggingItemIndex = -1;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    Repaint(); // Force the window to repaint
                }

                EditorGUILayout.EndScrollView();


                ExtraBar(null, itemsController);

                /*
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create New"))
                {
                    scrollPos += new Vector2(0, 999999999);

                    AnimatorControllerParameter newParam = new AnimatorControllerParameter { name = "New Parameter", type = AnimatorControllerParameterType.Bool };


                    itemsController.Add(newParam);
                }

                if (globalCreationMode)
                {
                    GUI.color = new Color(0.5f, 0.8f, 0.5f);
                }

                if (GUILayout.Button("▼ Create Global ▼"))
                {
                    globalCreationMode = !globalCreationMode;
                }
                GUI.color = defaultColor;

                EditorGUILayout.EndHorizontal();

                if (globalCreationMode && CreateGlobal())
                {
                    for (int i = 0; i < changesMadeGlobalDelay.Length; i++)
                    {
                        if (changesMadeGlobalDelay[i])
                        {
                            changesMadeGlobalDelay[i] = false;
                        }
                    }
                }

                EditorGUILayout.Space();

                for (int i = 0; i < changesMadeGlobalDelay.Length; i++)
                {
                    if (!changesMadeGlobalDelay[i])
                    {
                        changesMadeGlobalDelay[i] = true;
                        return;
                    }
                }

                ((AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController).parameters = itemsController.ToArray();

                if (changesMadeGlobalDelay[4])
                    Undo.RecordObject((AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController, "Changes To Controller");

                //EditorGUILayout.Space();*/
            }

        }

        List<string> EditMode(List<string> listToEdit, List<VRCExpressionParameters.Parameter> expressionParameters = null, List<AnimatorControllerParameter> controllerParameters = null)
        {

            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                find = EditorGUILayout.TextField("Find: ", find, GUILayout.MinWidth(25));

                if (caseSensitive)
                    GUI.backgroundColor = CommonActions.selectionColor;
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_TrueTypeFontImporter Icon"), GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(25)))
                {
                    caseSensitive = !caseSensitive;
                }
                GUI.backgroundColor = defaultColor;
            }

            EditorGUILayout.Space();
            editingOptions = EditorGUILayout.BeginFoldoutHeaderGroup(editingOptions, "Replace");

            if (editingOptions)
            {
                using (new GUILayout.HorizontalScope())
                {
                    //GUILayout.Label("Replace with: ");
                    replace = EditorGUILayout.TextField("Replace with: ", replace, GUILayout.MinWidth(25));

                    if (GUILayout.Button("Replace", GUILayout.Width(70)))
                    {
                        List<string> editedStrings = new List<string>(listToEdit);

                        for (int i = 0; i < editedStrings.Count; i++)
                        {
                            if (caseSensitive && !string.IsNullOrEmpty(find) && find != " " && editedStrings[i].Contains(find))
                            {
                                editedStrings[i] = editedStrings[i].Replace(find, replace);
                            }
                            if (!caseSensitive && !string.IsNullOrEmpty(find) && find != " " && editedStrings[i].IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string start = editedStrings[i].Remove(editedStrings[i].ToLower().IndexOf(find.ToLower()));
                                string end = editedStrings[i].Substring(editedStrings[i].ToLower().IndexOf(find.ToLower()) + find.Length);

                                editedStrings[i] = start + replace + end;
                            }
                        }

                        return editedStrings;
                    }
                }

                EditorGUILayout.Space();

                /*
                GUI.color += new Color(0.5f, -0.2f, -0.2f);
                if (GUILayout.Button("Delete All Found"))
                {
                    if (expressionParameters != null)
                    {
                        for (int i = 0; i < listToEdit.Count; i++)
                        {
                            if (caseSensitive && !string.IsNullOrEmpty(find) && find != " " && items[i].name.Contains(find))
                            {
                                Debug.Log(items[i].name);

                                items.Remove(items[i]);
                            }
                            else if (!caseSensitive && !string.IsNullOrEmpty(find) && find != " " && listToEdit[i].IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Debug.Log(items[i].name);
                                //Debug.Log(items.Count);
                                items.Remove(items[i]);
                            }
                        }


                    }
                    else if (controllerParameters != null)
                    {
                        for (int i = 0; i < controllerParameters.Count; i++)
                        {
                            if (caseSensitive && !string.IsNullOrEmpty(find) && find != " " && controllerParameters[i].name.Contains(find))
                            {
                                itemsController.Remove(controllerParameters[i]);
                            }
                            if (!caseSensitive && !string.IsNullOrEmpty(find) && find != " " && controllerParameters[i].name.IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                itemsController.Remove(controllerParameters[i]);
                            }
                        }
                    }
                }*/
                GUI.color = defaultColor;
            }

            EditorGUILayout.Space();

            highlightMode = false;
            highlightHide = new List<bool>();

            // Found Names
            scrollPosFoundNames = EditorGUILayout.BeginScrollView(scrollPosFoundNames, 
                EditorStyles.helpBox, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false), GUILayout.Height(100f));
            {

                for (int i = 0; i < listToEdit.Count; i++)
                {
                    highlightHide.Add(false);


                    if (caseSensitive && !string.IsNullOrEmpty(find) && find != " " && listToEdit[i].Contains(find))
                    {
                        if (highlightHide.Count > i)
                        {
                            highlightHide[i] = true;
                            highlightMode = true;
                        }
                        else
                        {
                            Debug.Log("Index " + i + " was not found");
                        }
                        GUILayout.Label(listToEdit[i], GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    else if (!caseSensitive && !string.IsNullOrEmpty(find) && find != " " && listToEdit[i].IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (highlightHide.Count > i)
                        {
                            highlightHide[i] = true;
                            highlightMode = true;
                        }
                        else
                        {
                            Debug.Log("Index " + i + " was not found");
                        }
                        GUILayout.Label(listToEdit[i], GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }

                }

            }

            EditorGUILayout.EndScrollView();

            return null;
        }


        bool CreateGlobal()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < selectedConvModes.Length; i++)
            {
                globalCreationBools[i] = EditorGUILayout.ToggleLeft(selectedConvModes[i], globalCreationBools[i], GUILayout.Width(Screen.width / 6));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Header
            {
                EditorGUILayout.BeginHorizontal("box");
                // Name
                GUILayout.Label("Name");
                GUILayout.Space(10);

                // Value Type
                GUILayout.Label("Value Type", GUILayout.Width(70));
                GUILayout.Space(10);

                // Default
                GUILayout.Label("Default", GUILayout.Width(50));
                GUILayout.Space(10);

                // Saved
                GUILayout.Label("Saved", GUILayout.Width(50));

                // Synced
                GUILayout.Label("Synced", GUILayout.Width(50));

                EditorGUILayout.EndHorizontal();
            }

            // New Parameter

            EditorGUILayout.BeginHorizontal("box");

            // Name
            newParameter.name = EditorGUILayout.TextField(newParameter.name);
            GUILayout.Space(10);

            // Value Type
            int valueTypeSelectedIndex = ValueToIndex(newParameter.valueType);
            valueTypeSelectedIndex = EditorGUILayout.Popup("", valueTypeSelectedIndex, valueTypeOptions, GUILayout.Width(70));
            newParameter.valueType = IndexToValue(valueTypeSelectedIndex);
            GUILayout.Space(10);

            // Default
            switch (newParameter.valueType)
            {
                case VRCExpressionParameters.ValueType.Int:
                    newParameter.defaultValue = EditorGUILayout.IntField(Convert.ToInt32(newParameter.defaultValue), GUILayout.Width(50));
                    break;
                case VRCExpressionParameters.ValueType.Float:
                    newParameter.defaultValue = EditorGUILayout.FloatField(newParameter.defaultValue, GUILayout.Width(50));
                    break;
                case VRCExpressionParameters.ValueType.Bool:
                    newParameter.defaultValue = Convert.ToSingle(EditorGUILayout.Toggle(Convert.ToBoolean(newParameter.defaultValue), GUILayout.Width(50)));
                    break;
                default:
                    break;
            }
            GUILayout.Space(10);

            // Saved
            newParameter.saved = EditorGUILayout.Toggle(newParameter.saved, GUILayout.Width(50));

            // Synced
            newParameter.networkSynced = EditorGUILayout.Toggle(newParameter.networkSynced, GUILayout.Width(50));


            EditorGUILayout.EndHorizontal();

            // Check if it exists
            List<string> parameterNames = new List<string> { };
            if (globalCreationBools[0]) // Expression Selected
            {
                foreach (var item in avatarDescriptor.expressionParameters.parameters)
                {
                    parameterNames.Add(item.name);
                }
            }
            if (globalCreationBools[1]) // FX Selected
            {
                RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(1)].animatorController;

                if (controllerRuntime != null)
                {
                    foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                    {
                        parameterNames.Add(item.name);
                    }
                }
            }
            if (globalCreationBools[2]) // Gesture Selected
            {
                RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(2)].animatorController;

                if (controllerRuntime != null)
                {
                    foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                    {
                        parameterNames.Add(item.name);
                    }
                }
            }
            if (globalCreationBools[3]) // Action Selected
            {
                RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(3)].animatorController;

                if (controllerRuntime != null)
                {
                    foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                    {
                        parameterNames.Add(item.name);
                    }
                }
            }
            if (globalCreationBools[4]) // Base Selected
            {
                RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(4)].animatorController;

                if (controllerRuntime != null)
                {
                    foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                    {
                        parameterNames.Add(item.name);
                    }
                }
            }
            if (globalCreationBools[5]) // Add Selected
            {
                RuntimeAnimatorController controllerRuntime = avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(5)].animatorController;

                if (controllerRuntime != null)
                {
                    foreach (var item in ((AnimatorController)controllerRuntime).parameters)
                    {
                        parameterNames.Add(item.name);
                    }
                }
            }

            if (parameterNames.Contains(newParameter.name))
            {
                EditorGUILayout.HelpBox(newParameter.name + " already exists! It'll be replaced", MessageType.Warning);
            }


            if (GUILayout.Button("Add to selected", GUILayout.Height(25f)))
            {
                List<UnityEngine.Object> objectsToCheck = new List<UnityEngine.Object> { avatarDescriptor.expressionParameters,
                avatarDescriptor.baseAnimationLayers[0].animatorController,
                avatarDescriptor.baseAnimationLayers[1].animatorController,
                avatarDescriptor.baseAnimationLayers[2].animatorController,
                avatarDescriptor.baseAnimationLayers[3].animatorController,
                avatarDescriptor.baseAnimationLayers[4].animatorController};

                objectsToCheck.RemoveAll(item => item == null);

                Undo.RecordObjects(objectsToCheck.ToArray(), "Global Parameter");


                if (globalCreationBools[0]) // Expressions Parameters Selected
                {
                    VRCExpressionParameters.Parameter copy = new VRCExpressionParameters.Parameter
                    {
                        name = newParameter.name,
                        defaultValue = newParameter.defaultValue,
                        networkSynced = newParameter.networkSynced,
                        saved = newParameter.saved,
                        valueType = newParameter.valueType
                    };

                    items = new List<VRCExpressionParameters.Parameter>();
                    foreach (var item in avatarDescriptor.expressionParameters.parameters)
                    {
                        items.Add(item);
                    }

                    // Remove the parameter if it exists
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i].name == copy.name)
                        {
                            items.RemoveAt(i);
                        }
                    }

                    items.Add(copy);
                    avatarDescriptor.expressionParameters.parameters = items.ToArray();
                }
                if (globalCreationBools[1]) // FX Selected
                {
                    GlobalControllerAdd(4, valueTypeSelectedIndex);
                }
                if (globalCreationBools[2]) // Gesture Selected
                {
                    GlobalControllerAdd(2, valueTypeSelectedIndex);
                }
                if (globalCreationBools[3]) // Action Selected
                {
                    GlobalControllerAdd(3, valueTypeSelectedIndex);
                }
                if (globalCreationBools[4]) // Base Selected
                {
                    GlobalControllerAdd(0, valueTypeSelectedIndex);
                }
                if (globalCreationBools[5]) // Add Selected
                {
                    GlobalControllerAdd(1, valueTypeSelectedIndex);
                }

                return true;

            }
            return false;
        }

        void GlobalControllerAdd(int controllerIndex, int valueTypeSelectedIndex)
        {
            if (avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController)
            {
                AnimatorControllerParameter newControllerParameter = new AnimatorControllerParameter
                {
                    name = newParameter.name,
                    defaultBool = Convert.ToBoolean(newParameter.defaultValue),
                    defaultFloat = newParameter.defaultValue,
                    defaultInt = Convert.ToInt32(newParameter.defaultValue),
                    type = IndexToType(valueTypeSelectedIndex)
                };

                itemsController = new List<AnimatorControllerParameter>();

                AnimatorController animatorController = (AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController;
                foreach (var item in animatorController.parameters)
                {
                    itemsController.Add(item);
                }

                // Remove the parameter if it exists
                for (int i = 0; i < itemsController.Count; i++)
                {
                    if (itemsController[i].name == newControllerParameter.name)
                    {
                        itemsController.RemoveAt(i);
                    }
                }

                itemsController.Add(newControllerParameter);
                ((AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController).parameters = itemsController.ToArray();
            }
            else
            {
                Debug.Log("The selected animator does not exist");
            }
        }

        int SelectedParameterMapping(int selectedParameter)
        {
            switch (selectedParameter)
            {
                case 0:
                    break;
                case 1: // FX
                    return 4;
                case 2: // Gesture
                    return 2;
                case 3: // Action
                    return 3;
                case 4: // Base
                    return 0;
                case 5: // Additive
                    return 1;
                default:
                    break;
            }

            return 4;
        }

        int TypeToIndex(AnimatorControllerParameterType type)
        {
            switch (type)
            {
                case AnimatorControllerParameterType.Int:
                    return 0;
                case AnimatorControllerParameterType.Float:
                    return 1;
                case AnimatorControllerParameterType.Bool:
                    return 2;
                case AnimatorControllerParameterType.Trigger:
                    return 2;
                default:
                    return 2;
            }
        }

        AnimatorControllerParameterType IndexToType(int index)
        {
            switch (index)
            {
                case 0:
                    return AnimatorControllerParameterType.Int;
                case 1:
                    return AnimatorControllerParameterType.Float;
                case 2:
                    return AnimatorControllerParameterType.Bool;
                default:
                    return AnimatorControllerParameterType.Bool;
            }

        }


        int ValueToIndex(VRCExpressionParameters.ValueType valueType)
        {
            switch (valueType)
            {
                case VRCExpressionParameters.ValueType.Int:
                    return 0;
                case VRCExpressionParameters.ValueType.Float:
                    return 1;
                case VRCExpressionParameters.ValueType.Bool:
                    return 2;
                default:
                    return 2;
            }
        }

        public VRCExpressionParameters.ValueType IndexToValue(int index)
        {
            switch (index)
            {
                case 0:
                    return VRCExpressionParameters.ValueType.Int;
                case 1:
                    return VRCExpressionParameters.ValueType.Float;
                case 2:
                    return VRCExpressionParameters.ValueType.Bool;
                default:
                    return VRCExpressionParameters.ValueType.Bool;
            }

        }


    }
}
#endif
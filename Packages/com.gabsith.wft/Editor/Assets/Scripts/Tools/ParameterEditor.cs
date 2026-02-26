#if UNITY_EDITOR


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
        VRCAvatarDescriptor[] avatarDescriptorsFromScene;
        VRCAvatarDescriptor avatarDescriptor;
        Vector2 scrollPosDescriptors;

        string[] valueTypeOptions = new string[3] { "Int", "Float", "Bool" };

        VRCExpressionParameters.Parameter newParameter = new VRCExpressionParameters.Parameter { };

        string[] selectedConvModes = new string[6] { "Expression", "FX", "Gesture", "Action", "Base", "Additive" };

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

        bool[] globalCreationBools = new bool[6] { true, true, false, false, false, false };

        bool[] changesMadeGlobalDelay = new bool[5] { false, false, false, false, false };
        int selectedParamMode;
        Color defaultColor;

        Rect ghostRect;

        private List<VRCExpressionParameters.Parameter> items;
        List<AnimatorControllerParameter> itemsController = new List<AnimatorControllerParameter>();

        private int currentlyDraggingItemIndex = -1;
        private Vector2 scrollPos;
        private int pendingDeleteIndex = -1;

        private const string ShowCloneKey = "ParameterEditorShowClone";
        private const string ShowDeleteKey = "ParameterEditorShowDelete";
        private const string ModifyTransitionsKey = "ParameterEditorModifyTransitions";

        // Rects captured from the first rendered row — used to position sticky header labels
        // exactly over the corresponding columns regardless of window width or optional columns.
        private Rect _exprHdrName, _exprHdrType, _exprHdrDefault, _exprHdrSaved, _exprHdrSynced, _exprHdrClone, _exprHdrDelete;
        private Rect _ctrlHdrName, _ctrlHdrType, _ctrlHdrDefault, _ctrlHdrClone, _ctrlHdrDelete;

        GUIStyle tableHeaderStyle;
        float typeWidth = 50f;
        float defaultWidth = 40f;
        float savedWidth = 40f;
        float syncedWidth = 40f;
        float cloneWidth = 35f;
        float deleteWidth = 35f;

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
            wantsMouseMove = true;
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
            if (tableHeaderStyle == null)
            {
                tableHeaderStyle = new GUIStyle(EditorStyles.label);
                tableHeaderStyle.fontSize = 11;
            }

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
                    Undo.RecordObject(avatarDescriptor.expressionParameters, "Create Parameter");
                    scrollPos += new Vector2(0, 999999999);
                    expressionParameters.Add(new VRCExpressionParameters.Parameter { name = "New Parameter", valueType = VRCExpressionParameters.ValueType.Bool });
                    EditorUtility.SetDirty(avatarDescriptor.expressionParameters);

                }
                else if (controllerParameters != null)
                {
                    AnimatorController animCtrl = (AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController;
                    Undo.RecordObject(animCtrl, "Create Parameter");
                    scrollPos += new Vector2(0, 999999999);
                    AnimatorControllerParameter newParam = new AnimatorControllerParameter { name = "New Parameter", type = AnimatorControllerParameterType.Bool };

                    itemsController.Add(newParam);
                    EditorUtility.SetDirty(animCtrl);
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
                            Undo.RecordObject(avatarDescriptor.expressionParameters, "Replace Parameter Names");
                            for (int i = 0; i < expressionParameters.Count; i++)
                            {
                                Debug.Log(expressionParameters[i].name + "=" + newNames[i]);

                                expressionParameters[i].name = newNames[i];
                            }
                            EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                        }
                        else if (controllerParameters != null)
                        {
                            AnimatorController animCtrl = (AnimatorController)avatarDescriptor.baseAnimationLayers[SelectedParameterMapping(selectedParamMode)].animatorController;
                            Undo.RecordObject(animCtrl, "Replace Parameter Names");
                            for (int i = 0; i < controllerParameters.Count; i++)
                            {
                                controllerParameters[i].name = newNames[i];
                            }
                            EditorUtility.SetDirty(animCtrl);
                        }
                    }

                }

            using (var group = new EditorGUILayout.FadeGroupScope(settingsMode.faded))
                if (group.visible)
                {
                    ProjectSettingsManager.EditorGUIBool(ShowCloneKey, "Show Clone", true);
                    ProjectSettingsManager.EditorGUIBool(ShowDeleteKey, "Show Delete", true);
                    ProjectSettingsManager.EditorGUIBool(ModifyTransitionsKey, "Modify Existing Transitions", true);
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


                // Header (pinned/sticky) + item list
                bool showCloneNow = ProjectSettingsManager.GetBool(ShowCloneKey, true);
                bool showDeleteNow = ProjectSettingsManager.GetBool(ShowDeleteKey, true);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                // Reserve layout space for the sticky header so items begin below it
                float stickyHeaderH = EditorGUIUtility.singleLineHeight + 4f;
                Rect headerSpace = GUILayoutUtility.GetRect(0, stickyHeaderH, GUILayout.ExpandWidth(true));

                GUIStyle compactRowStyle = new GUIStyle();
                compactRowStyle.margin = new RectOffset(2, 2, 1, 1);
                compactRowStyle.padding = new RectOffset(2, 2, 1, 1);

                for (int i = 0; i < items.Count; i++)
                {
                    // Dim the items that don't match the search
                    if (searchMode.target && highlightMode && highlightHide.Count > i && !highlightHide[i])
                    {
                        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }

                    // Change the color of the item being dragged
                    if (i == currentlyDraggingItemIndex)
                    {
                        GUI.color = Color.grey;
                        GUI.FocusControl("NotText");
                    }

                    EditorGUILayout.BeginHorizontal(compactRowStyle);

                    Rect menuIconRect = GUILayoutUtility.GetRect(25, EditorGUIUtility.singleLineHeight, GUILayout.Width(25));
                    GUI.Label(menuIconRect, EditorGUIUtility.IconContent("_Menu"));

                    // Name
                    EditorGUI.BeginChangeCheck();
                    items[i].name = EditorGUILayout.TextField(items[i].name);
                    if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrName = GUILayoutUtility.GetLastRect();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                    }
                    GUILayout.Space(10);

                    // Value Type
                    EditorGUI.BeginChangeCheck();
                    int valueTypeSelectedIndex = ValueToIndex(items[i].valueType);
                    valueTypeSelectedIndex = EditorGUILayout.Popup("", valueTypeSelectedIndex, valueTypeOptions, GUILayout.Width(typeWidth));
                    if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrType = GUILayoutUtility.GetLastRect();
                    items[i].valueType = IndexToValue(valueTypeSelectedIndex);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                    }
                    GUILayout.Space(10);

                    // Default
                    EditorGUI.BeginChangeCheck();
                    switch (items[i].valueType)
                    {
                        case VRCExpressionParameters.ValueType.Int:
                            items[i].defaultValue = EditorGUILayout.IntField(Convert.ToInt32(items[i].defaultValue), GUILayout.Width(defaultWidth));
                            break;
                        case VRCExpressionParameters.ValueType.Float:
                            items[i].defaultValue = EditorGUILayout.FloatField(items[i].defaultValue, GUILayout.Width(defaultWidth));
                            break;
                        case VRCExpressionParameters.ValueType.Bool:
                            items[i].defaultValue = Convert.ToSingle(EditorGUILayout.Toggle(Convert.ToBoolean(items[i].defaultValue), GUILayout.Width(defaultWidth)));
                            break;
                        default:
                            break;
                    }
                    if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrDefault = GUILayoutUtility.GetLastRect();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                    }
                    GUILayout.Space(10);

                    // Saved
                    EditorGUI.BeginChangeCheck();
                    items[i].saved = EditorGUILayout.Toggle(items[i].saved, GUILayout.Width(savedWidth));
                    if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrSaved = GUILayoutUtility.GetLastRect();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                    }

                    // Synced
                    EditorGUI.BeginChangeCheck();
                    items[i].networkSynced = EditorGUILayout.Toggle(items[i].networkSynced, GUILayout.Width(syncedWidth));
                    if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrSynced = GUILayoutUtility.GetLastRect();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                    }

                    // Copy
                    if (showCloneNow)
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Duplicate"), GUILayout.Width(cloneWidth)))
                        {
                            Undo.RecordObject(avatarDescriptor.expressionParameters, "Clone Parameter");
                            items.Add(new VRCExpressionParameters.Parameter
                            {
                                name = items[i].name,
                                defaultValue = items[i].defaultValue,
                                networkSynced = items[i].networkSynced,
                                saved = items[i].saved,
                                valueType = items[i].valueType
                            });
                            EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                            scrollPos += new Vector2(0, 999999);
                        }
                        if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrClone = GUILayoutUtility.GetLastRect();
                    }

                    // Delete
                    if (showDeleteNow)
                    {
                        if (GUILayout.Button("X", GUILayout.Width(deleteWidth)))
                        {
                            Undo.RecordObject(avatarDescriptor.expressionParameters, "Delete Parameter");
                            items.RemoveAt(i);
                            EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                        }
                        if (i == 0 && Event.current.type == EventType.Repaint) _exprHdrDelete = GUILayoutUtility.GetLastRect();
                    }

                    EditorGUILayout.EndHorizontal();

                    // Reset color
                    GUI.color = defaultColor;

                    Rect dropArea = GUILayoutUtility.GetLastRect();

                    // Subtle hover highlight (drawn as a transparent overlay over the whole row)
                    if (Event.current.type == EventType.Repaint && dropArea.Contains(e.mousePosition) && currentlyDraggingItemIndex == -1)
                    {
                        EditorGUI.DrawRect(dropArea, new Color(0.5f, 0.5f, 0.5f, 0.07f));
                    }

                    // Restrict drag start to the _Menu icon region only
                    Rect dragHandle = new Rect(dropArea.x, dropArea.y, 25, dropArea.height);
                    if (e.type == EventType.MouseDown && dragHandle.Contains(e.mousePosition))
                    {
                        currentlyDraggingItemIndex = i;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        Undo.RegisterCompleteObjectUndo(avatarDescriptor.expressionParameters, "Reorder Parameters");
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
                        Repaint();
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
                        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
                        Repaint();
                    }

                }

                // Keep hover highlight updating smoothly
                if (e.type == EventType.MouseMove) Repaint();

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

                // Draw sticky header overlay using rects captured from the first rendered row.
                // This guarantees pixel-perfect alignment with item columns at any window width.
                if (Event.current.type == EventType.Repaint && _exprHdrName.width > 1)
                {
                    Rect sr = new Rect(headerSpace.x, scrollPos.y, headerSpace.width, stickyHeaderH);
                    Color bg = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f, 1f)
                        : new Color(0.76f, 0.76f, 0.76f, 1f);
                    EditorGUI.DrawRect(sr, bg);
                    EditorGUI.DrawRect(new Rect(sr.x, sr.yMax - 1, sr.width, 1f), new Color(0f, 0f, 0f, 0.25f));

                    float lh = EditorGUIUtility.singleLineHeight;
                    float ly = sr.y + (stickyHeaderH - lh) * 0.5f;

                    GUI.Label(new Rect(_exprHdrName.x,    ly, _exprHdrName.width,    lh), "Name",    tableHeaderStyle);
                    GUI.Label(new Rect(_exprHdrType.x,    ly, _exprHdrType.width,    lh), "Type",    tableHeaderStyle);
                    GUI.Label(new Rect(_exprHdrDefault.x, ly, _exprHdrDefault.width, lh), "Default", tableHeaderStyle);
                    GUI.Label(new Rect(_exprHdrSaved.x,   ly, _exprHdrSaved.width,   lh), "Saved",   tableHeaderStyle);
                    GUI.Label(new Rect(_exprHdrSynced.x,  ly, _exprHdrSynced.width,  lh), "Synced",  tableHeaderStyle);
                    if (showCloneNow  && _exprHdrClone.width  > 1) GUI.Label(new Rect(_exprHdrClone.x,  ly, _exprHdrClone.width,  lh), "Clone",  tableHeaderStyle);
                    if (showDeleteNow && _exprHdrDelete.width > 1) GUI.Label(new Rect(_exprHdrDelete.x, ly, _exprHdrDelete.width, lh), "Delete", tableHeaderStyle);
                }

                EditorGUILayout.EndScrollView();

                if (avatarDescriptor.expressionParameters.CalcTotalCost() > VRCExpressionParameters.MAX_PARAMETER_COST)
                {
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                }

                EditorGUILayout.HelpBox("Total Memory: " + avatarDescriptor.expressionParameters.CalcTotalCost() + "/" + VRCExpressionParameters.MAX_PARAMETER_COST, MessageType.Info);
                GUI.color = defaultColor;


                ExtraBar(items, null);
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


                // Header (pinned/sticky) + item list
                bool showCloneNow = ProjectSettingsManager.GetBool(ShowCloneKey, true);
                bool showDeleteNow = ProjectSettingsManager.GetBool(ShowDeleteKey, true);
                bool modifyTransitionsNow = ProjectSettingsManager.GetBool(ModifyTransitionsKey, true);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                // Reserve layout space for the sticky header so items begin below it
                float stickyHeaderH = EditorGUIUtility.singleLineHeight + 4f;
                Rect headerSpace = GUILayoutUtility.GetRect(0, stickyHeaderH, GUILayout.ExpandWidth(true));

                GUIStyle compactRowStyle = new GUIStyle();
                compactRowStyle.margin = new RectOffset(2, 2, 1, 1);
                compactRowStyle.padding = new RectOffset(2, 2, 1, 1);

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

                    EditorGUILayout.BeginHorizontal(compactRowStyle);

                    {
                        Rect menuIconRect = GUILayoutUtility.GetRect(25, EditorGUIUtility.singleLineHeight, GUILayout.Width(25));
                        GUI.Label(menuIconRect, EditorGUIUtility.IconContent("_Menu"));

                        AnimatorController animatorController = (AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController;

                        // Name
                        EditorGUI.BeginChangeCheck();
                        itemsController[i].name = EditorGUILayout.TextField(itemsController[i].name);
                        if (i == 0 && Event.current.type == EventType.Repaint) _ctrlHdrName = GUILayoutUtility.GetLastRect();
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(animatorController);
                        }
                        GUILayout.Space(10);

                        // Value Type
                        AnimatorControllerParameterType oldType = itemsController[i].type;
                        EditorGUI.BeginChangeCheck();
                        int valueTypeSelectedIndex = TypeToIndex(itemsController[i].type);
                        valueTypeSelectedIndex = EditorGUILayout.Popup("", valueTypeSelectedIndex, valueTypeOptions, GUILayout.Width(typeWidth));
                        if (i == 0 && Event.current.type == EventType.Repaint) _ctrlHdrType = GUILayoutUtility.GetLastRect();
                        itemsController[i].type = IndexToType(valueTypeSelectedIndex);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (modifyTransitionsNow)
                            {
                                ModifyTransitionsForTypeChange(animatorController, itemsController[i].name, oldType, itemsController[i].type);
                            }
                            EditorUtility.SetDirty(animatorController);
                        }
                        GUILayout.Space(10);

                        // Default
                        EditorGUI.BeginChangeCheck();
                        switch (itemsController[i].type)
                        {
                            case AnimatorControllerParameterType.Float:
                                itemsController[i].defaultFloat = EditorGUILayout.FloatField(itemsController[i].defaultFloat, GUILayout.Width(defaultWidth));
                                break;
                            case AnimatorControllerParameterType.Int:
                                itemsController[i].defaultInt = EditorGUILayout.IntField(itemsController[i].defaultInt, GUILayout.Width(defaultWidth));
                                break;
                            case AnimatorControllerParameterType.Bool:
                                itemsController[i].defaultBool = EditorGUILayout.Toggle(itemsController[i].defaultBool, GUILayout.Width(defaultWidth));
                                break;
                            case AnimatorControllerParameterType.Trigger:
                                // Placeholder so the default column rect is always capturable
                                GUILayout.Label(GUIContent.none, GUILayout.Width(defaultWidth));
                                break;
                            default:
                                break;
                        }
                        if (i == 0 && Event.current.type == EventType.Repaint) _ctrlHdrDefault = GUILayoutUtility.GetLastRect();
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(animatorController);
                        }
                        GUILayout.Space(10);

                        // Copy
                        if (showCloneNow)
                        {
                            if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Duplicate"), GUILayout.Width(cloneWidth)))
                            {
                                Undo.RecordObject(animatorController, "Clone Parameter");
                                itemsController.Add(new AnimatorControllerParameter
                                {
                                    name = itemsController[i].name,
                                    defaultBool = itemsController[i].defaultBool,
                                    defaultFloat = itemsController[i].defaultFloat,
                                    defaultInt = itemsController[i].defaultInt,
                                    type = itemsController[i].type
                                });
                                EditorUtility.SetDirty(animatorController);
                                scrollPos += new Vector2(0, 999999);
                            }
                            if (i == 0 && Event.current.type == EventType.Repaint) _ctrlHdrClone = GUILayoutUtility.GetLastRect();
                            GUILayout.Space(5);
                        }

                        // Delete
                        if (showDeleteNow)
                        {
                            if (GUILayout.Button("X", GUILayout.Width(deleteWidth)))
                            {
                                pendingDeleteIndex = i;
                            }
                            if (i == 0 && Event.current.type == EventType.Repaint) _ctrlHdrDelete = GUILayoutUtility.GetLastRect();
                        }

                        EditorGUILayout.EndHorizontal();

                    }

                    GUI.color = defaultColor; // Reset color

                    AnimatorController animCtrl = (AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController;

                    Rect dropArea = GUILayoutUtility.GetLastRect();

                    // Subtle hover highlight
                    if (Event.current.type == EventType.Repaint && dropArea.Contains(e.mousePosition) && currentlyDraggingItemIndex == -1)
                    {
                        EditorGUI.DrawRect(dropArea, new Color(0.5f, 0.5f, 0.5f, 0.07f));
                    }

                    // Restrict drag start to the _Menu icon region only
                    Rect dragHandle = new Rect(dropArea.x, dropArea.y, 25, dropArea.height);
                    if (e.type == EventType.MouseDown && dragHandle.Contains(e.mousePosition))
                    {
                        currentlyDraggingItemIndex = i;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        Undo.RegisterCompleteObjectUndo(animCtrl, "Reorder Parameters");
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
                        Repaint();
                    }


                    if (currentlyDraggingItemIndex > -1)
                    {
                        Rect ghostRect = new Rect(dropArea.x, e.mousePosition.y - (dropArea.height / 2), dropArea.width, dropArea.height);
                        EditorGUI.DrawRect(ghostRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                    }
                    if (currentlyDraggingItemIndex > -1 && dropArea.Contains(e.mousePosition))
                    {
                        AnimatorControllerParameter temp = itemsController[currentlyDraggingItemIndex];
                        itemsController.RemoveAt(currentlyDraggingItemIndex);
                        itemsController.Insert(i, temp);
                        currentlyDraggingItemIndex = i;
                        EditorUtility.SetDirty(animCtrl);
                        Repaint();
                    }
                }

                // Keep hover highlight updating smoothly
                if (e.type == EventType.MouseMove) Repaint();

                // If the mouse button is released outside of any item, reset currentlyDraggingItemIndex
                if ((e.type == EventType.Ignore || e.type == EventType.MouseUp) && currentlyDraggingItemIndex > -1)
                {
                    currentlyDraggingItemIndex = -1;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    Repaint();
                }

                if (pendingDeleteIndex >= 0 && pendingDeleteIndex < itemsController.Count)
                {
                    AnimatorController animCtrl = (AnimatorController)avatarDescriptor.baseAnimationLayers[controllerIndex].animatorController;
                    var usages = FindParameterUsages(animCtrl, itemsController[pendingDeleteIndex].name);
                    bool shouldDelete = true;
                    if (usages.Count > 0)
                    {
                        string message = $"It's used by:\n{string.Join("\n", usages)}";
                        shouldDelete = EditorUtility.DisplayDialog($"Delete parameter {itemsController[pendingDeleteIndex].name}?", message, "Delete", "Cancel");
                    }
                    if (shouldDelete)
                    {
                        Undo.RecordObject(animCtrl, "Delete Parameter");
                        itemsController.RemoveAt(pendingDeleteIndex);
                        EditorUtility.SetDirty(animCtrl);
                    }
                    pendingDeleteIndex = -1;
                }

                // Draw sticky header overlay using rects captured from the first rendered row.
                if (Event.current.type == EventType.Repaint && _ctrlHdrName.width > 1)
                {
                    Rect sr = new Rect(headerSpace.x, scrollPos.y, headerSpace.width, stickyHeaderH);
                    Color bg = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f, 1f)
                        : new Color(0.76f, 0.76f, 0.76f, 1f);
                    EditorGUI.DrawRect(sr, bg);
                    EditorGUI.DrawRect(new Rect(sr.x, sr.yMax - 1, sr.width, 1f), new Color(0f, 0f, 0f, 0.25f));

                    float lh = EditorGUIUtility.singleLineHeight;
                    float ly = sr.y + (stickyHeaderH - lh) * 0.5f;

                    GUI.Label(new Rect(_ctrlHdrName.x,    ly, _ctrlHdrName.width,    lh), "Name",    tableHeaderStyle);
                    GUI.Label(new Rect(_ctrlHdrType.x,    ly, _ctrlHdrType.width,    lh), "Value",   tableHeaderStyle);
                    GUI.Label(new Rect(_ctrlHdrDefault.x, ly, _ctrlHdrDefault.width, lh), "Default", tableHeaderStyle);
                    if (showCloneNow  && _ctrlHdrClone.width  > 1) GUI.Label(new Rect(_ctrlHdrClone.x,  ly, _ctrlHdrClone.width,  lh), "Clone",  tableHeaderStyle);
                    if (showDeleteNow && _ctrlHdrDelete.width > 1) GUI.Label(new Rect(_ctrlHdrDelete.x, ly, _ctrlHdrDelete.width, lh), "Delete", tableHeaderStyle);
                }

                EditorGUILayout.EndScrollView();


                ExtraBar(null, itemsController);

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
                    EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
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
                animatorController.parameters = itemsController.ToArray();
                EditorUtility.SetDirty(animatorController);
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

        void ModifyTransitionsForTypeChange(AnimatorController controller, string parameterName, AnimatorControllerParameterType oldType, AnimatorControllerParameterType newType)
        {
            if (oldType == newType) return;

            foreach (var layer in controller.layers)
            {
                ModifyTransitionsInStateMachine(layer.stateMachine, parameterName, oldType, newType);

                foreach (var childStateMachine in layer.stateMachine.stateMachines)
                {
                    ModifyTransitionsInStateMachine(childStateMachine.stateMachine, parameterName, oldType, newType);
                }
            }
        }

        void ModifyTransitionsInStateMachine(AnimatorStateMachine stateMachine, string parameterName, AnimatorControllerParameterType oldType, AnimatorControllerParameterType newType)
        {
            foreach (var state in stateMachine.states)
            {
                foreach (var transition in state.state.transitions)
                {
                    ModifyTransitionConditions(transition, parameterName, oldType, newType);
                }
            }

            foreach (var transition in stateMachine.anyStateTransitions)
            {
                ModifyTransitionConditions(transition, parameterName, oldType, newType);
            }

            foreach (var entryTransition in stateMachine.entryTransitions)
            {
                ModifyTransitionConditions(entryTransition, parameterName, oldType, newType);
            }
        }

        void ModifyTransitionConditions(AnimatorStateTransition transition, string parameterName, AnimatorControllerParameterType oldType, AnimatorControllerParameterType newType)
        {
            var conditions = new List<AnimatorCondition>(transition.conditions);
            bool modified = false;

            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].parameter != parameterName) continue;

                var cond = conditions[i];
                AnimatorCondition newCond = ConvertCondition(cond, oldType, newType);
                conditions[i] = newCond;
                modified = true;
            }

            if (modified)
            {
                transition.conditions = conditions.ToArray();
            }
        }

        void ModifyTransitionConditions(AnimatorTransition transition, string parameterName, AnimatorControllerParameterType oldType, AnimatorControllerParameterType newType)
        {
            var conditions = new List<AnimatorCondition>(transition.conditions);
            bool modified = false;

            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].parameter != parameterName) continue;

                var cond = conditions[i];
                AnimatorCondition newCond = ConvertCondition(cond, oldType, newType);
                conditions[i] = newCond;
                modified = true;
            }

            if (modified)
            {
                transition.conditions = conditions.ToArray();
            }
        }

        AnimatorCondition ConvertCondition(AnimatorCondition cond, AnimatorControllerParameterType oldType, AnimatorControllerParameterType newType)
        {
            AnimatorCondition result = new AnimatorCondition
            {
                parameter = cond.parameter
            };

            if (oldType == AnimatorControllerParameterType.Bool || oldType == AnimatorControllerParameterType.Trigger)
            {
                if (newType == AnimatorControllerParameterType.Float)
                {
                    if (cond.mode == AnimatorConditionMode.If)
                    {
                        result.mode = AnimatorConditionMode.Greater;
                        result.threshold = 0.5f;
                    }
                    else
                    {
                        result.mode = AnimatorConditionMode.Less;
                        result.threshold = 0.5f;
                    }
                }
                else if (newType == AnimatorControllerParameterType.Int)
                {
                    if (cond.mode == AnimatorConditionMode.If)
                    {
                        result.mode = AnimatorConditionMode.Greater;
                        result.threshold = 0f;
                    }
                    else
                    {
                        result.mode = AnimatorConditionMode.Equals;
                        result.threshold = 0f;
                    }
                }
            }
            else if (oldType == AnimatorControllerParameterType.Float)
            {
                if (newType == AnimatorControllerParameterType.Bool || newType == AnimatorControllerParameterType.Trigger)
                {
                    if (cond.mode == AnimatorConditionMode.Greater && cond.threshold > 0f)
                    {
                        result.mode = AnimatorConditionMode.If;
                        result.threshold = 0f;
                    }
                    else if (cond.mode == AnimatorConditionMode.Less || cond.mode == AnimatorConditionMode.Equals)
                    {
                        result.mode = AnimatorConditionMode.IfNot;
                        result.threshold = 0f;
                    }
                    else
                    {
                        result.mode = AnimatorConditionMode.If;
                        result.threshold = 0f;
                    }
                }
                else if (newType == AnimatorControllerParameterType.Int)
                {
                    result.mode = cond.mode;
                    result.threshold = Mathf.Round(cond.threshold);
                }
            }
            else if (oldType == AnimatorControllerParameterType.Int)
            {
                if (newType == AnimatorControllerParameterType.Bool || newType == AnimatorControllerParameterType.Trigger)
                {
                    if (cond.mode == AnimatorConditionMode.Equals)
                    {
                        if (cond.threshold == 0f)
                        {
                            result.mode = AnimatorConditionMode.IfNot;
                        }
                        else
                        {
                            result.mode = AnimatorConditionMode.If;
                        }
                        result.threshold = 0f;
                    }
                    else if (cond.mode == AnimatorConditionMode.NotEqual)
                    {
                        if (cond.threshold == 0f)
                        {
                            result.mode = AnimatorConditionMode.If;
                        }
                        else
                        {
                            result.mode = AnimatorConditionMode.IfNot;
                        }
                        result.threshold = 0f;
                    }
                    else if (cond.mode == AnimatorConditionMode.Greater && cond.threshold >= 0f)
                    {
                        result.mode = AnimatorConditionMode.If;
                        result.threshold = 0f;
                    }
                    else if (cond.mode == AnimatorConditionMode.Less && cond.threshold <= 1f)
                    {
                        result.mode = AnimatorConditionMode.IfNot;
                        result.threshold = 0f;
                    }
                    else
                    {
                        result.mode = AnimatorConditionMode.If;
                        result.threshold = 0f;
                    }
                }
                else if (newType == AnimatorControllerParameterType.Float)
                {
                    result.mode = cond.mode;
                    result.threshold = cond.threshold;
                }
            }

            return result;
        }

        List<string> FindParameterUsages(AnimatorController controller, string parameterName)
        {
            List<string> usages = new List<string>();

            foreach (var layer in controller.layers)
            {
                FindParameterUsagesInStateMachine(layer.stateMachine, parameterName, usages);

                foreach (var childStateMachine in layer.stateMachine.stateMachines)
                {
                    FindParameterUsagesInStateMachine(childStateMachine.stateMachine, parameterName, usages);
                }
            }

            return usages;
        }

        void FindParameterUsagesInStateMachine(AnimatorStateMachine stateMachine, string parameterName, List<string> usages)
        {
            foreach (var state in stateMachine.states)
            {
                foreach (var transition in state.state.transitions)
                {
                    if (TransitionUsesParameter(transition.conditions, parameterName))
                    {
                        usages.Add($"Transition to {transition.destinationState?.name ?? "Unknown"}");
                    }
                }

                if (state.state.motion is BlendTree blendTree)
                {
                    if (BlendTreeUsesParameter(blendTree, parameterName))
                    {
                        usages.Add($"BlendTree in {state.state.name}");
                    }
                }
            }

            foreach (var transition in stateMachine.anyStateTransitions)
            {
                if (TransitionUsesParameter(transition.conditions, parameterName))
                {
                    usages.Add($"AnyState to {transition.destinationState?.name ?? "Unknown"}");
                }
            }

            foreach (var entryTransition in stateMachine.entryTransitions)
            {
                if (TransitionUsesParameter(entryTransition.conditions, parameterName))
                {
                    usages.Add($"Entry to {entryTransition.destinationState?.name ?? "Unknown"}");
                }
            }
        }

        bool BlendTreeUsesParameter(BlendTree blendTree, string parameterName)
        {
            if (blendTree.blendParameter == parameterName || blendTree.blendParameterY == parameterName)
            {
                return true;
            }

            foreach (var childMotion in blendTree.children)
            {
                if (childMotion.directBlendParameter == parameterName)
                {
                    return true;
                }

                if (childMotion.motion is BlendTree childTree && BlendTreeUsesParameter(childTree, parameterName))
                {
                    return true;
                }
            }

            return false;
        }

        bool TransitionUsesParameter(AnimatorCondition[] conditions, string parameterName)
        {
            foreach (var cond in conditions)
            {
                if (cond.parameter == parameterName)
                {
                    return true;
                }
            }
            return false;
        }


    }
}
#endif
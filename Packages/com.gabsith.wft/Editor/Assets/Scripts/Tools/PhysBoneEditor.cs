#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace GabSith.WFT
{
    public class PhysBoneEditor : EditorWindow
    {
        private GameObject selectedAvatar;
        private VRCAvatarDescriptor[] avatarDescriptorsFromScene;
        private Vector2 scrollPosDescriptors;
        private Vector2 scrollPosList;
        private Vector2 scrollPosInspector;

        private List<VRCPhysBone> allPhysBones = new List<VRCPhysBone>();
        private HashSet<VRCPhysBone> selectedPhysBones = new HashSet<VRCPhysBone>();
        private int lastClickedIndex = -1;

        private AnimBool physBonesListFold = new AnimBool(true);
        private AnimBool testAnimationsFold = new AnimBool(false);
        private bool inspectorFold = true;

        private bool shakeX = false;
        private bool shakeY = false;
        private bool shakeZ = false;
        private float moveSpeed = 10f;
        private float moveAmount = 0.15f;

        private bool rotX = false;
        private bool rotY = false;
        private bool rotZ = false;
        private float rotSpeed = 5f;
        private float rotAmount = 45f;

        private Vector3 originalAvatarPosition;
        private Quaternion originalAvatarRotation;
        private bool hasStoredOriginals = false;

        private float testAnimationTime = 0f;

        private string searchFilter = "";

        private const string PersistentKey = "PhysBoneEditorPersistentKey";

        private Dictionary<string, string> persistentData = new Dictionary<string, string>();

        private Editor nestedEditor = null;


        // Deferred editor refresh flag to avoid GUI layout errors when refreshing during play mode changes
        private bool needsEditorRefresh = false;

        // Stores selected PhysBone keys across play mode transitions
        private List<string> pendingSelectionKeys = new List<string>();

        // Cached GUIStyles for list rendering (initialized once)
        private GUIStyle listLabelStyle;
        private GUIStyle listSubLabelStyle;
        private GUIContent physBoneIcon;
        private GUIContent pingIcon;

        [MenuItem("GabSith/PhysBone Editor", false, 504)]

        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(PhysBoneEditor), false, "PhysBone Editor");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_RelativeJoint2D Icon").image, text = "PhysBone Editor", tooltip = "Edit PhysBones" };
            w.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            CommonActions.RefreshDescriptors(ref selectedAvatar, ref avatarDescriptorsFromScene);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            physBonesListFold.valueChanged.AddListener(new UnityAction(Repaint));
            testAnimationsFold.valueChanged.AddListener(new UnityAction(Repaint));
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            StopTestAnimations();
            DestroyNestedEditor();
        }

        private void OnUndoRedoPerformed()
        {
            if (nestedEditor != null)
            {
                // Force the nested inspector's serialized object to re-read from the component
                nestedEditor.serializedObject.Update();
            }
            Repaint();
        }

        private void DestroyNestedEditor()
        {
            if (nestedEditor != null)
            {
                DestroyImmediate(nestedEditor);
                nestedEditor = null;
            }
        }

        private void CreateNestedEditor()
        {
            DestroyNestedEditor();

            var selectedList = new List<VRCPhysBone>(selectedPhysBones);
            if (selectedList.Count > 0)
            {
                nestedEditor = Editor.CreateEditor(selectedList.ToArray());
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                // Save the selected PhysBone keys so we can restore them after the transition
                pendingSelectionKeys.Clear();
                foreach (var pb in selectedPhysBones)
                {
                    string key = GetStableKey(pb);
                    if (key != null)
                        pendingSelectionKeys.Add(key);
                }

                // Destroy the nested editor before the domain reload / mode change
                // to prevent stale component references that cause "(Script)" errors
                DestroyNestedEditor();
                selectedPhysBones.Clear();

                if (state == PlayModeStateChange.ExitingPlayMode && ProjectSettingsManager.GetBool(PersistentKey, true))
                {
                    StorePersistentData();
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                StopTestAnimations();
                shakeX = shakeY = shakeZ = false;
                rotX = rotY = rotZ = false;
                ApplyPersistentData();
                // Defer the refresh to OnEditorUpdate to avoid GUI layout errors
                needsEditorRefresh = true;
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Defer the refresh to OnEditorUpdate to avoid GUI layout errors
                needsEditorRefresh = true;
            }
        }

        /// <summary>
        /// Builds a stable key for a PhysBone that survives play/edit mode transitions.
        /// Uses the hierarchy path + component index on the same GameObject.
        /// </summary>
        private string GetStableKey(VRCPhysBone pb)
        {
            if (pb == null || selectedAvatar == null) return null;
            string path = AnimationUtility.CalculateTransformPath(pb.transform, selectedAvatar.transform);
            var siblings = pb.gameObject.GetComponents<VRCPhysBone>();
            int index = System.Array.IndexOf(siblings, pb);
            return $"{path}|{index}";
        }

        private void StorePersistentData()
        {
            persistentData.Clear();
            foreach (var pb in allPhysBones)
            {
                if (pb == null) continue;
                string key = GetStableKey(pb);
                if (key == null) continue;
                SerializedObject so = new SerializedObject(pb);
                List<string> propValues = new List<string>();

                SerializedProperty prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.Generic)
                    {
                        propValues.Add($"{prop.propertyPath}|{(int)prop.propertyType}|{SerializeValue(prop)}");
                    }
                }
                persistentData[key] = string.Join(";;", propValues);
            }
        }

        private void ApplyPersistentData()
        {
            if (persistentData.Count == 0) return;

            RefreshPhysBoneList();
            int applied = 0;

            foreach (var pb in allPhysBones)
            {
                if (pb == null) continue;
                string key = GetStableKey(pb);
                if (key == null) continue;

                if (persistentData.TryGetValue(key, out string data))
                {
                    SerializedObject so = new SerializedObject(pb);
                    string[] props = data.Split(new[] { ";;" }, System.StringSplitOptions.None);

                    foreach (string propStr in props)
                    {
                        if (string.IsNullOrEmpty(propStr)) continue;
                        string[] parts = propStr.Split(new[] { '|' }, 3);
                        if (parts.Length < 3) continue;

                        string path = parts[0];
                        SerializedPropertyType type = (SerializedPropertyType)int.Parse(parts[1]);
                        string value = parts[2];

                        SerializedProperty prop = so.FindProperty(path);
                        if (prop != null)
                        {
                            DeserializeValue(prop, value);
                        }
                    }

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(pb);
                    applied++;
                }
            }

            persistentData.Clear();
            Debug.Log($"[PhysBone Editor] Applied persistent changes to {applied} PhysBone(s)");
        }

        private string SerializeValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.ArraySize: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
                case SerializedPropertyType.Float: return prop.floatValue.ToString();
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Color: return JsonUtility.ToJson(prop.colorValue);
                case SerializedPropertyType.Vector2: return JsonUtility.ToJson(prop.vector2Value);
                case SerializedPropertyType.Vector3: return JsonUtility.ToJson(prop.vector3Value);
                case SerializedPropertyType.Vector4: return JsonUtility.ToJson(prop.vector4Value);
                case SerializedPropertyType.Quaternion: return JsonUtility.ToJson(prop.quaternionValue);
                case SerializedPropertyType.Rect: return JsonUtility.ToJson(prop.rectValue);
                case SerializedPropertyType.Bounds: return JsonUtility.ToJson(prop.boundsValue);
                case SerializedPropertyType.Enum: return prop.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    if (prop.objectReferenceValue == null) return "null";
                    // Store hierarchy path + type info so we can restore the correct component
                    Transform refTransform = null;
                    string refType = null;
                    int refSiblingIndex = 0;
                    if (prop.objectReferenceValue is Transform t)
                    {
                        refTransform = t;
                        refType = "Transform";
                    }
                    else if (prop.objectReferenceValue is GameObject go)
                    {
                        refTransform = go.transform;
                        refType = "GameObject";
                    }
                    else if (prop.objectReferenceValue is Component comp)
                    {
                        refTransform = comp.transform;
                        refType = comp.GetType().AssemblyQualifiedName;
                        var siblings = comp.gameObject.GetComponents(comp.GetType());
                        refSiblingIndex = System.Array.IndexOf(siblings, comp);
                    }
                    if (refTransform != null && selectedAvatar != null && refTransform.IsChildOf(selectedAvatar.transform))
                    {
                        string refPath = AnimationUtility.CalculateTransformPath(refTransform, selectedAvatar.transform);
                        return $"ref:{refType}:{refSiblingIndex}:{refPath}";
                    }
                    // Can't serialize this reference (e.g. MonoScript, Material) - skip it
                    return "";
                case SerializedPropertyType.AnimationCurve:
                    AnimationCurve curve = prop.animationCurveValue;
                    if (curve == null) return "";
                    var keyStrings = new List<string>();
                    foreach (var key in curve.keys)
                    {
                        keyStrings.Add($"{key.time};{key.value};{key.inTangent};{key.outTangent};{key.inWeight};{key.outWeight};{(int)key.weightedMode}");
                    }
                    return string.Join("|", keyStrings.ToArray()) + $"#{(int)curve.preWrapMode}#{(int)curve.postWrapMode}";
                default: return "";
            }
        }

        private void DeserializeValue(SerializedProperty prop, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = int.Parse(value); break;
                case SerializedPropertyType.ArraySize: prop.intValue = int.Parse(value); break;
                case SerializedPropertyType.Boolean: prop.boolValue = bool.Parse(value); break;
                case SerializedPropertyType.Float: prop.floatValue = float.Parse(value); break;
                case SerializedPropertyType.String: prop.stringValue = value; break;
                case SerializedPropertyType.Color: prop.colorValue = JsonUtility.FromJson<Color>(value); break;
                case SerializedPropertyType.Vector2: prop.vector2Value = JsonUtility.FromJson<Vector2>(value); break;
                case SerializedPropertyType.Vector3: prop.vector3Value = JsonUtility.FromJson<Vector3>(value); break;
                case SerializedPropertyType.Vector4: prop.vector4Value = JsonUtility.FromJson<Vector4>(value); break;
                case SerializedPropertyType.Quaternion: prop.quaternionValue = JsonUtility.FromJson<Quaternion>(value); break;
                case SerializedPropertyType.Rect: prop.rectValue = JsonUtility.FromJson<Rect>(value); break;
                case SerializedPropertyType.Bounds: prop.boundsValue = JsonUtility.FromJson<Bounds>(value); break;
                case SerializedPropertyType.Enum: prop.enumValueIndex = int.Parse(value); break;
                case SerializedPropertyType.ObjectReference:
                    if (value == "null")
                    {
                        prop.objectReferenceValue = null;
                    }
                    else if (value.StartsWith("ref:") && selectedAvatar != null)
                    {
                        // Format: ref:TypeName:SiblingIndex:HierarchyPath
                        string[] refParts = value.Substring(4).Split(new[] { ':' }, 3);
                        if (refParts.Length >= 3)
                        {
                            string refType = refParts[0];
                            int refSiblingIndex = int.Parse(refParts[1]);
                            string refPath = refParts[2];
                            Transform found = string.IsNullOrEmpty(refPath)
                                ? selectedAvatar.transform
                                : selectedAvatar.transform.Find(refPath);
                            if (found != null)
                            {
                                if (refType == "Transform")
                                    prop.objectReferenceValue = found;
                                else if (refType == "GameObject")
                                    prop.objectReferenceValue = found.gameObject;
                                else
                                {
                                    System.Type compType = System.Type.GetType(refType);
                                    if (compType != null)
                                    {
                                        var comps = found.gameObject.GetComponents(compType);
                                        if (refSiblingIndex >= 0 && refSiblingIndex < comps.Length)
                                            prop.objectReferenceValue = comps[refSiblingIndex];
                                        else if (comps.Length > 0)
                                            prop.objectReferenceValue = comps[0];
                                    }
                                }
                            }
                        }
                    }
                    else if (value.StartsWith("path:") && selectedAvatar != null)
                    {
                        // Backward compatibility with old format
                        string path = value.Substring(5);
                        Transform found = string.IsNullOrEmpty(path)
                            ? selectedAvatar.transform
                            : selectedAvatar.transform.Find(path);
                        if (found != null)
                        {
                            prop.objectReferenceValue = found;
                        }
                    }
                    break;
                case SerializedPropertyType.AnimationCurve:
                    try
                    {
                        // Format: key1|key2|...|keyN#preWrapMode#postWrapMode
                        string[] sections = value.Split('#');
                        string keysSection = sections[0];
                        WrapMode preWrap = sections.Length > 1 ? (WrapMode)int.Parse(sections[1]) : WrapMode.ClampForever;
                        WrapMode postWrap = sections.Length > 2 ? (WrapMode)int.Parse(sections[2]) : WrapMode.ClampForever;

                        var keyframes = new List<Keyframe>();
                        if (!string.IsNullOrEmpty(keysSection))
                        {
                            string[] keyStrings = keysSection.Split('|');
                            foreach (string ks in keyStrings)
                            {
                                string[] parts = ks.Split(';');
                                if (parts.Length >= 4)
                                {
                                    var kf = new Keyframe(
                                        float.Parse(parts[0]),
                                        float.Parse(parts[1]),
                                        float.Parse(parts[2]),
                                        float.Parse(parts[3])
                                    );
                                    if (parts.Length >= 7)
                                    {
                                        kf.inWeight = float.Parse(parts[4]);
                                        kf.outWeight = float.Parse(parts[5]);
                                        kf.weightedMode = (WeightedMode)int.Parse(parts[6]);
                                    }
                                    keyframes.Add(kf);
                                }
                            }
                        }
                        var newCurve = new AnimationCurve(keyframes.ToArray());
                        newCurve.preWrapMode = preWrap;
                        newCurve.postWrapMode = postWrap;
                        prop.animationCurveValue = newCurve;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[PhysBone Editor] Failed to deserialize AnimationCurve for '{prop.propertyPath}': {e.Message}");
                    }
                    break;
            }
        }

        private void OnEditorUpdate()
        {
            // Handle deferred editor refresh from play mode transitions
            if (needsEditorRefresh)
            {
                needsEditorRefresh = false;
                RefreshPhysBoneList();

                // Restore selection from saved keys
                if (pendingSelectionKeys.Count > 0)
                {
                    selectedPhysBones.Clear();
                    foreach (var pb in allPhysBones)
                    {
                        if (pb == null) continue;
                        string key = GetStableKey(pb);
                        if (key != null && pendingSelectionKeys.Contains(key))
                        {
                            selectedPhysBones.Add(pb);
                        }
                    }
                    pendingSelectionKeys.Clear();
                }

                if (selectedPhysBones.Count > 0)
                    CreateNestedEditor();
                Repaint();
            }

            if (Application.isPlaying && (shakeX || shakeY || shakeZ || rotX || rotY || rotZ) && selectedAvatar != null)
            {
                testAnimationTime += Time.deltaTime;
                ApplyTestAnimations();
                Repaint();
            }
        }


        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CommonActions.GenerateTitle("PhysBone Editor");

            CommonActions.FindAvatarsAsObjects(ref selectedAvatar, ref scrollPosDescriptors, ref avatarDescriptorsFromScene);

            if (GUILayout.Button(EditorApplication.isPlaying ? "Stop Play Mode" : "Enter Play Mode", GUILayout.Height(25f)))
            {
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
            }
            ProjectSettingsManager.EditorGUIBool(PersistentKey, "Persistent Changes In Play Mode", true);

            EditorGUILayout.Space(5);

            if (selectedAvatar != null)
            {
                RefreshPhysBoneList();

                physBonesListFold.target = EditorGUILayout.Foldout(physBonesListFold.target, "PhysBone List", true);

                using (var group = new EditorGUILayout.FadeGroupScope(physBonesListFold.faded))
                {
                    if (group.visible)
                    {

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"PhysBones: {allPhysBones.Count} | Selected: {selectedPhysBones.Count}", EditorStyles.boldLabel, GUILayout.Width(170f));

                        // Search bar
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(18f));
                        searchFilter = EditorGUILayout.TextField(searchFilter);

                        if (GUILayout.Button("All", GUILayout.Width(40)))
                        {
                            SelectAll();
                        }
                        if (GUILayout.Button("None", GUILayout.Width(40)))
                        {
                            SelectNone();
                        }
                        EditorGUILayout.EndHorizontal();

                        scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList, EditorStyles.helpBox, GUILayout.Height(Mathf.Min(100f, allPhysBones.Count * 20 + 5)));

                        if (listLabelStyle == null)
                        {
                            listLabelStyle = new GUIStyle(EditorStyles.label);
                            listLabelStyle.fontStyle = FontStyle.Normal;
                            listLabelStyle.padding = new RectOffset(2, 2, 0, 0);
                            listLabelStyle.margin = new RectOffset(0, 0, 0, 0);
                        }
                        if (listSubLabelStyle == null)
                        {
                            listSubLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                            listSubLabelStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
                            listSubLabelStyle.padding = new RectOffset(2, 4, 0, 0);
                            listSubLabelStyle.alignment = TextAnchor.MiddleRight;
                        }
                        if (physBoneIcon == null)
                        {
                            Texture2D tex = Resources.Load<Texture2D>("WFTAssets/PhysBone");
                            physBoneIcon = tex != null ? new GUIContent(tex) : EditorGUIUtility.IconContent("d_SpringJoint Icon");
                        }
                        if (pingIcon == null)
                        {
                            pingIcon = EditorGUIUtility.IconContent("d_Search Icon");
                        }

                        Color selColor = ProjectSettingsManager.GetColor("ButtonsColorKey", CommonActions.selectionColor);
                        int visibleIndex = 0;

                        for (int i = 0; i < allPhysBones.Count; i++)
                        {
                            VRCPhysBone pb = allPhysBones[i];
                            if (pb == null) continue;

                            // Build display name, appending index if multiple PhysBones on the same GameObject
                            string displayName = pb.gameObject.name;
                            var siblingPBs = pb.gameObject.GetComponents<VRCPhysBone>();
                            if (siblingPBs.Length > 1)
                            {
                                int siblingIndex = System.Array.IndexOf(siblingPBs, pb);
                                displayName += $" [{siblingIndex}]";
                            }

                            if (!string.IsNullOrEmpty(searchFilter) && !displayName.ToLower().Contains(searchFilter.ToLower()))
                                continue;

                            bool isSelected = selectedPhysBones.Contains(pb);
                            float rowHeight = 20f;

                            Rect rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

                            // Alternating row background
                            if (Event.current.type == EventType.Repaint)
                            {
                                Color rowBg = (visibleIndex % 2 == 0)
                                    ? new Color(0f, 0f, 0f, 0.06f)
                                    : new Color(0f, 0f, 0f, 0.0f);

                                if (isSelected)
                                {
                                    // Selected row background
                                    rowBg = new Color(selColor.r * 0.4f, selColor.g * 0.4f, selColor.b * 0.4f, 0.35f);
                                }
                                else if (rowRect.Contains(Event.current.mousePosition))
                                {
                                    // Hover highlight
                                    rowBg = new Color(0.5f, 0.5f, 0.5f, 0.12f);
                                }

                                EditorGUI.DrawRect(rowRect, rowBg);

                                // Selection accent bar on the left
                                if (isSelected)
                                {
                                    Rect accentRect = new Rect(rowRect.x, rowRect.y, 3f, rowRect.height);
                                    EditorGUI.DrawRect(accentRect, new Color(selColor.r, selColor.g, selColor.b, 0.85f));
                                }
                            }

                            // Handle click on row (but exclude the ping button area)
                            Rect pingRect = new Rect(rowRect.xMax - 18f - 4f, rowRect.y + (rowRect.height - 16f) / 2f, 18f, 16f);
                            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition) && !pingRect.Contains(Event.current.mousePosition))
                            {
                                HandleItemClick(i, Event.current);
                                Event.current.Use();
                            }

                            // Draw row contents: icon + name + ping button + root transform info
                            float pingBtnSize = 16f;
                            float iconSize = 16f;
                            Rect iconRect = new Rect(rowRect.x + 6f, rowRect.y + (rowRect.height - iconSize) / 2f, iconSize, iconSize);
                            GUI.Label(iconRect, physBoneIcon);

                            // Ping button on the right edge (pingRect was already computed above for click exclusion)
                            if (GUI.Button(pingRect, pingIcon, EditorStyles.iconButton))
                            {
                                // Ping and select the PhysBone's GameObject in the hierarchy
                                if (pb != null)
                                {
                                    EditorGUIUtility.PingObject(pb.gameObject);
                                    Selection.activeObject = pb.gameObject;
                                }
                            }

                            // Root/sub label on the right side (before ping button)
                            string rootInfo = "";
                            if (pb.rootTransform != null && pb.rootTransform != pb.transform)
                                rootInfo = "→ " + pb.rootTransform.name;

                            float rightLabelWidth = string.IsNullOrEmpty(rootInfo) ? 0 : listSubLabelStyle.CalcSize(new GUIContent(rootInfo)).x + 4f;

                            Rect nameRect = new Rect(iconRect.xMax + 4f, rowRect.y, rowRect.width - iconRect.xMax - 4f - rightLabelWidth - pingBtnSize - 12f, rowRect.height);
                            listLabelStyle.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                            GUI.Label(nameRect, displayName, listLabelStyle);

                            if (!string.IsNullOrEmpty(rootInfo))
                            {
                                Rect rootRect = new Rect(pingRect.x - rightLabelWidth - 4f, rowRect.y, rightLabelWidth, rowRect.height);
                                GUI.Label(rootRect, rootInfo, listSubLabelStyle);
                            }

                            visibleIndex++;
                        }

                        // Request repaint for hover effects
                        if (Event.current.type == EventType.MouseMove)
                            Repaint();

                        EditorGUILayout.EndScrollView();

                        EditorGUILayout.LabelField(new GUIContent("Click: select  |  Shift+Click: fill range | Ctrl+Click: toggle individual"), EditorStyles.centeredGreyMiniLabel);
                        //EditorGUILayout.HelpBox("Multiple selection supported! Use Shift+Click for range selection and Ctrl+Click for toggling individual items.", MessageType.Info);
                    }
                }


                EditorGUILayout.Space(5);


                if (selectedPhysBones.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Copy is only valid with exactly one physbone selected
                    using (new EditorGUI.DisabledScope(selectedPhysBones.Count != 1))
                    {
                        if (GUILayout.Button("Copy", GUILayout.Height(22f)))
                        {
                            CopySelectedSettings();
                        }
                    }

                    // Paste buttons are only valid when clipboard has data
                    bool hasClipboard = hasClipboardData;

                    // Regular Paste is also disabled during play mode (root transform can't be changed)
                    using (new EditorGUI.DisabledScope(!hasClipboard))
                    {
                        if (GUILayout.Button("Paste", GUILayout.Height(22f)))
                        {
                            PasteSettings(false);
                        }
                    }
                    using (new EditorGUI.DisabledScope(!hasClipboard))
                    {
                        if (GUILayout.Button("Paste (Keep Root)", GUILayout.Height(22f)))
                        {
                            PasteSettings(true);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);

                if (selectedPhysBones.Count != 0)
                {
                    EditorGUILayout.EndVertical();
                    DrawNestedInspector();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                }


                EditorGUILayout.Space(5);

                DrawTestAnimations();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }

        private void HandleItemClick(int index, Event e)
        {
            VRCPhysBone pb = allPhysBones[index];

            if (e.shift && lastClickedIndex >= 0 && lastClickedIndex != index)
            {
                int start = Mathf.Min(lastClickedIndex, index);
                int end = Mathf.Max(lastClickedIndex, index);

                for (int i = start; i <= end; i++)
                {
                    if (allPhysBones[i] != null)
                        selectedPhysBones.Add(allPhysBones[i]);
                }
            }
            else if (e.control)
            {
                if (selectedPhysBones.Contains(pb))
                    selectedPhysBones.Remove(pb);
                else
                    selectedPhysBones.Add(pb);
            }
            else
            {
                selectedPhysBones.Clear();
                selectedPhysBones.Add(pb);
            }

            lastClickedIndex = index;
            CreateNestedEditor();
        }

        private void SelectAll()
        {
            selectedPhysBones.Clear();
            foreach (var pb in allPhysBones)
            {
                if (pb != null)
                    selectedPhysBones.Add(pb);
            }
            if (allPhysBones.Count > 0)
                lastClickedIndex = allPhysBones.Count - 1;
            CreateNestedEditor();
        }

        private void SelectNone()
        {
            selectedPhysBones.Clear();
            lastClickedIndex = -1;
            DestroyNestedEditor();
        }


        private void DrawNestedInspector()
        {
            if (selectedPhysBones.Count == 0) return;

            // Validate that all selected physbones and editor targets are still alive
            bool needsRefresh = false;

            if (selectedPhysBones.Any(pb => pb == null))
            {
                selectedPhysBones.RemoveWhere(pb => pb == null);
                needsRefresh = true;
            }

            if (nestedEditor != null && nestedEditor.targets.Any(t => t == null))
            {
                needsRefresh = true;
            }

            if (needsRefresh)
            {
                DestroyNestedEditor();
                if (selectedPhysBones.Count > 0)
                    CreateNestedEditor();
                if (selectedPhysBones.Count == 0) return;
            }

            EditorGUILayout.BeginVertical();

            inspectorFold = EditorGUILayout.InspectorTitlebar(inspectorFold, nestedEditor?.targets ?? new Object[0]);

            if (inspectorFold)
            {
                scrollPosInspector = EditorGUILayout.BeginScrollView(scrollPosInspector);

                if (nestedEditor != null)
                {
                    nestedEditor.OnInspectorGUI();
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a PhysBone to inspect.", MessageType.Info);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshPhysBoneList()
        {
            var previousCount = allPhysBones.Count;
            allPhysBones.Clear();
            if (selectedAvatar != null)
            {
                allPhysBones.AddRange(selectedAvatar.GetComponentsInChildren<VRCPhysBone>(true));
            }

            if (allPhysBones.Count != previousCount)
            {
                selectedPhysBones.RemoveWhere(pb => !allPhysBones.Contains(pb));
            }
        }

        private string clipboardSourcePath = null;
        private bool hasClipboardData = false;

        private void CopySelectedSettings()
        {
            if (selectedPhysBones.Count == 0) return;

            var source = new List<VRCPhysBone>(selectedPhysBones)[0];
            clipboardSourcePath = GetStableKey(source);
            hasClipboardData = UnityEditorInternal.ComponentUtility.CopyComponent(source);

            if (hasClipboardData)
                Debug.Log($"[PhysBone Editor] Copied settings from: {source.gameObject.name}");
        }

        private void PasteSettings(bool keepRootTransform)
        {
            if (!hasClipboardData)
            {
                Debug.LogWarning("[PhysBone Editor] No settings in clipboard.");
                return;
            }

            int count = 0;

            foreach (var pb in selectedPhysBones)
            {
                // Skip pasting onto the source itself
                if (clipboardSourcePath != null && GetStableKey(pb) == clipboardSourcePath) continue;

                // Save root transform before paste overwrites it
                Object savedRoot = null;
                if (keepRootTransform)
                {
                    SerializedObject soBefore = new SerializedObject(pb);
                    var rootProp = soBefore.FindProperty("rootTransform");
                    if (rootProp != null)
                        savedRoot = rootProp.objectReferenceValue;
                }

                UnityEditorInternal.ComponentUtility.PasteComponentValues(pb);

                // Restore root transform if requested
                if (keepRootTransform)
                {
                    SerializedObject soAfter = new SerializedObject(pb);
                    var rootProp = soAfter.FindProperty("rootTransform");
                    if (rootProp != null)
                    {
                        rootProp.objectReferenceValue = savedRoot;
                        soAfter.ApplyModifiedProperties();
                    }
                }

                EditorUtility.SetDirty(pb);
                count++;
            }

            // Refresh the nested inspector to reflect pasted values
            CreateNestedEditor();

            Debug.Log($"[PhysBone Editor] Pasted settings to {count} PhysBone(s){(keepRootTransform ? " (kept root transforms)" : "")}");
        }


        private void DrawAxisToggle(string axis, ref bool value, Color axisColor)
        {
            Color defBg = GUI.backgroundColor;
            if (value)
                GUI.backgroundColor = axisColor;
            else
                GUI.backgroundColor = new Color(axisColor.r * 0.4f, axisColor.g * 0.4f, axisColor.b * 0.4f, 0.5f);

            if (GUILayout.Button(axis, value ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Width(36), GUILayout.Height(22)))
            {
                value = !value;
                if (value) StoreOriginals();
                else ResetToOriginal();
            }
            GUI.backgroundColor = defBg;
        }

        private void DrawTestAnimations()
        {
            testAnimationsFold.target = EditorGUILayout.Foldout(testAnimationsFold.target, "Testing", true);

            using (var group = new EditorGUILayout.FadeGroupScope(testAnimationsFold.faded))
            {
                if (group.visible)
                {
                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Testing is only available in Play Mode.", MessageType.Info);
                    }
                    else
                    {
                        bool anyActive = shakeX || shakeY || shakeZ || rotX || rotY || rotZ;

                        // --- Movement Section ---
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("MoveTool"), GUILayout.Width(18));
                        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Axes", GUILayout.Width(38));
                        DrawAxisToggle("X", ref shakeX, new Color(0.9f, 0.35f, 0.35f));
                        DrawAxisToggle("Y", ref shakeY, new Color(0.45f, 0.8f, 0.35f));
                        DrawAxisToggle("Z", ref shakeZ, new Color(0.35f, 0.55f, 0.9f));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space(2);

                        moveSpeed = EditorGUILayout.Slider("Speed", moveSpeed, 0.1f, 20f);
                        moveAmount = EditorGUILayout.Slider("Amount", moveAmount, 0.01f, 1f);

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.Space(2);

                        // --- Rotation Section ---
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("RotateTool"), GUILayout.Width(18));
                        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Axes", GUILayout.Width(38));
                        DrawAxisToggle("X", ref rotX, new Color(0.9f, 0.35f, 0.35f));
                        DrawAxisToggle("Y", ref rotY, new Color(0.45f, 0.8f, 0.35f));
                        DrawAxisToggle("Z", ref rotZ, new Color(0.35f, 0.55f, 0.9f));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space(2);

                        rotSpeed = EditorGUILayout.Slider("Speed", rotSpeed, 0.1f, 20f);
                        rotAmount = EditorGUILayout.Slider("Amount", rotAmount, 0f, 180f);

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.Space(4);

                        // --- Reset Button ---
                        Color defBg = GUI.backgroundColor;
                        if (anyActive)
                            GUI.backgroundColor = new Color(1f, 0.75f, 0.35f);
                        if (GUILayout.Button(anyActive ? "⟲  Reset Transform" : "Reset Transform", GUILayout.Height(24f)))
                        {
                            shakeX = shakeY = shakeZ = false;
                            rotX = rotY = rotZ = false;
                            ResetToOriginal();
                        }
                        GUI.backgroundColor = defBg;
                    }
                }
            }

        }

        private void StoreOriginals()
        {
            if (!hasStoredOriginals && selectedAvatar != null)
            {
                originalAvatarPosition = selectedAvatar.transform.position;
                originalAvatarRotation = selectedAvatar.transform.rotation;
                hasStoredOriginals = true;
            }
        }

        private void ApplyTestAnimations()
        {
            if (selectedAvatar == null || !hasStoredOriginals) return;

            Vector3 position = originalAvatarPosition;
            Quaternion rotation = originalAvatarRotation;

            float moveSin = Mathf.Sin(testAnimationTime * moveSpeed) * moveAmount;
            float rotSin = Mathf.Sin(testAnimationTime * rotSpeed);

            if (shakeX)
                position.x += moveSin;
            if (shakeY)
                position.y += moveSin;
            if (shakeZ)
                position.z += moveSin;

            Vector3 eulerRot = Vector3.zero;
            if (rotX)
                eulerRot.x = rotSin * rotAmount;
            if (rotY)
                eulerRot.y = rotSin * rotAmount;
            if (rotZ)
                eulerRot.z = rotSin * rotAmount;

            rotation = originalAvatarRotation * Quaternion.Euler(eulerRot);

            selectedAvatar.transform.position = position;
            selectedAvatar.transform.rotation = rotation;
        }

        private void StopTestAnimations()
        {
            if (hasStoredOriginals && selectedAvatar != null)
            {
                ResetToOriginal();
            }
            testAnimationTime = 0f;
        }

        private void ResetToOriginal()
        {
            if (selectedAvatar != null && hasStoredOriginals)
            {
                selectedAvatar.transform.position = originalAvatarPosition;
                selectedAvatar.transform.rotation = originalAvatarRotation;
            }
            hasStoredOriginals = false;
            shakeX = shakeY = shakeZ = false;
            rotX = rotY = rotZ = false;
        }
    }
}

#endif

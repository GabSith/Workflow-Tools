#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

using UnityEditor.AnimatedValues;
using UnityEngine.Events;


namespace GabSith.WFT
{
    public class ColorTextureGenerator : EditorWindow
    {
        private enum GradientType { Horizontal, Vertical, Radial }

        private Color textureColor = Color.white;
        private int textureWidth = 16;
        private int textureHeight = 16;
        private string textureName = "ColorTexture";
        private Vector2 scrollPosition;

        private bool useGradient = false;
        private GradientType gradientType = GradientType.Horizontal;
        private Gradient gradient = new Gradient();
        private bool overrideExisting = false;

        private AnimBool showPreview = new AnimBool(false);

        private const string ColorTextureFolderKey = "ColorTextureFolderKey";
        private const string ColorTextureUseGlobalKey = "ColorTextureUseGlobalKey";
        private const string ColorTextureFolderSuffixKey = "ColorTextureFolderSuffixKey";
        private string suffix;

        private Texture2D previewTexture;

        [MenuItem("GabSith/Niche/Color Texture Generator", false, 1000)]

        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(ColorTextureGenerator), false, "Color Texture Generator");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("d_ColorPicker.CycleColor").image, text = "Color Texture Generator", tooltip = "♥" };
            w.minSize = new Vector2(250, 300);
        }

        private void OnEnable()
        {
            suffix = ProjectSettingsManager.GetString(ColorTextureFolderSuffixKey);
            InitializeDefaultGradient();
            CreatePreviewTexture();
            showPreview.valueChanged.AddListener(new UnityAction(Repaint));
        }

        private void InitializeDefaultGradient()
        {
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.black, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }

        private void OnDisable()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            CommonActions.GenerateTitle("Color Texture Generator");

            textureName = EditorGUILayout.TextField("Texture Name", textureName);
            overrideExisting = EditorGUILayout.Toggle("Override Existing", overrideExisting);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            textureWidth = EditorGUILayout.IntField("Width", textureWidth);
            textureHeight = EditorGUILayout.IntField("Height", textureHeight);
            EditorGUILayout.EndHorizontal();

            if (textureWidth < 1) textureWidth = 1;
            if (textureHeight < 1) textureHeight = 1;
            if (textureWidth > 4096) textureWidth = 4096;
            if (textureHeight > 4096) textureHeight = 4096;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("4x4")) { textureWidth = 4; textureHeight = 4; }
            if (GUILayout.Button("16x16")) { textureWidth = 16; textureHeight = 16; }
            if (GUILayout.Button("64x64")) { textureWidth = 64; textureHeight = 64; }
            if (GUILayout.Button("256x256")) { textureWidth = 256; textureHeight = 256; }
            if (GUILayout.Button("512x512")) { textureWidth = 512; textureHeight = 512; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            EditorGUI.BeginChangeCheck();

            if (CommonActions.ToggleButton("Use Gradient", useGradient, GUILayout.Height(25)))
            {
                useGradient = !useGradient;
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (useGradient)
            {
                EditorGUILayout.Space(5);
                gradientType = (GradientType)EditorGUILayout.EnumPopup("Gradient Type", gradientType);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Gradient Colors");
                gradient = EditorGUILayout.GradientField(gradient, GUILayout.Height(30));
            }
            else
            {
                GUILayout.Label("Texture Color",
                    new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter },
                    GUILayout.ExpandWidth(true));
                textureColor = EditorGUILayout.ColorField(new GUIContent(""), textureColor, true, true, false, GUILayout.Height(40));
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                CreatePreviewTexture();
            }

            if (useGradient)
            {
                EditorGUILayout.Space(10);
                showPreview.target = EditorGUILayout.Toggle("Show Preview", showPreview.target);

                using (var group = new EditorGUILayout.FadeGroupScope(showPreview.faded))
                {
                    if (group.visible)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("Preview",
                            new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter },
                            GUILayout.ExpandWidth(true));
                        EditorGUILayout.Space(5);
                        Rect previewRect = GUILayoutUtility.GetAspectRect(2f, GUILayout.Height(60));
                        EditorGUI.DrawTextureTransparent(previewRect, previewTexture, ScaleMode.StretchToFill);
                        EditorGUILayout.Space(5);
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space(10);

            CommonActions.SelectFolder(ColorTextureUseGlobalKey, ColorTextureFolderKey, ColorTextureFolderSuffixKey, ref suffix);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Texture", new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fixedHeight = 35,
            }))
            {
                CreateColorTexture();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void CreatePreviewTexture()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }
            previewTexture = new Texture2D(256, 128, TextureFormat.RGBA32, false);

            if (useGradient)
            {
                ApplyGradientToTexture(previewTexture);
            }
            else
            {
                Color[] colors = new Color[256 * 128];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = textureColor;
                }
                previewTexture.SetPixels(colors);
            }
            previewTexture.Apply();
        }

        private void ApplyGradientToTexture(Texture2D tex)
        {
            int width = tex.width;
            int height = tex.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = 0f;

                    switch (gradientType)
                    {
                        case GradientType.Horizontal:
                            t = width > 1 ? (float)x / (width - 1) : 0f;
                            break;
                        case GradientType.Vertical:
                            t = height > 1 ? (float)y / (height - 1) : 0f;
                            break;
                        case GradientType.Radial:
                            float centerX = width / 2f;
                            float centerY = height / 2f;
                            float maxDist = Mathf.Sqrt(centerX * centerX + centerY * centerY);
                            float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                            t = maxDist > 0 ? Mathf.Clamp01(dist / maxDist) : 0f;
                            break;
                    }

                    tex.SetPixel(x, y, gradient.Evaluate(t));
                }
            }
        }

        private void CreateColorTexture()
        {
            if (string.IsNullOrEmpty(GetFolder()))
            {
                EditorUtility.DisplayDialog("Error", "Please select a save folder first.", "OK");
                return;
            }

            string baseName = string.IsNullOrEmpty(textureName) ? "ColorTexture" : textureName;
            string filename = overrideExisting ? baseName + ".png" : GetUniqueFilename(baseName);

            Texture2D newTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

            if (useGradient)
            {
                ApplyGradientToTexture(newTexture);
            }
            else
            {
                Color[] colors = new Color[textureWidth * textureHeight];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = textureColor;
                }
                newTexture.SetPixels(colors);
            }
            newTexture.Apply();

            byte[] bytes = newTexture.EncodeToPNG();
            Directory.CreateDirectory(GetFolder());
            File.WriteAllBytes(Path.Combine(GetFolder(), filename), bytes);

            DestroyImmediate(newTexture);

            AssetDatabase.Refresh();
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(Path.Combine(GetFolder(), filename));
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;

                if (useGradient)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                }

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(GetFolder(), filename));
            EditorGUIUtility.PingObject(savedTexture);
        }

        private string GetUniqueFilename(string baseName)
        {
            string folder = GetFolder();
            string filename = baseName + ".png";
            string fullPath = Path.Combine(folder, filename);

            if (!File.Exists(fullPath))
            {
                return filename;
            }

            int counter = 1;
            while (true)
            {
                filename = $"{baseName} {counter}.png";
                fullPath = Path.Combine(folder, filename);
                if (!File.Exists(fullPath))
                {
                    return filename;
                }
                counter++;
            }
        }

        private string GetFolder()
        {
            return CommonActions.GetFolder(ColorTextureUseGlobalKey, ColorTextureFolderKey, ColorTextureFolderSuffixKey);
        }
    }
}
#endif

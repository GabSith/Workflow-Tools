#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;


namespace GabSith.WFT
{
    public class ImageCreator : EditorWindow
    {
        private string screenshotName;
        private int resolutionWidth = 256;
        private int resolutionHeight = 256;
        private float fieldOfView = 30f;
        private Color backgroundColor = new Color(0.69f, 0.34f, 0.34f);
        private bool useSkybox = false;
        private bool useTransparentBackground = true;
        private bool showPreview = false;
        private bool useSceneView = true;
        private Camera selectedCamera;
        private RenderTexture previewTexture;
        private bool captureSelectedOnly = false;
        private Vector2 scrollPosition;

        private bool saveAsIcon = true;
        private bool isVisible = true;

        private const string ScreenshotFolderKey = "ScreenshotFolderKey";
        private const string ScreenshotUseGlobalKey = "ScreenshotUseGlobalKey";
        private const string ScreenshotFolderSuffixKey = "ScreenshotFolderSuffixKey";
        string suffix;


        private Texture2D borderTexture;
        private Color borderColor = Color.black;
        private int borderWidth = 2;

        [MenuItem("GabSith/Image Creator", false, 102)]
        public static void ShowWindow()
        {
            EditorWindow w = EditorWindow.GetWindow(typeof(ImageCreator), false, "Image Creator");
            w.titleContent = new GUIContent { image = EditorGUIUtility.IconContent("FrameCapture On").image, text = "Image Creator", tooltip = "♥" };

        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            suffix = ProjectSettingsManager.GetString(ScreenshotFolderSuffixKey);

        }
        private void OnBecameInvisible()
        {
            isVisible = false;
        }
        private void OnBecameVisible()
        {
            isVisible = true;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }
            if (borderTexture != null)
            {
                DestroyImmediate(borderTexture);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            CommonActions.GenerateTitle("Image Creator");

            screenshotName = EditorGUILayout.TextField("Name", screenshotName);
            GUILayout.Label("Screenshot Settings", EditorStyles.boldLabel);


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Icon"))
            {
                resolutionWidth = 256;
                resolutionHeight = 256;
                saveAsIcon = true;
                useTransparentBackground = true;
            }
            if (GUILayout.Button("Thumbnail"))
            {
                resolutionWidth = 1200;
                resolutionHeight = 900;
                saveAsIcon = false;
                useTransparentBackground = false;
            }
            if (GUILayout.Button("1080"))
            {
                resolutionWidth = 1920;
                resolutionHeight = 1080;
                saveAsIcon = false;
                useTransparentBackground = false;
            }
            if (GUILayout.Button("4K"))
            {
                resolutionWidth = 3840;
                resolutionHeight = 2160;
                saveAsIcon = false;
                useTransparentBackground = false;
            }
            EditorGUILayout.EndHorizontal();



            EditorGUI.BeginChangeCheck();
            resolutionWidth = EditorGUILayout.IntField("Width", resolutionWidth);
            resolutionHeight = EditorGUILayout.IntField("Height", resolutionHeight);
            if (EditorGUI.EndChangeCheck())
            {
                if (resolutionWidth < 1)
                {
                    resolutionWidth = 1;
                }
                if (resolutionHeight < 1)
                {
                    resolutionHeight = 1;
                }
                UpdatePreviewTexture();
            }

            fieldOfView = EditorGUILayout.Slider("Field of View", fieldOfView, 1f, 179f);

            saveAsIcon = EditorGUILayout.Toggle("Save as icon", saveAsIcon);


            GUILayout.Space(10);
            GUILayout.Label("View Settings", EditorStyles.boldLabel);
            useSceneView = EditorGUILayout.Toggle("Use Scene View", useSceneView);
            if (!useSceneView)
            {
                selectedCamera = (Camera)EditorGUILayout.ObjectField("Selected Camera", selectedCamera, typeof(Camera), true);
            }

            captureSelectedOnly = EditorGUILayout.Toggle("Capture Selected Only", captureSelectedOnly);

            GUILayout.Space(10);
            GUILayout.Label("Background Settings", EditorStyles.boldLabel);
            useTransparentBackground = EditorGUILayout.Toggle("Transparent Background", useTransparentBackground);

            if (!useTransparentBackground)
            {
                useSkybox = EditorGUILayout.Toggle("Use Skybox", useSkybox);
            }

            if (!useSkybox && !useTransparentBackground)
            {
                backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
            }

            GUILayout.Space(10);

            CommonActions.SelectFolder(ScreenshotUseGlobalKey, ScreenshotFolderKey, ScreenshotFolderSuffixKey, ref suffix);

            GUILayout.Space(10);
            showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);

            /*
            EditorGUI.BeginChangeCheck();
            borderColor = EditorGUILayout.ColorField("Border Color", borderColor);
            borderWidth = EditorGUILayout.IntSlider("Border Width", borderWidth, 1, 10);
            if (EditorGUI.EndChangeCheck())
            {
                CreateBorderTexture();
            }
            */
            if (showPreview && isVisible)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                UpdatePreview();
                Rect previewRect = GUILayoutUtility.GetAspectRect(16f / 9f);

                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);

                if (useTransparentBackground)
                {
                    GUI.DrawTexture(previewRect, borderTexture, ScaleMode.ScaleToFit);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.HelpBox("The preview uses resources when active. Remember to disable it when you're done using it.", MessageType.Info);
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Take Screenshot", new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fixedHeight = 35,
            }))
            {
                TakeScreenshot();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void CreateBorderTexture()
        {
            if (borderTexture != null)
            {
                DestroyImmediate(borderTexture);
            }
            borderTexture = new Texture2D(resolutionWidth, resolutionHeight);
            Color[] colors = new Color[resolutionWidth * resolutionHeight];
            for (int y = 0; y < resolutionHeight; y++)
            {
                for (int x = 0; x < resolutionWidth; x++)
                {
                    if (x < borderWidth || x >= resolutionWidth - borderWidth ||
                        y < borderWidth || y >= resolutionHeight - borderWidth)
                    {
                        colors[y * resolutionWidth + x] = borderColor;
                    }
                    else
                    {
                        colors[y * resolutionWidth + x] = Color.clear;
                    }
                }
            }
            borderTexture.SetPixels(colors);
            borderTexture.Apply();
        }


        private void UpdatePreviewTexture()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }
            previewTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
            previewTexture.antiAliasing = 8;
            CreateBorderTexture();
        }

        private void UpdatePreview()
        {
            if (previewTexture == null || previewTexture.width != resolutionWidth || previewTexture.height != resolutionHeight)
            {
                UpdatePreviewTexture();
            }

            Camera previewCamera = GetActiveCamera();
            if (previewCamera != null)
            {
                RenderTexture originalRenderTexture = previewCamera.targetTexture;
                CameraClearFlags originalClearFlags = previewCamera.clearFlags;
                Color originalBackgroundColor = previewCamera.backgroundColor;
                float originalFOV = previewCamera.fieldOfView;

                previewCamera.targetTexture = previewTexture;
                SetCameraBackground(previewCamera);
                previewCamera.fieldOfView = fieldOfView;

                if (captureSelectedOnly)
                {
                    RenderSelectedObjects(previewCamera);
                }
                else
                {
                    previewCamera.Render();
                }

                previewCamera.targetTexture = originalRenderTexture;
                previewCamera.clearFlags = originalClearFlags;
                previewCamera.backgroundColor = originalBackgroundColor;
                previewCamera.fieldOfView = originalFOV;
            }
        }

        private void RenderSelectedObjects(Camera camera)
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            // Store the original layers and set selected objects to a temporary layer
            int tempLayer = LayerMask.NameToLayer("Ignore Raycast"); // Using an existing layer
            var originalLayers = new int[selectedObjects.Length];
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                originalLayers[i] = selectedObjects[i].layer;
                SetLayerRecursively(selectedObjects[i], tempLayer);
            }

            // Set the camera to only render the temporary layer
            int originalCullingMask = camera.cullingMask;
            camera.cullingMask = 1 << tempLayer;

            // Render
            camera.Render();

            // Restore original layers
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                SetLayerRecursively(selectedObjects[i], originalLayers[i]);
            }

            // Restore camera's original culling mask
            camera.cullingMask = originalCullingMask;
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (showPreview && useSceneView)
            {
                Repaint();
            }
        }

        private void TakeScreenshot()
        {
            if (string.IsNullOrEmpty(GetFolder()))
            {
                EditorUtility.DisplayDialog("Error", "Please select a save folder first.", "OK");
                return;
            }

            Camera camera = GetActiveCamera();
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Error", "No camera found in the scene.", "OK");
                return;
            }

            // Store original camera settings
            float originalFOV = camera.fieldOfView;
            CameraClearFlags originalClearFlags = camera.clearFlags;
            Color originalBackgroundColor = camera.backgroundColor;
            int originalCullingMask = camera.cullingMask;

            // Apply new settings
            camera.fieldOfView = fieldOfView;
            SetCameraBackground(camera);

            // Create render texture
            RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
            rt.antiAliasing = 8;
            RenderTexture.active = rt;
            camera.targetTexture = rt;

            // Render to texture
            if (captureSelectedOnly)
            {
                RenderSelectedObjects(camera);
            }
            else
            {
                camera.Render();
            }

            // Read pixels from render texture
            Texture2D screenshot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGBA32, false);
            screenshot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
            screenshot.Apply();

            // Save screenshot
            byte[] bytes = screenshot.EncodeToPNG();
            string filename;
            if (!string.IsNullOrEmpty(screenshotName))
            {
                filename = screenshotName + ".png";
            }
            else
            {
                filename = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            }
            Directory.CreateDirectory(GetFolder());
            File.WriteAllBytes(Path.Combine(GetFolder(), filename), bytes);

            // Clean up
            RenderTexture.active = null;
            camera.targetTexture = null;
            DestroyImmediate(rt);
            DestroyImmediate(screenshot);

            // Restore original camera settings
            camera.fieldOfView = originalFOV;
            camera.clearFlags = originalClearFlags;
            camera.backgroundColor = originalBackgroundColor;
            camera.cullingMask = originalCullingMask;

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(Path.Combine(GetFolder(), filename)));

            if (saveAsIcon)
            {
                TextureImporter icon = (TextureImporter)TextureImporter.GetAtPath(Path.Combine(GetFolder(), filename));

                icon.textureType = TextureImporterType.Sprite;
                icon.maxTextureSize = 256;

                EditorUtility.SetDirty(icon);
                icon.SaveAndReimport();
            }

            Debug.Log($"Screenshot saved to:\n{Path.Combine(GetFolder(), filename)}");
        }

        private Camera GetActiveCamera()
        {
            if (useSceneView && SceneView.lastActiveSceneView != null)
            {
                return SceneView.lastActiveSceneView.camera;
            }
            else if (selectedCamera != null)
            {
                return selectedCamera;
            }
            else
            {
                return Camera.main;
            }
        }

        private void SetCameraBackground(Camera camera)
        {
            if (useTransparentBackground)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
            }
            else if (useSkybox)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = backgroundColor;
            }
        }

        private string GetFolder()
        {
            return CommonActions.GetFolder(ScreenshotUseGlobalKey, ScreenshotFolderKey) + "/" + suffix;
        }
    }
}
#endif
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

public class ScreenshotTool : EditorWindow
{
    string folderPath = "Screenshots/"; // Default save location
    string[] formats = { "PNG", "JPEG", "EXR" }; // Supported image formats
    int selectedFormat = 0; // Default to PNG
    int resolutionMultiplier = 1; // Resolution multiplier
    string fileTag = "Screenshot"; // Custom tag for file naming

    // Advanced settings variables
    bool showAdvancedSettings = false; // Toggle for advanced settings
    int captureDelay = 0; // Delay before capturing (seconds)
    Texture2D watermark; // Watermark image

    [MenuItem("Tools/Quick Screenshot Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScreenshotTool>("Screenshot Tool");
        window.minSize = new Vector2(400, 300); // Set minimum size
    }

    void OnGUI()
    {
        GUILayout.Label("Quick Screenshot Tool", EditorStyles.boldLabel);

        GUILayout.Space(10);
        DrawHorizontalLine();

        GUILayout.Label("Save Settings", EditorStyles.boldLabel);
        DisplaySaveLocationControls();
        DisplayFileTagSetting();

        GUILayout.Space(10);
        DrawHorizontalLine();

        GUILayout.Label("Capture Settings", EditorStyles.boldLabel);
        resolutionMultiplier = EditorGUILayout.IntSlider("Resolution Multiplier", resolutionMultiplier, 1, 5);
        selectedFormat = EditorGUILayout.Popup("Image Format", selectedFormat, formats);

        GUILayout.Space(10);
        DrawAdvancedSettings(); // Advanced settings dropdown

        GUILayout.Space(10);
        DrawHorizontalLine();

        GUILayout.Label("Preview", EditorStyles.boldLabel);
        DisplayPreview();

        GUILayout.Space(10);
        SceneView sceneView = SceneView.lastActiveSceneView;

        EditorGUI.BeginDisabledGroup(sceneView == null);
        if (GUILayout.Button("Take Screenshot of Scene View"))
        {
            TakeEditorScreenshot();
        }
        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// Displays the advanced settings in a foldout section.
    /// </summary>
    void DrawAdvancedSettings()
    {
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++; // Indent for better structure
            captureDelay = EditorGUILayout.IntSlider("Capture Delay (s)", captureDelay, 0, 10);
            watermark = (Texture2D)EditorGUILayout.ObjectField("Watermark", watermark, typeof(Texture2D), false);

            if (watermark != null && !watermark.isReadable)
            {
                EditorGUILayout.HelpBox("The selected watermark texture is not readable. Enable 'Read/Write' in the texture import settings.", MessageType.Warning);
            }
            EditorGUI.indentLevel--;
        }
    }

    void DisplaySaveLocationControls()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Save Location", GUILayout.Width(100));
        EditorGUILayout.SelectableLabel(folderPath, EditorStyles.textField, GUILayout.Height(18));

        if (GUILayout.Button("Choose", GUILayout.Width(60)))
        {
            ChooseSaveLocation();
        }

        if (GUILayout.Button("Open", GUILayout.Width(60)))
        {
            OpenSaveLocation();
        }
        GUILayout.EndHorizontal();
    }

    void DisplayFileTagSetting()
    {
        fileTag = EditorGUILayout.TextField("File Tag", fileTag);
    }

    void DisplayPreview()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            var (width, height) = GetScreenshotDimensions(sceneView);
            GUILayout.Label($"Preview: {width} x {height} pixels");
        }
    }

    void DrawHorizontalLine()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }

    void ChooseSaveLocation()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Choose Save Folder", folderPath, "");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            folderPath = selectedPath;
        }
    }

    void OpenSaveLocation()
    {
        if (Directory.Exists(folderPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        else
        {
            UnityEngine.Debug.LogWarning("Save location does not exist.");
            folderPath = "Screenshots/"; // Reset to default if invalid
        }
    }

    async void TakeEditorScreenshot()
    {
        await Task.Delay(captureDelay * 1000); // Apply capture delay

        string fullPath = GenerateFilePath();

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        SceneView sceneView = GetActiveSceneView();
        if (sceneView == null) return;

        var (width, height) = GetScreenshotDimensions(sceneView);
        RenderTexture renderTexture = CaptureScene(sceneView, width, height);

        SaveScreenshot(renderTexture, fullPath);
        CleanupAfterCapture(sceneView, renderTexture);

        ShowNotification(new GUIContent($"Screenshot saved: {fullPath}"));
        UnityEngine.Debug.Log($"Screenshot saved: {fullPath}");
        AssetDatabase.Refresh();
    }

    string GenerateFilePath()
    {
        string extension = formats[selectedFormat].ToLower();
        string fileName = $"{fileTag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
        return Path.Combine(folderPath, fileName);
    }

    SceneView GetActiveSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            UnityEngine.Debug.LogError("No active Scene View found.");
        }
        return sceneView;
    }

    (int width, int height) GetScreenshotDimensions(SceneView sceneView)
    {
        Rect sceneViewRect = sceneView.position;
        int width = Mathf.FloorToInt(sceneViewRect.width) * resolutionMultiplier;
        int height = Mathf.FloorToInt(sceneViewRect.height) * resolutionMultiplier;
        return (width, height);
    }

    RenderTexture CaptureScene(SceneView sceneView, int width, int height)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        sceneView.camera.targetTexture = renderTexture;
        sceneView.camera.Render();
        return renderTexture;
    }

    void SaveScreenshot(RenderTexture renderTexture, string fullPath)
    {
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshot.Apply();

        if (watermark != null)
        {
            ApplyWatermark(screenshot);
        }

        byte[] bytes = selectedFormat == 0 ? screenshot.EncodeToPNG() :
                       selectedFormat == 1 ? screenshot.EncodeToJPG() :
                       screenshot.EncodeToEXR();

        File.WriteAllBytes(fullPath, bytes);
    }

    void ApplyWatermark(Texture2D screenshot)
    {
        int x = screenshot.width - watermark.width;
        int y = 0;

        for (int i = 0; i < watermark.width; i++)
        {
            for (int j = 0; j < watermark.height; j++)
            {
                Color watermarkPixel = watermark.GetPixel(i, j);
                watermarkPixel.a *= 0.5f; // Reduce opacity to 50%

                if (watermarkPixel.a > 0)
                {
                    Color screenshotPixel = screenshot.GetPixel(x + i, y + j);
                    Color blendedPixel = Color.Lerp(screenshotPixel, watermarkPixel, watermarkPixel.a);
                    screenshot.SetPixel(x + i, y + j, blendedPixel);
                }
            }
        }
        screenshot.Apply();
    }

    void CleanupAfterCapture(SceneView sceneView, RenderTexture renderTexture)
    {
        sceneView.camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);
    }
}

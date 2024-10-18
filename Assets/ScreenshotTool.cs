using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics; // For opening the file explorer

public class ScreenshotTool : EditorWindow
{
    string folderPath = "Screenshots/"; // Default save location
    string[] formats = { "PNG", "JPEG", "EXR" }; // Supported image formats
    int selectedFormat = 0; // Default to PNG
    int resolutionMultiplier = 1; // Resolution multiplier for the screenshot

    [MenuItem("Tools/Quick Screenshot Tool")]
    public static void ShowWindow()
    {
        GetWindow<ScreenshotTool>("Screenshot Tool");
    }

    void OnGUI()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        GUILayout.Label("Quick Screenshot Tool", EditorStyles.boldLabel);

        GUILayout.Space(10);

        GUILayout.Label("Save Settings", EditorStyles.boldLabel);
        DisplaySaveLocationControls();

        GUILayout.Space(5);

        GUILayout.Label("Capture Settings", EditorStyles.boldLabel);
        resolutionMultiplier = EditorGUILayout.IntSlider("Resolution Multiplier", resolutionMultiplier, 1, 5);
        selectedFormat = EditorGUILayout.Popup("Image Format", selectedFormat, formats);

        GUILayout.Space(5);

        DrawHorizontalLine();
        DisplayPreview();
        EditorGUI.BeginDisabledGroup(sceneView == null); // Disable button if no active Scene View
        if (GUILayout.Button("Take Screenshot of Scene View"))
        {
            TakeEditorScreenshot();
        }
        EditorGUI.EndDisabledGroup();
    }
    void DrawHorizontalLine()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    }

    void DisplaySaveLocationControls()
{
        GUILayout.BeginHorizontal();
            GUILayout.Label($"Save Location"); // Non-editable label
            EditorGUILayout.SelectableLabel(folderPath, EditorStyles.textField, GUILayout.Height(18)); // Read-only, selectable

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


    void DisplayPreview()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            var (width, height) = GetScreenshotDimensions(sceneView);
            GUILayout.Label($"Preview: {width} x {height} pixels");
        }
    }

    /// <summary>
    /// Opens a folder selection dialog to choose the save location.
    /// </summary>
    void ChooseSaveLocation()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Choose Save Folder", folderPath, "");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            folderPath = selectedPath;
        }
    }

    /// <summary>
    /// Opens the folder where screenshots are saved.
    /// </summary>
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
            UnityEngine.Debug.LogWarning("Save location does not exist. Resetting to default.");
            folderPath = "Screenshots/"; // Reset to default if path is invalid
        }
    }


    /// <summary>
    /// Captures an image of the current Scene View.
    /// </summary>
    void TakeEditorScreenshot()
    {
        string fullPath = GenerateFilePath();
        
        //makes the folder if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        SceneView sceneView = GetActiveSceneView();
        if (sceneView == null) return;

        var (width, height) = GetScreenshotDimensions(sceneView);
        RenderTexture renderTexture = CaptureScene(sceneView, width, height);

        SaveScreenshot(renderTexture, fullPath);
        
        //Cleanup so that there isn't any memory leaks (Being overly preventitive)
        sceneView.camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);

        this.ShowNotification(new GUIContent($"Screenshot saved"), 0.3f);
        UnityEngine.Debug.Log($"Screenshot saved: {fullPath}");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Generates the full path for the screenshot file.
    /// </summary>
    string GenerateFilePath()
    {
        string extension = formats[selectedFormat].ToLower();
        string fileName = $"UnityScreenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
        return Path.Combine(folderPath, fileName);
    }

    /// <summary>
    /// Retrieves the currently active Scene View.
    /// </summary>
    SceneView GetActiveSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            UnityEngine.Debug.LogError("No active Scene View found.");
        }
        return sceneView;
    }

    /// <summary>
    /// Calculates the dimensions for the screenshot based on the Scene View.
    /// </summary>
    (int width, int height) GetScreenshotDimensions(SceneView sceneView)
    {
        Rect sceneViewRect = sceneView.position;
        int width = Mathf.FloorToInt(sceneViewRect.width) * resolutionMultiplier;
        int height = Mathf.FloorToInt(sceneViewRect.height) * resolutionMultiplier;
        return (width, height);
    }

    /// <summary>
    /// Captures the Scene View into a RenderTexture.
    /// </summary>
    RenderTexture CaptureScene(SceneView sceneView, int width, int height)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        sceneView.camera.targetTexture = renderTexture;
        sceneView.camera.Render();
        return renderTexture;
    }

    /// <summary>
    /// Saves the captured screenshot to the specified file path.
    /// </summary>
    void SaveScreenshot(RenderTexture renderTexture, string fullPath)
    {
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshot.Apply();

        byte[] bytes = selectedFormat == 0 ? screenshot.EncodeToPNG() :
                       selectedFormat == 1 ? screenshot.EncodeToJPG() :
                       screenshot.EncodeToEXR();

        File.WriteAllBytes(fullPath, bytes);
    }
}

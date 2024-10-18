/*
 Made by Samuel Murrandah
Student Number: 1031741
Student Email: 1031741@student.sae.edu.au
Class Code: GPG315
Assignment: 1
*/

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics; //This is for the actual saving of the image
using System.Threading.Tasks; // This is pretty much only for the countdown

public class ScreenshotTool : EditorWindow
{
    // Basic settings variables
    string folderPath = "Screenshots/"; // Default save location
    string[] formats = { "PNG", "JPEG", "EXR" }; // Supported image formats
    int selectedFormat = 0; // Default format (PNG)
    int resolutionMultiplier = 1; // Multiplier for screenshot resolution
    string fileTag = "Screenshot"; // Custom tag for file naming

    // Advanced settings variables
    int captureDelay = 0; // Delay before capturing (in seconds)
    Texture2D watermark; // Optional watermark image

    // Menu item to open the tool window
    [MenuItem("Tools/Quick Screenshot Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScreenshotTool>("Screenshot Tool");
        window.minSize = new Vector2(400, 300); // Set minimum window size
    }

    // Toggles for save, capture, and advanced settings
    bool showSaveSettings = true;   // Open by default
    bool showCaptureSettings = true; // Open by default
    bool showAdvancedSettings = false; // Closed by default

    void OnGUI()
    {
        GUILayout.Label(new GUIContent("Quick Screenshot Tool", "A tool for taking screenshots of the Scene View"), EditorStyles.boldLabel);

        GUILayout.Space(10);
        DrawHorizontalLine(); // Draw separator

        // Save Settings Foldout (Open by default)
        showSaveSettings = EditorGUILayout.Foldout(showSaveSettings, new GUIContent("Save Settings", "Settings for saving screenshots"));
        if (showSaveSettings)
        {
            EditorGUI.indentLevel++;
            DisplaySaveLocationControls(); // Show save path and controls
            DisplayFileTagSetting(); // Input for file tag
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);
        DrawHorizontalLine(); // Draw separator

        // Capture Settings Foldout (Open by default)
        showCaptureSettings = EditorGUILayout.Foldout(showCaptureSettings, new GUIContent("Capture Settings", "Settings for capturing the screenshot"));
        if (showCaptureSettings)
        {
            EditorGUI.indentLevel++;
            resolutionMultiplier = EditorGUILayout.IntSlider(
                new GUIContent("Resolution Multiplier", "Adjusts the resolution scale of the screenshot"),
                resolutionMultiplier, 1, 5
            );

            selectedFormat = EditorGUILayout.Popup(
                new GUIContent("Image Format", "Select the format for saving the screenshot"),
                selectedFormat, formats
            );
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);
        DrawHorizontalLine(); // Draw separator

        // Advanced Settings Foldout (Closed by default)
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, new GUIContent("Advanced Settings", "Optional advanced settings"));
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;
            captureDelay = EditorGUILayout.IntSlider(
                new GUIContent("Capture Delay (s)", "Delay before the screenshot is taken (in seconds)"),
                captureDelay, 0, 10
            );

            watermark = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("Watermark", "Optional watermark to overlay on the screenshot"),
                watermark, typeof(Texture2D), false
            );

            if (watermark != null && !watermark.isReadable)
            {
                EditorGUILayout.HelpBox("The selected watermark texture is not readable. Enable 'Read/Write' in the texture import settings.", MessageType.Warning);
            }

            GUILayout.Label(new GUIContent("Preview", "Preview the dimensions of the screenshot"), EditorStyles.boldLabel);
            DisplayPreview(); // Show screenshot dimensions (Basically resolution)

            GUILayout.Space(10);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);

        SceneView sceneView = SceneView.lastActiveSceneView;
        EditorGUI.BeginDisabledGroup(sceneView == null); // Disable button if no active Scene View

        if (GUILayout.Button(new GUIContent("Take Screenshot of Scene View", "Capture a screenshot of the active Scene View")))
        {
            TakeEditorScreenshot(); // Capture screenshot
        }
        EditorGUI.EndDisabledGroup();
    }




    /// <summary>
    /// Draws the advanced settings foldout section.
    /// </summary>
    void DrawAdvancedSettings()
    {
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, new GUIContent("Advanced Settings", "Optional advanced settings"));

        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;

            captureDelay = EditorGUILayout.IntSlider(
                new GUIContent("Capture Delay (s)", "Delay before the screenshot is taken (in seconds)"),
                captureDelay, 0, 10
            );

            watermark = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("Watermark", "Optional watermark to overlay on the screenshot"),
                watermark, typeof(Texture2D), false
            );

            if (watermark != null && !watermark.isReadable)
            {
                EditorGUILayout.HelpBox("The selected watermark texture is not readable. Enable 'Read/Write' in the texture import settings.", MessageType.Warning);
            }


            GUILayout.Label(new GUIContent("Preview", "Preview the dimensions of the screenshot"), EditorStyles.boldLabel);
            DisplayPreview(); // Show screenshot dimensions

            DrawHorizontalLine();

            GUILayout.Space(10);
            EditorGUI.indentLevel--;
        }
    }


    /// <summary>
    /// Displays save location controls (path, choose, open).
    /// </summary>
    void DisplaySaveLocationControls()
    {
        GUILayout.BeginHorizontal();

            GUILayout.Label(new GUIContent("Save Location", "Path where screenshots will be saved"), GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(folderPath, EditorStyles.textField, GUILayout.Height(18));

            if (GUILayout.Button(new GUIContent("Choose", "Select a folder to save screenshots"), GUILayout.Width(60)))
            {
                ChooseSaveLocation();
            }

            if (GUILayout.Button(new GUIContent("Open", "Open the folder where screenshots are saved"), GUILayout.Width(60)))
            {
                OpenSaveLocation();
            }

        GUILayout.EndHorizontal();
    }


    /// <summary>
    /// Displays input field for custom file tag.
    /// </summary>
    void DisplayFileTagSetting()
    {
        fileTag = EditorGUILayout.TextField(
            new GUIContent("File Tag", "Tag to be included in the screenshot filename"),
            fileTag
        );
    }


    /// <summary>
    /// Displays the preview of screenshot dimensions.
    /// </summary>
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
    /// Draws a horizontal separator line.
    /// </summary>
    void DrawHorizontalLine()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }

    /// <summary>
    /// Opens a folder selection dialog to choose save location.
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
    /// Opens the save location folder in the file explorer.
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
            UnityEngine.Debug.LogWarning("Save location does not exist.");
            folderPath = "Screenshots/"; // Reset to default if invalid
        }
    }

    /// <summary>
    /// Captures a screenshot of the Scene View with the current settings.
    /// </summary>
    async void TakeEditorScreenshot()
    {
        await Task.Delay(captureDelay * 1000); // Apply capture delay

        string fullPath = GenerateFilePath(); // Generate file path

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath); // Create folder if it doesn't exist
        }

        SceneView sceneView = GetActiveSceneView();
        if (sceneView == null) return;

        var (width, height) = GetScreenshotDimensions(sceneView);
        RenderTexture renderTexture = CaptureScene(sceneView, width, height);

        SaveScreenshot(renderTexture, fullPath); // Save the screenshot
        CleanupAfterCapture(sceneView, renderTexture); // Clean up resources

        this.ShowNotification(new GUIContent("Screenshot saved!"));
        UnityEngine.Debug.Log($"Screenshot saved: {fullPath}");
        AssetDatabase.Refresh(); // Refresh AssetDatabase to reflect changes
    }

    /// <summary>
    /// Generates the full path for the screenshot file.
    /// </summary>
    string GenerateFilePath()
    {
        string extension = formats[selectedFormat].ToLower();
        string fileName = $"{fileTag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
        return Path.Combine(folderPath, fileName);
    }

    /// <summary>
    /// Retrieves the active Scene View.
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
    /// Captures the Scene View to a RenderTexture. (Basically screenshotting)
    /// </summary>
    RenderTexture CaptureScene(SceneView sceneView, int width, int height)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        sceneView.camera.targetTexture = renderTexture;
        sceneView.camera.Render();
        return renderTexture;
    }

    /// <summary>
    /// Saves the captured screenshot to the specified path.
    /// </summary>
    void SaveScreenshot(RenderTexture renderTexture, string fullPath)
    {
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshot.Apply();

        if (watermark != null) ApplyWatermark(screenshot);

        byte[] bytes = selectedFormat == 0 ? screenshot.EncodeToPNG() :
                       selectedFormat == 1 ? screenshot.EncodeToJPG() :
                       screenshot.EncodeToEXR();

        File.WriteAllBytes(fullPath, bytes);
    }

    /// <summary>
    /// Applies a watermark to the screenshot.
    /// </summary>
    void ApplyWatermark(Texture2D screenshot)
    {
        //This is techy and I hate it.
        // Define the max percentage of the screenshot the watermark can take (this stops lower resolutions from being completely overtaken by a watermark)
        float maxWatermarkWidthPercentage = 0.5f; // % of the screenshot's width
        float maxWatermarkHeightPercentage = 0.5f; // % of the screenshot's height

        // Calculate the max width and height for the watermark
        int maxWidth = Mathf.FloorToInt(screenshot.width * maxWatermarkWidthPercentage);
        int maxHeight = Mathf.FloorToInt(screenshot.height * maxWatermarkHeightPercentage);

        // Calculate scaling factors for the watermark
        float widthScale = Mathf.Min(1, maxWidth / (float)watermark.width);
        float heightScale = Mathf.Min(1, maxHeight / (float)watermark.height);
        float scale = Mathf.Min(widthScale, heightScale); // Use the smaller scale to maintain aspect ratio

        // Calculate the final size of the watermark
        int finalWidth = Mathf.FloorToInt(watermark.width * scale);
        int finalHeight = Mathf.FloorToInt(watermark.height * scale);

        // Determine the position to place the watermark (bottom-right corner)
        int x = screenshot.width - finalWidth;
        int y = 0; // You can adjust this if you want it vertically centered or positioned differently

        // Loop through the scaled watermark and apply it to the screenshot
        for (int i = 0; i < finalWidth; i++)
        {
            for (int j = 0; j < finalHeight; j++)
            {
                // Calculate the corresponding pixel from the original watermark
                int watermarkX = Mathf.FloorToInt(i / scale);
                int watermarkY = Mathf.FloorToInt(j / scale);

                // Get the watermark pixel and adjust opacity
                Color watermarkPixel = watermark.GetPixel(watermarkX, watermarkY);
                watermarkPixel.a *= 0.5f; // Reduce opacity

                if (watermarkPixel.a > 0)
                {
                    Color screenshotPixel = screenshot.GetPixel(x + i, y + j);
                    Color blendedPixel = Color.Lerp(screenshotPixel, watermarkPixel, watermarkPixel.a);
                    screenshot.SetPixel(x + i, y + j, blendedPixel);
                }
            }
        }

        screenshot.Apply(); // Apply the changes to the screenshot
    }


    /// <summary>
    /// Cleans up resources after capturing. (Good for memory leaks and just overall cleanliness)
    /// </summary>
    void CleanupAfterCapture(SceneView sceneView, RenderTexture renderTexture)
    {
        sceneView.camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);
    }
}

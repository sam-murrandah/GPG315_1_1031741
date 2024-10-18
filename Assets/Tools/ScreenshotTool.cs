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
using static PostProcessingEffects;

public class ScreenshotTool : EditorWindow
{
    // Basic Settings
    public string folderPath = Path.Combine(Application.dataPath, "Screenshots");
    public string[] formats = { "PNG", "JPEG", "EXR" };
    public int selectedFormat = 0;
    public int resolutionMultiplier = 1;
    public string fileTag = "Screenshot";
    public int captureDelay = 0;
    public Texture2D watermark;

    // Post-Processing Settings
    public bool shiftyMode = false;
    public float vignetteIntensity = 0f;
    public float noiseAmount = 0f;
    public Effect selectedEffect = Effect.None;

    // UI State
    private RenderTexture previewTexture;
    private Vector2 scrollPosition = Vector2.zero;
    private bool showSaveSettings = true;
    private bool showCaptureSettings = true;
    private bool showAdvancedSettings = false;
    private bool showPostProcSettings = false;

    // UI Management
    private ScreenshotToolUI ui;


    [MenuItem("Tools/Quick Screenshot Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScreenshotTool>("Screenshot Tool");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        ui = new ScreenshotToolUI(this);
    }

    void OnGUI()
    {
        scrollPosition = ui.DrawScrollView(scrollPosition, position.width, position.height);

        ui.DrawHeader("Quick Screenshot Tool");
        ui.DrawSaveSettings(ref showSaveSettings);
        ui.DrawCaptureSettings(ref showCaptureSettings);
        ui.DrawAdvancedSettings(ref showAdvancedSettings);
        ui.DrawPostProcessingSettings(ref showPostProcSettings);
        ui.DrawTakeScreenshotButton(SceneView.lastActiveSceneView);
        ui.DrawLivePreview();

        EditorGUILayout.EndScrollView();
    }
   
    /// <summary>
    /// Displays save location controls (path, choose, open).
    /// </summary>
    internal void DisplaySaveLocationControls()
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
    internal void DisplayFileTagSetting()
    {
        fileTag = EditorGUILayout.TextField(
            new GUIContent("File Tag", "Tag to be included in the screenshot filename"),
            fileTag
        );
    }

    /// <summary>
    /// Displays the preview of screenshot dimensions.
    /// </summary>
    internal void DisplayPreview()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        var (width, height) = GetScreenshotDimensions(sceneView);
        GUILayout.Label($"Resolution: {width} x {height} pixels");
    }


    /// <summary>
    /// Opens a folder selection dialog to choose save location.
    /// </summary>
    internal void ChooseSaveLocation()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Choose Save Folder", folderPath, "");
        if (string.IsNullOrEmpty(selectedPath)) return;
            folderPath = selectedPath;
    }

    /// <summary>
    /// Opens the save location folder in the file explorer.
    /// </summary>
    internal void OpenSaveLocation()
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
    internal async void TakeEditorScreenshot()
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
    internal string GenerateFilePath()
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

    internal void DisplayLivePreview()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
        {
            GUILayout.Label("No active Scene View available.");
            return;
        }

        // Get the dimensions of the Scene View
        var (sceneWidth, sceneHeight) = GetScreenshotDimensions(sceneView);

        // Calculate the aspect ratio
        float aspectRatio = (float)sceneWidth / sceneHeight;

        // Get the available width and height in the editor window
        float availableWidth = position.width - 20;  // Subtracting some padding
        float availableHeight = position.height - 100; // Accounting for layout elements

        // Adjust the preview size to fit within available space while maintaining aspect ratio
        float previewWidth, previewHeight;

        if (availableWidth / aspectRatio <= availableHeight)
        {
            previewWidth = availableWidth;
            previewHeight = availableWidth / aspectRatio;
        }
        else
        {
            previewHeight = availableHeight;
            previewWidth = availableHeight * aspectRatio;
        }

        // Create or reuse the preview RenderTexture
        if (previewTexture == null || previewTexture.width != (int)previewWidth || previewTexture.height != (int)previewHeight)
        {
            if (previewTexture != null) previewTexture.Release(); // Release previous texture
            previewTexture = new RenderTexture((int)previewWidth, (int)previewHeight, 24);
        }

        // Set the camera to render to the preview texture
        sceneView.camera.targetTexture = previewTexture;
        sceneView.camera.Render();
        sceneView.camera.targetTexture = null;

        // Read the RenderTexture into a Texture2D
        RenderTexture.active = previewTexture;
        Texture2D tempTexture = new Texture2D((int)previewWidth, (int)previewHeight, TextureFormat.RGB24, false);
        tempTexture.ReadPixels(new Rect(0, 0, previewWidth, previewHeight), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null; // Unbind the RenderTexture

        AddAllEffects(tempTexture);

        // Display the preview texture in the editor window
        GUILayout.Label(new GUIContent(tempTexture), GUILayout.Width(previewWidth), GUILayout.Height(previewHeight));

        // Clean up the temporary texture
        DestroyImmediate(tempTexture);
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

        AddAllEffects(screenshot);
        byte[] bytes = selectedFormat == 0 ? screenshot.EncodeToPNG() :
                       selectedFormat == 1 ? screenshot.EncodeToJPG() :
                       screenshot.EncodeToEXR();

        File.WriteAllBytes(fullPath, bytes);
    }

    void AddAllEffects(Texture2D screenshot)
    {
        ApplyEffect(screenshot, (Effect)selectedEffect); // Use enum from PostProcessingEffects
        if (shiftyMode) ApplyShiftyModeAcrossRows(screenshot);
        if (noiseAmount > 0) ApplyNoise(screenshot, noiseAmount);
        ApplyVignette(screenshot, vignetteIntensity);
        if (watermark != null) ApplyWatermark(screenshot);
    }

    /// <summary>
    /// Applies a watermark to the screenshot.
    /// </summary>
    void ApplyWatermark(Texture2D screenshot)
    {
        if (watermark == null)
        {
            return;
        }

        // Check if the watermark texture has read/write enabled
        try
        {
            // Try to access a pixel to test read/write permissions
            watermark.GetPixel(0, 0);
        }
        catch (UnityException)
        {
            return;
        }

        float maxWatermarkWidthPercentage = 0.5f;
        float maxWatermarkHeightPercentage = 0.5f;

        int maxWidth = Mathf.FloorToInt(screenshot.width * maxWatermarkWidthPercentage);
        int maxHeight = Mathf.FloorToInt(screenshot.height * maxWatermarkHeightPercentage);

        float widthScale = Mathf.Min(1, maxWidth / (float)watermark.width);
        float heightScale = Mathf.Min(1, maxHeight / (float)watermark.height);
        float scale = Mathf.Min(widthScale, heightScale);

        int finalWidth = Mathf.FloorToInt(watermark.width * scale);
        int finalHeight = Mathf.FloorToInt(watermark.height * scale);

        int x = screenshot.width - finalWidth;
        int y = 0;

        for (int i = 0; i < finalWidth; i++)
        {
            for (int j = 0; j < finalHeight; j++)
            {
                int watermarkX = Mathf.FloorToInt(i / scale);
                int watermarkY = Mathf.FloorToInt(j / scale);

                Color watermarkPixel = watermark.GetPixel(watermarkX, watermarkY);
                watermarkPixel.a *= 0.5f;

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


    void ApplyShiftyModeAcrossRows(Texture2D screenshot)
    {
        int width = screenshot.width;
        int height = screenshot.height;

        // Get the original pixels
        Color[] originalPixels = screenshot.GetPixels();
        Color[] glitchPixels = new Color[originalPixels.Length];

        // Initialize glitchPixels with the original pixels (avoids black gaps)
        for (int i = 0; i < originalPixels.Length; i++)
        {
            glitchPixels[i] = originalPixels[i];
        }

        // Randomly shift pixels and corrupt colors
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate the original pixel index
                int originalIndex = y * width + x;

                // Generate a random offset (can wrap around the edges)
                int offsetX = (x + Random.Range(-30, 30) + width) % width;
                int offsetY = (y + Random.Range(-30, 30) + height) % height;
                int newIndex = offsetY * width + offsetX;

                // Randomly corrupt the color channels
                Color corruptedColor = originalPixels[originalIndex];
                if (Random.value > 0.9f) // % chance of color corruption
                {
                    corruptedColor.r = Random.value; // Random red value
                    corruptedColor.g = Random.value; // Random green value
                    corruptedColor.b = Random.value; // Random blue value
                }
                else if (Random.value > 0.9f) // % chance of black
                {
                    corruptedColor.r = 0.2f; // Random red value
                    corruptedColor.g = 0.2f; // Random green value
                    corruptedColor.b = 0.2f; // Random blue value
                }
                else
                {
                    corruptedColor.g += 0.1f; // Random green value
                }

                // Assign the corrupted color to the new position
                glitchPixels[newIndex] = corruptedColor;
            }
        }

        // Apply the glitchy pixels back to the texture
        screenshot.SetPixels(glitchPixels);
        screenshot.Apply(); // Apply the changes
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

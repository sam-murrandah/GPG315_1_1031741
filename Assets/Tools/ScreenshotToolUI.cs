using UnityEditor;
using UnityEngine;
using static PostProcessingEffects;

public class ScreenshotToolUI
{
    private ScreenshotTool screenshotTool;

    public ScreenshotToolUI(ScreenshotTool tool)
    {
        screenshotTool = tool;
    }

    public Vector2 DrawScrollView(Vector2 scrollPosition, float windowWidth, float windowHeight)
    {
        return EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Width(windowWidth),
            GUILayout.Height(windowHeight)
        );
    }

    public void DrawHeader(string title)
    {
        GUILayout.Label(new GUIContent(title, "A tool for taking screenshots of the Scene View"), EditorStyles.boldLabel);
        GUILayout.Space(10);
        DrawHorizontalLine();
    }

    public void DrawSaveSettings(ref bool showSaveSettings)
    {
        showSaveSettings = EditorGUILayout.Foldout(showSaveSettings, new GUIContent("Save Settings", "Settings for saving screenshots"));

        if (showSaveSettings)
        {
            EditorGUI.indentLevel++;
            screenshotTool.DisplaySaveLocationControls();
            screenshotTool.DisplayFileTagSetting();
            EditorGUI.indentLevel--;
            DrawHorizontalLine();
        }

        GUILayout.Space(10);
    }

    public void DrawCaptureSettings(ref bool showCaptureSettings)
    {
        showCaptureSettings = EditorGUILayout.Foldout(showCaptureSettings, new GUIContent("Capture Settings", "Settings for capturing the screenshot"));

        if (showCaptureSettings)
        {
            EditorGUI.indentLevel++;

            screenshotTool.resolutionMultiplier = EditorGUILayout.IntSlider(
                new GUIContent("Resolution Multiplier", "Adjusts the resolution scale of the screenshot"),
                screenshotTool.resolutionMultiplier, 1, 5
            );

            screenshotTool.selectedFormat = EditorGUILayout.Popup(
                new GUIContent("Image Format", "Select the format for saving the screenshot"),
                screenshotTool.selectedFormat, screenshotTool.formats
            );

            GUILayout.Label(new GUIContent("Preview", "Preview the dimensions of the screenshot"), EditorStyles.boldLabel);
            screenshotTool.DisplayPreview();
            
            EditorGUI.indentLevel--;
            DrawHorizontalLine();
        }
        GUILayout.Space(10);
    }

    public void DrawAdvancedSettings(ref bool showAdvancedSettings)
    {
        showAdvancedSettings = EditorGUILayout.Foldout(
            showAdvancedSettings,
            new GUIContent("Advanced Settings", "Optional advanced settings")
        );

        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;

            screenshotTool.captureDelay = EditorGUILayout.IntSlider(
                new GUIContent("Capture Delay (s)", "Delay before the screenshot is taken (in seconds)"),
                screenshotTool.captureDelay, 0, 10
            );

            // Watermark field with read/write validation
            screenshotTool.watermark = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("Watermark", "Optional watermark to overlay on the screenshot"),
                screenshotTool.watermark, typeof(Texture2D), false
            );

            if (screenshotTool.watermark != null)
            {
                if (!IsTextureReadable(screenshotTool.watermark))
                {
                    EditorGUILayout.HelpBox(
                        "Watermark texture is not readable. Please enable 'Read/Write' in the texture import settings.",
                        MessageType.Error
                    );
                }
                else
                {
                    EditorGUILayout.HelpBox("Watermark is valid and ready to use.", MessageType.Info);
                }
            }

            EditorGUI.indentLevel--;
            DrawHorizontalLine();
        }

        GUILayout.Space(10);
    }

    // Helper method to check if the texture is readable
    private bool IsTextureReadable(Texture2D texture)
    {
        try
        {
            texture.GetPixel(0, 0); // Test read access
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }


    public void DrawPostProcessingSettings(ref bool showPostProcSettings)
    {
        showPostProcSettings = EditorGUILayout.Foldout(showPostProcSettings, new GUIContent("Post Processing Settings", "Optional post processing settings"));

        if (showPostProcSettings)
        {
            EditorGUI.indentLevel++;

            screenshotTool.vignetteIntensity = EditorGUILayout.Slider(
                new GUIContent("Vignette Intensity", "Adjusts the intensity of the vignette effect"),
                screenshotTool.vignetteIntensity, 0.0f, 1f
            );

            screenshotTool.noiseAmount = EditorGUILayout.Slider(
                new GUIContent("Noise Amount", "Adds random noise to the image"),
                screenshotTool.noiseAmount, 0f, 1f
            );

            screenshotTool.selectedEffect = (Effect)EditorGUILayout.EnumPopup(
                new GUIContent("Post-Processing", "Choose a post-processing effect to apply"),
                screenshotTool.selectedEffect
            );

            screenshotTool.shiftyMode = EditorGUILayout.Toggle(
                new GUIContent("Radiation Mode", "Bit of a joke setting, makes it look like you're staring at a bar of Plutonium"),
                screenshotTool.shiftyMode
            );

            EditorGUI.indentLevel--;
            DrawHorizontalLine();
        }

        GUILayout.Space(10);
    }

    public void DrawTakeScreenshotButton(SceneView sceneView)
    {
        EditorGUI.BeginDisabledGroup(sceneView == null);

        if (GUILayout.Button(new GUIContent("Take Screenshot of Scene View", "Capture a screenshot of the active Scene View")))
        {
            screenshotTool.TakeEditorScreenshot();
        }

        EditorGUI.EndDisabledGroup();
        GUILayout.Space(10);
    }

    public void DrawLivePreview()
    {
        GUILayout.Label(new GUIContent("Live Preview", "Preview of the post-processed screenshot"), EditorStyles.boldLabel);
        screenshotTool.DisplayLivePreview();
        GUILayout.Space(10);
    }

    private void DrawHorizontalLine()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }
}

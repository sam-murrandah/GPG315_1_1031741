/*
 Made by Samuel Murrandah
Student Number: 1031741
Student Email: 1031741@student.sae.edu.au
Class Code: GPG315
Assignment: 1
*/

using UnityEngine;

public static class PostProcessingEffects
{
    public enum Effect { None, Grayscale, Sepia, Posterize, Inverted, BadEyes }
    public enum ColourblindMode { Normal, Protanopia, Deuteranopia, Tritanopia }

    #region Post Processing Presets
    public static void ApplyEffect(Texture2D texture, Effect effect)
    {
        switch (effect)
        {
            case Effect.Grayscale:
                ApplyGrayscale(texture);
                break;
            case Effect.Sepia:
                ApplySepia(texture);
                break;
            case Effect.Posterize:
                ApplyPosterize(texture, 15); //Change this if you want more colours, 15 seemed to look best in my opinion
                break;
            case Effect.Inverted:
                ApplyInvertColours(texture);
                break;
            case Effect.BadEyes:
                ApplyBadEyes(texture);
                break;
        }
        texture.Apply();
    }

    public static void ApplyVignette(Texture2D texture, float intensity)
    {
        int width = texture.width;
        int height = texture.height;
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDistance = Vector2.Distance(Vector2.zero, center);

        Color[] pixels = texture.GetPixels();

        // Loop through every pixel in the texture
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;

                // Calculate how much darker the pixel should be
                float factor = Mathf.Clamp01(1.0f - intensity * distance);

                // Apply the darkening factor to the pixel colour
                pixels[index] *= factor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(); // Update the texture with the new pixel values
    }

    public static void ApplyNoise(Texture2D texture, float noiseAmount)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float noise = (Random.value * 1.5f - 1) * noiseAmount;
            pixels[i].r = Mathf.Clamp01(pixels[i].r + noise);
            pixels[i].g = Mathf.Clamp01(pixels[i].g + noise);
            pixels[i].b = Mathf.Clamp01(pixels[i].b + noise);
        }
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private static void ApplyGrayscale(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float gray = (pixels[i].r + pixels[i].g + pixels[i].b) / 3f;
            pixels[i] = new Color(gray, gray, gray, pixels[i].a);
        }
        texture.SetPixels(pixels);
    }

    private static void ApplySepia(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color p = pixels[i];
            float r = (p.r * 0.393f) + (p.g * 0.769f) + (p.b * 0.189f);
            float g = (p.r * 0.349f) + (p.g * 0.686f) + (p.b * 0.168f);
            float b = (p.r * 0.272f) + (p.g * 0.534f) + (p.b * 0.131f);
            pixels[i] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), p.a);
           
            /* Values sourced from:
             * Processing Images to Sepia Tone in Python: A Step-by-Step Guide. (2024, October 8).
             * Terra Magnetica. https://terramagnetica.com/processing-an-image-to-sepia-tone-in-python/ */

        }
        texture.SetPixels(pixels);
    }

    private static void ApplyPosterize(Texture2D texture, int levels)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].r = Mathf.Floor(pixels[i].r * levels) / levels;
            pixels[i].g = Mathf.Floor(pixels[i].g * levels) / levels;
            pixels[i].b = Mathf.Floor(pixels[i].b * levels) / levels;
        }
        texture.SetPixels(pixels);
    }

    private static void ApplyInvertColours(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].r = 1.0f - pixels[i].r;
            pixels[i].g = 1.0f - pixels[i].g;
            pixels[i].b = 1.0f - pixels[i].b;
        }
        texture.SetPixels(pixels);
    }

    private static void ApplyBadEyes(Texture2D texture)
    {
        // Get the width and height of the texture.
        int width = texture.width;
        int height = texture.height;

        // Copy all the original pixels from the texture into an array.
        Color[] originalPixels = texture.GetPixels();

        // Create two new arrays: 
        // One to store intermediate horizontal pass results, and one for the final blurred result.
        Color[] tempPixels = new Color[originalPixels.Length];
        Color[] blurredPixels = new Color[originalPixels.Length];

        int blurRadius = 5; // Set how much blur we want to apply.

        // First, blur the image horizontally.
        for (int y = 0; y < height; y++) // Loop through each row (horizontal pass).
        {
            for (int x = 0; x < width; x++) // Loop through each pixel in the row.
            {
                Color sum = Color.black; // Start with black (empty) to accumulate colors.
                int pixelCount = 0; // Track how many pixels we sum up.

                // Add colors from surrounding pixels in the horizontal direction.
                for (int offsetX = -blurRadius; offsetX <= blurRadius; offsetX++)
                {
                    // Keep the pixel coordinates within the image boundaries.
                    int sampleX = Mathf.Clamp(x + offsetX, 0, width - 1);

                    // Add the color of the sampled pixel to the sum.
                    sum += originalPixels[y * width + sampleX];
                    pixelCount++; // Increment the count of pixels used.
                }

                // Store the average color in the temporary buffer.
                tempPixels[y * width + x] = sum / pixelCount;
            }
        }

        // Now, blur the image vertically using the results from the horizontal pass.
        for (int x = 0; x < width; x++) // Loop through each column (vertical pass).
        {
            for (int y = 0; y < height; y++) // Loop through each pixel in the column.
            {
                Color sum = Color.black; // Reset the color sum for each pixel.
                int pixelCount = 0; // Reset the count of pixels used.

                // Add colors from surrounding pixels in the vertical direction.
                for (int offsetY = -blurRadius; offsetY <= blurRadius; offsetY++)
                {
                    // Keep the coordinates within the image boundaries.
                    int sampleY = Mathf.Clamp(y + offsetY, 0, height - 1);

                    // Add the color of the sampled pixel to the sum.
                    sum += tempPixels[sampleY * width + x];
                    pixelCount++; // Increment the count of pixels used.
                }

                // Store the final blurred color in the output buffer.
                blurredPixels[y * width + x] = sum / pixelCount;
            }
        }

        // Apply the blurred pixels to the texture and update it.
        texture.SetPixels(blurredPixels);
        texture.Apply(); // Apply the changes to make them visible.
    }

    public static void ApplyRadMode(Texture2D screenshot)
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
    #endregion

    #region Colourblindness Support
    public static void ApplyColourblindFilter(Texture2D texture, ColourblindMode mode)
    {
        Color[] pixels = texture.GetPixels();
        Color[] modifiedPixels = new Color[pixels.Length];

        // Color transformation matrices
        float[,] protanopiaMatrix = {
            { 0.56667f, 0.43333f, 0.0f },
            { 0.55833f, 0.44167f, 0.0f },
            { 0.0f, 0.24167f, 0.75833f }
        };

        float[,] deuteranopiaMatrix = {
            { 0.625f, 0.375f, 0.0f },
            { 0.7f, 0.3f, 0.0f },
            { 0.0f, 0.3f, 0.7f }
        };

        float[,] tritanopiaMatrix = {
            { 0.95f, 0.05f, 0.0f },
            { 0.0f, 0.43333f, 0.56667f },
            { 0.0f, 0.475f, 0.525f }
        };
        /* Data for matrixes sourced from:
             * DaltonLens. (2021, October 21). Understanding CVD simulation.
             * https://daltonlens.org/understanding-cvd-simulation/ */


        //Cleaner way to make a switch case and also just better for debugging (I hate big switch cases)
        float[,] selectedMatrix = mode switch
        {
            ColourblindMode.Protanopia => protanopiaMatrix,
            ColourblindMode.Deuteranopia => deuteranopiaMatrix,
            ColourblindMode.Tritanopia => tritanopiaMatrix,
            _ => null
        };

        if (selectedMatrix == null) return; // Skip processing for Normal mode

        // Apply the selected color transformation matrix
        for (int i = 0; i < pixels.Length; i++)
        {
            Color original = pixels[i];

            float r = original.r * selectedMatrix[0, 0] + original.g * selectedMatrix[0, 1] + original.b * selectedMatrix[0, 2];
            float g = original.r * selectedMatrix[1, 0] + original.g * selectedMatrix[1, 1] + original.b * selectedMatrix[1, 2];
            float b = original.r * selectedMatrix[2, 0] + original.g * selectedMatrix[2, 1] + original.b * selectedMatrix[2, 2];

            modifiedPixels[i] = new Color(r, g, b, original.a); // Preserve alpha channel
        }

        // Set the modified pixels back to the texture
        texture.SetPixels(modifiedPixels);
        texture.Apply();
    }
    #endregion
}

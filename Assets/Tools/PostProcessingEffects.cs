using UnityEngine;

public static class PostProcessingEffects
{
    public enum Effect { None, Grayscale, Sepia, Posterize, Inverted }

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
                ApplyPosterize(texture, 15);
                break;
            case Effect.Inverted:
                ApplyInvertColors(texture);
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
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float factor = Mathf.Clamp01(1.0f - intensity * distance);
                pixels[index] *= factor;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
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

    private static void ApplyInvertColors(Texture2D texture)
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
}

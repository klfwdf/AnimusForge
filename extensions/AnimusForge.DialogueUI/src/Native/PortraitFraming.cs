using System;

namespace AnimusForge.DialogueUI.Native;

// Logical UI dimensions, independent of screen resolution and render-texture quality.
internal static class PortraitFraming
{
    internal const float TextureHeight = 630f;
    internal const float ViewportHeight = 169f;
    internal const float TextureOffsetY = -134f;
    internal const float EyeY = 68f;
    internal const float PortraitWorldHeight = 0.52f;
    internal const float RenderQuality = 1.35f;

    internal static bool TryGetCameraOffsets(float scale, out float distance, out float eyeAboveCenter)
    {
        distance = eyeAboveCenter = 0f;
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.01f) return false;
        float fullHeight = PortraitWorldHeight * scale * TextureHeight / ViewportHeight;
        // CharacterTableau uses a vertical FOV of PI/4; render scale only changes pixels.
        distance = fullHeight / (2f * (float)Math.Tan(Math.PI / 8));
        eyeAboveCenter = (0.5f - (EyeY - TextureOffsetY) / TextureHeight) * fullHeight;
        return true;
    }
}

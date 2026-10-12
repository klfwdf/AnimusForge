namespace AnimusForge.Illustrator.Core
{
    internal sealed class IllustrationStylePreset
    {
        internal IllustrationStylePreset(string directorPrompt, string imagePrompt, string negativePrompt = null, string apiStyle = null, bool isCustom = false)
        {
            DirectorPrompt = directorPrompt;
            // Send the full selected style independently of anything the director returns.
            // Keep endpoint-specific wording as a supplement, never as a shorter replacement.
            ImagePrompt = string.IsNullOrEmpty(imagePrompt) || imagePrompt == directorPrompt
                ? directorPrompt
                : directorPrompt + "\n" + imagePrompt;
            NegativePrompt = negativePrompt;
            ApiStyle = apiStyle;
            IsCustom = isCustom;
        }

        public string DirectorPrompt { get; }
        public string ImagePrompt { get; }
        public string NegativePrompt { get; }
        public string ApiStyle { get; }
        public bool IsCustom { get; }
    }

    /// <summary>
    /// One shared definition per style. Resolve once per request; built-in presets are immutable and cached.
    /// The image endpoint receives the full director style plus any endpoint-specific supplement.
    /// </summary>
    internal static class IllustrationStylePresets
    {
        private static readonly IllustrationStylePreset ClassicOil = new IllustrationStylePreset(
            IllustratorText.Get("Style_754") +
            IllustratorText.Get("Style_755") +
            IllustratorText.Get("Style_756") +
            IllustratorText.Get("Style_757") +
            IllustratorText.Get("Style_758") +
            IllustratorText.Get("Style_759") +
            IllustratorText.Get("Style_760") +
            IllustratorText.Get("Style_761") +
            IllustratorText.Get("Style_762"),
            null,
            IllustratorText.Get("Style_763"));

        private static readonly IllustrationStylePreset DarkEpic = new IllustrationStylePreset(
            IllustratorText.Get("Style_764"),
            IllustratorText.Get("Style_765"),
            "bright cheerful colors, cartoon, anime, cel shading, clean untarnished surfaces");

        private static readonly IllustrationStylePreset Cinematic = new IllustrationStylePreset(
            IllustratorText.Get("Style_766"),
            IllustratorText.Get("Style_767"),
            "flat lighting, washed-out colors, cartoon, anime, cluttered composition");

        private static readonly IllustrationStylePreset MosanArt = new IllustrationStylePreset(
            IllustratorText.Get("Style_768"),
            IllustratorText.Get("Style_769"),
            IllustratorText.Get("Style_770"));

        private static readonly IllustrationStylePreset NarrativeAnime = new IllustrationStylePreset(
            IllustratorText.Get("Style_771") +
            IllustratorText.Get("Style_772") +
            IllustratorText.Get("Style_773") +
            IllustratorText.Get("Style_774") +
            IllustratorText.Get("Style_775") +
            IllustratorText.Get("Style_776") +
            IllustratorText.Get("Style_777") +
            IllustratorText.Get("Style_778") +
            IllustratorText.Get("Style_779"),
            null,
            "chibi, oversized anime eyes, thick comic outlines, flat vector art, rough sketch, plastic skin, 3d game render, photographic rendering, heavy impasto, muddy colors, excessive bloom");

        private static readonly IllustrationStylePreset Vivid = new IllustrationStylePreset(
            IllustratorText.Get("Style_780"), null, "dull colors, washed out, cartoon, anime", "vivid");

        private static readonly IllustrationStylePreset Natural = new IllustrationStylePreset(
            IllustratorText.Get("Style_781"), null, "oversaturated, cartoon, anime", "natural");

        internal static IllustrationStylePreset Resolve(string selectedStyle, string customStylePrompt)
        {
            switch (selectedStyle)
            {
                case "custom":
                    // Do not rewrite saved user text or substitute a built-in style for an intentionally empty prompt.
                    string custom = customStylePrompt ?? string.Empty;
                    return new IllustrationStylePreset(custom, custom, isCustom: true);
                case "dark-epic": return DarkEpic;
                case "cinematic": return Cinematic;
                case "mosan-art": return MosanArt;
                case "narrative-anime": return NarrativeAnime;
                case "vivid": return Vivid;
                case "natural": return Natural;
                case "classic-oil":
                default: return ClassicOil;
            }
        }
    }
}

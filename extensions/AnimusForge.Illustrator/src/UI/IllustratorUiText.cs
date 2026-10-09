using TaleWorlds.Localization;

namespace AnimusForge.Illustrator.UI
{
    /// <summary>
    /// Player-facing labels used by Illustrator Gauntlet views.
    /// Resolve these when a view model is created so the UI does not parse
    /// localization strings from a per-frame binding getter.
    /// </summary>
    internal static class IllustratorUiText
    {
        internal static string Resolve(string id, string englishFallback)
            => new TextObject($"{{={id}}}{englishFallback}").ToString();

        internal static string GalleryTitle => Resolve("AF_ILL_UI_GALLERY_TITLE", "Calradia Visual Chronicle Gallery");
        internal static string All => Resolve("AF_ILL_UI_ALL", "All");
        internal static string Favorites => Resolve("AF_ILL_UI_FAVORITES", "Favorites");
        internal static string Redraw => Resolve("AF_ILL_UI_REDRAW", "Redraw");
        internal static string RedrawWithPrompt => Resolve("AF_ILL_UI_REDRAW_PROMPT", "Redraw with prompt");
        internal static string RedrawBasedOnImage => Resolve("AF_ILL_UI_REDRAW_IMAGE", "Redraw from this image");
        internal static string Copy => Resolve("AF_ILL_UI_COPY", "Copy");
        internal static string Gallery => Resolve("AF_ILL_UI_GALLERY", "Visual Chronicle");
        internal static string Close => Resolve("AF_ILL_UI_CLOSE", "Close");
        internal static string SceneProbe => Resolve("AF_ILL_UI_SCENE_PROBE", "Capture scene preview");
        internal static string ConvertToRpItem => Resolve("AF_ILL_UI_CONVERT_RP", "Convert to RP item");
        internal static string SetDefault => Resolve("AF_ILL_UI_SET_DEFAULT", "Set as default");
        internal static string Delete => Resolve("AF_ILL_UI_DELETE", "Delete");
        internal static string OpenFolder => Resolve("AF_ILL_UI_OPEN_FOLDER", "Open image folder");
    }
}

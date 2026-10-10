using TaleWorlds.Localization;

namespace AnimusForge.Illustrator.UI
{
    // Fixed display and editable input defaults. Existing asset titles and metadata stay in their producer path.
    internal static class IllustratorRemainingUiText
    {
        private static string Resolve(string id, string fallback)
            => new TextObject($"{{={id}}}{fallback}").ToString();

        private static string Format(string id, string fallback, string variable, object value)
        {
            var text = new TextObject($"{{={id}}}{fallback}");
            text.SetTextVariable(variable, value?.ToString() ?? string.Empty);
            return text.ToString();
        }

        internal static string GalleryPreviewUnavailable => Resolve("AF_ILL_UI_GALLERY_PREVIEW_UNAVAILABLE", "The image is missing or cannot be decoded. Refresh the gallery.");
        internal static string GalleryRedrawLoading => Resolve("AF_ILL_UI_GALLERY_REDRAW_LOADING", "Redrawing from the selected illustration. You may close the gallery while you wait.");
        internal static string GalleryRedrawComplete => Resolve("AF_ILL_UI_GALLERY_REDRAW_COMPLETE", "Redraw complete; saved as a new illustration.");
        internal static string GalleryConversionUnavailable => Resolve("AF_ILL_UI_GALLERY_CONVERSION_UNAVAILABLE", "The illustration or current campaign is no longer available. Reopen the gallery.");
        internal static string GalleryConversionSourceMissing => Resolve("AF_ILL_UI_GALLERY_CONVERSION_SOURCE_MISSING", "The original image file no longer exists. No item was created.");
        internal static string GalleryConversionDescriptionRequired => Resolve("AF_ILL_UI_GALLERY_CONVERSION_DESCRIPTION_REQUIRED", "Enter an illustration description for NPCs to read when you show them the item.");
        internal static string GalleryConversionCreateFailed => Resolve("AF_ILL_UI_GALLERY_CONVERSION_CREATE_FAILED", "The illustration item could not be created. Nothing was added to the inventory.");
        internal static string GalleryConversionDescriptionFailed(string error) => Format("AF_ILL_UI_GALLERY_CONVERSION_DESCRIPTION_FAILED", "The illustration description could not be saved: {ERROR}", "ERROR", error);
        internal static string GalleryConversionAlreadyOwned(string item) => Format("AF_ILL_UI_GALLERY_CONVERSION_ALREADY_OWNED", "{ITEM} is already in your inventory. Its description was updated; no duplicate was added.", "ITEM", item);
        internal static string GalleryConversionDelivered(string item) => Format("AF_ILL_UI_GALLERY_CONVERSION_DELIVERED", "{ITEM} was added to your inventory. You can show it to NPCs using Show Item during a conversation.", "ITEM", item);
        internal static string GalleryConversionDeliveryFailed => Resolve("AF_ILL_UI_GALLERY_CONVERSION_DELIVERY_FAILED", "The illustration could not be added to your inventory. Check your inventory and try again.");
        internal static string GalleryConversionDefaultTitle => Resolve("AF_ILL_UI_GALLERY_CONVERSION_DEFAULT_TITLE", "Calradia Chronicles");
        internal static string GalleryConversionItemName(string title) => Format("AF_ILL_UI_GALLERY_CONVERSION_ITEM_NAME", "{TITLE} illustration scroll", "TITLE", title);
        internal static string GalleryIntroductionName(string item) => Format("AF_ILL_UI_GALLERY_INTRODUCTION_NAME", "This is a painting called {ITEM}.", "ITEM", item);
        internal static string GalleryIntroductionTheme(string theme) => Format("AF_ILL_UI_GALLERY_INTRODUCTION_THEME", "Theme: {THEME}", "THEME", theme);
        internal static string GalleryIntroductionAction(string action) => Format("AF_ILL_UI_GALLERY_INTRODUCTION_ACTION", "Depicts: {ACTION}", "ACTION", action);
        internal static string GalleryIntroductionUnannotated => Resolve("AF_ILL_UI_GALLERY_INTRODUCTION_UNANNOTATED", "The contents of this painting have not been described.");
        internal static string GalleryIntroductionFiction => Resolve("AF_ILL_UI_GALLERY_INTRODUCTION_FICTION", "The contents of the painting are an artistic depiction and do not mean that these events actually happened.");
        internal static string PhotoAim(int count) => Format("AF_ILL_UI_PHOTO_AIM", "Enter: capture image {COUNT} - Esc: cancel", "COUNT", count);
        internal static string PhotoDecision(int count) => Format("AF_ILL_UI_PHOTO_DECISION", "Captured {COUNT} images. Start generation?", "COUNT", count);
        internal static string PhotoStopped => Resolve("AF_ILL_UI_PHOTO_STOPPED", "This framing session was stopped");
        internal static string PhotoGenerate => Resolve("AF_ILL_UI_PHOTO_GENERATE", "Generate");
        internal static string PhotoContinue => Resolve("AF_ILL_UI_PHOTO_CONTINUE", "Capture another");
        internal static string PhotoRestart => Resolve("AF_ILL_UI_PHOTO_RESTART", "Capture again");
    }
}

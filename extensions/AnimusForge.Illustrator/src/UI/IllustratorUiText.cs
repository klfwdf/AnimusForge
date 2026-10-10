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

        internal static string Format(string id, string englishFallback, string variable, object value)
        {
            var text = new TextObject($"{{={id}}}{englishFallback}");
            text.SetTextVariable(variable, value?.ToString() ?? string.Empty);
            return text.ToString();
        }

        internal static string Format2(string id, string englishFallback, string firstName, object firstValue, string secondName, object secondValue)
        {
            var text = new TextObject($"{{={id}}}{englishFallback}");
            text.SetTextVariable(firstName, firstValue?.ToString() ?? string.Empty);
            text.SetTextVariable(secondName, secondValue?.ToString() ?? string.Empty);
            return text.ToString();
        }

        internal static string Format3(string id, string englishFallback, string firstName, object firstValue, string secondName, object secondValue, string thirdName, object thirdValue)
        {
            var text = new TextObject($"{{={id}}}{englishFallback}");
            text.SetTextVariable(firstName, firstValue?.ToString() ?? string.Empty);
            text.SetTextVariable(secondName, secondValue?.ToString() ?? string.Empty);
            text.SetTextVariable(thirdName, thirdValue?.ToString() ?? string.Empty);
            return text.ToString();
        }

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
        internal static string GalleryWelcome => Resolve("AF_ILL_UI_GALLERY_WELCOME", "Browse the Calradia visual chronicle gallery.");
        internal static string GalleryReading => Resolve("AF_ILL_UI_GALLERY_READING", "Reading the gallery…");
        internal static string GalleryBusy => Resolve("AF_ILL_UI_GALLERY_BUSY", "Gallery is busy in the background. Try refreshing later.");
        internal static string GalleryLoading => Resolve("AF_ILL_UI_GALLERY_LOADING", "Loading image…");
        internal static string GalleryTextureFailed => Resolve("AF_ILL_UI_GALLERY_TEXTURE_FAILED", "Image texture could not be loaded. Refresh the gallery.");
        internal static string GalleryEmptyAll => Resolve("AF_ILL_UI_GALLERY_EMPTY_ALL", "No images have been archived yet. A new illustration will be created when a bulletin is generated.");
        internal static string GalleryEmptyFavorites => Resolve("AF_ILL_UI_GALLERY_EMPTY_FAVORITES", "No favorites yet. Select an image in All and click Favorites.");
        internal static string GalleryFavorite => Resolve("AF_ILL_UI_GALLERY_FAVORITE", "Favorite");
        internal static string GalleryUnfavorite => Resolve("AF_ILL_UI_GALLERY_UNFAVORITE", "Unfavorite");
        internal static string GalleryFavoriteFailed => Resolve("AF_ILL_UI_GALLERY_FAVORITE_FAILED", "Could not update favorite");
        internal static string GalleryUnfavoriteFailed => Resolve("AF_ILL_UI_GALLERY_UNFAVORITE_FAILED", "Could not remove favorite");
        internal static string GalleryFavoriteAdded => Resolve("AF_ILL_UI_GALLERY_FAVORITE_ADDED", "Favorited: ") + "";
        internal static string GalleryFavoriteRemoved => Resolve("AF_ILL_UI_GALLERY_FAVORITE_REMOVED", "Unfavorited: ") + "";
        internal static string GalleryCountAll(int count) => Format("AF_ILL_UI_GALLERY_COUNT_ALL", "{COUNT} archived images", "COUNT", count);
        internal static string GalleryCountFavorites(int count) => Format("AF_ILL_UI_GALLERY_COUNT_FAVORITES", "{COUNT} favorite images", "COUNT", count);
        internal static string GalleryError(string error) => Format("AF_ILL_UI_GALLERY_ERROR", "Gallery operation failed: {ERROR}", "ERROR", error);
        internal static string GalleryLoadError(string error) => Format("AF_ILL_UI_GALLERY_LOAD_ERROR", "Image loading failed: {ERROR}", "ERROR", error);
        internal static string GalleryFavoriteStatus(bool favorite, string title) => Format(favorite ? "AF_ILL_UI_GALLERY_FAVORITED" : "AF_ILL_UI_GALLERY_UNFAVORITED", favorite ? "Favorited: {TITLE}" : "Unfavorited: {TITLE}", "TITLE", title);
        internal static string GalleryItemTitle(string title, bool isDefault, bool isFavorite) => Format3("AF_ILL_UI_GALLERY_TITLE_MARKER", "{TITLE}{DEFAULT}{FAVORITE}", "TITLE", title, "DEFAULT", isDefault ? GalleryDefaultMarker : string.Empty, "FAVORITE", isFavorite ? GalleryFavoriteMarker : string.Empty);
        internal static string GalleryDeleted(string title) => Format("AF_ILL_UI_GALLERY_DELETED", "Deleted: {TITLE} (moved to recycle area)", "TITLE", title);
        internal static string GalleryDefaultMarker => Resolve("AF_ILL_UI_GALLERY_DEFAULT_MARKER", " [Default]");
        internal static string GalleryFavoriteMarker => Resolve("AF_ILL_UI_GALLERY_FAVORITE_MARKER", " [Favorite]");
        internal static string GalleryConvertTitle => Resolve("AF_ILL_UI_GALLERY_CONVERT_TITLE", "Convert to RP item");
        internal static string GalleryConvertHint => Resolve("AF_ILL_UI_GALLERY_CONVERT_HINT", "Review the image description before adding it to your inventory. NPCs will read this description when you show them the item.");
        internal static string GalleryConvertCancelled => Resolve("AF_ILL_UI_GALLERY_CONVERT_CANCELLED", "Conversion cancelled; no item was added.");
        internal static string GalleryConvertUnavailable => Resolve("AF_ILL_UI_GALLERY_CONVERT_UNAVAILABLE", "Could not open the image description editor.");
        internal static string GallerySetDefaultLoading => Resolve("AF_ILL_UI_GALLERY_SET_DEFAULT_LOADING", "Reading the local image and setting it as default…");
        internal static string GallerySetDefaultFailed => Resolve("AF_ILL_UI_GALLERY_SET_DEFAULT_FAILED", "Could not set the default image.");
        internal static string GalleryDeleteFailed => Resolve("AF_ILL_UI_GALLERY_DELETE_FAILED", "Delete failed");
        internal static string GalleryFolderFailed(string error) => Format("AF_ILL_UI_GALLERY_FOLDER_FAILED", "Could not open the image folder: {ERROR}", "ERROR", error);
        internal static string CardReady => Resolve("AF_ILL_UI_CARD_READY", "Ready");
        internal static string CardCopySuccess => Resolve("AF_ILL_UI_CARD_COPY_SUCCESS", "Prompt copied to clipboard");
        internal static string CardCopyFailed(string error) => Format("AF_ILL_UI_CARD_COPY_FAILED", "Copy failed: {ERROR}", "ERROR", error);
        internal static string CardPrepareFailed(string error) => Format("AF_ILL_UI_CARD_PREPARE_FAILED", "Generation preparation failed: {ERROR}", "ERROR", error);
        internal static string CardProbeFailed(string error) => Format("AF_ILL_UI_CARD_PROBE_FAILED", "Scene probe failed: {ERROR}", "ERROR", error);
        internal static string CardRedrawFailed(string error) => Format("AF_ILL_UI_CARD_REDRAW_FAILED", "Redraw preparation failed: {ERROR}", "ERROR", error);
        internal static string RedrawPromptTitle(bool basedOnImage) => Resolve(basedOnImage ? "AF_ILL_UI_REDRAW_IMAGE" : "AF_ILL_UI_REDRAW_PROMPT", basedOnImage ? "Redraw from this image" : "Redraw with prompt");
        internal static string RedrawPromptHint(bool basedOnImage) => Resolve(basedOnImage ? "AF_ILL_UI_REDRAW_IMAGE_HINT" : "AF_ILL_UI_REDRAW_PROMPT_HINT", basedOnImage ? "Describe the changes to make to the current image. Leave blank to cancel." : "Describe the image you want to redraw. Leave blank to cancel.");
        internal static string RedrawPromptRequired => Resolve("AF_ILL_UI_REDRAW_REQUIRED", "Enter a redraw prompt; generation was not started.");
        internal static string Save => Resolve("AF_ILL_UI_SAVE", "Save");
        internal static string Cancel => Resolve("AF_ILL_UI_CANCEL", "Cancel");
        internal static string WeeklyDefaultTitle => Resolve("AF_ILL_UI_WEEKLY_DEFAULT_TITLE", "Imperial Chronicle Illustration");
        internal static string WeeklyThinking => Resolve("AF_ILL_UI_WEEKLY_THINKING", "Preparing this week's chronicle illustration…");
        internal static string WeeklyDrawing => Resolve("AF_ILL_UI_WEEKLY_DRAWING", "Drawing a new illustration for this bulletin…");
        internal static string WeeklyDeleteUnsaved => Resolve("AF_ILL_UI_WEEKLY_DELETE_UNSAVED", "This illustration has not been archived and cannot be deleted.");
        internal static string WeeklyDeleteFailed => Resolve("AF_ILL_UI_WEEKLY_DELETE_FAILED", "Delete failed. Manage this image in the gallery.");
        internal static string WeeklyDeleted => Resolve("AF_ILL_UI_WEEKLY_DELETED", "Illustration moved to the recycle area. Click Redraw to create another.");
        internal static string WeeklyLoadFailed => Resolve("AF_ILL_UI_WEEKLY_LOAD_FAILED", "The bulletin illustration could not be loaded. Click Redraw to try again.");
        internal static string WeeklyReadBusy => Resolve("AF_ILL_UI_WEEKLY_READ_BUSY", "Image loading is busy. Reopen this bulletin later.");
        internal static string WeeklyPrepareFailed(string error) => Format("AF_ILL_UI_WEEKLY_PREPARE_FAILED", "Generation preparation failed: {ERROR}", "ERROR", error);
        internal static string WeeklyRedrawLoading => Resolve("AF_ILL_UI_WEEKLY_REDRAW_LOADING", "Redrawing from this image…");
        internal static string WeeklyRedrawSuccess => Resolve("AF_ILL_UI_WEEKLY_REDRAW_SUCCESS", "Redraw complete; saved as a new version.");
        internal static string WeeklyRedrawFailed(string error) => Format("AF_ILL_UI_WEEKLY_REDRAW_FAILED", "Redraw failed: {ERROR}", "ERROR", error);
        internal static string WeeklyBackground => Resolve("AF_ILL_UI_WEEKLY_BACKGROUND", "The illustration is generating in the background. You will be notified and the gallery will update when it is ready.");
        internal static string WeeklyComplete => Resolve("AF_ILL_UI_WEEKLY_COMPLETE", "This week's chronicle illustration is complete.");
        internal static string WeeklyNotSaved => Resolve("AF_ILL_UI_WEEKLY_NOT_SAVED", "The illustration could not be saved.");
        internal static string EncyclopediaButton => Resolve("AF_ILL_UI_ENCYCLOPEDIA_BUTTON", "Chronicle illustration");
        internal static string HeroUnavailable => Resolve("AF_ILL_UI_HERO_UNAVAILABLE", "The current hero is unavailable.");
        internal static string ApiTestPrefix => Resolve("AF_ILL_UI_API_TEST_PREFIX", "Image API test");
        internal static string ApiTestCanceling => Resolve("AF_ILL_UI_API_TEST_CANCELING", "Cancelling test…");
        internal static string ApiTestEnable => Resolve("AF_ILL_UI_API_TEST_ENABLE", "Enable image generation first.");
        internal static string ApiTestEndpoint => Resolve("AF_ILL_UI_API_TEST_ENDPOINT", "Enter a valid HTTP/HTTPS image API address first.");
        internal static string ApiTestCancelled => Resolve("AF_ILL_UI_API_TEST_CANCELLED", "Test cancelled.");
        internal static string ApiTestBusy => Resolve("AF_ILL_UI_API_TEST_BUSY", "Image generation is busy. Try again later.");
        internal static string ApiTestStarted => Resolve("AF_ILL_UI_API_TEST_STARTED", "Test started with the current configuration. Click again to cancel.");
        internal static string ApiTestModel => Resolve("AF_ILL_UI_API_TEST_MODEL", "Enter an image model name first.");
        internal static string ApiTestCanvas => Resolve("AF_ILL_UI_API_TEST_CANVAS", "Blank canvas for API testing; draw an apple on the canvas.");
        internal static string ApiTestSuccess(string elapsed) => Format("AF_ILL_UI_API_TEST_SUCCESS", "Test succeeded ({SECONDS}s). Image saved to the module Cache/Illustrator/ApiTest/last-result.png.", "SECONDS", elapsed);
        internal static string ApiTestFailure(string channel, string elapsed, string error) => Format3("AF_ILL_UI_API_TEST_FAILURE", "{CHANNEL} test failed ({SECONDS}s): {ERROR}", "CHANNEL", channel, "SECONDS", elapsed, "ERROR", error);
        internal static string ApiTestModePlayer2Edit => Resolve("AF_ILL_UI_API_TEST_MODE_PLAYER2_EDIT", "Player2 /image/edit");
        internal static string ApiTestModePlayer2Generate => Resolve("AF_ILL_UI_API_TEST_MODE_PLAYER2_GENERATE", "Player2 /image/generate");
        internal static string ApiTestModeImageApi => Resolve("AF_ILL_UI_API_TEST_MODE_IMAGE", "Image API");
        internal static string WeeklyDefaultChanged => Resolve("AF_ILL_UI_WEEKLY_DEFAULT_CHANGED", "The new default illustration is now displayed.");
        internal static string CardAutoSaveHint => Resolve("AF_ILL_UI_CARD_AUTOSAVE_HINT", "You may close this panel; the completed image is saved to the gallery automatically.");
        internal static string SceneTitle => Resolve("AF_ILL_UI_SCENE_TITLE", "Scene illustration");
        internal static string SceneProbeTitle => Resolve("AF_ILL_UI_SCENE_PROBE_TITLE", "Independent scene panorama probe");
        internal static string SceneProbeComplete => Resolve("AF_ILL_UI_SCENE_PROBE_COMPLETE", "Panorama probe complete; preview only, no model was called.");
        internal static string PortraitPreparing => Resolve("AF_ILL_UI_PORTRAIT_PREPARING", "Preparing character references…");
        internal static string PortraitComplete => Resolve("AF_ILL_UI_PORTRAIT_COMPLETE", "Chronicle portrait complete.");
        internal static string ConversationComplete => Resolve("AF_ILL_UI_CONVERSATION_COMPLETE", "Conversation illustration complete.");
        internal static string PortraitTitle(string hero) => Format("AF_ILL_UI_PORTRAIT_TITLE", "Chronicle portrait · {HERO}", "HERO", hero);
        internal static string ConversationTitle(string partner) => Format("AF_ILL_UI_CONVERSATION_TITLE", "Conversation illustration · with {HERO}", "HERO", partner);
    }
}

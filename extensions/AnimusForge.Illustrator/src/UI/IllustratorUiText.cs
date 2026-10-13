using System;
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
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static string RenderLiteral(TextObject text, params (string Name, object Value)[] values)
    {
        for (int i = 0; i < values.Length; i++)
            if (values[i].Value is TextObject nested)
                text.SetTextVariable(values[i].Name, nested);
            else
                text.SetTextVariable(values[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => values[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }

        internal static string Resolve(string id, string englishFallback)
            => new TextObject($"{{={id}}}{englishFallback}").ToString();

        internal static string Format(string id, string englishFallback, string variable, object value)
    {
        return RenderLiteral(new TextObject($"{{={id}}}{englishFallback}"), (variable, DisplayValue(variable, value)));
    }

        internal static string Format2(string id, string englishFallback, string firstName, object firstValue, string secondName, object secondValue)
    {
        return RenderLiteral(new TextObject($"{{={id}}}{englishFallback}"), (firstName, DisplayValue(firstName, firstValue)), (secondName, DisplayValue(secondName, secondValue)));
    }

        internal static string Format3(string id, string englishFallback, string firstName, object firstValue, string secondName, object secondValue, string thirdName, object thirdValue)
    {
        return RenderLiteral(new TextObject($"{{={id}}}{englishFallback}"), (firstName, DisplayValue(firstName, firstValue)), (secondName, DisplayValue(secondName, secondValue)), (thirdName, DisplayValue(thirdName, thirdValue)));
    }

        private static string DisplayValue(string name, object value)
        {
            string raw = value?.ToString() ?? string.Empty;
            return name == "ERROR" ? IllustrationFailureUiText.Project(raw) : raw;
        }

        internal static string ProjectKnownCachedPrompt(string value)
            => string.Equals(value, IllustratorText.Raw("Residual_DiskImageCacheManager_ec79815ca580"), StringComparison.Ordinal)
                ? Resolve("AF_ILL_UI_LOCAL_CACHE_PROMPT", "(A visual chronicle is retained locally)") : value;

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
        internal static string PortraitDrawing => Resolve("AF_ILL_UI_PORTRAIT_DRAWING", "Drawing a new chronicle portrait…");
        internal static string ConversationDrawing => Resolve("AF_ILL_UI_CONVERSATION_DRAWING", "Drawing a new conversation illustration…");
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
        internal static string DefaultHeroName => Resolve("AF_ILL_UI_DEFAULT_HERO_NAME", "Hero");
        internal static string DefaultPartnerName => Resolve("AF_ILL_UI_DEFAULT_PARTNER_NAME", "the other person");
        internal static string ConversationTitle(string partner) => Format("AF_ILL_UI_CONVERSATION_TITLE", "Conversation illustration · with {HERO}", "HERO", partner);
        internal static string ProjectKnownDefaultTitle(string title, string category)
        {
            if (string.IsNullOrWhiteSpace(title)) return title ?? string.Empty;
            if (string.Equals(title, IllustratorText.Raw("Residual_IllustratorUiText_3e7ebd8d664d"), StringComparison.Ordinal))
                return Resolve("AF_ILL_UI_GALLERY_LEGACY_DEFAULT_TITLE", "Calradia history illustration");
            // Only the fixed built-in fallback templates are projected. Model and user titles remain data.
            if (string.Equals(category, "encyclopedia", StringComparison.Ordinal))
            {
                string hero = DefaultTitleName(title, "Chronicle portrait · ", "")
                    ?? DefaultTitleName(title, IllustratorText.Raw("Residual_IllustratorUiText_e8c9a56ad3e5"), "】");
                if (hero != null) return PortraitTitle(hero == IllustratorText.Raw("Residual_IllustratorUiText_09e9b2088fa0") || hero == "Hero" ? DefaultHeroName : hero);
            }
            if (string.Equals(category, "conversation", StringComparison.Ordinal))
            {
                string partner = DefaultTitleName(title, "Conversation illustration · with ", "")
                    ?? DefaultTitleName(title, IllustratorText.Raw("Residual_IllustratorUiText_02e40f24f75d"), "】");
                if (partner != null) return ConversationTitle(partner == IllustratorText.Raw("Residual_IllustratorUiText_ddfdac569e37") || partner == "the other person" ? DefaultPartnerName : partner);
            }
            string portraitSuffix = IllustratorText.Raw("Residual_IllustratorUiText_82114fcb7616");
            if (string.Equals(category, "encyclopedia", StringComparison.Ordinal) && title.EndsWith(portraitSuffix, StringComparison.Ordinal))
            {
                string hero = title.Substring(0, title.Length - portraitSuffix.Length);
                if (!string.IsNullOrWhiteSpace(hero)) return PortraitTitle(string.Equals(hero, IllustratorText.Raw("Residual_IllustratorUiText_09e9b2088fa0"), StringComparison.Ordinal) ? DefaultHeroName : hero);
            }
            string meetingPrefix = IllustratorText.Raw("Residual_IllustratorUiText_c37e425bf8af");
            string meetingSuffix = IllustratorText.Raw("Residual_IllustratorUiText_1e2231fdda28");
            if (string.Equals(category, "conversation", StringComparison.Ordinal) && title.StartsWith(meetingPrefix, StringComparison.Ordinal) && title.EndsWith(meetingSuffix, StringComparison.Ordinal))
            {
                int partnerLength = title.Length - meetingPrefix.Length - meetingSuffix.Length;
                if (partnerLength > 0)
                {
                    string partner = title.Substring(meetingPrefix.Length, partnerLength);
                    return ConversationTitle(string.Equals(partner, IllustratorText.Raw("Residual_IllustratorUiText_ddfdac569e37"), StringComparison.Ordinal) ? DefaultPartnerName : partner);
                }
            }
            return title;
        }

        private static string DefaultTitleName(string title, string prefix, string suffix)
        {
            if (!title.StartsWith(prefix, StringComparison.Ordinal) || !title.EndsWith(suffix, StringComparison.Ordinal)
                || title.Length <= prefix.Length + suffix.Length) return null;
            string name = title.Substring(prefix.Length, title.Length - prefix.Length - suffix.Length);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        internal static string ConversationSessionChanged => Resolve("AF_ILL_UI_CONVERSATION_SESSION_CHANGED", "The previous conversation illustration was archived; the current conversation has changed.");
        internal static string ConversationFirstImageRequired => Resolve("AF_ILL_UI_CONVERSATION_FIRST_IMAGE_REQUIRED", "Click Redraw to create the first scene illustration for this conversation.");
        internal static string ConversationPreparing => Resolve("AF_ILL_UI_CONVERSATION_PREPARING", "The AI artist is analyzing the conversation and body language...");
        internal static string ConversationEquipmentUnavailable => Resolve("AF_ILL_UI_CONVERSATION_EQUIPMENT_UNAVAILABLE", "The complete equipment snapshot for both characters was unavailable. Generation stopped without substituting a troop template or another outfit.");
        internal static string ConversationReferenceReuse => Resolve("AF_ILL_UI_CONVERSATION_REFERENCE_REUSE", "Reusing the environment and character references captured at the start of this conversation");
        internal static string PortraitEquipmentUnavailable => Resolve("AF_ILL_UI_PORTRAIT_EQUIPMENT_UNAVAILABLE", "The encyclopedia's current displayed equipment could not be read. Generation stopped without guessing civilian or battle clothing.");
        internal static string CardDrawFailed(string error) => Format("AF_ILL_UI_CARD_DRAW_FAILED", "Drawing failed: {ERROR}", "ERROR", error);
        internal static string CardGenerationException(string error) => Format("AF_ILL_UI_CARD_GENERATION_EXCEPTION", "Generation error: {ERROR}", "ERROR", error);
        internal static string CardNotSavedOrDisplayed => Resolve("AF_ILL_UI_CARD_NOT_SAVED_OR_DISPLAYED", "The image could not be saved or displayed.");
        internal static string SceneProbeLoading => Resolve("AF_ILL_UI_SCENE_PROBE_LOADING", "Rebuilding the nearby environment and capturing a complete panorama...");
        internal static string SceneProbeDisplayFailed => Resolve("AF_ILL_UI_SCENE_PROBE_DISPLAY_FAILED", "The exported preview could not be displayed.");
        internal static string SceneProbeNote => Resolve("AF_ILL_UI_SCENE_PROBE_NOTE", "Isolated scene capture preview: six 90-degree views (front, back, left, right, up, and down) are captured from a static copy of the environment within 30 meters and projected into a 360-by-180-degree panorama. The center faces forward, the left and right edges meet behind you, and the top and bottom face up and down. Projection stretching does not mean the buildings are deformed. Characters were not copied, and the observation lighting does not represent the scene's actual lighting. No director or image model was called. This preview was not saved to the gallery or set as the default image.");
        internal static string StatusChronicle => Resolve("AF_ILL_UI_STATUS_CHRONICLE", "Chronicle illustration");
        internal static string StatusTheme(string theme) => Format("AF_ILL_UI_STATUS_THEME", "Theme: {THEME}", "THEME", theme);
        internal static string StatusStyleChanged => Resolve("AF_ILL_UI_STATUS_STYLE_CHANGED", "The style settings have changed. Click Redraw to apply the new style.");
        internal static string StatusDirectorDisabled => Resolve("AF_ILL_UI_STATUS_DIRECTOR_DISABLED", "The director is disabled");
        internal static string StatusDirectorUnconfigured => Resolve("AF_ILL_UI_STATUS_DIRECTOR_UNCONFIGURED", "The director API is not configured");
        internal static string StatusDirectorConstraint => Resolve("AF_ILL_UI_STATUS_DIRECTOR_CONSTRAINT", "The director output violated character or equipment constraints");
        internal static string StatusDirectorNarrative => Resolve("AF_ILL_UI_STATUS_DIRECTOR_NARRATIVE", "The director output was not converted into a visual description");
        internal static string StatusDirectorStructure => Resolve("AF_ILL_UI_STATUS_DIRECTOR_STRUCTURE", "The director output did not meet structure or composition requirements");
        internal static string StatusDirectorEmpty => Resolve("AF_ILL_UI_STATUS_DIRECTOR_EMPTY", "The director output lacked a visual description");
        internal static string StatusDirectorUnavailable => Resolve("AF_ILL_UI_STATUS_DIRECTOR_UNAVAILABLE", "The director output was unavailable");
        internal static string StatusVisionPrefix => Resolve("AF_ILL_UI_STATUS_VISION_PREFIX", "The director does not support image input; ");
        internal static string StatusLocalFallback(string reason) => Format("AF_ILL_UI_STATUS_LOCAL_FALLBACK", "{REASON}. A local composition was used", "REASON", reason);
        internal static string StatusSceneTerrain => Resolve("AF_ILL_UI_STATUS_SCENE_TERRAIN", "Field conversation: composing from terrain facts without a screenshot");
        internal static string StatusSceneDisabledScreenshot => Resolve("AF_ILL_UI_STATUS_SCENE_DISABLED_SCREENSHOT", "Scene panorama is disabled; using the current scene screenshot");
        internal static string StatusSceneDisabledFacts => Resolve("AF_ILL_UI_STATUS_SCENE_DISABLED_FACTS", "Scene panorama is disabled; composing from text facts");
        internal static string StatusScenePanorama => Resolve("AF_ILL_UI_STATUS_SCENE_PANORAMA", "Mission scene: panorama references within 30 meters are available");
        internal static string StatusScenePanoramaNoScreenshot => Resolve("AF_ILL_UI_STATUS_SCENE_PANORAMA_NO_SCREENSHOT", "Mission scene: panorama references within 30 meters are available; the current-position screenshot was unavailable");
        internal static string StatusSceneBudget(string limit) => Format("AF_ILL_UI_STATUS_SCENE_BUDGET", "The environment exceeded the {LIMIT}-object budget. Environment capture was skipped; drawing continues.", "LIMIT", limit);
        internal static string StatusDirectorFailed(string reason) => Format("AF_ILL_UI_STATUS_DIRECTOR_FAILED", "{REASON}; image generation stopped", "REASON", reason);
        internal static string StatusDirectorTruncated => Resolve("AF_ILL_UI_STATUS_DIRECTOR_TRUNCATED", "The director output was truncated; image generation stopped");
        internal static string StatusDirectorTextOnly => Resolve("AF_ILL_UI_STATUS_DIRECTOR_TEXT_ONLY", "The director does not support image input; text-only planning was used");
        internal static string StatusDirectorValidated => Resolve("AF_ILL_UI_STATUS_DIRECTOR_VALIDATED", "The director output was validated; the API did not return a finish marker");
        internal static string StatusDirectorComplete => Resolve("AF_ILL_UI_STATUS_DIRECTOR_COMPLETE", "The director finished planning");
        internal static string GalleryMenu => Resolve("AF_ILL_MENU_GALLERY", "Calradia Visual Chronicle Gallery (AI illustrations)");
        internal static string BulletinPreparing => Resolve("AF_ILL_UI_BULLETIN_PREPARING", "This bulletin's illustration is being prepared...");
        internal static string BulletinDiscarded => Resolve("AF_ILL_UI_BULLETIN_DISCARDED", "This bulletin event has been discarded.");
        internal static string BulletinCancelled => Resolve("AF_ILL_UI_BULLETIN_CANCELLED", "This bulletin's illustration was cancelled. Click Redraw to try again.");
        internal static string BulletinMissing => Resolve("AF_ILL_UI_BULLETIN_MISSING", "No generated illustration is available. Click Redraw to create one.");
        internal static string BulletinRedrawCancelled => Resolve("AF_ILL_UI_BULLETIN_REDRAW_CANCELLED", "Redrawing this image was cancelled.");
        internal static string BulletinRedrawComplete => Resolve("AF_ILL_UI_BULLETIN_REDRAW_COMPLETE", "Redraw complete; a new version was saved.");
        internal static string BulletinPrepareFailed(string error) => Format("AF_ILL_UI_BULLETIN_PREPARE_FAILED", "Illustration preparation failed: {ERROR}. Click Redraw to try again.", "ERROR", error);
        internal static string BulletinImageFailed(string error) => Format("AF_ILL_UI_BULLETIN_IMAGE_FAILED", "Illustration failed: {ERROR}. Click Redraw to try again.", "ERROR", error);
        internal static string BulletinRetry(string error) => Format("AF_ILL_UI_BULLETIN_RETRY", "{ERROR}; click Redraw to try again.", "ERROR", error);
        internal static string BulletinSaveImageFailed => Resolve("AF_ILL_UI_BULLETIN_SAVE_IMAGE_FAILED", "The image could not be saved");
        internal static string BulletinSaveUnavailable => Resolve("AF_ILL_UI_BULLETIN_SAVE_UNAVAILABLE", "Could not save");
        internal static string GenerationComplete(string title) => Format("AF_ILL_NOTIFY_GENERATION_COMPLETE", "[AI Illustration] {TITLE} is complete and the gallery has been updated.", "TITLE", title);
        internal static string GenerationFailed(string error) => Format("AF_ILL_NOTIFY_GENERATION_FAILED", "[AI Illustration] Generation failed: {ERROR}", "ERROR", error);
        internal static string MissionNotice(string status) => Format("AF_ILL_MISSION_NOTICE", "[AI Illustration] {STATUS}", "STATUS", status);
        internal static string MissionCancelled => Resolve("AF_ILL_MISSION_CANCELLED", "Capture cancelled; no image generation request was sent.");
        internal static string MissionCaptureFailed(string error) => Format("AF_ILL_MISSION_CAPTURE_FAILED", "Capture failed: {ERROR}", "ERROR", error);
        internal static string MissionStartFailed(string error) => Format("AF_ILL_MISSION_START_FAILED", "Could not start image generation: {ERROR}", "ERROR", error);
        internal static string MissionFailed(string error) => Format("AF_ILL_MISSION_FAILED", "Image generation failed: {ERROR}", "ERROR", error);
        internal static string MissionBattleTitle => Resolve("AF_ILL_MISSION_BATTLE_TITLE", "Battle scene");
        internal static string MissionShoutTitle => Resolve("AF_ILL_MISSION_SHOUT_TITLE", "Scene shout");
        internal static string MissionInputHint => Resolve("AF_ILL_MISSION_INPUT_HINT", "Click Generate image to frame the scene freely. Press Enter to capture one or two images, then confirm for the director to plan the illustration.");
        internal static string MissionInputPlaceholder => Resolve("AF_ILL_MISSION_INPUT_PLACEHOLDER", "This mode only generates images from screenshots; it does not send NPC shouts.");
        internal static string MissionNoDialogue => Resolve("AF_ILL_MISSION_NO_DIALOGUE", "[Scene Shout] No dialogue channel is available in this custom battle.");
        internal static string MissionCapturing => Resolve("AF_ILL_MISSION_CAPTURING", "[AI Illustration] Capturing the scene. You can generate another image once capture is complete.");
        internal static string MissionFraming => Resolve("AF_ILL_MISSION_FRAMING", "Frame the scene freely: press Enter to capture up to two images. Generation starts only after confirmation.");
        internal static string MissionDirecting => Resolve("AF_ILL_MISSION_DIRECTING", "Screenshots confirmed; the director is planning the illustration...");
        internal static string MissionGenerating => Resolve("AF_ILL_MISSION_GENERATING", "The director has finished; generating the illustration. You may close the panel...");
        internal static string MissionSaved => Resolve("AF_ILL_MISSION_SAVED", "Illustration saved to the gallery.");
        internal static string MissionBusy => Resolve("AF_ILL_MISSION_BUSY", "Image generation is busy. Wait for the existing tasks to finish.");
        internal static string MissionPreparing => Resolve("AF_ILL_MISSION_PREPARING", "Screenshots confirmed; preparing the illustration...");
        internal static string MissionPanelUnavailable => Resolve("AF_ILL_MISSION_PANEL_UNAVAILABLE", "[AI Illustration] The progress panel could not be opened. Generation continues in the background; view the result in the gallery when it is complete.");
        internal static string MissionScopeCancelled => Resolve("AF_ILL_MISSION_SCOPE_CANCELLED", "Screenshot illustration cancelled because image generation was disabled or the host changed.");
        internal static string GenerationScopeExpired => Resolve("AF_ILL_NOTIFY_SCOPE_EXPIRED", "The image generation context has expired because the view changed or the panel closed. Reopen it to continue.");
        internal static string GenerationTimeout => Resolve("AF_ILL_NOTIFY_TIMEOUT", "Image generation timed out: the upstream model did not respond within the time limit. Try again later.");
        internal static string GenerationBusy => Resolve("AF_ILL_NOTIFY_BUSY", "Image generation is busy. Wait for the existing tasks to finish, then try again.");
    }
}

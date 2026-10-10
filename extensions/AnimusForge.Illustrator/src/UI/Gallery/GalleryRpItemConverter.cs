using System;
using System.IO;
using System.Text;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace AnimusForge.Illustrator.UI.Gallery
{
    // Explicit click only. Reuse the shared RP registry, introduction and roster persistence.
    internal static class GalleryRpItemConverter
    {
        internal static string ItemName(CachedIllustrationItem item)
        {
            string title = AnimusForgeTextInputSanitizer.SanitizeSingleLine(item?.Title ?? "", 100).Trim();
            return "《" + (title.Length == 0 ? "卡拉迪亚纪事" : title) + "》画卷";
        }

        internal static string Identity(CachedIllustrationItem item)
        {
            return "illustrator_gallery:" + DiskImageCacheManager.SanitizeKey(item.CampaignKey) + ":" + item.Key;
        }

        internal static string BuildIntroduction(CachedIllustrationItem item)
        {
            var text = new StringBuilder(IllustratorRemainingUiText.GalleryIntroductionName(DisplayItemName(item))).Append('\n');
            string theme = Clean(item?.Theme, 600);
            string action = Clean(item?.ActionSummary, 900);
            if (theme.Length > 0) text.Append(IllustratorRemainingUiText.GalleryIntroductionTheme(theme)).Append('\n');
            if (action.Length > 0) text.Append(IllustratorRemainingUiText.GalleryIntroductionAction(action)).Append('\n');
            if (theme.Length == 0 && action.Length == 0)
                text.Append(IllustratorRemainingUiText.GalleryIntroductionUnannotated).Append('\n');
            text.Append(IllustratorRemainingUiText.GalleryIntroductionFiction);
            return text.ToString();
        }

        internal static bool TryConvert(CachedIllustrationItem image, string campaignKey, string introduction, out string message)
        {
            IllustratorRuntime.AssertMainThread();
            message = "";
            if (image == null || image.Deleted || string.IsNullOrWhiteSpace(image.Key) ||
                !string.Equals(DiskImageCacheManager.SanitizeKey(image.CampaignKey), DiskImageCacheManager.SanitizeKey(campaignKey), StringComparison.Ordinal) ||
                !string.Equals(campaignKey, IllustratorRuntime.CampaignKey, StringComparison.Ordinal) ||
                Campaign.Current == null || Hero.MainHero == null || RewardSystemBehavior.Instance == null ||
                MobileParty.MainParty?.ItemRoster == null)
            {
                message = IllustratorRemainingUiText.GalleryConversionUnavailable;
                return false;
            }
            if (!File.Exists(image.FilePath))
            {
                message = IllustratorRemainingUiText.GalleryConversionSourceMissing;
                return false;
            }
            string description = Clean(introduction, AnimusForgeTextInputSanitizer.MaxCourierLetterChars);
            if (description.Length == 0)
            {
                message = IllustratorRemainingUiText.GalleryConversionDescriptionRequired;
                return false;
            }
            string name = ItemName(image);
            if (!RewardSystemBehavior.TryCreateGeneratedInventoryItemForExternal(name, Identity(image), out ItemObject item, logSource: "gallery_rp"))
            {
                message = IllustratorRemainingUiText.GalleryConversionCreateFailed;
                return false;
            }
            // Store the description before delivery: never hand out an item that NPCs cannot describe.
            if (!RewardSystemBehavior.TrySetGeneratedRpItemIntroductionForExternal(item.StringId, description, out string error))
            {
                message = IllustratorRemainingUiText.GalleryConversionDescriptionFailed(error);
                return false;
            }
            var roster = MobileParty.MainParty.ItemRoster;
            // Old-save repairs can replace ItemObject instances; compare stable IDs as well.
            bool alreadyOwned = false;
            for (int i = 0; i < roster.Count; i++)
            {
                var entry = roster.GetElementCopyAtIndex(i);
                if (entry.Amount > 0 && string.Equals(entry.EquipmentElement.Item?.StringId, item.StringId, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyOwned = true;
                    break;
                }
            }
            if (alreadyOwned)
            {
                message = IllustratorRemainingUiText.GalleryConversionAlreadyOwned(DisplayItemName(image));
                return true;
            }
            int added = RewardSystemBehavior.GenerateKnownInventoryItemToRosterForExternal(roster,
                item.StringId, name, null, item.Id.InternalValue, 1, out _, out _, out _, out _, "gallery_rp");
            message = added > 0
                ? IllustratorRemainingUiText.GalleryConversionDelivered(DisplayItemName(image))
                : IllustratorRemainingUiText.GalleryConversionDeliveryFailed;
            return added > 0;
        }

        internal static string DisplayItemName(CachedIllustrationItem item)
        {
            string title = AnimusForgeTextInputSanitizer.SanitizeSingleLine(item?.Title ?? "", 100).Trim();
            return IllustratorRemainingUiText.GalleryConversionItemName(title.Length == 0 ? IllustratorRemainingUiText.GalleryConversionDefaultTitle : title);
        }

        private static string Clean(string value, int maxChars)
        {
            return AnimusForgeTextInputSanitizer.SanitizeMultiline(value ?? "", maxChars).Trim();
        }
    }
}

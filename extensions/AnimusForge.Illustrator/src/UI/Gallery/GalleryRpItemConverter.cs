using System;
using System.IO;
using System.Text;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
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
            var text = new StringBuilder("这是一幅名为" + ItemName(item) + "的画作。\n");
            string theme = Clean(item?.Theme, 600);
            string action = Clean(item?.ActionSummary, 900);
            if (theme.Length > 0) text.Append("画面主题：").Append(theme).Append('\n');
            if (action.Length > 0) text.Append("画面描绘：").Append(action).Append('\n');
            if (theme.Length == 0 && action.Length == 0)
                text.Append("画面内容尚未注明。\n");
            text.Append("画中的内容属于艺术描绘，不代表其中的事件已经真实发生。");
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
                message = "画卷或当前战役已失效，请重新打开画廊。";
                return false;
            }
            if (!File.Exists(image.FilePath))
            {
                message = "原画文件已不存在，未创建物品。";
                return false;
            }
            string description = Clean(introduction, AnimusForgeTextInputSanitizer.MaxCourierLetterChars);
            if (description.Length == 0)
            {
                message = "请填写画卷介绍，NPC 展示时需要读取这段内容。";
                return false;
            }
            string name = ItemName(image);
            if (!RewardSystemBehavior.TryCreateGeneratedInventoryItemForExternal(name, Identity(image), out ItemObject item, logSource: "gallery_rp"))
            {
                message = "未能创建画卷物品，背包未增加物品。";
                return false;
            }
            // Store the description before delivery: never hand out an item that NPCs cannot describe.
            if (!RewardSystemBehavior.TrySetGeneratedRpItemIntroductionForExternal(item.StringId, description, out string error))
            {
                message = "画卷介绍保存失败：" + error;
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
                message = "背包中已有" + name + "，已更新介绍，未重复添加。";
                return true;
            }
            int added = RewardSystemBehavior.GenerateKnownInventoryItemToRosterForExternal(roster,
                item.StringId, name, null, item.Id.InternalValue, 1, out _, out _, out _, out _, "gallery_rp");
            message = added > 0
                ? "已将" + name + "放入背包；可通过对话中的“展示物品”向 NPC 展示。"
                : "画卷未能放入背包，请检查背包后再试。";
            return added > 0;
        }

        private static string Clean(string value, int maxChars)
        {
            return AnimusForgeTextInputSanitizer.SanitizeMultiline(value ?? "", maxChars).Trim();
        }
    }
}

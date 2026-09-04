using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AFWarStatsTerminal.UI;

internal static class AfWarStatsEncyclopedia
{
    public static Hero ResolveHero(string heroId)
    {
        if (Campaign.Current == null || string.IsNullOrWhiteSpace(heroId))
        {
            return null;
        }

        try
        {
            return Hero.Find(heroId);
        }
        catch (Exception ex)
        {
            Debug.Print("[AFWarStatsTerminal] Failed to resolve hero encyclopedia target '" + heroId + "': " + ex.Message);
            return null;
        }
    }

    public static void OpenKingdom(Kingdom kingdom)
    {
        OpenLink("kingdom", () => kingdom?.EncyclopediaLink);
    }

    public static void OpenHero(Hero hero)
    {
        OpenLink("hero", () => hero?.EncyclopediaLink);
    }

    private static void OpenLink(string targetKind, Func<string> linkFactory)
    {
        if (Campaign.Current?.EncyclopediaManager == null || linkFactory == null)
        {
            return;
        }

        try
        {
            string link = linkFactory();
            if (!string.IsNullOrWhiteSpace(link))
            {
                Campaign.Current.EncyclopediaManager.GoToLink(link);
            }
        }
        catch (Exception ex)
        {
            Debug.Print("[AFWarStatsTerminal] Failed to open " + targetKind + " encyclopedia link: " + ex);
        }
    }
}

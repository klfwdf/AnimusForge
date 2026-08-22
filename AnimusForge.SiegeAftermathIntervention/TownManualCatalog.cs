using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// One localized page in the town-only GCCZ manual.
/// </summary>
public sealed class TownManualPage
{
    public string Id { get; set; }

    public string Title { get; set; }

    public string Body { get; set; }
}

/// <summary>
/// Immutable navigation result consumed by the AF inquiry bridge.
/// </summary>
public sealed class TownManualPageView
{
    internal TownManualPageView(
        int index,
        int totalPages,
        TownManualPage page,
        string windowTitle)
    {
        Index = index;
        TotalPages = totalPages;
        Page = page;
        WindowTitle = windowTitle;
    }

    public int Index { get; }

    public int TotalPages { get; }

    public TownManualPage Page { get; }

    public string WindowTitle { get; }

    public bool HasPrevious => Index > 0;

    public bool HasNext => Index + 1 < TotalPages;
}

/// <summary>
/// Validates localized manual resources and owns page navigation semantics.
/// </summary>
public sealed class TownManualCatalog
{
    private const int MaximumPages = 12;

    public int Version { get; set; }

    public string ManualTitle { get; set; }

    public string WindowTitleTemplate { get; set; }

    public string PreviousButtonText { get; set; }

    public string NextButtonText { get; set; }

    public string CloseButtonText { get; set; }

    public string OpenFailedMessage { get; set; }

    public List<TownManualPage> Pages { get; set; }

    public TownManualPageView GetPage(int requestedIndex)
    {
        TownManualCatalog resolved = Resolve(this);
        int index = Math.Max(0, Math.Min(requestedIndex, resolved.Pages.Count - 1));
        TownManualPage page = resolved.Pages[index];
        string windowTitle = resolved.WindowTitleTemplate
            .Replace("{manual_title}", resolved.ManualTitle)
            .Replace("{page_title}", page.Title)
            .Replace("{page}", (index + 1).ToString(CultureInfo.InvariantCulture))
            .Replace("{total}", resolved.Pages.Count.ToString(CultureInfo.InvariantCulture));
        return new TownManualPageView(index, resolved.Pages.Count, page, windowTitle);
    }

    public static TownManualCatalog Resolve(TownManualCatalog source)
    {
        TownManualCatalog fallback = CreateEnglishFallback();
        if (source == null)
        {
            return fallback;
        }

        var pages = new List<TownManualPage>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (source.Pages != null)
        {
            foreach (TownManualPage page in source.Pages)
            {
                if (page == null || pages.Count >= MaximumPages)
                {
                    continue;
                }

                string id = Clean(page.Id);
                string title = Clean(page.Title);
                string body = Clean(page.Body);
                if (id.Length == 0 || title.Length == 0 || body.Length == 0 || !ids.Add(id))
                {
                    continue;
                }

                pages.Add(new TownManualPage
                {
                    Id = id,
                    Title = title,
                    Body = body,
                });
            }
        }

        if (pages.Count == 0)
        {
            pages = fallback.Pages;
        }

        return new TownManualCatalog
        {
            Version = source.Version > 0 ? source.Version : fallback.Version,
            ManualTitle = Pick(source.ManualTitle, fallback.ManualTitle),
            WindowTitleTemplate = Pick(source.WindowTitleTemplate, fallback.WindowTitleTemplate),
            PreviousButtonText = Pick(source.PreviousButtonText, fallback.PreviousButtonText),
            NextButtonText = Pick(source.NextButtonText, fallback.NextButtonText),
            CloseButtonText = Pick(source.CloseButtonText, fallback.CloseButtonText),
            OpenFailedMessage = Pick(source.OpenFailedMessage, fallback.OpenFailedMessage),
            Pages = pages,
        };
    }

    public static TownManualCatalog CreateEnglishFallback()
    {
        return new TownManualCatalog
        {
            Version = 1,
            ManualTitle = "GCCZ Town Manual",
            WindowTitleTemplate = "{manual_title} | {page_title} ({page}/{total})",
            PreviousButtonText = "Previous",
            NextButtonText = "Next",
            CloseButtonText = "Close",
            OpenFailedMessage = "The GCCZ town manual could not be opened.",
            Pages = new List<TownManualPage>
            {
                new TownManualPage
                {
                    Id = "overview",
                    Title = "Overview",
                    Body = "This manual describes the GCCZ town occupation scene and its guarded AF integration.",
                },
            },
        };
    }

    private static string Pick(string value, string fallback)
    {
        string cleaned = Clean(value);
        return cleaned.Length == 0 ? fallback : cleaned;
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

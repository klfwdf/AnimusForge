using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.TwoDimension;

namespace AnimusForge;

// UI-only entry; declarations still use the existing diplomacy presentation/submission owner.
internal static class KingdomDiplomacyDeclarationEntry
{
    internal const string EntryId = "AnimusForgeDiplomacyDeclarationEntry";
    private static readonly ConditionalWeakTable<KingdomDiplomacyVM, Entry> Entries = new();
    private static bool _patched;

    internal static void EnsurePatched(Harmony harmony)
    {
        if (_patched) return;
        harmony.Patch(AccessTools.Method(typeof(GauntletMovie), nameof(GauntletMovie.Load)),
            postfix: new HarmonyMethod(typeof(KingdomDiplomacyDeclarationEntry), nameof(MovieLoaded)));
        foreach (string name in new[] { "OnSetCurrentDiplomacyItem", "RefreshValues" })
            harmony.Patch(AccessTools.Method(typeof(KingdomDiplomacyVM), name),
                postfix: new HarmonyMethod(typeof(KingdomDiplomacyDeclarationEntry), nameof(PageRefreshed)));
        harmony.Patch(AccessTools.Method(typeof(KingdomManagementVM), "OnFinalize"),
            prefix: new HarmonyMethod(typeof(KingdomDiplomacyDeclarationEntry), nameof(PageFinalized)));
        harmony.Patch(AccessTools.Method(typeof(GauntletKingdomScreen), "OnFrameTick"),
            prefix: new HarmonyMethod(typeof(KingdomDiplomacyDeclarationEntry), nameof(KingdomInputAllowed)));
        _patched = true;
    }

    private static bool KingdomInputAllowed() => !WorldDiplomacyComposePopup.IsOpen;

    private static void MovieLoaded(string movieName, IViewModel datasource, IGauntletMovie __result)
    {
        if (movieName != "KingdomManagement" || datasource is not KingdomManagementVM vm) return;
        try { Inject(__result?.RootWidget, vm.Diplomacy); }
        catch (Exception ex) { Logger.Log("DiplomacyDeclarationEntry", "Injection failed: " + ex.Message); }
    }

    private static void PageRefreshed(KingdomDiplomacyVM __instance)
    {
        if (__instance != null && Entries.TryGetValue(__instance, out Entry entry)) entry.Refresh();
    }

    private static void PageFinalized(KingdomManagementVM __instance)
    {
        if (__instance?.Diplomacy != null && Entries.TryGetValue(__instance.Diplomacy, out Entry entry))
        {
            entry.EndHint();
            Entries.Remove(__instance.Diplomacy);
        }
    }

    internal static void Inject(Widget root, KingdomDiplomacyVM vm)
    {
        if (root == null || vm == null || root.FindChild(EntryId, true) != null) return;
        Widget panel = root.FindChild("DiplomacyPanel", true);
        Widget stats = panel?.FindChild("StatBars", true);
        Widget comparison = stats?.ParentWidget?.ParentWidget?.ParentWidget;
        var parent = comparison?.ParentWidget as ListPanel;
        if (parent == null)
        {
            Logger.Log("DiplomacyDeclarationEntry", "Diplomacy statistics anchor missing; entry not injected.");
            return;
        }

        // Reserve a row above both statistics and war logs, outside their scrolling/clipping areas.
        // The portrait's native war-strategy overlay must not share the declaration's layout slot.
        int insertAt = parent.Children.TakeWhile(child => child != comparison
            && child.FindChild("WarLogs", true) == null).Count();
        var wrapper = new Widget(root.Context)
        {
            Id = EntryId,
            WidthSizePolicy = SizePolicy.Fixed, HeightSizePolicy = SizePolicy.Fixed,
            SuggestedWidth = 135,
            SuggestedHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top,
            MarginRight = comparison.MarginRight, MarginBottom = 4,
            IsVisible = true, IsEnabled = true, DoNotAcceptEvents = false
        };
        var button = new ButtonWidget(root.Context)
        {
            WidthSizePolicy = SizePolicy.StretchToParent, HeightSizePolicy = SizePolicy.StretchToParent,
            DoNotAcceptEvents = false, DoNotPassEventsToChildren = true
        };
        var text = new TextWidget(root.Context)
        {
            WidthSizePolicy = SizePolicy.StretchToParent, HeightSizePolicy = SizePolicy.StretchToParent,
            MarginLeft = 8, MarginRight = 8, DoNotAcceptEvents = true,
            Brush = root.Context.GetBrush("Popup.Button.Text").Clone()
        };
        text.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
        text.Brush.TextVerticalAlignment = TextVerticalAlignment.Center;
        button.AddChild(text);
        wrapper.AddChild(button);
        // A sibling hint remains hoverable when the actual button is disabled, just like native buttons.
        var hint = new HintWidget(root.Context);
        wrapper.AddChild(hint);
        var entry = new Entry(button, text);
        hint.EventFire += entry.HintEvent;
        button.ClickEventHandlers.Add(_ => entry.Click());
        wrapper.OnVisibilityChanged += _ => entry.EndHint();
        Entries.Remove(vm);
        Entries.Add(vm, entry);
        parent.AddChildAtIndex(wrapper, insertAt);
        entry.Refresh();
    }

    private static bool AiEnabled() => DuelSettings.GetSettings()?.EnableWorldDiplomacy == true;
    private static bool PlayerIsRuler() => Campaign.Current != null && Hero.MainHero != null
        && Clan.PlayerClan?.Kingdom?.Leader == Hero.MainHero;

    internal static string DisabledReason(bool aiEnabled, bool isRuler)
    {
        if (!aiEnabled) return Text("AF_UI_DiplomacyEntry_AiDisabled", "AI diplomacy is not enabled.");
        if (!isRuler) return Text("AF_UI_DiplomacyEntry_RulerOnly", "Only the ruler can write a diplomatic declaration.");
        return "";
    }

    private static string Text(string key, string fallback) => new TextObject("{=" + key + "}" + fallback).ToString();

    private sealed class Entry
    {
        private readonly ButtonWidget _button;
        private readonly TextWidget _text;
        private readonly Brush _green;
        private readonly Brush _grey;
        private HintViewModel _hint;

        internal Entry(ButtonWidget button, TextWidget text)
        {
            _button = button;
            _text = text;
            _green = button.Context.GetBrush("Popup.Done.Button");
            _grey = button.Context.GetBrush("Popup.Cancel.Button");
        }

        internal string Refresh()
        {
            string reason = DisabledReason(AiEnabled(), PlayerIsRuler());
            _button.IsEnabled = reason.Length == 0;
            _button.Brush = _button.IsEnabled ? _green : _grey;
            _text.Text = Text("AF_UI_DiplomacyEntry_Title", "Write declaration");
            _text.Brush.FontSize = _text.Text.Length > 10 ? 14 : 16;
            _text.AlphaFactor = _button.IsEnabled ? 1f : 0.5f;
            return reason;
        }

        internal void HintEvent(Widget widget, string eventName, object[] args)
        {
            if (eventName == "HoverBegin")
            {
                EndHint();
                string reason = Refresh();
                _hint = new HintViewModel(new TextObject(reason.Length > 0 ? reason
                    : Text("AF_UI_DiplomacyEntry_Hint", "Write a diplomatic declaration.")));
                _hint.ExecuteBeginHint();
            }
            else if (eventName == "HoverEnd") EndHint();
        }

        internal void EndHint()
        {
            _hint?.ExecuteEndHint();
            _hint = null;
        }

        internal void Click()
        {
            // Recheck live settings and leadership before calling the existing editor.
            if (Refresh().Length != 0) return;
            EndHint();
            DiplomacyPresentationBridge.OpenComposeFromTerminal();
        }
    }
}

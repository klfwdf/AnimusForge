using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

internal static class KingdomFactionUiText
{
	internal static string T(string id, string englishFallback)
		=> new TextObject("{=" + id + "}" + englishFallback).ToString();

	internal static string F(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + englishFallback);
		if (variables != null)
			foreach ((string name, object value) in variables) text.SetTextVariable(name, value?.ToString() ?? string.Empty);
		return text.ToString();
	}
}

// ════════════════════════════════════════════════════════════════════════
//  Faction half of the kingdom screen "内政" tab (layout in KingdomInteriorPanel.cs, Pen frame LEL0F).
//  Kingdom strip (banner, stability, power split) → catalog rows (each faction, or a grievance
//  ledger before any faction forms; crown; undecided) → parchment dossier (demand, progress,
//  figures, paged clans) → identity action bar.
//  Rebuilt when the tab opens, on selection and on civil-war state changes while open; never per frame.
// ════════════════════════════════════════════════════════════════════════

public sealed class KingdomFactionRowVM : ViewModel
{
	private const int Track = KingdomInteriorPanelPatch.GrievanceTrack;
	private readonly CivilWarPanelClan _clan;
	private readonly int _index;
	private readonly int _threshold;
	private readonly bool _decimal;
	private readonly bool _thresholdHigh;

	// threshold > 0 draws the formation tick; decimal shows one decimal place;
	// thresholdHigh also marks values above the tick in red (ledger only).
	internal KingdomFactionRowVM(CivilWarPanelClan clan, int index, int threshold, bool showDecimal, bool thresholdHigh)
	{
		_clan = clan ?? new CivilWarPanelClan();
		_index = index;
		_threshold = threshold;
		_decimal = showDecimal;
		_thresholdHigh = thresholdHigh;
	}

	[DataSourceProperty] public string Name => _clan.IsLeader ? _clan.Name + " ★" : _clan.Name;
	[DataSourceProperty] public string Stance => _clan.Stance;
	[DataSourceProperty] public string Reason => _clan.Reason;
	[DataSourceProperty] public string GrievanceText => _decimal ? _clan.GrievanceValue.ToString("0.0") : _clan.Grievance.ToString();
	[DataSourceProperty] public string PowerText => _clan.PowerPercent + "%";
	[DataSourceProperty] public string RelationText => _clan.RelationText;
	[DataSourceProperty] public bool IsLeader => _clan.IsLeader;
	[DataSourceProperty] public bool IsPlayer => _clan.IsPlayer;
	[DataSourceProperty] public bool IsNegative => _clan.RelationToKing < 0;
	[DataSourceProperty] public bool IsHigh => _clan.GrievanceValue >= 60f || (_thresholdHigh && _threshold > 0 && _clan.GrievanceValue >= _threshold);
	[DataSourceProperty] public int BarWidth => Math.Max(2, Math.Min(Track, (int)Math.Round(Track * _clan.GrievanceValue / 100f)));
	[DataSourceProperty] public Color BarColor => Color.ConvertStringToColor(_clan.GrievanceValue >= 70f ? "#8B3827FF" : _clan.GrievanceValue >= 20f ? "#A0643AFF" : "#8C7A5CFF");
	[DataSourceProperty] public bool HasTick => _threshold > 0 && _threshold < 100;
	[DataSourceProperty] public int TickOffset => Math.Max(0, Math.Min(Track - 2, Track * _threshold / 100 - 1));
	[DataSourceProperty] public Color StripeColor => Color.ConvertStringToColor(_clan.IsPlayer ? "#E8DAB9FF" : _index % 2 == 1 ? "#D5C5A4FF" : "#DCCDAEFF");
}

// One catalog row of the faction group (a faction, the ledger, crown or undecided).
public sealed class KingdomFactionTabVM : ViewModel
{
	private readonly Action<string> _select;
	private readonly string _marker;
	private bool _selected;

	internal KingdomFactionTabVM(string key, string name, string meta, string color, string badge, bool selected, Action<string> select)
	{
		Key = key; Name = name; Meta = meta; _marker = color; Badge = badge ?? ""; _selected = selected; _select = select;
	}

	internal string Key { get; }
	[DataSourceProperty] public string Name { get; }
	[DataSourceProperty] public string Meta { get; }
	[DataSourceProperty] public string Badge { get; }
	[DataSourceProperty] public bool HasBadge => Badge.Length > 0;
	[DataSourceProperty] public Color MarkerColor => Color.ConvertStringToColor(_marker);
	[DataSourceProperty] public bool IsSelected => _selected;
	[DataSourceMethod] public void ExecuteSelect() => _select?.Invoke(Key);

	internal void SetSelected(bool selected)
	{
		if (_selected == selected) return;
		_selected = selected;
		OnPropertyChanged(nameof(IsSelected));
	}
}

public sealed class KingdomFactionStepVM : ViewModel
{
	private readonly CivilWarPanelStep _step;

	internal KingdomFactionStepVM(CivilWarPanelStep step, bool hasLine)
	{
		_step = step ?? new CivilWarPanelStep();
		HasLine = hasLine;
	}

	[DataSourceProperty] public string Name => _step.Name;
	[DataSourceProperty] public string Note => _step.Note;
	[DataSourceProperty] public bool IsDone => _step.State == 1;
	[DataSourceProperty] public bool IsCurrent => _step.State == 2;
	[DataSourceProperty] public bool IsNext => _step.State == 0;
	[DataSourceProperty] public bool HasLine { get; }
	[DataSourceProperty] public Color DotColor => Color.ConvertStringToColor(IsCurrent ? "#8B3827FF" : "#7A6248FF");
	[DataSourceProperty] public Color LineColor => Color.ConvertStringToColor(IsDone ? "#7A6248FF" : "#C4B18DFF");
}

public sealed class KingdomFactionConditionVM : ViewModel
{
	private readonly CivilWarPanelCondition _condition;
	internal KingdomFactionConditionVM(CivilWarPanelCondition condition) { _condition = condition ?? new CivilWarPanelCondition(); }
	[DataSourceProperty] public string Name => _condition.Name;
	[DataSourceProperty] public string Value => _condition.Value;
	[DataSourceProperty] public bool IsMet => _condition.Met;
	[DataSourceProperty] public Color MarkColor => Color.ConvertStringToColor(_condition.Met ? "#6D6541FF" : "#8B3827FF");
}

public sealed class KingdomFactionSegmentVM : ViewModel
{
	private readonly string _color;
	internal KingdomFactionSegmentVM(int width, string color, string name, int percent) { Width = width; _color = color; Name = name; PercentText = percent + "%"; }
	[DataSourceProperty] public int Width { get; }
	[DataSourceProperty] public Color SegmentColor => Color.ConvertStringToColor(_color);
	[DataSourceProperty] public string Name { get; }
	[DataSourceProperty] public string PercentText { get; }
}

// One identity action button. Dim buttons stay clickable so the full reason is shown.
public sealed class KingdomFactionActionVM : ViewModel
{
	private readonly Action _click;
	private readonly string _tint;

	internal KingdomFactionActionVM(string label, string sub, bool dim, string tint, Action click)
	{
		Label = label; Sub = sub; IsDim = dim; _tint = tint; _click = click;
	}

	[DataSourceProperty] public string Label { get; }
	[DataSourceProperty] public string Sub { get; }
	[DataSourceProperty] public bool IsDim { get; }
	[DataSourceProperty] public Color TintColor => Color.ConvertStringToColor(IsDim ? "#00000000" : _tint);
	[DataSourceMethod] public void ExecuteClick() => _click?.Invoke();
}

public sealed partial class KingdomFactionPanelVM : ViewModel
{
	private const string CrownKey = "crown", MiddleKey = "middle", LedgerKey = "ledger";
	private const string CrownColor = "#B8963FFF", MiddleColor = "#8F8270FF", LedgerColor = "#6A5A40FF";
	private CivilWarPanelKingdom _panel = new CivilWarPanelKingdom();
	private CivilWarPanelFaction _faction;
	// "" while an agenda (or nothing) is selected in the shared catalog.
	private string _key = "";
	// False while the agenda half shows another kingdom; the faction group belongs to the player's kingdom.
	private bool _catalogShown = true;
	private int _page;
	private int _pageSize = KingdomInteriorPanelPatch.FactionRows;
	private int _rowThreshold;
	private bool _rowDecimal;
	private bool _rowThresholdHigh;
	private List<CivilWarPanelClan> _source = new List<CivilWarPanelClan>();
	private string _kingdomBannerKey = "", _sealBannerKey = "";

	[DataSourceProperty] public bool IsAvailable => _panel.Available;
	[DataSourceProperty] public bool IsCatalogVisible => _panel.Available && _catalogShown;
	[DataSourceProperty] public bool HasEntry => IsCatalogVisible && _key.Length > 0;
	[DataSourceProperty] public string CatalogCountText => _panel.Factions.Count > 0
		? KingdomFactionUiText.F("AF_DIP_UI_Faction_CatalogCount", "{COUNT} factions  ·  threshold {THRESHOLD}", ("COUNT", _panel.Factions.Count), ("THRESHOLD", _panel.Threshold))
		: KingdomFactionUiText.F("AF_DIP_UI_Faction_CatalogEmpty", "No factions  ·  threshold {THRESHOLD}", ("THRESHOLD", _panel.Threshold));
	[DataSourceProperty] public string KingdomName => _panel.Name;
	[DataSourceProperty] public string IdentityText => _panel.Identity;
	[DataSourceProperty] public string StabilityValue => _panel.Stability.ToString();
	[DataSourceProperty] public string StabilityTierText => "/ 100  ·  " + _panel.StabilityTier;
	[DataSourceProperty] public int StabilityBarWidth => Math.Max(2, KingdomInteriorPanelPatch.StabilityTrack * Math.Max(0, Math.Min(100, _panel.Stability)) / 100);
	[DataSourceProperty] public Color StabilityColor => Color.ConvertStringToColor(_panel.Stability >= 60 ? "#8FA85AFF" : _panel.Stability >= 35 ? "#C8963CFF" : "#C0492FFF");
	[DataSourceProperty] public string PowerStageText => _panel.StageText;
	[DataSourceProperty] public MBBindingList<KingdomFactionSegmentVM> PowerSegments { get; } = new MBBindingList<KingdomFactionSegmentVM>();
	[DataSourceProperty] public ImageIdentifierVM KingdomBanner { get; private set; }
	[DataSourceProperty] public MBBindingList<KingdomFactionTabVM> LeftTabs { get; } = new MBBindingList<KingdomFactionTabVM>();
	[DataSourceProperty] public MBBindingList<KingdomFactionTabVM> RightTabs { get; } = new MBBindingList<KingdomFactionTabVM>();

	[DataSourceProperty] public bool IsFactionTab => _faction != null;
	[DataSourceProperty] public bool IsLedgerTab => _key == LedgerKey;
	[DataSourceProperty] public bool HasBand => IsFactionTab || IsLedgerTab;
	[DataSourceProperty] public string TagText { get; private set; } = "";
	[DataSourceProperty] public Color TagColor { get; private set; }
	[DataSourceProperty] public string HeaderMeta { get; private set; } = "";
	[DataSourceProperty] public string HeaderTitle { get; private set; } = "";
	[DataSourceProperty] public string HeaderNote { get; private set; } = "";
	[DataSourceProperty] public bool HasBlock { get; private set; }
	[DataSourceProperty] public bool BlockUrgent { get; private set; }
	[DataSourceProperty] public string BlockLabel { get; private set; } = "";
	[DataSourceProperty] public string BlockValue { get; private set; } = "";
	[DataSourceProperty] public string BlockUnit { get; private set; } = "";
	[DataSourceProperty] public string BlockNote { get; private set; } = "";
	[DataSourceProperty] public MBBindingList<KingdomFactionStepVM> Steps { get; } = new MBBindingList<KingdomFactionStepVM>();
	[DataSourceProperty] public string FigureGrievance => _faction?.Grievance.ToString() ?? "";
	[DataSourceProperty] public string FigureGrievanceNote => _faction == null ? "" : KingdomFactionUiText.F(
		_faction.Grievance >= _panel.Threshold ? "AF_DIP_UI_Faction_AboveFormationThreshold" : "AF_DIP_UI_Faction_BelowFormationThreshold",
		_faction.Grievance >= _panel.Threshold ? "Above formation threshold {THRESHOLD}" : "Below formation threshold {THRESHOLD}",
		("THRESHOLD", _panel.Threshold));
	[DataSourceProperty] public string FigurePower => _faction == null ? "" : _faction.PowerPercent + "%";
	[DataSourceProperty] public string FigurePowerUnit => _faction == null ? "" : KingdomFactionUiText.F("AF_DIP_UI_Faction_PowerUnit", "Crown {CROWN}%  ·  {FORTIFICATIONS} fiefs", ("CROWN", _panel.CrownPowerPercent), ("FORTIFICATIONS", _faction.Fortifications));
	[DataSourceProperty] public string FigureRefusals => _faction?.Refusals.ToString() ?? "";
	[DataSourceProperty] public string FigureRefusalUnit => _faction?.RefusalUnit ?? "";
	// The chronicle box of the faction figures is a quarter of the dossier wide.
	[DataSourceProperty] public string FigureSource1 => _panel.Chronicle.Count > 0 ? Clip(_panel.Chronicle[0], 18) : KingdomFactionUiText.T("AF_DIP_UI_Faction_NoRecentChronicle", "No recent chronicle");
	[DataSourceProperty] public string FigureSource2 => _panel.Chronicle.Count > 1 ? Clip(_panel.Chronicle[1], 18) : "";
	[DataSourceProperty] public MBBindingList<KingdomFactionConditionVM> Conditions { get; } = new MBBindingList<KingdomFactionConditionVM>();
	[DataSourceProperty] public string SourceLine1 => _panel.Chronicle.Count > 0 ? Clip(_panel.Chronicle[0], 40) : KingdomFactionUiText.T("AF_DIP_UI_Faction_NoRecentChronicle", "No recent chronicle");
	[DataSourceProperty] public string SourceLine2 => _panel.Chronicle.Count > 1 ? Clip(_panel.Chronicle[1], 40) : "";
	[DataSourceProperty] public string TableTitle { get; private set; } = "";
	[DataSourceProperty] public string GrievanceTitle => _rowThreshold > 0
		? KingdomFactionUiText.F("AF_DIP_UI_Faction_GrievanceThreshold", "Clan grievance  ·  threshold {THRESHOLD}", ("THRESHOLD", _rowThreshold))
		: KingdomFactionUiText.T("AF_DIP_UI_Faction_Grievance", "Clan grievance");
	[DataSourceProperty] public MBBindingList<KingdomFactionRowVM> Rows { get; } = new MBBindingList<KingdomFactionRowVM>();
	[DataSourceProperty] public string FooterText { get; private set; } = "";
	[DataSourceProperty] public string LegendText => _rowThreshold > 0 ? KingdomFactionUiText.T("AF_DIP_UI_Faction_FormationTickLegend", "Red tick marks the formation threshold") : "";
	[DataSourceProperty] public bool HasPages => PageCount > 1;
	[DataSourceProperty] public bool HasPrevPage => _page > 0;
	[DataSourceProperty] public bool HasNextPage => _page < PageCount - 1;
	[DataSourceProperty] public string PageText => (_page + 1) + " / " + PageCount;
	[DataSourceProperty] public string ActionTitle { get; private set; } = "";
	[DataSourceProperty] public string ActionHint => _panel.ActionHint;
	[DataSourceProperty] public ImageIdentifierVM SealBanner { get; private set; }
	[DataSourceProperty] public MBBindingList<KingdomFactionActionVM> Actions { get; } = new MBBindingList<KingdomFactionActionVM>();

	private int PageCount => Math.Max(1, (_source.Count + _pageSize - 1) / _pageSize);

	private static readonly string[] Bound =
	{
		nameof(IsAvailable), nameof(IsCatalogVisible), nameof(HasEntry), nameof(CatalogCountText), nameof(KingdomName), nameof(IdentityText),
		nameof(StabilityValue), nameof(StabilityTierText), nameof(StabilityBarWidth), nameof(StabilityColor), nameof(PowerStageText),
		nameof(IsFactionTab), nameof(IsLedgerTab), nameof(HasBand), nameof(TagText), nameof(TagColor), nameof(HeaderMeta), nameof(HeaderTitle),
		nameof(HeaderNote), nameof(HasBlock), nameof(BlockUrgent), nameof(BlockLabel), nameof(BlockValue), nameof(BlockUnit), nameof(BlockNote),
		nameof(FigureGrievance), nameof(FigureGrievanceNote), nameof(FigurePower), nameof(FigurePowerUnit), nameof(FigureRefusals),
		nameof(FigureRefusalUnit), nameof(FigureSource1), nameof(FigureSource2), nameof(SourceLine1), nameof(SourceLine2), nameof(TableTitle),
		nameof(GrievanceTitle), nameof(FooterText), nameof(LegendText), nameof(HasPages), nameof(HasPrevPage), nameof(HasNextPage),
		nameof(PageText), nameof(ActionTitle), nameof(ActionHint)
	};

	// Called when the tab opens and on civil-war state changes while it is open.
	internal void Refresh()
	{
		try
		{
			_panel = TeamModuleServices.CivilWar.GetPlayerKingdomPanel() ?? new CivilWarPanelKingdom { EmptyText = KingdomFactionUiText.T("AF_DIP_UI_Faction_DataUnavailable", "Faction data unavailable") };
		}
		catch (Exception ex)
		{
			Logger.Log("CivilWar", "[KingdomFactionTab] refresh failed: " + ex.Message);
			_panel = new CivilWarPanelKingdom { EmptyText = KingdomFactionUiText.T("AF_DIP_UI_Faction_DataReadFailed", "Failed to read faction data") };
		}
		RefreshBanners();
		RebuildSegments();
		RebuildTabs();
		RebuildView(false);
		RebuildActions();
		Notify();
	}

	private void Notify()
	{
		foreach (string name in Bound) OnPropertyChanged(name);
	}

	// Crown, each faction, undecided; widths fill PowerTrack exactly.
	private void RebuildSegments()
	{
		PowerSegments.Clear();
		var parts = new List<Tuple<string, string, int>> { Tuple.Create(KingdomFactionUiText.T("AF_DIP_UI_Faction_Crown", "Crown faction"), CrownColor, _panel.CrownPowerPercent) };
		foreach (CivilWarPanelFaction f in _panel.Factions) parts.Add(Tuple.Create(f.Tag, f.Color, f.PowerPercent));
		parts.Add(Tuple.Create(KingdomFactionUiText.T("AF_DIP_UI_Faction_Undecided", "Undecided"), "#5E5546FF", _panel.MiddlePowerPercent));
		parts.RemoveAll(x => x.Item3 <= 0);
		if (parts.Count == 0) return;
		int track = KingdomInteriorPanelPatch.PowerTrack - KingdomInteriorPanelPatch.PowerGap * parts.Count;
		int sum = Math.Max(1, parts.Sum(x => x.Item3));
		int[] widths = parts.Select(x => Math.Max(4, track * x.Item3 / sum)).ToArray();
		int largest = Array.IndexOf(widths, widths.Max());
		widths[largest] = Math.Max(4, widths[largest] + track - widths.Sum());
		for (int i = 0; i < parts.Count; i++) PowerSegments.Add(new KingdomFactionSegmentVM(widths[i], parts[i].Item2, parts[i].Item1, parts[i].Item3));
	}

	// Kingdom banner in the strip; the action-bar seal is the kingdom (king / crown) or the player's clan.
	private void RefreshBanners()
	{
		try
		{
			TaleWorlds.CampaignSystem.Kingdom kingdom = CivilWarWorld.FindKingdom(_panel.KingdomId);
			TaleWorlds.CampaignSystem.Clan player = TaleWorlds.CampaignSystem.Clan.PlayerClan;
			bool crownSeal = _panel.Role == CivilWarPanelRole.King || _panel.Role == CivilWarPanelRole.Crown;
			KingdomBanner = BannerFor(kingdom?.StringId ?? "", kingdom?.Banner ?? kingdom?.RulingClan?.Banner, ref _kingdomBannerKey, KingdomBanner);
			SealBanner = crownSeal ? BannerFor("k:" + (kingdom?.StringId ?? ""), kingdom?.Banner ?? kingdom?.RulingClan?.Banner, ref _sealBannerKey, SealBanner)
				: BannerFor("c:" + (player?.StringId ?? ""), player?.Banner, ref _sealBannerKey, SealBanner);
		}
		catch (Exception ex)
		{
			Logger.Log("CivilWar", "[KingdomFactionTab] banner failed: " + ex.Message);
		}
		OnPropertyChanged(nameof(KingdomBanner));
		OnPropertyChanged(nameof(SealBanner));
	}

	private static ImageIdentifierVM BannerFor(string key, Banner banner, ref string cachedKey, ImageIdentifierVM current)
	{
		if (current != null && cachedKey == key) return current;
		cachedKey = key;
		return banner == null ? null : new BannerImageIdentifierVM(banner, false);
	}

	private string DefaultKey() => _panel.Factions.Count == 0 ? LedgerKey : "f:" + (_panel.Factions.FirstOrDefault(x => x.Id == _panel.PlayerFactionId) ?? _panel.Factions[0]).Id;

	private void RebuildTabs()
	{
		LeftTabs.Clear();
		RightTabs.Clear();
		if (!IsCatalogVisible) { _key = ""; return; }
		bool valid = _key == CrownKey || _key == MiddleKey || (_key == LedgerKey && _panel.Factions.Count == 0) || _panel.Factions.Any(x => "f:" + x.Id == _key);
		// A faction-side selection that disappeared (dissolved, formed) moves to the default entry.
		if (_key.Length > 0 && !valid) _key = DefaultKey();
		foreach (CivilWarPanelFaction f in _panel.Factions)
		{
			string mine = f.IsPlayerLeader ? KingdomFactionUiText.T("AF_DIP_UI_Faction_PlayerLeads", "You lead · ") : f.IsPlayerFaction ? KingdomFactionUiText.T("AF_DIP_UI_Faction_PlayerFaction", "Your faction · ") : "";
			string meta = mine + KingdomFactionUiText.F("AF_DIP_UI_Faction_TabMeta", "{COUNT} clans · grievance {GRIEVANCE}", ("COUNT", f.MemberCount), ("GRIEVANCE", f.Grievance));
			LeftTabs.Add(new KingdomFactionTabVM("f:" + f.Id, Clip(f.ShortName, 12), meta, f.Color, Badge(f), _key == "f:" + f.Id, SelectTab));
		}
		if (_panel.Factions.Count == 0)
			LeftTabs.Add(new KingdomFactionTabVM(LedgerKey, KingdomFactionUiText.T("AF_DIP_UI_Faction_GrievanceLedger", "Noble grievance ledger"), KingdomFactionUiText.F("AF_DIP_UI_Faction_LedgerMeta", "{COUNT} clans · highest {GRIEVANCE}", ("COUNT", _panel.CrownCount + _panel.MiddleCount), ("GRIEVANCE", _panel.TopGrievance.ToString("0.0"))), LedgerColor, "", _key == LedgerKey, SelectTab));
		bool inCrown = _panel.Role == CivilWarPanelRole.Crown, inMiddle = _panel.Middle.Any(x => x.IsPlayer);
		RightTabs.Add(new KingdomFactionTabVM(CrownKey, KingdomFactionUiText.T("AF_DIP_UI_Faction_Crown", "Crown faction"), KingdomFactionUiText.F("AF_DIP_UI_Faction_PowerMeta", "{COUNT} clans · power {POWER}%{PLAYER}", ("COUNT", _panel.CrownCount), ("POWER", _panel.CrownPowerPercent), ("PLAYER", inCrown ? KingdomFactionUiText.T("AF_DIP_UI_Faction_IncludesYou", " · includes you") : "")), CrownColor, "", _key == CrownKey, SelectTab));
		RightTabs.Add(new KingdomFactionTabVM(MiddleKey, KingdomFactionUiText.T("AF_DIP_UI_Faction_Undecided", "Undecided"), KingdomFactionUiText.F("AF_DIP_UI_Faction_PowerMeta", "{COUNT} clans · power {POWER}%{PLAYER}", ("COUNT", _panel.MiddleCount), ("POWER", _panel.MiddlePowerPercent), ("PLAYER", inMiddle ? KingdomFactionUiText.T("AF_DIP_UI_Faction_IncludesYou", " · includes you") : "")), MiddleColor, "", _key == MiddleKey, SelectTab));
	}

	// Only urgent deadlines (ultimatum, royal reply, war) earn a catalog badge.
	private static string Badge(CivilWarPanelFaction f)
	{
		if (!f.DeadlineUrgent || string.IsNullOrEmpty(f.DeadlineLabel)) return "";
		// Driven by the panel's stable flag, not by comparing the localized label, so this keeps working
		// in any language.
		string label = f.DeadlineIsUltimatum ? KingdomFactionUiText.T("AF_DIP_UI_Faction_Ultimatum", "Ultimatum") : f.DeadlineLabel;
		return Clip(label + " " + f.DeadlineValue + f.DeadlineUnit, 9);
	}

	private void SelectTab(string key)
	{
		if (!_open || !IsCatalogVisible || key == _key) return;
		_key = key;
		foreach (KingdomFactionTabVM tab in LeftTabs) tab.SetSelected(tab.Key == key);
		foreach (KingdomFactionTabVM tab in RightTabs) tab.SetSelected(tab.Key == key);
		KingdomAgendaTabState.ClearItemSelection();
		RebuildView(true);
		RebuildActions();
		Notify();
	}

	// The shared catalog keeps one selection: an agenda pick clears the faction side.
	internal void Deselect()
	{
		if (_key.Length == 0) return;
		_key = "";
		foreach (KingdomFactionTabVM tab in LeftTabs) tab.SetSelected(false);
		foreach (KingdomFactionTabVM tab in RightTabs) tab.SetSelected(false);
		RebuildView(true);
		RebuildActions();
		Notify();
	}

	internal bool SelectDefault()
	{
		if (!IsCatalogVisible) return false;
		if (_key.Length == 0) SelectTab(DefaultKey());
		return HasEntry;
	}

	internal void SetCatalogShown(bool shown)
	{
		if (_catalogShown == shown) return;
		_catalogShown = shown;
		if (!shown) Deselect();
		else if (_open) Refresh();
		OnPropertyChanged(nameof(IsCatalogVisible));
		OnPropertyChanged(nameof(HasEntry));
	}

	private void RebuildView(bool resetPage)
	{
		if (resetPage) _page = 0;
		_faction = _key.StartsWith("f:", StringComparison.Ordinal) ? _panel.Factions.FirstOrDefault(x => "f:" + x.Id == _key) : null;
		Steps.Clear();
		Conditions.Clear();
		HasBlock = false; BlockUrgent = false; BlockLabel = BlockValue = BlockUnit = BlockNote = "";
		_rowThreshold = 0; _rowDecimal = false; _rowThresholdHigh = false; _pageSize = KingdomInteriorPanelPatch.FactionRows;
		if (!HasEntry) { _source = new List<CivilWarPanelClan>(); TagText = HeaderMeta = HeaderTitle = HeaderNote = TableTitle = FooterText = ""; }
		else if (_faction != null) DescribeFaction(_faction);
		else if (_key == LedgerKey) DescribeLedger();
		else DescribeSide(_key == CrownKey);
		RebuildRows();
	}

	private void DescribeFaction(CivilWarPanelFaction f)
	{
		TagText = f.DemandTag; TagColor = Color.ConvertStringToColor(f.Color);
		HeaderMeta = f.LeaderLine; HeaderTitle = f.DemandTitle; HeaderNote = f.Note;
		HasBlock = true; BlockUrgent = f.DeadlineUrgent; BlockLabel = f.DeadlineLabel; BlockValue = f.DeadlineValue; BlockUnit = f.DeadlineUnit; BlockNote = f.DeadlineNote;
		for (int i = 0; i < f.Steps.Count; i++) Steps.Add(new KingdomFactionStepVM(f.Steps[i], i < f.Steps.Count - 1));
		_source = f.Members;
		_rowThreshold = _panel.Threshold;
		TableTitle = KingdomFactionUiText.T("AF_DIP_UI_Faction_SupportingClans", "Supporting clans");
		FooterText = _panel.Chronicle.Count > 0 ? KingdomFactionUiText.T("AF_DIP_UI_Faction_LatestPrefix", "Latest  ·  ") + Clip(_panel.Chronicle[0], 48) : f.Stage;
	}

	// No faction yet: who is closest to the formation threshold, and why it has not formed.
	private void DescribeLedger()
	{
		TagText = KingdomFactionUiText.T("AF_DIP_UI_Faction_NoFaction", "No faction formed"); TagColor = Color.ConvertStringToColor(LedgerColor);
		HeaderMeta = KingdomFactionUiText.F("AF_DIP_UI_Faction_StabilityMeta", "Stability {TIER}  ·  grievance and stability are calculated separately", ("TIER", _panel.StabilityTier));
		// Driven by the panel's stable flag, not by comparing the localized StageText, so the dedicated
		// title survives any language.
		HeaderTitle = _panel.NoFactionFormed ? KingdomFactionUiText.T("AF_DIP_UI_Faction_NoOppositionFormed", "No opposition faction has formed in the kingdom") : _panel.StageText;
		float gap = _panel.Threshold - _panel.TopGrievance;
		HeaderNote = string.IsNullOrEmpty(_panel.TopGrievanceClan) ? KingdomFactionUiText.T("AF_DIP_UI_Faction_NoEligibleVassal", "No formal vassal clan can form a faction.")
			: gap > 0 ? KingdomFactionUiText.F("AF_DIP_UI_Faction_FormationPending", "Highest clan grievance is {GRIEVANCE}; formation threshold is {THRESHOLD}. A probability check runs at most once every seven days.", ("GRIEVANCE", _panel.TopGrievance.ToString("0.0")), ("THRESHOLD", _panel.Threshold))
			: KingdomFactionUiText.F("AF_DIP_UI_Faction_FormationReady", "Highest clan grievance is {GRIEVANCE}; formation threshold {THRESHOLD} reached. A probability check runs every seven days.", ("GRIEVANCE", _panel.TopGrievance.ToString("0.0")), ("THRESHOLD", _panel.Threshold));
		HasBlock = true;
		BlockLabel = gap > 0 ? KingdomFactionUiText.T("AF_DIP_UI_Faction_FormationGap", "To formation threshold") : KingdomFactionUiText.T("AF_DIP_UI_Faction_FormationThreshold", "Formation threshold");
		BlockValue = gap > 0 ? gap.ToString("0.0") : KingdomFactionUiText.T("AF_DIP_UI_Faction_Reached", "Reached");
		BlockUnit = gap > 0 ? KingdomFactionUiText.T("AF_DIP_UI_Faction_Points", "points") : "";
		BlockNote = string.IsNullOrEmpty(_panel.TopGrievanceClan) ? "" : _panel.TopGrievance.ToString("0.0") + " / " + _panel.Threshold + "  ·  " + _panel.TopGrievanceClan;
		foreach (CivilWarPanelCondition c in _panel.Conditions) Conditions.Add(new KingdomFactionConditionVM(c));
		_source = _panel.Crown.Concat(_panel.Middle).OrderByDescending(x => x.GrievanceValue).ToList();
		_rowThreshold = _panel.Threshold; _rowDecimal = true; _rowThresholdHigh = true;
		_pageSize = KingdomInteriorPanelPatch.LedgerRows;
		TableTitle = KingdomFactionUiText.T("AF_DIP_UI_Faction_NobleClans", "Noble clans");
		FooterText = KingdomFactionUiText.T("AF_DIP_UI_Faction_SortGrievance", "Sorted by clan grievance, highest first");
	}

	private void DescribeSide(bool crown)
	{
		TagText = crown ? KingdomFactionUiText.T("AF_DIP_UI_Faction_Crown", "Crown faction") : KingdomFactionUiText.T("AF_DIP_UI_Faction_Undecided", "Undecided"); TagColor = Color.ConvertStringToColor(crown ? "#8A6E2EFF" : "#6A6052FF");
		int count = crown ? _panel.CrownCount : _panel.MiddleCount;
		HeaderMeta = KingdomFactionUiText.F("AF_DIP_UI_Faction_MilitaryMeta", "{COUNT} clans  ·  military power {POWER}%", ("COUNT", count), ("POWER", crown ? _panel.CrownPowerPercent : _panel.MiddlePowerPercent));
		HeaderTitle = crown ? KingdomFactionUiText.T("AF_DIP_UI_Faction_CrownRespond", "Crown faction · stand with the king against factions") : KingdomFactionUiText.T("AF_DIP_UI_Faction_UndecidedClans", "Undecided clans");
		HeaderNote = crown ? KingdomFactionUiText.F("AF_DIP_UI_Faction_CrownNote", "If a faction rebels, crown clans fight with the king; a clan withdraws support when grievance reaches {THRESHOLD}.", ("THRESHOLD", _panel.Threshold))
			: KingdomFactionUiText.T("AF_DIP_UI_Faction_UndecidedNote", "Not yet in a faction; may join the crown or any pre-war faction.");
		_source = crown ? _panel.Crown : _panel.Middle;
		_pageSize = KingdomInteriorPanelPatch.SideRows;
		TableTitle = crown ? KingdomFactionUiText.T("AF_DIP_UI_Faction_CrownClans", "Crown faction clans") : KingdomFactionUiText.T("AF_DIP_UI_Faction_UndecidedClans", "Undecided clans");
		bool mine = _source.Any(x => x.IsPlayer);
		FooterText = (crown ? KingdomFactionUiText.T("AF_DIP_UI_Faction_KingFirst", "King first, ") : "") + (mine ? KingdomFactionUiText.T("AF_DIP_UI_Faction_PlayerClanFirst", "your clan first, ") : "") + KingdomFactionUiText.T("AF_DIP_UI_Faction_OthersByGrievance", "others by grievance, highest first");
	}

	private void RebuildRows()
	{
		Rows.Clear();
		_page = Math.Max(0, Math.Min(_page, PageCount - 1));
		int start = _page * _pageSize;
		for (int i = start; i < Math.Min(_source.Count, start + _pageSize); i++) Rows.Add(new KingdomFactionRowVM(_source[i], i - start, _rowThreshold, _rowDecimal, _rowThresholdHigh));
	}

	[DataSourceMethod] public void ExecutePrevPage() => TurnPage(-1);
	[DataSourceMethod] public void ExecuteNextPage() => TurnPage(1);

	private void TurnPage(int delta)
	{
		int next = Math.Max(0, Math.Min(PageCount - 1, _page + delta));
		if (!_open || next == _page) return;
		_page = next;
		RebuildRows();
		OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage)); OnPropertyChanged(nameof(PageText));
	}

	private static string Clip(string text, int max) => string.IsNullOrEmpty(text) || text.Length <= max ? text ?? "" : text.Substring(0, max);
}

// Owns the faction half of the "内政" tab on the kingdom screen. The tab itself (button, visibility,
// selection hand-off with the agenda half) belongs to KingdomAgendaVMMixin in VoteDealBehavior.cs.
[ViewModelMixin("RefreshValues", true)]
internal sealed class KingdomFactionVMMixin : BaseViewModelMixin<KingdomManagementVM>
{
	[DataSourceProperty]
	public KingdomFactionPanelVM CivilWarFactions { get; set; }

	public KingdomFactionVMMixin(KingdomManagementVM vm) : base(vm)
	{
		CivilWarFactions = new KingdomFactionPanelVM();
		KingdomFactionTabState.Register(this);
	}
}

// Kingdom management is a single active screen, so a weak shortcut to the latest mixin is enough.
internal static class KingdomFactionTabState
{
	private static WeakReference<KingdomFactionVMMixin> _current;

	internal static void Register(KingdomFactionVMMixin mixin)
	{
		_current = new WeakReference<KingdomFactionVMMixin>(mixin);
	}

	private static KingdomFactionPanelVM Panel => _current != null && _current.TryGetTarget(out KingdomFactionVMMixin mixin) ? mixin.CivilWarFactions : null;

	// The "内政" tab became visible: listen to civil-war changes and rebuild once.
	internal static void Show()
	{
		KingdomFactionPanelVM panel = Panel;
		if (panel == null) return;
		panel.Open();
		panel.Refresh();
	}

	// The "内政" tab was hidden (another tab, screen closed).
	internal static void Clear() => Panel?.Close();

	internal static void Deselect() => Panel?.Deselect();

	internal static bool SelectDefault() => Panel?.SelectDefault() == true;

	internal static bool HasSelection => Panel?.HasEntry == true;

	internal static void SetCatalogShown(bool shown) => Panel?.SetCatalogShown(shown);
}

[HarmonyPatch(typeof(KingdomManagementVM), nameof(KingdomManagementVM.OnFinalize))]
internal static class KingdomFactionFinalizePatch
{
	private static void Prefix() => KingdomFactionTabState.Clear();
}

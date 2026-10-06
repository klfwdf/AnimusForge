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
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

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
	[DataSourceProperty] public string CatalogCountText => (_panel.Factions.Count > 0 ? _panel.Factions.Count + " 派" : "尚无派系") + "  ·  门槛 " + _panel.Threshold;
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
	[DataSourceProperty] public string FigureGrievanceNote => _faction == null ? "" : (_faction.Grievance >= _panel.Threshold ? "高于" : "低于") + "成派门槛 " + _panel.Threshold;
	[DataSourceProperty] public string FigurePower => _faction == null ? "" : _faction.PowerPercent + "%";
	[DataSourceProperty] public string FigurePowerUnit => _faction == null ? "" : "王室 " + _panel.CrownPowerPercent + "%  ·  " + _faction.Fortifications + " 城";
	[DataSourceProperty] public string FigureRefusals => _faction?.Refusals.ToString() ?? "";
	[DataSourceProperty] public string FigureRefusalUnit => _faction?.RefusalUnit ?? "";
	// The chronicle box of the faction figures is a quarter of the dossier wide.
	[DataSourceProperty] public string FigureSource1 => _panel.Chronicle.Count > 0 ? Clip(_panel.Chronicle[0], 18) : "暂无近期纪事";
	[DataSourceProperty] public string FigureSource2 => _panel.Chronicle.Count > 1 ? Clip(_panel.Chronicle[1], 18) : "";
	[DataSourceProperty] public MBBindingList<KingdomFactionConditionVM> Conditions { get; } = new MBBindingList<KingdomFactionConditionVM>();
	[DataSourceProperty] public string SourceLine1 => _panel.Chronicle.Count > 0 ? Clip(_panel.Chronicle[0], 40) : "暂无近期纪事";
	[DataSourceProperty] public string SourceLine2 => _panel.Chronicle.Count > 1 ? Clip(_panel.Chronicle[1], 40) : "";
	[DataSourceProperty] public string TableTitle { get; private set; } = "";
	[DataSourceProperty] public string GrievanceTitle => _rowThreshold > 0 ? "家族不满  ·  门槛 " + _rowThreshold : "家族不满";
	[DataSourceProperty] public MBBindingList<KingdomFactionRowVM> Rows { get; } = new MBBindingList<KingdomFactionRowVM>();
	[DataSourceProperty] public string FooterText { get; private set; } = "";
	[DataSourceProperty] public string LegendText => _rowThreshold > 0 ? "红色刻度为成派门槛" : "";
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
			_panel = TeamModuleServices.CivilWar.GetPlayerKingdomPanel() ?? new CivilWarPanelKingdom { EmptyText = "派系数据不可用" };
		}
		catch (Exception ex)
		{
			Logger.Log("CivilWar", "[KingdomFactionTab] refresh failed: " + ex.Message);
			_panel = new CivilWarPanelKingdom { EmptyText = "派系数据读取失败" };
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
		var parts = new List<Tuple<string, string, int>> { Tuple.Create("王室阵营", CrownColor, _panel.CrownPowerPercent) };
		foreach (CivilWarPanelFaction f in _panel.Factions) parts.Add(Tuple.Create(f.Tag, f.Color, f.PowerPercent));
		parts.Add(Tuple.Create("未表态", "#5E5546FF", _panel.MiddlePowerPercent));
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
			string mine = f.IsPlayerLeader ? "你领导 · " : f.IsPlayerFaction ? "你的派系 · " : "";
			string meta = mine + f.MemberCount + " 家 · 不满 " + f.Grievance;
			LeftTabs.Add(new KingdomFactionTabVM("f:" + f.Id, Clip(f.ShortName, 12), meta, f.Color, Badge(f), _key == "f:" + f.Id, SelectTab));
		}
		if (_panel.Factions.Count == 0)
			LeftTabs.Add(new KingdomFactionTabVM(LedgerKey, "贵族不满账册", (_panel.CrownCount + _panel.MiddleCount) + " 家 · 最高 " + _panel.TopGrievance.ToString("0.0"), LedgerColor, "", _key == LedgerKey, SelectTab));
		bool inCrown = _panel.Role == CivilWarPanelRole.Crown, inMiddle = _panel.Middle.Any(x => x.IsPlayer);
		RightTabs.Add(new KingdomFactionTabVM(CrownKey, "王室阵营", _panel.CrownCount + " 家 · 军力 " + _panel.CrownPowerPercent + "%" + (inCrown ? " · 含你" : ""), CrownColor, "", _key == CrownKey, SelectTab));
		RightTabs.Add(new KingdomFactionTabVM(MiddleKey, "未表态", _panel.MiddleCount + " 家 · 军力 " + _panel.MiddlePowerPercent + "%" + (inMiddle ? " · 含你" : ""), MiddleColor, "", _key == MiddleKey, SelectTab));
	}

	// Only urgent deadlines (ultimatum, royal reply, war) earn a catalog badge.
	private static string Badge(CivilWarPanelFaction f)
	{
		if (!f.DeadlineUrgent || string.IsNullOrEmpty(f.DeadlineLabel)) return "";
		string label = f.DeadlineLabel == "最后通牒" ? "通牒" : f.DeadlineLabel;
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
		TableTitle = "支持家族";
		FooterText = _panel.Chronicle.Count > 0 ? "最新  ·  " + Clip(_panel.Chronicle[0], 48) : f.Stage;
	}

	// No faction yet: who is closest to the formation threshold, and why it has not formed.
	private void DescribeLedger()
	{
		TagText = "尚无派系"; TagColor = Color.ConvertStringToColor(LedgerColor);
		HeaderMeta = "稳定度" + _panel.StabilityTier + "  ·  不满与稳定度分别计算";
		HeaderTitle = _panel.StageText == "尚无派系成形" ? "王国内尚无反对派成立" : _panel.StageText;
		float gap = _panel.Threshold - _panel.TopGrievance;
		HeaderNote = string.IsNullOrEmpty(_panel.TopGrievanceClan) ? "没有可以成派的正式封臣家族。"
			: gap > 0 ? "最高家族不满 " + _panel.TopGrievance.ToString("0.0") + "，尚未达到成派门槛 " + _panel.Threshold + "。达到后按概率判定，同一王国每七日最多尝试一次。"
			: "最高家族不满 " + _panel.TopGrievance.ToString("0.0") + "，已达成派门槛 " + _panel.Threshold + "，每七日按概率判定一次。";
		HasBlock = true;
		BlockLabel = gap > 0 ? "距成派门槛" : "成派门槛";
		BlockValue = gap > 0 ? gap.ToString("0.0") : "已达";
		BlockUnit = gap > 0 ? "点" : "";
		BlockNote = string.IsNullOrEmpty(_panel.TopGrievanceClan) ? "" : _panel.TopGrievance.ToString("0.0") + " / " + _panel.Threshold + "  ·  " + _panel.TopGrievanceClan;
		foreach (CivilWarPanelCondition c in _panel.Conditions) Conditions.Add(new KingdomFactionConditionVM(c));
		_source = _panel.Crown.Concat(_panel.Middle).OrderByDescending(x => x.GrievanceValue).ToList();
		_rowThreshold = _panel.Threshold; _rowDecimal = true; _rowThresholdHigh = true;
		_pageSize = KingdomInteriorPanelPatch.LedgerRows;
		TableTitle = "贵族家族";
		FooterText = "按家族不满由高到低";
	}

	private void DescribeSide(bool crown)
	{
		TagText = crown ? "王室阵营" : "未表态"; TagColor = Color.ConvertStringToColor(crown ? "#8A6E2EFF" : "#6A6052FF");
		int count = crown ? _panel.CrownCount : _panel.MiddleCount;
		HeaderMeta = count + " 家  ·  军事实力 " + (crown ? _panel.CrownPowerPercent : _panel.MiddlePowerPercent) + "%";
		HeaderTitle = crown ? "王室阵营 · 随国王应对派系" : "未表态家族";
		HeaderNote = crown ? "若派系起兵，王室阵营家族随国王应战；家族不满达到 " + _panel.Threshold + " 会撤回对王室的支持。"
			: "尚未加入任何阵营，可加入王室或任一战前派系。";
		_source = crown ? _panel.Crown : _panel.Middle;
		_pageSize = KingdomInteriorPanelPatch.SideRows;
		TableTitle = crown ? "王室阵营家族" : "未表态家族";
		bool mine = _source.Any(x => x.IsPlayer);
		FooterText = (crown ? "国王在前，" : "") + (mine ? "你的家族置顶，" : "") + "其余按不满由高到低";
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

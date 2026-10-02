using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// ════════════════════════════════════════════════════════════════════════
//  Kingdom screen "派系" tab, v5 single-dossier layout (Pen frames uSZIC, RReCB,
//  jXQsV, jxOP8, zxPSj, a6PLm; notes in docs/designs/civilwar-v5).
//  Kingdom strip (banner, stability, power split) → tabs (each faction, crown,
//  undecided; a grievance ledger before any faction forms) → parchment dossier
//  (demand, progress, paged clans) → identity action bar.
//  Rebuilt on tab selection and on civil-war state changes while open; never per frame.
// ════════════════════════════════════════════════════════════════════════

[PrefabExtension("KingdomManagement", "descendant::ButtonWidget[@Id='ArmiesTabButton']")]
internal sealed class KingdomFactionTabButtonPatch : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _document;

	public override InsertType Type => (InsertType)4;

	public KingdomFactionTabButtonPatch()
	{
		_document = new XmlDocument();
		_document.LoadXml(@"
			<ButtonWidget Id='CivilWarFactionTabButton' IsVisible='@IsCivilWarFactionTabVisible' IsSelected='@IsCivilWarFactionSelected' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='!Header.Tab.Center.Width.Scaled' SuggestedHeight='!Header.Tab.Center.Height.Scaled' VerticalAlignment='Center' PositionYOffset='2' Brush='Header.Tab.Center' Command.Click='ExecuteShowCivilWarFactions' UpdateChildrenStates='true'>
			  <Children>
			    <TextWidget DataSource='{..}' WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' HorizontalAlignment='Center' VerticalAlignment='Center' Brush='Clan.TabControl.Text' Text='@CivilWarFactionTabText' />
			  </Children>
			</ButtonWidget>");
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _document;
	}
}

[PrefabExtension("KingdomManagement", "descendant::DiplomacyPanel[@Id='DiplomacyPanel']")]
internal sealed class KingdomFactionPanelPatch : PrefabExtensionInsertPatch
{
	// Fixed tracks; the VM sizes bars against the same constants.
	internal const int StabilityTrack = 240;
	internal const int PowerTrack = 800;
	internal const int PowerGap = 2;
	internal const int GrievanceTrack = 160;
	internal const int FactionRows = 5;
	internal const int SideRows = 7;

	// v5 palette (docs/designs/civilwar-v5)
	private const string Bg = "#17130FFF", Line = "#3A3025FF", Hair = "#3E3327FF";
	private const string Cream = "#EFE0BFFF", Tan = "#B8A27EFF", Mute = "#9C8C6CFF", Mute2 = "#8F7F62FF";
	private const string Paper = "#DCCDAEFF", PaperLine = "#BFAE8AFF", Ink = "#33251AFF", Ink2 = "#4A3826FF";
	private const string Brown = "#6D5940FF", Brown2 = "#7A6248FF", Brown3 = "#8C7456FF", Dim = "#9C8A6AFF";
	private const string Red = "#8B3827FF", Olive = "#5E6A3EFF", Gold = "#C9A66BFF";
	private const string BarBg = "#201A14FF", BarLine = "#4A3C2CFF";

	private readonly XmlDocument _document;

	public override InsertType Type => (InsertType)4;

	public KingdomFactionPanelPatch()
	{
		_document = new XmlDocument();
		_document.LoadXml(Root());
#if BANNERLORD_1_4_OR_GREATER
		foreach (XmlElement element in _document.SelectNodes("//*[@StackLayout.LayoutMethod='VerticalBottomToTop']"))
		{
			element.SetAttribute("StackLayout.LayoutMethod", "VerticalTopToBottom");
		}
#endif
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _document;
	}

	// ---------------------------------------------------------------- tiny XML helpers (built once, at load)

	private static string Size(string w, string h)
	{
		string ws = w == "*" ? "WidthSizePolicy='StretchToParent'" : w == "~" ? "WidthSizePolicy='CoverChildren'" : "WidthSizePolicy='Fixed' SuggestedWidth='" + w + "'";
		string hs = h == "*" ? "HeightSizePolicy='StretchToParent'" : h == "~" ? "HeightSizePolicy='CoverChildren'" : "HeightSizePolicy='Fixed' SuggestedHeight='" + h + "'";
		return ws + " " + hs;
	}

	// Text: literal or @binding; heading uses the serif kingdom brush.
	private static string T(string text, string w, string h, int size, string color, string align = "Left", bool heading = false, string extra = "")
		=> "<TextWidget " + Size(w, h) + " Brush='" + (heading ? "Kingdom.PoliciesItem.Text" : "Popup.Description.Text") + "' Brush.FontSize='" + size + "' Brush.FontColor='" + color + "' Brush.TextHorizontalAlignment='" + align + "' Text='" + text + "' DoNotAcceptEvents='true' " + extra + " />";

	private static string Box(string w, string h, string color, string extra = "")
		=> "<Widget " + Size(w, h) + " Sprite='BlankWhiteSquare_9' Color='" + color + "' DoNotAcceptEvents='true' " + extra + " />";

	private static string Row(string w, string h, string children, string extra = "")
		=> "<ListPanel " + Size(w, h) + " StackLayout.LayoutMethod='HorizontalLeftToRight' " + extra + "><Children>" + children + "</Children></ListPanel>";

	private static string Col(string w, string h, string children, string extra = "")
		=> "<ListPanel " + Size(w, h) + " StackLayout.LayoutMethod='VerticalBottomToTop' " + extra + "><Children>" + children + "</Children></ListPanel>";

	private static string Layer(string w, string h, string children, string extra = "")
		=> string.IsNullOrEmpty(children) ? "<Widget " + Size(w, h) + " DoNotAcceptEvents='true' " + extra + " />" : "<Widget " + Size(w, h) + " " + extra + "><Children>" + children + "</Children></Widget>";

	private static string Items(string source, string w, string h, string layout, string template, string extra = "")
		=> "<ListPanel DataSource='{" + source + "}' " + Size(w, h) + " StackLayout.LayoutMethod='" + layout + "' " + extra + "><ItemTemplate>" + template + "</ItemTemplate></ListPanel>";

	private static string BannerImage(string source, string w, string h, string extra = "")
		=> "<ImageIdentifierWidget DataSource='{" + source + "}' " + Size(w, h) + " ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' DoNotAcceptEvents='true' " + extra + " />";

	// ---------------------------------------------------------------- layout

	private static string Root() => @"
		<Widget Id='CivilWarFactionPanelRoot' IsVisible='@IsCivilWarFactionSelected' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginTop='188' MarginBottom='75'>
		  <Children>
		    <Widget DataSource='{CivilWarFactions}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='8' MarginRight='8' MarginTop='6' MarginBottom='8'>
		      <Children>
		        " + Box("*", "*", Bg) + @"
		        <RichTextWidget IsHidden='@IsAvailable' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='26' Text='@EmptyText' DoNotAcceptEvents='true' />
		        " + Col("*", "*", Strip() + Box("*", "1", Line, "MarginTop='14'") + Tabs() + Dossier() + ActionBar(), "IsVisible='@IsAvailable' MarginLeft='40' MarginRight='40' MarginTop='16'") + @"
		      </Children>
		    </Widget>
		  </Children>
		</Widget>";

	// Kingdom strip: banner + name/identity | stability | power split.
	private static string Strip() => Row("*", "78",
		Row("420", "*",
			Layer("50", "68", Box("*", "*", Gold) + BannerImage("KingdomBanner", "*", "*", "MarginLeft='2' MarginRight='2' MarginTop='2' MarginBottom='2'"), "VerticalAlignment='Center'")
			+ Col("*", "~",
				T("@KingdomName", "*", "40", 30, Cream, heading: true)
				+ T("@IdentityText", "*", "22", 15, Tan), "VerticalAlignment='Center' MarginLeft='18'"))
		+ Box("1", "56", Hair, "VerticalAlignment='Center' MarginRight='36'")
		+ Col("240", "~",
			Row("*", "34",
				T("王国稳定度", "~", "*", 14, Mute, extra: "VerticalAlignment='Bottom'")
				+ T("@StabilityValue", "~", "*", 28, "#E6D5B4FF", heading: true, extra: "MarginLeft='10'")
				+ T("@StabilityTierText", "~", "*", 14, "#D2876AFF", extra: "MarginLeft='8' VerticalAlignment='Bottom'"))
			+ Layer("*", "6", Box("*", "*", "#352A1FFF") + Box("@StabilityBarWidth", "*", "@StabilityColor"), "MarginTop='8'"), "VerticalAlignment='Center'")
		+ Layer("*", "*", "")
		+ Box("1", "56", Hair, "VerticalAlignment='Center' MarginRight='36'")
		+ Col(PowerTrack.ToString(), "~",
			Row("*", "22",
				T("王国军力分布", "*", "*", 14, Mute)
				+ T("@PowerStageText", "~", "*", 14, "#C9A37AFF", "Right"))
			+ Items("PowerSegments", "*", "12", "HorizontalLeftToRight", Box("@Width", "*", "@SegmentColor", "MarginRight='" + PowerGap + "'"), "MarginTop='8'")
			+ Items("PowerSegments", "*", "22", "HorizontalLeftToRight",
				Row("~", "*",
					Box("10", "10", "@SegmentColor", "VerticalAlignment='Center'")
					+ T("@Name", "~", "*", 14, "#CDBB98FF", extra: "MarginLeft='8'")
					+ T("@PercentText", "~", "*", 14, Mute2, extra: "MarginLeft='6'"), "MarginRight='26'"), "MarginTop='8'"), "VerticalAlignment='Center'"));

	// Tabs: factions (or the ledger) on the left, crown / undecided on the right.
	private static string Tabs() => Row("*", "52",
		Items("LeftTabs", "~", "*", "HorizontalLeftToRight", Tab())
		+ Layer("*", "*", "")
		+ Items("RightTabs", "~", "*", "HorizontalLeftToRight", Tab()), "MarginTop='22'");

	private static string Tab() => @"
		<ButtonWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' MarginLeft='4' Command.Click='ExecuteSelect' DoNotPassEventsToChildren='true'>
		  <Children>
		    " + Box("*", "*", "@BackColor") + Box("*", "3", "@MarkerColor", "IsVisible='@IsSelected'") + @"
		    " + Row("~", "*",
				Box("10", "10", "@MarkerColor", "VerticalAlignment='Center'")
				+ T("@Name", "~", "*", 19, Ink, heading: true, extra: "IsVisible='@IsSelected' MarginLeft='10'")
				+ T("@Name", "~", "*", 17, "#DCCBA8FF", heading: true, extra: "IsHidden='@IsSelected' MarginLeft='10'")
				+ T("@Meta", "~", "*", 13, Brown2, extra: "IsVisible='@IsSelected' MarginLeft='10'")
				+ T("@Meta", "~", "*", 13, Mute2, extra: "IsHidden='@IsSelected' MarginLeft='10'"), "MarginLeft='24' MarginRight='24'") + @"
		  </Children>
		</ButtonWidget>";

	// Parchment dossier: header → progress band (faction) or conditions band (ledger) → paged table → footer.
	private static string Dossier() => Layer("*", "*",
		Box("*", "*", Paper)
		+ Col("*", "*",
			Header()
			+ Box("*", "1", PaperLine, "IsVisible='@HasBand' MarginTop='14'")
			+ StepsBand() + LedgerBand()
			+ Box("*", "1", PaperLine, "MarginTop='14'")
			+ Table()
			+ Footer(), "MarginLeft='36' MarginRight='36' MarginTop='22' MarginBottom='14'"), "ClipContents='true'");

	private static string Header() => Row("*", "116",
		Col("*", "*",
			Row("*", "24",
				Layer("~", "*", Box("*", "*", "@TagColor") + T("@TagText", "~", "*", 13, "#F3E2C6FF", extra: "MarginLeft='10' MarginRight='10'"))
				+ T("@HeaderMeta", "*", "*", 14, Brown2, extra: "MarginLeft='14'"))
			+ T("@HeaderTitle", "*", "50", 32, Ink, heading: true, extra: "MarginTop='8'")
			+ T("@HeaderNote", "*", "24", 15, Brown, extra: "MarginTop='4'"))
		+ Layer("230", "*",
			Box("1", "*", PaperLine)
			+ Col("*", "~",
				T("@BlockLabel", "*", "20", 14, Red, "Right", extra: "IsVisible='@BlockUrgent'")
				+ T("@BlockLabel", "*", "20", 14, Brown2, "Right", extra: "IsHidden='@BlockUrgent'")
				+ Row("~", "52",
					T("@BlockValue", "~", "*", 44, Red, heading: true, extra: "IsVisible='@BlockUrgent'")
					+ T("@BlockValue", "~", "*", 44, "#5E4A32FF", heading: true, extra: "IsHidden='@BlockUrgent'")
					+ T("@BlockUnit", "~", "*", 16, Red, extra: "IsVisible='@BlockUrgent' MarginLeft='6' VerticalAlignment='Bottom'")
					+ T("@BlockUnit", "~", "*", 16, Brown2, extra: "IsHidden='@BlockUrgent' MarginLeft='6' VerticalAlignment='Bottom'"), "HorizontalAlignment='Right'")
				+ T("@BlockNote", "*", "20", 13, Brown3, "Right"), "MarginLeft='24'"), "IsVisible='@HasBlock'"));

	// Progress: four steps + key figures (faction tabs only).
	private static string StepsBand() => Row("*", "70",
		Items("Steps", "*", "*", "HorizontalLeftToRight",
			Col("250", "*",
				Row("*", "14",
					Layer("14", "14",
						Box("*", "*", "@DotColor", "IsHidden='@IsNext'")
						+ Box("*", "*", "#B9A784FF", "IsVisible='@IsNext'")
						+ Box("*", "*", Paper, "IsVisible='@IsNext' MarginLeft='2' MarginRight='2' MarginTop='2' MarginBottom='2'"), "VerticalAlignment='Center'")
					+ Box("*", "2", "@LineColor", "IsVisible='@HasLine' VerticalAlignment='Center' MarginLeft='8' MarginRight='8'"))
				+ T("@Name", "*", "26", 17, Ink2, heading: true, extra: "IsVisible='@IsDone' MarginTop='8'")
				+ T("@Name", "*", "26", 17, Red, heading: true, extra: "IsVisible='@IsCurrent' MarginTop='8'")
				+ T("@Name", "*", "26", 17, Dim, heading: true, extra: "IsVisible='@IsNext' MarginTop='8'")
				+ T("@Note", "*", "20", 13, "#9A4A35FF", extra: "IsVisible='@IsCurrent'")
				+ T("@Note", "*", "20", 13, Brown3, extra: "IsHidden='@IsCurrent'")))
		+ Box("1", "56", PaperLine, "VerticalAlignment='Center' MarginRight='36'")
		+ Row("~", "*",
			Figure("派系不满", "@FigureGrievance", "/ 100", true)
			+ Figure("军事实力", "@FigurePower", "@FigurePowerUnit", false)
			+ Figure("被拒次数", "@FigureRefusals", "@FigureRefusalUnit", false), "VerticalAlignment='Center'"),
		"IsVisible='@IsFactionTab' MarginTop='16'");

	private static string Figure(string label, string value, string unit, bool red) => Col("~", "~",
		T(label, "~", "20", 13, Brown2)
		+ Row("~", "38",
			T(value, "~", "*", 30, red ? Red : Ink, heading: true)
			+ T(unit, "~", "*", 13, Brown3, extra: "MarginLeft='6' VerticalAlignment='Bottom'")), "MarginRight='40'");

	// Formation conditions + recent chronicle (ledger tab only).
	private static string LedgerBand() => Row("*", "74",
		Col("*", "*",
			T("成派条件", "*", "26", 17, Ink2, heading: true)
			+ Items("Conditions", "*", "26", "HorizontalLeftToRight",
				Row("~", "*",
					Box("8", "8", "@MarkColor", "VerticalAlignment='Center'")
					+ T("@Name", "~", "*", 14, Brown, extra: "MarginLeft='8'")
					+ T("@Value", "~", "*", 14, Ink2, extra: "IsVisible='@IsMet' MarginLeft='6'")
					+ T("@Value", "~", "*", 14, Red, extra: "IsHidden='@IsMet' MarginLeft='6'"), "MarginRight='30'"), "MarginTop='10'"))
		+ Box("1", "56", PaperLine, "VerticalAlignment='Center' MarginRight='36'")
		+ Col("560", "*",
			T("近期纪事", "*", "26", 17, Ink2, heading: true)
			+ T("@SourceLine1", "*", "22", 13, Brown, extra: "MarginTop='6'")
			+ T("@SourceLine2", "*", "22", 13, Brown3)),
		"IsVisible='@IsLedgerTab' MarginTop='16'");

	// Clan table: name stretches; the other columns are fixed so header and rows line up.
	private static string Table() => Col("*", "*",
		Row("*", "34",
			T("@TableTitle", "*", "*", 13, Brown3)
			+ T("立场", "190", "*", 13, Brown3)
			+ T("@GrievanceTitle", "260", "*", 13, Brown3)
			+ T("主要原因", "220", "*", 13, Brown3)
			+ T("实力占比", "120", "*", 13, Brown3, "Right")
			+ T("对国王关系", "130", "*", 13, Brown3, "Right"), "MarginLeft='16' MarginRight='16'")
		+ Box("*", "1", PaperLine)
		+ Items("Rows", "*", "~", "VerticalBottomToTop", TableRow()), "MarginTop='10'");

	private static string TableRow() => Layer("*", "38",
		Box("*", "*", "@StripeColor")
		+ Box("3", "*", Red, "IsVisible='@IsPlayer'")
		+ Row("*", "*",
			T("@Name", "*", "*", 18, Ink, heading: true)
			+ T("@Stance", "190", "*", 14, Red, extra: "IsVisible='@IsLeader'")
			+ T("@Stance", "190", "*", 14, Brown, extra: "IsHidden='@IsLeader'")
			+ Row("260", "*",
				Layer(GrievanceTrack.ToString(), "11",
					Box("*", "5", "#C4B18DFF", "VerticalAlignment='Center'")
					+ Box("@BarWidth", "5", "@BarColor", "VerticalAlignment='Center'")
					+ Row("*", "*", Layer("@TickOffset", "*", "") + Box("2", "11", Red), "IsVisible='@HasTick'"), "VerticalAlignment='Center'")
				+ T("@GrievanceText", "88", "*", 16, Red, extra: "IsVisible='@IsHigh' MarginLeft='12'")
				+ T("@GrievanceText", "88", "*", 16, Brown, extra: "IsHidden='@IsHigh' MarginLeft='12'"))
			+ T("@Reason", "220", "*", 14, Brown)
			+ T("@PowerText", "120", "*", 16, Ink2, "Right")
			+ T("@RelationText", "130", "*", 16, Red, "Right", extra: "IsVisible='@IsNegative'")
			+ T("@RelationText", "130", "*", 16, Olive, "Right", extra: "IsHidden='@IsNegative'"), "MarginLeft='16' MarginRight='16'"));

	private static string Footer() => Row("*", "30",
		T("@FooterText", "*", "*", 14, Brown2)
		+ Row("~", "*",
			Pager("ExecutePrevPage", "上一页", "@HasPrevPage")
			+ T("@PageText", "~", "*", 14, Ink2, extra: "MarginLeft='18' MarginRight='18'")
			+ Pager("ExecuteNextPage", "下一页", "@HasNextPage"), "IsVisible='@HasPages'"), "MarginTop='8'");

	private static string Pager(string command, string label, string enabled) =>
		"<ButtonWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' Command.Click='" + command + "' DoNotPassEventsToChildren='true'><Children>"
		+ T(label, "~", "*", 14, "#6D4A30FF", extra: "IsVisible='" + enabled + "'")
		+ T(label, "~", "*", 14, "#A8977AFF", extra: "IsHidden='" + enabled + "'")
		+ "</Children></ButtonWidget>";

	// Identity action bar: seal + role/hint on the left, role-specific actions on the right.
	private static string ActionBar() => Layer("*", "92",
		Box("*", "*", BarBg) + Box("*", "1", BarLine)
		+ Row("*", "*",
			Layer("44", "56", Box("*", "*", "#9A7A48FF") + BannerImage("SealBanner", "*", "*", "MarginLeft='2' MarginRight='2' MarginTop='2' MarginBottom='2'"), "VerticalAlignment='Center'")
			+ Col("*", "~",
				T("@ActionTitle", "*", "28", 19, "#E7D0A5FF", heading: true)
				+ T("@ActionHint", "*", "20", 13, "#9C8A6CFF", extra: "MarginTop='4'"), "VerticalAlignment='Center' MarginLeft='16'")
			+ Items("Actions", "~", "*", "HorizontalLeftToRight", ActionButton(), "VerticalAlignment='Center'"),
			"MarginLeft='24' MarginRight='24'"));

	private static string ActionButton() => @"
		<ButtonWidget WidthSizePolicy='Fixed' SuggestedWidth='188' HeightSizePolicy='Fixed' SuggestedHeight='58' VerticalAlignment='Center' MarginLeft='12' Brush='Standard.Button' Command.Click='ExecuteClick' DoNotPassEventsToChildren='true'>
		  <Children>
		    " + Box("*", "*", "@TintColor", "MarginLeft='3' MarginRight='3' MarginTop='3' MarginBottom='3'") + @"
		    " + Col("*", "~",
				T("@Label", "*", "28", 19, "#F1DFC1FF", "Center", true, "IsHidden='@IsDim'")
				+ T("@Label", "*", "28", 19, "#9A8B70FF", "Center", true, "IsVisible='@IsDim'")
				+ T("@Sub", "*", "18", 12, "#C6AD86FF", "Center", extra: "IsHidden='@IsDim'")
				+ T("@Sub", "*", "18", 12, "#857760FF", "Center", extra: "IsVisible='@IsDim'"), "VerticalAlignment='Center'") + @"
		  </Children>
		</ButtonWidget>";
}

public sealed class KingdomFactionRowVM : ViewModel
{
	private const int Track = KingdomFactionPanelPatch.GrievanceTrack;
	private readonly CivilWarPanelClan _clan;
	private readonly int _index;
	private readonly int _threshold;
	private readonly bool _decimal;

	// threshold > 0 shows the formation tick (ledger only); decimal shows one decimal place.
	internal KingdomFactionRowVM(CivilWarPanelClan clan, int index, int threshold, bool showDecimal)
	{
		_clan = clan ?? new CivilWarPanelClan();
		_index = index;
		_threshold = threshold;
		_decimal = showDecimal;
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
	[DataSourceProperty] public bool IsHigh => _clan.GrievanceValue >= 60f || (_threshold > 0 && _clan.GrievanceValue >= _threshold);
	[DataSourceProperty] public int BarWidth => Math.Max(2, Math.Min(Track, (int)Math.Round(Track * _clan.GrievanceValue / 100f)));
	[DataSourceProperty] public Color BarColor => Color.ConvertStringToColor(_clan.GrievanceValue >= 70f ? "#8B3827FF" : _clan.GrievanceValue >= 20f ? "#A0643AFF" : "#8C7A5CFF");
	[DataSourceProperty] public bool HasTick => _threshold > 0 && _threshold < 100;
	[DataSourceProperty] public int TickOffset => Math.Max(0, Math.Min(Track - 2, Track * _threshold / 100 - 1));
	[DataSourceProperty] public Color StripeColor => Color.ConvertStringToColor(_clan.IsPlayer ? "#E8DAB9FF" : _index % 2 == 1 ? "#D5C5A4FF" : "#DCCDAEFF");
}

public sealed class KingdomFactionTabVM : ViewModel
{
	private readonly Action<string> _select;
	private bool _selected;

	internal KingdomFactionTabVM(string key, string name, string meta, string color, bool selected, Action<string> select)
	{
		Key = key; Name = name; Meta = meta; _marker = color; _selected = selected; _select = select;
	}

	private readonly string _marker;
	internal string Key { get; }
	[DataSourceProperty] public string Name { get; }
	[DataSourceProperty] public string Meta { get; }
	[DataSourceProperty] public Color MarkerColor => Color.ConvertStringToColor(_marker);
	[DataSourceProperty] public bool IsSelected => _selected;
	[DataSourceProperty] public Color BackColor => Color.ConvertStringToColor(_selected ? "#DCCDAEFF" : "#251F18FF");
	[DataSourceMethod] public void ExecuteSelect() => _select?.Invoke(Key);

	internal void SetSelected(bool selected)
	{
		if (_selected == selected) return;
		_selected = selected;
		OnPropertyChanged(nameof(IsSelected));
		OnPropertyChanged(nameof(BackColor));
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
	private string _key = "";
	private int _page;
	private int _pageSize = KingdomFactionPanelPatch.FactionRows;
	private int _rowThreshold;
	private bool _rowDecimal;
	private List<CivilWarPanelClan> _source = new List<CivilWarPanelClan>();
	private string _kingdomBannerKey = "", _sealBannerKey = "";

	[DataSourceProperty] public bool IsAvailable => _panel.Available;
	[DataSourceProperty] public string EmptyText => _panel.EmptyText;
	[DataSourceProperty] public string KingdomName => _panel.Name;
	[DataSourceProperty] public string IdentityText => _panel.Identity;
	[DataSourceProperty] public string StabilityValue => _panel.Stability.ToString();
	[DataSourceProperty] public string StabilityTierText => "/ 100  ·  " + _panel.StabilityTier;
	[DataSourceProperty] public int StabilityBarWidth => Math.Max(2, KingdomFactionPanelPatch.StabilityTrack * Math.Max(0, Math.Min(100, _panel.Stability)) / 100);
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
	[DataSourceProperty] public string FigurePower => _faction == null ? "" : _faction.PowerPercent + "%";
	[DataSourceProperty] public string FigurePowerUnit => _faction == null ? "" : "王室 " + _panel.CrownPowerPercent + "%  ·  " + _faction.Fortifications + " 城";
	[DataSourceProperty] public string FigureRefusals => _faction?.Refusals.ToString() ?? "";
	[DataSourceProperty] public string FigureRefusalUnit => _faction?.RefusalUnit ?? "";
	[DataSourceProperty] public MBBindingList<KingdomFactionConditionVM> Conditions { get; } = new MBBindingList<KingdomFactionConditionVM>();
	[DataSourceProperty] public string SourceLine1 => _panel.Chronicle.Count > 0 ? Clip(_panel.Chronicle[0], 40) : "暂无近期纪事";
	[DataSourceProperty] public string SourceLine2 => _panel.Chronicle.Count > 1 ? Clip(_panel.Chronicle[1], 40) : "";
	[DataSourceProperty] public string TableTitle { get; private set; } = "";
	[DataSourceProperty] public string GrievanceTitle => _rowThreshold > 0 ? "家族不满  ·  门槛 " + _rowThreshold : "不满";
	[DataSourceProperty] public MBBindingList<KingdomFactionRowVM> Rows { get; } = new MBBindingList<KingdomFactionRowVM>();
	[DataSourceProperty] public string FooterText { get; private set; } = "";
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
		nameof(IsAvailable), nameof(EmptyText), nameof(KingdomName), nameof(IdentityText), nameof(StabilityValue), nameof(StabilityTierText),
		nameof(StabilityBarWidth), nameof(StabilityColor), nameof(PowerStageText), nameof(IsFactionTab), nameof(IsLedgerTab), nameof(HasBand),
		nameof(TagText), nameof(TagColor), nameof(HeaderMeta), nameof(HeaderTitle), nameof(HeaderNote), nameof(HasBlock), nameof(BlockUrgent),
		nameof(BlockLabel), nameof(BlockValue), nameof(BlockUnit), nameof(BlockNote), nameof(FigureGrievance), nameof(FigurePower),
		nameof(FigurePowerUnit), nameof(FigureRefusals), nameof(FigureRefusalUnit), nameof(SourceLine1), nameof(SourceLine2), nameof(TableTitle),
		nameof(GrievanceTitle), nameof(FooterText), nameof(HasPages), nameof(HasPrevPage), nameof(HasNextPage), nameof(PageText),
		nameof(ActionTitle), nameof(ActionHint)
	};

	// Called on tab selection and on civil-war state changes while the tab is open.
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
		int track = KingdomFactionPanelPatch.PowerTrack - KingdomFactionPanelPatch.PowerGap * parts.Count;
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

	private void RebuildTabs()
	{
		LeftTabs.Clear();
		RightTabs.Clear();
		if (!_panel.Available) { _key = ""; return; }
		bool valid = _key == CrownKey || _key == MiddleKey || (_key == LedgerKey && _panel.Factions.Count == 0) || _panel.Factions.Any(x => "f:" + x.Id == _key);
		if (!valid) _key = _panel.Factions.Count == 0 ? LedgerKey : "f:" + (_panel.Factions.FirstOrDefault(x => x.Id == _panel.PlayerFactionId) ?? _panel.Factions[0]).Id;
		// Four factions (MCM max) plus the two side tabs only fit with the short meta.
		bool compact = _panel.Factions.Count >= 4;
		foreach (CivilWarPanelFaction f in _panel.Factions)
		{
			string mine = f.IsPlayerLeader ? "你领导 · " : f.IsPlayerFaction ? (compact ? "你的 · " : "你的派系 · ") : "";
			string meta = mine + f.MemberCount + " 家" + (compact ? "" : " · 不满 " + f.Grievance);
			LeftTabs.Add(new KingdomFactionTabVM("f:" + f.Id, Clip(f.ShortName, compact ? 10 : 16), meta, f.Color, _key == "f:" + f.Id, SelectTab));
		}
		if (_panel.Factions.Count == 0)
			LeftTabs.Add(new KingdomFactionTabVM(LedgerKey, "贵族不满账册", (_panel.CrownCount + _panel.MiddleCount) + " 家 · 最高 " + _panel.TopGrievance.ToString("0.0"), LedgerColor, _key == LedgerKey, SelectTab));
		bool inCrown = _panel.Role == CivilWarPanelRole.Crown, inMiddle = _panel.Middle.Any(x => x.IsPlayer);
		RightTabs.Add(new KingdomFactionTabVM(CrownKey, "王室阵营", _panel.CrownCount + " 家" + (inCrown ? " · 含你" : ""), CrownColor, _key == CrownKey, SelectTab));
		RightTabs.Add(new KingdomFactionTabVM(MiddleKey, "未表态", _panel.MiddleCount + " 家" + (inMiddle ? " · 含你" : ""), MiddleColor, _key == MiddleKey, SelectTab));
	}

	private void SelectTab(string key)
	{
		if (!_open || key == _key) return;
		_key = key;
		foreach (KingdomFactionTabVM tab in LeftTabs) tab.SetSelected(tab.Key == key);
		foreach (KingdomFactionTabVM tab in RightTabs) tab.SetSelected(tab.Key == key);
		RebuildView(true);
		RebuildActions();
		Notify();
	}

	private void RebuildView(bool resetPage)
	{
		if (resetPage) _page = 0;
		_faction = _key.StartsWith("f:", StringComparison.Ordinal) ? _panel.Factions.FirstOrDefault(x => "f:" + x.Id == _key) : null;
		Steps.Clear();
		Conditions.Clear();
		HasBlock = false; BlockUrgent = false; BlockLabel = BlockValue = BlockUnit = BlockNote = "";
		_rowThreshold = 0; _rowDecimal = false; _pageSize = KingdomFactionPanelPatch.FactionRows;
		if (!_panel.Available) { _source = new List<CivilWarPanelClan>(); TagText = HeaderMeta = HeaderTitle = HeaderNote = TableTitle = FooterText = ""; }
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
		_rowThreshold = _panel.Threshold; _rowDecimal = true;
		TableTitle = "贵族家族";
		FooterText = "按家族不满由高到低  ·  红色刻度为成派门槛";
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
		_pageSize = KingdomFactionPanelPatch.SideRows;
		TableTitle = crown ? "王室阵营家族" : "未表态家族";
		bool mine = _source.Any(x => x.IsPlayer);
		FooterText = (crown ? "国王在前，" : "") + (mine ? "你的家族置顶，" : "") + "其余按不满由高到低";
	}

	private void RebuildRows()
	{
		Rows.Clear();
		_page = Math.Max(0, Math.Min(_page, PageCount - 1));
		int start = _page * _pageSize;
		for (int i = start; i < Math.Min(_source.Count, start + _pageSize); i++) Rows.Add(new KingdomFactionRowVM(_source[i], i - start, _rowThreshold, _rowDecimal));
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

[ViewModelMixin("RefreshValues", true)]
internal sealed class KingdomFactionVMMixin : BaseViewModelMixin<KingdomManagementVM>
{
	[DataSourceProperty]
	public string CivilWarFactionTabText { get; set; }

	[DataSourceProperty]
	public bool IsCivilWarFactionTabVisible { get; set; }

	[DataSourceProperty]
	public bool IsCivilWarFactionSelected { get; set; }

	[DataSourceProperty]
	public KingdomFactionPanelVM CivilWarFactions { get; set; }

	public KingdomFactionVMMixin(KingdomManagementVM vm) : base(vm)
	{
		CivilWarFactionTabText = "派系";
		IsCivilWarFactionTabVisible = DuelSettings.IsCivilWarFactionsEnabled();
		IsCivilWarFactionSelected = false;
		CivilWarFactions = new KingdomFactionPanelVM();
		KingdomFactionTabState.Register(this);
	}

	[DataSourceMethod]
	public void ExecuteShowCivilWarFactions()
	{
		ViewModel.Clan.Show = false;
		ViewModel.Settlement.Show = false;
		ViewModel.Policy.Show = false;
		ViewModel.Army.Show = false;
		ViewModel.Diplomacy.Show = false;
		KingdomAgendaTabState.ClearForCustomTabClick();

		CivilWarFactions.Open();
		CivilWarFactions.Refresh();
		IsCivilWarFactionSelected = true;
		ViewModel.OnPropertyChangedWithValue(true, nameof(IsCivilWarFactionSelected));
	}

	internal void ClearSelection()
	{
		if (!IsCivilWarFactionSelected) return;
		IsCivilWarFactionSelected = false;
		CivilWarFactions.Close();
		ViewModel.OnPropertyChangedWithValue(false, nameof(IsCivilWarFactionSelected));
	}
}

// Kingdom management is a single active screen, so a weak shortcut to the
// latest mixin is enough; clicks on any other kingdom tab clear the faction tab.
internal static class KingdomFactionTabState
{
	private static WeakReference<KingdomFactionVMMixin> _current;

	internal static void Register(KingdomFactionVMMixin mixin)
	{
		_current = new WeakReference<KingdomFactionVMMixin>(mixin);
	}

	internal static void Clear()
	{
		if (_current != null && _current.TryGetTarget(out KingdomFactionVMMixin mixin)) mixin.ClearSelection();
	}
}

[HarmonyPatch(typeof(KingdomManagementVM), nameof(KingdomManagementVM.OnFinalize))]
internal static class KingdomFactionFinalizePatch
{
	private static void Prefix() => KingdomFactionTabState.Clear();
}

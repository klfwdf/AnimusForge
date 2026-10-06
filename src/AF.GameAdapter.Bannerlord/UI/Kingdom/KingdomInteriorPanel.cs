using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace AnimusForge;

// ════════════════════════════════════════════════════════════════════════
//  Kingdom screen "内政" tab: agendas and factions in one panel (Pen frames bJ1Yq, LEL0F;
//  notes in docs/designs/neizheng-v1). Player's own kingdom only.
//  Kingdom strip → catalog (agenda section above, faction section below, each with its own
//  heading bar and scroll) | parchment dossier of the selected entry → identity action bar.
//  Built once at prefab load. KingdomAgendaVM drives the agenda half, KingdomFactionPanelVM the
//  faction half; KingdomAgendaVMMixin keeps at most one entry selected across both.
//
//  Gauntlet rule used throughout: a CoverChildren ("~") parent must never hold a StretchToParent
//  ("*") child on the same axis — the stretch child takes all offered space and the parent with it.
//  Coloured tags and badges are therefore Chip()s: the chip widget draws its own sprite and hugs its text.
// ════════════════════════════════════════════════════════════════════════

[PrefabExtension("KingdomManagement", "descendant::DiplomacyPanel[@Id='DiplomacyPanel']")]
internal sealed class KingdomInteriorPanelPatch : PrefabExtensionInsertPatch
{
	// Fixed tracks and page sizes; the view models size bars and pages against the same constants.
	internal const int StabilityTrack = 240;
	internal const int PowerTrack = 800;
	internal const int PowerGap = 2;
	internal const int GrievanceTrack = 220;
	internal const int FactionRows = 4;
	internal const int LedgerRows = 5;
	internal const int SideRows = 8;
	internal const int OptionCardWidth = 420;
	internal const int SupportTrack = OptionCardWidth - 40;
	internal const int MaxSupporters = 2;

	// Palette (docs/designs/neizheng-v1, inherited from civilwar-v5)
	private const string Bg = "#17130FFF", Line = "#3A3025FF", Hair = "#3E3327FF", CatalogBg = "#1D1813FF", SectionBg = "#241D16FF";
	private const string Cream = "#EFE0BFFF", Tan = "#B8A27EFF", Mute = "#9C8C6CFF", Mute2 = "#8F7F62FF", RowText = "#DCCBA8FF";
	private const string Paper = "#DCCDAEFF", PaperLine = "#BFAE8AFF", Ink = "#33251AFF", Ink2 = "#4A3826FF";
	private const string Brown = "#6D5940FF", Brown2 = "#7A6248FF", Brown3 = "#8C7456FF", Dim = "#9C8A6AFF";
	private const string Red = "#8B3827FF", Crimson = "#8C1E1EFF", Olive = "#5E6A3EFF", Gold = "#C9A66BFF", Light = "#F3E2C6FF";
	private const string BarBg = "#201A14FF", BarLine = "#4A3C2CFF";

	private readonly XmlDocument _document;

	public override InsertType Type => (InsertType)4;

	public KingdomInteriorPanelPatch()
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

	// Text: literal or @binding; heading uses the serif kingdom brush. Text sits vertically centred in its box.
	private static string T(string text, string w, string h, int size, string color, string align = "Left", bool heading = false, string extra = "")
		=> "<TextWidget " + Size(w, h) + " Brush='" + (heading ? "Kingdom.PoliciesItem.Text" : "Popup.Description.Text") + "' Brush.FontSize='" + size + "' Brush.FontColor='" + color + "' Brush.TextHorizontalAlignment='" + align + "' Brush.TextVerticalAlignment='Center' Text='" + text + "' DoNotAcceptEvents='true' " + extra + " />";

	private static string Box(string w, string h, string color, string extra = "")
		=> "<Widget " + Size(w, h) + " Sprite='BlankWhiteSquare_9' Color='" + color + "' DoNotAcceptEvents='true' " + extra + " />";

	// Coloured tag / badge that hugs its (CoverChildren) texts; only visible texts count toward its width.
	private static string Chip(string h, string color, string texts, string extra = "")
		=> "<Widget " + Size("~", h) + " Sprite='BlankWhiteSquare_9' Color='" + color + "' DoNotAcceptEvents='true' " + extra + "><Children>" + texts + "</Children></Widget>";

	private static string Row(string w, string h, string children, string extra = "")
		=> "<ListPanel " + Size(w, h) + " StackLayout.LayoutMethod='HorizontalLeftToRight' " + extra + "><Children>" + children + "</Children></ListPanel>";

	private static string Col(string w, string h, string children, string extra = "")
		=> "<ListPanel " + Size(w, h) + " StackLayout.LayoutMethod='VerticalBottomToTop' " + extra + "><Children>" + children + "</Children></ListPanel>";

	private static string Layer(string w, string h, string children, string extra = "")
		=> string.IsNullOrEmpty(children) ? "<Widget " + Size(w, h) + " DoNotAcceptEvents='true' " + extra + " />" : "<Widget " + Size(w, h) + " " + extra + "><Children>" + children + "</Children></Widget>";

	private static string Items(string source, string w, string h, string layout, string template, string extra = "")
		=> "<ListPanel DataSource='{" + source + "}' " + Size(w, h) + " StackLayout.LayoutMethod='" + layout + "' " + extra + "><ItemTemplate>" + template + "</ItemTemplate></ListPanel>";

	private static string Image(string source, string w, string h, string extra = "")
		=> "<ImageIdentifierWidget DataSource='{" + source + "}' " + Size(w, h) + " ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' DoNotAcceptEvents='true' " + extra + " />";

	// Switches the data source, then shows the content only while `visible` holds on it. Scopes overlap
	// inside the dossier and the action bar, so the wrappers never take clicks meant for a sibling's buttons.
	private static string Scope(string source, string visible, string children)
		=> "<Widget DataSource='{" + source + "}' " + Size("*", "*") + " DoNotAcceptEvents='true'><Children><Widget IsVisible='" + visible + "' " + Size("*", "*") + " DoNotAcceptEvents='true'><Children>" + children + "</Children></Widget></Children></Widget>";

	private static string Source(string source, string children)
		=> "<Widget DataSource='{" + source + "}' " + Size("*", "*") + " DoNotAcceptEvents='true'><Children>" + children + "</Children></Widget>";

	// Vertical list that scrolls with the wheel. The bare ScrollbarWidget (vanilla PoliciesPanel pattern)
	// disappears completely while the content fits; the Standard prefab would leave its dark bed behind.
	private static string Scroll(string id, string inner) =>
		"<ScrollablePanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' AutoHideScrollBars='true' ClipRect='" + id + "Clip' InnerPanel='" + id + "Clip\\" + id + "Inner' VerticalScrollbar='..\\" + id + "Scrollbar'><Children>"
		+ "<Widget Id='" + id + "Clip' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ClipContents='true'><Children>"
		+ "<ListPanel Id='" + id + "Inner' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop'><Children>" + inner + "</Children></ListPanel>"
		+ "</Children></Widget></Children></ScrollablePanel>"
		+ "<ScrollbarWidget Id='" + id + "Scrollbar' WidthSizePolicy='Fixed' HeightSizePolicy='StretchToParent' SuggestedWidth='8' HorizontalAlignment='Right' MarginRight='3' MarginTop='4' MarginBottom='4' AlignmentAxis='Vertical' Handle='" + id + "ScrollbarHandle' IsVisible='false' MaxValue='100' MinValue='0'><Children>"
		+ "<Widget WidthSizePolicy='Fixed' HeightSizePolicy='StretchToParent' SuggestedWidth='4' HorizontalAlignment='Center' Sprite='BlankWhiteSquare_9' Color='#5A4033FF' AlphaFactor='0.25' DoNotAcceptEvents='true' />"
		+ "<ImageWidget Id='" + id + "ScrollbarHandle' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='8' SuggestedHeight='10' HorizontalAlignment='Center' Brush='FaceGen.Scrollbar.Handle' IsVisible='false' />"
		+ "</Children></ScrollbarWidget>";

	// ---------------------------------------------------------------- layout

	private static string Root() => @"
		<Widget Id='AgendaPanelRoot' IsVisible='@IsAgendaSelected' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginTop='188' MarginBottom='75'>
		  <Children>
		    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='8' MarginRight='8' MarginTop='6' MarginBottom='8'>
		      <Children>
		        " + Box("*", "*", Bg) + @"
		        " + Col("*", "*", Strip() + Row("*", "*", Catalog() + Dossier(), "MarginTop='14'") + ActionBar(), "MarginLeft='40' MarginRight='40' MarginTop='14'") + @"
		      </Children>
		    </Widget>
		  </Children>
		</Widget>";

	// Kingdom strip of the player's kingdom; collapses when civil-war factions are unavailable.
	private static string Strip() => "<ListPanel DataSource='{CivilWarFactions}' " + Size("*", "~") + " StackLayout.LayoutMethod='VerticalBottomToTop'><Children>"
		+ Col("*", "~", StripRow() + Box("*", "1", Line, "MarginTop='12'"), "IsVisible='@IsAvailable'")
		+ "</Children></ListPanel>";

	private static string StripRow() => Row("*", "80",
		Row("440", "*",
			Layer("50", "68", Box("*", "*", Gold) + Image("KingdomBanner", "*", "*", "MarginLeft='2' MarginRight='2' MarginTop='2' MarginBottom='2'"), "VerticalAlignment='Center'")
			+ Col("*", "~",
				T("@KingdomName", "*", "42", 32, Cream, heading: true)
				+ T("@IdentityText", "*", "24", 17, Tan), "VerticalAlignment='Center' MarginLeft='18'"))
		+ Box("1", "56", Hair, "VerticalAlignment='Center' MarginRight='36'")
		+ Col(StabilityTrack.ToString(), "~",
			Row("*", "36",
				T("王国稳定度", "~", "*", 16, Mute)
				+ T("@StabilityValue", "~", "*", 30, "#E6D5B4FF", heading: true, extra: "MarginLeft='10'")
				+ T("@StabilityTierText", "~", "*", 16, "#D2876AFF", extra: "MarginLeft='8'"))
			+ Layer("*", "6", Box("*", "*", "#352A1FFF") + Box("@StabilityBarWidth", "*", "@StabilityColor"), "MarginTop='8'"), "VerticalAlignment='Center'")
		+ Layer("*", "*", "")
		+ Box("1", "56", Hair, "VerticalAlignment='Center' MarginRight='36'")
		+ Col(PowerTrack.ToString(), "~",
			Row("*", "24",
				T("王国军力分布", "*", "*", 16, Mute)
				+ T("@PowerStageText", "~", "*", 16, "#C9A37AFF", "Right"))
			+ Items("PowerSegments", "*", "12", "HorizontalLeftToRight", Box("@Width", "*", "@SegmentColor", "MarginRight='" + PowerGap + "'"), "MarginTop='8'")
			+ Items("PowerSegments", "*", "24", "HorizontalLeftToRight",
				Row("~", "*",
					Box("10", "10", "@SegmentColor", "VerticalAlignment='Center'")
					+ T("@Name", "~", "*", 16, "#CDBB98FF", extra: "MarginLeft='8'")
					+ T("@PercentText", "~", "*", 16, Mute2, extra: "MarginLeft='6'"), "MarginRight='26'"), "MarginTop='8'"), "VerticalAlignment='Center'"));

	// ---------------------------------------------------------------- catalog

	// Two separate sections, agendas above and factions below, each with its own heading bar and scroll.
	private static string Catalog() => Layer("400", "*",
		Box("*", "*", CatalogBg)
		+ Col("*", "*",
			T("内政事务", "*", "40", 26, Cream, heading: true, extra: "MarginLeft='22' MarginTop='14'")
			+ AgendaSection()
			+ FactionSection(), "MarginBottom='12'"));

	private static string SectionHead(string name, string count) => Layer("*", "40",
		Box("*", "*", SectionBg) + Box("*", "1", Line, "VerticalAlignment='Bottom'")
		+ Row("*", "*",
			Box("4", "18", Gold, "VerticalAlignment='Center'")
			+ T(name, "~", "*", 18, Cream, heading: true, extra: "MarginLeft='12'")
			+ Layer("*", "*", "")
			+ T(count, "~", "*", 15, Mute2), "MarginLeft='18' MarginRight='18'"));

	// Agendas of the player's kingdom (KingdomAgendaVM).
	private static string AgendaSection() => "<Widget DataSource='{Agenda}' " + Size("*", "*") + " MarginTop='10'><Children>"
		+ Col("*", "*",
			SectionHead("王国议程", "@ItemCountText")
			+ Layer("*", "*",
				Scroll("InteriorAgenda", Items("AgendaItems", "*", "~", "VerticalBottomToTop", AgendaRow()))
				+ T("暂无进行中的议程", "*", "60", 17, Mute2, "Center", extra: "IsHidden='@HasItems' VerticalAlignment='Top' MarginTop='12'")))
		+ "</Children></Widget>";

	// Factions (or the grievance ledger), crown and undecided of the player's kingdom (KingdomFactionPanelVM).
	private static string FactionSection() => "<Widget DataSource='{CivilWarFactions}' " + Size("*", "*") + " IsVisible='@IsCatalogVisible' MarginTop='16'><Children>"
		+ Col("*", "*",
			SectionHead("派系与阵营", "@CatalogCountText")
			+ Layer("*", "*",
				Scroll("InteriorFaction",
					Items("LeftTabs", "*", "~", "VerticalBottomToTop", FactionRow())
					+ Items("RightTabs", "*", "~", "VerticalBottomToTop", FactionRow()))))
		+ "</Children></Widget>";

	// Selected rows turn to parchment with a red edge; texts come in selected / plain pairs.
	// The text column stretches; the badge is a Chip so it can never claim the row's width.
	private static string EntryButton(string marker, string name, string info, string badge) =>
		"<ButtonWidget " + Size("*", "66") + " Command.Click='ExecuteSelect' DoNotPassEventsToChildren='true'><Children>"
		+ Box("*", "*", Paper, "IsVisible='@IsSelected'") + Box("4", "*", Crimson, "IsVisible='@IsSelected'")
		+ Row("*", "*",
			marker
			+ Col("*", "~",
				T(name, "*", "27", 19, Ink, heading: true, extra: "IsVisible='@IsSelected'")
				+ T(name, "*", "27", 19, RowText, heading: true, extra: "IsHidden='@IsSelected'")
				+ T(info, "*", "20", 15, Brown2, extra: "IsVisible='@IsSelected' MarginTop='3'")
				+ T(info, "*", "20", 15, Mute2, extra: "IsHidden='@IsSelected' MarginTop='3'"), "VerticalAlignment='Center' MarginLeft='14' MarginRight='10'")
			+ badge, "MarginLeft='22' MarginRight='16'")
		+ "</Children></ButtonWidget>";

	private static string AgendaRow() => EntryButton(
		Box("10", "10", Brown, "IsVisible='@IsSelected' VerticalAlignment='Center'") + Box("10", "10", Mute, "IsHidden='@IsSelected' VerticalAlignment='Center'"),
		"@CatalogTitleText", "@InfoText",
		Chip("24", "@BadgeColor",
			T("@BadgeText", "~", "*", 15, Light, extra: "IsVisible='@IsBadgeUrgent' MarginLeft='8' MarginRight='8'")
			+ T("@BadgeText", "~", "*", 15, "#B9C38EFF", extra: "IsVisible='@IsVoteOpen' MarginLeft='8' MarginRight='8'")
			+ T("@BadgeText", "~", "*", 15, Tan, extra: "IsVisible='@IsBadgePlain' MarginLeft='8' MarginRight='8'"), "VerticalAlignment='Center'"));

	private static string FactionRow() => EntryButton(
		Box("10", "10", "@MarkerColor", "VerticalAlignment='Center'"),
		"@Name", "@Meta",
		Layer("~", "24",
			Chip("24", "#5A2418FF", T("@Badge", "~", "*", 15, Light, extra: "MarginLeft='8' MarginRight='8'"), "IsVisible='@IsSelected'")
			+ Chip("24", "#3A1A14FF", T("@Badge", "~", "*", 15, "#E7A08FFF", extra: "MarginLeft='8' MarginRight='8'"), "IsHidden='@IsSelected'"),
			"IsVisible='@HasBadge' VerticalAlignment='Center'"));

	// ---------------------------------------------------------------- dossier

	// The prompt sits under both dossiers; whichever entry is selected covers it with parchment.
	private static string Dossier() => Layer("*", "*",
		Box("*", "*", Paper)
		+ T("选择左侧条目查看详情", "*", "*", 26, Brown3, "Center", true)
		+ AgendaDossier()
		+ FactionDossier(), "ClipContents='true'");

	// Selected agenda (KingdomAgendaItemVM): centred header with the vote button → option cards → footer.
	private static string AgendaDossier() => Scope("Agenda", "@HasSelectedItem", Source("SelectedItem",
		Box("*", "*", Paper)
		+ Col("*", "*",
			AgendaHeader()
			+ Box("*", "1", PaperLine, "MarginTop='14'")
			+ Layer("*", "*",
				Items("Options", "~", "*", "HorizontalLeftToRight", OptionCard(), "HorizontalAlignment='Center'")
				+ T("此议程暂无投票详情", "*", "60", 20, Brown3, "Center", extra: "IsHidden='@HasDetail'"), "MarginTop='18'")
			+ Row("*", "26",
				T("@FooterText", "*", "*", 16, Red, "Center")
				+ T("@DealText", "~", "*", 16, Brown2), "MarginTop='10'"),
			"MarginLeft='40' MarginRight='40' MarginTop='22' MarginBottom='14'")));

	// Text column centred between the dossier edge and the deadline block; a matching left spacer keeps it
	// centred on the whole dossier.
	private static string AgendaHeader() => Row("*", "190",
		Layer("230", "*", "")
		+ Col("*", "*",
			Row("~", "28",
				Chip("28", Olive, T("@DecisionTypeText", "~", "*", 16, Light, extra: "MarginLeft='12' MarginRight='12'"))
				+ T("@MetaText", "~", "*", 16, Brown2, extra: "MarginLeft='14'"), "HorizontalAlignment='Center'")
			+ T("@TitleText", "*", "52", 38, Ink, "Center", true, "MarginTop='10'")
			+ T("@NoteText", "*", "26", 17, Brown, "Center", extra: "MarginTop='6'")
			+ "<ButtonWidget IsVisible='@CanCallVoteMeeting' HorizontalAlignment='Center' WidthSizePolicy='Fixed' SuggestedWidth='220' HeightSizePolicy='Fixed' SuggestedHeight='46' MarginTop='12' Brush='ButtonBrush1' Command.Click='ExecuteCallVoteMeeting' DoNotPassEventsToChildren='true'><Children>"
			+ T("@MeetingButtonText", "*", "*", 19, "#FFE7A8FF", "Center", true)
			+ "</Children></ButtonWidget>")
		+ Block(""));

	// Right-hand deadline block shared by both dossiers (BlockLabel / BlockValue / BlockUnit / BlockNote, urgent in red).
	private static string Block(string extra) => Layer("230", "*",
		Box("1", "*", PaperLine)
		+ Col("*", "~",
			T("@BlockLabel", "*", "24", 16, Red, "Right", extra: "IsVisible='@BlockUrgent'")
			+ T("@BlockLabel", "*", "24", 16, Brown2, "Right", extra: "IsHidden='@BlockUrgent'")
			+ Row("~", "58",
				T("@BlockValue", "~", "*", 48, Red, heading: true, extra: "IsVisible='@BlockUrgent'")
				+ T("@BlockValue", "~", "*", 48, "#5E4A32FF", heading: true, extra: "IsHidden='@BlockUrgent'")
				+ T("@BlockUnit", "~", "*", 18, Red, extra: "IsVisible='@BlockUrgent' MarginLeft='6'")
				+ T("@BlockUnit", "~", "*", 18, Brown2, extra: "IsHidden='@BlockUrgent' MarginLeft='6'"), "HorizontalAlignment='Right'")
			+ T("@BlockNote", "*", "24", 15, Brown3, "Right"), "MarginLeft='24'"),
		extra);

	// One vote option (AgendaOptionVM), centred: sponsor, name, support share (or a caption when shares
	// mean nothing), effect, top supporters.
	private static string OptionCard() => Layer(OptionCardWidth.ToString(), "*",
		Box("*", "*", "@CardColor")
		+ Col("*", "~",
			Row("~", "34",
				Layer("34", "34", Box("*", "*", Brown3) + Image("SponsorVisual", "*", "*"), "IsVisible='@HasSponsor'")
				+ Layer("20", "20", Image("SponsorBanner", "*", "*"), "IsVisible='@HasSponsorBanner' VerticalAlignment='Center' MarginLeft='10'")
				+ T("@SponsorName", "~", "*", 17, Brown, extra: "MarginLeft='10'")
				+ Chip("24", Olive, T("领先", "~", "*", 15, Light, extra: "MarginLeft='8' MarginRight='8'"), "IsVisible='@IsLeading' VerticalAlignment='Center' MarginLeft='12'"),
				"HorizontalAlignment='Center'")
			+ T("@Name", "*", "34", 24, Ink, "Center", true, "MarginTop='8'")
			+ Layer(SupportTrack.ToString(), "30",
				Box("*", "*", "#C4B18DFF")
				+ Box("@BarWidth", "*", "@BarColor")
				+ T("@SupportText", "*", "*", 17, Ink, "Center"), "IsVisible='@ShowSupport' HorizontalAlignment='Center' MarginTop='8'")
			+ T("@SupportCaption", "*", "30", 17, Brown, "Center", extra: "IsHidden='@ShowSupport' MarginTop='8'")
			+ T("@Description", "*", "~", 16, Brown, "Center", extra: "MarginTop='8'")
			+ T("@SupporterCountText", "*", "26", 16, Brown3, "Center", extra: "IsVisible='@HasSupporters' MarginTop='8'")
			+ Items("Supporters", "~", "~", "VerticalBottomToTop",
				Row("~", "30",
					Layer("24", "24", Box("*", "*", "#A8936EFF") + Image("Visual", "*", "*"), "VerticalAlignment='Center'")
					+ T("@Name", "~", "*", 17, Ink, extra: "MarginLeft='10'")
					+ T("@WeightText", "~", "*", 15, Red, extra: "IsVisible='@IsStrong' MarginLeft='12'")
					+ T("@WeightText", "~", "*", 15, Brown, extra: "IsHidden='@IsStrong' MarginLeft='12'")), "HorizontalAlignment='Center'")
			+ T("@MoreSupportersText", "*", "22", 15, Brown3, "Center", extra: "IsVisible='@HasMoreSupporters'"),
			"MarginLeft='20' MarginRight='20' MarginTop='18'"),
		"MarginLeft='10' MarginRight='10' ClipContents='true'");

	// Faction / ledger / crown / undecided dossier: header → progress and figures (faction) or
	// conditions (ledger) → paged clan table → footer.
	private static string FactionDossier() => Scope("CivilWarFactions", "@HasEntry",
		Box("*", "*", Paper)
		+ Col("*", "*",
			FactionHeader()
			+ Box("*", "1", PaperLine, "IsVisible='@HasBand' MarginTop='10'")
			+ StepsBand() + FiguresBand() + LedgerBand()
			+ Table()
			+ Row("*", "26",
				T("@FooterText", "*", "*", 15, Brown2)
				+ T("@LegendText", "~", "*", 15, Brown2), "MarginTop='6'"),
			"MarginLeft='40' MarginRight='40' MarginTop='20' MarginBottom='12'"));

	private static string FactionHeader() => Row("*", "112",
		Layer("230", "*", "")
		+ Col("*", "*",
			Row("~", "28",
				Chip("28", "@TagColor", T("@TagText", "~", "*", 16, Light, extra: "MarginLeft='12' MarginRight='12'"))
				+ T("@HeaderMeta", "~", "*", 16, Brown2, extra: "MarginLeft='14'"), "HorizontalAlignment='Center'")
			+ T("@HeaderTitle", "*", "46", 34, Ink, "Center", true, "MarginTop='6'")
			+ T("@HeaderNote", "*", "26", 17, Brown, "Center", extra: "MarginTop='4'"))
		+ Block("IsVisible='@HasBlock'"));

	// Four steps sharing the width equally (faction entries only).
	private static string StepsBand() => Layer("*", "46",
		Items("Steps", "*", "*", "HorizontalLeftToRight",
			Col("*", "*",
				Row("*", "14",
					Layer("14", "14",
						Box("*", "*", "@DotColor", "IsHidden='@IsNext'")
						+ Box("*", "*", "#B9A784FF", "IsVisible='@IsNext'")
						+ Box("*", "*", Paper, "IsVisible='@IsNext' MarginLeft='2' MarginRight='2' MarginTop='2' MarginBottom='2'"), "VerticalAlignment='Center'")
					+ Box("*", "2", "@LineColor", "IsVisible='@HasLine' VerticalAlignment='Center' MarginLeft='8' MarginRight='8'"))
				+ Row("~", "26",
					T("@Name", "~", "*", 18, Ink2, heading: true, extra: "IsVisible='@IsDone'")
					+ T("@Name", "~", "*", 18, Red, heading: true, extra: "IsVisible='@IsCurrent'")
					+ T("@Name", "~", "*", 18, Dim, heading: true, extra: "IsVisible='@IsNext'")
					+ T("@Note", "~", "*", 15, "#9A4A35FF", extra: "IsVisible='@IsCurrent' MarginLeft='8'")
					+ T("@Note", "~", "*", 15, Brown3, extra: "IsHidden='@IsCurrent' MarginLeft='8'"), "MarginTop='6'"))),
		"IsVisible='@IsFactionTab' MarginTop='10'");

	private static string FiguresBand() => Row("*", "104",
		Figure("派系不满", "@FigureGrievance", "/ 100", "@FigureGrievanceNote", true)
		+ Figure("派系军事实力", "@FigurePower", "@FigurePowerUnit", "", false)
		+ Figure("国王已拒绝", "@FigureRefusals", "@FigureRefusalUnit", "", false)
		+ Layer("*", "*",
			Box("*", "*", "#D3C3A1FF")
			+ Col("*", "~",
				T("近期纪事", "*", "22", 15, Brown3)
				+ T("@FigureSource1", "*", "22", 15, Ink, extra: "MarginTop='4'")
				+ T("@FigureSource2", "*", "22", 15, Brown), "MarginLeft='16' MarginRight='16' MarginTop='8'")),
		"IsVisible='@IsFactionTab' MarginTop='10'");

	private static string Figure(string label, string value, string unit, string note, bool red) => Layer("*", "*",
		Box("*", "*", "#E6D9BCFF")
		+ Col("*", "~",
			T(label, "*", "22", 15, Brown3)
			+ Row("~", "44",
				T(value, "~", "*", 32, red ? Red : Ink, heading: true)
				+ T(unit, "~", "*", 15, Brown, extra: "MarginLeft='6'"))
			+ (note.Length > 0 ? T(note, "*", "20", 14, Brown3) : ""), "MarginLeft='16' MarginRight='16' MarginTop='8'"),
		"MarginRight='16'");

	// Formation conditions + recent chronicle (ledger entry only).
	private static string LedgerBand() => Row("*", "80",
		Col("*", "*",
			T("成派条件", "*", "28", 19, Ink2, heading: true)
			+ Items("Conditions", "*", "28", "HorizontalLeftToRight",
				Row("~", "*",
					Box("9", "9", "@MarkColor", "VerticalAlignment='Center'")
					+ T("@Name", "~", "*", 16, Brown, extra: "MarginLeft='8'")
					+ T("@Value", "~", "*", 16, Ink2, extra: "IsVisible='@IsMet' MarginLeft='6'")
					+ T("@Value", "~", "*", 16, Red, extra: "IsHidden='@IsMet' MarginLeft='6'"), "MarginRight='28'"), "MarginTop='10'"))
		+ Box("1", "56", PaperLine, "VerticalAlignment='Center' MarginRight='36'")
		+ Col("560", "*",
			T("近期纪事", "*", "28", 19, Ink2, heading: true)
			+ T("@SourceLine1", "*", "24", 15, Brown, extra: "MarginTop='4'")
			+ T("@SourceLine2", "*", "24", 15, Brown3)),
		"IsVisible='@IsLedgerTab' MarginTop='10'");

	// Clan table: name stretches; the other columns are fixed so header and rows line up.
	private static string Table() => Col("*", "*",
		Row("*", "32",
			T("@TableTitle", "~", "*", 20, Ink, heading: true)
			+ Layer("*", "*", "")
			+ Row("~", "*",
				Pager("ExecutePrevPage", "上一页", "@HasPrevPage")
				+ T("@PageText", "~", "*", 15, Ink2, extra: "MarginLeft='14' MarginRight='14'")
				+ Pager("ExecuteNextPage", "下一页", "@HasNextPage"), "IsVisible='@HasPages'"))
		+ Layer("*", "32",
			Box("*", "*", "#C9B892FF")
			+ Row("*", "*",
				T("家族", "*", "*", 15, "#5E4A32FF")
				+ T("立场", "170", "*", 15, "#5E4A32FF")
				+ T("主要不满来源", "200", "*", 15, "#5E4A32FF")
				+ T("@GrievanceTitle", "300", "*", 15, "#5E4A32FF")
				+ T("军力", "90", "*", 15, "#5E4A32FF", "Right")
				+ T("与国王", "100", "*", 15, "#5E4A32FF", "Right"), "MarginLeft='12' MarginRight='12'"))
		+ Items("Rows", "*", "~", "VerticalBottomToTop", TableRow()), "MarginTop='10'");

	private static string TableRow() => Layer("*", "36",
		Box("*", "*", "@StripeColor")
		+ Box("3", "*", Red, "IsVisible='@IsPlayer'")
		+ Row("*", "*",
			T("@Name", "*", "*", 18, Ink, heading: true)
			+ T("@Stance", "170", "*", 16, Red, extra: "IsVisible='@IsLeader'")
			+ T("@Stance", "170", "*", 16, Brown, extra: "IsHidden='@IsLeader'")
			+ T("@Reason", "200", "*", 16, Brown)
			+ Row("300", "*",
				Layer(GrievanceTrack.ToString(), "14",
					Box("*", "8", "#C4B18DFF", "VerticalAlignment='Center'")
					+ Box("@BarWidth", "8", "@BarColor", "VerticalAlignment='Center'")
					+ Row("*", "*", Layer("@TickOffset", "*", "") + Box("2", "14", Crimson), "IsVisible='@HasTick'"), "VerticalAlignment='Center'")
				+ T("@GrievanceText", "70", "*", 16, Red, extra: "IsVisible='@IsHigh' MarginLeft='10'")
				+ T("@GrievanceText", "70", "*", 16, Ink, extra: "IsHidden='@IsHigh' MarginLeft='10'"))
			+ T("@PowerText", "90", "*", 16, Ink, "Right")
			+ T("@RelationText", "100", "*", 16, Red, "Right", extra: "IsVisible='@IsNegative'")
			+ T("@RelationText", "100", "*", 16, Olive, "Right", extra: "IsHidden='@IsNegative'"), "MarginLeft='12' MarginRight='12'"));

	private static string Pager(string command, string label, string enabled) =>
		"<ButtonWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' Command.Click='" + command + "' DoNotPassEventsToChildren='true'><Children>"
		+ T(label, "~", "*", 15, "#6D4A30FF", extra: "IsVisible='" + enabled + "'")
		+ T(label, "~", "*", 15, "#A8977AFF", extra: "IsHidden='" + enabled + "'")
		+ "</Children></ButtonWidget>";

	// ---------------------------------------------------------------- identity action bar

	// Agenda entry: role and hint of KingdomAgendaVM only (the vote button sits in the dossier header).
	// Faction entry: role, hint and the role's quoted actions.
	private static string ActionBar() => Layer("*", "92",
		Box("*", "*", BarBg) + Box("*", "1", BarLine)
		+ Scope("Agenda", "@HasSelectedItem", Row("*", "*",
			Seal() + Identity(), "MarginLeft='24' MarginRight='24'"))
		+ Scope("CivilWarFactions", "@HasEntry", Row("*", "*",
			Seal()
			+ Identity()
			+ Items("Actions", "~", "*", "HorizontalLeftToRight", ActionButton(), "VerticalAlignment='Center'"),
			"MarginLeft='24' MarginRight='24'")));

	private static string Seal() => Layer("44", "56", Box("*", "*", "#9A7A48FF") + Image("SealBanner", "*", "*", "MarginLeft='2' MarginRight='2' MarginTop='2' MarginBottom='2'"), "VerticalAlignment='Center'");

	private static string Identity() => Col("*", "~",
		T("@ActionTitle", "*", "30", 21, "#E7D0A5FF", heading: true)
		+ T("@ActionHint", "*", "22", 15, "#9C8A6CFF", extra: "MarginTop='4'"), "VerticalAlignment='Center' MarginLeft='16'");

	private static string ActionButton() => @"
		<ButtonWidget WidthSizePolicy='Fixed' SuggestedWidth='196' HeightSizePolicy='Fixed' SuggestedHeight='62' VerticalAlignment='Center' MarginLeft='12' Brush='ButtonBrush1' Command.Click='ExecuteClick' DoNotPassEventsToChildren='true'>
		  <Children>
		    " + Col("*", "~",
				T("@Label", "*", "30", 21, "#F1DFC1FF", "Center", true, "IsHidden='@IsDim'")
				+ T("@Label", "*", "30", 21, "#9A8B70FF", "Center", true, "IsVisible='@IsDim'")
				+ T("@Sub", "*", "20", 14, "#C6AD86FF", "Center", extra: "IsHidden='@IsDim'")
				+ T("@Sub", "*", "20", 14, "#857760FF", "Center", extra: "IsVisible='@IsDim'"), "VerticalAlignment='Center'") + @"
		  </Children>
		</ButtonWidget>";
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Library;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// ════════════════════════════════════════════════════════════════════════
//  Kingdom screen "派系" tab (v3): the player's own kingdom only. Summary bar
//  (kingdom stability, faction/side counts), one column per faction with its
//  own demand, stage and grievance, and a crown/undecided side list.
//  Built only on tab selection; never per frame.
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
	internal const int StabilityTrackWidth = 520;
	internal const int GrievanceTrackWidth = 300;
	private readonly XmlDocument _document;

	public override InsertType Type => (InsertType)4;

	public KingdomFactionPanelPatch()
	{
		_document = new XmlDocument();
		_document.LoadXml(@"
			<Widget Id='CivilWarFactionPanelRoot' IsVisible='@IsCivilWarFactionSelected' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginTop='188' MarginBottom='75'>
			  <Children>
			    <Widget DataSource='{CivilWarFactions}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='8' MarginRight='8' MarginTop='6' MarginBottom='8'>
			      <Children>
			        <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#120E0AFF' DoNotAcceptEvents='true' />
			        <RichTextWidget IsHidden='@IsAvailable' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='26' Text='@EmptyText' DoNotAcceptEvents='true' />
			        <ListPanel IsVisible='@IsAvailable' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='14' MarginRight='14' MarginTop='12' MarginBottom='12' StackLayout.LayoutMethod='VerticalBottomToTop'>
			          <Children>
			            " + SummaryBar + @"
			            <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginTop='12' StackLayout.LayoutMethod='HorizontalLeftToRight'>
			              <Children>
			                " + FactionColumns + @"
			                " + SideColumn + @"
			              </Children>
			            </ListPanel>
			          </Children>
			        </ListPanel>
			      </Children>
			    </Widget>
			  </Children>
			</Widget>");
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

	// Top bar: kingdom name + stage, kingdom stability meter, faction/side counts.
	private static readonly string SummaryBar = @"
		<Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='104'>
		  <Children>
		    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#1E1811FF' DoNotAcceptEvents='true' />
		    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='1' VerticalAlignment='Bottom' Sprite='BlankWhiteSquare_9' Color='#4A3C28FF' DoNotAcceptEvents='true' />
		    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='24' MarginRight='24' StackLayout.LayoutMethod='HorizontalLeftToRight'>
		      <Children>
		        <ListPanel WidthSizePolicy='Fixed' SuggestedWidth='420' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' StackLayout.LayoutMethod='VerticalBottomToTop'>
		          <Children>
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='22' Brush='Popup.Description.Text' Brush.FontSize='15' Brush.FontColor='#9C8C6CFF' Brush.TextHorizontalAlignment='Left' Text='内战派系 · 本国' />
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='40' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='32' Brush.FontColor='#F1E3BFFF' Brush.TextHorizontalAlignment='Left' Text='@KingdomName' />
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='22' Brush='Popup.Description.Text' Brush.FontSize='16' Brush.FontColor='#FFB59EFF' Brush.TextHorizontalAlignment='Left' Text='@StageText' />
		          </Children>
		        </ListPanel>
		        <Widget WidthSizePolicy='Fixed' SuggestedWidth='1' HeightSizePolicy='Fixed' SuggestedHeight='64' VerticalAlignment='Center' MarginRight='28' Sprite='BlankWhiteSquare_9' Color='#4A3C28FF' DoNotAcceptEvents='true' />
		        <ListPanel WidthSizePolicy='Fixed' SuggestedWidth='" + StabilityTrackWidth + @"' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' MarginRight='28' StackLayout.LayoutMethod='VerticalBottomToTop'>
		          <Children>
		            <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='30' StackLayout.LayoutMethod='HorizontalLeftToRight'>
		              <Children>
		                <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Popup.Description.Text' Brush.FontSize='17' Brush.FontColor='#C9B58AFF' Brush.TextHorizontalAlignment='Left' Text='王国稳定度' />
		                <TextWidget WidthSizePolicy='Fixed' SuggestedWidth='220' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='22' Brush.FontColor='#F1E3BFFF' Brush.TextHorizontalAlignment='Right' Text='@StabilityText' />
		              </Children>
		            </ListPanel>
		            <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='14' MarginTop='6'>
		              <Children>
		                <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#0C0906FF' DoNotAcceptEvents='true' />
		                <Widget WidthSizePolicy='Fixed' SuggestedWidth='@StabilityBarWidth' HeightSizePolicy='StretchToParent' MarginTop='1' MarginBottom='1' Sprite='BlankWhiteSquare_9' Color='@StabilityColor' DoNotAcceptEvents='true' />
		              </Children>
		            </Widget>
		          </Children>
		        </ListPanel>
		        <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' DoNotAcceptEvents='true' />
		        <Widget WidthSizePolicy='Fixed' SuggestedWidth='1' HeightSizePolicy='Fixed' SuggestedHeight='64' VerticalAlignment='Center' MarginRight='20' Sprite='BlankWhiteSquare_9' Color='#4A3C28FF' DoNotAcceptEvents='true' />
		        " + Stat("FactionCount", "成形派系", "#F1E3BFFF") + Stat("OppositionCount", "派系家族", "#D9624EFF") + Stat("CrownCount", "王室派", "#D7B45EFF") + Stat("MiddleCount", "未表态", "#A79E8CFF") + @"
		      </Children>
		    </ListPanel>
		  </Children>
		</Widget>";

	private static string Stat(string binding, string label, string color) => @"
		<ListPanel WidthSizePolicy='Fixed' SuggestedWidth='96' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' StackLayout.LayoutMethod='VerticalBottomToTop'>
		  <Children>
		    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='38' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='30' Brush.FontColor='" + color + @"' Brush.TextHorizontalAlignment='Center' Text='@" + binding + @"' />
		    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='20' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.FontColor='#9C8C6CFF' Brush.TextHorizontalAlignment='Center' Text='" + label + @"' />
		  </Children>
		</ListPanel>";

	// One column per faction (up to 4); each shows its own demand, stage, grievance and members.
	private static readonly string FactionColumns = @"
		<Widget IsVisible='@HasFactions' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
		  <Children>
		<ListPanel DataSource='{Factions}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='HorizontalLeftToRight'>
		  <ItemTemplate>
		    <Widget WidthSizePolicy='Fixed' SuggestedWidth='@ColumnWidth' HeightSizePolicy='StretchToParent' MarginRight='12' ClipContents='true'>
		      <Children>
		        <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='@BackColor' DoNotAcceptEvents='true' />
		        <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='5' Sprite='BlankWhiteSquare_9' Color='@Color' DoNotAcceptEvents='true' />
		        <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='18' MarginRight='18' MarginTop='16' MarginBottom='12' StackLayout.LayoutMethod='VerticalBottomToTop'>
		          <Children>
		            <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='28' StackLayout.LayoutMethod='HorizontalLeftToRight'>
		              <Children>
		                <Widget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent'>
		                  <Children>
		                    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='@Color' DoNotAcceptEvents='true' />
		                    <TextWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' MarginLeft='10' MarginRight='10' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='16' Brush.FontColor='#FFF4E0FF' Text='@Tag' />
		                  </Children>
		                </Widget>
		                <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='10' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.FontColor='#E6D3AEFF' Brush.TextHorizontalAlignment='Right' Text='@Stage' />
		              </Children>
		            </ListPanel>
		            <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='10' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='21' Brush.FontColor='#FFF1D6FF' Brush.TextHorizontalAlignment='Left' Text='@Name' />
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='22' MarginTop='4' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.FontColor='#C9B58AFF' Brush.TextHorizontalAlignment='Left' Text='@Leader' />
		            <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='12'>
		              <Children>
		                <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#00000066' DoNotAcceptEvents='true' />
		                <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginLeft='12' MarginRight='12' MarginTop='10' MarginBottom='10' StackLayout.LayoutMethod='VerticalBottomToTop'>
		                  <Children>
		                    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='20' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.FontColor='#D7B45EFF' Brush.TextHorizontalAlignment='Left' Text='诉 求' />
		                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='4' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='22' Brush.FontColor='#F6E7C4FF' Brush.TextHorizontalAlignment='Left' Text='@Demand' />
		                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='4' Brush='Popup.Description.Text' Brush.FontSize='13' Brush.FontColor='#A8977AFF' Brush.TextHorizontalAlignment='Left' Text='@Goal' />
		                  </Children>
		                </ListPanel>
		              </Children>
		            </Widget>
		            <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='24' MarginTop='12' StackLayout.LayoutMethod='HorizontalLeftToRight'>
		              <Children>
		                <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Popup.Description.Text' Brush.FontSize='15' Brush.FontColor='#C9B58AFF' Brush.TextHorizontalAlignment='Left' Text='派系不满' />
		                <TextWidget WidthSizePolicy='Fixed' SuggestedWidth='90' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='18' Brush.FontColor='#F1E3BFFF' Brush.TextHorizontalAlignment='Right' Text='@GrievanceText' />
		              </Children>
		            </ListPanel>
		            <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='8' MarginTop='4'>
		              <Children>
		                <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#0C0906FF' DoNotAcceptEvents='true' />
		                <Widget WidthSizePolicy='Fixed' SuggestedWidth='@GrievanceBarWidth' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='@Color' DoNotAcceptEvents='true' />
		              </Children>
		            </Widget>
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='22' MarginTop='10' Brush='Popup.Description.Text' Brush.FontSize='15' Brush.FontColor='#E8DCC4FF' Brush.TextHorizontalAlignment='Left' Text='@Power' />
		            <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='2' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.FontColor='#B7A88CFF' Brush.TextHorizontalAlignment='Left' Text='@Refusal' />
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='22' MarginTop='12' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.FontColor='#8F7F62FF' Brush.TextHorizontalAlignment='Left' Text='@MembersTitle' />
		            " + ClanList("Members") + @"
		          </Children>
		        </ListPanel>
		      </Children>
		    </Widget>
	  </ItemTemplate>
		</ListPanel>
		  </Children>
		</Widget>
		<Widget IsHidden='@HasFactions' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginRight='12'>
		  <Children>
		    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#1A140EFF' DoNotAcceptEvents='true' />
		    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='24' Text='@NoFactionText' DoNotAcceptEvents='true' />
		  </Children>
		</Widget>";

	// Right column: crown side and undecided clans (the player is listed among them).
	private static readonly string SideColumn = @"
		<ListPanel WidthSizePolicy='Fixed' SuggestedWidth='300' HeightSizePolicy='StretchToParent' ClipContents='true' StackLayout.LayoutMethod='VerticalBottomToTop'>
		  <Children>
		    " + SideBox("CrownTitle", "Crown", "#D7B45EFF") + @"
		    " + SideBox("MiddleTitle", "Middle", "#A79E8CFF") + @"
		  </Children>
		</ListPanel>";

	private static string SideBox(string titleBinding, string listBinding, string color) => @"
		<Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginBottom='12'>
		  <Children>
		    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#1A140EFF' DoNotAcceptEvents='true' />
		    <Widget WidthSizePolicy='Fixed' SuggestedWidth='4' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='" + color + @"' DoNotAcceptEvents='true' />
		    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginLeft='16' MarginRight='12' MarginTop='12' MarginBottom='12' StackLayout.LayoutMethod='VerticalBottomToTop'>
		      <Children>
		        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='26' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='18' Brush.FontColor='" + color + @"' Brush.TextHorizontalAlignment='Left' Text='@" + titleBinding + @"' />
		        " + ClanList(listBinding) + @"
		      </Children>
		    </ListPanel>
		  </Children>
		</Widget>";

	// Clan rows, one compact line (30px incl. gap): name (+ leader star), info, and a small grievance bar on the right.
	// Row counts are capped in KingdomCivilWarOwner (PanelMemberLimit / PanelSideLimit) so the lists fit without scrolling.
	internal const int ClanBarTrack = 56;
	private static string ClanList(string binding) => @"
		<ListPanel DataSource='{" + binding + @"}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='2' StackLayout.LayoutMethod='VerticalBottomToTop'>
		  <ItemTemplate>
		    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='27' MarginTop='3'>
		      <Children>
		        <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#0000004D' DoNotAcceptEvents='true' />
		        <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='10' MarginRight='10' StackLayout.LayoutMethod='HorizontalLeftToRight'>
		          <Children>
		            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='16' Brush.FontColor='#F1E3BFFF' Brush.TextHorizontalAlignment='Left' Text='@Name' />
		            <TextWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' MarginLeft='6' Brush='Popup.Description.Text' Brush.FontSize='13' Brush.FontColor='#B7A88CFF' Brush.TextHorizontalAlignment='Right' Text='@Info' />
		            <Widget IsVisible='@HasGrievance' WidthSizePolicy='Fixed' SuggestedWidth='" + ClanBarTrack + @"' HeightSizePolicy='Fixed' SuggestedHeight='5' VerticalAlignment='Center' MarginLeft='8'>
		              <Children>
		                <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#0C0906FF' DoNotAcceptEvents='true' />
		                <Widget WidthSizePolicy='Fixed' SuggestedWidth='@BarWidth' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='@BarColor' DoNotAcceptEvents='true' />
		              </Children>
		            </Widget>
		          </Children>
		        </ListPanel>
		      </Children>
		    </Widget>
		  </ItemTemplate>
		</ListPanel>";
}

public sealed class KingdomFactionClanVM : ViewModel
{
	private const int BarTrack = KingdomFactionPanelPatch.ClanBarTrack;
	private readonly CivilWarPanelClan _clan;
	private readonly string _barColor;
	private readonly bool _showGrievance;

	internal KingdomFactionClanVM(CivilWarPanelClan clan, string barColor, bool showGrievance)
	{
		_clan = clan ?? new CivilWarPanelClan();
		_barColor = string.IsNullOrWhiteSpace(barColor) ? GrievanceColor(_clan.Grievance) : barColor;
		_showGrievance = showGrievance;
	}

	[DataSourceProperty] public string Name => _clan.IsLeader ? _clan.Name + " ★" : _clan.Name;
	// Faction members: "<main source> <points>"; side lists: relation to the king.
	[DataSourceProperty] public string Info => _showGrievance ? ((_clan.Info ?? "") + " " + _clan.Grievance).Trim() : _clan.Info;
	[DataSourceProperty] public bool HasGrievance => _showGrievance && _clan.Grievance > 0;
	[DataSourceProperty] public int BarWidth => Math.Max(2, Math.Min(BarTrack, _clan.Grievance * BarTrack / 100));
	[DataSourceProperty] public Color BarColor => Color.ConvertStringToColor(_barColor);

	internal static string GrievanceColor(int grievance) => grievance >= 60 ? "#C0492FFF" : grievance >= 35 ? "#C08A3AFF" : "#6E6552FF";
}

public sealed class KingdomFactionColumnVM : ViewModel
{
	private readonly CivilWarPanelFaction _faction;
	private readonly int _columnWidth;
	private readonly MBBindingList<KingdomFactionClanVM> _members = new MBBindingList<KingdomFactionClanVM>();

	internal KingdomFactionColumnVM(CivilWarPanelFaction faction, int columnWidth)
	{
		_faction = faction ?? new CivilWarPanelFaction();
		_columnWidth = columnWidth;
		foreach (CivilWarPanelClan clan in _faction.Members ?? new List<CivilWarPanelClan>()) _members.Add(new KingdomFactionClanVM(clan, _faction.Color, true));
	}

	[DataSourceProperty] public int ColumnWidth => _columnWidth;
	[DataSourceProperty] public string Tag => _faction.Tag;
	[DataSourceProperty] public string Name => _faction.Name;
	[DataSourceProperty] public TaleWorlds.Library.Color Color => TaleWorlds.Library.Color.ConvertStringToColor(_faction.Color);
	// Same hue as the faction, very dark, so each column reads as its own faction.
	[DataSourceProperty] public TaleWorlds.Library.Color BackColor => TaleWorlds.Library.Color.ConvertStringToColor(Darken(_faction.Color));
	[DataSourceProperty] public string Stage => _faction.Stage;
	[DataSourceProperty] public string Leader => _faction.Leader;
	[DataSourceProperty] public string Demand => _faction.Demand;
	[DataSourceProperty] public string Goal => _faction.Goal;
	[DataSourceProperty] public string Power => _faction.Power;
	[DataSourceProperty] public string Refusal => _faction.Refusal;
	[DataSourceProperty] public string GrievanceText => _faction.Grievance + " / 100";
	[DataSourceProperty] public int GrievanceBarWidth => Math.Max(2, (_columnWidth - 36) * Math.Max(0, Math.Min(100, _faction.Grievance)) / 100);
	[DataSourceProperty] public string MembersTitle => "成员 " + _faction.MemberCount + " 家" + (_faction.MemberCount > _members.Count ? "（显示前 " + _members.Count + " 家）" : "");
	[DataSourceProperty] public MBBindingList<KingdomFactionClanVM> Members => _members;

	private static string Darken(string color)
	{
		string hex = (color ?? "").TrimStart('#');
		if (hex.Length < 6) return "#1C150FFF";
		try
		{
			int r = Convert.ToInt32(hex.Substring(0, 2), 16), g = Convert.ToInt32(hex.Substring(2, 2), 16), b = Convert.ToInt32(hex.Substring(4, 2), 16);
			// 22% of the faction colour over the panel background #120E0A.
			int Mix(int c, int bg) => (int)Math.Round(bg + (c - bg) * 0.22);
			return "#" + Mix(r, 0x12).ToString("X2") + Mix(g, 0x0E).ToString("X2") + Mix(b, 0x0A).ToString("X2") + "FF";
		}
		catch
		{
			return "#1C150FFF";
		}
	}
}

public sealed class KingdomFactionPanelVM : ViewModel
{
	// Content width left for faction columns: kingdom screen width minus side column, margins and gaps.
	private const int ColumnsAreaWidth = 1480;
	private const int MaxColumnWidth = 520;
	private readonly MBBindingList<KingdomFactionColumnVM> _factions = new MBBindingList<KingdomFactionColumnVM>();
	private readonly MBBindingList<KingdomFactionClanVM> _crown = new MBBindingList<KingdomFactionClanVM>();
	private readonly MBBindingList<KingdomFactionClanVM> _middle = new MBBindingList<KingdomFactionClanVM>();
	private CivilWarPanelKingdom _panel = new CivilWarPanelKingdom();

	[DataSourceProperty] public bool IsAvailable => _panel.Available;
	[DataSourceProperty] public string EmptyText => _panel.EmptyText;
	[DataSourceProperty] public string KingdomName => _panel.Name;
	[DataSourceProperty] public string StageText => _panel.StageText;
	[DataSourceProperty] public string StabilityText => _panel.Stability + " / 100 · " + _panel.StabilityTier;
	[DataSourceProperty] public int StabilityBarWidth => Math.Max(2, (KingdomFactionPanelPatch.StabilityTrackWidth - 2) * Math.Max(0, Math.Min(100, _panel.Stability)) / 100);
	[DataSourceProperty] public Color StabilityColor => Color.ConvertStringToColor(_panel.Stability >= 60 ? "#8FA85AFF" : _panel.Stability >= 35 ? "#C8963CFF" : "#C0492FFF");
	[DataSourceProperty] public string FactionCount => _factions.Count.ToString();
	[DataSourceProperty] public string OppositionCount => _panel.OppositionCount.ToString();
	[DataSourceProperty] public string CrownCount => _panel.CrownCount.ToString();
	[DataSourceProperty] public string MiddleCount => _panel.MiddleCount.ToString();
	[DataSourceProperty] public bool HasFactions => _factions.Count > 0;
	[DataSourceProperty] public string NoFactionText => _panel.StageText;
	[DataSourceProperty] public string CrownTitle => "王室派 · " + _panel.CrownCount + " 家";
	[DataSourceProperty] public string MiddleTitle => "未表态 · " + _panel.MiddleCount + " 家";
	[DataSourceProperty] public MBBindingList<KingdomFactionColumnVM> Factions => _factions;
	[DataSourceProperty] public MBBindingList<KingdomFactionClanVM> Crown => _crown;
	[DataSourceProperty] public MBBindingList<KingdomFactionClanVM> Middle => _middle;

	// Called on tab selection only.
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
		_factions.Clear();
		_crown.Clear();
		_middle.Clear();
		int count = Math.Max(1, _panel.Factions.Count);
		int width = Math.Min(MaxColumnWidth, (ColumnsAreaWidth - 12 * count) / count);
		foreach (CivilWarPanelFaction faction in _panel.Factions) _factions.Add(new KingdomFactionColumnVM(faction, width));
		foreach (CivilWarPanelClan clan in _panel.Crown) _crown.Add(new KingdomFactionClanVM(clan, null, false));
		foreach (CivilWarPanelClan clan in _panel.Middle) _middle.Add(new KingdomFactionClanVM(clan, null, false));
		foreach (string name in new[] { nameof(IsAvailable), nameof(EmptyText), nameof(KingdomName), nameof(StageText), nameof(StabilityText), nameof(StabilityBarWidth), nameof(StabilityColor), nameof(FactionCount), nameof(OppositionCount), nameof(CrownCount), nameof(MiddleCount), nameof(HasFactions), nameof(NoFactionText), nameof(CrownTitle), nameof(MiddleTitle) })
			OnPropertyChanged(name);
	}
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

		CivilWarFactions.Refresh();
		IsCivilWarFactionSelected = true;
		ViewModel.OnPropertyChangedWithValue(true, nameof(IsCivilWarFactionSelected));
	}

	internal void ClearSelection()
	{
		if (!IsCivilWarFactionSelected) return;
		IsCivilWarFactionSelected = false;
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

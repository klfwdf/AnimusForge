using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class SailToTheGulfOfCharasQuest : NavalStorylineQuestBase
{
	private const string LaharShipHullId = "ship_liburna_q2_storyline";

	private static readonly Dictionary<string, string> LaharShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl3" },
		{ "sail", "sails_lvl2" },
		{ "bow", "bow_northern_reinforced_ram_lvl3" }
	};

	private const string GunnarShipHullId = "northern_medium_ship";

	private static readonly Dictionary<string, string> GunnarShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	[SaveableField(1)]
	private readonly CampaignVec2 _corsairSpawnPosition;

	[SaveableField(2)]
	private readonly MapMarker _corsairHuntingGroundMarker;

	[SaveableField(3)]
	private bool _willProgressStoryline;

	public override bool WillProgressStoryline => _willProgressStoryline;

	public override TextObject Title => new TextObject("{=LMRgfeFC}Sail to the Gulf of Charas");

	private TextObject QuestStartLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=7i9UFPLB}Find {HERO.NAME} in her hunting grounds in the Gulf of Charas");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.EmiraAlFahda.CharacterObject);
			return textObject;
		}
	}

	private TextObject QuestSuccessLogText => new TextObject("{=lY5770ox}You found the corsairs.");

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest2;

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_2_main_party_template";

	public SailToTheGulfOfCharasQuest(string questId, Hero questGiver, CampaignVec2 corsairSpawnPosition)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_corsairSpawnPosition = corsairSpawnPosition;
		_willProgressStoryline = true;
		_corsairHuntingGroundMarker = Campaign.Current.MapTrackerManager.CreateMapMarker(NavalStorylineData.CorsairBanner, new TextObject("{=QLrwlirp}Corsair Hunting Grounds"), _corsairSpawnPosition.AsVec3(), isVisibleOnMap: true, base.StringId);
	}

	protected override void SetDialogs()
	{
	}

	protected override void OnStartQuestInternal()
	{
		InitializeQuestParty();
		AddLog(QuestStartLogText);
		AddTrackedObject(_corsairHuntingGroundMarker);
	}

	protected override void HourlyTick()
	{
		if (MobileParty.MainParty.SeeingRange > _corsairSpawnPosition.Distance(MobileParty.MainParty.Position))
		{
			AddLog(QuestSuccessLogText);
			_corsairHuntingGroundMarker.IsVisibleOnMap = false;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			new HuntDownTheEmiraAlFahdaAndTheCorsairsQuest("naval_storyline_act3_quest2_2", NavalStorylineData.Gunnar, _corsairSpawnPosition).StartQuest();
			TextObject textObject = new TextObject("{=tBigbw3U}You have reached the Gulf of Charas. Winds whip across the waves, carrying dust from the deserts, and visibility comes and goes. Lahar's ship keeps station several bowshots off of your port side, and together you comb the seas for the corsairs.");
			InformationManager.ShowInquiry(new InquiryData("", textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_continue").ToString(), GameTexts.FindText("str_no").ToString(), null, null));
			_willProgressStoryline = false;
			CompleteQuestWithSuccess();
		}
	}

	protected override void IsNavalQuestPartyInternal(PartyBase party, NavalStorylinePartyData data)
	{
		if (party == PartyBase.MainParty)
		{
			data.PartySize++;
		}
	}

	protected override void RegisterEventsInternal()
	{
	}

	protected override void OnFinalizeInternal()
	{
	}

	protected override void OnCanceledInternal()
	{
		EnterSettlementAction.ApplyForCharacterOnly(NavalStorylineData.Lahar, NavalStorylineData.HomeSettlement);
		NavalStorylineData.Lahar.Heal(NavalStorylineData.Lahar.MaxHitPoints);
	}

	private void InitializeQuestParty()
	{
		NavalStorylineData.Lahar.ChangeState(Hero.CharacterStates.Active);
		NavalStorylineData.Lahar.Heal(NavalStorylineData.Lahar.MaxHitPoints);
		AddHeroToPartyAction.Apply(NavalStorylineData.Lahar, MobileParty.MainParty);
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.ShipHull.StringId == "ship_liburna_q2_storyline")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Hawk);
				AddShipUpgradePieces(ship, LaharShipUpgradePieces);
			}
			else if (ship.ShipHull.StringId == "northern_medium_ship")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Dragon);
				AddShipUpgradePieces(ship, GunnarShipUpgradePieces);
			}
		}
	}

	private void AddShipUpgradePieces(Ship ship, Dictionary<string, string> upgradePieces)
	{
		foreach (KeyValuePair<string, string> kv in upgradePieces)
		{
			ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(kv.Value);
			if (ship.ShipHull.AvailableSlots.Any((KeyValuePair<string, ShipSlot> slot) => slot.Key == kv.Key))
			{
				ship.EquipUpgradePiece(kv.Key, newUpgradePiece);
			}
		}
	}
}

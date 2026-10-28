using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class GoToSkatriaIslandsQuest : NavalStorylineQuestBase
{
	private static readonly Dictionary<string, string> PlayerShipUpgradePieces = new Dictionary<string, string>
	{
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl2" }
	};

	[SaveableField(1)]
	private CampaignVec2 _corsairSpawnPosition;

	[SaveableField(2)]
	private readonly MapMarker _skatriaIslandMarker;

	[SaveableField(3)]
	private bool _willProgressStoryline;

	public override TextObject Title => new TextObject("{=HEpykTDR}Go to the Skatria Islands");

	private TextObject QuestSuccessLogText => new TextObject("{=U6O5y26b}You found the Skatria Islands.");

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest4;

	public override bool WillProgressStoryline => _willProgressStoryline;

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_4_main_party_template";

	private TextObject QuestStartLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=5ygak6Ob}Sail to the Skatria Islands off {SETTLEMENT_NAME}");
			textObject.SetTextVariable("SETTLEMENT_NAME", NavalStorylineData.Act3Quest4TargetSettlement.Name);
			return textObject;
		}
	}

	public GoToSkatriaIslandsQuest(string questId, Hero questGiver, CampaignVec2 corsairSpawnPosition)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_corsairSpawnPosition = corsairSpawnPosition;
		_willProgressStoryline = true;
		_skatriaIslandMarker = Campaign.Current.MapTrackerManager.CreateMapMarker(NavalStorylineData.CorsairBanner, new TextObject("{=9EIh8xRM}Skatria Islands"), _corsairSpawnPosition.AsVec3(), isVisibleOnMap: true, base.StringId);
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
	}

	protected override void SetDialogs()
	{
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
	}

	protected override void OnStartQuestInternal()
	{
		InitializeQuestParty();
		AddLog(QuestStartLogText);
		_skatriaIslandMarker.IsVisibleOnMap = true;
	}

	protected override void OnFinalizeInternal()
	{
		base.OnFinalizeInternal();
		_skatriaIslandMarker.IsVisibleOnMap = false;
	}

	private void InitializeQuestParty()
	{
		NavalStorylineData.Bjolgur.ChangeState(Hero.CharacterStates.Active);
		AddHeroToPartyAction.Apply(NavalStorylineData.Bjolgur, MobileParty.MainParty);
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			foreach (KeyValuePair<string, string> playerShipUpgradePiece in PlayerShipUpgradePieces)
			{
				if (ship.HasSlot(playerShipUpgradePiece.Key))
				{
					ship.EquipUpgradePiece(playerShipUpgradePiece.Key, MBObjectManager.Instance.GetObject<ShipUpgradePiece>(playerShipUpgradePiece.Value));
				}
			}
			ship.ChangeFigurehead(DefaultFigureheads.Raven);
		}
		MobileParty.MainParty.Ships.FirstOrDefault()?.ChangeFigurehead(DefaultFigureheads.Dragon);
	}

	private void OnTick(float deltaTime)
	{
		if (MobileParty.MainParty.SeeingRange + 5f > _corsairSpawnPosition.Distance(MobileParty.MainParty.Position) && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(CaptureTheImperialMerchantPrusas)))
		{
			AddLog(QuestSuccessLogText);
			_willProgressStoryline = false;
			_skatriaIslandMarker.IsVisibleOnMap = false;
			CompleteQuestWithSuccess();
			new CaptureTheImperialMerchantPrusas("naval_storyline_act3_quest4_2", NavalStorylineData.Gunnar, _corsairSpawnPosition).StartQuest();
		}
	}
}

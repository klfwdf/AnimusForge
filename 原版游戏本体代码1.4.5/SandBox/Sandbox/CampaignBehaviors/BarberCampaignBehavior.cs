using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors;

public class BarberCampaignBehavior : CampaignBehaviorBase, IFacegenCampaignBehavior, ICampaignBehavior
{
	private class BarberFaceGeneratorCustomFilter : IFaceGeneratorCustomFilter
	{
		private readonly int[] _haircutIndices;

		private readonly int[] _facialHairIndices;

		private readonly bool _defaultStages;

		public BarberFaceGeneratorCustomFilter(bool useDefaultStages, int[] haircutIndices, int[] faircutIndices)
		{
			_haircutIndices = haircutIndices;
			_facialHairIndices = faircutIndices;
			_defaultStages = useDefaultStages;
		}

		public int[] GetHaircutIndices(BasicCharacterObject character)
		{
			return _haircutIndices;
		}

		public int[] GetFacialHairIndices(BasicCharacterObject character)
		{
			return _facialHairIndices;
		}

		public FaceGeneratorStage[] GetAvailableStages()
		{
			if (!_defaultStages)
			{
				return new FaceGeneratorStage[1] { FaceGeneratorStage.Hair };
			}
			return new FaceGeneratorStage[7]
			{
				FaceGeneratorStage.Body,
				FaceGeneratorStage.Face,
				FaceGeneratorStage.Eyes,
				FaceGeneratorStage.Nose,
				FaceGeneratorStage.Mouth,
				FaceGeneratorStage.Hair,
				FaceGeneratorStage.Taint
			};
		}
	}

	private const int BarberCost = 100;

	private bool _isOpenedFromBarberDialogue;

	private StaticBodyProperties _previousBodyProperties;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	public override void SyncData(IDataStore store)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("barber_start_talk_beggar", "start", "close_window", "{=pWzdxd7O}May the Heavens bless you, my poor {?PLAYER.GENDER}lady{?}fellow{\\?}, but I can't spare a coin right now.", InDisguiseSpeakingToBarber, InitializeBarberConversation);
		campaignGameStarter.AddDialogLine("barber_start_talk", "start", "barber_question1", "{=2aXYYNBG}Come to have your hair cut, {?PLAYER.GENDER}my lady{?}my lord{\\?}? A new look for a new day?", IsConversationAgentBarber, InitializeBarberConversation);
		campaignGameStarter.AddPlayerLine("player_accept_haircut", "barber_question1", "start_cut_token", "{=Q7wBRXtR}Yes, I have. ({GOLD_COST} {GOLD_ICON})", GivePlayerAHaircutCondition, GivePlayerAHaircut, 100, DoesPlayerHaveEnoughGold);
		campaignGameStarter.AddPlayerLine("player_refuse_haircut", "barber_question1", "no_haircut_conversation_token", "{=xPAAZAaI}My hair is fine as it is, thank you.", null, null);
		campaignGameStarter.AddDialogLine("barber_ask_if_done", "start_cut_token", "finish_cut_token", "{=M3K8wUOO}So... Does this please you, {?PLAYER.GENDER}my lady{?}my lord{\\?}?", null, null);
		campaignGameStarter.AddPlayerLine("player_done_with_haircut", "finish_cut_token", "finish_barber", "{=zTF4bJm0}Yes, it's fine.", null, null);
		campaignGameStarter.AddPlayerLine("player_not_done_with_haircut", "finish_cut_token", "start_cut_token", "{=BnoSOi3r}Actually...", GivePlayerAHaircutCondition, GivePlayerAHaircut, 100, DoesPlayerHaveEnoughGold);
		campaignGameStarter.AddDialogLine("barber_no_haircut_talk", "no_haircut_conversation_token", "close_window", "{=BusYGTrN}Excellent! Have a good day, then, {?PLAYER.GENDER}my lady{?}my lord{\\?}.", null, null);
		campaignGameStarter.AddDialogLine("barber_haircut_finished", "finish_barber", "player_had_a_haircut_token", "{=akqJbZpH}Marvellous! You cut a splendid appearance, {?PLAYER.GENDER}my lady{?}my lord{\\?}, if you don't mind my saying. Most splendid.", DidPlayerHaveAHaircut, ChargeThePlayer);
		campaignGameStarter.AddDialogLine("barber_haircut_no_change", "finish_barber", "player_did_not_cut_token", "{=yLIZlaS1}Very well. Do come back when you're ready, {?PLAYER.GENDER}my lady{?}my lord{\\?}.", DidPlayerNotHaveAHaircut, null);
		campaignGameStarter.AddPlayerLine("player_no_haircut_finish_talk", "player_did_not_cut_token", "close_window", "{=oPUVNuhN}I'll keep you in mind", null, null);
		campaignGameStarter.AddPlayerLine("player_haircut_finish_talk", "player_had_a_haircut_token", "close_window", "{=F9Xjbchh}Thank you.", null, null);
	}

	private bool InDisguiseSpeakingToBarber()
	{
		if (IsConversationAgentBarber())
		{
			return Campaign.Current.IsMainHeroDisguised;
		}
		return false;
	}

	private bool DoesPlayerHaveEnoughGold(out TextObject explanation)
	{
		if (Hero.MainHero.Gold < 100)
		{
			explanation = new TextObject("{=RYJdU43V}Not Enough Gold");
			return false;
		}
		explanation = null;
		return true;
	}

	private void ChargeThePlayer()
	{
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 100);
	}

	private bool DidPlayerNotHaveAHaircut()
	{
		return !DidPlayerHaveAHaircut();
	}

	private bool DidPlayerHaveAHaircut()
	{
		return Hero.MainHero.BodyProperties.StaticProperties != _previousBodyProperties;
	}

	private bool IsConversationAgentBarber()
	{
		return Settlement.CurrentSettlement?.Culture.Barber == CharacterObject.OneToOneConversationCharacter;
	}

	private bool GivePlayerAHaircutCondition()
	{
		MBTextManager.SetTextVariable("GOLD_COST", 100);
		return true;
	}

	private void GivePlayerAHaircut()
	{
		_isOpenedFromBarberDialogue = true;
		BarberState gameState = Game.Current.GameStateManager.CreateState<BarberState>(new object[2]
		{
			Hero.MainHero.CharacterObject,
			GetFaceGenFilter()
		});
		_isOpenedFromBarberDialogue = false;
		GameStateManager.Current.PushState(gameState);
	}

	private void InitializeBarberConversation()
	{
		_previousBodyProperties = Hero.MainHero.BodyProperties.StaticProperties;
	}

	private LocationCharacter CreateBarber(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject barber = culture.Barber;
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(barber, out var minimumAge, out var maximumAge, "Barber");
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(barber)).Monster(FaceGen.GetMonsterWithSuffix(barber.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_barber", fixedLocation: true, relation, null, useCivilianEquipment: true);
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
		if (CampaignMission.Current.Location == locationWithId && Campaign.Current.IsDay && unusedUsablePointCount.TryGetValue("sp_merchant_notary", out var _))
		{
			locationWithId.AddLocationCharacters(CreateBarber, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
		}
	}

	public IFaceGeneratorCustomFilter GetFaceGenFilter()
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		if (Settlement.CurrentSettlement != null)
		{
			list.AddRange(Campaign.Current.Models.BodyPropertiesModel.GetHairIndicesForCulture(Hero.MainHero.CharacterObject.Race, Hero.MainHero.IsFemale ? 1 : 0, Hero.MainHero.Age, Settlement.CurrentSettlement.Culture));
			list2.AddRange(Campaign.Current.Models.BodyPropertiesModel.GetBeardIndicesForCulture(Hero.MainHero.CharacterObject.Race, Hero.MainHero.IsFemale ? 1 : 0, Hero.MainHero.Age, Settlement.CurrentSettlement.Culture));
		}
		else
		{
			foreach (CultureObject objectType in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
			{
				list.AddRange(Campaign.Current.Models.BodyPropertiesModel.GetHairIndicesForCulture(Hero.MainHero.CharacterObject.Race, Hero.MainHero.IsFemale ? 1 : 0, Hero.MainHero.Age, objectType));
				list2.AddRange(Campaign.Current.Models.BodyPropertiesModel.GetBeardIndicesForCulture(Hero.MainHero.CharacterObject.Race, Hero.MainHero.IsFemale ? 1 : 0, Hero.MainHero.Age, objectType));
			}
		}
		return new BarberFaceGeneratorCustomFilter(!_isOpenedFromBarberDialogue, list.Distinct().ToArray(), list2.Distinct().ToArray());
	}
}

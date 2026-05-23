using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000CA RID: 202
	public class BarberCampaignBehavior : CampaignBehaviorBase, IFacegenCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x060008B3 RID: 2227 RVA: 0x0003FFF2 File Offset: 0x0003E1F2
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x060008B4 RID: 2228 RVA: 0x00040022 File Offset: 0x0003E222
		public override void SyncData(IDataStore store)
		{
		}

		// Token: 0x060008B5 RID: 2229 RVA: 0x00040024 File Offset: 0x0003E224
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x060008B6 RID: 2230 RVA: 0x00040030 File Offset: 0x0003E230
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("barber_start_talk_beggar", "start", "close_window", "{=pWzdxd7O}May the Heavens bless you, my poor {?PLAYER.GENDER}lady{?}fellow{\\?}, but I can't spare a coin right now.", new ConversationSentence.OnConditionDelegate(this.InDisguiseSpeakingToBarber), new ConversationSentence.OnConsequenceDelegate(this.InitializeBarberConversation), 100, null);
			campaignGameStarter.AddDialogLine("barber_start_talk", "start", "barber_question1", "{=2aXYYNBG}Come to have your hair cut, {?PLAYER.GENDER}my lady{?}my lord{\\?}? A new look for a new day?", new ConversationSentence.OnConditionDelegate(this.IsConversationAgentBarber), new ConversationSentence.OnConsequenceDelegate(this.InitializeBarberConversation), 100, null);
			campaignGameStarter.AddPlayerLine("player_accept_haircut", "barber_question1", "start_cut_token", "{=Q7wBRXtR}Yes, I have. ({GOLD_COST} {GOLD_ICON})", new ConversationSentence.OnConditionDelegate(this.GivePlayerAHaircutCondition), new ConversationSentence.OnConsequenceDelegate(this.GivePlayerAHaircut), 100, new ConversationSentence.OnClickableConditionDelegate(this.DoesPlayerHaveEnoughGold), null);
			campaignGameStarter.AddPlayerLine("player_refuse_haircut", "barber_question1", "no_haircut_conversation_token", "{=xPAAZAaI}My hair is fine as it is, thank you.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("barber_ask_if_done", "start_cut_token", "finish_cut_token", "{=M3K8wUOO}So... Does this please you, {?PLAYER.GENDER}my lady{?}my lord{\\?}?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("player_done_with_haircut", "finish_cut_token", "finish_barber", "{=zTF4bJm0}Yes, it's fine.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("player_not_done_with_haircut", "finish_cut_token", "start_cut_token", "{=BnoSOi3r}Actually...", new ConversationSentence.OnConditionDelegate(this.GivePlayerAHaircutCondition), new ConversationSentence.OnConsequenceDelegate(this.GivePlayerAHaircut), 100, new ConversationSentence.OnClickableConditionDelegate(this.DoesPlayerHaveEnoughGold), null);
			campaignGameStarter.AddDialogLine("barber_no_haircut_talk", "no_haircut_conversation_token", "close_window", "{=BusYGTrN}Excellent! Have a good day, then, {?PLAYER.GENDER}my lady{?}my lord{\\?}.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("barber_haircut_finished", "finish_barber", "player_had_a_haircut_token", "{=akqJbZpH}Marvellous! You cut a splendid appearance, {?PLAYER.GENDER}my lady{?}my lord{\\?}, if you don't mind my saying. Most splendid.", new ConversationSentence.OnConditionDelegate(this.DidPlayerHaveAHaircut), new ConversationSentence.OnConsequenceDelegate(this.ChargeThePlayer), 100, null);
			campaignGameStarter.AddDialogLine("barber_haircut_no_change", "finish_barber", "player_did_not_cut_token", "{=yLIZlaS1}Very well. Do come back when you're ready, {?PLAYER.GENDER}my lady{?}my lord{\\?}.", new ConversationSentence.OnConditionDelegate(this.DidPlayerNotHaveAHaircut), null, 100, null);
			campaignGameStarter.AddPlayerLine("player_no_haircut_finish_talk", "player_did_not_cut_token", "close_window", "{=oPUVNuhN}I'll keep you in mind", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("player_haircut_finish_talk", "player_had_a_haircut_token", "close_window", "{=F9Xjbchh}Thank you.", null, null, 100, null, null);
		}

		// Token: 0x060008B7 RID: 2231 RVA: 0x00040252 File Offset: 0x0003E452
		private bool InDisguiseSpeakingToBarber()
		{
			return this.IsConversationAgentBarber() && Campaign.Current.IsMainHeroDisguised;
		}

		// Token: 0x060008B8 RID: 2232 RVA: 0x00040268 File Offset: 0x0003E468
		private bool DoesPlayerHaveEnoughGold(out TextObject explanation)
		{
			if (Hero.MainHero.Gold < 100)
			{
				explanation = new TextObject("{=RYJdU43V}Not Enough Gold", null);
				return false;
			}
			explanation = null;
			return true;
		}

		// Token: 0x060008B9 RID: 2233 RVA: 0x0004028B File Offset: 0x0003E48B
		private void ChargeThePlayer()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 100, false);
		}

		// Token: 0x060008BA RID: 2234 RVA: 0x0004029B File Offset: 0x0003E49B
		private bool DidPlayerNotHaveAHaircut()
		{
			return !this.DidPlayerHaveAHaircut();
		}

		// Token: 0x060008BB RID: 2235 RVA: 0x000402A8 File Offset: 0x0003E4A8
		private bool DidPlayerHaveAHaircut()
		{
			return Hero.MainHero.BodyProperties.StaticProperties != this._previousBodyProperties;
		}

		// Token: 0x060008BC RID: 2236 RVA: 0x000402D2 File Offset: 0x0003E4D2
		private bool IsConversationAgentBarber()
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			return ((currentSettlement != null) ? currentSettlement.Culture.Barber : null) == CharacterObject.OneToOneConversationCharacter;
		}

		// Token: 0x060008BD RID: 2237 RVA: 0x000402F1 File Offset: 0x0003E4F1
		private bool GivePlayerAHaircutCondition()
		{
			MBTextManager.SetTextVariable("GOLD_COST", 100);
			return true;
		}

		// Token: 0x060008BE RID: 2238 RVA: 0x00040300 File Offset: 0x0003E500
		private void GivePlayerAHaircut()
		{
			this._isOpenedFromBarberDialogue = true;
			BarberState gameState = Game.Current.GameStateManager.CreateState<BarberState>(new object[]
			{
				Hero.MainHero.CharacterObject,
				this.GetFaceGenFilter()
			});
			this._isOpenedFromBarberDialogue = false;
			GameStateManager.Current.PushState(gameState, 0);
		}

		// Token: 0x060008BF RID: 2239 RVA: 0x00040354 File Offset: 0x0003E554
		private void InitializeBarberConversation()
		{
			this._previousBodyProperties = Hero.MainHero.BodyProperties.StaticProperties;
		}

		// Token: 0x060008C0 RID: 2240 RVA: 0x0004037C File Offset: 0x0003E57C
		private LocationCharacter CreateBarber(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject barber = culture.Barber;
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(barber, out minValue, out maxValue, "Barber");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(barber, -1, null, default(UniqueTroopDescriptor))).Monster(FaceGen.GetMonsterWithSuffix(barber.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_barber", true, relation, null, true, false, null, false, false, true, null, false);
		}

		// Token: 0x060008C1 RID: 2241 RVA: 0x00040410 File Offset: 0x0003E610
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
			int num;
			if (CampaignMission.Current.Location == locationWithId && Campaign.Current.IsDay && unusedUsablePointCount.TryGetValue("sp_merchant_notary", out num))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateBarber), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
		}

		// Token: 0x060008C2 RID: 2242 RVA: 0x00040478 File Offset: 0x0003E678
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
				foreach (CultureObject culture in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
				{
					list.AddRange(Campaign.Current.Models.BodyPropertiesModel.GetHairIndicesForCulture(Hero.MainHero.CharacterObject.Race, Hero.MainHero.IsFemale ? 1 : 0, Hero.MainHero.Age, culture));
					list2.AddRange(Campaign.Current.Models.BodyPropertiesModel.GetBeardIndicesForCulture(Hero.MainHero.CharacterObject.Race, Hero.MainHero.IsFemale ? 1 : 0, Hero.MainHero.Age, culture));
				}
			}
			return new BarberCampaignBehavior.BarberFaceGeneratorCustomFilter(!this._isOpenedFromBarberDialogue, list.Distinct<int>().ToArray<int>(), list2.Distinct<int>().ToArray<int>());
		}

		// Token: 0x04000452 RID: 1106
		private const int BarberCost = 100;

		// Token: 0x04000453 RID: 1107
		private bool _isOpenedFromBarberDialogue;

		// Token: 0x04000454 RID: 1108
		private StaticBodyProperties _previousBodyProperties;

		// Token: 0x020001EB RID: 491
		private class BarberFaceGeneratorCustomFilter : IFaceGeneratorCustomFilter
		{
			// Token: 0x0600135C RID: 4956 RVA: 0x00077662 File Offset: 0x00075862
			public BarberFaceGeneratorCustomFilter(bool useDefaultStages, int[] haircutIndices, int[] faircutIndices)
			{
				this._haircutIndices = haircutIndices;
				this._facialHairIndices = faircutIndices;
				this._defaultStages = useDefaultStages;
			}

			// Token: 0x0600135D RID: 4957 RVA: 0x0007767F File Offset: 0x0007587F
			public int[] GetHaircutIndices(BasicCharacterObject character)
			{
				return this._haircutIndices;
			}

			// Token: 0x0600135E RID: 4958 RVA: 0x00077687 File Offset: 0x00075887
			public int[] GetFacialHairIndices(BasicCharacterObject character)
			{
				return this._facialHairIndices;
			}

			// Token: 0x0600135F RID: 4959 RVA: 0x0007768F File Offset: 0x0007588F
			public FaceGeneratorStage[] GetAvailableStages()
			{
				if (this._defaultStages)
				{
					FaceGeneratorStage[] array = new FaceGeneratorStage[7];
					RuntimeHelpers.InitializeArray(array, fieldof(<PrivateImplementationDetails>.50567A6578C37E24118E2B7EE8F5C7930666F62F).FieldHandle);
					return array;
				}
				return new FaceGeneratorStage[] { FaceGeneratorStage.Hair };
			}

			// Token: 0x0400090E RID: 2318
			private readonly int[] _haircutIndices;

			// Token: 0x0400090F RID: 2319
			private readonly int[] _facialHairIndices;

			// Token: 0x04000910 RID: 2320
			private readonly bool _defaultStages;
		}
	}
}

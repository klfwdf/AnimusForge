using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x0200035D RID: 861
	public class EscortMerchantCaravanIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000C36 RID: 3126
		// (get) Token: 0x060032C3 RID: 12995 RVA: 0x000D03F0 File Offset: 0x000CE5F0
		private static EscortMerchantCaravanIssueBehavior Instance
		{
			get
			{
				return Campaign.Current.GetCampaignBehavior<EscortMerchantCaravanIssueBehavior>();
			}
		}

		// Token: 0x060032C4 RID: 12996 RVA: 0x000D03FC File Offset: 0x000CE5FC
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
		}

		// Token: 0x060032C5 RID: 12997 RVA: 0x000D0450 File Offset: 0x000CE650
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeOnStart();
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.9.1", 0))
			{
				for (int i = MobileParty.All.Count - 1; i >= 0; i--)
				{
					MobileParty mobileParty = MobileParty.All[i];
					if (mobileParty.StringId.Contains("defend_caravan_quest"))
					{
						if (mobileParty.MapEvent != null)
						{
							mobileParty.MapEvent.FinalizeEvent();
						}
						DestroyPartyAction.Apply(null, MobileParty.All[i]);
					}
				}
			}
		}

		// Token: 0x060032C6 RID: 12998 RVA: 0x000D04DC File Offset: 0x000CE6DC
		private void InitializeOnStart()
		{
			if (MBObjectManager.Instance.GetObject<ItemObject>("hardwood") == null || MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse") == null)
			{
				CampaignEventDispatcher.Instance.RemoveListeners(this);
				using (List<KeyValuePair<Hero, IssueBase>>.Enumerator enumerator = (from x in Campaign.Current.IssueManager.Issues
					where x.Value.GetType() == typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue)
					select x).ToList<KeyValuePair<Hero, IssueBase>>().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<Hero, IssueBase> keyValuePair = enumerator.Current;
						keyValuePair.Value.CompleteIssueWithStayAliveConditionsFailed();
					}
					return;
				}
			}
			this.DefaultCaravanItems.Add(DefaultItems.Grain);
			foreach (string objectName in new string[] { "cotton", "velvet", "oil", "linen", "date_fruit" })
			{
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(objectName);
				if (@object != null)
				{
					this.DefaultCaravanItems.Add(@object);
				}
			}
		}

		// Token: 0x060032C7 RID: 12999 RVA: 0x000D060C File Offset: 0x000CE80C
		private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeOnStart();
		}

		// Token: 0x060032C8 RID: 13000 RVA: 0x000D0614 File Offset: 0x000CE814
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060032C9 RID: 13001 RVA: 0x000D0618 File Offset: 0x000CE818
		private bool ConditionsHold(Hero issueGiver)
		{
			return issueGiver.IsMerchant && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && !issueGiver.CurrentSettlement.HasPort && issueGiver.CurrentSettlement.Town.Security <= 50f && issueGiver.OwnedCaravans.Count < 2;
		}

		// Token: 0x060032CA RID: 13002 RVA: 0x000D0678 File Offset: 0x000CE878
		public void OnCheckForIssue(Hero hero)
		{
			if (this.ConditionsHold(hero))
			{
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnSelected), typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue), IssueBase.IssueFrequency.VeryCommon, null));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue), IssueBase.IssueFrequency.VeryCommon));
		}

		// Token: 0x060032CB RID: 13003 RVA: 0x000D06DC File Offset: 0x000CE8DC
		private IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
		{
			return new EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue(issueOwner);
		}

		// Token: 0x04000E82 RID: 3714
		private const IssueBase.IssueFrequency EscortMerchantCaravanIssueFrequency = IssueBase.IssueFrequency.VeryCommon;

		// Token: 0x04000E83 RID: 3715
		internal readonly List<ItemObject> DefaultCaravanItems = new List<ItemObject>();

		// Token: 0x020006F3 RID: 1779
		public class EscortMerchantCaravanIssue : IssueBase
		{
			// Token: 0x06005541 RID: 21825 RVA: 0x0018FCDD File Offset: 0x0018DEDD
			internal static void AutoGeneratedStaticCollectObjectsEscortMerchantCaravanIssue(object o, List<object> collectedObjects)
			{
				((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005542 RID: 21826 RVA: 0x0018FCEB File Offset: 0x0018DEEB
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005543 RID: 21827 RVA: 0x0018FCF4 File Offset: 0x0018DEF4
			internal static object AutoGeneratedGetMemberValue_companionRewardRandom(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue)o)._companionRewardRandom;
			}

			// Token: 0x17001022 RID: 4130
			// (get) Token: 0x06005544 RID: 21828 RVA: 0x0018FD06 File Offset: 0x0018DF06
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.Casualties | IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x17001023 RID: 4131
			// (get) Token: 0x06005545 RID: 21829 RVA: 0x0018FD0A File Offset: 0x0018DF0A
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 10 + MathF.Ceiling(16f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17001024 RID: 4132
			// (get) Token: 0x06005546 RID: 21830 RVA: 0x0018FD20 File Offset: 0x0018DF20
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 6 + MathF.Ceiling(10f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17001025 RID: 4133
			// (get) Token: 0x06005547 RID: 21831 RVA: 0x0018FD35 File Offset: 0x0018DF35
			protected int DailyQuestRewardGold
			{
				get
				{
					return 250 + MathF.Ceiling(1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17001026 RID: 4134
			// (get) Token: 0x06005548 RID: 21832 RVA: 0x0018FD4E File Offset: 0x0018DF4E
			protected override int RewardGold
			{
				get
				{
					return Math.Min(this.DailyQuestRewardGold * this._companionRewardRandom, 8000);
				}
			}

			// Token: 0x17001027 RID: 4135
			// (get) Token: 0x06005549 RID: 21833 RVA: 0x0018FD68 File Offset: 0x0018DF68
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject result = new TextObject("{=CSqaF7tz}There's been a real surge of banditry around here recently. I don't know if it's because the lords are away fighting or something else, but it's a miracle if a traveler can make three leagues beyond the gates without being set upon by highwaymen.[if:convo_annoyed][ib:hip]", null);
					if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt || base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaSoftspoken)
					{
						result = new TextObject("{=xwc9mJdC}Things have gotten a lot worse recently with the brigands on the roads around town. My caravans get looted as soon as they're out of sight of the gates.[if:convo_stern][ib:hip]", null);
					}
					return result;
				}
			}

			// Token: 0x17001028 RID: 4136
			// (get) Token: 0x0600554A RID: 21834 RVA: 0x0018FDBC File Offset: 0x0018DFBC
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=TGYJUUn0}Go on.", null);
				}
			}

			// Token: 0x17001029 RID: 4137
			// (get) Token: 0x0600554B RID: 21835 RVA: 0x0018FDC9 File Offset: 0x0018DFC9
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					return new TextObject("{=8ym6UvxE}I'm of a mind to send out a new caravan but I fear it will be plundered before it can turn a profit. So I am looking for some good fighters who can escort it until it finds its footing and visits a couple of settlements.", null);
				}
			}

			// Token: 0x1700102A RID: 4138
			// (get) Token: 0x0600554C RID: 21836 RVA: 0x0018FDD8 File Offset: 0x0018DFD8
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=ytdZutjw}I will be willing to pay generously {BASE_REWARD}{GOLD_ICON} for each day the caravan is on the road. It will be more than I usually pay for caravan guards, but you look like the type who send a message to these brigands, that my caravans aren't to be messed with.[if:convo_undecided_closed]", null);
					if (base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt || base.IssueOwner.CharacterObject.GetPersona() == DefaultTraits.PersonaSoftspoken)
					{
						textObject = new TextObject("{=YbbfaHqd}I will be willing to pay generously {BASE_REWARD}{GOLD_ICON} for each day the caravan is on the road. It will be more than I usually pay for guards, but figure maybe you can scare these bandits off. I'm sick of choosing between sending my men to the their deaths or letting them go because I've lost my goods and can't pay their wages.[if:convo_undecided_closed]", null);
					}
					textObject.SetTextVariable("BASE_REWARD", this.DailyQuestRewardGold);
					return textObject;
				}
			}

			// Token: 0x1700102B RID: 4139
			// (get) Token: 0x0600554D RID: 21837 RVA: 0x0018FE3E File Offset: 0x0018E03E
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=a7fEPW5Y}Don't worry, I'll escort the caravan myself.", null);
				}
			}

			// Token: 0x1700102C RID: 4140
			// (get) Token: 0x0600554E RID: 21838 RVA: 0x0018FE4B File Offset: 0x0018E04B
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=N4p2GCsG}I'll assign one of my companions and {NEEDED_MEN_COUNT} of my men to protect your caravan for {RETURN_DAYS} days.", null);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x1700102D RID: 4141
			// (get) Token: 0x0600554F RID: 21839 RVA: 0x0018FE7C File Offset: 0x0018E07C
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					return new TextObject("{=hU5j7b3e}I am sure your men are as capable as you are and will look after my caravan. Thanks again for your help, my friend.[if:convo_focused_happy]", null);
				}
			}

			// Token: 0x1700102E RID: 4142
			// (get) Token: 0x06005550 RID: 21840 RVA: 0x0018FE89 File Offset: 0x0018E089
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					return new TextObject("{=iny76Ifh}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}, I think they will be enough.", null);
				}
			}

			// Token: 0x1700102F RID: 4143
			// (get) Token: 0x06005551 RID: 21841 RVA: 0x0018FE96 File Offset: 0x0018E096
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001030 RID: 4144
			// (get) Token: 0x06005552 RID: 21842 RVA: 0x0018FE99 File Offset: 0x0018E099
			public override bool IsThereLordSolution
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17001031 RID: 4145
			// (get) Token: 0x06005553 RID: 21843 RVA: 0x0018FE9C File Offset: 0x0018E09C
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=6y59FBgL}{ISSUEGIVER.LINK}, a merchant from {SETTLEMENT}, has told you about {?ISSUEGIVER.GENDER}her{?}his{\\?} recent problems with bandits. {?ISSUEGIVER.GENDER}She{?}he{\\?} asked you to guard {?ISSUEGIVER.GENDER}her{?}his{\\?} caravan for a while and deal with any attackers. In return {?ISSUEGIVER.GENDER}she{?}he{\\?} offered you {GOLD}{GOLD_ICON} for each day your troops spend on escort duty.{newline}You agreed to lend {?ISSUEGIVER.GENDER}her{?}him{\\?} {NEEDED_MEN_COUNT} men. They should be enough to turn away most of the bandits. Your troops should return after {RETURN_DAYS} days.", null);
					StringHelpers.SetCharacterProperties("ISSUEGIVER", base.IssueOwner.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.Name);
					textObject.SetTextVariable("NEEDED_MEN_COUNT", this.AlternativeSolutionSentTroops.TotalManCount);
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					textObject.SetTextVariable("GOLD", this.DailyQuestRewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x17001032 RID: 4146
			// (get) Token: 0x06005554 RID: 21844 RVA: 0x0018FF36 File Offset: 0x0018E136
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=VpLzd69e}Escort Merchant Caravan", null);
				}
			}

			// Token: 0x17001033 RID: 4147
			// (get) Token: 0x06005555 RID: 21845 RVA: 0x0018FF43 File Offset: 0x0018E143
			public override TextObject Description
			{
				get
				{
					return new TextObject("{=8RNueEmy}A merchant caravan needs an escort for protection against bandits and brigands.", null);
				}
			}

			// Token: 0x17001034 RID: 4148
			// (get) Token: 0x06005556 RID: 21846 RVA: 0x0018FF50 File Offset: 0x0018E150
			public override TextObject IssueAlternativeSolutionFailLog
			{
				get
				{
					return new TextObject("{=KLauwaRJ}The caravan was destroyed despite your companion's efforts. Quest failed.", null);
				}
			}

			// Token: 0x17001035 RID: 4149
			// (get) Token: 0x06005557 RID: 21847 RVA: 0x0018FF60 File Offset: 0x0018E160
			public override TextObject IssueAlternativeSolutionSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=3NX8H4TJ}Your companion has protected the caravan that belongs to {ISSUE_GIVER.LINK} from {SETTLEMENT} as promised. {?ISSUE_GIVER.GENDER}She{?}He{\\?} was happy with your work.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.IssueSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x06005558 RID: 21848 RVA: 0x0018FFA9 File Offset: 0x0018E1A9
			public EscortMerchantCaravanIssue(Hero issueOwner)
				: base(issueOwner, CampaignTime.DaysFromNow(30f))
			{
				this._companionRewardRandom = MBRandom.RandomInt(3, 10);
			}

			// Token: 0x06005559 RID: 21849 RVA: 0x0018FFCA File Offset: 0x0018E1CA
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementProsperity)
				{
					return -0.4f;
				}
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.2f;
				}
				return 0f;
			}

			// Token: 0x0600555A RID: 21850 RVA: 0x0018FFED File Offset: 0x0018E1ED
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Scouting) >= hero.GetSkillValue(DefaultSkills.Riding)) ? DefaultSkills.Scouting : DefaultSkills.Riding, 120);
			}

			// Token: 0x0600555B RID: 21851 RVA: 0x0019001A File Offset: 0x0018E21A
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x0600555C RID: 21852 RVA: 0x0019002B File Offset: 0x0018E22B
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x17001036 RID: 4150
			// (get) Token: 0x0600555D RID: 21853 RVA: 0x00190045 File Offset: 0x0018E245
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(800f + 1000f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x0600555E RID: 21854 RVA: 0x0019005A File Offset: 0x0018E25A
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x0600555F RID: 21855 RVA: 0x00190068 File Offset: 0x0018E268
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.VeryCommon;
			}

			// Token: 0x06005560 RID: 21856 RVA: 0x0019006C File Offset: 0x0018E26C
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flags, out Hero relationHero, out SkillObject skill)
			{
				skill = null;
				relationHero = null;
				flags = IssueBase.PreconditionFlags.None;
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flags |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					flags |= IssueBase.PreconditionFlags.AtWar;
				}
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 20)
				{
					flags |= IssueBase.PreconditionFlags.NotEnoughTroops;
				}
				return flags == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x06005561 RID: 21857 RVA: 0x001900D9 File Offset: 0x0018E2D9
			public override bool IssueStayAliveConditions()
			{
				return base.IssueOwner.OwnedCaravans.Count < 2 && base.IssueOwner.CurrentSettlement.Town.Security <= 80f;
			}

			// Token: 0x06005562 RID: 21858 RVA: 0x0019010F File Offset: 0x0018E30F
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06005563 RID: 21859 RVA: 0x00190111 File Offset: 0x0018E311
			protected override void HourlyTick()
			{
			}

			// Token: 0x06005564 RID: 21860 RVA: 0x00190113 File Offset: 0x0018E313
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(30f), base.IssueDifficultyMultiplier, this.DailyQuestRewardGold);
			}

			// Token: 0x06005565 RID: 21861 RVA: 0x00190138 File Offset: 0x0018E338
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				base.IssueOwner.AddPower(-5f);
				this.RelationshipChangeWithIssueOwner = -5;
				TraitLevelingHelper.OnIssueFailed(base.IssueOwner, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
				});
				base.IssueSettlement.Town.Prosperity -= 20f;
			}

			// Token: 0x06005566 RID: 21862 RVA: 0x00190199 File Offset: 0x0018E399
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				base.IssueOwner.AddPower(10f);
				this.RelationshipChangeWithIssueOwner = 5;
				base.IssueSettlement.Town.Prosperity += 10f;
			}

			// Token: 0x06005567 RID: 21863 RVA: 0x001901CE File Offset: 0x0018E3CE
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x04001BDD RID: 7133
			private const int MinimumRequiredMenCount = 20;

			// Token: 0x04001BDE RID: 7134
			private const int AlternativeSolutionTroopTierRequirement = 2;

			// Token: 0x04001BDF RID: 7135
			private const int NeededCompanionSkillAmount = 120;

			// Token: 0x04001BE0 RID: 7136
			private const int QuestTimeLimit = 30;

			// Token: 0x04001BE1 RID: 7137
			private const int IssueDuration = 30;

			// Token: 0x04001BE2 RID: 7138
			[SaveableField(10)]
			private int _companionRewardRandom;
		}

		// Token: 0x020006F4 RID: 1780
		public class EscortMerchantCaravanIssueQuest : QuestBase
		{
			// Token: 0x06005568 RID: 21864 RVA: 0x001901D0 File Offset: 0x0018E3D0
			internal static void AutoGeneratedStaticCollectObjectsEscortMerchantCaravanIssueQuest(object o, List<object> collectedObjects)
			{
				((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06005569 RID: 21865 RVA: 0x001901E0 File Offset: 0x0018E3E0
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._visitedSettlements);
				collectedObjects.Add(this._questCaravanMobileParty);
				collectedObjects.Add(this._questBanditMobileParty);
				collectedObjects.Add(this._otherBanditParty);
				collectedObjects.Add(this._playerStartsQuestLog);
			}

			// Token: 0x0600556A RID: 21866 RVA: 0x00190230 File Offset: 0x0018E430
			internal static object AutoGeneratedGetMemberValue_requiredSettlementNumber(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._requiredSettlementNumber;
			}

			// Token: 0x0600556B RID: 21867 RVA: 0x00190242 File Offset: 0x0018E442
			internal static object AutoGeneratedGetMemberValue_visitedSettlements(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._visitedSettlements;
			}

			// Token: 0x0600556C RID: 21868 RVA: 0x0019024F File Offset: 0x0018E44F
			internal static object AutoGeneratedGetMemberValue_questCaravanMobileParty(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._questCaravanMobileParty;
			}

			// Token: 0x0600556D RID: 21869 RVA: 0x0019025C File Offset: 0x0018E45C
			internal static object AutoGeneratedGetMemberValue_questBanditMobileParty(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._questBanditMobileParty;
			}

			// Token: 0x0600556E RID: 21870 RVA: 0x00190269 File Offset: 0x0018E469
			internal static object AutoGeneratedGetMemberValue_difficultyMultiplier(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._difficultyMultiplier;
			}

			// Token: 0x0600556F RID: 21871 RVA: 0x0019027B File Offset: 0x0018E47B
			internal static object AutoGeneratedGetMemberValue_isPlayerNotifiedForDanger(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._isPlayerNotifiedForDanger;
			}

			// Token: 0x06005570 RID: 21872 RVA: 0x0019028D File Offset: 0x0018E48D
			internal static object AutoGeneratedGetMemberValue_otherBanditParty(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._otherBanditParty;
			}

			// Token: 0x06005571 RID: 21873 RVA: 0x0019029A File Offset: 0x0018E49A
			internal static object AutoGeneratedGetMemberValue_questBanditPartyFollowDuration(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._questBanditPartyFollowDuration;
			}

			// Token: 0x06005572 RID: 21874 RVA: 0x001902AC File Offset: 0x0018E4AC
			internal static object AutoGeneratedGetMemberValue_otherBanditPartyFollowDuration(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._otherBanditPartyFollowDuration;
			}

			// Token: 0x06005573 RID: 21875 RVA: 0x001902BE File Offset: 0x0018E4BE
			internal static object AutoGeneratedGetMemberValue_daysSpentForEscorting(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._daysSpentForEscorting;
			}

			// Token: 0x06005574 RID: 21876 RVA: 0x001902D0 File Offset: 0x0018E4D0
			internal static object AutoGeneratedGetMemberValue_questBanditPartyAlreadyAttacked(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._questBanditPartyAlreadyAttacked;
			}

			// Token: 0x06005575 RID: 21877 RVA: 0x001902E2 File Offset: 0x0018E4E2
			internal static object AutoGeneratedGetMemberValue_playerStartsQuestLog(object o)
			{
				return ((EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest)o)._playerStartsQuestLog;
			}

			// Token: 0x17001037 RID: 4151
			// (get) Token: 0x06005576 RID: 21878 RVA: 0x001902EF File Offset: 0x0018E4EF
			private float BanditPartyAttackRadiusMin
			{
				get
				{
					return Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.5f;
				}
			}

			// Token: 0x17001038 RID: 4152
			// (get) Token: 0x06005577 RID: 21879 RVA: 0x0019030B File Offset: 0x0018E50B
			private float QuestBanditPartySpawnDistance
			{
				get
				{
					return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f;
				}
			}

			// Token: 0x17001039 RID: 4153
			// (get) Token: 0x06005578 RID: 21880 RVA: 0x0019031E File Offset: 0x0018E51E
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=VpLzd69e}Escort Merchant Caravan", null);
				}
			}

			// Token: 0x1700103A RID: 4154
			// (get) Token: 0x06005579 RID: 21881 RVA: 0x0019032B File Offset: 0x0018E52B
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x1700103B RID: 4155
			// (get) Token: 0x0600557A RID: 21882 RVA: 0x0019032E File Offset: 0x0018E52E
			private int BanditPartyTroopCount
			{
				get
				{
					return (int)MathF.Min(40f, (float)(MobileParty.MainParty.MemberRoster.TotalHealthyCount + this._questCaravanMobileParty.MemberRoster.TotalHealthyCount) * 0.7f);
				}
			}

			// Token: 0x1700103C RID: 4156
			// (get) Token: 0x0600557B RID: 21883 RVA: 0x00190362 File Offset: 0x0018E562
			private int CaravanPartyTroopCount
			{
				get
				{
					return (int)(5f * this._difficultyMultiplier) + 10;
				}
			}

			// Token: 0x1700103D RID: 4157
			// (get) Token: 0x0600557C RID: 21884 RVA: 0x00190374 File Offset: 0x0018E574
			private bool CaravanIsInsideSettlement
			{
				get
				{
					return this._questCaravanMobileParty.CurrentSettlement != null;
				}
			}

			// Token: 0x1700103E RID: 4158
			// (get) Token: 0x0600557D RID: 21885 RVA: 0x00190384 File Offset: 0x0018E584
			private int TotalRewardGold
			{
				get
				{
					return MathF.Min(8000, this.RewardGold * this._daysSpentForEscorting);
				}
			}

			// Token: 0x1700103F RID: 4159
			// (get) Token: 0x0600557E RID: 21886 RVA: 0x0019039D File Offset: 0x0018E59D
			private CustomPartyComponent CaravanCustomPartyComponent
			{
				get
				{
					if (this._customPartyComponent == null)
					{
						this._customPartyComponent = this._questCaravanMobileParty.PartyComponent as CustomPartyComponent;
					}
					return this._customPartyComponent;
				}
			}

			// Token: 0x17001040 RID: 4160
			// (get) Token: 0x0600557F RID: 21887 RVA: 0x001903C4 File Offset: 0x0018E5C4
			private TextObject PlayerStartsQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=YXbKXUDu}{ISSUE_GIVER.LINK}, a merchant from {SETTLEMENT}, has told you about {?ISSUE_GIVER.GENDER}her{?}his{\\?} recent problems with bandits. {?ISSUE_GIVER.GENDER}She{?}He{\\?} asked you to guard {?ISSUE_GIVER.GENDER}her{?}his{\\?} caravan for a while and deal with any attackers. In return {?ISSUE_GIVER.GENDER}she{?}he{\\?} offered you {GOLD}{GOLD_ICON} denars for each day you spend on escort duty.{newline}You have agreed to guard it yourself until it visits {NUMBER_OF_SETTLEMENTS} settlements.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.Name);
					textObject.SetTextVariable("NUMBER_OF_SETTLEMENTS", this._requiredSettlementNumber);
					textObject.SetTextVariable("GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x17001041 RID: 4161
			// (get) Token: 0x06005580 RID: 21888 RVA: 0x00190441 File Offset: 0x0018E641
			private TextObject CaravanDestroyedQuestLogText
			{
				get
				{
					return new TextObject("{=zk9QyKIz}The caravan was destroyed. Quest failed.", null);
				}
			}

			// Token: 0x17001042 RID: 4162
			// (get) Token: 0x06005581 RID: 21889 RVA: 0x00190450 File Offset: 0x0018E650
			private TextObject CaravanLostTheTrackLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=y62dyzH6}You have lost the track of caravan. Your agreement with {ISSUE_GIVER.LINK} is failed.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17001043 RID: 4163
			// (get) Token: 0x06005582 RID: 21890 RVA: 0x00190484 File Offset: 0x0018E684
			private TextObject CaravanDestroyedByBanditsLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=MhvyTcrH}The caravan is destroyed by some bandits. Your agreement with {ISSUE_GIVER.LINK} is failed.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17001044 RID: 4164
			// (get) Token: 0x06005583 RID: 21891 RVA: 0x001904B6 File Offset: 0x0018E6B6
			private TextObject CaravanDestroyedByPlayerQuestLogText
			{
				get
				{
					return new TextObject("{=Rd3m5kyk}You have attacked the caravan.", null);
				}
			}

			// Token: 0x17001045 RID: 4165
			// (get) Token: 0x06005584 RID: 21892 RVA: 0x001904C4 File Offset: 0x0018E6C4
			private TextObject SuccessQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=dKEADOhG}You have protected the caravan belonging to {QUEST_GIVER.LINK} from {SETTLEMENT} as promised. {?QUEST_GIVER.GENDER}She{?}He{\\?} was happy with your work.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
					return textObject;
				}
			}

			// Token: 0x17001046 RID: 4166
			// (get) Token: 0x06005585 RID: 21893 RVA: 0x00190514 File Offset: 0x0018E714
			private TextObject CancelByWarQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=KhNkBd9O}Your clan is now at war with the {QUEST_GIVER.LINK}’s lord. Your agreement with {QUEST_GIVER.LINK} was canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x06005586 RID: 21894 RVA: 0x00190548 File Offset: 0x0018E748
			public EscortMerchantCaravanIssueQuest(string questId, Hero giverHero, CampaignTime duration, float difficultyMultiplier, int rewardGold)
				: base(questId, giverHero, duration, rewardGold)
			{
				this._difficultyMultiplier = difficultyMultiplier;
				this._requiredSettlementNumber = MathF.Round(2f + 4f * this._difficultyMultiplier);
				this._visitedSettlements = new List<Settlement>();
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x06005587 RID: 21895 RVA: 0x001905A4 File Offset: 0x0018E7A4
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=TdwKwExD}Thank you. You can find the caravan just outside the settlement.[if:convo_grateful]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=vtZYmAaR}I feel good knowing that you're looking after my caravan. Safe journeys, my friend![if:convo_grateful]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.CloseDialog();
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCaravanPartyDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCaravanGreetingDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCaravanTradeDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCaravanLootDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCaravanFarewellDialogFlow(), this);
			}

			// Token: 0x17001047 RID: 4167
			// (get) Token: 0x06005588 RID: 21896 RVA: 0x001906A8 File Offset: 0x0018E8A8
			private TextObject CaravanNoTargetLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=1FOmvEdf}All profitable trade routes of the caravan are blocked by recent wars. {QUEST_GIVER.LINK} decided to recall the caravan until the situation gets better. {?QUEST_GIVER.GENDER}She{?}He{\\?} was happy with your service and sent you {REWARD}{GOLD_ICON} as promised.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD", this.TotalRewardGold);
					return textObject;
				}
			}

			// Token: 0x06005589 RID: 21897 RVA: 0x001906EC File Offset: 0x0018E8EC
			private DialogFlow GetCaravanPartyDialogFlow()
			{
				TextObject textObject = new TextObject("{=ZAqEJI9T}About the task {QUEST_GIVER.LINK} gave me.", null);
				StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
				return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions(null, false).PlayerOption(textObject, null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.caravan_talk_on_condition))
					.NpcLine("{=heWYa9Oq}I feel safe knowing that you're looking after us. Please continue to follow us my friend!", null, null, null, null)
					.Consequence(delegate
					{
						PlayerEncounter.LeaveEncounter = true;
					})
					.CloseDialog()
					.EndPlayerOptions();
			}

			// Token: 0x0600558A RID: 21898 RVA: 0x00190788 File Offset: 0x0018E988
			private bool caravan_talk_on_condition()
			{
				bool flag = this._questCaravanMobileParty.MemberRoster.Contains(CharacterObject.OneToOneConversationCharacter) && this._questCaravanMobileParty == MobileParty.ConversationParty && MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCustomParty && !CharacterObject.OneToOneConversationCharacter.IsHero && MobileParty.ConversationParty.Party.Owner != Hero.MainHero;
				if (flag)
				{
					MBTextManager.SetTextVariable("HOMETOWN", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
					StringHelpers.SetCharacterProperties("MERCHANT", MobileParty.ConversationParty.Party.Owner.CharacterObject, null, false);
					StringHelpers.SetCharacterProperties("PROTECTOR", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject, null, false);
				}
				return flag;
			}

			// Token: 0x0600558B RID: 21899 RVA: 0x00190858 File Offset: 0x0018EA58
			private DialogFlow GetCaravanFarewellDialogFlow()
			{
				TextObject text = new TextObject("{=1IJouNaM}Carry on, then. Farewell.", null);
				return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions(null, false).PlayerOption(text, null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.caravan_talk_on_condition))
					.NpcLine("{=heWYa9Oq}I feel safe knowing that you're looking after us. Please continue to follow us my friend!", null, null, null, null)
					.Consequence(delegate
					{
						PlayerEncounter.LeaveEncounter = true;
					})
					.CloseDialog()
					.EndPlayerOptions();
			}

			// Token: 0x0600558C RID: 21900 RVA: 0x001908DC File Offset: 0x0018EADC
			private DialogFlow GetCaravanLootDialogFlow()
			{
				TextObject text = new TextObject("{=WOBy5UfY}Hand over your goods, or die!", null);
				return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions(null, false).PlayerOption(text, null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.caravan_loot_on_condition))
					.NpcLine("{=QNaKmkt9}We're paid to guard this caravan. If you want to rob it, it's going to be over our dead bodies![if:convo_angry][ib:aggressive]", null, null, null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption("{=EhxS7NQ4}So be it. Attack!", null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.conversation_caravan_fight_on_consequence))
					.CloseDialog()
					.PlayerOption("{=bfPsE9M1}You must have misunderstood me. Go in peace.", null, null, null)
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.caravan_talk_leave_on_consequence))
					.CloseDialog()
					.EndPlayerOptions()
					.EndPlayerOptions();
			}

			// Token: 0x0600558D RID: 21901 RVA: 0x00190987 File Offset: 0x0018EB87
			private void conversation_caravan_fight_on_consequence()
			{
				BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
			}

			// Token: 0x0600558E RID: 21902 RVA: 0x0019099D File Offset: 0x0018EB9D
			private void caravan_talk_leave_on_consequence()
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
			}

			// Token: 0x0600558F RID: 21903 RVA: 0x001909AC File Offset: 0x0018EBAC
			private DialogFlow GetCaravanTradeDialogFlow()
			{
				TextObject text = new TextObject("{=t0UGXPV4}I'm interested in trading. What kind of products do you have?", null);
				return DialogFlow.CreateDialogFlow("escort_caravan_talk", 125).BeginPlayerOptions(null, false).PlayerOption(text, null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.caravan_buy_products_on_condition))
					.NpcLine("{=tlLDHAIu}Very well. A pleasure doing business with you.[if:convo_relaxed_happy][ib:demure]", null, null, null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.conversation_caravan_player_trade_end_on_condition))
					.NpcLine("{=DQBaaC0e}Is there anything else?", null, null, null, null)
					.GotoDialogState("escort_caravan_talk")
					.EndPlayerOptions();
			}

			// Token: 0x06005590 RID: 21904 RVA: 0x00190A30 File Offset: 0x0018EC30
			private bool caravan_buy_products_on_condition()
			{
				if (MobileParty.ConversationParty != null && MobileParty.ConversationParty == this._questCaravanMobileParty && !MobileParty.ConversationParty.IsCaravan)
				{
					for (int i = 0; i < MobileParty.ConversationParty.ItemRoster.Count; i++)
					{
						if (MobileParty.ConversationParty.ItemRoster.GetElementNumber(i) > 0)
						{
							return true;
						}
					}
				}
				return false;
			}

			// Token: 0x06005591 RID: 21905 RVA: 0x00190A8D File Offset: 0x0018EC8D
			private bool conversation_caravan_player_trade_end_on_condition()
			{
				if (MobileParty.ConversationParty != null && MobileParty.ConversationParty == this._questCaravanMobileParty && !MobileParty.ConversationParty.IsCaravan)
				{
					InventoryScreenHelper.OpenTradeWithCaravanOrAlleyParty(MobileParty.ConversationParty, InventoryScreenHelper.InventoryCategoryType.None);
				}
				return true;
			}

			// Token: 0x06005592 RID: 21906 RVA: 0x00190ABC File Offset: 0x0018ECBC
			private DialogFlow GetCaravanGreetingDialogFlow()
			{
				TextObject npcText = new TextObject("{=FpUybbSk}Greetings. This caravan is owned by {MERCHANT.LINK}. We trade under the protection of {PROTECTOR.LINK}, master of {HOMETOWN}. How may we help you?[if:convo_normal]", null);
				if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					npcText = new TextObject("{=yGttYe7g}Greetings. This ship is owned by {MERCHANT.LINK}. We sail under the protection of {PROTECTOR.LINK}, master of {HOMETOWN}. How may we help you?[if:convo_normal]", null);
				}
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(npcText, null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.caravan_talk_on_condition))
					.GotoDialogState("escort_caravan_talk");
			}

			// Token: 0x06005593 RID: 21907 RVA: 0x00190B25 File Offset: 0x0018ED25
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				this.SpawnCaravan();
				this._playerStartsQuestLog = base.AddDiscreteLog(this.PlayerStartsQuestLogText, new TextObject("{=r2y3n7dR}Visited Settlements", null), this._visitedSettlements.Count, this._requiredSettlementNumber, null, false);
			}

			// Token: 0x06005594 RID: 21908 RVA: 0x00190B64 File Offset: 0x0018ED64
			private bool caravan_loot_on_condition()
			{
				bool flag = MobileParty.ConversationParty != null && MobileParty.ConversationParty.Party.MapFaction != Hero.MainHero.MapFaction && !MobileParty.ConversationParty.IsCaravan && MobileParty.ConversationParty == this._questCaravanMobileParty;
				if (flag)
				{
					MBTextManager.SetTextVariable("HOMETOWN", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
					StringHelpers.SetCharacterProperties("MERCHANT", MobileParty.ConversationParty.Party.Owner.CharacterObject, null, false);
					StringHelpers.SetCharacterProperties("PROTECTOR", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject, null, false);
				}
				return flag;
			}

			// Token: 0x06005595 RID: 21909 RVA: 0x00190C14 File Offset: 0x0018EE14
			private void SpawnCaravan()
			{
				ItemRoster itemRoster = new ItemRoster();
				foreach (ItemObject item in EscortMerchantCaravanIssueBehavior.Instance.DefaultCaravanItems)
				{
					itemRoster.AddToCounts(item, 7);
				}
				string partyMountStringId;
				string partyHarnessStringId;
				this.GetAdditionalVisualsForParty(base.QuestGiver.Culture, out partyMountStringId, out partyHarnessStringId);
				TextObject textObject = GameTexts.FindText("str_caravan_party_name", null);
				textObject.SetCharacterProperties("OWNER", base.QuestGiver.CharacterObject, false);
				this._questCaravanMobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(base.QuestGiver.CurrentSettlement.GatePosition, 0f, base.QuestGiver.CurrentSettlement, textObject, base.QuestGiver.Clan, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), base.QuestGiver, partyMountStringId, partyHarnessStringId, 4f, false);
				this.InitializeCaravanOnCreation(this._questCaravanMobileParty, base.QuestGiver, base.QuestGiver.CurrentSettlement, itemRoster);
				base.AddTrackedObject(this._questCaravanMobileParty);
				this._questCaravanMobileParty.SetPartyUsedByQuest(true);
				this._questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(true);
				this._questCaravanMobileParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
				this._caravanWaitedInSettlementForHours = 4;
			}

			// Token: 0x06005596 RID: 21910 RVA: 0x00190D5C File Offset: 0x0018EF5C
			private bool ProperSettlementCondition(Settlement settlement)
			{
				return settlement != Settlement.CurrentSettlement && settlement.IsTown && !settlement.IsUnderSiege && !this._visitedSettlements.Contains(settlement);
			}

			// Token: 0x06005597 RID: 21911 RVA: 0x00190D88 File Offset: 0x0018EF88
			private void InitializeCaravanOnCreation(MobileParty mobileParty, Hero owner, Settlement settlement, ItemRoster caravanItems)
			{
				mobileParty.Aggressiveness = 0f;
				PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(owner.Culture, false, true);
				mobileParty.InitializeMobilePartyAtPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), settlement.GatePosition, false);
				MobilePartyHelper.FillPartyManuallyAfterCreation(mobileParty, randomCaravanTemplate, this.CaravanPartyTroopCount);
				CharacterObject character2 = CharacterObject.All.First((CharacterObject character) => character.Occupation == Occupation.CaravanGuard && character.IsInfantry && character.Level == 26 && character.Culture == mobileParty.Party.Owner.Culture);
				mobileParty.MemberRoster.AddToCounts(character2, 1, true, 0, 0, true, -1);
				mobileParty.Party.SetVisualAsDirty();
				mobileParty.InitializePartyTrade(Campaign.Current.Models.CaravanModel.GetInitialTradeGold(owner, false, false));
				if (caravanItems != null)
				{
					mobileParty.ItemRoster.Add(caravanItems);
					return;
				}
				float num = 10000f;
				ItemObject itemObject = null;
				foreach (ItemObject itemObject2 in Items.All)
				{
					if (itemObject2.ItemCategory == DefaultItemCategories.PackAnimal && !itemObject2.NotMerchandise && (float)itemObject2.Value < num)
					{
						itemObject = itemObject2;
						num = (float)itemObject2.Value;
					}
				}
				if (itemObject != null)
				{
					mobileParty.ItemRoster.Add(new ItemRosterElement(itemObject, (int)((float)mobileParty.MemberRoster.TotalManCount * 0.5f), null));
				}
			}

			// Token: 0x06005598 RID: 21912 RVA: 0x00190F14 File Offset: 0x0018F114
			private void GetAdditionalVisualsForParty(CultureObject culture, out string mountStringId, out string harnessStringId)
			{
				if (culture.StringId == "aserai" || culture.StringId == "khuzait")
				{
					mountStringId = "camel";
					harnessStringId = ((MBRandom.RandomFloat > 0.5f) ? "camel_saddle_a" : "camel_saddle_b");
					return;
				}
				mountStringId = "mule";
				harnessStringId = ((MBRandom.RandomFloat > 0.5f) ? "mule_load_a" : ((MBRandom.RandomFloat > 0.5f) ? "mule_load_b" : "mule_load_c"));
			}

			// Token: 0x06005599 RID: 21913 RVA: 0x00190F9C File Offset: 0x0018F19C
			protected override void RegisterEvents()
			{
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyHourlyTick));
				CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			}

			// Token: 0x0600559A RID: 21914 RVA: 0x0019104A File Offset: 0x0018F24A
			private void OnPartyHourlyTick(MobileParty mobileParty)
			{
				this.CheckPartyAndMakeItAttackTheCaravan(mobileParty);
				this.CheckEncounterForBanditParty(this._questBanditMobileParty);
				this.CheckEncounterForBanditParty(this._otherBanditParty);
				this.CheckOtherBanditPartyDistance();
			}

			// Token: 0x0600559B RID: 21915 RVA: 0x00191074 File Offset: 0x0018F274
			private void CheckOtherBanditPartyDistance()
			{
				if (base.IsOngoing)
				{
					if (this._otherBanditParty != null && this._otherBanditParty.IsActive && this._otherBanditParty.TargetParty == this._questCaravanMobileParty && this._otherBanditPartyFollowDuration < 0)
					{
						if (base.IsTracked(this._otherBanditParty))
						{
							base.RemoveTrackedObject(this._otherBanditParty);
						}
						this._otherBanditParty.SetMoveModeHold();
						this._otherBanditParty.Ai.SetDoNotMakeNewDecisions(false);
						this._otherBanditParty = null;
					}
					if (this._questBanditMobileParty != null && this._questBanditMobileParty.IsActive && this._questBanditMobileParty.MapEvent == null && this._questBanditMobileParty.TargetParty == this._questCaravanMobileParty && this._questBanditPartyFollowDuration < 0 && !this._questBanditMobileParty.IsVisible)
					{
						if (base.IsTracked(this._questBanditMobileParty))
						{
							base.RemoveTrackedObject(this._questBanditMobileParty);
						}
						this._questBanditMobileParty.SetMoveModeHold();
						this._questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(false);
					}
				}
			}

			// Token: 0x0600559C RID: 21916 RVA: 0x0019117C File Offset: 0x0018F37C
			private void CheckEncounterForBanditParty(MobileParty mobileParty)
			{
				if (base.IsOngoing && mobileParty != null && mobileParty.IsActive && mobileParty.MapEvent == null && this._questCaravanMobileParty.IsActive && this._questCaravanMobileParty.MapEvent == null && this._questCaravanMobileParty.CurrentSettlement == null && mobileParty.Position.DistanceSquared(this._questCaravanMobileParty.Position) <= 1f)
				{
					EncounterManager.StartPartyEncounter(mobileParty.Party, this._questCaravanMobileParty.Party);
					MBInformationManager.AddQuickInformation(new TextObject("{=o8uAzFaJ}The caravan you are protecting is ambushed by raiders!", null), 0, null, null, "");
					this._questCaravanMobileParty.MapEvent.IsInvulnerable = true;
				}
			}

			// Token: 0x0600559D RID: 21917 RVA: 0x00191238 File Offset: 0x0018F438
			private void CheckPartyAndMakeItAttackTheCaravan(MobileParty mobileParty)
			{
				if (this._otherBanditParty == null && mobileParty != this._questBanditMobileParty && mobileParty.IsBandit && !mobileParty.IsCurrentlyUsedByAQuest && mobileParty.MapEvent == null && mobileParty.NavigationCapability == MobileParty.NavigationType.Default && mobileParty.Party.NumberOfHealthyMembers > this._questCaravanMobileParty.Party.NumberOfHealthyMembers && (mobileParty.Speed > this._questCaravanMobileParty.Speed || mobileParty.Position.DistanceSquared(this._questCaravanMobileParty.Position) < 9f))
				{
					Settlement toSettlement = this._visitedSettlements.LastOrDefault<Settlement>() ?? this._questCaravanMobileParty.HomeSettlement;
					Settlement targetSettlement = this._questCaravanMobileParty.TargetSettlement;
					if (targetSettlement == null)
					{
						this.TryToFindAndSetTargetToNextSettlement();
						return;
					}
					float distance;
					float distance2;
					if (this._questCaravanMobileParty.CurrentSettlement != null)
					{
						distance = Campaign.Current.Models.MapDistanceModel.GetDistance(this._questCaravanMobileParty.CurrentSettlement, targetSettlement, false, false, MobileParty.NavigationType.Default);
						distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(this._questCaravanMobileParty.CurrentSettlement, toSettlement, false, false, MobileParty.NavigationType.Default);
					}
					else
					{
						float num;
						distance = Campaign.Current.Models.MapDistanceModel.GetDistance(this._questCaravanMobileParty, targetSettlement, false, MobileParty.NavigationType.Default, out num);
						distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(this._questCaravanMobileParty, toSettlement, false, MobileParty.NavigationType.Default, out num);
					}
					float num2 = mobileParty.Position.DistanceSquared(this._questCaravanMobileParty.Position);
					if (distance > 5f && distance2 > 5f && num2 < this.BanditPartyAttackRadiusMin * this.BanditPartyAttackRadiusMin)
					{
						SetPartyAiAction.GetActionForEngagingParty(mobileParty, this._questCaravanMobileParty, MobileParty.NavigationType.Default, false);
						mobileParty.Ai.SetDoNotMakeNewDecisions(true);
						if (!base.IsTracked(mobileParty))
						{
							base.AddTrackedObject(mobileParty);
						}
						float num3 = mobileParty.Speed + this._questCaravanMobileParty.Speed;
						this._otherBanditPartyFollowDuration = (int)(num2 / num3) + 5;
						this._otherBanditParty = mobileParty;
					}
				}
			}

			// Token: 0x0600559E RID: 21918 RVA: 0x0019143C File Offset: 0x0018F63C
			private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
			{
				if (party == this._questCaravanMobileParty && settlement != this._questCaravanMobileParty.HomeSettlement && settlement.Position.NearlyEquals(MobileParty.MainParty.Position.ToVec2(), MobileParty.MainParty.SeeingRange + 2f) && settlement == this._questCaravanMobileParty.TargetSettlement)
				{
					this._visitedSettlements.Add(settlement);
					base.UpdateQuestTaskStage(this._playerStartsQuestLog, this._visitedSettlements.Count);
					TextObject textObject = new TextObject("{=0wj3HIbh}Caravan entered {SETTLEMENT_LINK}.", null);
					textObject.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
					base.AddLog(textObject, true);
					if (this._questBanditMobileParty != null && this._questBanditMobileParty.IsActive)
					{
						if (base.IsTracked(this._questBanditMobileParty))
						{
							base.RemoveTrackedObject(this._questBanditMobileParty);
						}
						this._questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(false);
						this._questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
						if (this._questBanditMobileParty.MapEvent == null)
						{
							SetPartyAiAction.GetActionForPatrollingAroundSettlement(this._questBanditMobileParty, settlement, MobileParty.NavigationType.Default, false, false);
						}
					}
					if (this._otherBanditParty != null)
					{
						if (base.IsTracked(this._otherBanditParty))
						{
							base.RemoveTrackedObject(this._otherBanditParty);
						}
						this._otherBanditParty.SetMoveModeHold();
						this._otherBanditParty.Ai.SetDoNotMakeNewDecisions(false);
						this._otherBanditParty = null;
					}
					if (this._visitedSettlements.Count == this._requiredSettlementNumber)
					{
						this.SuccessConsequences(false);
						return;
					}
					int num = this.CaravanPartyTroopCount - this._questCaravanMobileParty.MemberRoster.TotalManCount;
					if (num > 0)
					{
						this._questCaravanMobileParty.AddElementToMemberRoster(this._questCaravanMobileParty.TargetSettlement.Culture.CaravanGuard, MBRandom.RandomInt(Math.Min(15, num)), false);
					}
				}
			}

			// Token: 0x0600559F RID: 21919 RVA: 0x0019160D File Offset: 0x0018F80D
			protected override void DailyTick()
			{
				this._daysSpentForEscorting++;
			}

			// Token: 0x060055A0 RID: 21920 RVA: 0x0019161D File Offset: 0x0018F81D
			private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
			{
				this.CheckWarDeclaration();
			}

			// Token: 0x060055A1 RID: 21921 RVA: 0x00191625 File Offset: 0x0018F825
			private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
			{
				this.CheckWarDeclaration();
			}

			// Token: 0x060055A2 RID: 21922 RVA: 0x0019162D File Offset: 0x0018F82D
			private void CheckWarDeclaration()
			{
				if (base.QuestGiver.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					base.CompleteQuestWithCancel(this.CancelByWarQuestLogText);
				}
			}

			// Token: 0x060055A3 RID: 21923 RVA: 0x0019165C File Offset: 0x0018F85C
			private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
			{
				if (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility && (faction1 == this._questCaravanMobileParty.MapFaction || faction2 == this._questCaravanMobileParty.MapFaction) && PlayerEncounter.Current != null && PlayerEncounter.PlayerIsAttacker)
				{
					this.FailByPlayerHostileConsequences();
				}
				else
				{
					this.CheckWarDeclaration();
				}
				if (this._questCaravanMobileParty != null && (this._questCaravanMobileParty.TargetSettlement == null || this._questCaravanMobileParty.TargetSettlement.MapFaction.IsAtWarWith(this._questCaravanMobileParty.MapFaction)) && base.IsOngoing)
				{
					this.TryToFindAndSetTargetToNextSettlement();
				}
			}

			// Token: 0x060055A4 RID: 21924 RVA: 0x001916EC File Offset: 0x0018F8EC
			protected override void HourlyTick()
			{
				if (base.IsOngoing && this._questCaravanMobileParty.TargetSettlement == null)
				{
					this.TryToFindAndSetTargetToNextSettlement();
				}
				if (base.IsOngoing)
				{
					if (this.CaravanIsInsideSettlement)
					{
						this.SimulateSettlementWaitForCaravan();
					}
					else if (this._questCaravanMobileParty.MapEvent == null)
					{
						this.AdjustCaravansSpeed();
					}
					this.NotifyPlayerOrCancelTheQuestIfCaravanIsFar();
					if (base.IsOngoing)
					{
						this.ThinkAboutSpawningBanditParty();
						this.CheckCaravanMapEvent();
						this._otherBanditPartyFollowDuration--;
						this._questBanditPartyFollowDuration--;
					}
				}
			}

			// Token: 0x060055A5 RID: 21925 RVA: 0x00191778 File Offset: 0x0018F978
			private void CheckCaravanMapEvent()
			{
				if (this._questCaravanMobileParty.MapEvent != null && this._questCaravanMobileParty.MapEvent.IsInvulnerable && this._questCaravanMobileParty.MapEvent.BattleStartTime.ElapsedHoursUntilNow > 3f)
				{
					this._questCaravanMobileParty.MapEvent.IsInvulnerable = false;
				}
			}

			// Token: 0x060055A6 RID: 21926 RVA: 0x001917D4 File Offset: 0x0018F9D4
			private void AdjustCaravansSpeed()
			{
				float speed = MobileParty.MainParty.Speed;
				float speed2 = this._questCaravanMobileParty.Speed;
				while (speed < speed2 || speed - speed2 > 1f)
				{
					if (speed2 >= speed)
					{
						this.CaravanCustomPartyComponent.SetBaseSpeed(this.CaravanCustomPartyComponent.BaseSpeed - 0.05f);
					}
					else if (speed - speed2 > 1f)
					{
						this.CaravanCustomPartyComponent.SetBaseSpeed(this.CaravanCustomPartyComponent.BaseSpeed + 0.05f);
					}
					speed = MobileParty.MainParty.Speed;
					speed2 = this._questCaravanMobileParty.Speed;
				}
			}

			// Token: 0x060055A7 RID: 21927 RVA: 0x00191868 File Offset: 0x0018FA68
			private void ThinkAboutSpawningBanditParty()
			{
				if (!this._questBanditPartyAlreadyAttacked && this._questBanditMobileParty == null)
				{
					Settlement targetSettlement = this._questCaravanMobileParty.TargetSettlement;
					if (targetSettlement != null)
					{
						float num2;
						float num = ((this._questCaravanMobileParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(this._questCaravanMobileParty.CurrentSettlement, targetSettlement, false, false, MobileParty.NavigationType.Default) : Campaign.Current.Models.MapDistanceModel.GetDistance(this._questCaravanMobileParty, targetSettlement, false, MobileParty.NavigationType.Default, out num2));
						if (num > 10f && num < this.QuestBanditPartySpawnDistance)
						{
							this.ActivateBanditParty();
							float num3 = this._questBanditMobileParty.Speed + this._questCaravanMobileParty.Speed;
							this._questBanditPartyFollowDuration = (int)(this.QuestBanditPartySpawnDistance / num3) + 5;
							this._questBanditPartyAlreadyAttacked = true;
						}
					}
				}
			}

			// Token: 0x060055A8 RID: 21928 RVA: 0x00191936 File Offset: 0x0018FB36
			private void SimulateSettlementWaitForCaravan()
			{
				this._caravanWaitedInSettlementForHours++;
				if (this._caravanWaitedInSettlementForHours >= 5)
				{
					LeaveSettlementAction.ApplyForParty(this._questCaravanMobileParty);
					this._caravanWaitedInSettlementForHours = 0;
				}
			}

			// Token: 0x060055A9 RID: 21929 RVA: 0x00191964 File Offset: 0x0018FB64
			private void NotifyPlayerOrCancelTheQuestIfCaravanIsFar()
			{
				if (this._questCaravanMobileParty.IsActive && !this._questCaravanMobileParty.IsVisible)
				{
					float num = this._questCaravanMobileParty.Position.Distance(MobileParty.MainParty.Position);
					if (!this._isPlayerNotifiedForDanger && num >= MobileParty.MainParty.SeeingRange + 3f)
					{
						MBInformationManager.AddQuickInformation(new TextObject("{=2y9DhzCR}You are about to lose sight of the caravan. Find the caravan before they are in danger!", null), 0, null, null, "");
						this._isPlayerNotifiedForDanger = true;
						return;
					}
					if (num >= MobileParty.MainParty.SeeingRange + 20f)
					{
						base.AddLog(this.CaravanLostTheTrackLogText, false);
						this.FailConsequences(false);
					}
				}
			}

			// Token: 0x060055AA RID: 21930 RVA: 0x00191A14 File Offset: 0x0018FC14
			private void OnSettlementLeft(MobileParty party, Settlement settlement)
			{
				if (party == this._questCaravanMobileParty)
				{
					this.AdjustCaravansSpeed();
					if (party.TargetSettlement == null || party.TargetSettlement == settlement)
					{
						this.TryToFindAndSetTargetToNextSettlement();
					}
					this._caravanWaitedInSettlementForHours = 0;
					this._questBanditPartyAlreadyAttacked = false;
					this._questCaravanMobileParty.Party.SetAsCameraFollowParty();
					if (base.IsTracked(settlement))
					{
						base.RemoveTrackedObject(settlement);
					}
				}
			}

			// Token: 0x060055AB RID: 21931 RVA: 0x00191A78 File Offset: 0x0018FC78
			private void TryToFindAndSetTargetToNextSettlement()
			{
				int num = 0;
				int num2 = -1;
				do
				{
					num2 = SettlementHelper.FindNextSettlementAroundMobileParty(this._questCaravanMobileParty, MobileParty.NavigationType.Default, 150f, num2, null);
					if (num2 >= 0)
					{
						Settlement settlement = Settlement.All[num2];
						if (this.ProperSettlementCondition(settlement) && settlement != this._questCaravanMobileParty.HomeSettlement && (this._visitedSettlements.Count == 0 || settlement != this._visitedSettlements[this._visitedSettlements.Count - 1]) && !settlement.MapFaction.IsAtWarWith(this._questCaravanMobileParty.MapFaction))
						{
							num++;
						}
					}
				}
				while (num2 >= 0);
				if (num > 0)
				{
					int num3 = MBRandom.RandomInt(num);
					num2 = -1;
					Settlement settlement2;
					for (;;)
					{
						num2 = SettlementHelper.FindNextSettlementAroundMobileParty(this._questCaravanMobileParty, MobileParty.NavigationType.Default, 150f, num2, null);
						if (num2 >= 0)
						{
							settlement2 = Settlement.All[num2];
							if (this.ProperSettlementCondition(settlement2) && settlement2 != this._questCaravanMobileParty.HomeSettlement && (this._visitedSettlements.Count == 0 || settlement2 != this._visitedSettlements[this._visitedSettlements.Count - 1]) && !settlement2.MapFaction.IsAtWarWith(this._questCaravanMobileParty.MapFaction))
							{
								num3--;
								if (num3 < 0)
								{
									break;
								}
							}
						}
						if (num2 < 0)
						{
							return;
						}
					}
					Settlement settlement3 = settlement2;
					SetPartyAiAction.GetActionForVisitingSettlement(this._questCaravanMobileParty, settlement3, MobileParty.NavigationType.Default, false, false);
					this._questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(true);
					TextObject textObject = new TextObject("{=OjI8uGFa}We are traveling to {SETTLEMENT_NAME}.", null);
					textObject.SetTextVariable("SETTLEMENT_NAME", settlement3.Name);
					MBInformationManager.AddQuickInformation(textObject, 100, PartyBaseHelper.GetVisualPartyLeader(this._questCaravanMobileParty.Party), null, "");
					TextObject textObject2 = new TextObject("{=QDpfYm4c}The caravan is moving to {SETTLEMENT_NAME}.", null);
					textObject2.SetTextVariable("SETTLEMENT_NAME", settlement3.EncyclopediaLinkWithName);
					base.AddLog(textObject2, true);
					if (!base.IsTracked(settlement3))
					{
						base.AddTrackedObject(settlement3);
					}
					if (this._questBanditMobileParty == null || !this._questBanditMobileParty.IsActive)
					{
						return;
					}
					float num4 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(this._questCaravanMobileParty, this._questBanditMobileParty, MobileParty.NavigationType.Default);
					if (this._questBanditMobileParty.Speed < this._questCaravanMobileParty.Speed || num4 > 10f)
					{
						this._questBanditMobileParty.SetMoveModeHold();
						this._questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(false);
						this._questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
						if (base.IsTracked(this._questBanditMobileParty))
						{
							base.RemoveTrackedObject(this._questBanditMobileParty);
						}
						this._questBanditMobileParty = null;
						return;
					}
					return;
				}
				this.CaravanNoTargetQuestSuccess();
			}

			// Token: 0x060055AC RID: 21932 RVA: 0x00191D02 File Offset: 0x0018FF02
			private void CaravanNoTargetQuestSuccess()
			{
				this.SuccessConsequences(true);
			}

			// Token: 0x060055AD RID: 21933 RVA: 0x00191D0C File Offset: 0x0018FF0C
			private void OnMapEventEnded(MapEvent mapEvent)
			{
				if (this._questCaravanMobileParty != null && mapEvent.InvolvedParties.Contains(this._questCaravanMobileParty.Party))
				{
					if (mapEvent.HasWinner)
					{
						bool flag = this._questCaravanMobileParty.MapEventSide == MobileParty.MainParty.MapEventSide && mapEvent.IsPlayerMapEvent;
						bool flag2 = mapEvent.Winner == this._questCaravanMobileParty.MapEventSide;
						bool flag3 = mapEvent.InvolvedParties.Contains(PartyBase.MainParty);
						if (!flag2)
						{
							if (!flag3)
							{
								base.AddLog(this.CaravanDestroyedByBanditsLogText, false);
								this.FailConsequences(true);
								return;
							}
							if (flag)
							{
								base.AddLog(this.CaravanDestroyedQuestLogText, false);
								this.FailConsequences(true);
								return;
							}
							this.FailByPlayerHostileConsequences();
							return;
						}
						else
						{
							if (this._questBanditMobileParty != null && this._questBanditMobileParty.IsActive && mapEvent.InvolvedParties.Contains(this._questBanditMobileParty.Party))
							{
								DestroyPartyAction.Apply(MobileParty.MainParty.Party, this._questBanditMobileParty);
							}
							if (this._otherBanditParty != null && this._otherBanditParty.IsActive && mapEvent.InvolvedParties.Contains(this._otherBanditParty.Party))
							{
								DestroyPartyAction.Apply(MobileParty.MainParty.Party, this._otherBanditParty);
							}
							if (this._questCaravanMobileParty.MemberRoster.TotalManCount <= 0)
							{
								this.FailConsequences(true);
							}
							if (this._questCaravanMobileParty.Speed < 2f)
							{
								this._questCaravanMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), 5);
								return;
							}
						}
					}
					else if (this._questCaravanMobileParty.MemberRoster.TotalManCount <= 0)
					{
						this.FailConsequences(true);
					}
				}
			}

			// Token: 0x060055AE RID: 21934 RVA: 0x00191EB8 File Offset: 0x001900B8
			private void SuccessConsequences(bool isNoTargetLeftSuccess)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.TotalRewardGold, false);
				base.QuestGiver.AddPower(10f);
				this.RelationshipChangeWithQuestGiver = 5;
				TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
				});
				base.QuestGiver.CurrentSettlement.Town.Prosperity += 10f;
				if (isNoTargetLeftSuccess)
				{
					base.AddLog(this.CaravanNoTargetLogText, false);
				}
				else
				{
					base.AddLog(this.SuccessQuestLogText, true);
				}
				MobileParty questBanditMobileParty = this._questBanditMobileParty;
				if (questBanditMobileParty != null)
				{
					questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(false);
				}
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x060055AF RID: 21935 RVA: 0x00191F70 File Offset: 0x00190170
			private void FailConsequences(bool banditsWon = false)
			{
				base.QuestGiver.AddPower(-10f);
				this.RelationshipChangeWithQuestGiver = -5;
				TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
				});
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 10f;
				if (this._questBanditMobileParty != null)
				{
					this._questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(false);
					this._questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
					if (base.IsTracked(this._questBanditMobileParty))
					{
						base.RemoveTrackedObject(this._questBanditMobileParty);
					}
				}
				if (this._questCaravanMobileParty != null)
				{
					this._questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(false);
					this._questCaravanMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
				}
				if (this._questBanditMobileParty != null && !banditsWon)
				{
					if (base.IsTracked(this._questBanditMobileParty))
					{
						base.RemoveTrackedObject(this._questBanditMobileParty);
					}
					this._questBanditMobileParty.SetPartyUsedByQuest(false);
					this._questBanditMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
					if (this._questBanditMobileParty.IsActive && this._questBanditMobileParty.IsVisible)
					{
						DestroyPartyAction.Apply(null, this._questBanditMobileParty);
					}
				}
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x060055B0 RID: 21936 RVA: 0x001920B0 File Offset: 0x001902B0
			private void FailByPlayerHostileConsequences()
			{
				base.QuestGiver.AddPower(-10f);
				this.RelationshipChangeWithQuestGiver = -10;
				TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -80)
				});
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 20f;
				base.AddLog(this.CaravanDestroyedByPlayerQuestLogText, true);
				MobileParty questBanditMobileParty = this._questBanditMobileParty;
				if (questBanditMobileParty != null)
				{
					questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(false);
				}
				base.CompleteQuestWithFail(null);
			}

			// Token: 0x060055B1 RID: 21937 RVA: 0x00192142 File Offset: 0x00190342
			protected override void InitializeQuestOnGameLoad()
			{
				MobileParty questCaravanMobileParty = this._questCaravanMobileParty;
				if (questCaravanMobileParty != null && questCaravanMobileParty.IsCaravan)
				{
					base.CompleteQuestWithCancel(null);
				}
				this.SetDialogs();
			}

			// Token: 0x060055B2 RID: 21938 RVA: 0x00192168 File Offset: 0x00190368
			private void ActivateBanditParty()
			{
				Hideout closestHideout = SettlementHelper.FindNearestHideoutToMobileParty(this._questCaravanMobileParty, this._questCaravanMobileParty.NavigationCapability, (Settlement x) => x.IsActive);
				Clan clan = Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
				PartyTemplateObject partyTemplateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("kingdom_hero_party_caravan_ambushers") ?? clan.DefaultPartyTemplate;
				this._questBanditMobileParty = BanditPartyComponent.CreateBanditParty("escort_caravan_quest_" + base.StringId, clan, closestHideout.Settlement.Hideout, false, partyTemplateObject, this._questCaravanMobileParty.TargetSettlement.GatePosition);
				this._questBanditMobileParty.Party.SetCustomName(new TextObject("{=u1Pkt4HC}Raiders", null));
				Campaign.Current.MobilePartyLocator.UpdateLocator(this._questBanditMobileParty);
				this._questBanditMobileParty.ActualClan = clan;
				this._questBanditMobileParty.MemberRoster.Clear();
				for (int i = 0; i < this.BanditPartyTroopCount; i++)
				{
					List<ValueTuple<PartyTemplateStack, float>> list = new List<ValueTuple<PartyTemplateStack, float>>();
					foreach (PartyTemplateStack partyTemplateStack in partyTemplateObject.Stacks)
					{
						list.Add(new ValueTuple<PartyTemplateStack, float>(partyTemplateStack, (float)(64 - partyTemplateStack.Character.Level)));
					}
					PartyTemplateStack partyTemplateStack2 = MBRandom.ChooseWeighted<PartyTemplateStack>(list);
					this._questBanditMobileParty.MemberRoster.AddToCounts(partyTemplateStack2.Character, 1, false, 0, 0, true, -1);
				}
				this._questBanditMobileParty.ItemRoster.AddToCounts(DefaultItems.Grain, this.BanditPartyTroopCount);
				this._questBanditMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), this.BanditPartyTroopCount);
				this._questBanditMobileParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
				SetPartyAiAction.GetActionForEngagingParty(this._questBanditMobileParty, this._questCaravanMobileParty, MobileParty.NavigationType.Default, false);
				this._questBanditMobileParty.Ai.SetDoNotMakeNewDecisions(true);
				base.AddTrackedObject(this._questBanditMobileParty);
			}

			// Token: 0x060055B3 RID: 21939 RVA: 0x001923A0 File Offset: 0x001905A0
			protected override void OnFinalize()
			{
				if (this._questCaravanMobileParty != null && this._questCaravanMobileParty.IsActive && this._questCaravanMobileParty.IsCustomParty)
				{
					CaravanPartyComponent.ConvertPartyToCaravanParty(this._questCaravanMobileParty, base.QuestGiver, base.QuestGiver.CurrentSettlement, false, null, null, false);
					this._questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(false);
					this._questCaravanMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
				}
				if (this._questCaravanMobileParty != null)
				{
					base.RemoveTrackedObject(this._questCaravanMobileParty);
				}
				if (this._otherBanditParty != null && this._otherBanditParty.IsActive)
				{
					this._otherBanditParty.Ai.SetDoNotMakeNewDecisions(false);
					this._otherBanditParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
				}
			}

			// Token: 0x060055B4 RID: 21940 RVA: 0x0019245C File Offset: 0x0019065C
			protected override void OnTimedOut()
			{
				base.QuestGiver.AddPower(-5f);
				this.RelationshipChangeWithQuestGiver = -5;
				TraitLevelingHelper.OnIssueFailed(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -20)
				});
				base.QuestGiver.CurrentSettlement.Town.Prosperity -= 20f;
				base.AddLog(new TextObject("{=pUrSIed8}You have failed to escort the caravan to its destination.", null), false);
			}

			// Token: 0x04001BE3 RID: 7139
			private const int BattleFakeSimulationDuration = 3;

			// Token: 0x04001BE4 RID: 7140
			private const string CustomPartyComponentTalkId = "escort_caravan_talk";

			// Token: 0x04001BE5 RID: 7141
			[SaveableField(2)]
			private readonly int _requiredSettlementNumber;

			// Token: 0x04001BE6 RID: 7142
			[SaveableField(3)]
			private List<Settlement> _visitedSettlements;

			// Token: 0x04001BE7 RID: 7143
			[SaveableField(4)]
			private MobileParty _questCaravanMobileParty;

			// Token: 0x04001BE8 RID: 7144
			[SaveableField(5)]
			private MobileParty _questBanditMobileParty;

			// Token: 0x04001BE9 RID: 7145
			[SaveableField(7)]
			private readonly float _difficultyMultiplier;

			// Token: 0x04001BEA RID: 7146
			[SaveableField(12)]
			private bool _isPlayerNotifiedForDanger;

			// Token: 0x04001BEB RID: 7147
			[SaveableField(26)]
			private MobileParty _otherBanditParty;

			// Token: 0x04001BEC RID: 7148
			[SaveableField(30)]
			private int _questBanditPartyFollowDuration;

			// Token: 0x04001BED RID: 7149
			[SaveableField(31)]
			private int _otherBanditPartyFollowDuration;

			// Token: 0x04001BEE RID: 7150
			[SaveableField(11)]
			private int _daysSpentForEscorting = 1;

			// Token: 0x04001BEF RID: 7151
			private int _caravanWaitedInSettlementForHours;

			// Token: 0x04001BF0 RID: 7152
			[SaveableField(23)]
			private bool _questBanditPartyAlreadyAttacked;

			// Token: 0x04001BF1 RID: 7153
			private CustomPartyComponent _customPartyComponent;

			// Token: 0x04001BF2 RID: 7154
			[SaveableField(1)]
			private JournalLog _playerStartsQuestLog;
		}

		// Token: 0x020006F5 RID: 1781
		public class EscortMerchantCaravanIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x060055B7 RID: 21943 RVA: 0x001924F3 File Offset: 0x001906F3
			public EscortMerchantCaravanIssueTypeDefiner()
				: base(450000)
			{
			}

			// Token: 0x060055B8 RID: 21944 RVA: 0x00192500 File Offset: 0x00190700
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssue), 1, null);
				base.AddClassDefinition(typeof(EscortMerchantCaravanIssueBehavior.EscortMerchantCaravanIssueQuest), 2, null);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.Issues
{
	// Token: 0x020000B0 RID: 176
	public class FamilyFeudIssueBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600075D RID: 1885 RVA: 0x000329EC File Offset: 0x00030BEC
		public override void RegisterEvents()
		{
			CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
		}

		// Token: 0x0600075E RID: 1886 RVA: 0x00032A08 File Offset: 0x00030C08
		public void OnCheckForIssue(Hero hero)
		{
			Settlement value;
			Hero key;
			if (this.ConditionsHold(hero, out value, out key))
			{
				KeyValuePair<Hero, Settlement> keyValuePair = new KeyValuePair<Hero, Settlement>(key, value);
				Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(this.OnStartIssue), typeof(FamilyFeudIssueBehavior.FamilyFeudIssue), IssueBase.IssueFrequency.Rare, keyValuePair));
				return;
			}
			Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(FamilyFeudIssueBehavior.FamilyFeudIssue), IssueBase.IssueFrequency.Rare));
		}

		// Token: 0x0600075F RID: 1887 RVA: 0x00032A80 File Offset: 0x00030C80
		private bool ConditionsHold(Hero issueGiver, out Settlement otherVillage, out Hero otherNotable)
		{
			otherVillage = null;
			otherNotable = null;
			if (!issueGiver.IsNotable)
			{
				return false;
			}
			if (issueGiver.IsRuralNotable && issueGiver.CurrentSettlement.IsVillage)
			{
				Settlement bound = issueGiver.CurrentSettlement.Village.Bound;
				if (bound.IsTown)
				{
					foreach (Village village in bound.BoundVillages.WhereQ((Village x) => x != issueGiver.CurrentSettlement.Village))
					{
						Hero hero = village.Settlement.Notables.FirstOrDefaultQ((Hero y) => y.IsRuralNotable && y.CanHaveCampaignIssues() && y.GetTraitLevel(DefaultTraits.Mercy) <= 0);
						if (hero != null)
						{
							otherVillage = village.Settlement;
							otherNotable = hero;
						}
					}
					return otherVillage != null;
				}
			}
			return false;
		}

		// Token: 0x06000760 RID: 1888 RVA: 0x00032B88 File Offset: 0x00030D88
		private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
		{
			PotentialIssueData potentialIssueData = pid;
			KeyValuePair<Hero, Settlement> keyValuePair = (KeyValuePair<Hero, Settlement>)potentialIssueData.RelatedObject;
			return new FamilyFeudIssueBehavior.FamilyFeudIssue(issueOwner, keyValuePair.Key, keyValuePair.Value);
		}

		// Token: 0x06000761 RID: 1889 RVA: 0x00032BBD File Offset: 0x00030DBD
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x040003F6 RID: 1014
		private const IssueBase.IssueFrequency FamilyFeudIssueFrequency = IssueBase.IssueFrequency.Rare;

		// Token: 0x020001B6 RID: 438
		public class FamilyFeudIssueTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06000F29 RID: 3881 RVA: 0x00066A7E File Offset: 0x00064C7E
			public FamilyFeudIssueTypeDefiner()
				: base(1087000)
			{
			}

			// Token: 0x06000F2A RID: 3882 RVA: 0x00066A8B File Offset: 0x00064C8B
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(FamilyFeudIssueBehavior.FamilyFeudIssue), 1, null);
				base.AddClassDefinition(typeof(FamilyFeudIssueBehavior.FamilyFeudIssueQuest), 2, null);
			}
		}

		// Token: 0x020001B7 RID: 439
		public class FamilyFeudIssueMissionBehavior : MissionLogic
		{
			// Token: 0x06000F2B RID: 3883 RVA: 0x00066AB1 File Offset: 0x00064CB1
			public FamilyFeudIssueMissionBehavior(Action<Agent, Agent, int> agentHitAction)
			{
				this.OnAgentHitAction = agentHitAction;
			}

			// Token: 0x06000F2C RID: 3884 RVA: 0x00066AC0 File Offset: 0x00064CC0
			public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
			{
				Action<Agent, Agent, int> onAgentHitAction = this.OnAgentHitAction;
				if (onAgentHitAction == null)
				{
					return;
				}
				onAgentHitAction(affectedAgent, affectorAgent, blow.InflictedDamage);
			}

			// Token: 0x040007F3 RID: 2035
			private Action<Agent, Agent, int> OnAgentHitAction;
		}

		// Token: 0x020001B8 RID: 440
		public class FamilyFeudIssue : IssueBase
		{
			// Token: 0x1700012D RID: 301
			// (get) Token: 0x06000F2D RID: 3885 RVA: 0x00066ADB File Offset: 0x00064CDB
			public override IssueBase.AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
			{
				get
				{
					return IssueBase.AlternativeSolutionScaleFlag.FailureRisk;
				}
			}

			// Token: 0x1700012E RID: 302
			// (get) Token: 0x06000F2E RID: 3886 RVA: 0x00066ADE File Offset: 0x00064CDE
			public override int AlternativeSolutionBaseNeededMenCount
			{
				get
				{
					return 3 + MathF.Ceiling(5f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x1700012F RID: 303
			// (get) Token: 0x06000F2F RID: 3887 RVA: 0x00066AF3 File Offset: 0x00064CF3
			protected override int AlternativeSolutionBaseDurationInDaysInternal
			{
				get
				{
					return 3 + MathF.Ceiling(7f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000130 RID: 304
			// (get) Token: 0x06000F30 RID: 3888 RVA: 0x00066B08 File Offset: 0x00064D08
			protected override int RewardGold
			{
				get
				{
					return (int)(350f + 1500f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000131 RID: 305
			// (get) Token: 0x06000F31 RID: 3889 RVA: 0x00066B1D File Offset: 0x00064D1D
			// (set) Token: 0x06000F32 RID: 3890 RVA: 0x00066B25 File Offset: 0x00064D25
			[SaveableProperty(30)]
			public override Hero CounterOfferHero { get; protected set; }

			// Token: 0x17000132 RID: 306
			// (get) Token: 0x06000F33 RID: 3891 RVA: 0x00066B2E File Offset: 0x00064D2E
			public override int NeededInfluenceForLordSolution
			{
				get
				{
					return 20;
				}
			}

			// Token: 0x17000133 RID: 307
			// (get) Token: 0x06000F34 RID: 3892 RVA: 0x00066B32 File Offset: 0x00064D32
			protected override int CompanionSkillRewardXP
			{
				get
				{
					return (int)(500f + 700f * base.IssueDifficultyMultiplier);
				}
			}

			// Token: 0x17000134 RID: 308
			// (get) Token: 0x06000F35 RID: 3893 RVA: 0x00066B48 File Offset: 0x00064D48
			protected override TextObject AlternativeSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=zRJ1bQFO}{ISSUE_GIVER.LINK}, a landowner from {ISSUE_GIVER_SETTLEMENT}, told you about an incident that is about to turn into an ugly feud. One of the youngsters killed another in an accident and the victim's family refused blood money as compensation and wants blood. You decided to leave {COMPANION.LINK} with some men for {RETURN_DAYS} days to let things cool down. They should return with the reward of {REWARD_GOLD}{GOLD_ICON} denars as promised by {ISSUE_GIVER.LINK} after {RETURN_DAYS} days.", null);
					StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					textObject.SetTextVariable("ISSUE_GIVER_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					textObject.SetTextVariable("REWARD_GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x17000135 RID: 309
			// (get) Token: 0x06000F36 RID: 3894 RVA: 0x00066BE3 File Offset: 0x00064DE3
			public override bool IsThereAlternativeSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17000136 RID: 310
			// (get) Token: 0x06000F37 RID: 3895 RVA: 0x00066BE6 File Offset: 0x00064DE6
			public override bool IsThereLordSolution
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17000137 RID: 311
			// (get) Token: 0x06000F38 RID: 3896 RVA: 0x00066BE9 File Offset: 0x00064DE9
			public override TextObject IssueBriefByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=7qPda0SA}Yes... We do have a problem. One of my relatives fell victim to his temper during a quarrel and killed a man from {TARGET_VILLAGE}.[ib:normal2][if:convo_dismayed] We offered to pay blood money but the family of the deceased have stubbornly refused it. As it turns out, the deceased is kin to {TARGET_NOTABLE}, an elder of this region and now the men of {TARGET_VILLAGE} have sworn to kill my relative.", null);
					textObject.SetTextVariable("TARGET_VILLAGE", this._targetVillage.Name);
					textObject.SetTextVariable("TARGET_NOTABLE", this._targetNotable.Name);
					return textObject;
				}
			}

			// Token: 0x17000138 RID: 312
			// (get) Token: 0x06000F39 RID: 3897 RVA: 0x00066C24 File Offset: 0x00064E24
			public override TextObject IssueAcceptByPlayer
			{
				get
				{
					return new TextObject("{=XX3sWsVX}This sounds pretty serious. Go on.", null);
				}
			}

			// Token: 0x17000139 RID: 313
			// (get) Token: 0x06000F3A RID: 3898 RVA: 0x00066C34 File Offset: 0x00064E34
			public override TextObject IssueQuestSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=mgUoXwZt}My family is concerned for the boy's life. He has gone hiding around the village commons. We need someone who can protect him until [ib:normal][if:convo_normal]{TARGET_NOTABLE.LINK} sees reason, accepts the blood money and ends the feud. We would be eternally grateful, if you can help my relative and take him with you for a while maybe.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_VILLAGE", this._targetVillage.Name);
					return textObject;
				}
			}

			// Token: 0x1700013A RID: 314
			// (get) Token: 0x06000F3B RID: 3899 RVA: 0x00066C7D File Offset: 0x00064E7D
			public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=cDYz49kZ}You can keep my relative under your protection for a time until the calls for vengeance die down.[ib:closed][if:convo_pondering] Maybe you can leave one of your warrior companions and {ALTERNATIVE_TROOP_COUNT} men with him to protect him.", null);
					textObject.SetTextVariable("ALTERNATIVE_TROOP_COUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					return textObject;
				}
			}

			// Token: 0x1700013B RID: 315
			// (get) Token: 0x06000F3C RID: 3900 RVA: 0x00066C9C File Offset: 0x00064E9C
			protected override TextObject LordSolutionStartLog
			{
				get
				{
					TextObject textObject = new TextObject("{=oJt4bemH}{QUEST_GIVER.LINK}, a landowner from {QUEST_SETTLEMENT}, told you about an incident that is about to turn into an ugly feud. One young man killed another in an quarrel and the victim's family refused blood money compensation, demanding vengeance instead.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x1700013C RID: 316
			// (get) Token: 0x06000F3D RID: 3901 RVA: 0x00066D04 File Offset: 0x00064F04
			protected override TextObject LordSolutionCounterOfferRefuseLog
			{
				get
				{
					TextObject textObject = new TextObject("{=JqN5BSjN}As the dispenser of justice in the district, you decided to allow {TARGET_NOTABLE.LINK} to take vengeance for {?TARGET_NOTABLE.GENDER}her{?}his{\\?} kinsman. You failed to protect the culprit as you promised. {QUEST_GIVER.LINK} is furious.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700013D RID: 317
			// (get) Token: 0x06000F3E RID: 3902 RVA: 0x00066D50 File Offset: 0x00064F50
			protected override TextObject LordSolutionCounterOfferAcceptLog
			{
				get
				{
					TextObject textObject = new TextObject("{=UxrXNSW7}As the ruler, you have let {TARGET_NOTABLE.LINK} to take {?TARGET_NOTABLE.GENDER}her{?}him{\\?} kinsman's vengeance and failed to protect the boy as you have promised to {QUEST_GIVER.LINK}. {QUEST_GIVER.LINK} is furious.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700013E RID: 318
			// (get) Token: 0x06000F3F RID: 3903 RVA: 0x00066DAC File Offset: 0x00064FAC
			public override TextObject IssueLordSolutionExplanationByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=tsjwrZCZ}I am sure that, as {?PLAYER.GENDER}lady{?}lord{\\?} of this district, you will not let these unlawful threats go unpunished. As the lord of the region, you can talk to {TARGET_NOTABLE.LINK} and force him to accept the blood money.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700013F RID: 319
			// (get) Token: 0x06000F40 RID: 3904 RVA: 0x00066DDE File Offset: 0x00064FDE
			public override TextObject IssuePlayerResponseAfterLordExplanation
			{
				get
				{
					return new TextObject("{=A3GfCPUb}I'm not sure about using my authority in this way. Is there any other way to solve this?", null);
				}
			}

			// Token: 0x17000140 RID: 320
			// (get) Token: 0x06000F41 RID: 3905 RVA: 0x00066DEB File Offset: 0x00064FEB
			public override TextObject IssuePlayerResponseAfterAlternativeExplanation
			{
				get
				{
					return new TextObject("{=8EaCJ2uw}What else can I do?", null);
				}
			}

			// Token: 0x17000141 RID: 321
			// (get) Token: 0x06000F42 RID: 3906 RVA: 0x00066DF8 File Offset: 0x00064FF8
			public override TextObject IssueLordSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=Du31GKSb}As the magistrate of this district, I hereby order that blood money shall be accepted. This is a crime of passion, not malice. Tell {TARGET_NOTABLE.LINK} to take the silver or face my wrath!", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000142 RID: 322
			// (get) Token: 0x06000F43 RID: 3907 RVA: 0x00066E2A File Offset: 0x0006502A
			public override TextObject IssueLordSolutionResponseByIssueGiver
			{
				get
				{
					return new TextObject("{=xNyLPMnx}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}, thank you.", null);
				}
			}

			// Token: 0x17000143 RID: 323
			// (get) Token: 0x06000F44 RID: 3908 RVA: 0x00066E38 File Offset: 0x00065038
			public override TextObject IssueLordSolutionCounterOfferExplanationByOtherNpc
			{
				get
				{
					TextObject textObject = new TextObject("{=vjk2q3OT}{?PLAYER.GENDER}Madam{?}Sir{\\?}, {TARGET_NOTABLE.LINK}'s nephew murdered one of my kinsman, [ib:aggressive][if:convo_bared_teeth]and it is our right to take vengeance on the murderer. Custom gives us the right of vengeance. Everyone must know that we are willing to avenge our sons, or others will think little of killing them. Does it do us good to be a clan of old men and women, drowning in silver, if all our sons are slain? Please sir, allow us to take vengeance. We promise we won't let this turn into a senseless blood feud.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000144 RID: 324
			// (get) Token: 0x06000F45 RID: 3909 RVA: 0x00066E7C File Offset: 0x0006507C
			public override TextObject IssueLordSolutionCounterOfferBriefByOtherNpc
			{
				get
				{
					return new TextObject("{=JhbbB2dp}My {?PLAYER.GENDER}lady{?}lord{\\?}, may I have a word please?", null);
				}
			}

			// Token: 0x17000145 RID: 325
			// (get) Token: 0x06000F46 RID: 3910 RVA: 0x00066E89 File Offset: 0x00065089
			public override TextObject IssueLordSolutionCounterOfferAcceptByPlayer
			{
				get
				{
					return new TextObject("{=TIVHLAjy}You may have a point. I hereby revoke my previous decision.", null);
				}
			}

			// Token: 0x17000146 RID: 326
			// (get) Token: 0x06000F47 RID: 3911 RVA: 0x00066E96 File Offset: 0x00065096
			public override TextObject IssueLordSolutionCounterOfferAcceptResponseByOtherNpc
			{
				get
				{
					return new TextObject("{=A9uSikTY}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}.", null);
				}
			}

			// Token: 0x17000147 RID: 327
			// (get) Token: 0x06000F48 RID: 3912 RVA: 0x00066EA3 File Offset: 0x000650A3
			public override TextObject IssueLordSolutionCounterOfferDeclineByPlayer
			{
				get
				{
					return new TextObject("{=Vs9DfZmJ}No. My word is final. You will have to take the blood money.", null);
				}
			}

			// Token: 0x17000148 RID: 328
			// (get) Token: 0x06000F49 RID: 3913 RVA: 0x00066EB0 File Offset: 0x000650B0
			public override TextObject IssueLordSolutionCounterOfferDeclineResponseByOtherNpc
			{
				get
				{
					return new TextObject("{=3oaVUNdr}I hope you won't be [if:convo_disbelief]regret with your decision, my {?PLAYER.GENDER}lady{?}lord{\\?}.", null);
				}
			}

			// Token: 0x17000149 RID: 329
			// (get) Token: 0x06000F4A RID: 3914 RVA: 0x00066EBD File Offset: 0x000650BD
			public override TextObject IssueQuestSolutionAcceptByPlayer
			{
				get
				{
					return new TextObject("{=VcfZdKcp}Don't worry, I will protect your relative.", null);
				}
			}

			// Token: 0x1700014A RID: 330
			// (get) Token: 0x06000F4B RID: 3915 RVA: 0x00066ECA File Offset: 0x000650CA
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=ZpDQxmzJ}Family Feud", null);
				}
			}

			// Token: 0x1700014B RID: 331
			// (get) Token: 0x06000F4C RID: 3916 RVA: 0x00066ED8 File Offset: 0x000650D8
			public override TextObject Description
			{
				get
				{
					TextObject textObject = new TextObject("{=aSZvZRYC}A relative of {QUEST_GIVER.NAME} kills a relative of {TARGET_NOTABLE.NAME}. {QUEST_GIVER.NAME} offers to pay blood money for the crime but {TARGET_NOTABLE.NAME} wants revenge.", null);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700014C RID: 332
			// (get) Token: 0x06000F4D RID: 3917 RVA: 0x00066F22 File Offset: 0x00065122
			public override TextObject IssueAlternativeSolutionAcceptByPlayer
			{
				get
				{
					TextObject textObject = new TextObject("{=9ZngZ6W7}I will have one of my companions and {REQUIRED_TROOP_AMOUNT} of my men protect your kinsman for {RETURN_DAYS} days. ", null);
					textObject.SetTextVariable("REQUIRED_TROOP_AMOUNT", base.GetTotalAlternativeSolutionNeededMenCount());
					textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
					return textObject;
				}
			}

			// Token: 0x1700014D RID: 333
			// (get) Token: 0x06000F4E RID: 3918 RVA: 0x00066F53 File Offset: 0x00065153
			public override TextObject IssueDiscussAlternativeSolution
			{
				get
				{
					TextObject textObject = new TextObject("{=n9QRnxbC}I have no doubt that {TARGET_NOTABLE.LINK} will have to accept[ib:closed][if:convo_grateful] the offer after seeing the boy with that many armed men behind him. Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}, for helping to ending this without more blood.", null);
					textObject.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, false);
					return textObject;
				}
			}

			// Token: 0x1700014E RID: 334
			// (get) Token: 0x06000F4F RID: 3919 RVA: 0x00066F78 File Offset: 0x00065178
			public override TextObject IssueAlternativeSolutionResponseByIssueGiver
			{
				get
				{
					TextObject textObject = new TextObject("{=MaGPKGHA}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}. [if:convo_pondering]I am sure your men will protect the boy and {TARGET_NOTABLE.LINK} will have nothing to do but to accept the blood money. I have to add, I'm ready to pay you {REWARD_GOLD}{GOLD_ICON} denars for your trouble.", null);
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD_GOLD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x1700014F RID: 335
			// (get) Token: 0x06000F50 RID: 3920 RVA: 0x00066FE0 File Offset: 0x000651E0
			public override TextObject IssueAsRumorInSettlement
			{
				get
				{
					TextObject textObject = new TextObject("{=lmVCRD4Q}I hope {QUEST_GIVER.LINK} [if:convo_disbelief]can work out that trouble with {?QUEST_GIVER.GENDER}her{?}his{\\?} kinsman.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x17000150 RID: 336
			// (get) Token: 0x06000F51 RID: 3921 RVA: 0x00067014 File Offset: 0x00065214
			public override TextObject IssueAlternativeSolutionSuccessLog
			{
				get
				{
					TextObject textObject = new TextObject("{=vS6oZJPA}Your companion {COMPANION.LINK} and your men returns with the news of their success. Apparently {TARGET_NOTABLE.LINK} and {?TARGET_NOTABLE.GENDER}her{?}his{\\?} thugs finds the culprit and tries to murder him but your men manages to drive them away. {COMPANION.LINK} tells you that they bloodied their noses so badly that they wouldn’t dare to try again. {QUEST_GIVER.LINK} is grateful and sends {?QUEST_GIVER.GENDER}her{?}his{\\?} regards with a purse full of {REWARD}{GOLD_ICON} denars.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD", this.RewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x06000F52 RID: 3922 RVA: 0x00067099 File Offset: 0x00065299
			public FamilyFeudIssue(Hero issueOwner, Hero targetNotable, Settlement targetVillage)
				: base(issueOwner, CampaignTime.DaysFromNow(30f))
			{
				this._targetNotable = targetNotable;
				this._targetVillage = targetVillage;
			}

			// Token: 0x06000F53 RID: 3923 RVA: 0x000670BA File Offset: 0x000652BA
			public override void OnHeroCanBeSelectedInInventoryInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonResrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000F54 RID: 3924 RVA: 0x000670C4 File Offset: 0x000652C4
			public override void OnHeroCanHavePartyRoleOrBeGovernorInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonResrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000F55 RID: 3925 RVA: 0x000670CE File Offset: 0x000652CE
			public override void OnHeroCanLeadPartyInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonResrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000F56 RID: 3926 RVA: 0x000670D8 File Offset: 0x000652D8
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonResrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000F57 RID: 3927 RVA: 0x000670E2 File Offset: 0x000652E2
			private void CommonResrictionInfoIsRequested(Hero hero, ref bool result)
			{
				if (this._targetNotable == hero)
				{
					result = false;
				}
			}

			// Token: 0x06000F58 RID: 3928 RVA: 0x000670F0 File Offset: 0x000652F0
			protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
			{
				if (issueEffect == DefaultIssueEffects.SettlementSecurity)
				{
					return -1f;
				}
				if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
				{
					return -0.1f;
				}
				return 0f;
			}

			// Token: 0x06000F59 RID: 3929 RVA: 0x00067113 File Offset: 0x00065313
			public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
			{
				return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Athletics) >= hero.GetSkillValue(DefaultSkills.Charm)) ? DefaultSkills.Athletics : DefaultSkills.Charm, 120);
			}

			// Token: 0x06000F5A RID: 3930 RVA: 0x00067140 File Offset: 0x00065340
			protected override void LordSolutionConsequenceWithAcceptCounterOffer()
			{
				TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.IssueOwner, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
				});
				this.RelationshipChangeWithIssueOwner = -10;
				ChangeRelationAction.ApplyPlayerRelation(this._targetNotable, 5, true, true);
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security -= 5f;
			}

			// Token: 0x06000F5B RID: 3931 RVA: 0x000671D9 File Offset: 0x000653D9
			protected override void LordSolutionConsequenceWithRefuseCounterOffer()
			{
				this.ApplySuccessRewards();
			}

			// Token: 0x06000F5C RID: 3932 RVA: 0x000671E1 File Offset: 0x000653E1
			public override bool LordSolutionCondition(out TextObject explanation)
			{
				if (base.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan)
				{
					explanation = null;
					return true;
				}
				explanation = new TextObject("{=9y0zpKUF}You need to be the owner of this settlement!", null);
				return false;
			}

			// Token: 0x06000F5D RID: 3933 RVA: 0x0006720D File Offset: 0x0006540D
			public override bool AlternativeSolutionCondition(out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06000F5E RID: 3934 RVA: 0x00067227 File Offset: 0x00065427
			public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
			{
				return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), out explanation, 2, false);
			}

			// Token: 0x06000F5F RID: 3935 RVA: 0x00067238 File Offset: 0x00065438
			public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
			{
				return character.Tier >= 2;
			}

			// Token: 0x06000F60 RID: 3936 RVA: 0x00067248 File Offset: 0x00065448
			protected override void AlternativeSolutionEndWithSuccessConsequence()
			{
				this.ApplySuccessRewards();
				float randomFloat = MBRandom.RandomFloat;
				SkillObject skill;
				if (randomFloat <= 0.33f)
				{
					skill = DefaultSkills.OneHanded;
				}
				else if (randomFloat <= 0.66f)
				{
					skill = DefaultSkills.TwoHanded;
				}
				else
				{
					skill = DefaultSkills.Polearm;
				}
				base.AlternativeSolutionHero.AddSkillXp(skill, (float)((int)(500f + 700f * base.IssueDifficultyMultiplier)));
			}

			// Token: 0x06000F61 RID: 3937 RVA: 0x000672AC File Offset: 0x000654AC
			protected override void AlternativeSolutionEndWithFailureConsequence()
			{
				this.RelationshipChangeWithIssueOwner = -10;
				ChangeRelationAction.ApplyPlayerRelation(this._targetNotable, 5, true, true);
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security -= 5f;
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
			}

			// Token: 0x06000F62 RID: 3938 RVA: 0x00067328 File Offset: 0x00065528
			private void ApplySuccessRewards()
			{
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				this.RelationshipChangeWithIssueOwner = 10;
				ChangeRelationAction.ApplyPlayerRelation(this._targetNotable, -5, true, true);
				base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security += 10f;
			}

			// Token: 0x06000F63 RID: 3939 RVA: 0x00067387 File Offset: 0x00065587
			protected override void AfterIssueCreation()
			{
				this.CounterOfferHero = base.IssueOwner.CurrentSettlement.Notables.FirstOrDefault((Hero x) => x.CharacterObject.IsHero && x.CharacterObject.HeroObject != base.IssueOwner);
			}

			// Token: 0x06000F64 RID: 3940 RVA: 0x000673B0 File Offset: 0x000655B0
			protected override void OnGameLoad()
			{
			}

			// Token: 0x06000F65 RID: 3941 RVA: 0x000673B2 File Offset: 0x000655B2
			protected override void HourlyTick()
			{
			}

			// Token: 0x06000F66 RID: 3942 RVA: 0x000673B4 File Offset: 0x000655B4
			protected override QuestBase GenerateIssueQuest(string questId)
			{
				return new FamilyFeudIssueBehavior.FamilyFeudIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(20f), this._targetVillage, this._targetNotable, this.RewardGold);
			}

			// Token: 0x06000F67 RID: 3943 RVA: 0x000673DE File Offset: 0x000655DE
			public override IssueBase.IssueFrequency GetFrequency()
			{
				return IssueBase.IssueFrequency.Rare;
			}

			// Token: 0x06000F68 RID: 3944 RVA: 0x000673E4 File Offset: 0x000655E4
			protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out IssueBase.PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
			{
				skill = null;
				relationHero = null;
				flag = IssueBase.PreconditionFlags.None;
				if (issueGiver.GetRelationWithPlayer() < -10f)
				{
					flag |= IssueBase.PreconditionFlags.Relation;
					relationHero = issueGiver;
				}
				if (FactionManager.IsAtWarAgainstFaction(issueGiver.CurrentSettlement.MapFaction, Hero.MainHero.MapFaction))
				{
					flag |= IssueBase.PreconditionFlags.AtWar;
				}
				if (Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
				{
					flag |= IssueBase.PreconditionFlags.CompanionLimitReached;
				}
				return flag == IssueBase.PreconditionFlags.None;
			}

			// Token: 0x06000F69 RID: 3945 RVA: 0x00067460 File Offset: 0x00065660
			public override bool IssueStayAliveConditions()
			{
				return this._targetNotable != null && this._targetNotable.IsActive && (this.CounterOfferHero == null || (this.CounterOfferHero.IsActive && this.CounterOfferHero.CurrentSettlement == base.IssueSettlement));
			}

			// Token: 0x06000F6A RID: 3946 RVA: 0x000674B0 File Offset: 0x000656B0
			protected override void CompleteIssueWithTimedOutConsequences()
			{
			}

			// Token: 0x06000F6B RID: 3947 RVA: 0x000674B2 File Offset: 0x000656B2
			internal static void AutoGeneratedStaticCollectObjectsFamilyFeudIssue(object o, List<object> collectedObjects)
			{
				((FamilyFeudIssueBehavior.FamilyFeudIssue)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06000F6C RID: 3948 RVA: 0x000674C0 File Offset: 0x000656C0
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._targetVillage);
				collectedObjects.Add(this._targetNotable);
				collectedObjects.Add(this.CounterOfferHero);
			}

			// Token: 0x06000F6D RID: 3949 RVA: 0x000674ED File Offset: 0x000656ED
			internal static object AutoGeneratedGetMemberValueCounterOfferHero(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssue)o).CounterOfferHero;
			}

			// Token: 0x06000F6E RID: 3950 RVA: 0x000674FA File Offset: 0x000656FA
			internal static object AutoGeneratedGetMemberValue_targetVillage(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssue)o)._targetVillage;
			}

			// Token: 0x06000F6F RID: 3951 RVA: 0x00067507 File Offset: 0x00065707
			internal static object AutoGeneratedGetMemberValue_targetNotable(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssue)o)._targetNotable;
			}

			// Token: 0x040007F4 RID: 2036
			private const int CompanionRequiredSkillLevel = 120;

			// Token: 0x040007F5 RID: 2037
			private const int QuestTimeLimit = 20;

			// Token: 0x040007F6 RID: 2038
			private const int IssueDuration = 30;

			// Token: 0x040007F7 RID: 2039
			private const int TroopTierForAlternativeSolution = 2;

			// Token: 0x040007F8 RID: 2040
			[SaveableField(10)]
			private Settlement _targetVillage;

			// Token: 0x040007F9 RID: 2041
			[SaveableField(20)]
			private Hero _targetNotable;
		}

		// Token: 0x020001B9 RID: 441
		public class FamilyFeudIssueQuest : QuestBase
		{
			// Token: 0x17000151 RID: 337
			// (get) Token: 0x06000F71 RID: 3953 RVA: 0x0006753B File Offset: 0x0006573B
			public override bool IsRemainingTimeHidden
			{
				get
				{
					return false;
				}
			}

			// Token: 0x17000152 RID: 338
			// (get) Token: 0x06000F72 RID: 3954 RVA: 0x0006753E File Offset: 0x0006573E
			private bool FightEnded
			{
				get
				{
					return this._isCulpritDiedInMissionFight || this._isNotableKnockedDownInMissionFight || this._persuationInDoneAndSuccessfull;
				}
			}

			// Token: 0x17000153 RID: 339
			// (get) Token: 0x06000F73 RID: 3955 RVA: 0x00067558 File Offset: 0x00065758
			public override TextObject Title
			{
				get
				{
					return new TextObject("{=ZpDQxmzJ}Family Feud", null);
				}
			}

			// Token: 0x17000154 RID: 340
			// (get) Token: 0x06000F74 RID: 3956 RVA: 0x00067568 File Offset: 0x00065768
			private TextObject PlayerStartsQuestLogText1
			{
				get
				{
					TextObject textObject = new TextObject("{=rjHQpVDZ}{QUEST_GIVER.LINK} a landowner from {QUEST_GIVER_SETTLEMENT}, told you about an incident that is about to turn into an ugly feud. One of the youngsters killed another during a quarrel and the victim's family refuses the blood money as compensation and wants blood.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000155 RID: 341
			// (get) Token: 0x06000F75 RID: 3957 RVA: 0x000675B8 File Offset: 0x000657B8
			private TextObject PlayerStartsQuestLogText2
			{
				get
				{
					TextObject textObject = new TextObject("{=fgRq7kF2}You agreed to talk to {CULPRIT.LINK} in {QUEST_GIVER_SETTLEMENT} first and convince him to go to {TARGET_NOTABLE.LINK} with you in {TARGET_SETTLEMENT} and mediate the issue between them peacefully and end unnecessary bloodshed. {QUEST_GIVER.LINK} said {?QUEST_GIVER.GENDER}she{?}he{\\?} will pay you {REWARD_GOLD} once the boy is safe again.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_GIVER_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					textObject.SetTextVariable("REWARD_GOLD", this._rewardGold);
					return textObject;
				}
			}

			// Token: 0x17000156 RID: 342
			// (get) Token: 0x06000F76 RID: 3958 RVA: 0x00067660 File Offset: 0x00065860
			private TextObject SuccessQuestSolutionLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=KJ61SXEU}You have successfully protected {CULPRIT.LINK} from harm as you have promised. {QUEST_GIVER.LINK} is grateful for your service and sends his regards with a purse full of {REWARD_GOLD}{GOLD_ICON} denars for your trouble.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					textObject.SetTextVariable("REWARD_GOLD", this._rewardGold);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					return textObject;
				}
			}

			// Token: 0x17000157 RID: 343
			// (get) Token: 0x06000F77 RID: 3959 RVA: 0x000676D0 File Offset: 0x000658D0
			private TextObject CulpritJoinedPlayerPartyLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=s5fXZf2f}You have convinced {CULPRIT.LINK} to go to {TARGET_SETTLEMENT} to face {TARGET_NOTABLE.LINK} to try to solve this issue peacefully. He agreed on the condition that you protect him from his victim's angry relatives.", null);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000158 RID: 344
			// (get) Token: 0x06000F78 RID: 3960 RVA: 0x00067734 File Offset: 0x00065934
			private TextObject QuestGiverVillageRaidedBeforeTalkingToCulpritCancel
			{
				get
				{
					TextObject textObject = new TextObject("{=gJG0xmAq}{QUEST_GIVER.LINK}'s village {QUEST_SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					textObject.SetTextVariable("QUEST_SETTLEMENT", base.QuestGiver.CurrentSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x17000159 RID: 345
			// (get) Token: 0x06000F79 RID: 3961 RVA: 0x00067784 File Offset: 0x00065984
			private TextObject TargetVillageRaidedBeforeTalkingToCulpritCancel
			{
				get
				{
					TextObject textObject = new TextObject("{=WqY4nvHc}{TARGET_NOTABLE.LINK}'s village {TARGET_SETTLEMENT} was raided. Your agreement with {QUEST_GIVER.LINK} is canceled.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this._targetSettlement.EncyclopediaLinkWithName);
					return textObject;
				}
			}

			// Token: 0x1700015A RID: 346
			// (get) Token: 0x06000F7A RID: 3962 RVA: 0x000677E8 File Offset: 0x000659E8
			private TextObject CulpritDiedQuestFail
			{
				get
				{
					TextObject textObject = new TextObject("{=6zcG8eng}You tried to defend {CULPRIT.LINK} but you were overcome. {NOTABLE.LINK} took {?NOTABLE.GENDER}her{?}his{\\?} revenge. You failed to protect {CULPRIT.LINK} as promised to {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}she{?}he{\\?} is furious.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700015B RID: 347
			// (get) Token: 0x06000F7B RID: 3963 RVA: 0x0006784C File Offset: 0x00065A4C
			private TextObject PlayerDiedInNotableBattle
			{
				get
				{
					TextObject textObject = new TextObject("{=kG92fjCY}You fell unconscious while defending {CULPRIT.LINK}. {TARGET_NOTABLE.LINK} has taken revenge. You failed to protect {CULPRIT.LINK} as you promised {QUEST_GIVER.LINK}. {?QUEST_GIVER.GENDER}She{?}He{\\?} is furious.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700015C RID: 348
			// (get) Token: 0x06000F7C RID: 3964 RVA: 0x000678B0 File Offset: 0x00065AB0
			private TextObject FailQuestLogText
			{
				get
				{
					TextObject textObject = new TextObject("{=LWjIbTBi}You failed to protect {CULPRIT.LINK} as you promised {QUEST_GIVER.LINK}. {QUEST_GIVER.LINK} is furious.", null);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700015D RID: 349
			// (get) Token: 0x06000F7D RID: 3965 RVA: 0x000678FC File Offset: 0x00065AFC
			private TextObject CulpritNoLongerAClanMember
			{
				get
				{
					TextObject textObject = new TextObject("{=wWrEvkuj}{CULPRIT.LINK} is no longer a member of your clan. Your agreement with {QUEST_GIVER.LINK} was terminated.", null);
					StringHelpers.SetCharacterProperties("CULPRIT", this._culprit.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					return textObject;
				}
			}

			// Token: 0x1700015E RID: 350
			// (get) Token: 0x06000F7E RID: 3966 RVA: 0x00067946 File Offset: 0x00065B46
			private TextObject CompanionLimitReachedQuestLogText
			{
				get
				{
					return new TextObject("{=rkQ7D36f}The quest was canceled because your party had more companions than you could manage.", null);
				}
			}

			// Token: 0x06000F7F RID: 3967 RVA: 0x00067954 File Offset: 0x00065B54
			public FamilyFeudIssueQuest(string questId, Hero questGiver, CampaignTime duration, Settlement targetSettlement, Hero targetHero, int rewardGold)
				: base(questId, questGiver, duration, rewardGold)
			{
				this._targetSettlement = targetSettlement;
				this._targetNotable = targetHero;
				this._culpritJoinedPlayerParty = false;
				this._checkForMissionEvents = false;
				this._culprit = HeroCreator.CreateSpecialHero(MBObjectManager.Instance.GetObject<CharacterObject>("townsman_" + targetSettlement.Culture.StringId), targetSettlement, null, null, -1);
				this._culprit.SetNewOccupation(Occupation.Wanderer);
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>("pugio");
				this._culprit.CivilianEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(@object, null, null, false));
				this._culprit.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(@object, null, null, false));
				this._notableGangsterCharacterObject = questGiver.CurrentSettlement.MapFaction.Culture.GangleaderBodyguard;
				this._rewardGold = rewardGold;
				this.InitializeQuestDialogs();
				this.SetDialogs();
				base.InitializeQuestOnCreation();
			}

			// Token: 0x06000F80 RID: 3968 RVA: 0x00067A40 File Offset: 0x00065C40
			private void InitializeQuestDialogs()
			{
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCulpritDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableThugDialogFlow(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlowBeforeTalkingToCulprit(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlowAfterTalkingToCulprit(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlowAfterKillingCulprit(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlowAfterPlayerBetrayCulprit(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCulpritDialogFlowAfterCulpritJoin(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlowAfterNotableKnowdown(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlowAfterQuestEnd(), this);
				Campaign.Current.ConversationManager.AddDialogFlow(this.GetCulpritDialogFlowAfterQuestEnd(), this);
			}

			// Token: 0x06000F81 RID: 3969 RVA: 0x00067B29 File Offset: 0x00065D29
			protected override void InitializeQuestOnGameLoad()
			{
				this.SetDialogs();
				this.InitializeQuestDialogs();
				this._notableGangsterCharacterObject = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
			}

			// Token: 0x06000F82 RID: 3970 RVA: 0x00067B4C File Offset: 0x00065D4C
			protected override void HourlyTick()
			{
			}

			// Token: 0x06000F83 RID: 3971 RVA: 0x00067B50 File Offset: 0x00065D50
			private DialogFlow GetNotableDialogFlowBeforeTalkingToCulprit()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=dpTHWqwv}Are you the {?PLAYER.GENDER}woman{?}man{\\?} who thinks our blood is cheap, that we will accept silver for the life of one of our own?", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.notable_culprit_is_not_near_on_condition))
					.NpcLine(new TextObject("{=Vd22iVGE}Well {?PLAYER.GENDER}lady{?}sir{\\?}, sorry to disappoint you, but our people have some self-respect.", null), null, null, null, null)
					.PlayerLine(new TextObject("{=a3AFjfsU}We will see. ", null), null, null, null)
					.NpcLine(new TextObject("{=AeJqCMJc}Yes, you will see. Good day to you. ", null), null, null, null, null)
					.CloseDialog();
			}

			// Token: 0x06000F84 RID: 3972 RVA: 0x00067BD0 File Offset: 0x00065DD0
			private DialogFlow GetNotableDialogFlowAfterKillingCulprit()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=108Dchvt}Stop! We don't need to fight any longer. We have no quarrel with you as justice has been served.", null), null, null, null, null).Condition(() => this._isCulpritDiedInMissionFight && Hero.OneToOneConversationHero == this._targetNotable && !this._playerBetrayedCulprit)
					.NpcLine(new TextObject("{=NMrzr7Me}Now, leave peacefully...", null), null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.CulpritDiedInNotableFightFail;
					})
					.CloseDialog();
			}

			// Token: 0x06000F85 RID: 3973 RVA: 0x00067C38 File Offset: 0x00065E38
			private DialogFlow GetNotableDialogFlowAfterPlayerBetrayCulprit()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=4aiabOd4}I knew you are a reasonable {?PLAYER.GENDER}woman{?}man{\\?}.", null), null, null, null, null).Condition(() => this._isCulpritDiedInMissionFight && this._playerBetrayedCulprit && Hero.OneToOneConversationHero == this._targetNotable)
					.NpcLine(new TextObject("{=NMrzr7Me}Now, leave peacefully...", null), null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.CulpritDiedInNotableFightFail;
					})
					.CloseDialog();
			}

			// Token: 0x06000F86 RID: 3974 RVA: 0x00067CA0 File Offset: 0x00065EA0
			private DialogFlow GetCulpritDialogFlowAfterCulpritJoin()
			{
				TextObject textObject = new TextObject("{=56ynu2bW}Yes, {?PLAYER.GENDER}milady{?}sir{\\?}.", null);
				TextObject textObject2 = new TextObject("{=c452Kevh}Well I'm anxious, but I am in your hands now. I trust you will protect me {?PLAYER.GENDER}milady{?}sir{\\?}.", null);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject, false);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2, false);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null, null, null).Condition(() => !this.FightEnded && this._culpritJoinedPlayerParty && Hero.OneToOneConversationHero == this._culprit)
					.PlayerLine(new TextObject("{=p1ETQbzg}Just checking on you.", null), null, null, null)
					.NpcLine(textObject2, null, null, null, null)
					.CloseDialog();
			}

			// Token: 0x06000F87 RID: 3975 RVA: 0x00067D34 File Offset: 0x00065F34
			private DialogFlow GetNotableDialogFlowAfterQuestEnd()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=UBFS1JLj}I have no problem with the boy anymore,[ib:closed][if:convo_annoyed] okay? Just leave me alone.", null), null, null, null, null).Condition(() => this.FightEnded && !this._persuationInDoneAndSuccessfull && Hero.OneToOneConversationHero == this._targetNotable && !this._playerBetrayedCulprit)
					.CloseDialog()
					.NpcLine(new TextObject("{=adbQR9j0}I got my gold, you got your boy.[if:convo_bored2] Now leave me alone...", null), null, null, null, null)
					.Condition(() => this.FightEnded && this._persuationInDoneAndSuccessfull && Hero.OneToOneConversationHero == this._targetNotable && !this._playerBetrayedCulprit)
					.CloseDialog();
			}

			// Token: 0x06000F88 RID: 3976 RVA: 0x00067DA1 File Offset: 0x00065FA1
			private DialogFlow GetCulpritDialogFlowAfterQuestEnd()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=OybG76Kf}Thank you for saving me, sir.[ib:normal][if:convo_astonished] I won't forget what you did here today.", null), null, null, null, null).Condition(() => this.FightEnded && Hero.OneToOneConversationHero == this._culprit)
					.CloseDialog();
			}

			// Token: 0x06000F89 RID: 3977 RVA: 0x00067DDC File Offset: 0x00065FDC
			private DialogFlow GetNotableDialogFlowAfterNotableKnowdown()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=c6GbRQlg}Stop. We don’t need to fight any longer. [ib:closed][if:convo_insulted]You have made your point. We will accept the blood money.", null), (IAgent agent) => this.IsTargetNotable(agent), (IAgent agent) => this.IsMainAgent(agent), null, null).Condition(new ConversationSentence.OnConditionDelegate(this.multi_character_conversation_condition_after_fight))
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.multi_character_conversation_consequence_after_fight))
					.NpcLine(new TextObject("{=pS0bBRjt}You! Go to your family and tell [if:convo_angry]them to send us the blood money.", null), (IAgent agent) => this.IsTargetNotable(agent), (IAgent agent) => this.IsCulprit(agent), null, null)
					.NpcLine(new TextObject("{=nxs2U0Yk}Leave now and never come back! [if:convo_furious]If we ever see you here we will kill you.", null), (IAgent agent) => this.IsTargetNotable(agent), (IAgent agent) => this.IsCulprit(agent), null, null)
					.NpcLine("{=udD7Y7mO}Thank you, my {?PLAYER.GENDER}lady{?}sir{\\?}, for protecting me. I will go and tell {ISSUE_GIVER.LINK} of your success here.", (IAgent agent) => this.IsCulprit(agent), (IAgent agent) => this.IsMainAgent(agent), null, null)
					.Condition(new ConversationSentence.OnConditionDelegate(this.AfterNotableKnowdownEndingCondition))
					.PlayerLine(new TextObject("{=g8qb3Ame}Thank you.", null), (IAgent agent) => this.IsCulprit(agent), null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.PlayerAndCulpritKnockedDownNotableQuestSuccess;
					})
					.CloseDialog();
			}

			// Token: 0x06000F8A RID: 3978 RVA: 0x00067EFE File Offset: 0x000660FE
			private bool AfterNotableKnowdownEndingCondition()
			{
				StringHelpers.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, null, false);
				return true;
			}

			// Token: 0x06000F8B RID: 3979 RVA: 0x00067F19 File Offset: 0x00066119
			private void PlayerAndCulpritKnockedDownNotableQuestSuccess()
			{
				this._conversationAfterFightIsDone = true;
				this.HandleAgentBehaviorAfterQuestConversations();
			}

			// Token: 0x06000F8C RID: 3980 RVA: 0x00067F28 File Offset: 0x00066128
			private void HandleAgentBehaviorAfterQuestConversations()
			{
				foreach (AccompanyingCharacter accompanyingCharacter in PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer)
				{
					if (accompanyingCharacter.LocationCharacter.Character == this._culprit.CharacterObject && this._culpritAgent.IsActive())
					{
						accompanyingCharacter.LocationCharacter.SpecialTargetTag = "npc_common";
						accompanyingCharacter.LocationCharacter.CharacterRelation = LocationCharacter.CharacterRelations.Neutral;
						this._culpritAgent.SetMortalityState(Agent.MortalityState.Invulnerable);
						this._culpritAgent.SetTeam(Team.Invalid, false);
						DailyBehaviorGroup behaviorGroup = this._culpritAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
						behaviorGroup.AddBehavior<WalkingBehavior>();
						behaviorGroup.RemoveBehavior<FollowAgentBehavior>();
						this._culpritAgent.ResetEnemyCaches();
						this._culpritAgent.InvalidateTargetAgent();
						this._culpritAgent.InvalidateAIWeaponSelections();
						this._culpritAgent.SetWatchState(Agent.WatchState.Patrolling);
						if (this._notableAgent != null)
						{
							this._notableAgent.ResetEnemyCaches();
							this._notableAgent.InvalidateTargetAgent();
							this._notableAgent.InvalidateAIWeaponSelections();
							this._notableAgent.SetWatchState(Agent.WatchState.Patrolling);
						}
						this._culpritAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
						this._culpritAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
					}
				}
				Mission.Current.SetMissionMode(MissionMode.StartUp, false);
			}

			// Token: 0x06000F8D RID: 3981 RVA: 0x00068098 File Offset: 0x00066298
			private void ApplySuccessConsequences()
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold, false);
				GainRenownAction.Apply(Hero.MainHero, 1f, false);
				this.RelationshipChangeWithQuestGiver = 10;
				ChangeRelationAction.ApplyPlayerRelation(this._targetNotable, -5, true, true);
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security += 10f;
				base.CompleteQuestWithSuccess();
			}

			// Token: 0x06000F8E RID: 3982 RVA: 0x0006810F File Offset: 0x0006630F
			private bool multi_character_conversation_condition_after_fight()
			{
				return !this._conversationAfterFightIsDone && Hero.OneToOneConversationHero == this._targetNotable && this._isNotableKnockedDownInMissionFight;
			}

			// Token: 0x06000F8F RID: 3983 RVA: 0x0006812E File Offset: 0x0006632E
			private void multi_character_conversation_consequence_after_fight()
			{
				if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null)
				{
					Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { this._culpritAgent }, true);
				}
				this._conversationAfterFightIsDone = true;
			}

			// Token: 0x06000F90 RID: 3984 RVA: 0x00068164 File Offset: 0x00066364
			private DialogFlow GetNotableDialogFlowAfterTalkingToCulprit()
			{
				DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=nh7a3Nog}Well well. Who did you bring to see us? [ib:confident][if:convo_irritable]Did he bring his funeral shroud with him? I hope so. He's not leaving here alive.", null), (IAgent agent) => this.IsTargetNotable(agent), (IAgent agent) => this.IsCulprit(agent), null, null).Condition(new ConversationSentence.OnConditionDelegate(this.multi_character_conversation_on_condition))
					.NpcLine(new TextObject("{=RsOmvdmU}We have come to talk! Just listen to us please![if:convo_shocked]", null), (IAgent agent) => this.IsCulprit(agent), (IAgent agent) => this.IsTargetNotable(agent), null, null)
					.NpcLine("{=JUjvu4XL}I knew we'd find you eventually. Now you will face justice![if:convo_evil_smile]", (IAgent agent) => this.IsTargetNotable(agent), (IAgent agent) => this.IsCulprit(agent), null, null)
					.PlayerLine("{=UQyCoQCY}Wait! This lad is now under my protection. We have come to talk in peace..", (IAgent agent) => this.IsTargetNotable(agent), null, null)
					.NpcLine("{=7AiP4BwY}What there is to talk about? [if:convo_confused_annoyed]This bastard murdered one of my kinsman, and it is our right to take vengeance on him!", (IAgent agent) => this.IsTargetNotable(agent), (IAgent agent) => this.IsMainAgent(agent), null, null)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=2iVytG2y}I am not convinced. I will protect the accused until you see reason.", null), null, null, null)
					.NpcLine(new TextObject("{=4HokUcma}You will regret pushing [if:convo_very_stern]your nose into issues that do not concern you!", null), null, null, null, null)
					.NpcLine(new TextObject("{=vjOkDM6C}If you defend a murderer [ib:warrior][if:convo_furious]then you die like a murderer. Boys, kill them all!", null), null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
						{
							this.StartFightWithNotableGang(false);
						};
					})
					.CloseDialog()
					.PlayerOption(new TextObject("{=boAcQxVV}You're breaking the law.", null), null, null, null)
					.Condition(delegate
					{
						if (this._task != null)
						{
							return !this._task.Options.All((PersuasionOptionArgs x) => x.IsBlocked);
						}
						return true;
					})
					.GotoDialogState("start_notable_family_feud_persuasion")
					.PlayerOption(new TextObject("{=J5cQPqGQ}You are right. You are free to deliver justice as you see fit.", null), null, null, null)
					.NpcLine(new TextObject("{=aRPLW15x}Thank you. I knew you are a reasonable[ib:aggressive][if:convo_evil_smile] {?PLAYER.GENDER}woman{?}man{\\?}.", null), null, null, null, null)
					.NpcLine(new TextObject("{=k5R4qGtL}What? Are you just going [ib:nervous][if:convo_nervous2]to leave me here to be killed? My kin will never forget this!", null), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsCulprit), new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainAgent), null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
						{
							this._playerBetrayedCulprit = true;
							this.StartFightWithNotableGang(this._playerBetrayedCulprit);
						};
					})
					.CloseDialog();
				this.AddPersuasionDialogs(dialogFlow);
				return dialogFlow;
			}

			// Token: 0x06000F91 RID: 3985 RVA: 0x00068338 File Offset: 0x00066538
			private bool IsMainAgent(IAgent agent)
			{
				return agent == Mission.Current.MainAgent;
			}

			// Token: 0x06000F92 RID: 3986 RVA: 0x00068347 File Offset: 0x00066547
			private bool IsTargetNotable(IAgent agent)
			{
				return agent.Character == this._targetNotable.CharacterObject;
			}

			// Token: 0x06000F93 RID: 3987 RVA: 0x0006835C File Offset: 0x0006655C
			private bool IsCulprit(IAgent agent)
			{
				return agent.Character == this._culprit.CharacterObject;
			}

			// Token: 0x06000F94 RID: 3988 RVA: 0x00068374 File Offset: 0x00066574
			private bool notable_culprit_is_not_near_on_condition()
			{
				return Hero.OneToOneConversationHero == this._targetNotable && Mission.Current != null && !this.FightEnded && Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 10f, new MBList<Agent>()).All((Agent a) => a.Character != this._culprit.CharacterObject);
			}

			// Token: 0x06000F95 RID: 3989 RVA: 0x000683D8 File Offset: 0x000665D8
			private bool multi_character_conversation_on_condition()
			{
				if (Hero.OneToOneConversationHero != this._targetNotable || Mission.Current == null || this.FightEnded)
				{
					return false;
				}
				MBList<Agent> nearbyAgents = Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 10f, new MBList<Agent>());
				if (nearbyAgents.IsEmpty<Agent>() || nearbyAgents.All((Agent a) => a.Character != this._culprit.CharacterObject))
				{
					return false;
				}
				foreach (Agent agent in nearbyAgents)
				{
					if (agent.Character == this._culprit.CharacterObject)
					{
						this._culpritAgent = agent;
						if (Mission.Current.GetMissionBehavior<MissionConversationLogic>() != null)
						{
							Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { this._culpritAgent }, true);
							break;
						}
						break;
					}
				}
				return true;
			}

			// Token: 0x06000F96 RID: 3990 RVA: 0x000684CC File Offset: 0x000666CC
			private void AddPersuasionDialogs(DialogFlow dialog)
			{
				dialog.AddDialogLine("family_feud_notable_persuasion_check_accepted", "start_notable_family_feud_persuasion", "family_feud_notable_persuasion_start_reservation", "{=6P1ruzsC}Maybe...", null, new ConversationSentence.OnConsequenceDelegate(this.persuasion_start_with_notable_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("family_feud_notable_persuasion_failed", "family_feud_notable_persuasion_start_reservation", "persuation_failed", "{=!}{FAILED_PERSUASION_LINE}", new ConversationSentence.OnConditionDelegate(this.persuasion_failed_with_family_feud_notable_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_failed_with_notable_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("family_feud_notable_persuasion_rejected", "persuation_failed", "close_window", "{=vjOkDM6C}If you defend a murderer [ib:warrior][if:convo_furious]then you die like a murderer. Boys, kill them all!", null, new ConversationSentence.OnConsequenceDelegate(this.persuasion_failed_with_notable_start_fight_on_consequence), this, 100, null, null, null);
				dialog.AddDialogLine("family_feud_notable_persuasion_attempt", "family_feud_notable_persuasion_start_reservation", "family_feud_notable_persuasion_select_option", "{CONTINUE_PERSUASION_LINE}", () => !this.persuasion_failed_with_family_feud_notable_on_condition(), null, this, 100, null, null, null);
				dialog.AddDialogLine("family_feud_notable_persuasion_success", "family_feud_notable_persuasion_start_reservation", "close_window", "{=qIQbIjVS}All right! I spare the boy's life. Now get out of my sight[ib:closed][if:convo_nonchalant]", new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied), new ConversationSentence.OnConsequenceDelegate(this.persuasion_complete_with_notable_on_consequence), this, int.MaxValue, null, null, null);
				string id = "family_feud_notable_persuasion_select_option_1";
				string inputToken = "family_feud_notable_persuasion_select_option";
				string outputToken = "family_feud_notable_persuasion_selected_option_response";
				string text = "{=!}{FAMILY_FEUD_PERSUADE_ATTEMPT_1}";
				ConversationSentence.OnConditionDelegate conditionDelegate = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_1_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_1_on_consequence);
				ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_1);
				ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_1_on_condition);
				dialog.AddPlayerLine(id, inputToken, outputToken, text, conditionDelegate, consequenceDelegate, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id2 = "family_feud_notable_persuasion_select_option_2";
				string inputToken2 = "family_feud_notable_persuasion_select_option";
				string outputToken2 = "family_feud_notable_persuasion_selected_option_response";
				string text2 = "{=!}{FAMILY_FEUD_PERSUADE_ATTEMPT_2}";
				ConversationSentence.OnConditionDelegate conditionDelegate2 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_2_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_2_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_2);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_2_on_condition);
				dialog.AddPlayerLine(id2, inputToken2, outputToken2, text2, conditionDelegate2, consequenceDelegate2, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				string id3 = "family_feud_notable_persuasion_select_option_3";
				string inputToken3 = "family_feud_notable_persuasion_select_option";
				string outputToken3 = "family_feud_notable_persuasion_selected_option_response";
				string text3 = "{=!}{FAMILY_FEUD_PERSUADE_ATTEMPT_3}";
				ConversationSentence.OnConditionDelegate conditionDelegate3 = new ConversationSentence.OnConditionDelegate(this.persuasion_select_option_3_on_condition);
				ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new ConversationSentence.OnConsequenceDelegate(this.persuasion_select_option_3_on_consequence);
				persuasionOptionDelegate = new ConversationSentence.OnPersuasionOptionDelegate(this.persuasion_setup_option_3);
				clickableConditionDelegate = new ConversationSentence.OnClickableConditionDelegate(this.persuasion_clickable_option_3_on_condition);
				dialog.AddPlayerLine(id3, inputToken3, outputToken3, text3, conditionDelegate3, consequenceDelegate3, this, 100, clickableConditionDelegate, persuasionOptionDelegate, null, null);
				dialog.AddDialogLine("family_feud_notable_persuasion_select_option_reaction", "family_feud_notable_persuasion_selected_option_response", "family_feud_notable_persuasion_start_reservation", "{=D0xDRqvm}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.persuasion_selected_option_response_on_condition), new ConversationSentence.OnConsequenceDelegate(this.persuasion_selected_option_response_on_consequence), this, 100, null, null, null);
			}

			// Token: 0x06000F97 RID: 3991 RVA: 0x0006870D File Offset: 0x0006690D
			private void persuasion_complete_with_notable_on_consequence()
			{
				ConversationManager.EndPersuasion();
				this._persuationInDoneAndSuccessfull = true;
				this.HandleAgentBehaviorAfterQuestConversations();
			}

			// Token: 0x06000F98 RID: 3992 RVA: 0x00068721 File Offset: 0x00066921
			private void persuasion_failed_with_notable_on_consequence()
			{
				ConversationManager.EndPersuasion();
			}

			// Token: 0x06000F99 RID: 3993 RVA: 0x00068728 File Offset: 0x00066928
			private void persuasion_failed_with_notable_start_fight_on_consequence()
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
				{
					this.StartFightWithNotableGang(false);
				};
			}

			// Token: 0x06000F9A RID: 3994 RVA: 0x00068748 File Offset: 0x00066948
			private bool persuasion_failed_with_family_feud_notable_on_condition()
			{
				MBTextManager.SetTextVariable("CONTINUE_PERSUASION_LINE", "{=7B7BhVhV}Let's see what you will come up with...[if:convo_confused_annoyed]", false);
				if (this._task.Options.Any((PersuasionOptionArgs x) => x.IsBlocked))
				{
					MBTextManager.SetTextVariable("CONTINUE_PERSUASION_LINE", "{=wvbiyZfp}What else do you have to say?[if:convo_confused_annoyed]", false);
				}
				if (this._task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
				{
					MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", this._task.FinalFailLine, false);
					return true;
				}
				return false;
			}

			// Token: 0x06000F9B RID: 3995 RVA: 0x000687F8 File Offset: 0x000669F8
			private void persuasion_selected_option_response_on_consequence()
			{
				Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
				float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.MediumHard);
				float moveToNextStageChance;
				float blockRandomOptionChance;
				Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
				this._task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
			}

			// Token: 0x06000F9C RID: 3996 RVA: 0x00068854 File Offset: 0x00066A54
			private bool persuasion_selected_option_response_on_condition()
			{
				PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
				MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
				return true;
			}

			// Token: 0x06000F9D RID: 3997 RVA: 0x00068883 File Offset: 0x00066A83
			private void persuasion_start_with_notable_on_consequence()
			{
				this._task = this.GetPersuasionTask();
				ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
			}

			// Token: 0x06000F9E RID: 3998 RVA: 0x000688B8 File Offset: 0x00066AB8
			private bool persuasion_select_option_1_on_condition()
			{
				if (this._task.Options.Count > 0)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(0), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(0).Line);
					MBTextManager.SetTextVariable("FAMILY_FEUD_PERSUADE_ATTEMPT_1", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06000F9F RID: 3999 RVA: 0x00068938 File Offset: 0x00066B38
			private bool persuasion_select_option_2_on_condition()
			{
				if (this._task.Options.Count > 1)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(1), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(1).Line);
					MBTextManager.SetTextVariable("FAMILY_FEUD_PERSUADE_ATTEMPT_2", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06000FA0 RID: 4000 RVA: 0x000689B8 File Offset: 0x00066BB8
			private bool persuasion_select_option_3_on_condition()
			{
				if (this._task.Options.Count > 2)
				{
					TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
					textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(2), false));
					textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._task.Options.ElementAt(2).Line);
					MBTextManager.SetTextVariable("FAMILY_FEUD_PERSUADE_ATTEMPT_3", textObject, false);
					return true;
				}
				return false;
			}

			// Token: 0x06000FA1 RID: 4001 RVA: 0x00068A38 File Offset: 0x00066C38
			private void persuasion_select_option_1_on_consequence()
			{
				if (this._task.Options.Count > 0)
				{
					this._task.Options[0].BlockTheOption(true);
				}
			}

			// Token: 0x06000FA2 RID: 4002 RVA: 0x00068A64 File Offset: 0x00066C64
			private void persuasion_select_option_2_on_consequence()
			{
				if (this._task.Options.Count > 1)
				{
					this._task.Options[1].BlockTheOption(true);
				}
			}

			// Token: 0x06000FA3 RID: 4003 RVA: 0x00068A90 File Offset: 0x00066C90
			private void persuasion_select_option_3_on_consequence()
			{
				if (this._task.Options.Count > 2)
				{
					this._task.Options[2].BlockTheOption(true);
				}
			}

			// Token: 0x06000FA4 RID: 4004 RVA: 0x00068ABC File Offset: 0x00066CBC
			private PersuasionOptionArgs persuasion_setup_option_1()
			{
				return this._task.Options.ElementAt(0);
			}

			// Token: 0x06000FA5 RID: 4005 RVA: 0x00068ACF File Offset: 0x00066CCF
			private PersuasionOptionArgs persuasion_setup_option_2()
			{
				return this._task.Options.ElementAt(1);
			}

			// Token: 0x06000FA6 RID: 4006 RVA: 0x00068AE2 File Offset: 0x00066CE2
			private PersuasionOptionArgs persuasion_setup_option_3()
			{
				return this._task.Options.ElementAt(2);
			}

			// Token: 0x06000FA7 RID: 4007 RVA: 0x00068AF8 File Offset: 0x00066CF8
			private bool persuasion_clickable_option_1_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Any<PersuasionOptionArgs>())
				{
					hintText = (this._task.Options.ElementAt(0).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(0).IsBlocked;
				}
				return false;
			}

			// Token: 0x06000FA8 RID: 4008 RVA: 0x00068B60 File Offset: 0x00066D60
			private bool persuasion_clickable_option_2_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 1)
				{
					hintText = (this._task.Options.ElementAt(1).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(1).IsBlocked;
				}
				return false;
			}

			// Token: 0x06000FA9 RID: 4009 RVA: 0x00068BC8 File Offset: 0x00066DC8
			private bool persuasion_clickable_option_3_on_condition(out TextObject hintText)
			{
				hintText = new TextObject("{=9ACJsI6S}Blocked", null);
				if (this._task.Options.Count > 2)
				{
					hintText = (this._task.Options.ElementAt(2).IsBlocked ? hintText : null);
					return !this._task.Options.ElementAt(2).IsBlocked;
				}
				return false;
			}

			// Token: 0x06000FAA RID: 4010 RVA: 0x00068C30 File Offset: 0x00066E30
			private PersuasionTask GetPersuasionTask()
			{
				PersuasionTask persuasionTask = new PersuasionTask(0);
				persuasionTask.FinalFailLine = new TextObject("{=rzGqa5oD}Revenge will be taken. Save your breath for the fight...", null);
				persuasionTask.TryLaterLine = new TextObject("{=!}IF YOU SEE THIS. CALL CAMPAIGN TEAM.", null);
				persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...", null);
				PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.Easy, false, new TextObject("{=K9i5SaDc}Blood money is appropriate for a crime of passion. But you kill this boy in cold blood, you will be a real murderer in the eyes of the law, and will no doubt die.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option);
				PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyHard, true, new TextObject("{=FUL8TcYa}I promised to protect the boy at the cost of my life. If you try to harm him, you will bleed for it.", null), null, true, false, false);
				persuasionTask.AddOptionToTask(option2);
				PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal, false, new TextObject("{=Ytws5O9S}Some day you may wish to save the life of one of your sons through blood money. If you refuse mercy, mercy may be refused you.", null), null, false, false, false);
				persuasionTask.AddOptionToTask(option3);
				return persuasionTask;
			}

			// Token: 0x06000FAB RID: 4011 RVA: 0x00068CF4 File Offset: 0x00066EF4
			private void StartFightWithNotableGang(bool playerBetrayedCulprit)
			{
				this._notableAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0];
				List<Agent> list = new List<Agent> { this._culpritAgent };
				List<Agent> list2 = new List<Agent> { this._notableAgent };
				MBList<Agent> agents = new MBList<Agent>();
				foreach (Agent agent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 30f, agents))
				{
					if ((CharacterObject)agent.Character == this._notableGangsterCharacterObject)
					{
						list2.Add(agent);
					}
				}
				if (playerBetrayedCulprit)
				{
					Agent.Main.SetTeam(Mission.Current.SpectatorTeam, false);
				}
				else
				{
					list.Add(Agent.Main);
					foreach (Agent agent2 in list2)
					{
						agent2.Defensiveness = 2f;
					}
					this._culpritAgent.Health = 350f;
					this._culpritAgent.BaseHealthLimit = 350f;
					this._culpritAgent.HealthLimit = 350f;
				}
				this._notableAgent.Health = 350f;
				this._notableAgent.BaseHealthLimit = 350f;
				this._notableAgent.HealthLimit = 350f;
				Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(list, list2, false, false, delegate(bool isPlayerSideWon)
				{
					if (this._isNotableKnockedDownInMissionFight)
					{
						if (Agent.Main != null && this._notableAgent.Position.DistanceSquared(Agent.Main.Position) < 49f)
						{
							MissionConversationLogic.Current.StartConversation(this._notableAgent, false, false);
							return;
						}
						this.PlayerAndCulpritKnockedDownNotableQuestSuccess();
						return;
					}
					else
					{
						if (Agent.Main != null && this._notableAgent.Position.DistanceSquared(Agent.Main.Position) < 49f)
						{
							MissionConversationLogic.Current.StartConversation(this._notableAgent, false, false);
							return;
						}
						this.CulpritDiedInNotableFightFail();
						return;
					}
				}, float.Epsilon);
			}

			// Token: 0x06000FAC RID: 4012 RVA: 0x00068EAC File Offset: 0x000670AC
			private void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage)
			{
				if (base.IsOngoing && !this._persuationInDoneAndSuccessfull && affectedAgent.Health <= (float)damage && Agent.Main != null)
				{
					if (affectedAgent == this._notableAgent && !this._isNotableKnockedDownInMissionFight)
					{
						affectedAgent.Health = 50f;
						this._isNotableKnockedDownInMissionFight = true;
						Mission.Current.GetMissionBehavior<MissionFightHandler>().EndFight(false);
					}
					if (affectedAgent == this._culpritAgent && !this._isCulpritDiedInMissionFight)
					{
						Blow b = new Blow
						{
							DamageCalculated = true,
							BaseMagnitude = (float)damage,
							InflictedDamage = damage,
							DamagedPercentage = 1f,
							OwnerId = ((affectorAgent != null) ? affectorAgent.Index : (-1))
						};
						affectedAgent.Die(b, Agent.KillInfo.Invalid);
						this._isCulpritDiedInMissionFight = true;
					}
				}
			}

			// Token: 0x06000FAD RID: 4013 RVA: 0x00068F7C File Offset: 0x0006717C
			protected override void SetDialogs()
			{
				this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=JjXETjYb}Thank you.[ib:demure][if:convo_thinking] I have to add, I'm ready to pay you {REWARD_GOLD}{GOLD_ICON} denars for your trouble. He is hiding somewhere nearby. Go talk to him, and tell him that you're here to sort things out.", null), null, null, null, null).Condition(delegate
				{
					MBTextManager.SetTextVariable("REWARD_GOLD", this._rewardGold);
					MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
					return Hero.OneToOneConversationHero == base.QuestGiver;
				})
					.Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
					.CloseDialog();
				this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=ndDpjT8s}Have you been able to talk with my boy yet?[if:convo_innocent_smile]", null), null, null, null, null).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
					.BeginPlayerOptions(null, false)
					.PlayerOption(new TextObject("{=ETiAbgHa}I will talk with them right away", null), null, null, null)
					.NpcLine(new TextObject("{=qmqTLZ9R}Thank you {?PLAYER.GENDER}madam{?}sir{\\?}. You are a savior.", null), null, null, null, null)
					.CloseDialog()
					.PlayerOption(new TextObject("{=18NtjryL}Not yet, but I will soon.", null), null, null, null)
					.NpcLine(new TextObject("{=HeIIW3EH}We are waiting for your good news {?PLAYER.GENDER}milady{?}sir{\\?}.", null), null, null, null, null)
					.CloseDialog()
					.EndPlayerOptions()
					.CloseDialog();
			}

			// Token: 0x06000FAE RID: 4014 RVA: 0x00069078 File Offset: 0x00067278
			private void QuestAcceptedConsequences()
			{
				base.StartQuest();
				base.AddLog(this.PlayerStartsQuestLogText1, false);
				base.AddLog(this.PlayerStartsQuestLogText2, false);
				base.AddTrackedObject(this._targetNotable);
				base.AddTrackedObject(this._culprit);
				Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center");
				Settlement.CurrentSettlement.LocationComplex.ChangeLocation(this.CreateCulpritLocationCharacter(Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral), null, locationWithId);
			}

			// Token: 0x06000FAF RID: 4015 RVA: 0x000690F8 File Offset: 0x000672F8
			private DialogFlow GetCulpritDialogFlow()
			{
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=w0HPC53e}Who are you? What do you want from me?[ib:nervous][if:convo_bared_teeth]", null), null, null, null, null).Condition(() => !this._culpritJoinedPlayerParty && Hero.OneToOneConversationHero == this._culprit)
					.PlayerLine(new TextObject("{=UGTCe2qP}Relax. I've talked with your relative, {QUEST_GIVER.NAME}. I know all about your situation. I'm here to help.", null), null, null, null)
					.Condition(delegate
					{
						StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, null, false);
						return Hero.OneToOneConversationHero == this._culprit;
					})
					.Consequence(delegate
					{
						this._culprit.SetHasMet();
					})
					.NpcLine(new TextObject("{=45llLiYG}How will you help? Will you protect me?[ib:normal][if:convo_astonished]", null), null, null, null, null)
					.PlayerLine(new TextObject("{=4mwSvCgG}Yes I will. Come now, I will take you with me to {TARGET_NOTABLE.NAME} to resolve this issue peacefully.", null), null, null, null)
					.Condition(delegate
					{
						StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, null, false);
						return Hero.OneToOneConversationHero == this._culprit;
					})
					.NpcLine(new TextObject("{=bHRZhYzd}No! I won't go anywhere near them! They'll kill me![ib:closed2][if:convo_stern]", null), null, null, null, null)
					.PlayerLine(new TextObject("{=sakSp6H8}You can't hide in the shadows forever. I pledge on my honor to protect you if things turn ugly.", null), null, null, null)
					.NpcLine(new TextObject("{=4CFOH0kB}I'm still not sure about all this, but I suppose you're right that I don't have much choice. Let's go get this over.[ib:closed][if:convo_pondering]", null), null, null, null, null)
					.Consequence(delegate
					{
						Campaign.Current.ConversationManager.ConversationEndOneShot += this.CulpritJoinedPlayersArmy;
					})
					.CloseDialog();
			}

			// Token: 0x06000FB0 RID: 4016 RVA: 0x000691F4 File Offset: 0x000673F4
			private DialogFlow GetNotableThugDialogFlow()
			{
				TextObject textObject = new TextObject("{=QMaYa25R}If you dare to even breathe a word against {TARGET_NOTABLE.LINK},[ib:aggressive2][if:convo_furious] it will be your last. You got it scum?", null);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject, false);
				TextObject textObject2 = new TextObject("{=vGnY4KBO}I care very little for your threats. My business is with {TARGET_NOTABLE.LINK}.", null);
				StringHelpers.SetCharacterProperties("TARGET_NOTABLE", this._targetNotable.CharacterObject, textObject2, false);
				return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null, null, null).Condition(delegate
				{
					if (this._notableThugs != null)
					{
						return this._notableThugs.Exists((LocationCharacter x) => x.AgentOrigin == Campaign.Current.ConversationManager.ConversationAgents[0].Origin);
					}
					return false;
				})
					.PlayerLine(textObject2, null, null, null)
					.CloseDialog();
			}

			// Token: 0x06000FB1 RID: 4017 RVA: 0x00069280 File Offset: 0x00067480
			private void CulpritJoinedPlayersArmy()
			{
				this._culprit.ChangeState(Hero.CharacterStates.Active);
				AddCompanionAction.Apply(Clan.PlayerClan, this._culprit);
				AddHeroToPartyAction.Apply(this._culprit, MobileParty.MainParty, true);
				base.AddLog(this.CulpritJoinedPlayerPartyLogText, false);
				if (Mission.Current != null)
				{
					DailyBehaviorGroup behaviorGroup = ((Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0]).GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
					FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
					behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
					followAgentBehavior.SetTargetAgent(Agent.Main);
				}
				this._culpritJoinedPlayerParty = true;
			}

			// Token: 0x06000FB2 RID: 4018 RVA: 0x00069318 File Offset: 0x00067518
			protected override void RegisterEvents()
			{
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
				CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, new Action<Village>(this.OnVillageRaid));
				CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, new Action(this.OnBeforeMissionOpened));
				CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
				CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnd));
				CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionStarted));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, new Action<Hero, RemoveCompanionAction.RemoveCompanionDetail>(this.OnCompanionRemoved));
				CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnPrisonerTaken));
				CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
				CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
				CampaignEvents.CanMoveToSettlementEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.CanMoveToSettlement));
				CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.CanHeroDie));
				CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
				CampaignEvents.PerkResetEvent.AddNonSerializedListener(this, new Action<Hero, PerkObject>(this.OnPerksReset));
				CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, new Action<Hero>(this.OnNewCompanionAdded));
				CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
			}

			// Token: 0x06000FB3 RID: 4019 RVA: 0x000694AC File Offset: 0x000676AC
			private void OnGameLoadFinished()
			{
				this.CheckCompanionLimit();
			}

			// Token: 0x06000FB4 RID: 4020 RVA: 0x000694B4 File Offset: 0x000676B4
			private void OnNewCompanionAdded(Hero hero)
			{
				this.CheckCompanionLimit();
			}

			// Token: 0x06000FB5 RID: 4021 RVA: 0x000694BC File Offset: 0x000676BC
			private void OnPerksReset(Hero hero, PerkObject perk)
			{
				if (hero == Hero.MainHero)
				{
					this.CheckCompanionLimit();
				}
			}

			// Token: 0x06000FB6 RID: 4022 RVA: 0x000694CC File Offset: 0x000676CC
			private void CheckCompanionLimit()
			{
				if (Clan.PlayerClan.Companions.Count > Clan.PlayerClan.CompanionLimit)
				{
					base.AddLog(this.CompanionLimitReachedQuestLogText, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06000FB7 RID: 4023 RVA: 0x00069500 File Offset: 0x00067700
			private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
			{
				if (!this._culpritJoinedPlayerParty && Settlement.CurrentSettlement == base.QuestGiver.CurrentSettlement)
				{
					Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center").AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateCulpritLocationCharacter), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
				}
			}

			// Token: 0x06000FB8 RID: 4024 RVA: 0x00069558 File Offset: 0x00067758
			private void CanMoveToSettlement(Hero hero, ref bool result)
			{
				this.CommonRestrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000FB9 RID: 4025 RVA: 0x00069562 File Offset: 0x00067762
			public override void OnHeroCanBeSelectedInInventoryInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonRestrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000FBA RID: 4026 RVA: 0x0006956C File Offset: 0x0006776C
			public override void OnHeroCanHavePartyRoleOrBeGovernorInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonRestrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000FBB RID: 4027 RVA: 0x00069576 File Offset: 0x00067776
			public override void OnHeroCanLeadPartyInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonRestrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000FBC RID: 4028 RVA: 0x00069580 File Offset: 0x00067780
			public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
			{
				this.CommonRestrictionInfoIsRequested(hero, ref result);
			}

			// Token: 0x06000FBD RID: 4029 RVA: 0x0006958A File Offset: 0x0006778A
			private void CommonRestrictionInfoIsRequested(Hero hero, ref bool result)
			{
				if (hero == this._culprit || this._targetNotable == hero)
				{
					result = false;
				}
			}

			// Token: 0x06000FBE RID: 4030 RVA: 0x000695A1 File Offset: 0x000677A1
			private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
			{
				if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
				{
					QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
				}
			}

			// Token: 0x06000FBF RID: 4031 RVA: 0x000695B4 File Offset: 0x000677B4
			private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
			{
				if (hero == this._targetNotable)
				{
					result = false;
					return;
				}
				if (hero == Hero.MainHero && Settlement.CurrentSettlement == this._targetSettlement && Mission.Current != null)
				{
					result = false;
				}
			}

			// Token: 0x06000FC0 RID: 4032 RVA: 0x000695E4 File Offset: 0x000677E4
			private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
			{
				if (victim == this._targetNotable)
				{
					TextObject textObject = ((detail == KillCharacterAction.KillCharacterActionDetail.Lost) ? this.TargetHeroDisappearedLogText : this.TargetHeroDiedLogText);
					StringHelpers.SetCharacterProperties("QUEST_TARGET", this._targetNotable.CharacterObject, textObject, false);
					StringHelpers.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject, textObject, false);
					base.AddLog(textObject, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06000FC1 RID: 4033 RVA: 0x0006964D File Offset: 0x0006784D
			private void OnPrisonerTaken(PartyBase capturer, Hero prisoner)
			{
				if (prisoner == this._culprit)
				{
					base.AddLog(this.FailQuestLogText, false);
					this.TiemoutFailConsequences();
					base.CompleteQuestWithFail(null);
				}
			}

			// Token: 0x06000FC2 RID: 4034 RVA: 0x00069674 File Offset: 0x00067874
			private void OnVillageRaid(Village village)
			{
				if (village == this._targetSettlement.Village)
				{
					base.AddLog(this.TargetVillageRaidedBeforeTalkingToCulpritCancel, false);
					base.CompleteQuestWithCancel(null);
					return;
				}
				if (village == base.QuestGiver.CurrentSettlement.Village && !this._culpritJoinedPlayerParty)
				{
					base.AddLog(this.QuestGiverVillageRaidedBeforeTalkingToCulpritCancel, false);
					base.CompleteQuestWithCancel(null);
				}
			}

			// Token: 0x06000FC3 RID: 4035 RVA: 0x000696D5 File Offset: 0x000678D5
			private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
			{
				if (base.IsOngoing && !this._isCulpritDiedInMissionFight && !this._isPlayerKnockedOutMissionFight && companion == this._culprit)
				{
					base.AddLog(this.CulpritNoLongerAClanMember, false);
					this.TiemoutFailConsequences();
					base.CompleteQuestWithFail(null);
				}
			}

			// Token: 0x06000FC4 RID: 4036 RVA: 0x00069714 File Offset: 0x00067914
			public void OnMissionStarted(IMission iMission)
			{
				if (this._checkForMissionEvents)
				{
					if (PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.All((AccompanyingCharacter x) => x.LocationCharacter.Character != this._culprit.CharacterObject))
					{
						LocationCharacter locationCharacterOfHero = LocationComplex.Current.GetLocationCharacterOfHero(this._culprit);
						if (locationCharacterOfHero != null)
						{
							PlayerEncounter.LocationEncounter.AddAccompanyingCharacter(locationCharacterOfHero, true);
						}
					}
					FamilyFeudIssueBehavior.FamilyFeudIssueMissionBehavior missionBehavior = new FamilyFeudIssueBehavior.FamilyFeudIssueMissionBehavior(new Action<Agent, Agent, int>(this.OnAgentHit));
					Mission.Current.AddMissionBehavior(missionBehavior);
					Mission.Current.GetMissionBehavior<MissionConversationLogic>().SetSpawnArea("alley_2");
				}
			}

			// Token: 0x06000FC5 RID: 4037 RVA: 0x00069798 File Offset: 0x00067998
			private void OnMissionEnd(IMission mission)
			{
				if (this._checkForMissionEvents)
				{
					this._notableAgent = null;
					this._culpritAgent = null;
					if (Agent.Main == null)
					{
						base.AddLog(this.PlayerDiedInNotableBattle, false);
						this.RelationshipChangeWithQuestGiver = -10;
						base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
						base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
						this._isPlayerKnockedOutMissionFight = true;
						base.CompleteQuestWithFail(null);
						return;
					}
					if (this._isCulpritDiedInMissionFight)
					{
						if (this._playerBetrayedCulprit)
						{
							base.AddLog(this.FailQuestLogText, false);
							TraitLevelingHelper.OnIssueSolvedThroughBetrayal(Hero.MainHero, new Tuple<TraitObject, int>[]
							{
								new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
							});
							ChangeRelationAction.ApplyPlayerRelation(this._targetNotable, 5, true, true);
						}
						else
						{
							base.AddLog(this.CulpritDiedQuestFail, false);
						}
						this.RelationshipChangeWithQuestGiver = -10;
						base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
						base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
						base.CompleteQuestWithFail(null);
						return;
					}
					if (this._persuationInDoneAndSuccessfull)
					{
						base.AddLog(this.SuccessQuestSolutionLogText, false);
						this.ApplySuccessConsequences();
						return;
					}
					if (this._isNotableKnockedDownInMissionFight)
					{
						base.AddLog(this.SuccessQuestSolutionLogText, false);
						this.ApplySuccessConsequences();
					}
				}
			}

			// Token: 0x06000FC6 RID: 4038 RVA: 0x0006993D File Offset: 0x00067B3D
			private void OnGameMenuOpened(MenuCallbackArgs args)
			{
				if (this._culpritJoinedPlayerParty && Hero.MainHero.CurrentSettlement == this._targetSettlement)
				{
					this._checkForMissionEvents = args.MenuContext.GameMenu.StringId == "village";
				}
			}

			// Token: 0x06000FC7 RID: 4039 RVA: 0x0006997C File Offset: 0x00067B7C
			public void OnSettlementLeft(MobileParty party, Settlement settlement)
			{
				if (party == MobileParty.MainParty)
				{
					if (settlement == this._targetSettlement)
					{
						this._checkForMissionEvents = false;
					}
					if (settlement == base.QuestGiver.CurrentSettlement && this._culpritJoinedPlayerParty && !base.IsTracked(this._targetSettlement))
					{
						base.AddTrackedObject(this._targetSettlement);
					}
				}
			}

			// Token: 0x06000FC8 RID: 4040 RVA: 0x000699D4 File Offset: 0x00067BD4
			public void OnBeforeMissionOpened()
			{
				if (this._checkForMissionEvents)
				{
					Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center");
					if (locationWithId != null)
					{
						locationWithId.GetLocationCharacter(this._targetNotable).SpecialTargetTag = "alley_2";
						if (this._notableThugs == null)
						{
							this._notableThugs = new List<LocationCharacter>();
						}
						else
						{
							this._notableThugs.Clear();
						}
						locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateNotablesThugs), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, MathF.Ceiling(Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier() * 3f));
					}
				}
			}

			// Token: 0x06000FC9 RID: 4041 RVA: 0x00069A78 File Offset: 0x00067C78
			private LocationCharacter CreateCulpritLocationCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(this._culprit.CharacterObject.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, this._culprit.CharacterObject.IsFemale, "_villager"), monsterWithSuffix);
				return new LocationCharacter(new AgentData(new SimpleAgentOrigin(this._culprit.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior), "alley_2", true, relation, tuple.Item1, true, false, null, false, false, true, null, false);
			}

			// Token: 0x06000FCA RID: 4042 RVA: 0x00069B20 File Offset: 0x00067D20
			private LocationCharacter CreateNotablesThugs(CultureObject culture, LocationCharacter.CharacterRelations relation)
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(this._notableGangsterCharacterObject.Race, "_settlement");
				Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, this._notableGangsterCharacterObject.IsFemale, "_villain"), monsterWithSuffix);
				LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(this._notableGangsterCharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(tuple.Item2), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "alley_2", true, relation, tuple.Item1, true, false, null, false, false, true, null, false);
				this._notableThugs.Add(locationCharacter);
				return locationCharacter;
			}

			// Token: 0x06000FCB RID: 4043 RVA: 0x00069BC4 File Offset: 0x00067DC4
			private void OnMapEventEnded(MapEvent mapEvent)
			{
				if (mapEvent.IsPlayerMapEvent && this._culpritJoinedPlayerParty && !MobileParty.MainParty.MemberRoster.GetTroopRoster().Exists((TroopRosterElement x) => x.Character == this._culprit.CharacterObject))
				{
					base.AddLog(this.FailQuestLogText, false);
					this.TiemoutFailConsequences();
					base.CompleteQuestWithFail(null);
				}
			}

			// Token: 0x06000FCC RID: 4044 RVA: 0x00069C1E File Offset: 0x00067E1E
			private void CulpritDiedInNotableFightFail()
			{
				this._conversationAfterFightIsDone = true;
				this.HandleAgentBehaviorAfterQuestConversations();
			}

			// Token: 0x06000FCD RID: 4045 RVA: 0x00069C30 File Offset: 0x00067E30
			protected override void OnFinalize()
			{
				if (this._culprit.IsPlayerCompanion)
				{
					if (this._culprit.IsPrisoner)
					{
						EndCaptivityAction.ApplyByEscape(this._culprit, null, false);
					}
					RemoveCompanionAction.ApplyAfterQuest(Clan.PlayerClan, this._culprit);
				}
				if (this._culprit.IsAlive)
				{
					this._culprit.Clan = null;
					KillCharacterAction.ApplyByRemove(this._culprit, false, true);
				}
			}

			// Token: 0x06000FCE RID: 4046 RVA: 0x00069C9A File Offset: 0x00067E9A
			protected override void OnTimedOut()
			{
				base.AddLog(this.FailQuestLogText, false);
				this.TiemoutFailConsequences();
			}

			// Token: 0x06000FCF RID: 4047 RVA: 0x00069CB0 File Offset: 0x00067EB0
			private void TiemoutFailConsequences()
			{
				TraitLevelingHelper.OnIssueSolvedThroughBetrayal(base.QuestGiver, new Tuple<TraitObject, int>[]
				{
					new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
				});
				this.RelationshipChangeWithQuestGiver = -10;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Prosperity -= 5f;
				base.QuestGiver.CurrentSettlement.Village.Bound.Town.Security -= 5f;
			}

			// Token: 0x06000FD0 RID: 4048 RVA: 0x00069D3B File Offset: 0x00067F3B
			internal static void AutoGeneratedStaticCollectObjectsFamilyFeudIssueQuest(object o, List<object> collectedObjects)
			{
				((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06000FD1 RID: 4049 RVA: 0x00069D49 File Offset: 0x00067F49
			protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				base.AutoGeneratedInstanceCollectObjects(collectedObjects);
				collectedObjects.Add(this._targetSettlement);
				collectedObjects.Add(this._targetNotable);
				collectedObjects.Add(this._culprit);
			}

			// Token: 0x06000FD2 RID: 4050 RVA: 0x00069D76 File Offset: 0x00067F76
			internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o)._targetSettlement;
			}

			// Token: 0x06000FD3 RID: 4051 RVA: 0x00069D83 File Offset: 0x00067F83
			internal static object AutoGeneratedGetMemberValue_targetNotable(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o)._targetNotable;
			}

			// Token: 0x06000FD4 RID: 4052 RVA: 0x00069D90 File Offset: 0x00067F90
			internal static object AutoGeneratedGetMemberValue_culprit(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o)._culprit;
			}

			// Token: 0x06000FD5 RID: 4053 RVA: 0x00069D9D File Offset: 0x00067F9D
			internal static object AutoGeneratedGetMemberValue_culpritJoinedPlayerParty(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o)._culpritJoinedPlayerParty;
			}

			// Token: 0x06000FD6 RID: 4054 RVA: 0x00069DAF File Offset: 0x00067FAF
			internal static object AutoGeneratedGetMemberValue_checkForMissionEvents(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o)._checkForMissionEvents;
			}

			// Token: 0x06000FD7 RID: 4055 RVA: 0x00069DC1 File Offset: 0x00067FC1
			internal static object AutoGeneratedGetMemberValue_rewardGold(object o)
			{
				return ((FamilyFeudIssueBehavior.FamilyFeudIssueQuest)o)._rewardGold;
			}

			// Token: 0x040007FB RID: 2043
			private const int CustomCulpritAgentHealth = 350;

			// Token: 0x040007FC RID: 2044
			private const int CustomTargetNotableAgentHealth = 350;

			// Token: 0x040007FD RID: 2045
			public const string CommonAreaTag = "alley_2";

			// Token: 0x040007FE RID: 2046
			[SaveableField(10)]
			private readonly Settlement _targetSettlement;

			// Token: 0x040007FF RID: 2047
			[SaveableField(20)]
			private Hero _targetNotable;

			// Token: 0x04000800 RID: 2048
			[SaveableField(30)]
			private Hero _culprit;

			// Token: 0x04000801 RID: 2049
			[SaveableField(40)]
			private bool _culpritJoinedPlayerParty;

			// Token: 0x04000802 RID: 2050
			[SaveableField(50)]
			private bool _checkForMissionEvents;

			// Token: 0x04000803 RID: 2051
			[SaveableField(70)]
			private int _rewardGold;

			// Token: 0x04000804 RID: 2052
			private bool _isCulpritDiedInMissionFight;

			// Token: 0x04000805 RID: 2053
			private bool _isPlayerKnockedOutMissionFight;

			// Token: 0x04000806 RID: 2054
			private bool _isNotableKnockedDownInMissionFight;

			// Token: 0x04000807 RID: 2055
			private bool _conversationAfterFightIsDone;

			// Token: 0x04000808 RID: 2056
			private bool _persuationInDoneAndSuccessfull;

			// Token: 0x04000809 RID: 2057
			private bool _playerBetrayedCulprit;

			// Token: 0x0400080A RID: 2058
			private Agent _notableAgent;

			// Token: 0x0400080B RID: 2059
			private Agent _culpritAgent;

			// Token: 0x0400080C RID: 2060
			private CharacterObject _notableGangsterCharacterObject;

			// Token: 0x0400080D RID: 2061
			private List<LocationCharacter> _notableThugs;

			// Token: 0x0400080E RID: 2062
			private PersuasionTask _task;

			// Token: 0x0400080F RID: 2063
			private const PersuasionDifficulty Difficulty = PersuasionDifficulty.MediumHard;
		}
	}
}

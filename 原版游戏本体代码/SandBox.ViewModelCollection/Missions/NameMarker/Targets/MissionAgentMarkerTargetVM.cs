using System;
using System.Collections.Generic;
using Helpers;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x02000036 RID: 54
	public class MissionAgentMarkerTargetVM : MissionNameMarkerTargetVM<Agent>
	{
		// Token: 0x060003FC RID: 1020 RVA: 0x0001066C File Offset: 0x0000E86C
		public MissionAgentMarkerTargetVM(Agent target)
			: base(target)
		{
			base.NameType = "Normal";
			base.IconType = "character";
			CharacterObject characterObject = target.Character as CharacterObject;
			if (characterObject != null)
			{
				Hero heroObject = characterObject.HeroObject;
				if (heroObject != null && heroObject.IsLord)
				{
					base.IconType = "noble";
					base.NameType = "Noble";
					if (FactionManager.IsAtWarAgainstFaction(characterObject.HeroObject.MapFaction, Hero.MainHero.MapFaction))
					{
						base.NameType = "Enemy";
						base.IsEnemy = true;
					}
					else if (DiplomacyHelper.IsSameFactionAndNotEliminated(characterObject.HeroObject.MapFaction, Hero.MainHero.MapFaction))
					{
						base.NameType = "Friendly";
						base.IsFriendly = true;
					}
				}
				if (characterObject.HeroObject != null && characterObject.HeroObject.IsPrisoner)
				{
					base.IconType = "prisoner";
				}
				if (target.IsHuman && target != Agent.Main)
				{
					this.UpdateQuestStatus();
				}
				CharacterObject characterObject2 = characterObject;
				Settlement currentSettlement = Settlement.CurrentSettlement;
				object obj;
				if (currentSettlement == null)
				{
					obj = null;
				}
				else
				{
					CultureObject culture = currentSettlement.Culture;
					obj = ((culture != null) ? culture.Barber : null);
				}
				if (characterObject2 == obj)
				{
					base.IconType = "barber";
				}
				else
				{
					CharacterObject characterObject3 = characterObject;
					Settlement currentSettlement2 = Settlement.CurrentSettlement;
					object obj2;
					if (currentSettlement2 == null)
					{
						obj2 = null;
					}
					else
					{
						CultureObject culture2 = currentSettlement2.Culture;
						obj2 = ((culture2 != null) ? culture2.Blacksmith : null);
					}
					if (characterObject3 == obj2)
					{
						base.IconType = "blacksmith";
					}
					else
					{
						CharacterObject characterObject4 = characterObject;
						Settlement currentSettlement3 = Settlement.CurrentSettlement;
						object obj3;
						if (currentSettlement3 == null)
						{
							obj3 = null;
						}
						else
						{
							CultureObject culture3 = currentSettlement3.Culture;
							obj3 = ((culture3 != null) ? culture3.TavernGamehost : null);
						}
						if (characterObject4 == obj3)
						{
							base.IconType = "game_host";
						}
						else if (characterObject.StringId == "sp_hermit")
						{
							base.IconType = "hermit";
						}
						else
						{
							BasicCharacterObject character = base.Target.Character;
							Settlement currentSettlement4 = Settlement.CurrentSettlement;
							object obj4;
							if (currentSettlement4 == null)
							{
								obj4 = null;
							}
							else
							{
								CultureObject culture4 = currentSettlement4.Culture;
								obj4 = ((culture4 != null) ? culture4.Shipwright : null);
							}
							if (character == obj4)
							{
								base.IconType = "shipwright";
							}
						}
					}
				}
			}
			this.RefreshValues();
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x0001084D File Offset: 0x0000EA4D
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, base.Target.GetEyeGlobalPosition() + MissionNameMarkerHelper.AgentHeightOffset);
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x0001086B File Offset: 0x0000EA6B
		protected override TextObject GetName()
		{
			return base.Target.NameTextObject;
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x00010878 File Offset: 0x0000EA78
		public void UpdateQuestStatus()
		{
			CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlags.None;
			Agent target = base.Target;
			CharacterObject characterObject = (CharacterObject)((target != null) ? target.Character : null);
			Hero hero = ((characterObject != null) ? characterObject.HeroObject : null);
			if (hero != null)
			{
				List<ValueTuple<CampaignUIHelper.IssueQuestFlags, TextObject, TextObject>> questStateOfHero = CampaignUIHelper.GetQuestStateOfHero(hero);
				for (int i = 0; i < questStateOfHero.Count; i++)
				{
					issueQuestFlags |= questStateOfHero[i].Item1;
				}
			}
			if (base.Target != null)
			{
				CharacterObject characterObject2 = base.Target.Character as CharacterObject;
				Hero hero2;
				if (characterObject2 == null)
				{
					hero2 = null;
				}
				else
				{
					Hero heroObject = characterObject2.HeroObject;
					if (heroObject == null)
					{
						hero2 = null;
					}
					else
					{
						Clan clan = heroObject.Clan;
						hero2 = ((clan != null) ? clan.Leader : null);
					}
				}
				if (hero2 != Hero.MainHero)
				{
					Settlement currentSettlement = Settlement.CurrentSettlement;
					bool flag;
					if (currentSettlement == null)
					{
						flag = false;
					}
					else
					{
						LocationComplex locationComplex = currentSettlement.LocationComplex;
						bool? flag2;
						if (locationComplex == null)
						{
							flag2 = null;
						}
						else
						{
							LocationCharacter locationCharacter = locationComplex.FindCharacter(base.Target);
							flag2 = ((locationCharacter != null) ? new bool?(locationCharacter.IsVisualTracked) : null);
						}
						bool? flag3 = flag2;
						bool flag4 = true;
						flag = (flag3.GetValueOrDefault() == flag4) & (flag3 != null);
					}
					if (flag)
					{
						issueQuestFlags |= CampaignUIHelper.IssueQuestFlags.TrackedIssue;
					}
				}
			}
			DisguiseMissionLogic missionBehavior = Mission.Current.GetMissionBehavior<DisguiseMissionLogic>();
			if (missionBehavior != null && missionBehavior.IsContactAgentTracked(base.Target))
			{
				issueQuestFlags |= CampaignUIHelper.IssueQuestFlags.TrackedIssue;
			}
			CampaignUIHelper.IssueQuestFlags[] issueQuestFlagsValues = CampaignUIHelper.IssueQuestFlagsValues;
			for (int j = 0; j < issueQuestFlagsValues.Length; j++)
			{
				CampaignUIHelper.IssueQuestFlags questFlag = issueQuestFlagsValues[j];
				if (questFlag != CampaignUIHelper.IssueQuestFlags.None && (issueQuestFlags & questFlag) != CampaignUIHelper.IssueQuestFlags.None && base.Quests.AllQ((QuestMarkerVM q) => q.IssueQuestFlag != questFlag))
				{
					base.Quests.Add(new QuestMarkerVM(questFlag, null, null));
					if ((questFlag & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.AvailableIssue) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.TrackedIssue) != CampaignUIHelper.IssueQuestFlags.None)
					{
						base.IsTracked = true;
					}
					else if ((questFlag & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
					{
						base.IsQuestMainStory = true;
					}
				}
			}
			base.Quests.Sort(new MissionAgentMarkerTargetVM.QuestMarkerComparer());
		}

		// Token: 0x0200009F RID: 159
		private class QuestMarkerComparer : IComparer<QuestMarkerVM>
		{
			// Token: 0x060006A0 RID: 1696 RVA: 0x00016C44 File Offset: 0x00014E44
			public int Compare(QuestMarkerVM x, QuestMarkerVM y)
			{
				return x.QuestMarkerType.CompareTo(y.QuestMarkerType);
			}
		}
	}
}

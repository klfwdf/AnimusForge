using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors
{
	// Token: 0x02000464 RID: 1124
	public class DiplomaticBartersBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600478B RID: 18315 RVA: 0x00166033 File Offset: 0x00164233
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
		}

		// Token: 0x0600478C RID: 18316 RVA: 0x0016604C File Offset: 0x0016424C
		private void DailyTickClan(Clan clan)
		{
			bool flag = false;
			using (List<WarPartyComponent>.Enumerator enumerator = clan.WarPartyComponents.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.MobileParty.MapEvent != null)
					{
						flag = true;
						break;
					}
				}
			}
			MBList<Clan> e = Clan.NonBanditFactions.ToMBList<Clan>();
			if (clan == Clan.PlayerClan || clan.CurrentTotalStrength <= 0f || clan.IsEliminated)
			{
				return;
			}
			if (clan.IsBanditFaction || clan.IsRebelClan)
			{
				return;
			}
			if (clan.Kingdom == null && MBRandom.RandomFloat < 0.5f)
			{
				if (MBRandom.RandomFloat < 0.5f)
				{
					Clan randomElement = e.GetRandomElement<Clan>();
					if (randomElement.Kingdom == null && randomElement != Clan.PlayerClan && clan.IsAtWarWith(randomElement) && !clan.IsMinorFaction && !randomElement.IsMinorFaction)
					{
						this.ConsiderPeace(clan, randomElement);
						return;
					}
				}
				else
				{
					bool flag2 = true;
					if (clan.Settlements.Count > 0 && MBRandom.RandomFloat < 0.5f)
					{
						flag2 = false;
					}
					if (flag2)
					{
						Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => x.IsAtWarWith(clan) && !x.IsAtConstantWarWith(clan));
						if (randomElementWithPredicate != null && randomElementWithPredicate != Clan.PlayerClan.Kingdom)
						{
							int relation = clan.Leader.GetRelation(randomElementWithPredicate.Leader);
							if (relation > -65 && MBMath.Map((float)relation, -100f, 100f, 0f, 1f) < MBRandom.RandomFloat)
							{
								MakePeaceAction.Apply(clan, randomElementWithPredicate);
								return;
							}
						}
					}
				}
			}
			else if (MBRandom.RandomFloat < 0.2f && !clan.IsUnderMercenaryService && clan.Kingdom != null && !clan.IsClanTypeMercenary)
			{
				if (MBRandom.RandomFloat < 0.1f)
				{
					Clan randomElement2 = e.GetRandomElement<Clan>();
					int num = 0;
					while (randomElement2.Kingdom == null || clan.Kingdom == randomElement2.Kingdom || randomElement2.IsEliminated)
					{
						randomElement2 = e.GetRandomElement<Clan>();
						num++;
						if (num >= 20)
						{
							break;
						}
					}
					if (randomElement2.Kingdom != null && clan.Kingdom != randomElement2.Kingdom && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, randomElement2.Kingdom) && !flag && randomElement2.MapFaction.IsKingdomFaction && !randomElement2.IsEliminated && randomElement2 != Clan.PlayerClan && randomElement2.MapFaction.Leader != Hero.MainHero)
					{
						if (clan.WarPartyComponents.All((WarPartyComponent x) => x.MobileParty.MapEvent == null))
						{
							this.ConsiderDefection(clan, randomElement2.MapFaction as Kingdom);
							return;
						}
					}
				}
			}
			else if (MBRandom.RandomFloat < ((clan.MapFaction.Leader == Hero.MainHero) ? 0.2f : 0.4f))
			{
				Kingdom kingdom = Kingdom.All[MBRandom.RandomInt(Kingdom.All.Count)];
				int num2 = 0;
				using (List<Kingdom>.Enumerator enumerator2 = Kingdom.All.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.Culture == clan.Culture)
						{
							num2 += 10;
						}
						else
						{
							num2++;
						}
					}
				}
				int num3 = (int)(MBRandom.RandomFloat * (float)num2);
				foreach (Kingdom kingdom2 in Kingdom.All)
				{
					if (kingdom2.Culture == clan.Culture)
					{
						num3 -= 10;
					}
					else
					{
						num3--;
					}
					if (num3 < 0)
					{
						kingdom = kingdom2;
						break;
					}
				}
				if (kingdom.Leader != Hero.MainHero && !kingdom.IsEliminated && (clan.Kingdom == null || clan.IsUnderMercenaryService) && clan.MapFaction != kingdom && !clan.MapFaction.IsAtWarWith(kingdom) && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, kingdom))
				{
					if (clan.WarPartyComponents.All((WarPartyComponent x) => x.MobileParty.MapEvent == null) && clan.ShouldStayInKingdomUntil.IsPast)
					{
						bool flag3 = true;
						if (!clan.IsMinorFaction)
						{
							foreach (Kingdom kingdom3 in Kingdom.All)
							{
								if (kingdom3 != kingdom && clan.IsAtWarWith(kingdom3) && !kingdom3.IsAtWarWith(kingdom) && kingdom.CurrentTotalStrength <= 10f * kingdom3.CurrentTotalStrength)
								{
									flag3 = false;
									break;
								}
							}
						}
						if (flag3)
						{
							if (clan.IsMinorFaction)
							{
								this.ConsiderClanJoinAsMercenary(clan, kingdom);
								return;
							}
							this.ConsiderClanJoin(clan, kingdom);
							return;
						}
					}
				}
			}
			else if (MBRandom.RandomFloat < 0.4f)
			{
				if (clan.Kingdom != null && !flag && clan.Kingdom.RulingClan != clan && clan != Clan.PlayerClan && clan.ShouldStayInKingdomUntil.IsPast)
				{
					if (clan.WarPartyComponents.All((WarPartyComponent x) => x.MobileParty.MapEvent == null))
					{
						if (clan.IsMinorFaction)
						{
							this.ConsiderClanLeaveAsMercenary(clan);
							return;
						}
						this.ConsiderClanLeaveKingdom(clan);
						return;
					}
				}
			}
			else if (MBRandom.RandomFloat < 0.7f)
			{
				Clan randomElement3 = e.GetRandomElement<Clan>();
				IFaction mapFaction = randomElement3.MapFaction;
				if (!clan.IsMinorFaction && (!mapFaction.IsMinorFaction || mapFaction == Clan.PlayerClan) && clan.Kingdom == null && randomElement3 != clan && !mapFaction.IsEliminated && mapFaction.WarPartyComponents.Count > 0 && clan.WarPartyComponents.Count > 0 && !clan.IsAtWarWith(mapFaction) && clan != Clan.PlayerClan)
				{
					this.ConsiderWar(clan, mapFaction);
				}
			}
		}

		// Token: 0x0600478D RID: 18317 RVA: 0x001667D0 File Offset: 0x001649D0
		private void ConsiderClanLeaveKingdom(Clan clan)
		{
			LeaveKingdomAsClanBarterable leaveKingdomAsClanBarterable = new LeaveKingdomAsClanBarterable(clan.Leader, null);
			if (leaveKingdomAsClanBarterable.GetValueForFaction(clan) > 0)
			{
				leaveKingdomAsClanBarterable.Apply();
			}
		}

		// Token: 0x0600478E RID: 18318 RVA: 0x001667FC File Offset: 0x001649FC
		private void ConsiderClanLeaveAsMercenary(Clan clan)
		{
			LeaveKingdomAsClanBarterable leaveKingdomAsClanBarterable = new LeaveKingdomAsClanBarterable(clan.Leader, null);
			if (leaveKingdomAsClanBarterable.GetValueForFaction(clan) > 500)
			{
				leaveKingdomAsClanBarterable.Apply();
			}
		}

		// Token: 0x0600478F RID: 18319 RVA: 0x0016682C File Offset: 0x00164A2C
		private void ConsiderClanJoin(Clan clan, Kingdom kingdom)
		{
			JoinKingdomAsClanBarterable joinKingdomAsClanBarterable = new JoinKingdomAsClanBarterable(clan.Leader, kingdom, false);
			if (joinKingdomAsClanBarterable.GetValueForFaction(clan) + joinKingdomAsClanBarterable.GetValueForFaction(kingdom) > 0)
			{
				Campaign.Current.BarterManager.ExecuteAiBarter(clan, kingdom, clan.Leader, kingdom.Leader, joinKingdomAsClanBarterable);
			}
		}

		// Token: 0x06004790 RID: 18320 RVA: 0x00166878 File Offset: 0x00164A78
		private void ConsiderClanJoinAsMercenary(Clan clan, Kingdom kingdom)
		{
			MercenaryJoinKingdomBarterable mercenaryJoinKingdomBarterable = new MercenaryJoinKingdomBarterable(clan.Leader, null, kingdom);
			if (mercenaryJoinKingdomBarterable.GetValueForFaction(clan) + mercenaryJoinKingdomBarterable.GetValueForFaction(kingdom) > 0)
			{
				Campaign.Current.BarterManager.ExecuteAiBarter(clan, kingdom, clan.Leader, kingdom.Leader, mercenaryJoinKingdomBarterable);
			}
		}

		// Token: 0x06004791 RID: 18321 RVA: 0x001668C4 File Offset: 0x00164AC4
		private void ConsiderDefection(Clan clan1, Kingdom kingdom)
		{
			JoinKingdomAsClanBarterable joinKingdomAsClanBarterable = new JoinKingdomAsClanBarterable(clan1.Leader, kingdom, true);
			int valueForFaction = joinKingdomAsClanBarterable.GetValueForFaction(clan1);
			int valueForFaction2 = joinKingdomAsClanBarterable.GetValueForFaction(kingdom);
			int num = valueForFaction + valueForFaction2;
			int num2 = 0;
			if (valueForFaction < 0)
			{
				num2 = -valueForFaction;
			}
			if (num > 0 && (float)num2 <= (float)kingdom.Leader.Gold * 0.5f)
			{
				Campaign.Current.BarterManager.ExecuteAiBarter(clan1, kingdom, clan1.Leader, kingdom.Leader, joinKingdomAsClanBarterable);
			}
		}

		// Token: 0x06004792 RID: 18322 RVA: 0x00166934 File Offset: 0x00164B34
		private void ConsiderPeace(Clan clan1, Clan clan2)
		{
			PeaceBarterable peaceBarterable = new PeaceBarterable(clan1.Leader, clan1.MapFaction, clan2.MapFaction, CampaignTime.Years(1f));
			if (peaceBarterable.GetValueForFaction(clan1) + peaceBarterable.GetValueForFaction(clan2) > 0)
			{
				Campaign.Current.BarterManager.ExecuteAiBarter(clan1, clan2, clan1.Leader, clan2.Leader, peaceBarterable);
			}
		}

		// Token: 0x06004793 RID: 18323 RVA: 0x00166994 File Offset: 0x00164B94
		private void ConsiderWar(Clan clan, IFaction otherMapFaction)
		{
			DeclareWarBarterable declareWarBarterable = new DeclareWarBarterable(clan, otherMapFaction);
			if (declareWarBarterable.GetValueForFaction(clan) > 1000)
			{
				declareWarBarterable.Apply();
			}
		}

		// Token: 0x06004794 RID: 18324 RVA: 0x001669BD File Offset: 0x00164BBD
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x040013C2 RID: 5058
		private const int MinimumDaysOfTributeNeededForPeace = 5;

		// Token: 0x040013C3 RID: 5059
		private const float IndependentClanLikelihoodThresholdToMakePeace = 0.5f;

		// Token: 0x040013C4 RID: 5060
		private const float IndependentClanPeaceConsiderChance = 0.5f;

		// Token: 0x040013C5 RID: 5061
		private const float IndependentClanConsiderPeaceWithAnotherClanChance = 0.5f;

		// Token: 0x040013C6 RID: 5062
		private const float ClanLeaveKingdomChance = 0.4f;

		// Token: 0x040013C7 RID: 5063
		private const float ClanConsideringWarDeclarationChance = 0.7f;

		// Token: 0x040013C8 RID: 5064
		private const int IndependentClanLeaderMinimumRelationForDeclaringPeaceWithKingdom = -65;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000B5 RID: 181
	[MenuOverlay("EncounterMenuOverlay")]
	public class EncounterMenuOverlayVM : GameMenuOverlay
	{
		// Token: 0x060011C0 RID: 4544 RVA: 0x00046BB0 File Offset: 0x00044DB0
		public EncounterMenuOverlayVM()
		{
			this.AttackerPartyList = new MBBindingList<GameMenuPartyItemVM>();
			this.DefenderPartyList = new MBBindingList<GameMenuPartyItemVM>();
			base.CurrentOverlayType = 1;
			this.AttackerMoraleHint = new BasicTooltipViewModel(() => this.GetEncounterSideMoraleTooltip(BattleSideEnum.Attacker));
			this.DefenderMoraleHint = new BasicTooltipViewModel(() => this.GetEncounterSideMoraleTooltip(BattleSideEnum.Defender));
			this.AttackerFoodHint = new BasicTooltipViewModel(() => this.GetEncounterSideFoodTooltip(BattleSideEnum.Attacker));
			this.DefenderFoodHint = new BasicTooltipViewModel(() => this.GetEncounterSideFoodTooltip(BattleSideEnum.Defender));
			this.DefenderWallHint = new BasicTooltipViewModel();
			base.IsInitializationOver = false;
			this.UpdateLists();
			this.UpdateProperties();
			base.IsInitializationOver = true;
			this.RefreshValues();
		}

		// Token: 0x060011C1 RID: 4545 RVA: 0x00046C68 File Offset: 0x00044E68
		private void SetAttackerAndDefenderParties(out bool attackerChanged, out bool defenderChanged)
		{
			attackerChanged = false;
			defenderChanged = false;
			if (MobileParty.MainParty.MapEvent != null)
			{
				PartyBase leaderParty = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker);
				if (leaderParty.IsSettlement)
				{
					if (this._attackerLeadingParty == null || this._attackerLeadingParty.Party != leaderParty)
					{
						attackerChanged = true;
						this._attackerLeadingParty = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), leaderParty.Settlement);
					}
				}
				else if (this._attackerLeadingParty == null || this._attackerLeadingParty.Party != leaderParty)
				{
					attackerChanged = true;
					this._attackerLeadingParty = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), leaderParty, false);
				}
				PartyBase leaderParty2 = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender);
				if (leaderParty2.IsSettlement)
				{
					if (this._defenderLeadingParty == null || this._defenderLeadingParty.Party != leaderParty2)
					{
						defenderChanged = true;
						this._defenderLeadingParty = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), leaderParty2.Settlement);
						return;
					}
				}
				else if (this._defenderLeadingParty == null || this._defenderLeadingParty.Party != leaderParty2)
				{
					defenderChanged = true;
					this._defenderLeadingParty = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), leaderParty2, false);
					return;
				}
			}
			else
			{
				Settlement settlement = Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
				SiegeEvent siegeEvent = settlement.SiegeEvent;
				if (siegeEvent != null)
				{
					if (this._defenderLeadingParty == null || this._defenderLeadingParty.Settlement != settlement)
					{
						defenderChanged = true;
						this._defenderLeadingParty = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), settlement);
					}
					if (this._attackerLeadingParty == null || this._attackerLeadingParty.Party != siegeEvent.BesiegerCamp.LeaderParty.Party)
					{
						attackerChanged = true;
						this._attackerLeadingParty = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), siegeEvent.BesiegerCamp.LeaderParty.Party, false);
					}
					this.DefenderWallHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetSiegeWallTooltip(settlement.Town.GetWallLevel(), MathF.Ceiling(settlement.SettlementTotalWallHitPoints)));
					return;
				}
				Debug.FailedAssert("Encounter overlay is open but MapEvent AND SiegeEvent is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\EncounterMenuOverlayVM.cs", "SetAttackerAndDefenderParties", 114);
			}
		}

		// Token: 0x060011C2 RID: 4546 RVA: 0x00046E88 File Offset: 0x00045088
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.AttackerBannerHint = new HintViewModel(GameTexts.FindText("str_attacker_banner", null), null);
			this.DefenderBannerHint = new HintViewModel(GameTexts.FindText("str_defender_banner", null), null);
			this.AttackerTroopNumHint = new HintViewModel(GameTexts.FindText("str_number_of_healthy_attacker_soldiers", null), null);
			this.DefenderTroopNumHint = new HintViewModel(GameTexts.FindText("str_number_of_healthy_defender_soldiers", null), null);
			this.AttackerShipNumHint = new HintViewModel(new TextObject("{=sZu7CCsh}Number of Attacker Ships", null), null);
			this.DefenderShipNumHint = new HintViewModel(new TextObject("{=RjBSR9iO}Number of Defender Ships", null), null);
			base.ContextList.Add(new StringItemWithEnabledAndHintVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", GameMenuOverlay.MenuOverlayContextList.Encyclopedia.ToString()).ToString(), true, GameMenuOverlay.MenuOverlayContextList.Encyclopedia, null));
			this.AttackerPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM x)
			{
				x.RefreshValues();
			});
			this.DefenderPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060011C3 RID: 4547 RVA: 0x00046FBC File Offset: 0x000451BC
		public override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (MobileParty.MainParty.MapEvent != null && this.AttackerPartyList.Count + this.DefenderPartyList.Count != MobileParty.MainParty.MapEvent.InvolvedParties.Count<PartyBase>())
			{
				this.UpdateLists();
			}
			this.AttackerPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM ap)
			{
				ap.RefreshCounts();
			});
			this.DefenderPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM dp)
			{
				dp.RefreshCounts();
			});
		}

		// Token: 0x060011C4 RID: 4548 RVA: 0x00047063 File Offset: 0x00045263
		public override void Refresh()
		{
			base.IsInitializationOver = false;
			this.UpdateLists();
			this.UpdateProperties();
			base.IsInitializationOver = true;
		}

		// Token: 0x060011C5 RID: 4549 RVA: 0x00047080 File Offset: 0x00045280
		private void UpdateProperties()
		{
			if (this.IsSiege)
			{
				GameMenuPartyItemVM defenderLeadingParty = this._defenderLeadingParty;
				bool flag = ((defenderLeadingParty != null) ? defenderLeadingParty.Settlement : null) != null;
				float num = 0f;
				float num2 = 0f;
				if (flag)
				{
					ValueTuple<int, int> townFoodAndMarketStocks = TownHelpers.GetTownFoodAndMarketStocks((flag ? this._defenderLeadingParty : this._attackerLeadingParty).Settlement.Town);
					num2 = (float)(townFoodAndMarketStocks.Item1 + townFoodAndMarketStocks.Item2) / -this._defenderLeadingParty.Settlement.Town.FoodChangeWithoutMarketStocks;
				}
				foreach (GameMenuPartyItemVM gameMenuPartyItemVM in this.DefenderPartyList)
				{
					num += gameMenuPartyItemVM.Party.MobileParty.Morale;
					if (!flag)
					{
						num2 += gameMenuPartyItemVM.Party.MobileParty.Food / -gameMenuPartyItemVM.Party.MobileParty.FoodChange;
					}
				}
				num /= (float)this.DefenderPartyList.Count;
				if (!flag)
				{
					num2 /= (float)this.DefenderPartyList.Count;
				}
				num2 = (float)Math.Max((int)Math.Ceiling((double)num2), 0);
				MBTextManager.SetTextVariable("DAY_NUM", num2.ToString(), false);
				MBTextManager.SetTextVariable("PLURAL", (num2 > 1f) ? 1 : 0);
				this.DefenderPartyFood = GameTexts.FindText("str_party_food_left", null).ToString();
				this.DefenderPartyMorale = num.ToString("0.0");
				num = 0f;
				num2 = 0f;
				if (!flag)
				{
					if (this._attackerLeadingParty.Settlement != null)
					{
						num2 = this._attackerLeadingParty.Settlement.Town.FoodStocks / this._attackerLeadingParty.Settlement.Town.FoodChangeWithoutMarketStocks;
					}
					else if (this._attackerLeadingParty.Party.MobileParty.CurrentSettlement != null)
					{
						num2 = this._attackerLeadingParty.Party.MobileParty.CurrentSettlement.Town.FoodStocks / this._attackerLeadingParty.Party.MobileParty.CurrentSettlement.Town.FoodChangeWithoutMarketStocks;
					}
					else
					{
						Settlement currentSettlement = Settlement.CurrentSettlement;
						if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) != null)
						{
							num2 = Settlement.CurrentSettlement.Town.FoodStocks / Settlement.CurrentSettlement.Town.FoodChangeWithoutMarketStocks;
						}
						else
						{
							Debug.FailedAssert("There are no settlements involved in the siege", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\EncounterMenuOverlayVM.cs", "UpdateProperties", 213);
						}
					}
				}
				else
				{
					Settlement currentSettlement2 = Settlement.CurrentSettlement;
					if (((currentSettlement2 != null) ? currentSettlement2.SiegeEvent : null) != null)
					{
						num2 = Settlement.CurrentSettlement.Town.FoodStocks / Settlement.CurrentSettlement.Town.FoodChangeWithoutMarketStocks;
					}
				}
				foreach (GameMenuPartyItemVM gameMenuPartyItemVM2 in this.AttackerPartyList)
				{
					num += gameMenuPartyItemVM2.Party.MobileParty.Morale;
					if (flag)
					{
						num2 += gameMenuPartyItemVM2.Party.MobileParty.Food / -gameMenuPartyItemVM2.Party.MobileParty.FoodChange;
					}
				}
				num /= (float)this.AttackerPartyList.Count;
				if (flag)
				{
					num2 /= (float)this.AttackerPartyList.Count;
				}
				num2 = (float)Math.Max((int)Math.Ceiling((double)num2), 0);
				MBTextManager.SetTextVariable("DAY_NUM", num2.ToString(), false);
				MBTextManager.SetTextVariable("PLURAL", (num2 > 1f) ? 1 : 0);
				this.AttackerPartyFood = GameTexts.FindText("str_party_food_left", null).ToString();
				this.AttackerPartyMorale = num.ToString("0.0");
				Settlement settlement;
				if ((settlement = Settlement.CurrentSettlement) == null)
				{
					SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
					settlement = ((playerSiegeEvent != null) ? playerSiegeEvent.BesiegedSettlement : null);
				}
				Settlement settlement2 = settlement;
				if (settlement2 != null)
				{
					this.DefenderWallHitPoints = MathF.Ceiling(settlement2.SettlementTotalWallHitPoints).ToString();
				}
			}
		}

		// Token: 0x060011C6 RID: 4550 RVA: 0x00047468 File Offset: 0x00045668
		private void UpdateLists()
		{
			if (MobileParty.MainParty.MapEvent == null)
			{
				Settlement settlement;
				if ((settlement = Settlement.CurrentSettlement) == null)
				{
					SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
					settlement = ((playerSiegeEvent != null) ? playerSiegeEvent.BesiegedSettlement : null);
				}
				if (settlement == null)
				{
					return;
				}
			}
			bool flag;
			bool flag2;
			this.SetAttackerAndDefenderParties(out flag, out flag2);
			if (this._defenderLeadingParty != null && flag2)
			{
				int num = this.DefenderPartyList.FindIndex((GameMenuPartyItemVM x) => x.Party == this._defenderLeadingParty.Party);
				if (num != -1)
				{
					this.DefenderPartyList.RemoveAt(num);
				}
				this.DefenderPartyList.Insert(0, this._defenderLeadingParty);
			}
			if (this._attackerLeadingParty != null && flag)
			{
				int num2 = this.AttackerPartyList.FindIndex((GameMenuPartyItemVM x) => x.Party == this._attackerLeadingParty.Party);
				if (num2 != -1)
				{
					this.AttackerPartyList.RemoveAt(num2);
				}
				this.AttackerPartyList.Insert(0, this._attackerLeadingParty);
			}
			List<PartyBase> list = new List<PartyBase>();
			List<PartyBase> list2 = new List<PartyBase>();
			List<int> list3 = new List<int>();
			List<int> list4 = new List<int>();
			List<PartyBase> list5 = new List<PartyBase>();
			if (MobileParty.MainParty.MapEvent != null)
			{
				list5.AddRange(MobileParty.MainParty.MapEvent.InvolvedParties);
				this.IsSiege = false;
				this.IsNaval = MobileParty.MainParty.MapEvent.IsNavalMapEvent;
			}
			else
			{
				Settlement settlement2 = Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
				if (settlement2.SiegeEvent == null)
				{
					this.PowerComparer = new PowerLevelComparer(1.0, 1.0);
					return;
				}
				SiegeEvent siegeEvent = settlement2.SiegeEvent;
				list5.AddRange(siegeEvent.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege));
				this.IsSiege = true;
				this.IsNaval = false;
			}
			foreach (PartyBase partyBase in list5)
			{
				bool flag3;
				if (MobileParty.MainParty.MapEvent != null)
				{
					flag3 = partyBase.Side == BattleSideEnum.Defender;
				}
				else
				{
					flag3 = (Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement).SiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).HasInvolvedPartyForEventType(partyBase, MapEvent.BattleTypes.Siege);
				}
				List<PartyBase> list6 = (flag3 ? list2 : list);
				List<int> list7 = (flag3 ? list4 : list3);
				if (partyBase.IsActive && partyBase.MemberRoster.Count > 0)
				{
					int numberOfHealthyMembers = partyBase.NumberOfHealthyMembers;
					int num3 = 0;
					while (num3 < list7.Count && numberOfHealthyMembers <= list7[num3])
					{
						num3++;
					}
					list7.Add(partyBase.NumberOfHealthyMembers);
					list6.Insert(num3, partyBase);
				}
			}
			float num4 = list2.Sum((PartyBase party) => party.CalculateCurrentStrength());
			float num5 = list.Sum((PartyBase party) => party.CalculateCurrentStrength());
			if (list5.AnyQ(delegate(PartyBase p)
			{
				MobileParty mobileParty = p.MobileParty;
				return mobileParty != null && mobileParty.IsInfoHidden;
			}))
			{
				num4 = 1f;
				num5 = 0f;
			}
			if (this.PowerComparer == null)
			{
				this.PowerComparer = new PowerLevelComparer((double)num4, (double)num5);
			}
			else
			{
				this.PowerComparer.Update((double)num4, (double)num5, (double)num4, (double)num5);
			}
			List<PartyBase> list8 = (from p in list
				orderby p.NumberOfAllMembers descending
				select p).ToList<PartyBase>();
			List<PartyBase> list9 = (from enemy in this.AttackerPartyList
				select enemy.Party).ToList<PartyBase>();
			List<PartyBase> list10 = list8.Except(list9).ToList<PartyBase>();
			list10.Remove(this._attackerLeadingParty.Party);
			foreach (PartyBase partyBase2 in list9.Except(list8).ToList<PartyBase>())
			{
				for (int i = this.AttackerPartyList.Count - 1; i >= 0; i--)
				{
					if (this.AttackerPartyList[i].Party == partyBase2)
					{
						this.AttackerPartyList.RemoveAt(i);
					}
				}
			}
			if (this.IsSiege)
			{
				list10 = (from x in list10
					where x.MemberRoster.TotalHealthyCount > 0
					select x).ToList<PartyBase>();
			}
			foreach (PartyBase item in list10)
			{
				this.AttackerPartyList.Add(new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), item, false));
			}
			List<PartyBase> list11 = (from p in list2
				orderby p.NumberOfAllMembers descending
				select p).ToList<PartyBase>();
			List<PartyBase> list12 = (from ally in this.DefenderPartyList
				select ally.Party).ToList<PartyBase>();
			List<PartyBase> list13 = list11.Except(list12).ToList<PartyBase>();
			list13.Remove(this._defenderLeadingParty.Party);
			foreach (PartyBase partyBase3 in list12.Except(list11).ToList<PartyBase>())
			{
				for (int j = this.DefenderPartyList.Count - 1; j >= 0; j--)
				{
					if (this.DefenderPartyList[j].Party == partyBase3)
					{
						this.DefenderPartyList.RemoveAt(j);
					}
				}
			}
			if (this.IsSiege)
			{
				list13 = (from x in list13
					where x.MemberRoster.TotalHealthyCount > 0
					select x).ToList<PartyBase>();
			}
			foreach (PartyBase item2 in list13)
			{
				this.DefenderPartyList.Add(new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), item2, false));
			}
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM in this.DefenderPartyList)
			{
				gameMenuPartyItemVM.RefreshProperties();
			}
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM2 in this.AttackerPartyList)
			{
				gameMenuPartyItemVM2.RefreshProperties();
			}
			this.DefenderPartyCount = this.DefenderPartyList.Sum(delegate(GameMenuPartyItemVM p)
			{
				int? num8;
				if (p == null)
				{
					num8 = null;
				}
				else
				{
					PartyBase party = p.Party;
					num8 = ((party != null) ? new int?(party.NumberOfHealthyMembers) : null);
				}
				int? num9 = num8;
				if (num9 == null)
				{
					return 0;
				}
				return num9.GetValueOrDefault();
			});
			this.DefenderPartyCountLbl = (this.DefenderPartyList.AnyQ(delegate(GameMenuPartyItemVM p)
			{
				MobileParty mobileParty = p.Party.MobileParty;
				return mobileParty != null && mobileParty.IsInfoHidden;
			}) ? "?" : this.DefenderPartyCount.ToString());
			this.DefenderShipCount = this.DefenderPartyList.Sum(delegate(GameMenuPartyItemVM p)
			{
				int? num8;
				if (p == null)
				{
					num8 = null;
				}
				else
				{
					PartyBase party = p.Party;
					num8 = ((party != null) ? new int?(party.Ships.Count) : null);
				}
				int? num9 = num8;
				if (num9 == null)
				{
					return 0;
				}
				return num9.GetValueOrDefault();
			});
			this.AttackerPartyCount = this.AttackerPartyList.Sum(delegate(GameMenuPartyItemVM p)
			{
				int? num8;
				if (p == null)
				{
					num8 = null;
				}
				else
				{
					PartyBase party = p.Party;
					num8 = ((party != null) ? new int?(party.NumberOfHealthyMembers) : null);
				}
				int? num9 = num8;
				if (num9 == null)
				{
					return 0;
				}
				return num9.GetValueOrDefault();
			});
			this.AttackerPartyCountLbl = (this.AttackerPartyList.AnyQ(delegate(GameMenuPartyItemVM p)
			{
				MobileParty mobileParty = p.Party.MobileParty;
				return mobileParty != null && mobileParty.IsInfoHidden;
			}) ? "?" : this.AttackerPartyCount.ToString());
			this.AttackerShipCount = this.AttackerPartyList.Sum(delegate(GameMenuPartyItemVM p)
			{
				int? num8;
				if (p == null)
				{
					num8 = null;
				}
				else
				{
					PartyBase party = p.Party;
					num8 = ((party != null) ? new int?(party.Ships.Count) : null);
				}
				int? num9 = num8;
				if (num9 == null)
				{
					return 0;
				}
				return num9.GetValueOrDefault();
			});
			if (MobileParty.MainParty.MapEvent != null)
			{
				PartyBase leaderParty = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker);
				PartyBase leaderParty2 = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender);
				if (this._attackerLeadingParty.Party == leaderParty2 || this._defenderLeadingParty.Party == leaderParty)
				{
					GameMenuPartyItemVM attackerLeadingParty = this._attackerLeadingParty;
					this._attackerLeadingParty = this._defenderLeadingParty;
					this._defenderLeadingParty = attackerLeadingParty;
				}
			}
			this.TitleText = (this.IsSiege ? GameTexts.FindText("str_siege", null).ToString() : (this.TitleText = GameTexts.FindText("str_battle", null).ToString()));
			IFaction faction = ((this._defenderLeadingParty.Party == null) ? this._defenderLeadingParty.Settlement.MapFaction : this._defenderLeadingParty.Party.MapFaction);
			IFaction faction2 = ((this._attackerLeadingParty.Party == null) ? this._attackerLeadingParty.Settlement.MapFaction : this._attackerLeadingParty.Party.MapFaction);
			Banner banner = ((this._defenderLeadingParty.Party == null) ? this._defenderLeadingParty.Settlement.OwnerClan.Banner : this._defenderLeadingParty.Party.Banner);
			Banner banner2 = ((this._attackerLeadingParty.Party == null) ? this._attackerLeadingParty.Settlement.OwnerClan.Banner : this._attackerLeadingParty.Party.Banner);
			this.DefenderPartyBanner = new BannerImageIdentifierVM(banner, true);
			this.AttackerPartyBanner = new BannerImageIdentifierVM(banner2, true);
			string defenderColor;
			if (faction != null && faction is Kingdom)
			{
				defenderColor = Color.FromUint(((Kingdom)faction).PrimaryBannerColor).ToString();
			}
			else
			{
				uint? num6;
				if (faction == null)
				{
					num6 = null;
				}
				else
				{
					Banner banner3 = faction.Banner;
					num6 = ((banner3 != null) ? new uint?(banner3.GetPrimaryColor()) : null);
				}
				defenderColor = Color.FromUint(num6 ?? Color.White.ToUnsignedInteger()).ToString();
			}
			string attackerColor;
			if (faction2 != null && faction2 is Kingdom)
			{
				attackerColor = Color.FromUint(((Kingdom)faction2).PrimaryBannerColor).ToString();
			}
			else
			{
				uint? num7;
				if (faction2 == null)
				{
					num7 = null;
				}
				else
				{
					Banner banner4 = faction2.Banner;
					num7 = ((banner4 != null) ? new uint?(banner4.GetPrimaryColor()) : null);
				}
				attackerColor = Color.FromUint(num7 ?? Color.White.ToUnsignedInteger()).ToString();
			}
			this.PowerComparer.SetColors(defenderColor, attackerColor);
		}

		// Token: 0x060011C7 RID: 4551 RVA: 0x00047F74 File Offset: 0x00046174
		private List<TooltipProperty> GetEncounterSideFoodTooltip(BattleSideEnum side)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			GameMenuPartyItemVM defenderLeadingParty = this._defenderLeadingParty;
			bool flag = ((defenderLeadingParty != null) ? defenderLeadingParty.Settlement : null) != null;
			bool flag2 = (flag && flag && side == BattleSideEnum.Defender) || (!flag && side == BattleSideEnum.Attacker);
			if (this.IsSiege && flag2)
			{
				list.Add(new TooltipProperty(new TextObject("{=OSsSBHKe}Settlement's Food", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
				GameMenuPartyItemVM gameMenuPartyItemVM = (flag ? this._defenderLeadingParty : this._attackerLeadingParty);
				Town town;
				if (gameMenuPartyItemVM == null)
				{
					town = null;
				}
				else
				{
					Settlement settlement = gameMenuPartyItemVM.Settlement;
					town = ((settlement != null) ? settlement.Town : null);
				}
				Town town2 = town;
				float foodChange = ((town2 != null) ? town2.FoodChangeWithoutMarketStocks : 0f);
				ValueTuple<int, int> townFoodAndMarketStocks = TownHelpers.GetTownFoodAndMarketStocks(town2);
				list.Add(new TooltipProperty(new TextObject("{=EkFDvG7z}Settlement Food Stocks", null).ToString(), townFoodAndMarketStocks.Item1.ToString("F0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				if (townFoodAndMarketStocks.Item2 != 0)
				{
					list.Add(new TooltipProperty(new TextObject("{=HTtWslIx}Market Food Stocks", null).ToString(), townFoodAndMarketStocks.Item2.ToString("F0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				list.Add(new TooltipProperty(new TextObject("{=laznt9ZK}Settlement Food Change", null).ToString(), foodChange.ToString("F2"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
				list.Add(new TooltipProperty(new TextObject("{=DNXD37JL}Settlement's Days Until Food Runs Out", null).ToString(), CampaignUIHelper.GetDaysUntilNoFood((float)(townFoodAndMarketStocks.Item1 + townFoodAndMarketStocks.Item2), foodChange), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				if (((town2 != null) ? town2.Settlement : null) != null && SettlementHelper.IsGarrisonStarving(town2.Settlement))
				{
					list.Add(new TooltipProperty(new TextObject("{=0rmpC7jf}The Garrison is Starving", null).ToString(), string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			else
			{
				list.Add(new TooltipProperty(new TextObject("{=Q8dhryRX}Parties' Food", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
				MBBindingList<GameMenuPartyItemVM> mbbindingList = ((side == BattleSideEnum.Attacker) ? this.AttackerPartyList : this.DefenderPartyList);
				double num = 0.0;
				foreach (GameMenuPartyItemVM gameMenuPartyItemVM2 in mbbindingList)
				{
					float val = gameMenuPartyItemVM2.Party.MobileParty.Food / -gameMenuPartyItemVM2.Party.MobileParty.FoodChange;
					num += (double)Math.Max(val, 0f);
					string daysUntilNoFood = CampaignUIHelper.GetDaysUntilNoFood(gameMenuPartyItemVM2.Party.MobileParty.Food, gameMenuPartyItemVM2.Party.MobileParty.FoodChange);
					list.Add(new TooltipProperty(gameMenuPartyItemVM2.Party.MobileParty.Name.ToString(), daysUntilNoFood, 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
				list.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
				list.Add(new TooltipProperty(new TextObject("{=rwKBR4NE}Average Days Until Food Runs Out", null).ToString(), MathF.Ceiling(num / (double)mbbindingList.Count).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return list;
		}

		// Token: 0x060011C8 RID: 4552 RVA: 0x000482B8 File Offset: 0x000464B8
		private List<TooltipProperty> GetEncounterSideMoraleTooltip(BattleSideEnum side)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty(new TextObject("{=QBB0KQ2Z}Parties' Average Morale", null).ToString(), "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			MBBindingList<GameMenuPartyItemVM> mbbindingList = ((side == BattleSideEnum.Attacker) ? this.AttackerPartyList : this.DefenderPartyList);
			double num = 0.0;
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM in mbbindingList)
			{
				list.Add(new TooltipProperty(gameMenuPartyItemVM.Party.MobileParty.Name.ToString(), gameMenuPartyItemVM.Party.MobileParty.Morale.ToString("0.0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				num += (double)gameMenuPartyItemVM.Party.MobileParty.Morale;
			}
			list.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(new TextObject("{=eoVW9z54}Average Morale", null).ToString(), (num / (double)mbbindingList.Count).ToString("0.0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		// Token: 0x170005CE RID: 1486
		// (get) Token: 0x060011C9 RID: 4553 RVA: 0x000483F0 File Offset: 0x000465F0
		// (set) Token: 0x060011CA RID: 4554 RVA: 0x000483F8 File Offset: 0x000465F8
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x170005CF RID: 1487
		// (get) Token: 0x060011CB RID: 4555 RVA: 0x0004841B File Offset: 0x0004661B
		// (set) Token: 0x060011CC RID: 4556 RVA: 0x00048423 File Offset: 0x00046623
		[DataSourceProperty]
		public BannerImageIdentifierVM DefenderPartyBanner
		{
			get
			{
				return this._defenderPartyBanner;
			}
			set
			{
				if (value != this._defenderPartyBanner)
				{
					this._defenderPartyBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "DefenderPartyBanner");
				}
			}
		}

		// Token: 0x170005D0 RID: 1488
		// (get) Token: 0x060011CD RID: 4557 RVA: 0x00048441 File Offset: 0x00046641
		// (set) Token: 0x060011CE RID: 4558 RVA: 0x00048449 File Offset: 0x00046649
		[DataSourceProperty]
		public BannerImageIdentifierVM AttackerPartyBanner
		{
			get
			{
				return this._attackerPartyBanner;
			}
			set
			{
				if (value != this._attackerPartyBanner)
				{
					this._attackerPartyBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "AttackerPartyBanner");
				}
			}
		}

		// Token: 0x170005D1 RID: 1489
		// (get) Token: 0x060011CF RID: 4559 RVA: 0x00048467 File Offset: 0x00046667
		// (set) Token: 0x060011D0 RID: 4560 RVA: 0x0004846F File Offset: 0x0004666F
		[DataSourceProperty]
		public PowerLevelComparer PowerComparer
		{
			get
			{
				return this._powerComparer;
			}
			set
			{
				if (value != this._powerComparer)
				{
					this._powerComparer = value;
					base.OnPropertyChangedWithValue<PowerLevelComparer>(value, "PowerComparer");
				}
			}
		}

		// Token: 0x170005D2 RID: 1490
		// (get) Token: 0x060011D1 RID: 4561 RVA: 0x0004848D File Offset: 0x0004668D
		// (set) Token: 0x060011D2 RID: 4562 RVA: 0x00048495 File Offset: 0x00046695
		[DataSourceProperty]
		public MBBindingList<GameMenuPartyItemVM> AttackerPartyList
		{
			get
			{
				return this._attackerPartyList;
			}
			set
			{
				if (value != this._attackerPartyList)
				{
					this._attackerPartyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuPartyItemVM>>(value, "AttackerPartyList");
				}
			}
		}

		// Token: 0x170005D3 RID: 1491
		// (get) Token: 0x060011D3 RID: 4563 RVA: 0x000484B3 File Offset: 0x000466B3
		// (set) Token: 0x060011D4 RID: 4564 RVA: 0x000484BB File Offset: 0x000466BB
		[DataSourceProperty]
		public MBBindingList<GameMenuPartyItemVM> DefenderPartyList
		{
			get
			{
				return this._defenderPartyList;
			}
			set
			{
				if (value != this._defenderPartyList)
				{
					this._defenderPartyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuPartyItemVM>>(value, "DefenderPartyList");
				}
			}
		}

		// Token: 0x170005D4 RID: 1492
		// (get) Token: 0x060011D5 RID: 4565 RVA: 0x000484D9 File Offset: 0x000466D9
		// (set) Token: 0x060011D6 RID: 4566 RVA: 0x000484E1 File Offset: 0x000466E1
		[DataSourceProperty]
		public string DefenderPartyMorale
		{
			get
			{
				return this._defenderPartyMorale;
			}
			set
			{
				if (value != this._defenderPartyMorale)
				{
					this._defenderPartyMorale = value;
					base.OnPropertyChangedWithValue<string>(value, "DefenderPartyMorale");
				}
			}
		}

		// Token: 0x170005D5 RID: 1493
		// (get) Token: 0x060011D7 RID: 4567 RVA: 0x00048504 File Offset: 0x00046704
		// (set) Token: 0x060011D8 RID: 4568 RVA: 0x0004850C File Offset: 0x0004670C
		[DataSourceProperty]
		public string AttackerPartyMorale
		{
			get
			{
				return this._attackerPartyMorale;
			}
			set
			{
				if (value != this._attackerPartyMorale)
				{
					this._attackerPartyMorale = value;
					base.OnPropertyChangedWithValue<string>(value, "AttackerPartyMorale");
				}
			}
		}

		// Token: 0x170005D6 RID: 1494
		// (get) Token: 0x060011D9 RID: 4569 RVA: 0x0004852F File Offset: 0x0004672F
		// (set) Token: 0x060011DA RID: 4570 RVA: 0x00048537 File Offset: 0x00046737
		[DataSourceProperty]
		public int DefenderPartyCount
		{
			get
			{
				return this._defenderPartyCount;
			}
			set
			{
				if (value != this._defenderPartyCount)
				{
					this._defenderPartyCount = value;
					base.OnPropertyChangedWithValue(value, "DefenderPartyCount");
				}
			}
		}

		// Token: 0x170005D7 RID: 1495
		// (get) Token: 0x060011DB RID: 4571 RVA: 0x00048555 File Offset: 0x00046755
		// (set) Token: 0x060011DC RID: 4572 RVA: 0x0004855D File Offset: 0x0004675D
		[DataSourceProperty]
		public int AttackerPartyCount
		{
			get
			{
				return this._attackerPartyCount;
			}
			set
			{
				if (value != this._attackerPartyCount)
				{
					this._attackerPartyCount = value;
					base.OnPropertyChangedWithValue(value, "AttackerPartyCount");
				}
			}
		}

		// Token: 0x170005D8 RID: 1496
		// (get) Token: 0x060011DD RID: 4573 RVA: 0x0004857B File Offset: 0x0004677B
		// (set) Token: 0x060011DE RID: 4574 RVA: 0x00048583 File Offset: 0x00046783
		[DataSourceProperty]
		public int DefenderShipCount
		{
			get
			{
				return this._defenderShipCount;
			}
			set
			{
				if (value != this._defenderShipCount)
				{
					this._defenderShipCount = value;
					base.OnPropertyChangedWithValue(value, "DefenderShipCount");
				}
			}
		}

		// Token: 0x170005D9 RID: 1497
		// (get) Token: 0x060011DF RID: 4575 RVA: 0x000485A1 File Offset: 0x000467A1
		// (set) Token: 0x060011E0 RID: 4576 RVA: 0x000485A9 File Offset: 0x000467A9
		[DataSourceProperty]
		public int AttackerShipCount
		{
			get
			{
				return this._attackerShipCount;
			}
			set
			{
				if (value != this._attackerShipCount)
				{
					this._attackerShipCount = value;
					base.OnPropertyChangedWithValue(value, "AttackerShipCount");
				}
			}
		}

		// Token: 0x170005DA RID: 1498
		// (get) Token: 0x060011E1 RID: 4577 RVA: 0x000485C7 File Offset: 0x000467C7
		// (set) Token: 0x060011E2 RID: 4578 RVA: 0x000485CF File Offset: 0x000467CF
		[DataSourceProperty]
		public string DefenderPartyFood
		{
			get
			{
				return this._defenderPartyFood;
			}
			set
			{
				if (value != this._defenderPartyFood)
				{
					this._defenderPartyFood = value;
					base.OnPropertyChangedWithValue<string>(value, "DefenderPartyFood");
				}
			}
		}

		// Token: 0x170005DB RID: 1499
		// (get) Token: 0x060011E3 RID: 4579 RVA: 0x000485F2 File Offset: 0x000467F2
		// (set) Token: 0x060011E4 RID: 4580 RVA: 0x000485FA File Offset: 0x000467FA
		[DataSourceProperty]
		public string AttackerPartyFood
		{
			get
			{
				return this._attackerPartyFood;
			}
			set
			{
				if (value != this._attackerPartyFood)
				{
					this._attackerPartyFood = value;
					base.OnPropertyChangedWithValue<string>(value, "AttackerPartyFood");
				}
			}
		}

		// Token: 0x170005DC RID: 1500
		// (get) Token: 0x060011E5 RID: 4581 RVA: 0x0004861D File Offset: 0x0004681D
		// (set) Token: 0x060011E6 RID: 4582 RVA: 0x00048625 File Offset: 0x00046825
		public string DefenderWallHitPoints
		{
			get
			{
				return this._defenderWallHitPoints;
			}
			set
			{
				if (value != this._defenderWallHitPoints)
				{
					this._defenderWallHitPoints = value;
					base.OnPropertyChangedWithValue<string>(value, "DefenderWallHitPoints");
				}
			}
		}

		// Token: 0x170005DD RID: 1501
		// (get) Token: 0x060011E7 RID: 4583 RVA: 0x00048648 File Offset: 0x00046848
		// (set) Token: 0x060011E8 RID: 4584 RVA: 0x00048650 File Offset: 0x00046850
		[DataSourceProperty]
		public bool IsNaval
		{
			get
			{
				return this._isNaval;
			}
			set
			{
				if (value != this._isNaval)
				{
					this._isNaval = value;
					base.OnPropertyChangedWithValue(value, "IsNaval");
				}
			}
		}

		// Token: 0x170005DE RID: 1502
		// (get) Token: 0x060011E9 RID: 4585 RVA: 0x0004866E File Offset: 0x0004686E
		// (set) Token: 0x060011EA RID: 4586 RVA: 0x00048676 File Offset: 0x00046876
		[DataSourceProperty]
		public bool IsSiege
		{
			get
			{
				return this._isSiege;
			}
			set
			{
				if (value != this._isSiege)
				{
					this._isSiege = value;
					base.OnPropertyChangedWithValue(value, "IsSiege");
				}
			}
		}

		// Token: 0x170005DF RID: 1503
		// (get) Token: 0x060011EB RID: 4587 RVA: 0x00048694 File Offset: 0x00046894
		// (set) Token: 0x060011EC RID: 4588 RVA: 0x0004869C File Offset: 0x0004689C
		[DataSourceProperty]
		public string DefenderPartyCountLbl
		{
			get
			{
				return this._defenderPartyCountLbl;
			}
			set
			{
				if (value != this._defenderPartyCountLbl)
				{
					this._defenderPartyCountLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DefenderPartyCountLbl");
				}
			}
		}

		// Token: 0x170005E0 RID: 1504
		// (get) Token: 0x060011ED RID: 4589 RVA: 0x000486BF File Offset: 0x000468BF
		// (set) Token: 0x060011EE RID: 4590 RVA: 0x000486C7 File Offset: 0x000468C7
		[DataSourceProperty]
		public string AttackerPartyCountLbl
		{
			get
			{
				return this._attackerPartyCountLbl;
			}
			set
			{
				if (value != this._attackerPartyCountLbl)
				{
					this._attackerPartyCountLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "AttackerPartyCountLbl");
				}
			}
		}

		// Token: 0x170005E1 RID: 1505
		// (get) Token: 0x060011EF RID: 4591 RVA: 0x000486EA File Offset: 0x000468EA
		// (set) Token: 0x060011F0 RID: 4592 RVA: 0x000486F2 File Offset: 0x000468F2
		[DataSourceProperty]
		public HintViewModel AttackerBannerHint
		{
			get
			{
				return this._attackerBannerHint;
			}
			set
			{
				if (value != this._attackerBannerHint)
				{
					this._attackerBannerHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AttackerBannerHint");
				}
			}
		}

		// Token: 0x170005E2 RID: 1506
		// (get) Token: 0x060011F1 RID: 4593 RVA: 0x00048710 File Offset: 0x00046910
		// (set) Token: 0x060011F2 RID: 4594 RVA: 0x00048718 File Offset: 0x00046918
		[DataSourceProperty]
		public HintViewModel DefenderBannerHint
		{
			get
			{
				return this._defenderBannerHint;
			}
			set
			{
				if (value != this._defenderBannerHint)
				{
					this._defenderBannerHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DefenderBannerHint");
				}
			}
		}

		// Token: 0x170005E3 RID: 1507
		// (get) Token: 0x060011F3 RID: 4595 RVA: 0x00048736 File Offset: 0x00046936
		// (set) Token: 0x060011F4 RID: 4596 RVA: 0x0004873E File Offset: 0x0004693E
		[DataSourceProperty]
		public HintViewModel AttackerTroopNumHint
		{
			get
			{
				return this._attackerTroopNumHint;
			}
			set
			{
				if (value != this._attackerTroopNumHint)
				{
					this._attackerTroopNumHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AttackerTroopNumHint");
				}
			}
		}

		// Token: 0x170005E4 RID: 1508
		// (get) Token: 0x060011F5 RID: 4597 RVA: 0x0004875C File Offset: 0x0004695C
		// (set) Token: 0x060011F6 RID: 4598 RVA: 0x00048764 File Offset: 0x00046964
		[DataSourceProperty]
		public HintViewModel DefenderTroopNumHint
		{
			get
			{
				return this._defenderTroopNumHint;
			}
			set
			{
				if (value != this._defenderTroopNumHint)
				{
					this._defenderTroopNumHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DefenderTroopNumHint");
				}
			}
		}

		// Token: 0x170005E5 RID: 1509
		// (get) Token: 0x060011F7 RID: 4599 RVA: 0x00048782 File Offset: 0x00046982
		// (set) Token: 0x060011F8 RID: 4600 RVA: 0x0004878A File Offset: 0x0004698A
		[DataSourceProperty]
		public HintViewModel AttackerShipNumHint
		{
			get
			{
				return this._attackerShipNumHint;
			}
			set
			{
				if (value != this._attackerShipNumHint)
				{
					this._attackerShipNumHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AttackerShipNumHint");
				}
			}
		}

		// Token: 0x170005E6 RID: 1510
		// (get) Token: 0x060011F9 RID: 4601 RVA: 0x000487A8 File Offset: 0x000469A8
		// (set) Token: 0x060011FA RID: 4602 RVA: 0x000487B0 File Offset: 0x000469B0
		[DataSourceProperty]
		public HintViewModel DefenderShipNumHint
		{
			get
			{
				return this._defenderShipNumHint;
			}
			set
			{
				if (value != this._defenderShipNumHint)
				{
					this._defenderShipNumHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DefenderShipNumHint");
				}
			}
		}

		// Token: 0x170005E7 RID: 1511
		// (get) Token: 0x060011FB RID: 4603 RVA: 0x000487CE File Offset: 0x000469CE
		// (set) Token: 0x060011FC RID: 4604 RVA: 0x000487D6 File Offset: 0x000469D6
		[DataSourceProperty]
		public BasicTooltipViewModel DefenderWallHint
		{
			get
			{
				return this._defenderWallHint;
			}
			set
			{
				if (value != this._defenderWallHint)
				{
					this._defenderWallHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "DefenderWallHint");
				}
			}
		}

		// Token: 0x170005E8 RID: 1512
		// (get) Token: 0x060011FD RID: 4605 RVA: 0x000487F4 File Offset: 0x000469F4
		// (set) Token: 0x060011FE RID: 4606 RVA: 0x000487FC File Offset: 0x000469FC
		[DataSourceProperty]
		public BasicTooltipViewModel DefenderFoodHint
		{
			get
			{
				return this._defenderFoodHint;
			}
			set
			{
				if (value != this._defenderFoodHint)
				{
					this._defenderFoodHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "DefenderFoodHint");
				}
			}
		}

		// Token: 0x170005E9 RID: 1513
		// (get) Token: 0x060011FF RID: 4607 RVA: 0x0004881A File Offset: 0x00046A1A
		// (set) Token: 0x06001200 RID: 4608 RVA: 0x00048822 File Offset: 0x00046A22
		[DataSourceProperty]
		public BasicTooltipViewModel AttackerFoodHint
		{
			get
			{
				return this._attackerFoodHint;
			}
			set
			{
				if (value != this._attackerFoodHint)
				{
					this._attackerFoodHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "AttackerFoodHint");
				}
			}
		}

		// Token: 0x170005EA RID: 1514
		// (get) Token: 0x06001201 RID: 4609 RVA: 0x00048840 File Offset: 0x00046A40
		// (set) Token: 0x06001202 RID: 4610 RVA: 0x00048848 File Offset: 0x00046A48
		[DataSourceProperty]
		public BasicTooltipViewModel AttackerMoraleHint
		{
			get
			{
				return this._attackerMoraleHint;
			}
			set
			{
				if (value != this._attackerMoraleHint)
				{
					this._attackerMoraleHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "AttackerMoraleHint");
				}
			}
		}

		// Token: 0x170005EB RID: 1515
		// (get) Token: 0x06001203 RID: 4611 RVA: 0x00048866 File Offset: 0x00046A66
		// (set) Token: 0x06001204 RID: 4612 RVA: 0x0004886E File Offset: 0x00046A6E
		[DataSourceProperty]
		public BasicTooltipViewModel DefenderMoraleHint
		{
			get
			{
				return this._defenderMoraleHint;
			}
			set
			{
				if (value != this._defenderMoraleHint)
				{
					this._defenderMoraleHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "DefenderMoraleHint");
				}
			}
		}

		// Token: 0x0400081D RID: 2077
		private GameMenuPartyItemVM _defenderLeadingParty;

		// Token: 0x0400081E RID: 2078
		private GameMenuPartyItemVM _attackerLeadingParty;

		// Token: 0x0400081F RID: 2079
		private string _titleText;

		// Token: 0x04000820 RID: 2080
		private BannerImageIdentifierVM _defenderPartyBanner;

		// Token: 0x04000821 RID: 2081
		private BannerImageIdentifierVM _attackerPartyBanner;

		// Token: 0x04000822 RID: 2082
		private MBBindingList<GameMenuPartyItemVM> _attackerPartyList;

		// Token: 0x04000823 RID: 2083
		private MBBindingList<GameMenuPartyItemVM> _defenderPartyList;

		// Token: 0x04000824 RID: 2084
		private string _attackerPartyMorale;

		// Token: 0x04000825 RID: 2085
		private string _defenderPartyMorale;

		// Token: 0x04000826 RID: 2086
		private int _attackerPartyCount;

		// Token: 0x04000827 RID: 2087
		private int _defenderPartyCount;

		// Token: 0x04000828 RID: 2088
		private int _defenderShipCount;

		// Token: 0x04000829 RID: 2089
		private int _attackerShipCount;

		// Token: 0x0400082A RID: 2090
		private string _attackerPartyFood;

		// Token: 0x0400082B RID: 2091
		private string _defenderPartyFood;

		// Token: 0x0400082C RID: 2092
		private string _defenderWallHitPoints;

		// Token: 0x0400082D RID: 2093
		private string _defenderPartyCountLbl;

		// Token: 0x0400082E RID: 2094
		private string _attackerPartyCountLbl;

		// Token: 0x0400082F RID: 2095
		private bool _isNaval;

		// Token: 0x04000830 RID: 2096
		private bool _isSiege;

		// Token: 0x04000831 RID: 2097
		private PowerLevelComparer _powerComparer;

		// Token: 0x04000832 RID: 2098
		private HintViewModel _attackerBannerHint;

		// Token: 0x04000833 RID: 2099
		private HintViewModel _defenderBannerHint;

		// Token: 0x04000834 RID: 2100
		private HintViewModel _attackerTroopNumHint;

		// Token: 0x04000835 RID: 2101
		private HintViewModel _defenderTroopNumHint;

		// Token: 0x04000836 RID: 2102
		private HintViewModel _attackerShipNumHint;

		// Token: 0x04000837 RID: 2103
		private HintViewModel _defenderShipNumHint;

		// Token: 0x04000838 RID: 2104
		private BasicTooltipViewModel _defenderWallHint;

		// Token: 0x04000839 RID: 2105
		private BasicTooltipViewModel _defenderFoodHint;

		// Token: 0x0400083A RID: 2106
		private BasicTooltipViewModel _attackerFoodHint;

		// Token: 0x0400083B RID: 2107
		private BasicTooltipViewModel _attackerMoraleHint;

		// Token: 0x0400083C RID: 2108
		private BasicTooltipViewModel _defenderMoraleHint;
	}
}

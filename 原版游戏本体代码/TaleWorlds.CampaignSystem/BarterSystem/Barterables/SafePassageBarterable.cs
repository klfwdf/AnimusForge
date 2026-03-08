using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables
{
	// Token: 0x02000488 RID: 1160
	public class SafePassageBarterable : Barterable
	{
		// Token: 0x17000E71 RID: 3697
		// (get) Token: 0x060048CE RID: 18638 RVA: 0x0016EDFD File Offset: 0x0016CFFD
		public override string StringID
		{
			get
			{
				return "safe_passage_barterable";
			}
		}

		// Token: 0x17000E72 RID: 3698
		// (get) Token: 0x060048CF RID: 18639 RVA: 0x0016EE04 File Offset: 0x0016D004
		public override TextObject Name
		{
			get
			{
				TextObject textObject;
				if (this._otherHero != null)
				{
					StringHelpers.SetCharacterProperties("HERO", this._otherHero.CharacterObject, null, false);
					textObject = new TextObject("{=BJbbahYe}Let {HERO.NAME} Go", null);
				}
				else
				{
					textObject = new TextObject("{=QKNWsJRb}Let {PARTY} Go", null);
					textObject.SetTextVariable("PARTY", this._otherParty.Name);
				}
				return textObject;
			}
		}

		// Token: 0x060048D0 RID: 18640 RVA: 0x0016EE63 File Offset: 0x0016D063
		public SafePassageBarterable(Hero originalOwner, Hero otherHero, PartyBase ownerParty, PartyBase otherParty)
			: base(originalOwner, ownerParty)
		{
			this._otherHero = otherHero;
			this._otherParty = otherParty;
		}

		// Token: 0x060048D1 RID: 18641 RVA: 0x0016EE7C File Offset: 0x0016D07C
		public override int GetUnitValueForFaction(IFaction faction)
		{
			float num = MathF.Clamp(this.GetPlayerStrengthRatioInEncounter(), 0f, 1f);
			int num2 = (int)MathF.Clamp((float)(Hero.MainHero.Gold + PartyBase.MainParty.ItemRoster.Sum((ItemRosterElement t) => t.EquipmentElement.Item.Value * t.Amount)), 0f, 2.1474836E+09f);
			float num3 = ((num < 1f) ? (0.05f + (1f - num) * 0.2f) : 0.1f);
			float num4 = ((faction.Leader == null) ? 1f : MathF.Clamp((50f + (float)faction.Leader.GetRelation(this._otherHero)) / 50f, 0.05f, 1.1f));
			if (!PlayerEncounter.EncounteredParty.IsMobile || !PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
			{
				num2 += 3000 + (int)(Hero.MainHero.Clan.Renown * 50f);
				num3 *= 1.5f;
			}
			if (MobileParty.MainParty.MapEvent != null || MobileParty.MainParty.SiegeEvent != null)
			{
				num3 *= 1.2f;
			}
			int num5 = (int)((float)num2 * num3 + 1000f);
			MobileParty mobileParty = PlayerEncounter.EncounteredParty.MobileParty;
			if (mobileParty != null && mobileParty.IsBandit)
			{
				num5 /= 8;
				if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.SweetTalker) && !MobileParty.MainParty.IsCurrentlyAtSea)
				{
					num5 += MathF.Round((float)num5 * DefaultPerks.Roguery.SweetTalker.PrimaryBonus);
				}
			}
			else
			{
				num5 /= 2;
				num5 += (int)(0.3f * num3 * Campaign.Current.Models.ValuationModel.GetMilitaryValueOfParty(this._otherParty.MobileParty));
				num5 += (int)(0.3f * num3 * Campaign.Current.Models.ValuationModel.GetValueOfHero(this._otherHero));
			}
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.MarketDealer))
			{
				num5 += MathF.Round((float)num5 * DefaultPerks.Trade.MarketDealer.PrimaryBonus);
			}
			Hero originalOwner = base.OriginalOwner;
			if (faction != ((originalOwner != null) ? originalOwner.Clan : null))
			{
				Hero originalOwner2 = base.OriginalOwner;
				if (faction != ((originalOwner2 != null) ? originalOwner2.MapFaction : null) && faction != base.OriginalParty.MapFaction)
				{
					Hero otherHero = this._otherHero;
					if (faction != ((otherHero != null) ? otherHero.Clan : null))
					{
						Hero otherHero2 = this._otherHero;
						if (faction != ((otherHero2 != null) ? otherHero2.MapFaction : null) && faction != this._otherParty.MapFaction)
						{
							return num5;
						}
					}
					return (int)(0.9f * (float)num5);
				}
			}
			return -(int)((float)num5 / (num4 * num4));
		}

		// Token: 0x060048D2 RID: 18642 RVA: 0x0016F120 File Offset: 0x0016D320
		public float GetPlayerStrengthRatioInEncounter()
		{
			List<MobileParty> list = new List<MobileParty>();
			List<MobileParty> list2 = new List<MobileParty>();
			PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
			if (!list.Contains(MobileParty.MainParty))
			{
				list.Add(MobileParty.MainParty);
			}
			if (!list2.Contains(base.OriginalParty.MobileParty))
			{
				list2.Add(base.OriginalParty.MobileParty);
			}
			float num = 0f;
			float num2 = 0f;
			MapEvent.PowerCalculationContext context = (PlayerEncounter.IsNavalEncounter() ? MapEvent.PowerCalculationContext.SeaBattle : MapEvent.PowerCalculationContext.PlainBattle);
			foreach (MobileParty mobileParty in list)
			{
				if (mobileParty != null)
				{
					num += mobileParty.Party.GetCustomStrength(BattleSideEnum.Defender, context);
				}
				else
				{
					Debug.FailedAssert("Player side party null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BarterSystem\\Barterables\\SafePassageBarterable.cs", "GetPlayerStrengthRatioInEncounter", 145);
				}
			}
			foreach (MobileParty mobileParty2 in list2)
			{
				if (mobileParty2 != null)
				{
					num2 += mobileParty2.Party.GetCustomStrength(BattleSideEnum.Attacker, context);
				}
				else
				{
					Debug.FailedAssert("Opponent side party null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BarterSystem\\Barterables\\SafePassageBarterable.cs", "GetPlayerStrengthRatioInEncounter", 157);
				}
			}
			if (num2 <= 0f)
			{
				num2 = 1E-05f;
			}
			return num / num2;
		}

		// Token: 0x060048D3 RID: 18643 RVA: 0x0016F284 File Offset: 0x0016D484
		public override bool IsCompatible(Barterable barterable)
		{
			return true;
		}

		// Token: 0x060048D4 RID: 18644 RVA: 0x0016F287 File Offset: 0x0016D487
		public override ImageIdentifier GetVisualIdentifier()
		{
			return null;
		}

		// Token: 0x060048D5 RID: 18645 RVA: 0x0016F28C File Offset: 0x0016D48C
		public override void Apply()
		{
			if (PlayerEncounter.Current != null)
			{
				List<MobileParty> partiesToJoinPlayerSide = new List<MobileParty>();
				List<MobileParty> list = new List<MobileParty>();
				PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(partiesToJoinPlayerSide, list);
				if (!list.Contains(base.OriginalParty.MobileParty))
				{
					list.Add(base.OriginalParty.MobileParty);
				}
				PartyBase originalParty = base.OriginalParty;
				if (((originalParty != null) ? originalParty.SiegeEvent : null) != null && base.OriginalParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(base.OriginalParty, MapEvent.BattleTypes.Siege) && this._otherParty != null && base.OriginalParty.SiegeEvent.BesiegedSettlement.HasInvolvedPartyForEventType(this._otherParty, MapEvent.BattleTypes.Siege))
				{
					if (base.OriginalParty.SiegeEvent.BesiegedSettlement.MapFaction == Hero.MainHero.MapFaction)
					{
						GainKingdomInfluenceAction.ApplyForSiegeSafePassageBarter(MobileParty.MainParty, -10f);
					}
					Campaign.Current.GameMenuManager.SetNextMenu("menu_siege_safe_passage_accepted");
					PlayerSiege.FinalizePlayerSiege();
					using (List<MobileParty>.Enumerator enumerator = list.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							MobileParty mobileParty = enumerator.Current;
							mobileParty.Ai.SetDoNotAttackMainParty(32);
						}
						return;
					}
				}
				foreach (MobileParty mobileParty2 in list)
				{
					mobileParty2.Ai.SetDoNotAttackMainParty(32);
					mobileParty2.SetMoveModeHold();
					mobileParty2.IgnoreForHours(32f);
					mobileParty2.Ai.SetInitiative(0f, 0.8f, 8f);
				}
				PlayerEncounter.LeaveEncounter = true;
				if (MobileParty.MainParty.SiegeEvent != null && MobileParty.MainParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(PartyBase.MainParty, MapEvent.BattleTypes.Siege))
				{
					MobileParty.MainParty.BesiegerCamp = null;
				}
				PartyBase originalParty2 = base.OriginalParty;
				bool flag;
				if (originalParty2 == null)
				{
					flag = null != null;
				}
				else
				{
					MobileParty mobileParty3 = originalParty2.MobileParty;
					flag = ((mobileParty3 != null) ? mobileParty3.Ai.AiBehaviorPartyBase : null) != null;
				}
				if (flag && base.OriginalParty != PartyBase.MainParty)
				{
					base.OriginalParty.MobileParty.SetMoveModeHold();
					if (base.OriginalParty.MobileParty.Army != null && MobileParty.MainParty.Army != base.OriginalParty.MobileParty.Army)
					{
						base.OriginalParty.MobileParty.Army.LeaderParty.SetMoveModeHold();
						return;
					}
				}
			}
			else
			{
				Debug.FailedAssert("Can not find player encounter for safe passage barterable", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BarterSystem\\Barterables\\SafePassageBarterable.cs", "Apply", 243);
			}
		}

		// Token: 0x060048D6 RID: 18646 RVA: 0x0016F520 File Offset: 0x0016D720
		internal static void AutoGeneratedStaticCollectObjectsSafePassageBarterable(object o, List<object> collectedObjects)
		{
			((SafePassageBarterable)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x060048D7 RID: 18647 RVA: 0x0016F52E File Offset: 0x0016D72E
		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x04001411 RID: 5137
		private readonly Hero _otherHero;

		// Token: 0x04001412 RID: 5138
		private readonly PartyBase _otherParty;
	}
}

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

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables;

public class SafePassageBarterable : Barterable
{
	private readonly Hero _otherHero;

	private readonly PartyBase _otherParty;

	public override string StringID => "safe_passage_barterable";

	public override TextObject Name
	{
		get
		{
			TextObject textObject;
			if (_otherHero != null)
			{
				StringHelpers.SetCharacterProperties("HERO", _otherHero.CharacterObject);
				textObject = new TextObject("{=BJbbahYe}Let {HERO.NAME} Go");
			}
			else
			{
				textObject = new TextObject("{=QKNWsJRb}Let {PARTY} Go");
				textObject.SetTextVariable("PARTY", _otherParty.Name);
			}
			return textObject;
		}
	}

	public SafePassageBarterable(Hero originalOwner, Hero otherHero, PartyBase ownerParty, PartyBase otherParty)
		: base(originalOwner, ownerParty)
	{
		_otherHero = otherHero;
		_otherParty = otherParty;
	}

	public override int GetUnitValueForFaction(IFaction faction)
	{
		float num = MathF.Clamp(GetPlayerStrengthRatioInEncounter(), 0f, 1f);
		int num2 = (int)MathF.Clamp(Hero.MainHero.Gold + PartyBase.MainParty.ItemRoster.Sum((ItemRosterElement t) => t.EquipmentElement.Item.Value * t.Amount), 0f, 2.1474836E+09f);
		float num3 = ((num < 1f) ? (0.05f + (1f - num) * 0.2f) : 0.1f);
		float num4 = ((faction.Leader == null) ? 1f : MathF.Clamp((50f + (float)faction.Leader.GetRelation(_otherHero)) / 50f, 0.05f, 1.1f));
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
			num5 += (int)(0.3f * num3 * Campaign.Current.Models.ValuationModel.GetMilitaryValueOfParty(_otherParty.MobileParty));
			num5 += (int)(0.3f * num3 * Campaign.Current.Models.ValuationModel.GetValueOfHero(_otherHero));
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.MarketDealer))
		{
			num5 += MathF.Round((float)num5 * DefaultPerks.Trade.MarketDealer.PrimaryBonus);
		}
		if (faction == base.OriginalOwner?.Clan || faction == base.OriginalOwner?.MapFaction || faction == base.OriginalParty.MapFaction)
		{
			return -(int)((float)num5 / (num4 * num4));
		}
		if (faction == _otherHero?.Clan || faction == _otherHero?.MapFaction || faction == _otherParty.MapFaction)
		{
			return (int)(0.9f * (float)num5);
		}
		return num5;
	}

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
		foreach (MobileParty item in list)
		{
			if (item != null)
			{
				num += item.Party.GetCustomStrength(BattleSideEnum.Defender, context);
			}
			else
			{
				Debug.FailedAssert("Player side party null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BarterSystem\\Barterables\\SafePassageBarterable.cs", "GetPlayerStrengthRatioInEncounter", 145);
			}
		}
		foreach (MobileParty item2 in list2)
		{
			if (item2 != null)
			{
				num2 += item2.Party.GetCustomStrength(BattleSideEnum.Attacker, context);
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

	public override bool IsCompatible(Barterable barterable)
	{
		return true;
	}

	public override ImageIdentifier GetVisualIdentifier()
	{
		return null;
	}

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
			if (base.OriginalParty?.SiegeEvent != null && base.OriginalParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(base.OriginalParty) && _otherParty != null && base.OriginalParty.SiegeEvent.BesiegedSettlement.HasInvolvedPartyForEventType(_otherParty))
			{
				if (base.OriginalParty.SiegeEvent.BesiegedSettlement.MapFaction == Hero.MainHero.MapFaction)
				{
					GainKingdomInfluenceAction.ApplyForSiegeSafePassageBarter(MobileParty.MainParty, -10f);
				}
				Campaign.Current.GameMenuManager.SetNextMenu("menu_siege_safe_passage_accepted");
				PlayerSiege.FinalizePlayerSiege();
				{
					foreach (MobileParty item in list)
					{
						item.Ai.SetDoNotAttackMainParty(32);
					}
					return;
				}
			}
			foreach (MobileParty item2 in list)
			{
				item2.Ai.SetDoNotAttackMainParty(32);
				item2.SetMoveModeHold();
				item2.IgnoreForHours(32f);
				item2.Ai.SetInitiative(0f, 0.8f, 8f);
			}
			PlayerEncounter.LeaveEncounter = true;
			if (MobileParty.MainParty.SiegeEvent != null && MobileParty.MainParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(PartyBase.MainParty))
			{
				MobileParty.MainParty.BesiegerCamp = null;
			}
			if (base.OriginalParty?.MobileParty?.Ai.AiBehaviorPartyBase != null && base.OriginalParty != PartyBase.MainParty)
			{
				base.OriginalParty.MobileParty.SetMoveModeHold();
				if (base.OriginalParty.MobileParty.Army != null && MobileParty.MainParty.Army != base.OriginalParty.MobileParty.Army)
				{
					base.OriginalParty.MobileParty.Army.LeaderParty.SetMoveModeHold();
				}
			}
		}
		else
		{
			Debug.FailedAssert("Can not find player encounter for safe passage barterable", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BarterSystem\\Barterables\\SafePassageBarterable.cs", "Apply", 243);
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsSafePassageBarterable(object o, List<object> collectedObjects)
	{
		((SafePassageBarterable)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}
}

using System;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class SeaDamageCampaignBehavior : CampaignBehaviorBase
{
	public static bool DebugSeaDamage;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
	}

	private void Tick(float dt)
	{
		if (!DebugSeaDamage)
		{
			return;
		}
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.IsVisible && item.CurrentSettlement == null && item.IsCurrentlyAtSea && item.Ships.Any())
			{
				Ship ship = item.Ships[0];
				float maxHitPoints = ship.MaxHitPoints;
				float hitPoints = ship.HitPoints;
				Vec3 vec = item.Position.AsVec3() + Vec3.Up * 3.75f;
				vec.x -= 1f;
				int num = 0;
				float num2 = Campaign.Current.Models.CampaignShipDamageModel.GetHourlyShipDamage(item, ship);
				if (num2 > 0f)
				{
					num = (int)(ship.HitPoints / ship.MaxHitPoints / num2);
				}
				string text = ((TerrainType)item.CurrentNavigationFace.FaceGroupIndex/*cast due to .constrained prefix*/).ToString();
				string text2 = $"Max Hitpoints: {maxHitPoints}\nHitpoints: {hitPoints}\nSeaworthiness: {ship.SeaWorthiness}\nTerrain: {text}\nEffected by: {Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(item.Position.ToVec2()).ToString()}";
				if (num > 0)
				{
					text2 += $"\nEstimated Hours: {num}";
				}
				else
				{
					text2 += "\nEstimated Hours: N/A";
				}
			}
		}
	}

	private void HourlyTickParty(MobileParty party)
	{
		if (!party.IsActive || !party.IsCurrentlyAtSea || party.IsInNavalAutoTravel || party.MapEvent != null)
		{
			return;
		}
		for (int num = party.Ships.Count - 1; num >= 0; num--)
		{
			float num2 = Campaign.Current.Models.CampaignShipDamageModel.GetHourlyShipDamage(party, party.Ships[num]);
			if (num2 > 0f)
			{
				party.Ships[num].OnShipDamaged(num2, null, out var _);
			}
		}
		Hero perkOwnerHero = null;
		if (party.HasPerk(NavalPerks.Shipmaster.MasterAndCommander, out perkOwnerHero))
		{
			int amount = TaleWorlds.Library.MathF.Round(NavalPerks.Shipmaster.MasterAndCommander.PrimaryBonus);
			AddXpToTroops(party, amount);
		}
	}

	private static void AddXpToTroops(MobileParty party, int amount)
	{
		TroopRoster memberRoster = party.MemberRoster;
		for (int i = 0; i < memberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
			if (!elementCopyAtIndex.Character.IsHero && MobilePartyHelper.CanTroopGainXp(party.Party, elementCopyAtIndex.Character, out var gainableMaxXp))
			{
				int xpAmount = Math.Min(gainableMaxXp, amount);
				memberRoster.AddXpToTroopAtIndex(i, xpAmount);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}

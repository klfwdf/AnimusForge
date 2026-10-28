using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC;

public static class NavalIncidentEffects
{
	public static IncidentEffect FleetShipHitPointsChangeFactor(float factor)
	{
		return new IncidentEffect(() => MobileParty.MainParty.Ships.Count > 0, delegate
		{
			for (int num = MobileParty.MainParty.Ships.Count - 1; num >= 0; num--)
			{
				MobileParty.MainParty.Ships[num].HitPoints += MobileParty.MainParty.Ships[num].MaxHitPoints * factor;
			}
			TextObject textObject = new TextObject("{=o1SaScaT}{?FACTOR > 0}Repaired{?}Damaged{\\?} each ship in your fleet by {ABS(FACTOR)}% of its hull.");
			textObject.SetTextVariable("FACTOR", TaleWorlds.Library.MathF.Round(factor * 100f));
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect.ChanceToOccur >= 1f)
			{
				textObject = new TextObject("{=LxKiqOsc}{?FACTOR > 0}Repair{?}Damage{\\?} each ship in your fleet by {ABS(FACTOR)}% of its hull");
			}
			else
			{
				textObject = new TextObject("{=gb3RUfJB}{CHANCE}% chance of {?FACTOR > 0}repairing{?}damaging{\\?} each ship in your fleet by {ABS(FACTOR)}% of its hull");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect.ChanceToOccur * 100f));
			}
			textObject.SetTextVariable("FACTOR", TaleWorlds.Library.MathF.Round(factor * 100f));
			return new IncidentHint(textObject);
		});
	}

	public static IncidentEffect FlagShipHitPointsChange(float factor)
	{
		return new IncidentEffect(() => MobileParty.MainParty.Party.FlagShip != null && MobileParty.MainParty.Party.FlagShip.HitPoints / MobileParty.MainParty.Party.FlagShip.MaxHitPoints > 0.2f, delegate
		{
			Ship flagShip = MobileParty.MainParty.Party.FlagShip;
			float num = factor * flagShip.MaxHitPoints;
			flagShip.HitPoints += num;
			TextObject textObject = new TextObject("{=nbPQMtez}{SHIP} {?AMOUNT > 0}repaired{?}damaged{\\?} by {ABS(AMOUNT)}% of its hull.");
			textObject.SetTextVariable("SHIP", flagShip.Name);
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(factor * 100f));
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			Ship flagShip = MobileParty.MainParty.Party.FlagShip;
			TextObject textObject;
			if (effect.ChanceToOccur >= 1f)
			{
				textObject = new TextObject("{=WfclGBnE}{?AMOUNT > 0}Repair{?}Damage{\\?} {SHIP} by {ABS(AMOUNT)}% of its hull");
			}
			else
			{
				textObject = new TextObject("{=vMQlD2Iz}{CHANCE}% chance of {?AMOUNT > 0}repairing{?}damaging{\\?} {SHIP} by {ABS(AMOUNT)}% of its hull");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect.ChanceToOccur * 100f));
			}
			textObject.SetTextVariable("SHIP", flagShip.Name);
			textObject.SetTextVariable("AMOUNT", TaleWorlds.Library.MathF.Round(factor * 100f));
			return new IncidentHint(textObject);
		});
	}

	public static IncidentEffect DestroyShipSiegeEngine(Func<Ship> shipGetter)
	{
		return new IncidentEffect(delegate
		{
			Ship ship = shipGetter?.Invoke();
			return ship != null && ship.GetSiegeEngines().Count > 0;
		}, delegate
		{
			Ship ship = shipGetter();
			List<TextObject> list = new List<TextObject>();
			foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
			{
				if (ship.GetPieceAtSlot(availableSlot.Key)?.SiegeEngine != null)
				{
					ship.EquipUpgradePiece(availableSlot.Key, null);
					TextObject textObject = new TextObject("{=dUaKktxr}The siege engine on {SHIP} was destroyed.");
					textObject.SetTextVariable("SHIP", ship.Name);
					list.Add(textObject);
					break;
				}
			}
			return list;
		}, delegate(IncidentEffect effect)
		{
			Ship ship = shipGetter();
			TextObject textObject;
			if (effect.ChanceToOccur >= 1f)
			{
				textObject = new TextObject("{=bhJuClWn}Destroy the ballista on {SHIP}");
			}
			else
			{
				textObject = new TextObject("{=co9YLn5v}{CHANCE}% chance of destroying the ballista on {SHIP}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect.ChanceToOccur * 100f));
			}
			textObject.SetTextVariable("SHIP", ship.Name);
			return new IncidentHint(textObject);
		});
	}

	public static IncidentEffect DestroyShip(Func<Ship> shipGetter)
	{
		return new IncidentEffect(() => shipGetter?.Invoke() != null, delegate
		{
			Ship ship = shipGetter();
			TextObject name = ship.Name;
			DestroyShipAction.Apply(ship);
			TextObject textObject = new TextObject("{=90DaUas5}{SHIP} was lost.");
			textObject.SetTextVariable("SHIP", name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			Ship ship = shipGetter();
			TextObject textObject;
			if (effect.ChanceToOccur >= 1f)
			{
				textObject = new TextObject("{=6xm2DTyL}{SHIP} is lost");
			}
			else
			{
				textObject = new TextObject("{=3DbDGh31}{CHANCE}% chance of losing {SHIP}");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect.ChanceToOccur * 100f));
			}
			textObject.SetTextVariable("SHIP", ship.Name);
			return new IncidentHint(textObject);
		});
	}

	public static IncidentEffect UnlockFigurehead(Figurehead figurehead)
	{
		return new IncidentEffect(() => figurehead != null && !Campaign.Current.UnlockedFigureheadsByMainHero.Contains(figurehead), delegate
		{
			Campaign.Current.UnlockFigurehead(figurehead);
			TextObject textObject = new TextObject("{=awUuSjS6}Unlocked the {FIGUREHEAD} figurehead.");
			textObject.SetTextVariable("FIGUREHEAD", figurehead.Name);
			return new List<TextObject> { textObject };
		}, delegate(IncidentEffect effect)
		{
			TextObject textObject;
			if (effect.ChanceToOccur >= 1f)
			{
				textObject = new TextObject("{=Z8lGvfwD}Unlock the {FIGUREHEAD} figurehead");
			}
			else
			{
				textObject = new TextObject("{=iRe1EQft}{CHANCE}% chance of unlocking the {FIGUREHEAD} figurehead");
				textObject.SetTextVariable("CHANCE", TaleWorlds.Library.MathF.Round(effect.ChanceToOccur * 100f));
			}
			textObject.SetTextVariable("FIGUREHEAD", figurehead.Name);
			return new IncidentHint(textObject);
		});
	}
}

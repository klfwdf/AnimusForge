using System;
using System.Collections.Generic;
using Helpers;
using NavalDLC.Missions.BattleScore;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.BattleScore;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace NavalDLC.ViewModelCollection;

public class NavalScoreboardVM : SPScoreboardVM
{
	private class ScoreboardShipComparer : IComparer<SPScoreboardShipVM>
	{
		public int Compare(SPScoreboardShipVM x, SPScoreboardShipVM y)
		{
			int num = y.IsPlayerTeam.CompareTo(x.IsPlayerTeam);
			if (num != 0)
			{
				return num;
			}
			num = y.FormationIndex.CompareTo(x.FormationIndex);
			if (num != 0)
			{
				return num;
			}
			bool isPlayerShip = x.Ship.IsPlayerShip;
			num = y.Ship.IsPlayerShip.CompareTo(isPlayerShip);
			if (num != 0)
			{
				return num;
			}
			string obj = x.Owner?.Name.ToString() ?? string.Empty;
			string strB = y.Owner?.Name.ToString() ?? string.Empty;
			num = obj.CompareTo(strB);
			if (num != 0)
			{
				return num;
			}
			return ResolveEquality(x, y);
		}

		private int ResolveEquality(SPScoreboardShipVM x, SPScoreboardShipVM y)
		{
			return (y.Ship as Ship).ShipHull.Value.CompareTo((x.Ship as Ship).ShipHull.Value);
		}
	}

	private NavalShipsLogic _navalShipsLogic;

	private readonly ScoreboardShipComparer _scoreboardShipComparer = new ScoreboardShipComparer();

	public new static NavalScoreboardVM CreateSimulation(BattleSimulation simulation)
	{
		return new NavalScoreboardVM((BattleScoreContext)(object)new NavalSimulationBattleScoreContext(simulation), simulation);
	}

	public new static NavalScoreboardVM CreateMission(Mission mission)
	{
		return new NavalScoreboardVM((BattleScoreContext)(object)new NavalBattleScoreContext(mission), null);
	}

	public new static NavalScoreboardVM CreateCustom(BattleScoreContext battleScoreContext, BattleSimulation simulation = null)
	{
		return new NavalScoreboardVM(battleScoreContext, simulation);
	}

	private NavalScoreboardVM(BattleScoreContext scoreboardContext, BattleSimulation simulation)
		: base(scoreboardContext, simulation)
	{
		SPScoreboardShipVM.GetTooltip = GetShipTooltip;
		base.IsNavalBattle = true;
	}

	public override void Initialize(IMissionScreen missionScreen, Mission mission, Action releaseSimulationSources, Action<bool> onToggle)
	{
		base.Initialize(missionScreen, mission, releaseSimulationSources, onToggle);
		if (base.IsSimulation)
		{
			MapEvent mapEvent = MobileParty.MainParty?.MapEvent;
			if (mapEvent == null || (!mapEvent.IsNavalMapEvent && !MapEventHelper.IsNavalRaid(mapEvent)))
			{
				Debug.FailedAssert("Naval scoreboard initialized in simulation mode, but the current map event isn't naval!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalScoreboardVM.cs", "Initialize", 58);
				return;
			}
		}
		else
		{
			Mission current = Mission.Current;
			if (current == null || (!current.IsNavalBattle && !current.IsNavalRaidBattle))
			{
				Debug.FailedAssert("Naval scoreboard initialized in mission mode, but the current mission isn't naval!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalScoreboardVM.cs", "Initialize", 68);
				return;
			}
		}
		if (base.IsSimulation)
		{
			bool flag = MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker;
			using (List<Ship>.Enumerator enumerator = MobileParty.MainParty.MapEvent.AttackerSide.SimulationShipList.GetEnumerator())
			{
				Ship current2;
				TeamSideEnum teamSideEnum;
				for (; enumerator.MoveNext(); base.Attackers.GetShipAddIfNotExists(current2, current2.ShipHull.Type.ToString(), current2.Owner, teamSideEnum, 10))
				{
					current2 = enumerator.Current;
					if (flag)
					{
						if (current2.Owner != PartyBase.MainParty)
						{
							Army army = MobileParty.MainParty.Army;
							if (army == null || !army.DoesLeaderPartyAndAttachedPartiesContain(current2.Owner.MobileParty))
							{
								teamSideEnum = TeamSideEnum.PlayerAllyTeam;
								continue;
							}
						}
						teamSideEnum = TeamSideEnum.PlayerTeam;
					}
					else
					{
						teamSideEnum = TeamSideEnum.EnemyTeam;
					}
				}
			}
			if (!MapEventHelper.IsNavalRaid(MobileParty.MainParty?.MapEvent))
			{
				using List<Ship>.Enumerator enumerator = MobileParty.MainParty.MapEvent.DefenderSide.SimulationShipList.GetEnumerator();
				Ship current3;
				TeamSideEnum teamSideEnum2;
				for (; enumerator.MoveNext(); base.Defenders.GetShipAddIfNotExists(current3, current3.ShipHull.Type.ToString(), current3.Owner, teamSideEnum2, 10))
				{
					current3 = enumerator.Current;
					if (flag)
					{
						teamSideEnum2 = TeamSideEnum.EnemyTeam;
						continue;
					}
					if (current3.Owner != PartyBase.MainParty)
					{
						Army army2 = MobileParty.MainParty.Army;
						if (army2 == null || !army2.DoesLeaderPartyAndAttachedPartiesContain(current3.Owner.MobileParty))
						{
							teamSideEnum2 = TeamSideEnum.PlayerAllyTeam;
							continue;
						}
					}
					teamSideEnum2 = TeamSideEnum.PlayerTeam;
				}
			}
			base.Attackers.Ships.Sort(_scoreboardShipComparer);
			base.Defenders.Ships.Sort(_scoreboardShipComparer);
		}
		else
		{
			_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (base.IsSimulation)
		{
			for (int i = 0; i < base.Attackers.Ships.Count; i++)
			{
				base.Attackers.Ships[i].CurrentHealth = base.Attackers.Ships[i].Ship.HitPoints;
			}
			for (int j = 0; j < base.Defenders.Ships.Count; j++)
			{
				base.Defenders.Ships[j].CurrentHealth = base.Defenders.Ships[j].Ship.HitPoints;
			}
		}
		else
		{
			if (_navalShipsLogic == null)
			{
				return;
			}
			UpdateTeamShips(removeOld: false, addNew: true, sort: false);
			for (int k = 0; k < base.Attackers.Ships.Count; k++)
			{
				SPScoreboardShipVM sPScoreboardShipVM = base.Attackers.Ships[k];
				ShipAssignment shipAssignment;
				bool flag = _navalShipsLogic.FindAssignmentOfShipOrigin(sPScoreboardShipVM.Ship, out shipAssignment);
				sPScoreboardShipVM.CurrentHealth = sPScoreboardShipVM.Ship.HitPoints;
				int isInactive;
				if (flag)
				{
					Formation formation = shipAssignment.Formation;
					isInactive = ((formation != null && formation.CountOfUnits == 0) ? 1 : 0);
				}
				else
				{
					isInactive = 1;
				}
				sPScoreboardShipVM.IsInactive = (byte)isInactive != 0;
				sPScoreboardShipVM.IsRetreated = !flag && sPScoreboardShipVM.CurrentHealth > 0f;
			}
			for (int l = 0; l < base.Defenders.Ships.Count; l++)
			{
				SPScoreboardShipVM sPScoreboardShipVM2 = base.Defenders.Ships[l];
				ShipAssignment shipAssignment2;
				bool flag2 = _navalShipsLogic.FindAssignmentOfShipOrigin(sPScoreboardShipVM2.Ship, out shipAssignment2);
				sPScoreboardShipVM2.CurrentHealth = sPScoreboardShipVM2.Ship.HitPoints;
				int isInactive2;
				if (flag2)
				{
					Formation formation2 = shipAssignment2.Formation;
					isInactive2 = ((formation2 != null && formation2.CountOfUnits == 0) ? 1 : 0);
				}
				else
				{
					isInactive2 = 1;
				}
				sPScoreboardShipVM2.IsInactive = (byte)isInactive2 != 0;
				sPScoreboardShipVM2.IsRetreated = !flag2 && sPScoreboardShipVM2.CurrentHealth > 0f;
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		SPScoreboardShipVM.GetTooltip = null;
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		UpdateTeamShips(removeOld: true, addNew: true, sort: true);
	}

	private void UpdateTeamShips(bool removeOld, bool addNew, bool sort)
	{
		if (removeOld)
		{
			ShipAssignment shipAssignment;
			for (int num = base.Attackers.Ships.Count - 1; num >= 0; num--)
			{
				if (!_navalShipsLogic.FindAssignmentOfShipOrigin(base.Attackers.Ships[num].Ship, out shipAssignment))
				{
					base.Attackers.Ships.RemoveAt(num);
				}
			}
			for (int num2 = base.Defenders.Ships.Count - 1; num2 >= 0; num2--)
			{
				if (!_navalShipsLogic.FindAssignmentOfShipOrigin(base.Defenders.Ships[num2].Ship, out shipAssignment))
				{
					base.Defenders.Ships.RemoveAt(num2);
				}
			}
		}
		if (addNew)
		{
			MBList<MissionShip> mBList = new MBList<MissionShip>();
			_navalShipsLogic.FillTeamShips(Mission.Current.AttackerTeam.TeamSide, mBList);
			MBList<MissionShip> mBList2 = new MBList<MissionShip>();
			if (Mission.Current.AttackerAllyTeam != null)
			{
				_navalShipsLogic.FillTeamShips(Mission.Current.AttackerAllyTeam.TeamSide, mBList2);
			}
			MBList<MissionShip> mBList3 = new MBList<MissionShip>();
			_navalShipsLogic.FillTeamShips(Mission.Current.DefenderTeam.TeamSide, mBList3);
			MBList<MissionShip> mBList4 = new MBList<MissionShip>();
			if (Mission.Current.DefenderAllyTeam != null)
			{
				_navalShipsLogic.FillTeamShips(Mission.Current.DefenderAllyTeam.TeamSide, mBList4);
			}
			for (int i = 0; i < mBList.Count; i++)
			{
				MissionShip missionShip = mBList[i];
				base.Attackers.GetShipAddIfNotExists(missionShip.ShipOrigin, missionShip.ShipOrigin.Hull.Type.ToString(), (missionShip.ShipOrigin as Ship).Owner, Mission.Current.AttackerTeam.TeamSide, (int)missionShip.FormationIndex);
			}
			for (int j = 0; j < mBList2.Count; j++)
			{
				MissionShip missionShip2 = mBList2[j];
				base.Attackers.GetShipAddIfNotExists(missionShip2.ShipOrigin, missionShip2.ShipOrigin.Hull.Type.ToString(), (missionShip2.ShipOrigin as Ship).Owner, Mission.Current.AttackerAllyTeam.TeamSide, (int)missionShip2.FormationIndex);
			}
			for (int k = 0; k < mBList3.Count; k++)
			{
				MissionShip missionShip3 = mBList3[k];
				base.Defenders.GetShipAddIfNotExists(missionShip3.ShipOrigin, missionShip3.ShipOrigin.Hull.Type.ToString(), (missionShip3.ShipOrigin as Ship).Owner, Mission.Current.DefenderTeam.TeamSide, (int)missionShip3.FormationIndex);
			}
			for (int l = 0; l < mBList4.Count; l++)
			{
				MissionShip missionShip4 = mBList4[l];
				base.Defenders.GetShipAddIfNotExists(missionShip4.ShipOrigin, missionShip4.ShipOrigin.Hull.Type.ToString(), (missionShip4.ShipOrigin as Ship).Owner, Mission.Current.DefenderAllyTeam.TeamSide, (int)missionShip4.FormationIndex);
			}
		}
		if (sort)
		{
			base.Attackers.Ships.Sort(_scoreboardShipComparer);
			base.Defenders.Ships.Sort(_scoreboardShipComparer);
		}
	}

	private List<TooltipProperty> GetShipTooltip(SPScoreboardShipVM shipVM)
	{
		IShipOrigin ship = shipVM.Ship;
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty(ship.Name.ToString(), string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		if (shipVM.IsRetreated)
		{
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=w6Wa3lSL}Retreated").ToString(), -1));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		}
		else if (shipVM.IsDestroyed)
		{
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=w8Yzf0F0}Destroyed").ToString(), -1));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		}
		if (shipVM.Owner != null)
		{
			list.Add(new TooltipProperty(GameTexts.FindText("str_owner").ToString(), shipVM.Owner.Name.ToString(), 0));
		}
		list.Add(new TooltipProperty(new TextObject("{=wEmx6fZi}Hull").ToString(), ship.Hull.Name.ToString(), 0));
		list.Add(new TooltipProperty(new TextObject("{=sqdzHOPe}Class").ToString(), GameTexts.FindText("str_ship_type", ship.Hull.Type.ToString().ToLowerInvariant()).ToString(), 0));
		string value = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", (int)ship.HitPoints).SetTextVariable("RIGHT", (int)ship.MaxHitPoints)
			.ToString();
		list.Add(new TooltipProperty(new TextObject("{=oBbiVeKE}Hit Points").ToString(), value, 0));
		if (_navalShipsLogic != null && _navalShipsLogic.FindAssignmentOfShipOrigin(ship, out var shipAssignment) && shipAssignment.MissionShip != null)
		{
			string value2 = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", shipAssignment.MissionShip.Formation?.CountOfUnits ?? 0).SetTextVariable("RIGHT", shipAssignment.MissionShip.CrewSizeOnMainDeck)
				.ToString();
			list.Add(new TooltipProperty(new TextObject("{=aClquusd}Troop Count").ToString(), value2, 0));
		}
		List<ShipSlotAndPieceName> shipSlotAndPieceNames = ship.GetShipSlotAndPieceNames();
		if (shipSlotAndPieceNames.Count > 0)
		{
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator)
			{
				OnlyShowWhenExtended = true
			});
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=zMvUzdKR}Ship Upgrades").ToString(), -1)
			{
				OnlyShowWhenExtended = true
			});
			foreach (ShipSlotAndPieceName item in shipSlotAndPieceNames)
			{
				list.Add(new TooltipProperty(item.SlotName, item.PieceName, 0)
				{
					OnlyShowWhenExtended = true
				});
			}
		}
		if (shipSlotAndPieceNames.Count > 0)
		{
			if (Input.IsGamepadActive)
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", "MapFollowModifier").ToString());
			}
			else
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString());
			}
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0)
			{
				OnlyShowWhenNotExtended = true
			});
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info").ToString(), -1)
			{
				OnlyShowWhenNotExtended = true
			});
		}
		return list;
	}
}

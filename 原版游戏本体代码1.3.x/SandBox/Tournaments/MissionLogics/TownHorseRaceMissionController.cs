using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Tournaments.AgentControllers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics
{
	// Token: 0x02000030 RID: 48
	public class TownHorseRaceMissionController : MissionLogic, ITournamentGameBehavior
	{
		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060001BD RID: 445 RVA: 0x0000B750 File Offset: 0x00009950
		// (set) Token: 0x060001BE RID: 446 RVA: 0x0000B758 File Offset: 0x00009958
		public List<TownHorseRaceMissionController.CheckPoint> CheckPoints { get; private set; }

		// Token: 0x060001BF RID: 447 RVA: 0x0000B761 File Offset: 0x00009961
		public TownHorseRaceMissionController(CultureObject culture)
		{
			this._culture = culture;
			this._agents = new List<TownHorseRaceAgentController>();
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x0000B77C File Offset: 0x0000997C
		public override void AfterStart()
		{
			base.AfterStart();
			this.CollectCheckPointsAndStartPoints();
			foreach (TownHorseRaceAgentController townHorseRaceAgentController in this._agents)
			{
				townHorseRaceAgentController.DisableMovement();
			}
			this._startTimer = new BasicMissionTimer();
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x0000B7E4 File Offset: 0x000099E4
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (this._startTimer != null && this._startTimer.ElapsedTime > 3f)
			{
				foreach (TownHorseRaceAgentController townHorseRaceAgentController in this._agents)
				{
					townHorseRaceAgentController.Start();
				}
			}
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0000B858 File Offset: 0x00009A58
		private void CollectCheckPointsAndStartPoints()
		{
			this.CheckPoints = new List<TownHorseRaceMissionController.CheckPoint>();
			foreach (WeakGameEntity weakGameEntity in from amo in base.Mission.ActiveMissionObjects
				select amo.GameEntity)
			{
				VolumeBox firstScriptOfType = weakGameEntity.GetFirstScriptOfType<VolumeBox>();
				if (firstScriptOfType != null)
				{
					this.CheckPoints.Add(new TownHorseRaceMissionController.CheckPoint(firstScriptOfType));
				}
			}
			this.CheckPoints = (from x in this.CheckPoints
				orderby x.Name
				select x).ToList<TownHorseRaceMissionController.CheckPoint>();
			this._startPoints = base.Mission.Scene.FindEntitiesWithTag("sp_horse_race").ToList<GameEntity>();
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x0000B944 File Offset: 0x00009B44
		private MatrixFrame GetStartFrame(int index)
		{
			MatrixFrame result;
			if (index < this._startPoints.Count)
			{
				result = this._startPoints[index].GetGlobalFrame();
			}
			else
			{
				result = ((this._startPoints.Count > 0) ? this._startPoints[0].GetGlobalFrame() : MatrixFrame.Identity);
			}
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			return result;
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x0000B9A8 File Offset: 0x00009BA8
		private void SetItemsAndSpawnCharacter(CharacterObject troop)
		{
			int count = this._agents.Count;
			Equipment equipment = new Equipment();
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("charger"), null, null, false));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("horse_harness_e"), null, null, false));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("horse_whip"), null, null, false));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("short_padded_robe"), null, null, false));
			MatrixFrame startFrame = this.GetStartFrame(count);
			AgentBuildData agentBuildData = new AgentBuildData(troop).Team(this._teams[count]).InitialPosition(startFrame.origin);
			Vec2 vec = startFrame.rotation.f.AsVec2;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).Equipment(equipment).Controller((troop == CharacterObject.PlayerCharacter) ? AgentControllerType.Player : AgentControllerType.AI);
			Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
			agent.Health = (float)agent.Monster.HitPoints;
			agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			this._agents.Add(this.AddHorseRaceAgentController(agent));
			if (troop == CharacterObject.PlayerCharacter)
			{
				base.Mission.PlayerTeam = this._teams[count];
			}
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0000BB19 File Offset: 0x00009D19
		private TownHorseRaceAgentController AddHorseRaceAgentController(Agent agent)
		{
			return agent.AddController(typeof(TownHorseRaceAgentController)) as TownHorseRaceAgentController;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0000BB30 File Offset: 0x00009D30
		private void InitializeTeams(int count)
		{
			this._teams = new List<Team>();
			for (int i = 0; i < count; i++)
			{
				this._teams.Add(base.Mission.Teams.Add(BattleSideEnum.None, uint.MaxValue, uint.MaxValue, null, true, false, true));
			}
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x0000BB76 File Offset: 0x00009D76
		public void StartMatch(TournamentMatch match, bool isLastRound)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x0000BB7D File Offset: 0x00009D7D
		public void SkipMatch(TournamentMatch match)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x0000BB84 File Offset: 0x00009D84
		public bool IsMatchEnded()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060001CA RID: 458 RVA: 0x0000BB8B File Offset: 0x00009D8B
		public void OnMatchEnded()
		{
			throw new NotImplementedException();
		}

		// Token: 0x0400009E RID: 158
		public const int TourCount = 2;

		// Token: 0x040000A0 RID: 160
		private readonly List<TownHorseRaceAgentController> _agents;

		// Token: 0x040000A1 RID: 161
		private List<Team> _teams;

		// Token: 0x040000A2 RID: 162
		private List<GameEntity> _startPoints;

		// Token: 0x040000A3 RID: 163
		private BasicMissionTimer _startTimer;

		// Token: 0x040000A4 RID: 164
		private CultureObject _culture;

		// Token: 0x0200013F RID: 319
		public class CheckPoint
		{
			// Token: 0x1700011C RID: 284
			// (get) Token: 0x06000DDF RID: 3551 RVA: 0x00063DC0 File Offset: 0x00061FC0
			public string Name
			{
				get
				{
					return this._volumeBox.GameEntity.Name;
				}
			}

			// Token: 0x06000DE0 RID: 3552 RVA: 0x00063DE0 File Offset: 0x00061FE0
			public CheckPoint(VolumeBox volumeBox)
			{
				this._volumeBox = volumeBox;
				this._bestTargetList = GameEntity.CreateFromWeakEntity(this._volumeBox.GameEntity).CollectChildrenEntitiesWithTag("best_target_point");
				this._volumeBox.SetIsOccupiedDelegate(new VolumeBox.VolumeBoxDelegate(this.OnAgentsEnterCheckBox));
			}

			// Token: 0x06000DE1 RID: 3553 RVA: 0x00063E34 File Offset: 0x00062034
			public Vec3 GetBestTargetPosition()
			{
				Vec3 origin;
				if (this._bestTargetList.Count > 0)
				{
					origin = this._bestTargetList[MBRandom.RandomInt(this._bestTargetList.Count)].GetGlobalFrame().origin;
				}
				else
				{
					origin = this._volumeBox.GameEntity.GetGlobalFrame().origin;
				}
				return origin;
			}

			// Token: 0x06000DE2 RID: 3554 RVA: 0x00063E91 File Offset: 0x00062091
			public void AddToCheckList(Agent agent)
			{
				this._volumeBox.AddToCheckList(agent);
			}

			// Token: 0x06000DE3 RID: 3555 RVA: 0x00063E9F File Offset: 0x0006209F
			public void RemoveFromCheckList(Agent agent)
			{
				this._volumeBox.RemoveFromCheckList(agent);
			}

			// Token: 0x06000DE4 RID: 3556 RVA: 0x00063EB0 File Offset: 0x000620B0
			private void OnAgentsEnterCheckBox(VolumeBox volumeBox, List<Agent> agentsInVolume)
			{
				foreach (Agent agent in agentsInVolume)
				{
					agent.GetController<TownHorseRaceAgentController>().OnEnterCheckPoint(volumeBox);
				}
			}

			// Token: 0x04000650 RID: 1616
			private readonly VolumeBox _volumeBox;

			// Token: 0x04000651 RID: 1617
			private readonly List<GameEntity> _bestTargetList;
		}
	}
}

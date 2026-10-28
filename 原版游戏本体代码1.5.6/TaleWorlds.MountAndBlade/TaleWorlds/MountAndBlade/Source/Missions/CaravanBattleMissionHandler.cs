using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class CaravanBattleMissionHandler : MissionLogic
{
	private const float CaravanDeploymentOffset = 2f;

	private GameEntity _entity;

	private bool _isCamelCulture;

	private bool _isCaravan;

	private Team _caravanTeam;

	private BattleSideEnum _playerSide;

	private IBattleCombatant _caravanCombatant;

	private readonly string[] _camelLoadHarnesses = new string[2] { "camel_saddle_a", "camel_saddle_b" };

	private readonly string[] _camelMountableHarnesses = new string[1] { "camel_saddle" };

	private readonly string[] _muleLoadHarnesses = new string[3] { "mule_load_a", "mule_load_b", "mule_load_c" };

	private readonly string[] _muleMountableHarnesses = new string[3] { "aseran_village_harness", "steppe_fur_harness", "steppe_harness" };

	private const string CaravanPrefabName = "caravan_scattered_goods_prop";

	private const string VillagerGoodsPrefabName = "villager_scattered_goods_prop";

	public CaravanBattleMissionHandler(IBattleCombatant caravanCombatant, bool isCamelCulture, bool isCaravan, BattleSideEnum playerSide)
	{
		_isCaravan = isCaravan;
		_isCamelCulture = isCamelCulture;
		_caravanCombatant = caravanCombatant;
		_playerSide = playerSide;
	}

	public override void OnBattleSideSpawned(BattleSideEnum side)
	{
		if (side != _caravanCombatant.Side)
		{
			return;
		}
		IMissionDeploymentPlan deploymentPlan = base.Mission.DeploymentPlan;
		MatrixFrame formationsCenterFrameAndExtents;
		Vec2 halfExtents;
		if (side != _playerSide)
		{
			_caravanTeam = Mission.GetTeam(TeamSideEnum.EnemyTeam);
			formationsCenterFrameAndExtents = deploymentPlan.GetFormationsCenterFrameAndExtents(_caravanTeam, out halfExtents, ignoreDimensionlessFormations: false);
		}
		else
		{
			base.Mission.GetMissionBehavior<MissionCombatantsLogic>().SupportsAllyTeamOnPlayerSide(out var allyCombatant);
			bool flag = allyCombatant != null && allyCombatant == _caravanCombatant;
			_caravanTeam = Mission.GetTeam(flag ? TeamSideEnum.PlayerAllyTeam : TeamSideEnum.PlayerTeam);
			formationsCenterFrameAndExtents = deploymentPlan.GetFormationsCenterFrameAndExtents(_caravanTeam, out halfExtents, ignoreDimensionlessFormations: false);
		}
		MatrixFrame frame = formationsCenterFrameAndExtents;
		frame.Advance(0f - (halfExtents.y + 2f));
		frame.origin.z = base.Mission.Scene.GetTerrainHeight(frame.origin.AsVec2);
		_entity = GameEntity.Instantiate(Mission.Current.Scene, _isCaravan ? "caravan_scattered_goods_prop" : "villager_scattered_goods_prop", frame);
		_entity.SetMobility(GameEntity.Mobility.Dynamic);
		foreach (GameEntity child in _entity.GetChildren())
		{
			base.Mission.Scene.GetTerrainHeightAndNormal(child.GlobalPosition.AsVec2, out var height, out var normal);
			MatrixFrame frame2 = child.GetGlobalFrame();
			frame2.origin.z = height;
			frame2.rotation.u = normal;
			frame2.rotation.Orthonormalize();
			child.SetGlobalFrame(in frame2);
		}
		IEnumerable<GameEntity> enumerable = from c in _entity.GetChildren()
			where c.HasTag("caravan_animal_spawn")
			select c;
		int num = (int)((float)enumerable.Count() * 0.4f);
		foreach (GameEntity item in enumerable)
		{
			MatrixFrame globalFrame = item.GetGlobalFrame();
			string objectName;
			if (_isCamelCulture)
			{
				if (num > 0)
				{
					int num2 = MBRandom.RandomInt(_camelMountableHarnesses.Length);
					objectName = _camelMountableHarnesses[num2];
				}
				else
				{
					int num3 = MBRandom.RandomInt(_camelLoadHarnesses.Length);
					objectName = _camelLoadHarnesses[num3];
				}
			}
			else if (num > 0)
			{
				int num4 = MBRandom.RandomInt(_muleMountableHarnesses.Length);
				objectName = _muleMountableHarnesses[num4];
			}
			else
			{
				int num5 = MBRandom.RandomInt(_muleLoadHarnesses.Length);
				objectName = _muleLoadHarnesses[num5];
			}
			ItemRosterElement harnessRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>(objectName));
			ItemRosterElement rosterElement = (_isCamelCulture ? ((num-- > 0) ? new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("pack_camel")) : new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("pack_camel_unmountable"))) : ((num-- > 0) ? new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("mule")) : new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("mule_unmountable"))));
			Agent agent = Mission.Current.SpawnMonster(rosterElement, harnessRosterElement, in globalFrame.origin, globalFrame.rotation.f.AsVec2.Normalized());
			agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFDFFFFu));
			_entity.GetFirstScriptInFamilyDescending<TacticalPosition>().UpdateLinkedTacticalPositions();
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		TacticalPosition firstScriptInFamilyDescending = _entity.GetFirstScriptInFamilyDescending<TacticalPosition>();
		if (firstScriptInFamilyDescending == null)
		{
			return;
		}
		foreach (Team team in Mission.Current.Teams)
		{
			team.TeamAI.TacticalPositions.Add(firstScriptInFamilyDescending);
			if (team == _caravanTeam && (!team.IsPlayerTeam || !team.IsPlayerGeneral))
			{
				team.TeamAI.ResetTactic(keepCurrentTactic: false);
			}
		}
	}
}

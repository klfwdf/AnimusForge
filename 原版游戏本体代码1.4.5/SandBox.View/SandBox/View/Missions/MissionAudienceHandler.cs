using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionAudienceHandler : MissionView
{
	private const int GapBetweenCheerSmallInSeconds = 10;

	private const int GapBetweenCheerMedium = 4;

	private float _minChance;

	private float _maxChance;

	private float _minDist;

	private float _maxDist;

	private float _minHeight;

	private float _maxHeight;

	private List<GameEntity> _audienceMidPoints;

	private List<KeyValuePair<GameEntity, float>> _audienceList;

	private readonly float _density;

	private GameEntity _arenaSoundEntity;

	private SoundEvent _ambientSoundEvent;

	private MissionTime _lastOneShotSoundEventStarted;

	private bool _allOneShotSoundEventsAreDisabled;

	public MissionAudienceHandler(float density)
	{
		_density = density;
	}

	public override void EarlyStart()
	{
		_allOneShotSoundEventsAreDisabled = true;
		_audienceMidPoints = base.Mission.Scene.FindEntitiesWithTag("audience_mid_point").ToList();
		_arenaSoundEntity = base.Mission.Scene.FindEntityWithTag("arena_sound");
		_audienceList = new List<KeyValuePair<GameEntity, float>>();
		if (_audienceMidPoints.Count > 0)
		{
			OnInit();
		}
	}

	public void OnInit()
	{
		_minChance = MathF.Max(_density - 0.5f, 0f);
		_maxChance = _density;
		GetAudienceEntities();
		SpawnAudienceAgents();
		_lastOneShotSoundEventStarted = MissionTime.Zero;
		_allOneShotSoundEventsAreDisabled = false;
		_ambientSoundEvent = SoundManager.CreateEvent("event:/mission/ambient/detail/arena/arena", base.Mission.Scene);
		_ambientSoundEvent.Play();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectorAgent != null && affectorAgent.IsHuman && affectedAgent.IsHuman)
		{
			Cheer();
		}
	}

	private void Cheer(bool onEnd = false)
	{
		if (!_allOneShotSoundEventsAreDisabled)
		{
			string text = null;
			if (onEnd)
			{
				text = "event:/mission/ambient/detail/arena/cheer_big";
				_allOneShotSoundEventsAreDisabled = true;
			}
			else if (_lastOneShotSoundEventStarted.ElapsedSeconds > 4f && _lastOneShotSoundEventStarted.ElapsedSeconds < 10f)
			{
				text = "event:/mission/ambient/detail/arena/cheer_medium";
			}
			else if (_lastOneShotSoundEventStarted.ElapsedSeconds > 10f)
			{
				text = "event:/mission/ambient/detail/arena/cheer_small";
			}
			if (text != null)
			{
				SoundManager.StartOneShotEvent(text, (_arenaSoundEntity != null) ? _arenaSoundEntity.GlobalPosition : (_audienceMidPoints.Any() ? _audienceMidPoints.GetRandomElement().GlobalPosition : Vec3.Zero));
				_lastOneShotSoundEventStarted = MissionTime.Now;
			}
		}
	}

	private void GetAudienceEntities()
	{
		_maxDist = 0f;
		_minDist = float.MaxValue;
		_maxHeight = 0f;
		_minHeight = float.MaxValue;
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("audience"))
		{
			float distanceSquareToArena = GetDistanceSquareToArena(item);
			_maxDist = ((distanceSquareToArena > _maxDist) ? distanceSquareToArena : _maxDist);
			_minDist = ((distanceSquareToArena < _minDist) ? distanceSquareToArena : _minDist);
			float z = item.GetFrame().origin.z;
			_maxHeight = ((z > _maxHeight) ? z : _maxHeight);
			_minHeight = ((z < _minHeight) ? z : _minHeight);
			_audienceList.Add(new KeyValuePair<GameEntity, float>(item, distanceSquareToArena));
			item.SetVisibilityExcludeParents(visible: false);
		}
	}

	private float GetDistanceSquareToArena(GameEntity audienceEntity)
	{
		float num = float.MaxValue;
		foreach (GameEntity audienceMidPoint in _audienceMidPoints)
		{
			float num2 = audienceMidPoint.GlobalPosition.DistanceSquared(audienceEntity.GlobalPosition);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	private CharacterObject GetRandomAudienceCharacterToSpawn()
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		CharacterObject characterObject = MBRandom.ChooseWeighted(new List<(CharacterObject, float)>
		{
			(currentSettlement.Culture.Townswoman, 0.2f),
			(currentSettlement.Culture.Townsman, 0.2f),
			(currentSettlement.Culture.Armorer, 0.1f),
			(currentSettlement.Culture.Merchant, 0.1f),
			(currentSettlement.Culture.Musician, 0.1f),
			(currentSettlement.Culture.Weaponsmith, 0.1f),
			(currentSettlement.Culture.RansomBroker, 0.1f),
			(currentSettlement.Culture.Barber, 0.05f),
			(currentSettlement.Culture.FemaleDancer, 0.05f)
		});
		if (characterObject == null)
		{
			characterObject = ((MBRandom.RandomFloat < 0.65f) ? currentSettlement.Culture.Townsman : currentSettlement.Culture.Townswoman);
		}
		return characterObject;
	}

	private void SpawnAudienceAgents()
	{
		for (int num = _audienceList.Count - 1; num >= 0; num--)
		{
			KeyValuePair<GameEntity, float> keyValuePair = _audienceList[num];
			float num2 = _minChance + (1f - (keyValuePair.Value - _minDist) / (_maxDist - _minDist)) * (_maxChance - _minChance);
			float num3 = _minChance + (1f - MathF.Pow((keyValuePair.Key.GetFrame().origin.z - _minHeight) / (_maxHeight - _minHeight), 2f)) * (_maxChance - _minChance);
			float num4 = num2 * 0.4f + num3 * 0.6f;
			if (MBRandom.RandomFloat < num4)
			{
				MatrixFrame globalFrame = keyValuePair.Key.GetGlobalFrame();
				CharacterObject randomAudienceCharacterToSpawn = GetRandomAudienceCharacterToSpawn();
				AgentBuildData agentBuildData = new AgentBuildData(randomAudienceCharacterToSpawn).InitialPosition(in globalFrame.origin).InitialDirection(new Vec2(0f - globalFrame.rotation.f.AsVec2.x, 0f - globalFrame.rotation.f.AsVec2.y)).TroopOrigin(new SimpleAgentOrigin(randomAudienceCharacterToSpawn))
					.Team(Team.Invalid)
					.ClothingColor1(Settlement.CurrentSettlement.MapFaction.Color)
					.ClothingColor2(Settlement.CurrentSettlement.MapFaction.Color2)
					.CanSpawnOutsideOfMissionBoundary(canSpawn: true);
				Agent agent = Mission.Current.SpawnAgent(agentBuildData);
				AnimationSystemData animationSystemData = MonsterExtensions.FillAnimationSystemData(actionSet: MBActionSet.GetActionSetWithIndex(0), monster: agentBuildData.AgentMonster, stepSize: randomAudienceCharacterToSpawn.GetStepSize(), hasClippingPlane: false);
				agent.SetActionSet(ref animationSystemData);
				MBAnimation.PrefetchAnimationClip(agent.ActionSet, ActionIndexCache.act_arena_spectator);
				agent.SetActionChannel(0, in ActionIndexCache.act_arena_spectator, ignorePriority: true, (AnimFlags)0uL, 0f, MBRandom.RandomFloatRanged(0.75f, 1f), -0.2f, 0.4f, MBRandom.RandomFloatRanged(0.01f, 1f));
				agent.Controller = AgentControllerType.None;
				agent.ToggleInvulnerable();
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (_audienceMidPoints != null && base.Mission.MissionEnded)
		{
			Cheer(onEnd: true);
		}
	}

	public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
	{
		if (oldMissionMode == MissionMode.Battle && Mission.Current.Mode == MissionMode.StartUp && Agent.Main != null && Agent.Main.IsActive())
		{
			Cheer(onEnd: true);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		_ambientSoundEvent?.Release();
	}
}

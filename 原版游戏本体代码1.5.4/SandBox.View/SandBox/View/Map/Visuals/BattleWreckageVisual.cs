using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;

namespace SandBox.View.Map.Visuals;

public class BattleWreckageVisual : MapEntityVisual<BattleWreckage>
{
	private const float FadeSpeed = 1.5f;

	private const int BannerColorIndex = 99;

	private const string LandWreckagePrefabName = "wreckage_prefab";

	private const string NavalWreckagePrefabName = "naval_wreckage_prefab";

	private const string bannerMeshName = "vlandia_tier_1_banner";

	private const string BattleRemainsTag = "battle_remains";

	private const string FlagTag = "flag";

	private const string LandSoundPath = "event:/map/ambient/node/wreckage/wreckage_land";

	private const string NavalSoundPath = "event:/map/ambient/node/wreckage/wreckage_sea";

	private float _entityAlpha;

	private bool _lastKnownVisibility;

	private bool _isInvestigated;

	private bool _isLandBattleWreckage;

	private GameEntity _wreckageEntity;

	private ClothSimulatorComponent _bannerClothSimulator;

	private readonly List<AgentVisuals> _agentVisualList = new List<AgentVisuals>();

	private readonly List<ActionIndexCache> _landActionList = new List<ActionIndexCache>
	{
		ActionIndexCache.act_wreckage_death_01,
		ActionIndexCache.act_wreckage_death_02
	};

	private readonly List<ActionIndexCache> _navalActionList = new List<ActionIndexCache>
	{
		ActionIndexCache.act_death_swim_1,
		ActionIndexCache.act_death_swim_2
	};

	private Scene _mapScene;

	private SoundEvent _ambientSound;

	private Scene MapScene
	{
		get
		{
			if (_mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
			{
				_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			}
			return _mapScene;
		}
	}

	public override CampaignVec2 InteractionPositionForPlayer => base.MapEntity.Position;

	public override MapEntityVisual AttachedTo => null;

	public GameEntity Entity { get; private set; }

	public bool IsFading { get; private set; }

	public float WreckageTypeCoefficient { get; private set; }

	public BattleWreckageVisual(BattleWreckage entity)
		: base(entity)
	{
	}

	public override Vec3 GetVisualPosition()
	{
		return base.MapEntity.Position.AsVec3();
	}

	public override bool IsVisibleOrFadingOut()
	{
		return _entityAlpha > 0f;
	}

	public override void OnHover()
	{
		InformationManager.ShowTooltip(typeof(BattleWreckage), base.MapEntity);
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		MobileParty.MainParty.SetMoveGoToInteractablePoint(base.MapEntity, MobileParty.NavigationType.Default);
		return true;
	}

	public override void OnOpenEncyclopedia()
	{
	}

	public void OnStartup()
	{
		switch (base.MapEntity.WreckageTypeCategory)
		{
		case BattleWreckage.WreckageType.Small:
			WreckageTypeCoefficient = 1.25f;
			break;
		case BattleWreckage.WreckageType.Normal:
			WreckageTypeCoefficient = 1.5f;
			break;
		case BattleWreckage.WreckageType.Epic:
			WreckageTypeCoefficient = 2f;
			break;
		}
		_isLandBattleWreckage = base.MapEntity.Position.IsOnLand;
		Entity = GetNewGameEntity();
		SetInitialPosition();
		Entity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
		_entityAlpha = 0f;
		if (base.MapEntity.IsVisible)
		{
			_entityAlpha = 1f;
		}
		MapScreen.VisualsOfEntities.Add(Entity.Pointer, this);
		string eventId = (_isLandBattleWreckage ? "event:/map/ambient/node/wreckage/wreckage_land" : "event:/map/ambient/node/wreckage/wreckage_sea");
		_ambientSound = SoundEvent.CreateEventFromString(eventId, MapScene);
		_ambientSound.PlayInPosition(base.MapEntity.Position.AsVec3());
		if (!base.MapEntity.IsVisible)
		{
			_ambientSound.Pause();
		}
	}

	public void OnRemoved()
	{
		MapScreen.VisualsOfEntities.Remove(Entity.Pointer);
		if (_agentVisualList.Count != 0)
		{
			foreach (AgentVisuals agentVisual in _agentVisualList)
			{
				agentVisual.Reset();
			}
			_agentVisualList.Clear();
		}
		_bannerClothSimulator = null;
		if (_wreckageEntity != null)
		{
			_wreckageEntity.ClearComponents();
		}
		Entity?.RemoveAllChildren();
		Entity?.Remove(111);
		Entity = null;
		_ambientSound?.Release();
		_ambientSound = null;
	}

	internal bool HasVisibilityChanged()
	{
		if (_lastKnownVisibility != base.MapEntity.IsVisible)
		{
			_lastKnownVisibility = base.MapEntity.IsVisible;
			IsFading = true;
			return true;
		}
		return false;
	}

	internal void OnVisibilityChanged()
	{
		SoundEvent ambientSound = _ambientSound;
		if (ambientSound != null && ambientSound.IsValid)
		{
			if (base.MapEntity.IsVisible)
			{
				_ambientSound.Resume();
			}
			else
			{
				_ambientSound.Pause();
			}
		}
	}

	internal void TickFadingState(float realDt)
	{
		if (base.MapEntity.IsVisible)
		{
			_entityAlpha = TaleWorlds.Library.MathF.Min(_entityAlpha + realDt * 1.5f, 1f);
			Entity.SetAlpha(_entityAlpha);
			if (!_isInvestigated)
			{
				foreach (AgentVisuals agentVisual in _agentVisualList)
				{
					WeakGameEntity weakEntity = agentVisual.GetWeakEntity();
					if (weakEntity != WeakGameEntity.Invalid)
					{
						weakEntity.SetAlpha(_entityAlpha);
					}
				}
			}
			if (!(_entityAlpha >= 1f))
			{
				return;
			}
			Entity.EntityFlags &= ~EntityFlags.DoNotTick;
			Entity.SetVisibilityExcludeParents(visible: true);
			IsFading = false;
			if (_isInvestigated)
			{
				return;
			}
			{
				foreach (AgentVisuals agentVisual2 in _agentVisualList)
				{
					WeakGameEntity weakEntity2 = agentVisual2.GetWeakEntity();
					if (weakEntity2 != WeakGameEntity.Invalid)
					{
						weakEntity2.SetVisibilityExcludeParents(visible: true);
					}
				}
				return;
			}
		}
		_entityAlpha = TaleWorlds.Library.MathF.Max(_entityAlpha - realDt * 1.5f, 0f);
		Entity.SetAlpha(_entityAlpha);
		if (!_isInvestigated)
		{
			foreach (AgentVisuals agentVisual3 in _agentVisualList)
			{
				WeakGameEntity weakEntity3 = agentVisual3.GetWeakEntity();
				if (weakEntity3 != WeakGameEntity.Invalid)
				{
					weakEntity3.SetAlpha(_entityAlpha);
				}
			}
		}
		if (!(_entityAlpha <= 0f))
		{
			return;
		}
		Entity.SetVisibilityExcludeParents(visible: false);
		Entity.EntityFlags |= EntityFlags.DoNotTick;
		IsFading = false;
		if (_isInvestigated)
		{
			return;
		}
		foreach (AgentVisuals agentVisual4 in _agentVisualList)
		{
			WeakGameEntity weakEntity4 = agentVisual4.GetWeakEntity();
			if (weakEntity4 != WeakGameEntity.Invalid)
			{
				weakEntity4.SetVisibilityExcludeParents(visible: false);
			}
		}
	}

	internal void Tick(float dt, float realDt)
	{
		if (IsVisibleOrFadingOut())
		{
			if (_wreckageEntity == null)
			{
				AddWreckageVisual();
			}
			_bannerClothSimulator?.SetForcedWind(Vec3.Side, isLocal: false);
			RefreshWreckageVisual();
		}
	}

	private GameEntity GetNewGameEntity()
	{
		GameEntity gameEntity = GameEntity.CreateEmpty(MapScene);
		gameEntity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 1f * WreckageTypeCoefficient, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
		return gameEntity;
	}

	private void AddWreckageVisual()
	{
		string prefabName = (_isLandBattleWreckage ? "wreckage_prefab" : "naval_wreckage_prefab");
		_wreckageEntity = GameEntity.Instantiate(MapScene, prefabName, callScriptCallbacks: true);
		Entity.AddChild(_wreckageEntity);
		_isInvestigated = base.MapEntity.IsInvestigated;
		if (_isInvestigated)
		{
			foreach (GameEntity child in _wreckageEntity.GetChildren())
			{
				if (!child.HasTag("battle_remains"))
				{
					child.SetVisibilityExcludeParents(visible: false);
				}
			}
			return;
		}
		_wreckageEntity.GetFirstChildEntityWithTag("battle_remains")?.SetVisibilityExcludeParents(visible: false);
		if (_isLandBattleWreckage)
		{
			AddFlagVisual();
		}
		AddAgentVisuals();
	}

	private void AddFlagVisual()
	{
		GameEntity firstChildEntityWithTagRecursive = _wreckageEntity.GetFirstChildEntityWithTagRecursive("flag");
		MetaMesh banner = SandBoxViewHelpers.BannerVisualHelper.GetBanner(new Banner(Banner.CreateOneColoredEmptyBanner(99).BannerCode), "vlandia_tier_1_banner");
		int componentCount = firstChildEntityWithTagRecursive.GetComponentCount(GameEntity.ComponentType.ClothSimulator);
		firstChildEntityWithTagRecursive.AddMultiMesh(banner);
		if (firstChildEntityWithTagRecursive.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > componentCount)
		{
			_bannerClothSimulator = (ClothSimulatorComponent)firstChildEntityWithTagRecursive.GetComponentAtIndex(componentCount, GameEntity.ComponentType.ClothSimulator);
		}
	}

	private void RefreshWreckageVisual()
	{
		if (!base.MapEntity.IsInvestigated || _isInvestigated || !(_wreckageEntity != null))
		{
			return;
		}
		foreach (GameEntity child in _wreckageEntity.GetChildren())
		{
			if (child.HasTag("battle_remains"))
			{
				child.SetVisibilityExcludeParents(visible: true);
			}
			else
			{
				child.SetVisibilityExcludeParents(visible: false);
			}
		}
		foreach (AgentVisuals agentVisual in _agentVisualList)
		{
			agentVisual.SetVisible(value: false);
		}
		_isInvestigated = base.MapEntity.IsInvestigated;
	}

	private void AddAgentVisuals()
	{
		CampaignVec2 campaignPosition = new CampaignVec2(Vec2.Zero, _isLandBattleWreckage);
		MBList<TroopRosterElement> totalDiedInBattle = base.MapEntity.GetTotalDiedInBattle();
		totalDiedInBattle.AddRange(base.MapEntity.GetTotalWoundedInBattle());
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		_wreckageEntity.WeakEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			bool flag = true;
			bool flag2 = false;
			MatrixFrame frame = item.GetFrame();
			switch (base.MapEntity.WreckageTypeCategory)
			{
			case BattleWreckage.WreckageType.Small:
				if (item.HasTag("normal") || item.HasTag("epic"))
				{
					item.Remove(111);
					flag = false;
				}
				break;
			case BattleWreckage.WreckageType.Normal:
				if (item.HasTag("epic"))
				{
					item.Remove(111);
					flag = false;
				}
				break;
			}
			if (!flag)
			{
				continue;
			}
			if (item.HasTag("spawn_point"))
			{
				flag2 = true;
			}
			bool isValid;
			MatrixFrame spawnFrame = GetSpawnFrame(frame, campaignPosition, item.HasTag("horse"), out isValid);
			if (isValid)
			{
				if (!flag2)
				{
					continue;
				}
				AgentVisuals agentVisuals;
				if (_isLandBattleWreckage)
				{
					agentVisuals = ((!item.HasTag("horse")) ? CreateHumanAgentVisual(spawnFrame, totalDiedInBattle.IsEmpty() ? CharacterObject.FindFirst((CharacterObject x) => !x.IsHero) : totalDiedInBattle.GetRandomElementWithPredicate((TroopRosterElement r) => !r.Character.IsHero).Character) : CreateMountAgentVisual(spawnFrame));
				}
				else
				{
					spawnFrame.origin = new Vec3(0f, 0f, 0f, 1f);
					agentVisuals = CreateHumanAgentVisual(spawnFrame, totalDiedInBattle.IsEmpty() ? CharacterObject.FindFirst((CharacterObject x) => !x.IsHero) : totalDiedInBattle.GetRandomElementWithPredicate((TroopRosterElement r) => !r.Character.IsHero).Character);
					item.AddChild(agentVisuals.GetWeakEntity());
				}
				agentVisuals.GetWeakEntity().SetVisibilityExcludeParents(base.MapEntity.IsVisible);
				_agentVisualList.Add(agentVisuals);
			}
			else if (!flag2)
			{
				item.SetVisibilityExcludeParents(visible: false);
			}
		}
	}

	private MatrixFrame GetSpawnFrame(MatrixFrame frame, CampaignVec2 campaignPosition, bool isHorseEntity, out bool isValid)
	{
		MatrixFrame identity = MatrixFrame.Identity;
		identity.rotation = frame.rotation;
		identity.origin = frame.origin + Entity.GlobalPosition;
		campaignPosition.AddVec2(identity.origin.AsVec2);
		identity.origin.z = campaignPosition.AsVec3().z;
		if (isHorseEntity)
		{
			identity.origin -= identity.rotation.s / 2f;
		}
		isValid = campaignPosition.IsValid();
		campaignPosition.AddVec2(-identity.origin.AsVec2);
		return identity;
	}

	private AgentVisuals CreateHumanAgentVisual(MatrixFrame frame, CharacterObject character)
	{
		Equipment equipment = character.Equipment.Clone();
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
		MBActionSet actionSet = MBGlobals.GetActionSet("as_human_warrior");
		float scale = (_isLandBattleWreckage ? 0.3f : 0.15f);
		frame.Rotate(MBRandom.RandomFloatRanged(System.MathF.PI), in Vec3.Up);
		AgentVisuals agentVisuals = AgentVisuals.Create(new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(equipment).BodyProperties(character.GetBodyProperties(character.Equipment))
			.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Scale(scale)
			.Frame(frame)
			.ActionSet(actionSet)
			.Scene(MapScene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(prepareImmediately: false)
			.HasClippingPlane(hasClippingPlane: true)
			.UseScaledWeapons(useScaledWeapons: true)
			.ClothColor1(4291609515u)
			.ClothColor2(4291609515u)
			.CharacterObjectStringId(character.StringId)
			.Race(character.Race), "BattleWreckageVisual " + character.Name, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		if (agentVisuals != null)
		{
			List<ActionIndexCache> e = (_isLandBattleWreckage ? _landActionList : _navalActionList);
			WeakGameEntity weakEntity = agentVisuals.GetWeakEntity();
			float speed = TaleWorlds.Library.MathF.Min(0.25f, 20f);
			weakEntity.Skeleton.SetAgentActionChannel(0, e.GetRandomElement(), MBRandom.NondeterministicRandomFloat * 0.7f);
			agentVisuals.Tick(null, 0.0001f, isEntityMoving: false, speed);
			weakEntity.Skeleton.ForceUpdateBoneFrames();
		}
		return agentVisuals;
	}

	private AgentVisuals CreateMountAgentVisual(MatrixFrame frame)
	{
		ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse");
		Monster monster = itemObject.HorseComponent.Monster;
		MBActionSet actionSet = MBGlobals.GetActionSet("as_horse");
		Equipment equipment = new Equipment();
		equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject);
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("light_harness");
		equipment[EquipmentIndex.HorseHarness] = new EquipmentElement(item);
		AgentVisuals agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(equipment).Scale(itemObject.ScaleFactor * 0.3f).Frame(frame)
			.ActionSet(actionSet)
			.Scene(MapScene)
			.Monster(monster)
			.PrepareImmediately(prepareImmediately: false)
			.UseScaledWeapons(useScaledWeapons: true)
			.HasClippingPlane(hasClippingPlane: true)
			.MountCreationKey(MountCreationKey.GetRandomMountKeyString(itemObject, MBRandom.NondeterministicRandomInt)), "BattleWreckageVisual mount", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		WeakGameEntity weakEntity = agentVisuals.GetWeakEntity();
		weakEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_horse_fall_right_continue);
		weakEntity.Skeleton.ForceUpdateBoneFrames();
		return agentVisuals;
	}

	private void SetInitialPosition()
	{
		MatrixFrame frame = CalculateFrame();
		Entity.SetFrame(ref frame);
	}

	private MatrixFrame CalculateFrame()
	{
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = GetVisualPosition();
		return identity;
	}
}

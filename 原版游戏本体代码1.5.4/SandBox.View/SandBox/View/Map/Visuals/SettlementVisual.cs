using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Helpers;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map.Visuals;

public class SettlementVisual : MapEntityVisual<PartyBase>
{
	private struct SiegeBombardmentData
	{
		public Vec3 LaunchGlobalPosition;

		public Vec3 TargetPosition;

		public MatrixFrame ShooterGlobalFrame;

		public MatrixFrame TargetAlignedShooterGlobalFrame;

		public float MissileSpeed;

		public float Gravity;

		public float LaunchAngle;

		public float RotationDuration;

		public float ReloadDuration;

		public float AimingDuration;

		public float MissileLaunchDuration;

		public float FireDuration;

		public float FlightDuration;

		public float TotalDuration;
	}

	private const string CircleTag = "map_settlement_circle";

	private const string BannerPlaceHolderTag = "map_banner_placeholder";

	private const string MapSiegeEngineTag = "map_siege_engine";

	private const string MapBreachableWallTag = "map_breachable_wall";

	private const string MapDefenderEngineTag = "map_defensive_engine";

	private const string MapSiegeEngineRamTag = "map_siege_ram";

	private const string TownPhysicalTag = "bo_town";

	private const string MapSiegeEngineTowerTag = "map_siege_tower";

	private const string MapPreparationTag = "siege_preparation";

	private const string BurnedTag = "looted";

	private GameEntity[] _attackerRangedEngineSpawnEntities;

	private GameEntity[] _attackerBatteringRamSpawnEntities;

	private GameEntity[] _defenderBreachableWallEntitiesCacheForCurrentLevel;

	private GameEntity[] _attackerSiegeTowerSpawnEntities;

	private GameEntity[] _defenderRangedEngineSpawnEntitiesForAllLevels;

	private GameEntity[] _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel;

	private GameEntity[] _defenderBreachableWallEntitiesForAllLevels;

	private readonly List<(GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity)> _siegeRangedMachineEntities;

	private readonly List<(GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity)> _siegeMeleeMachineEntities;

	private readonly List<(GameEntity, BattleSideEnum, int)> _siegeMissileEntities;

	private Dictionary<int, List<GameEntity>> _gateBannerEntitiesWithLevels;

	private uint _currentLevelMask;

	private MatrixFrame _hoveredSiegeEntityFrame = MatrixFrame.Identity;

	private GameEntity.UpgradeLevelMask _currentSettlementUpgradeLevelMask;

	private Scene _mapScene;

	private List<GameEntity> TownPhysicalEntities { get; set; }

	public override MapEntityVisual AttachedTo => null;

	public override CampaignVec2 InteractionPositionForPlayer => ((IInteractablePoint)base.MapEntity).GetInteractionPosition(MobileParty.MainParty);

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

	public GameEntity StrategicEntity { get; private set; }

	public SettlementVisual(PartyBase entity)
		: base(entity)
	{
		_siegeRangedMachineEntities = new List<(GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity)>();
		_siegeMeleeMachineEntities = new List<(GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity)>();
		_siegeMissileEntities = new List<(GameEntity, BattleSideEnum, int)>();
		CircleLocalFrame = MatrixFrame.Identity;
	}

	public override bool IsEnemyOf(IFaction faction)
	{
		return FactionManager.IsAtWarAgainstFaction(base.MapEntity.MapFaction, faction.MapFaction);
	}

	public override bool IsInSameFaction(IFaction faction)
	{
		return DiplomacyHelper.IsSameFactionAndNotEliminated(base.MapEntity.MapFaction, faction.MapFaction);
	}

	public override bool IsAllyOf(IFaction faction)
	{
		return DiplomacyHelper.HasAllianceWithFaction(base.MapEntity.MapFaction, faction.MapFaction);
	}

	internal void OnPartyRemoved()
	{
		if (!(StrategicEntity != null))
		{
			return;
		}
		MapScreen.VisualsOfEntities.Remove(StrategicEntity.Pointer);
		foreach (GameEntity child in StrategicEntity.GetChildren())
		{
			MapScreen.VisualsOfEntities.Remove(child.Pointer);
		}
		ReleaseResources();
		StrategicEntity.Remove(111);
	}

	public override Vec3 GetVisualPosition()
	{
		return base.MapEntity.Position.AsVec3();
	}

	public override bool IsVisibleOrFadingOut()
	{
		return base.MapEntity.IsVisible;
	}

	public override void OnHover()
	{
		if (base.MapEntity.MapEvent != null)
		{
			InformationManager.ShowTooltip(typeof(MapEvent), base.MapEntity.MapEvent);
		}
		else if (base.MapEntity.IsSettlement && base.MapEntity.IsVisible)
		{
			if (base.MapEntity.Settlement.SiegeEvent != null)
			{
				InformationManager.ShowTooltip(typeof(SiegeEvent), base.MapEntity.Settlement.SiegeEvent);
			}
			else
			{
				InformationManager.ShowTooltip(typeof(Settlement), base.MapEntity.Settlement);
			}
		}
	}

	public override void OnTrackAction()
	{
		Settlement settlement = base.MapEntity.Settlement;
		if (settlement != null)
		{
			if (Campaign.Current.VisualTrackerManager.CheckTracked(settlement))
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(settlement);
			}
			else
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(settlement);
			}
		}
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		if (followModifierUsed)
		{
			if (Campaign.Current.Models.EncounterModel.CanMainHeroDoParleyWithParty(base.MapEntity, out var explanation))
			{
				base.MapScreen.BeginParleyWith(base.MapEntity);
			}
			else if (!TextObject.IsNullOrEmpty(explanation))
			{
				MBInformationManager.AddQuickInformation(explanation);
			}
		}
		else if (base.MapEntity.IsVisible)
		{
			NavigationHelper.GetInteractionDataForMainParty(base.MapEntity.Settlement, out var canNavigate, out var bestNavigationType, out var isTargetingPort);
			if (canNavigate)
			{
				MobileParty.MainParty.SetMoveGoToSettlement(base.MapEntity.Settlement, bestNavigationType, isTargetingPort);
			}
		}
		return true;
	}

	public override void OnOpenEncyclopedia()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(base.MapEntity.Settlement.EncyclopediaLink);
	}

	public override void ReleaseResources()
	{
		RemoveSiege();
		ResetPartyIcon();
	}

	private void ResetPartyIcon()
	{
		if (StrategicEntity != null)
		{
			if ((StrategicEntity.EntityFlags & EntityFlags.Ignore) != 0)
			{
				StrategicEntity.RemoveFromPredisplayEntity();
			}
			StrategicEntity.ClearComponents();
		}
	}

	internal void ValidateIsDirty()
	{
		RefreshPartyIcon();
		if (base.MapEntity.IsVisible)
		{
			StrategicEntity.SetVisibilityExcludeParents(visible: true);
			StrategicEntity.SetAlpha(1f);
			StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
		}
		else
		{
			StrategicEntity.SetAlpha(0f);
			StrategicEntity.SetVisibilityExcludeParents(visible: false);
			StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
		}
	}

	internal Dictionary<int, List<GameEntity>> GetGateBannerEntitiesWithLevels()
	{
		return _gateBannerEntitiesWithLevels;
	}

	public Vec3 GetBannerPositionForParty(MobileParty mobileParty)
	{
		if (mobileParty.CurrentSettlement == base.MapEntity.Settlement && base.MapEntity.Settlement.IsFortification && _gateBannerEntitiesWithLevels != null && !_gateBannerEntitiesWithLevels.IsEmpty())
		{
			int wallLevel = base.MapEntity.Settlement.Town.GetWallLevel();
			int count = _gateBannerEntitiesWithLevels[wallLevel].Count;
			if (_gateBannerEntitiesWithLevels[wallLevel].Count > 0)
			{
				int num = 0;
				foreach (MobileParty party in base.MapEntity.Settlement.Parties)
				{
					if (party == mobileParty)
					{
						break;
					}
					if (party.LeaderHero?.ClanBanner != null)
					{
						num++;
					}
				}
				GameEntity gameEntity = _gateBannerEntitiesWithLevels[wallLevel][num % count];
				GameEntity child = gameEntity.GetChild(0);
				MatrixFrame matrixFrame = ((child != null) ? child.GetGlobalFrame() : gameEntity.GetGlobalFrame());
				num /= count;
				int num2 = base.MapEntity.Settlement.Parties.Count((MobileParty p) => p.LeaderHero?.ClanBanner != null);
				float num3 = 0.75f / (float)TaleWorlds.Library.MathF.Max(1, num2 / (count * 2));
				int num4 = ((num % 2 != 0) ? 1 : (-1));
				Vec3 vec = matrixFrame.rotation.f / 2f * num4;
				if (vec.Length < matrixFrame.rotation.s.Length)
				{
					vec = matrixFrame.rotation.s / 2f * num4;
				}
				return matrixFrame.origin + vec * ((num + 1) / 2) * (num % 2 * 2 - 1) * num3 * num4;
			}
			Debug.FailedAssert($"{base.MapEntity.Settlement.Name} - has no Banner Entities at level {wallLevel}.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Visuals\\SettlementVisual.cs", "GetBannerPositionForParty", 304);
		}
		return Vec3.Invalid;
	}

	internal void OnMapHoverSiegeEngineEnd()
	{
		_hoveredSiegeEntityFrame = MatrixFrame.Identity;
		MBInformationManager.HideInformations();
	}

	private void RefreshPartyIcon()
	{
		if (!base.MapEntity.IsVisualDirty)
		{
			return;
		}
		base.MapEntity.OnVisualsUpdated();
		RemoveSiege();
		StrategicEntity.RemoveAllParticleSystems();
		StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
		if (base.MapEntity.Settlement.IsFortification)
		{
			UpdateDefenderSiegeEntitiesCache();
		}
		AddSiegeIconComponents(base.MapEntity);
		SetSettlementLevelVisibility();
		RefreshWallState();
		RefreshTownPhysicalEntitiesState(base.MapEntity);
		RefreshSiegePreparations(base.MapEntity);
		bool flag = false;
		if (base.MapEntity.Settlement.IsVillage)
		{
			MapEvent mapEvent = base.MapEntity.MapEvent;
			if (mapEvent != null && mapEvent.IsRaid)
			{
				StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
				StrategicEntity.AddParticleSystemComponent("psys_fire_smoke_env_point");
				if ((StrategicEntity.EntityFlags & EntityFlags.Ignore) != 0)
				{
					StrategicEntity.RemoveFromPredisplayEntity();
				}
				flag = true;
			}
			else if (base.MapEntity.Settlement.IsRaided)
			{
				StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
				StrategicEntity.AddParticleSystemComponent("map_icon_village_plunder_fx");
				if ((StrategicEntity.EntityFlags & EntityFlags.Ignore) != 0)
				{
					StrategicEntity.RemoveFromPredisplayEntity();
				}
				flag = true;
			}
		}
		if (!flag && (StrategicEntity.EntityFlags & EntityFlags.Ignore) == 0)
		{
			StrategicEntity.SetAsPredisplayEntity();
		}
		StrategicEntity.CheckResources(addToQueue: true, checkFaceResources: false);
	}

	internal void OnStartup()
	{
		bool flag = false;
		StrategicEntity = MapScene.GetCampaignEntityWithName(base.MapEntity.Id);
		if (StrategicEntity == null)
		{
			Campaign.Current.MapSceneWrapper.AddNewEntityToMapScene(base.MapEntity.Settlement.StringId, base.MapEntity.Settlement.Position);
			StrategicEntity = MapScene.GetCampaignEntityWithName(base.MapEntity.Id);
		}
		bool flag2 = false;
		if (base.MapEntity.Settlement.IsFortification)
		{
			List<GameEntity> children = new List<GameEntity>();
			StrategicEntity.GetChildrenRecursive(ref children);
			PopulateSiegeEngineFrameListsFromChildren(children);
			UpdateDefenderSiegeEntitiesCache();
			TownPhysicalEntities = children.FindAll((GameEntity x) => x.HasTag("bo_town"));
			List<GameEntity> list = new List<GameEntity>();
			Dictionary<int, List<GameEntity>> dictionary = new Dictionary<int, List<GameEntity>>
			{
				{
					1,
					new List<GameEntity>()
				},
				{
					2,
					new List<GameEntity>()
				},
				{
					3,
					new List<GameEntity>()
				}
			};
			foreach (GameEntity item in children)
			{
				if (item.HasTag("main_map_city_gate"))
				{
					NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(item.GetGlobalFrame().origin.AsVec2, isOnLand: true), MobileParty.NavigationType.Default);
					flag2 = true;
					list.Add(item);
				}
				if (item.HasTag("map_settlement_circle"))
				{
					CircleLocalFrame = item.GetGlobalFrame();
					flag = true;
					item.SetVisibilityExcludeParents(visible: false);
					list.Add(item);
				}
				if (item.HasTag("map_banner_placeholder"))
				{
					int upgradeLevelOfEntity = item.Parent.GetUpgradeLevelOfEntity();
					if (upgradeLevelOfEntity == 0)
					{
						dictionary[1].Add(item);
						dictionary[2].Add(item);
						dictionary[3].Add(item);
					}
					else
					{
						dictionary[upgradeLevelOfEntity].Add(item);
					}
					list.Add(item);
				}
			}
			_gateBannerEntitiesWithLevels = dictionary;
			if (base.MapEntity.Settlement.IsFortification)
			{
				Campaign.Current.MapSceneWrapper.GetSiegeCampFrames(base.MapEntity.Settlement, out var siegeCamp1GlobalFrames, out var siegeCamp2GlobalFrames);
				base.MapEntity.Settlement.Town.BesiegerCampPositions1 = siegeCamp1GlobalFrames.ToArray();
				base.MapEntity.Settlement.Town.BesiegerCampPositions2 = siegeCamp2GlobalFrames.ToArray();
			}
			foreach (GameEntity item2 in list)
			{
				item2.Remove(112);
			}
			if (!flag2 && !base.MapEntity.Settlement.IsTown)
			{
				_ = base.MapEntity.Settlement.IsCastle;
			}
			bool flag3 = false;
			if (base.MapEntity.IsSettlement)
			{
				foreach (GameEntity child in StrategicEntity.GetChildren())
				{
					if (child.HasTag("main_map_city_port"))
					{
						NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(child.GetGlobalFrame().origin.AsVec2, isOnLand: false), MobileParty.NavigationType.Naval);
						flag3 = true;
					}
				}
				if ((flag3 || !base.MapEntity.Settlement.HasPort) && flag3)
				{
					_ = base.MapEntity.Settlement.HasPort;
				}
			}
		}
		if (!flag)
		{
			CircleLocalFrame = MatrixFrame.Identity;
			MatrixFrame circleLocalFrame = CircleLocalFrame;
			Mat3 rotation = circleLocalFrame.rotation;
			if (base.MapEntity.Settlement.IsVillage)
			{
				rotation.ApplyScaleLocal(1.75f);
			}
			else if (base.MapEntity.Settlement.IsTown)
			{
				rotation.ApplyScaleLocal(5.75f);
			}
			else if (base.MapEntity.Settlement.IsCastle)
			{
				rotation.ApplyScaleLocal(2.75f);
			}
			else
			{
				rotation.ApplyScaleLocal(1.75f);
			}
			circleLocalFrame.rotation = rotation;
			CircleLocalFrame = circleLocalFrame;
		}
		StrategicEntity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
		StrategicEntity.SetReadyToRender(ready: true);
		StrategicEntity.SetEntityEnvMapVisibility(value: false);
		List<GameEntity> children2 = new List<GameEntity>();
		StrategicEntity.GetChildrenRecursive(ref children2);
		if (!MapScreen.VisualsOfEntities.ContainsKey(StrategicEntity.Pointer))
		{
			MapScreen.VisualsOfEntities.Add(StrategicEntity.Pointer, this);
		}
		foreach (GameEntity item3 in children2)
		{
			if (!MapScreen.VisualsOfEntities.ContainsKey(item3.Pointer) && !MapScreen.FrameAndVisualOfEngines.ContainsKey(item3.Pointer))
			{
				MapScreen.VisualsOfEntities.Add(item3.Pointer, this);
			}
		}
		StrategicEntity.SetAsPredisplayEntity();
	}

	internal void Tick(float dt, ref int dirtyPartiesCount, ref SettlementVisual[] dirtyPartiesList)
	{
		if (StrategicEntity == null)
		{
			return;
		}
		if (base.MapEntity.IsVisualDirty)
		{
			int num = Interlocked.Increment(ref dirtyPartiesCount);
			dirtyPartiesList[num] = this;
		}
		else
		{
			double toHours = CampaignTime.Now.ToHours;
			foreach (var siegeMissileEntity in _siegeMissileEntities)
			{
				GameEntity item = siegeMissileEntity.Item1;
				ISiegeEventSide siegeEventSide = base.MapEntity.Settlement.SiegeEvent.GetSiegeEventSide(siegeMissileEntity.Item2);
				int item2 = siegeMissileEntity.Item3;
				bool flag = false;
				if (siegeEventSide.SiegeEngineMissiles.Count > item2)
				{
					SiegeEvent.SiegeEngineMissile siegeEngineMissile = siegeEventSide.SiegeEngineMissiles[item2];
					double toHours2 = siegeEngineMissile.CollisionTime.ToHours;
					CalculateDataAndDurationsForSiegeMachine(siegeEngineMissile.ShooterSlotIndex, siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide, siegeEngineMissile.TargetType, siegeEngineMissile.TargetSlotIndex, out var bombardmentData);
					float num2 = bombardmentData.MissileSpeed * TaleWorlds.Library.MathF.Cos(bombardmentData.LaunchAngle);
					if (toHours > toHours2 - (double)bombardmentData.TotalDuration)
					{
						bool flag2 = toHours - (double)dt > toHours2 - (double)bombardmentData.FlightDuration && toHours - (double)dt < toHours2;
						bool flag3 = toHours > toHours2 - (double)bombardmentData.FlightDuration && toHours < toHours2;
						if (flag3)
						{
							flag = true;
							float num3 = (float)(toHours - (toHours2 - (double)bombardmentData.FlightDuration));
							float num4 = bombardmentData.MissileSpeed * TaleWorlds.Library.MathF.Sin(bombardmentData.LaunchAngle);
							Vec2 vec = new Vec2(num2 * num3, num4 * num3 - bombardmentData.Gravity * 0.5f * num3 * num3);
							Vec3 o = bombardmentData.LaunchGlobalPosition + bombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.NormalizedCopy() * vec.x + bombardmentData.TargetAlignedShooterGlobalFrame.rotation.u.NormalizedCopy() * vec.y;
							float num5 = num3 + 0.1f;
							Vec2 vec2 = new Vec2(num2 * num5, num4 * num5 - bombardmentData.Gravity * 0.5f * num5 * num5);
							Vec3 vec3 = bombardmentData.LaunchGlobalPosition + bombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.NormalizedCopy() * vec2.x + bombardmentData.TargetAlignedShooterGlobalFrame.rotation.u.NormalizedCopy() * vec2.y;
							Mat3 rot = item.GetGlobalFrame().rotation;
							rot.f = vec3 - o;
							rot.Orthonormalize();
							rot.ApplyScaleLocal(base.MapScreen.PrefabEntityCache.GetScaleForSiegeEngine(siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide));
							item.SetGlobalFrame(new MatrixFrame(in rot, in o));
						}
						item.WeakEntity.GetChild(0).SetVisibilityExcludeParents(flag3);
						int soundCodeId = -1;
						if (!flag2 && flag3)
						{
							soundCodeId = ((siegeEngineMissile.ShooterSiegeEngineType != DefaultSiegeEngineTypes.Ballista && siegeEngineMissile.ShooterSiegeEngineType != DefaultSiegeEngineTypes.FireBallista) ? ((siegeEngineMissile.ShooterSiegeEngineType != DefaultSiegeEngineTypes.Catapult && siegeEngineMissile.ShooterSiegeEngineType != DefaultSiegeEngineTypes.FireCatapult && siegeEngineMissile.ShooterSiegeEngineType != DefaultSiegeEngineTypes.Onager && siegeEngineMissile.ShooterSiegeEngineType != DefaultSiegeEngineTypes.FireOnager) ? MiscSoundContainer.SoundCodeAmbientNodeSiegeTrebuchetFire : MiscSoundContainer.SoundCodeAmbientNodeSiegeMangonelFire) : MiscSoundContainer.SoundCodeAmbientNodeSiegeBallistaFire);
						}
						else if (flag2 && !flag3)
						{
							StrategicEntity.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName((siegeEngineMissile.TargetType == SiegeBombardTargets.RangedEngines) ? "psys_game_ballista_destruction" : "psys_campaign_boulder_stone_coll"), item.GetGlobalFrame());
							soundCodeId = ((siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.Ballista || siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.FireBallista) ? MiscSoundContainer.SoundCodeAmbientNodeSiegeBallistaHit : MiscSoundContainer.SoundCodeAmbientNodeSiegeBoulderHit);
						}
						MBSoundEvent.PlaySound(soundCodeId, item.GlobalPosition);
						if (!(toHours < toHours2 - (double)(bombardmentData.TotalDuration - bombardmentData.RotationDuration - bombardmentData.ReloadDuration)))
						{
							if (toHours < toHours2 - (double)(bombardmentData.TotalDuration - bombardmentData.RotationDuration - bombardmentData.ReloadDuration - bombardmentData.AimingDuration))
							{
								if (siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex] != null && siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex].SiegeEngine == siegeEngineMissile.ShooterSiegeEngineType)
								{
									foreach (var siegeRangedMachineEntity in _siegeRangedMachineEntities)
									{
										if (!flag && siegeRangedMachineEntity.Item2 == siegeEventSide.BattleSide && siegeRangedMachineEntity.Item3 == siegeEngineMissile.ShooterSlotIndex)
										{
											GameEntity item3 = siegeRangedMachineEntity.Item5;
											if (item3 != null)
											{
												flag = true;
												item.SetGlobalFrame(item3.GetGlobalFrame().TransformToParent(MBSkeletonExtensions.GetBoneEntitialFrame(item3.Skeleton, Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide))));
											}
										}
									}
								}
							}
							else if (toHours < toHours2 - (double)(bombardmentData.TotalDuration - bombardmentData.RotationDuration - bombardmentData.ReloadDuration - bombardmentData.AimingDuration - bombardmentData.FireDuration) && !flag3 && siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex] != null && siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex].SiegeEngine == siegeEngineMissile.ShooterSiegeEngineType)
							{
								foreach (var siegeRangedMachineEntity2 in _siegeRangedMachineEntities)
								{
									if (!flag && siegeRangedMachineEntity2.Item2 == siegeEventSide.BattleSide && siegeRangedMachineEntity2.Item3 == siegeEngineMissile.ShooterSlotIndex)
									{
										GameEntity item4 = siegeRangedMachineEntity2.Item5;
										if (item4 != null)
										{
											flag = true;
											item.SetGlobalFrame(item4.GetGlobalFrame().TransformToParent(MBSkeletonExtensions.GetBoneEntitialFrame(item4.Skeleton, Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide))));
										}
									}
								}
							}
						}
					}
				}
				item.SetVisibilityExcludeParents(flag);
			}
			foreach (var siegeRangedMachineEntity3 in _siegeRangedMachineEntities)
			{
				GameEntity item5 = siegeRangedMachineEntity3.Item1;
				BattleSideEnum item6 = siegeRangedMachineEntity3.Item2;
				int item7 = siegeRangedMachineEntity3.Item3;
				GameEntity item8 = siegeRangedMachineEntity3.Item5;
				SiegeEngineType siegeEngine = base.MapEntity.Settlement.SiegeEvent.GetSiegeEventSide(item6).SiegeEngines.DeployedRangedSiegeEngines[item7].SiegeEngine;
				if (!(item8 != null))
				{
					continue;
				}
				Skeleton skeleton = item8.Skeleton;
				string siegeEngineMapFireAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapFireAnimationName(siegeEngine, item6);
				string siegeEngineMapReloadAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapReloadAnimationName(siegeEngine, item6);
				SiegeEvent.RangedSiegeEngine rangedSiegeEngine = base.MapEntity.Settlement.SiegeEvent.GetSiegeEventSide(item6).SiegeEngines.DeployedRangedSiegeEngines[item7].RangedSiegeEngine;
				CalculateDataAndDurationsForSiegeMachine(item7, siegeEngine, item6, rangedSiegeEngine.CurrentTargetType, rangedSiegeEngine.CurrentTargetIndex, out var bombardmentData2);
				MatrixFrame frame = bombardmentData2.ShooterGlobalFrame;
				if (rangedSiegeEngine.PreviousTargetIndex >= 0)
				{
					Vec3 vec4 = ((rangedSiegeEngine.PreviousDamagedTargetType != SiegeBombardTargets.Wall) ? ((item6 == BattleSideEnum.Attacker) ? _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[rangedSiegeEngine.PreviousTargetIndex].GetGlobalFrame().origin : _attackerRangedEngineSpawnEntities[rangedSiegeEngine.PreviousTargetIndex].GetGlobalFrame().origin) : _defenderBreachableWallEntitiesCacheForCurrentLevel[rangedSiegeEngine.PreviousTargetIndex].GlobalPosition);
					frame.rotation.f.AsVec2 = (vec4 - frame.origin).AsVec2;
					frame.rotation.f.NormalizeWithoutChangingZ();
					frame.rotation.Orthonormalize();
				}
				item5.SetGlobalFrame(in frame);
				skeleton.TickAnimations(dt, MatrixFrame.Identity, tickAnimsForChildren: false);
				double toHours3 = rangedSiegeEngine.NextProjectileCollisionTime.ToHours;
				if (!(toHours > toHours3 - (double)bombardmentData2.TotalDuration))
				{
					continue;
				}
				if (toHours < toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration))
				{
					float rotationInRadians = (bombardmentData2.TargetPosition - frame.origin).AsVec2.RotationInRadians;
					float rotationInRadians2 = frame.rotation.f.AsVec2.RotationInRadians;
					float f = rotationInRadians - rotationInRadians2;
					float num6 = TaleWorlds.Library.MathF.Abs(f);
					float num7 = (float)(toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration) - toHours);
					if (num6 > num7 * 2f)
					{
						frame.rotation.f.AsVec2 = Vec2.FromRotation(rotationInRadians2 + (float)TaleWorlds.Library.MathF.Sign(f) * (num6 - num7 * 2f));
						frame.rotation.f.NormalizeWithoutChangingZ();
						frame.rotation.Orthonormalize();
						item5.SetGlobalFrame(in frame);
					}
				}
				else if (toHours < toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration - bombardmentData2.ReloadDuration))
				{
					item5.SetGlobalFrame(in bombardmentData2.TargetAlignedShooterGlobalFrame);
					skeleton.SetAnimationAtChannel(siegeEngineMapReloadAnimationName, 0, 1f, 0f, (float)((toHours - (toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration))) / (double)bombardmentData2.ReloadDuration));
				}
				else if (toHours < toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration - bombardmentData2.ReloadDuration - bombardmentData2.AimingDuration))
				{
					item5.SetGlobalFrame(in bombardmentData2.TargetAlignedShooterGlobalFrame);
					skeleton.SetAnimationAtChannel(siegeEngineMapReloadAnimationName, 0, 1f, 0f, 1f);
				}
				else if (toHours < toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration - bombardmentData2.ReloadDuration - bombardmentData2.AimingDuration - bombardmentData2.FireDuration))
				{
					item5.SetGlobalFrame(in bombardmentData2.TargetAlignedShooterGlobalFrame);
					skeleton.SetAnimationAtChannel(siegeEngineMapFireAnimationName, 0, 1f, 0f, (float)((toHours - (toHours3 - (double)(bombardmentData2.TotalDuration - bombardmentData2.RotationDuration - bombardmentData2.ReloadDuration - bombardmentData2.AimingDuration))) / (double)bombardmentData2.FireDuration));
				}
				else
				{
					item5.SetGlobalFrame(in bombardmentData2.TargetAlignedShooterGlobalFrame);
					skeleton.SetAnimationAtChannel(siegeEngineMapFireAnimationName, 0, 1f, 0f, 1f);
				}
			}
		}
		if (base.MapEntity.LevelMaskIsDirty)
		{
			RefreshLevelMask();
		}
	}

	internal void OnMapHoverSiegeEngine(MatrixFrame engineFrame)
	{
		if (PlayerSiege.PlayerSiegeEvent == null)
		{
			return;
		}
		for (int i = 0; i < _attackerBatteringRamSpawnEntities.Length; i++)
		{
			MatrixFrame m = _attackerBatteringRamSpawnEntities[i].GetGlobalFrame();
			if (m.NearlyEquals(engineFrame))
			{
				if (_hoveredSiegeEntityFrame != m)
				{
					SiegeEvent.SiegeEngineConstructionProgress engineInProgress = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[i];
					InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress));
				}
				return;
			}
		}
		for (int j = 0; j < _attackerSiegeTowerSpawnEntities.Length; j++)
		{
			MatrixFrame m2 = _attackerSiegeTowerSpawnEntities[j].GetGlobalFrame();
			if (m2.NearlyEquals(engineFrame))
			{
				if (_hoveredSiegeEntityFrame != m2)
				{
					SiegeEvent.SiegeEngineConstructionProgress engineInProgress2 = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[_attackerBatteringRamSpawnEntities.Length + j];
					InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress2));
				}
				return;
			}
		}
		for (int k = 0; k < _attackerRangedEngineSpawnEntities.Length; k++)
		{
			MatrixFrame m3 = _attackerRangedEngineSpawnEntities[k].GetGlobalFrame();
			if (m3.NearlyEquals(engineFrame))
			{
				if (_hoveredSiegeEntityFrame != m3)
				{
					SiegeEvent.SiegeEngineConstructionProgress engineInProgress3 = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines[k];
					InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress3));
				}
				return;
			}
		}
		for (int l = 0; l < _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length; l++)
		{
			MatrixFrame m4 = _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[l].GetGlobalFrame();
			if (m4.NearlyEquals(engineFrame))
			{
				if (_hoveredSiegeEntityFrame != m4)
				{
					SiegeEvent.SiegeEngineConstructionProgress engineInProgress4 = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines[l];
					InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress4));
				}
				return;
			}
		}
		for (int n = 0; n < _defenderBreachableWallEntitiesCacheForCurrentLevel.Length; n++)
		{
			MatrixFrame m5 = _defenderBreachableWallEntitiesCacheForCurrentLevel[n].GetGlobalFrame();
			if (m5.NearlyEquals(engineFrame))
			{
				if (_hoveredSiegeEntityFrame != m5 && base.MapEntity.IsSettlement)
				{
					InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetWallSectionTooltip(base.MapEntity.Settlement, n));
				}
				return;
			}
		}
		_hoveredSiegeEntityFrame = MatrixFrame.Identity;
	}

	private void RemoveSiege()
	{
		foreach (var siegeRangedMachineEntity in _siegeRangedMachineEntities)
		{
			StrategicEntity.RemoveChild(siegeRangedMachineEntity.Item1, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 36);
		}
		foreach (var siegeMissileEntity in _siegeMissileEntities)
		{
			StrategicEntity.RemoveChild(siegeMissileEntity.Item1, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 37);
		}
		foreach (var siegeMeleeMachineEntity in _siegeMeleeMachineEntities)
		{
			StrategicEntity.RemoveChild(siegeMeleeMachineEntity.Item1, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 38);
		}
		_siegeRangedMachineEntities.Clear();
		_siegeMeleeMachineEntities.Clear();
		_siegeMissileEntities.Clear();
	}

	private void AddSiegeIconComponents(PartyBase party)
	{
		if (!party.Settlement.IsUnderSiege)
		{
			return;
		}
		int wallLevel = -1;
		if (party.Settlement.SiegeEvent.BesiegedSettlement.IsTown || party.Settlement.SiegeEvent.BesiegedSettlement.IsCastle)
		{
			wallLevel = party.Settlement.SiegeEvent.BesiegedSettlement.Town.GetWallLevel();
		}
		SiegeEvent.SiegeEngineConstructionProgress[] deployedRangedSiegeEngines = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines;
		for (int i = 0; i < deployedRangedSiegeEngines.Length; i++)
		{
			SiegeEvent.SiegeEngineConstructionProgress obj = deployedRangedSiegeEngines[i];
			if (obj != null && obj.IsActive && i < _attackerRangedEngineSpawnEntities.Length)
			{
				MatrixFrame globalFrame = _attackerRangedEngineSpawnEntities[i].GetGlobalFrame();
				globalFrame.rotation.MakeUnit();
				AddSiegeMachine(deployedRangedSiegeEngines[i].SiegeEngine, globalFrame, BattleSideEnum.Attacker, wallLevel, i);
			}
		}
		SiegeEvent.SiegeEngineConstructionProgress[] deployedMeleeSiegeEngines = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines;
		for (int j = 0; j < deployedMeleeSiegeEngines.Length; j++)
		{
			SiegeEvent.SiegeEngineConstructionProgress obj2 = deployedMeleeSiegeEngines[j];
			if (obj2 == null || !obj2.IsActive)
			{
				continue;
			}
			if (deployedMeleeSiegeEngines[j].SiegeEngine == DefaultSiegeEngineTypes.SiegeTower)
			{
				int num = j - _attackerBatteringRamSpawnEntities.Length;
				if (num >= 0)
				{
					MatrixFrame globalFrame2 = _attackerSiegeTowerSpawnEntities[num].GetGlobalFrame();
					globalFrame2.rotation.MakeUnit();
					AddSiegeMachine(deployedMeleeSiegeEngines[j].SiegeEngine, globalFrame2, BattleSideEnum.Attacker, wallLevel, j);
				}
			}
			else if (deployedMeleeSiegeEngines[j].SiegeEngine == DefaultSiegeEngineTypes.Ram || deployedMeleeSiegeEngines[j].SiegeEngine == DefaultSiegeEngineTypes.ImprovedRam)
			{
				int num2 = j;
				if (num2 >= 0)
				{
					MatrixFrame globalFrame3 = _attackerBatteringRamSpawnEntities[num2].GetGlobalFrame();
					globalFrame3.rotation.MakeUnit();
					AddSiegeMachine(deployedMeleeSiegeEngines[j].SiegeEngine, globalFrame3, BattleSideEnum.Attacker, wallLevel, j);
				}
			}
		}
		SiegeEvent.SiegeEngineConstructionProgress[] deployedRangedSiegeEngines2 = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines;
		for (int k = 0; k < deployedRangedSiegeEngines2.Length; k++)
		{
			SiegeEvent.SiegeEngineConstructionProgress obj3 = deployedRangedSiegeEngines2[k];
			if (obj3 != null && obj3.IsActive)
			{
				MatrixFrame globalFrame4 = _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[k].GetGlobalFrame();
				globalFrame4.rotation.MakeUnit();
				AddSiegeMachine(deployedRangedSiegeEngines2[k].SiegeEngine, globalFrame4, BattleSideEnum.Defender, wallLevel, k);
			}
		}
		for (int l = 0; l < 2; l++)
		{
			BattleSideEnum side = ((l == 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			MBReadOnlyList<SiegeEvent.SiegeEngineMissile> siegeEngineMissiles = party.Settlement.SiegeEvent.GetSiegeEventSide(side).SiegeEngineMissiles;
			for (int m = 0; m < siegeEngineMissiles.Count; m++)
			{
				AddSiegeMissile(siegeEngineMissiles[m].ShooterSiegeEngineType, StrategicEntity.GetGlobalFrame(), side, m);
			}
		}
	}

	private void AddSiegeMachine(SiegeEngineType type, MatrixFrame globalFrame, BattleSideEnum side, int wallLevel, int slotIndex)
	{
		string siegeEngineMapPrefabName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapPrefabName(type, wallLevel, side);
		GameEntity gameEntity = GameEntity.Instantiate(MapScene, siegeEngineMapPrefabName, callScriptCallbacks: true);
		if (!(gameEntity != null))
		{
			return;
		}
		StrategicEntity.AddChild(gameEntity);
		gameEntity.GetLocalFrame(out var frame);
		gameEntity.SetGlobalFrame(globalFrame.TransformToParent(in frame));
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		gameEntity.WeakEntity.GetChildrenRecursive(ref children);
		GameEntity gameEntity2 = null;
		if (children.Any((WeakGameEntity entity) => entity.HasTag("siege_machine_mapicon_skeleton")))
		{
			WeakGameEntity weakEntity = children.Find((WeakGameEntity entity) => entity.HasTag("siege_machine_mapicon_skeleton"));
			if (weakEntity.Skeleton != null)
			{
				gameEntity2 = GameEntity.CreateFromWeakEntity(weakEntity);
				string siegeEngineMapFireAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapFireAnimationName(type, side);
				gameEntity2.Skeleton.SetAnimationAtChannel(siegeEngineMapFireAnimationName, 0, 1f, 0f, 1f);
			}
		}
		if (type.IsRanged)
		{
			_siegeRangedMachineEntities.Add(ValueTuple.Create(gameEntity, side, slotIndex, globalFrame, gameEntity2));
		}
		else
		{
			_siegeMeleeMachineEntities.Add(ValueTuple.Create(gameEntity, side, slotIndex, globalFrame, gameEntity2));
		}
	}

	private void AddSiegeMissile(SiegeEngineType type, MatrixFrame globalFrame, BattleSideEnum side, int missileIndex)
	{
		string siegeEngineMapProjectilePrefabName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapProjectilePrefabName(type);
		GameEntity gameEntity = GameEntity.Instantiate(MapScene, siegeEngineMapProjectilePrefabName, callScriptCallbacks: true);
		if (gameEntity != null)
		{
			_siegeMissileEntities.Add(ValueTuple.Create(gameEntity, side, missileIndex));
			StrategicEntity.AddChild(gameEntity);
			StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
			gameEntity.GetLocalFrame(out var frame);
			gameEntity.SetGlobalFrame(globalFrame.TransformToParent(in frame));
			gameEntity.SetVisibilityExcludeParents(visible: false);
		}
	}

	private void SetLevelMask(uint newMask)
	{
		_currentLevelMask = newMask;
		base.MapEntity.SetVisualAsDirty();
	}

	private void RefreshLevelMask()
	{
		uint num = 0u;
		if (base.MapEntity.Settlement.IsVillage)
		{
			num = ((base.MapEntity.Settlement.Village.VillageState != Village.VillageStates.Looted) ? (num | Campaign.Current.MapSceneWrapper.GetSceneLevel("civilian")) : (num | Campaign.Current.MapSceneWrapper.GetSceneLevel("looted")));
			num |= GetLevelOfProduction(base.MapEntity.Settlement);
		}
		else if (base.MapEntity.Settlement.IsTown || base.MapEntity.Settlement.IsCastle)
		{
			if (base.MapEntity.Settlement.Town.GetWallLevel() == 1)
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_1");
			}
			else if (base.MapEntity.Settlement.Town.GetWallLevel() == 2)
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_2");
			}
			else if (base.MapEntity.Settlement.Town.GetWallLevel() == 3)
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_3");
			}
			num = ((base.MapEntity.Settlement.SiegeEvent == null) ? (num | Campaign.Current.MapSceneWrapper.GetSceneLevel("civilian")) : (num | Campaign.Current.MapSceneWrapper.GetSceneLevel("siege")));
		}
		else if (base.MapEntity.Settlement.IsHideout)
		{
			num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_1");
		}
		if (_currentLevelMask != num)
		{
			SetLevelMask(num);
		}
		base.MapEntity.OnLevelMaskUpdated();
	}

	private static uint GetLevelOfProduction(Settlement settlement)
	{
		uint num = 0u;
		if (settlement.Village.Hearth < 200f)
		{
			return num | Campaign.Current.MapSceneWrapper.GetSceneLevel("level_1");
		}
		if (settlement.Village.Hearth < 600f)
		{
			return num | Campaign.Current.MapSceneWrapper.GetSceneLevel("level_2");
		}
		return num | Campaign.Current.MapSceneWrapper.GetSceneLevel("level_3");
	}

	private void SetSettlementLevelVisibility()
	{
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		StrategicEntity.WeakEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			if (((uint)item.GetUpgradeLevelMask() & _currentLevelMask) == _currentLevelMask)
			{
				item.SetVisibilityExcludeParents(visible: true);
				item.SetPhysicsState(isEnabled: true, setChildren: true);
			}
			else
			{
				item.SetVisibilityExcludeParents(visible: false);
				item.SetPhysicsState(isEnabled: false, setChildren: true);
			}
		}
	}

	private void PopulateSiegeEngineFrameListsFromChildren(List<GameEntity> children)
	{
		_attackerRangedEngineSpawnEntities = (from e in children.FindAll((GameEntity x) => x.Tags.Any((string t) => t.Contains("map_siege_engine")))
			orderby e.Tags.First((string s) => s.Contains("map_siege_engine"))
			select e).ToArray();
		GameEntity[] attackerRangedEngineSpawnEntities = _attackerRangedEngineSpawnEntities;
		foreach (GameEntity gameEntity in attackerRangedEngineSpawnEntities)
		{
			if (gameEntity.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity.GetChild(0).Pointer))
			{
				MapScreen.FrameAndVisualOfEngines.Add(gameEntity.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity.GetGlobalFrame(), this));
			}
		}
		_defenderRangedEngineSpawnEntitiesForAllLevels = (from e in children.FindAll((GameEntity x) => x.Tags.Any((string t) => t.Contains("map_defensive_engine")))
			orderby e.Tags.First((string s) => s.Contains("map_defensive_engine"))
			select e).ToArray();
		attackerRangedEngineSpawnEntities = _defenderRangedEngineSpawnEntitiesForAllLevels;
		foreach (GameEntity gameEntity2 in attackerRangedEngineSpawnEntities)
		{
			if (gameEntity2.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity2.GetChild(0).Pointer))
			{
				MapScreen.FrameAndVisualOfEngines.Add(gameEntity2.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity2.GetGlobalFrame(), this));
			}
		}
		_attackerBatteringRamSpawnEntities = children.FindAll((GameEntity x) => x.HasTag("map_siege_ram")).ToArray();
		attackerRangedEngineSpawnEntities = _attackerBatteringRamSpawnEntities;
		foreach (GameEntity gameEntity3 in attackerRangedEngineSpawnEntities)
		{
			if (gameEntity3.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity3.GetChild(0).Pointer))
			{
				MapScreen.FrameAndVisualOfEngines.Add(gameEntity3.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity3.GetGlobalFrame(), this));
			}
		}
		_attackerSiegeTowerSpawnEntities = children.FindAll((GameEntity x) => x.HasTag("map_siege_tower")).ToArray();
		attackerRangedEngineSpawnEntities = _attackerSiegeTowerSpawnEntities;
		foreach (GameEntity gameEntity4 in attackerRangedEngineSpawnEntities)
		{
			if (gameEntity4.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity4.GetChild(0).Pointer))
			{
				MapScreen.FrameAndVisualOfEngines.Add(gameEntity4.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity4.GetGlobalFrame(), this));
			}
		}
		_defenderBreachableWallEntitiesForAllLevels = children.FindAll((GameEntity x) => x.HasTag("map_breachable_wall")).ToArray();
		attackerRangedEngineSpawnEntities = _defenderBreachableWallEntitiesForAllLevels;
		foreach (GameEntity gameEntity5 in attackerRangedEngineSpawnEntities)
		{
			if (gameEntity5.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity5.GetChild(0).Pointer))
			{
				MapScreen.FrameAndVisualOfEngines.Add(gameEntity5.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity5.GetGlobalFrame(), this));
			}
		}
	}

	private void UpdateDefenderSiegeEntitiesCache()
	{
		GameEntity.UpgradeLevelMask currentSettlementUpgradeLevelMask = GameEntity.UpgradeLevelMask.None;
		if (base.MapEntity.IsSettlement && base.MapEntity.Settlement.IsFortification)
		{
			if (base.MapEntity.Settlement.Town.GetWallLevel() == 1)
			{
				currentSettlementUpgradeLevelMask = GameEntity.UpgradeLevelMask.Level1;
			}
			else if (base.MapEntity.Settlement.Town.GetWallLevel() == 2)
			{
				currentSettlementUpgradeLevelMask = GameEntity.UpgradeLevelMask.Level2;
			}
			else if (base.MapEntity.Settlement.Town.GetWallLevel() == 3)
			{
				currentSettlementUpgradeLevelMask = GameEntity.UpgradeLevelMask.Level3;
			}
		}
		_currentSettlementUpgradeLevelMask = currentSettlementUpgradeLevelMask;
		_defenderRangedEngineSpawnEntitiesCacheForCurrentLevel = _defenderRangedEngineSpawnEntitiesForAllLevels.Where((GameEntity e) => (e.GetUpgradeLevelMask() & _currentSettlementUpgradeLevelMask) == _currentSettlementUpgradeLevelMask).ToArray();
		_defenderBreachableWallEntitiesCacheForCurrentLevel = _defenderBreachableWallEntitiesForAllLevels.Where((GameEntity e) => (e.GetUpgradeLevelMask() & _currentSettlementUpgradeLevelMask) == _currentSettlementUpgradeLevelMask).ToArray();
	}

	private void RefreshWallState()
	{
		if (_defenderBreachableWallEntitiesForAllLevels == null)
		{
			return;
		}
		MBReadOnlyList<float> mBReadOnlyList = ((base.MapEntity?.Settlement != null && (base.MapEntity.Settlement == null || base.MapEntity.Settlement.IsFortification)) ? base.MapEntity.Settlement.SettlementWallSectionHitPointsRatioList : null);
		if (mBReadOnlyList == null)
		{
			return;
		}
		if (mBReadOnlyList.Count == 0)
		{
			Debug.FailedAssert("Town (" + base.MapEntity.Settlement.Name.ToString() + ") doesn't have wall entities defined for it's current level(" + base.MapEntity.Settlement.Town.GetWallLevel() + ")", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Visuals\\SettlementVisual.cs", "RefreshWallState", 1301);
			return;
		}
		for (int i = 0; i < _defenderBreachableWallEntitiesForAllLevels.Length; i++)
		{
			bool flag = mBReadOnlyList[i % mBReadOnlyList.Count] <= 0f;
			foreach (WeakGameEntity child in _defenderBreachableWallEntitiesForAllLevels[i].WeakEntity.GetChildren())
			{
				if (child.HasTag("map_solid_wall"))
				{
					child.SetVisibilityExcludeParents(!flag);
				}
				else if (child.HasTag("map_broken_wall"))
				{
					child.SetVisibilityExcludeParents(flag);
				}
			}
		}
	}

	private void RefreshTownPhysicalEntitiesState(PartyBase party)
	{
		if (party?.Settlement == null || !party.Settlement.IsFortification || TownPhysicalEntities == null)
		{
			return;
		}
		if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSiegeEvent.BesiegedSettlement == party.Settlement)
		{
			TownPhysicalEntities.ForEach(delegate(GameEntity p)
			{
				p.AddBodyFlags(BodyFlags.Disabled);
			});
		}
		else
		{
			TownPhysicalEntities.ForEach(delegate(GameEntity p)
			{
				p.RemoveBodyFlags(BodyFlags.Disabled);
			});
		}
	}

	private void RefreshSiegePreparations(PartyBase party)
	{
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		StrategicEntity.WeakEntity.GetChildrenRecursive(ref children);
		List<WeakGameEntity> list = children.FindAll((WeakGameEntity x) => x.HasTag("siege_preparation"));
		bool flag = false;
		if (party.Settlement != null && party.Settlement.IsUnderSiege)
		{
			SiegeEvent.SiegeEngineConstructionProgress siegePreparations = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.SiegePreparations;
			if (siegePreparations != null && siegePreparations.Progress >= 1f)
			{
				flag = true;
				foreach (WeakGameEntity item in list)
				{
					item.SetVisibilityExcludeParents(visible: true);
				}
			}
		}
		if (flag)
		{
			return;
		}
		foreach (WeakGameEntity item2 in list)
		{
			item2.SetVisibilityExcludeParents(visible: false);
		}
	}

	public MatrixFrame[] GetAttackerTowerSiegeEngineFrames()
	{
		MatrixFrame[] array = new MatrixFrame[_attackerSiegeTowerSpawnEntities.Length];
		for (int i = 0; i < _attackerSiegeTowerSpawnEntities.Length; i++)
		{
			array[i] = _attackerSiegeTowerSpawnEntities[i].GetGlobalFrame();
		}
		return array;
	}

	public MatrixFrame[] GetAttackerBatteringRamSiegeEngineFrames()
	{
		MatrixFrame[] array = new MatrixFrame[_attackerBatteringRamSpawnEntities.Length];
		for (int i = 0; i < _attackerBatteringRamSpawnEntities.Length; i++)
		{
			array[i] = _attackerBatteringRamSpawnEntities[i].GetGlobalFrame();
		}
		return array;
	}

	public MatrixFrame[] GetAttackerRangedSiegeEngineFrames()
	{
		MatrixFrame[] array = new MatrixFrame[_attackerRangedEngineSpawnEntities.Length];
		for (int i = 0; i < _attackerRangedEngineSpawnEntities.Length; i++)
		{
			array[i] = _attackerRangedEngineSpawnEntities[i].GetGlobalFrame();
		}
		return array;
	}

	public MatrixFrame[] GetDefenderRangedSiegeEngineFrames()
	{
		MatrixFrame[] array = new MatrixFrame[_defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length];
		for (int i = 0; i < _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length; i++)
		{
			array[i] = _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[i].GetGlobalFrame();
		}
		return array;
	}

	public MatrixFrame[] GetBreachableWallFrames()
	{
		MatrixFrame[] array = new MatrixFrame[_defenderBreachableWallEntitiesCacheForCurrentLevel.Length];
		for (int i = 0; i < _defenderBreachableWallEntitiesCacheForCurrentLevel.Length; i++)
		{
			array[i] = _defenderBreachableWallEntitiesCacheForCurrentLevel[i].GetGlobalFrame();
		}
		return array;
	}

	private void CalculateDataAndDurationsForSiegeMachine(int machineSlotIndex, SiegeEngineType machineType, BattleSideEnum side, SiegeBombardTargets targetType, int targetSlotIndex, out SiegeBombardmentData bombardmentData)
	{
		bombardmentData = default(SiegeBombardmentData);
		MatrixFrame shooterGlobalFrame = ((side == BattleSideEnum.Defender) ? _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[machineSlotIndex].GetGlobalFrame() : _attackerRangedEngineSpawnEntities[machineSlotIndex].GetGlobalFrame());
		shooterGlobalFrame.rotation.MakeUnit();
		bombardmentData.ShooterGlobalFrame = shooterGlobalFrame;
		string siegeEngineMapFireAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapFireAnimationName(machineType, side);
		string siegeEngineMapReloadAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapReloadAnimationName(machineType, side);
		bombardmentData.ReloadDuration = MBAnimation.GetAnimationDuration(siegeEngineMapReloadAnimationName) * 0.25f;
		bombardmentData.AimingDuration = 0.25f;
		bombardmentData.RotationDuration = 0.4f;
		bombardmentData.FireDuration = MBAnimation.GetAnimationDuration(siegeEngineMapFireAnimationName) * 0.25f;
		float animationParameter = MBAnimation.GetAnimationParameter1(siegeEngineMapFireAnimationName);
		bombardmentData.MissileLaunchDuration = bombardmentData.FireDuration * animationParameter;
		bombardmentData.MissileSpeed = 14f;
		bombardmentData.Gravity = ((machineType == DefaultSiegeEngineTypes.Ballista || machineType == DefaultSiegeEngineTypes.FireBallista) ? 10f : 40f);
		switch (targetType)
		{
		case SiegeBombardTargets.RangedEngines:
			bombardmentData.TargetPosition = ((side == BattleSideEnum.Attacker) ? _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[targetSlotIndex].GetGlobalFrame().origin : _attackerRangedEngineSpawnEntities[targetSlotIndex].GetGlobalFrame().origin);
			break;
		case SiegeBombardTargets.Wall:
			bombardmentData.TargetPosition = _defenderBreachableWallEntitiesCacheForCurrentLevel[targetSlotIndex].GlobalPosition;
			break;
		default:
			if (targetSlotIndex == -1)
			{
				bombardmentData.TargetPosition = Vec3.Zero;
				break;
			}
			bombardmentData.TargetPosition = ((side == BattleSideEnum.Attacker) ? _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[targetSlotIndex].GetGlobalFrame().origin : _attackerRangedEngineSpawnEntities[targetSlotIndex].GetGlobalFrame().origin);
			bombardmentData.TargetPosition += (bombardmentData.TargetPosition - bombardmentData.ShooterGlobalFrame.origin).NormalizedCopy() * 2f;
			Campaign.Current.MapSceneWrapper.GetHeightAtPoint(new CampaignVec2(bombardmentData.TargetPosition.AsVec2, isOnLand: true), ref bombardmentData.TargetPosition.z);
			break;
		}
		bombardmentData.TargetAlignedShooterGlobalFrame = bombardmentData.ShooterGlobalFrame;
		bombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.AsVec2 = (bombardmentData.TargetPosition - bombardmentData.ShooterGlobalFrame.origin).AsVec2;
		bombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.NormalizeWithoutChangingZ();
		bombardmentData.TargetAlignedShooterGlobalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		ref MatrixFrame targetAlignedShooterGlobalFrame = ref bombardmentData.TargetAlignedShooterGlobalFrame;
		MatrixFrame launchEntitialFrameForSiegeEngine = base.MapScreen.PrefabEntityCache.GetLaunchEntitialFrameForSiegeEngine(machineType, side);
		bombardmentData.LaunchGlobalPosition = targetAlignedShooterGlobalFrame.TransformToParent(in launchEntitialFrameForSiegeEngine.origin);
		float lengthSquared = (bombardmentData.LaunchGlobalPosition.AsVec2 - bombardmentData.TargetPosition.AsVec2).LengthSquared;
		float num = TaleWorlds.Library.MathF.Sqrt(lengthSquared);
		float num2 = bombardmentData.LaunchGlobalPosition.z - bombardmentData.TargetPosition.z;
		float num3 = bombardmentData.MissileSpeed * bombardmentData.MissileSpeed;
		float num4 = num3 * num3;
		float num5 = num4 - bombardmentData.Gravity * (bombardmentData.Gravity * lengthSquared - 2f * num2 * num3);
		if (num5 >= 0f)
		{
			bombardmentData.LaunchAngle = TaleWorlds.Library.MathF.Atan((num3 - TaleWorlds.Library.MathF.Sqrt(num5)) / (bombardmentData.Gravity * num));
		}
		else
		{
			bombardmentData.Gravity = 1f;
			num5 = num4 - bombardmentData.Gravity * (bombardmentData.Gravity * lengthSquared - 2f * num2 * num3);
			bombardmentData.LaunchAngle = TaleWorlds.Library.MathF.Atan((num3 - TaleWorlds.Library.MathF.Sqrt(num5)) / (bombardmentData.Gravity * num));
		}
		float num6 = bombardmentData.MissileSpeed * TaleWorlds.Library.MathF.Cos(bombardmentData.LaunchAngle);
		bombardmentData.FlightDuration = num / num6;
		bombardmentData.TotalDuration = bombardmentData.RotationDuration + bombardmentData.ReloadDuration + bombardmentData.AimingDuration + bombardmentData.MissileLaunchDuration + bombardmentData.FlightDuration;
	}
}

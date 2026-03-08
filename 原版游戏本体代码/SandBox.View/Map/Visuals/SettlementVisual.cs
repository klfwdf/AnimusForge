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

namespace SandBox.View.Map.Visuals
{
	// Token: 0x02000064 RID: 100
	public class SettlementVisual : MapEntityVisual<PartyBase>
	{
		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000421 RID: 1057 RVA: 0x00021265 File Offset: 0x0001F465
		// (set) Token: 0x06000422 RID: 1058 RVA: 0x0002126D File Offset: 0x0001F46D
		private List<GameEntity> TownPhysicalEntities { get; set; }

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000423 RID: 1059 RVA: 0x00021276 File Offset: 0x0001F476
		public override MapEntityVisual AttachedTo
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000424 RID: 1060 RVA: 0x00021279 File Offset: 0x0001F479
		public override CampaignVec2 InteractionPositionForPlayer
		{
			get
			{
				return ((IInteractablePoint)base.MapEntity).GetInteractionPosition(MobileParty.MainParty);
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000425 RID: 1061 RVA: 0x0002128C File Offset: 0x0001F48C
		private Scene MapScene
		{
			get
			{
				if (this._mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
				{
					this._mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
				}
				return this._mapScene;
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000426 RID: 1062 RVA: 0x000212DA File Offset: 0x0001F4DA
		// (set) Token: 0x06000427 RID: 1063 RVA: 0x000212E2 File Offset: 0x0001F4E2
		public GameEntity StrategicEntity { get; private set; }

		// Token: 0x06000428 RID: 1064 RVA: 0x000212EB File Offset: 0x0001F4EB
		public SettlementVisual(PartyBase entity)
			: base(entity)
		{
			this._siegeRangedMachineEntities = new List<ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>>();
			this._siegeMeleeMachineEntities = new List<ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>>();
			this._siegeMissileEntities = new List<ValueTuple<GameEntity, BattleSideEnum, int>>();
			this.CircleLocalFrame = MatrixFrame.Identity;
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x0002132B File Offset: 0x0001F52B
		public override bool IsEnemyOf(IFaction faction)
		{
			return FactionManager.IsAtWarAgainstFaction(base.MapEntity.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x0600042A RID: 1066 RVA: 0x00021347 File Offset: 0x0001F547
		public override bool IsAllyOf(IFaction faction)
		{
			return DiplomacyHelper.IsSameFactionAndNotEliminated(base.MapEntity.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x00021364 File Offset: 0x0001F564
		internal void OnPartyRemoved()
		{
			if (this.StrategicEntity != null)
			{
				MapScreen.VisualsOfEntities.Remove(this.StrategicEntity.Pointer);
				foreach (GameEntity gameEntity in this.StrategicEntity.GetChildren())
				{
					MapScreen.VisualsOfEntities.Remove(gameEntity.Pointer);
				}
				this.ReleaseResources();
				this.StrategicEntity.Remove(111);
			}
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x000213F8 File Offset: 0x0001F5F8
		public override Vec3 GetVisualPosition()
		{
			return base.MapEntity.Position.AsVec3();
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x00021418 File Offset: 0x0001F618
		public override bool IsVisibleOrFadingOut()
		{
			return base.MapEntity.IsVisible;
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x00021428 File Offset: 0x0001F628
		public override void OnHover()
		{
			if (base.MapEntity.MapEvent != null)
			{
				InformationManager.ShowTooltip(typeof(MapEvent), new object[] { base.MapEntity.MapEvent });
				return;
			}
			if (base.MapEntity.IsSettlement && base.MapEntity.IsVisible)
			{
				if (base.MapEntity.Settlement.SiegeEvent != null)
				{
					InformationManager.ShowTooltip(typeof(SiegeEvent), new object[] { base.MapEntity.Settlement.SiegeEvent });
					return;
				}
				InformationManager.ShowTooltip(typeof(Settlement), new object[] { base.MapEntity.Settlement });
			}
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x000214E0 File Offset: 0x0001F6E0
		public override void OnTrackAction()
		{
			Settlement settlement = base.MapEntity.Settlement;
			if (settlement != null)
			{
				if (Campaign.Current.VisualTrackerManager.CheckTracked(settlement))
				{
					Campaign.Current.VisualTrackerManager.RemoveTrackedObject(settlement, false);
					return;
				}
				Campaign.Current.VisualTrackerManager.RegisterObject(settlement);
			}
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x00021530 File Offset: 0x0001F730
		public override bool OnMapClick(bool followModifierUsed)
		{
			if (followModifierUsed)
			{
				TextObject textObject;
				if (Campaign.Current.Models.EncounterModel.CanMainHeroDoParleyWithParty(base.MapEntity, out textObject))
				{
					base.MapScreen.BeginParleyWith(base.MapEntity);
				}
				else if (!TextObject.IsNullOrEmpty(textObject))
				{
					MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
				}
			}
			else if (base.MapEntity.IsVisible)
			{
				MobileParty.NavigationType navigationType;
				MobileParty.NavigationType navigationType2;
				if (MobileParty.MainParty.IsCurrentlyAtSea && base.MapEntity.Settlement.HasPort && NavigationHelper.CanPlayerNavigateToPosition(base.MapEntity.Settlement.PortPosition, out navigationType))
				{
					MobileParty.MainParty.SetMoveGoToSettlement(base.MapEntity.Settlement, navigationType, true);
				}
				else if (NavigationHelper.CanPlayerNavigateToPosition(base.MapEntity.Settlement.GatePosition, out navigationType2))
				{
					MobileParty.MainParty.SetMoveGoToSettlement(base.MapEntity.Settlement, navigationType2, false);
				}
			}
			return true;
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x00021623 File Offset: 0x0001F823
		public override void OnOpenEncyclopedia()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(base.MapEntity.Settlement.EncyclopediaLink);
		}

		// Token: 0x06000432 RID: 1074 RVA: 0x00021644 File Offset: 0x0001F844
		public override void ReleaseResources()
		{
			this.RemoveSiege();
			this.ResetPartyIcon();
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x00021652 File Offset: 0x0001F852
		private void ResetPartyIcon()
		{
			if (this.StrategicEntity != null)
			{
				if ((this.StrategicEntity.EntityFlags & EntityFlags.Ignore) != (EntityFlags)0U)
				{
					this.StrategicEntity.RemoveFromPredisplayEntity();
				}
				this.StrategicEntity.ClearComponents();
			}
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x0002168C File Offset: 0x0001F88C
		internal void ValidateIsDirty()
		{
			this.RefreshPartyIcon();
			if (base.MapEntity.IsVisible)
			{
				this.StrategicEntity.SetVisibilityExcludeParents(true);
				this.StrategicEntity.SetAlpha(1f);
				this.StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
				return;
			}
			this.StrategicEntity.SetAlpha(0f);
			this.StrategicEntity.SetVisibilityExcludeParents(false);
			this.StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x00021713 File Offset: 0x0001F913
		internal Dictionary<int, List<GameEntity>> GetGateBannerEntitiesWithLevels()
		{
			return this._gateBannerEntitiesWithLevels;
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x0002171C File Offset: 0x0001F91C
		public Vec3 GetBannerPositionForParty(MobileParty mobileParty)
		{
			if (mobileParty.CurrentSettlement == base.MapEntity.Settlement && base.MapEntity.Settlement.IsFortification && this._gateBannerEntitiesWithLevels != null && !this._gateBannerEntitiesWithLevels.IsEmpty<KeyValuePair<int, List<GameEntity>>>())
			{
				int wallLevel = base.MapEntity.Settlement.Town.GetWallLevel();
				int count = this._gateBannerEntitiesWithLevels[wallLevel].Count;
				if (this._gateBannerEntitiesWithLevels[wallLevel].Count > 0)
				{
					int num = 0;
					foreach (MobileParty mobileParty2 in base.MapEntity.Settlement.Parties)
					{
						if (mobileParty2 == mobileParty)
						{
							break;
						}
						Hero leaderHero = mobileParty2.LeaderHero;
						if (((leaderHero != null) ? leaderHero.ClanBanner : null) != null)
						{
							num++;
						}
					}
					GameEntity gameEntity = this._gateBannerEntitiesWithLevels[wallLevel][num % count];
					GameEntity child = gameEntity.GetChild(0);
					MatrixFrame matrixFrame = ((child != null) ? child.GetGlobalFrame() : gameEntity.GetGlobalFrame());
					num /= count;
					int num2 = base.MapEntity.Settlement.Parties.Count(delegate(MobileParty p)
					{
						Hero leaderHero2 = p.LeaderHero;
						return ((leaderHero2 != null) ? leaderHero2.ClanBanner : null) != null;
					});
					float f = 0.75f / (float)MathF.Max(1, num2 / (count * 2));
					int num3 = ((num % 2 == 0) ? (-1) : 1);
					Vec3 v = matrixFrame.rotation.f / 2f * (float)num3;
					if (v.Length < matrixFrame.rotation.s.Length)
					{
						v = matrixFrame.rotation.s / 2f * (float)num3;
					}
					return matrixFrame.origin + v * (float)((num + 1) / 2) * (float)(num % 2 * 2 - 1) * f * (float)num3;
				}
				Debug.FailedAssert(string.Format("{0} - has no Banner Entities at level {1}.", base.MapEntity.Settlement.Name, wallLevel), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Visuals\\SettlementVisual.cs", "GetBannerPositionForParty", 306);
			}
			return Vec3.Invalid;
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x00021978 File Offset: 0x0001FB78
		internal void OnMapHoverSiegeEngineEnd()
		{
			this._hoveredSiegeEntityFrame = MatrixFrame.Identity;
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x0002198C File Offset: 0x0001FB8C
		private void RefreshPartyIcon()
		{
			if (base.MapEntity.IsVisualDirty)
			{
				base.MapEntity.OnVisualsUpdated();
				this.RemoveSiege();
				this.StrategicEntity.RemoveAllParticleSystems();
				this.StrategicEntity.EntityFlags |= EntityFlags.DoNotTick;
				if (base.MapEntity.Settlement.IsFortification)
				{
					this.UpdateDefenderSiegeEntitiesCache();
				}
				this.AddSiegeIconComponents(base.MapEntity);
				this.SetSettlementLevelVisibility();
				this.RefreshWallState();
				this.RefreshTownPhysicalEntitiesState(base.MapEntity);
				this.RefreshSiegePreparations(base.MapEntity);
				bool flag = false;
				if (base.MapEntity.Settlement.IsVillage)
				{
					MapEvent mapEvent = base.MapEntity.MapEvent;
					if (mapEvent != null && mapEvent.IsRaid)
					{
						this.StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
						this.StrategicEntity.AddParticleSystemComponent("psys_fire_smoke_env_point");
						if ((this.StrategicEntity.EntityFlags & EntityFlags.Ignore) != (EntityFlags)0U)
						{
							this.StrategicEntity.RemoveFromPredisplayEntity();
						}
						flag = true;
					}
					else if (base.MapEntity.Settlement.IsRaided)
					{
						this.StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
						this.StrategicEntity.AddParticleSystemComponent("map_icon_village_plunder_fx");
						if ((this.StrategicEntity.EntityFlags & EntityFlags.Ignore) != (EntityFlags)0U)
						{
							this.StrategicEntity.RemoveFromPredisplayEntity();
						}
						flag = true;
					}
				}
				if (!flag && (this.StrategicEntity.EntityFlags & EntityFlags.Ignore) == (EntityFlags)0U)
				{
					this.StrategicEntity.SetAsPredisplayEntity();
				}
				this.StrategicEntity.CheckResources(true, false);
			}
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x00021B28 File Offset: 0x0001FD28
		internal void OnStartup()
		{
			bool flag = false;
			this.StrategicEntity = this.MapScene.GetCampaignEntityWithName(base.MapEntity.Id);
			if (this.StrategicEntity == null)
			{
				IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
				string stringId = base.MapEntity.Settlement.StringId;
				CampaignVec2 position = base.MapEntity.Settlement.Position;
				mapSceneWrapper.AddNewEntityToMapScene(stringId, position);
				this.StrategicEntity = this.MapScene.GetCampaignEntityWithName(base.MapEntity.Id);
			}
			bool flag2 = false;
			if (base.MapEntity.Settlement.IsFortification)
			{
				List<GameEntity> list = new List<GameEntity>();
				this.StrategicEntity.GetChildrenRecursive(ref list);
				this.PopulateSiegeEngineFrameListsFromChildren(list);
				this.UpdateDefenderSiegeEntitiesCache();
				this.TownPhysicalEntities = list.FindAll((GameEntity x) => x.HasTag("bo_town"));
				List<GameEntity> list2 = new List<GameEntity>();
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
				foreach (GameEntity gameEntity in list)
				{
					if (gameEntity.HasTag("main_map_city_gate"))
					{
						NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(gameEntity.GetGlobalFrame().origin.AsVec2, true), MobileParty.NavigationType.Default);
						flag2 = true;
						list2.Add(gameEntity);
					}
					if (gameEntity.HasTag("map_settlement_circle"))
					{
						this.CircleLocalFrame = gameEntity.GetGlobalFrame();
						flag = true;
						gameEntity.SetVisibilityExcludeParents(false);
						list2.Add(gameEntity);
					}
					if (gameEntity.HasTag("map_banner_placeholder"))
					{
						int upgradeLevelOfEntity = gameEntity.Parent.GetUpgradeLevelOfEntity();
						if (upgradeLevelOfEntity == 0)
						{
							dictionary[1].Add(gameEntity);
							dictionary[2].Add(gameEntity);
							dictionary[3].Add(gameEntity);
						}
						else
						{
							dictionary[upgradeLevelOfEntity].Add(gameEntity);
						}
						list2.Add(gameEntity);
					}
				}
				this._gateBannerEntitiesWithLevels = dictionary;
				if (base.MapEntity.Settlement.IsFortification)
				{
					List<MatrixFrame> list3;
					List<MatrixFrame> list4;
					Campaign.Current.MapSceneWrapper.GetSiegeCampFrames(base.MapEntity.Settlement, out list3, out list4);
					base.MapEntity.Settlement.Town.BesiegerCampPositions1 = list3.ToArray();
					base.MapEntity.Settlement.Town.BesiegerCampPositions2 = list4.ToArray();
				}
				foreach (GameEntity gameEntity2 in list2)
				{
					gameEntity2.Remove(112);
				}
				if (!flag2 && !base.MapEntity.Settlement.IsTown)
				{
					bool isCastle = base.MapEntity.Settlement.IsCastle;
				}
				bool flag3 = false;
				if (base.MapEntity.IsSettlement)
				{
					foreach (GameEntity gameEntity3 in this.StrategicEntity.GetChildren())
					{
						if (gameEntity3.HasTag("main_map_city_port"))
						{
							NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(gameEntity3.GetGlobalFrame().origin.AsVec2, false), MobileParty.NavigationType.Naval);
							flag3 = true;
						}
					}
					if ((flag3 || !base.MapEntity.Settlement.HasPort) && flag3)
					{
						bool hasPort = base.MapEntity.Settlement.HasPort;
					}
				}
			}
			if (!flag)
			{
				this.CircleLocalFrame = MatrixFrame.Identity;
				MatrixFrame circleLocalFrame = this.CircleLocalFrame;
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
				this.CircleLocalFrame = circleLocalFrame;
			}
			this.StrategicEntity.SetVisibilityExcludeParents(base.MapEntity.IsVisible);
			this.StrategicEntity.SetReadyToRender(true);
			this.StrategicEntity.SetEntityEnvMapVisibility(false);
			List<GameEntity> list5 = new List<GameEntity>();
			this.StrategicEntity.GetChildrenRecursive(ref list5);
			if (!MapScreen.VisualsOfEntities.ContainsKey(this.StrategicEntity.Pointer))
			{
				MapScreen.VisualsOfEntities.Add(this.StrategicEntity.Pointer, this);
			}
			foreach (GameEntity gameEntity4 in list5)
			{
				if (!MapScreen.VisualsOfEntities.ContainsKey(gameEntity4.Pointer) && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity4.Pointer))
				{
					MapScreen.VisualsOfEntities.Add(gameEntity4.Pointer, this);
				}
			}
			this.StrategicEntity.SetAsPredisplayEntity();
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x00022068 File Offset: 0x00020268
		internal void Tick(float dt, ref int dirtyPartiesCount, ref SettlementVisual[] dirtyPartiesList)
		{
			if (this.StrategicEntity == null)
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
				foreach (ValueTuple<GameEntity, BattleSideEnum, int> valueTuple in this._siegeMissileEntities)
				{
					GameEntity item = valueTuple.Item1;
					ISiegeEventSide siegeEventSide = base.MapEntity.Settlement.SiegeEvent.GetSiegeEventSide(valueTuple.Item2);
					int item2 = valueTuple.Item3;
					bool flag = false;
					if (siegeEventSide.SiegeEngineMissiles.Count > item2)
					{
						SiegeEvent.SiegeEngineMissile siegeEngineMissile = siegeEventSide.SiegeEngineMissiles[item2];
						double toHours2 = siegeEngineMissile.CollisionTime.ToHours;
						SettlementVisual.SiegeBombardmentData siegeBombardmentData;
						this.CalculateDataAndDurationsForSiegeMachine(siegeEngineMissile.ShooterSlotIndex, siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide, siegeEngineMissile.TargetType, siegeEngineMissile.TargetSlotIndex, out siegeBombardmentData);
						float num2 = siegeBombardmentData.MissileSpeed * MathF.Cos(siegeBombardmentData.LaunchAngle);
						if (toHours > toHours2 - (double)siegeBombardmentData.TotalDuration)
						{
							bool flag2 = toHours - (double)dt > toHours2 - (double)siegeBombardmentData.FlightDuration && toHours - (double)dt < toHours2;
							bool flag3 = toHours > toHours2 - (double)siegeBombardmentData.FlightDuration && toHours < toHours2;
							if (flag3)
							{
								flag = true;
								float num3 = (float)(toHours - (toHours2 - (double)siegeBombardmentData.FlightDuration));
								float num4 = siegeBombardmentData.MissileSpeed * MathF.Sin(siegeBombardmentData.LaunchAngle);
								Vec2 vec = new Vec2(num2 * num3, num4 * num3 - siegeBombardmentData.Gravity * 0.5f * num3 * num3);
								Vec3 v = siegeBombardmentData.LaunchGlobalPosition + siegeBombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.NormalizedCopy() * vec.x + siegeBombardmentData.TargetAlignedShooterGlobalFrame.rotation.u.NormalizedCopy() * vec.y;
								float num5 = num3 + 0.1f;
								Vec2 vec2 = new Vec2(num2 * num5, num4 * num5 - siegeBombardmentData.Gravity * 0.5f * num5 * num5);
								Vec3 v2 = siegeBombardmentData.LaunchGlobalPosition + siegeBombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.NormalizedCopy() * vec2.x + siegeBombardmentData.TargetAlignedShooterGlobalFrame.rotation.u.NormalizedCopy() * vec2.y;
								Mat3 rotation = item.GetGlobalFrame().rotation;
								rotation.f = v2 - v;
								rotation.Orthonormalize();
								Vec3 vec3 = base.MapScreen.PrefabEntityCache.GetScaleForSiegeEngine(siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide);
								rotation.ApplyScaleLocal(vec3);
								MatrixFrame matrixFrame = new MatrixFrame(ref rotation, ref v);
								item.SetGlobalFrame(matrixFrame, true);
							}
							item.WeakEntity.GetChild(0).SetVisibilityExcludeParents(flag3);
							int soundCodeId = -1;
							if (!flag2 && flag3)
							{
								if (siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.Ballista || siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.FireBallista)
								{
									soundCodeId = MiscSoundContainer.SoundCodeAmbientNodeSiegeBallistaFire;
								}
								else if (siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.Catapult || siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.FireCatapult || siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.Onager || siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.FireOnager)
								{
									soundCodeId = MiscSoundContainer.SoundCodeAmbientNodeSiegeMangonelFire;
								}
								else
								{
									soundCodeId = MiscSoundContainer.SoundCodeAmbientNodeSiegeTrebuchetFire;
								}
							}
							else if (flag2 && !flag3)
							{
								this.StrategicEntity.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName((siegeEngineMissile.TargetType == SiegeBombardTargets.RangedEngines) ? "psys_game_ballista_destruction" : "psys_campaign_boulder_stone_coll"), item.GetGlobalFrame());
								soundCodeId = ((siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.Ballista || siegeEngineMissile.ShooterSiegeEngineType == DefaultSiegeEngineTypes.FireBallista) ? MiscSoundContainer.SoundCodeAmbientNodeSiegeBallistaHit : MiscSoundContainer.SoundCodeAmbientNodeSiegeBoulderHit);
							}
							MBSoundEvent.PlaySound(soundCodeId, item.GlobalPosition);
							if (toHours >= toHours2 - (double)(siegeBombardmentData.TotalDuration - siegeBombardmentData.RotationDuration - siegeBombardmentData.ReloadDuration))
							{
								if (toHours < toHours2 - (double)(siegeBombardmentData.TotalDuration - siegeBombardmentData.RotationDuration - siegeBombardmentData.ReloadDuration - siegeBombardmentData.AimingDuration))
								{
									if (siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex] == null || siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex].SiegeEngine != siegeEngineMissile.ShooterSiegeEngineType)
									{
										goto IL_66E;
									}
									using (List<ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>>.Enumerator enumerator2 = this._siegeRangedMachineEntities.GetEnumerator())
									{
										while (enumerator2.MoveNext())
										{
											ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity> valueTuple2 = enumerator2.Current;
											if (!flag && valueTuple2.Item2 == siegeEventSide.BattleSide && valueTuple2.Item3 == siegeEngineMissile.ShooterSlotIndex)
											{
												GameEntity item3 = valueTuple2.Item5;
												if (item3 != null)
												{
													flag = true;
													GameEntity gameEntity = item;
													MatrixFrame matrixFrame2 = item3.GetGlobalFrame();
													MatrixFrame matrixFrame3 = item3.Skeleton.GetBoneEntitialFrame(Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide), false);
													matrixFrame2 = matrixFrame2.TransformToParent(matrixFrame3);
													gameEntity.SetGlobalFrame(matrixFrame2, true);
												}
											}
										}
										goto IL_66E;
									}
								}
								if (toHours < toHours2 - (double)(siegeBombardmentData.TotalDuration - siegeBombardmentData.RotationDuration - siegeBombardmentData.ReloadDuration - siegeBombardmentData.AimingDuration - siegeBombardmentData.FireDuration) && !flag3 && siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex] != null && siegeEventSide.SiegeEngines.DeployedRangedSiegeEngines[siegeEngineMissile.ShooterSlotIndex].SiegeEngine == siegeEngineMissile.ShooterSiegeEngineType)
								{
									foreach (ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity> valueTuple3 in this._siegeRangedMachineEntities)
									{
										if (!flag && valueTuple3.Item2 == siegeEventSide.BattleSide && valueTuple3.Item3 == siegeEngineMissile.ShooterSlotIndex)
										{
											GameEntity item4 = valueTuple3.Item5;
											if (item4 != null)
											{
												flag = true;
												GameEntity gameEntity2 = item;
												MatrixFrame matrixFrame3 = item4.GetGlobalFrame();
												MatrixFrame matrixFrame2 = item4.Skeleton.GetBoneEntitialFrame(Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineMissile.ShooterSiegeEngineType, siegeEventSide.BattleSide), false);
												matrixFrame3 = matrixFrame3.TransformToParent(matrixFrame2);
												gameEntity2.SetGlobalFrame(matrixFrame3, true);
											}
										}
									}
								}
							}
						}
					}
					IL_66E:
					item.SetVisibilityExcludeParents(flag);
				}
				foreach (ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity> valueTuple4 in this._siegeRangedMachineEntities)
				{
					GameEntity item5 = valueTuple4.Item1;
					BattleSideEnum item6 = valueTuple4.Item2;
					int item7 = valueTuple4.Item3;
					GameEntity item8 = valueTuple4.Item5;
					SiegeEngineType siegeEngine = base.MapEntity.Settlement.SiegeEvent.GetSiegeEventSide(item6).SiegeEngines.DeployedRangedSiegeEngines[item7].SiegeEngine;
					if (item8 != null)
					{
						Skeleton skeleton = item8.Skeleton;
						string siegeEngineMapFireAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapFireAnimationName(siegeEngine, item6);
						string siegeEngineMapReloadAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapReloadAnimationName(siegeEngine, item6);
						SiegeEvent.RangedSiegeEngine rangedSiegeEngine = base.MapEntity.Settlement.SiegeEvent.GetSiegeEventSide(item6).SiegeEngines.DeployedRangedSiegeEngines[item7].RangedSiegeEngine;
						SettlementVisual.SiegeBombardmentData siegeBombardmentData2;
						this.CalculateDataAndDurationsForSiegeMachine(item7, siegeEngine, item6, rangedSiegeEngine.CurrentTargetType, rangedSiegeEngine.CurrentTargetIndex, out siegeBombardmentData2);
						MatrixFrame shooterGlobalFrame = siegeBombardmentData2.ShooterGlobalFrame;
						if (rangedSiegeEngine.PreviousTargetIndex >= 0)
						{
							Vec3 v3;
							if (rangedSiegeEngine.PreviousDamagedTargetType == SiegeBombardTargets.Wall)
							{
								v3 = this._defenderBreachableWallEntitiesCacheForCurrentLevel[rangedSiegeEngine.PreviousTargetIndex].GlobalPosition;
							}
							else
							{
								v3 = ((item6 == BattleSideEnum.Attacker) ? this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[rangedSiegeEngine.PreviousTargetIndex].GetGlobalFrame().origin : this._attackerRangedEngineSpawnEntities[rangedSiegeEngine.PreviousTargetIndex].GetGlobalFrame().origin);
							}
							Vec3 vec3 = v3 - shooterGlobalFrame.origin;
							shooterGlobalFrame.rotation.f.AsVec2 = vec3.AsVec2;
							shooterGlobalFrame.rotation.f.NormalizeWithoutChangingZ();
							shooterGlobalFrame.rotation.Orthonormalize();
						}
						item5.SetGlobalFrame(shooterGlobalFrame, true);
						skeleton.TickAnimations(dt, MatrixFrame.Identity, false);
						double toHours3 = rangedSiegeEngine.NextProjectileCollisionTime.ToHours;
						if (toHours > toHours3 - (double)siegeBombardmentData2.TotalDuration)
						{
							if (toHours < toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration))
							{
								Vec3 vec3 = siegeBombardmentData2.TargetPosition - shooterGlobalFrame.origin;
								float rotationInRadians = vec3.AsVec2.RotationInRadians;
								float rotationInRadians2 = shooterGlobalFrame.rotation.f.AsVec2.RotationInRadians;
								float f = rotationInRadians - rotationInRadians2;
								float num6 = MathF.Abs(f);
								float num7 = (float)(toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration) - toHours);
								if (num6 > num7 * 2f)
								{
									shooterGlobalFrame.rotation.f.AsVec2 = Vec2.FromRotation(rotationInRadians2 + (float)MathF.Sign(f) * (num6 - num7 * 2f));
									shooterGlobalFrame.rotation.f.NormalizeWithoutChangingZ();
									shooterGlobalFrame.rotation.Orthonormalize();
									item5.SetGlobalFrame(shooterGlobalFrame, true);
								}
							}
							else if (toHours < toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration - siegeBombardmentData2.ReloadDuration))
							{
								item5.SetGlobalFrame(siegeBombardmentData2.TargetAlignedShooterGlobalFrame, true);
								skeleton.SetAnimationAtChannel(siegeEngineMapReloadAnimationName, 0, 1f, 0f, (float)((toHours - (toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration))) / (double)siegeBombardmentData2.ReloadDuration));
							}
							else if (toHours < toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration - siegeBombardmentData2.ReloadDuration - siegeBombardmentData2.AimingDuration))
							{
								item5.SetGlobalFrame(siegeBombardmentData2.TargetAlignedShooterGlobalFrame, true);
								skeleton.SetAnimationAtChannel(siegeEngineMapReloadAnimationName, 0, 1f, 0f, 1f);
							}
							else if (toHours < toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration - siegeBombardmentData2.ReloadDuration - siegeBombardmentData2.AimingDuration - siegeBombardmentData2.FireDuration))
							{
								item5.SetGlobalFrame(siegeBombardmentData2.TargetAlignedShooterGlobalFrame, true);
								skeleton.SetAnimationAtChannel(siegeEngineMapFireAnimationName, 0, 1f, 0f, (float)((toHours - (toHours3 - (double)(siegeBombardmentData2.TotalDuration - siegeBombardmentData2.RotationDuration - siegeBombardmentData2.ReloadDuration - siegeBombardmentData2.AimingDuration))) / (double)siegeBombardmentData2.FireDuration));
							}
							else
							{
								item5.SetGlobalFrame(siegeBombardmentData2.TargetAlignedShooterGlobalFrame, true);
								skeleton.SetAnimationAtChannel(siegeEngineMapFireAnimationName, 0, 1f, 0f, 1f);
							}
						}
					}
				}
			}
			if (base.MapEntity.LevelMaskIsDirty)
			{
				this.RefreshLevelMask();
			}
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x00022BC8 File Offset: 0x00020DC8
		internal void OnMapHoverSiegeEngine(MatrixFrame engineFrame)
		{
			if (PlayerSiege.PlayerSiegeEvent == null)
			{
				return;
			}
			for (int i = 0; i < this._attackerBatteringRamSpawnEntities.Length; i++)
			{
				MatrixFrame globalFrame = this._attackerBatteringRamSpawnEntities[i].GetGlobalFrame();
				if (globalFrame.NearlyEquals(engineFrame, 1E-05f))
				{
					if ((this._hoveredSiegeEntityFrame) != (globalFrame))
					{
						SiegeEvent.SiegeEngineConstructionProgress engineInProgress = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[i];
						InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress) });
					}
					return;
				}
			}
			for (int j = 0; j < this._attackerSiegeTowerSpawnEntities.Length; j++)
			{
				MatrixFrame globalFrame2 = this._attackerSiegeTowerSpawnEntities[j].GetGlobalFrame();
				if (globalFrame2.NearlyEquals(engineFrame, 1E-05f))
				{
					if ((this._hoveredSiegeEntityFrame) != (globalFrame2))
					{
						SiegeEvent.SiegeEngineConstructionProgress engineInProgress2 = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[this._attackerBatteringRamSpawnEntities.Length + j];
						InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress2) });
					}
					return;
				}
			}
			for (int k = 0; k < this._attackerRangedEngineSpawnEntities.Length; k++)
			{
				MatrixFrame globalFrame3 = this._attackerRangedEngineSpawnEntities[k].GetGlobalFrame();
				if (globalFrame3.NearlyEquals(engineFrame, 1E-05f))
				{
					if ((this._hoveredSiegeEntityFrame) != (globalFrame3))
					{
						SiegeEvent.SiegeEngineConstructionProgress engineInProgress3 = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines[k];
						InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress3) });
					}
					return;
				}
			}
			for (int l = 0; l < this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length; l++)
			{
				MatrixFrame globalFrame4 = this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[l].GetGlobalFrame();
				if (globalFrame4.NearlyEquals(engineFrame, 1E-05f))
				{
					if ((this._hoveredSiegeEntityFrame) != (globalFrame4))
					{
						SiegeEvent.SiegeEngineConstructionProgress engineInProgress4 = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines[l];
						InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetSiegeEngineInProgressTooltip(engineInProgress4) });
					}
					return;
				}
			}
			for (int m = 0; m < this._defenderBreachableWallEntitiesCacheForCurrentLevel.Length; m++)
			{
				MatrixFrame globalFrame5 = this._defenderBreachableWallEntitiesCacheForCurrentLevel[m].GetGlobalFrame();
				if (globalFrame5.NearlyEquals(engineFrame, 1E-05f))
				{
					if ((this._hoveredSiegeEntityFrame) != (globalFrame5) && base.MapEntity.IsSettlement)
					{
						InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetWallSectionTooltip(base.MapEntity.Settlement, m) });
					}
					return;
				}
			}
			this._hoveredSiegeEntityFrame = MatrixFrame.Identity;
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x00022E5C File Offset: 0x0002105C
		private void RemoveSiege()
		{
			foreach (ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity> valueTuple in this._siegeRangedMachineEntities)
			{
				this.StrategicEntity.RemoveChild(valueTuple.Item1, false, false, true, 36);
			}
			foreach (ValueTuple<GameEntity, BattleSideEnum, int> valueTuple2 in this._siegeMissileEntities)
			{
				this.StrategicEntity.RemoveChild(valueTuple2.Item1, false, false, true, 37);
			}
			foreach (ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity> valueTuple3 in this._siegeMeleeMachineEntities)
			{
				this.StrategicEntity.RemoveChild(valueTuple3.Item1, false, false, true, 38);
			}
			this._siegeRangedMachineEntities.Clear();
			this._siegeMeleeMachineEntities.Clear();
			this._siegeMissileEntities.Clear();
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x00022F84 File Offset: 0x00021184
		private void AddSiegeIconComponents(PartyBase party)
		{
			if (party.Settlement.IsUnderSiege)
			{
				int wallLevel = -1;
				if (party.Settlement.SiegeEvent.BesiegedSettlement.IsTown || party.Settlement.SiegeEvent.BesiegedSettlement.IsCastle)
				{
					wallLevel = party.Settlement.SiegeEvent.BesiegedSettlement.Town.GetWallLevel();
				}
				SiegeEvent.SiegeEngineConstructionProgress[] deployedRangedSiegeEngines = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines;
				for (int i = 0; i < deployedRangedSiegeEngines.Length; i++)
				{
					SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress = deployedRangedSiegeEngines[i];
					if (siegeEngineConstructionProgress != null && siegeEngineConstructionProgress.IsActive && i < this._attackerRangedEngineSpawnEntities.Length)
					{
						MatrixFrame globalFrame = this._attackerRangedEngineSpawnEntities[i].GetGlobalFrame();
						globalFrame.rotation.MakeUnit();
						this.AddSiegeMachine(deployedRangedSiegeEngines[i].SiegeEngine, globalFrame, BattleSideEnum.Attacker, wallLevel, i);
					}
				}
				SiegeEvent.SiegeEngineConstructionProgress[] deployedMeleeSiegeEngines = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines;
				for (int j = 0; j < deployedMeleeSiegeEngines.Length; j++)
				{
					SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress2 = deployedMeleeSiegeEngines[j];
					if (siegeEngineConstructionProgress2 != null && siegeEngineConstructionProgress2.IsActive)
					{
						if (deployedMeleeSiegeEngines[j].SiegeEngine == DefaultSiegeEngineTypes.SiegeTower)
						{
							int num = j - this._attackerBatteringRamSpawnEntities.Length;
							if (num >= 0)
							{
								MatrixFrame globalFrame2 = this._attackerSiegeTowerSpawnEntities[num].GetGlobalFrame();
								globalFrame2.rotation.MakeUnit();
								this.AddSiegeMachine(deployedMeleeSiegeEngines[j].SiegeEngine, globalFrame2, BattleSideEnum.Attacker, wallLevel, j);
							}
						}
						else if (deployedMeleeSiegeEngines[j].SiegeEngine == DefaultSiegeEngineTypes.Ram || deployedMeleeSiegeEngines[j].SiegeEngine == DefaultSiegeEngineTypes.ImprovedRam)
						{
							int num2 = j;
							if (num2 >= 0)
							{
								MatrixFrame globalFrame3 = this._attackerBatteringRamSpawnEntities[num2].GetGlobalFrame();
								globalFrame3.rotation.MakeUnit();
								this.AddSiegeMachine(deployedMeleeSiegeEngines[j].SiegeEngine, globalFrame3, BattleSideEnum.Attacker, wallLevel, j);
							}
						}
					}
				}
				SiegeEvent.SiegeEngineConstructionProgress[] deployedRangedSiegeEngines2 = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines;
				for (int k = 0; k < deployedRangedSiegeEngines2.Length; k++)
				{
					SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress3 = deployedRangedSiegeEngines2[k];
					if (siegeEngineConstructionProgress3 != null && siegeEngineConstructionProgress3.IsActive)
					{
						MatrixFrame globalFrame4 = this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[k].GetGlobalFrame();
						globalFrame4.rotation.MakeUnit();
						this.AddSiegeMachine(deployedRangedSiegeEngines2[k].SiegeEngine, globalFrame4, BattleSideEnum.Defender, wallLevel, k);
					}
				}
				for (int l = 0; l < 2; l++)
				{
					BattleSideEnum side = ((l == 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
					MBReadOnlyList<SiegeEvent.SiegeEngineMissile> siegeEngineMissiles = party.Settlement.SiegeEvent.GetSiegeEventSide(side).SiegeEngineMissiles;
					for (int m = 0; m < siegeEngineMissiles.Count; m++)
					{
						this.AddSiegeMissile(siegeEngineMissiles[m].ShooterSiegeEngineType, this.StrategicEntity.GetGlobalFrame(), side, m);
					}
				}
			}
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x00023248 File Offset: 0x00021448
		private void AddSiegeMachine(SiegeEngineType type, MatrixFrame globalFrame, BattleSideEnum side, int wallLevel, int slotIndex)
		{
			string siegeEngineMapPrefabName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapPrefabName(type, wallLevel, side);
			GameEntity gameEntity = GameEntity.Instantiate(this.MapScene, siegeEngineMapPrefabName, true, true, "");
			if (gameEntity != null)
			{
				this.StrategicEntity.AddChild(gameEntity, false);
				MatrixFrame matrixFrame;
				gameEntity.GetLocalFrame(out matrixFrame);
				GameEntity gameEntity2 = gameEntity;
				MatrixFrame matrixFrame2 = globalFrame.TransformToParent(matrixFrame);
				gameEntity2.SetGlobalFrame(matrixFrame2, true);
				List<WeakGameEntity> list = new List<WeakGameEntity>();
				gameEntity.WeakEntity.GetChildrenRecursive(ref list);
				GameEntity gameEntity3 = null;
				if (list.Any((WeakGameEntity entity) => entity.HasTag("siege_machine_mapicon_skeleton")))
				{
					WeakGameEntity weakEntity = list.Find((WeakGameEntity entity) => entity.HasTag("siege_machine_mapicon_skeleton"));
					if (weakEntity.Skeleton != null)
					{
						gameEntity3 = GameEntity.CreateFromWeakEntity(weakEntity);
						string siegeEngineMapFireAnimationName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapFireAnimationName(type, side);
						gameEntity3.Skeleton.SetAnimationAtChannel(siegeEngineMapFireAnimationName, 0, 1f, 0f, 1f);
					}
				}
				if (type.IsRanged)
				{
					this._siegeRangedMachineEntities.Add(ValueTuple.Create<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>(gameEntity, side, slotIndex, globalFrame, gameEntity3));
					return;
				}
				this._siegeMeleeMachineEntities.Add(ValueTuple.Create<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>(gameEntity, side, slotIndex, globalFrame, gameEntity3));
			}
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x000233A4 File Offset: 0x000215A4
		private void AddSiegeMissile(SiegeEngineType type, MatrixFrame globalFrame, BattleSideEnum side, int missileIndex)
		{
			string siegeEngineMapProjectilePrefabName = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineMapProjectilePrefabName(type);
			GameEntity gameEntity = GameEntity.Instantiate(this.MapScene, siegeEngineMapProjectilePrefabName, true, true, "");
			if (gameEntity != null)
			{
				this._siegeMissileEntities.Add(ValueTuple.Create<GameEntity, BattleSideEnum, int>(gameEntity, side, missileIndex));
				this.StrategicEntity.AddChild(gameEntity, false);
				this.StrategicEntity.EntityFlags &= ~EntityFlags.DoNotTick;
				MatrixFrame matrixFrame;
				gameEntity.GetLocalFrame(out matrixFrame);
				GameEntity gameEntity2 = gameEntity;
				MatrixFrame matrixFrame2 = globalFrame.TransformToParent(matrixFrame);
				gameEntity2.SetGlobalFrame(matrixFrame2, true);
				gameEntity.SetVisibilityExcludeParents(false);
			}
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x0002343E File Offset: 0x0002163E
		private void SetLevelMask(uint newMask)
		{
			this._currentLevelMask = newMask;
			base.MapEntity.SetVisualAsDirty();
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x00023454 File Offset: 0x00021654
		private void RefreshLevelMask()
		{
			uint num = 0U;
			if (base.MapEntity.Settlement.IsVillage)
			{
				if (base.MapEntity.Settlement.Village.VillageState == Village.VillageStates.Looted)
				{
					num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("looted");
				}
				else
				{
					num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("civilian");
				}
				num |= SettlementVisual.GetLevelOfProduction(base.MapEntity.Settlement);
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
				if (base.MapEntity.Settlement.SiegeEvent != null)
				{
					num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("siege");
				}
				else
				{
					num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("civilian");
				}
			}
			else if (base.MapEntity.Settlement.IsHideout)
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_1");
			}
			if (this._currentLevelMask != num)
			{
				this.SetLevelMask(num);
			}
			base.MapEntity.OnLevelMaskUpdated();
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x00023618 File Offset: 0x00021818
		private static uint GetLevelOfProduction(Settlement settlement)
		{
			uint num = 0U;
			if (settlement.Village.Hearth < 200f)
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_1");
			}
			else if (settlement.Village.Hearth < 600f)
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_2");
			}
			else
			{
				num |= Campaign.Current.MapSceneWrapper.GetSceneLevel("level_3");
			}
			return num;
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x00023698 File Offset: 0x00021898
		private void SetSettlementLevelVisibility()
		{
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			this.StrategicEntity.WeakEntity.GetChildrenRecursive(ref list);
			foreach (WeakGameEntity gameEntity in list)
			{
				if ((gameEntity.GetUpgradeLevelMask() & (GameEntity.UpgradeLevelMask)this._currentLevelMask) == (GameEntity.UpgradeLevelMask)this._currentLevelMask)
				{
					gameEntity.SetVisibilityExcludeParents(true);
					gameEntity.SetPhysicsState(true, true);
				}
				else
				{
					gameEntity.SetVisibilityExcludeParents(false);
					gameEntity.SetPhysicsState(false, true);
				}
			}
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x00023734 File Offset: 0x00021934
		private void PopulateSiegeEngineFrameListsFromChildren(List<GameEntity> children)
		{
			this._attackerRangedEngineSpawnEntities = (from e in children.FindAll((GameEntity x) => x.Tags.Any((string t) => t.Contains("map_siege_engine")))
				orderby e.Tags.First((string s) => s.Contains("map_siege_engine"))
				select e).ToArray<GameEntity>();
			foreach (GameEntity gameEntity in this._attackerRangedEngineSpawnEntities)
			{
				if (gameEntity.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity.GetChild(0).Pointer))
				{
					MapScreen.FrameAndVisualOfEngines.Add(gameEntity.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity.GetGlobalFrame(), this));
				}
			}
			this._defenderRangedEngineSpawnEntitiesForAllLevels = (from e in children.FindAll((GameEntity x) => x.Tags.Any((string t) => t.Contains("map_defensive_engine")))
				orderby e.Tags.First((string s) => s.Contains("map_defensive_engine"))
				select e).ToArray<GameEntity>();
			foreach (GameEntity gameEntity2 in this._defenderRangedEngineSpawnEntitiesForAllLevels)
			{
				if (gameEntity2.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity2.GetChild(0).Pointer))
				{
					MapScreen.FrameAndVisualOfEngines.Add(gameEntity2.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity2.GetGlobalFrame(), this));
				}
			}
			this._attackerBatteringRamSpawnEntities = children.FindAll((GameEntity x) => x.HasTag("map_siege_ram")).ToArray();
			foreach (GameEntity gameEntity3 in this._attackerBatteringRamSpawnEntities)
			{
				if (gameEntity3.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity3.GetChild(0).Pointer))
				{
					MapScreen.FrameAndVisualOfEngines.Add(gameEntity3.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity3.GetGlobalFrame(), this));
				}
			}
			this._attackerSiegeTowerSpawnEntities = children.FindAll((GameEntity x) => x.HasTag("map_siege_tower")).ToArray();
			foreach (GameEntity gameEntity4 in this._attackerSiegeTowerSpawnEntities)
			{
				if (gameEntity4.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity4.GetChild(0).Pointer))
				{
					MapScreen.FrameAndVisualOfEngines.Add(gameEntity4.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity4.GetGlobalFrame(), this));
				}
			}
			this._defenderBreachableWallEntitiesForAllLevels = children.FindAll((GameEntity x) => x.HasTag("map_breachable_wall")).ToArray();
			foreach (GameEntity gameEntity5 in this._defenderBreachableWallEntitiesForAllLevels)
			{
				if (gameEntity5.ChildCount > 0 && !MapScreen.FrameAndVisualOfEngines.ContainsKey(gameEntity5.GetChild(0).Pointer))
				{
					MapScreen.FrameAndVisualOfEngines.Add(gameEntity5.GetChild(0).Pointer, new Tuple<MatrixFrame, SettlementVisual>(gameEntity5.GetGlobalFrame(), this));
				}
			}
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x00023A54 File Offset: 0x00021C54
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
			this._currentSettlementUpgradeLevelMask = currentSettlementUpgradeLevelMask;
			this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel = (from e in this._defenderRangedEngineSpawnEntitiesForAllLevels
				where (e.GetUpgradeLevelMask() & this._currentSettlementUpgradeLevelMask) == this._currentSettlementUpgradeLevelMask
				select e).ToArray<GameEntity>();
			this._defenderBreachableWallEntitiesCacheForCurrentLevel = (from e in this._defenderBreachableWallEntitiesForAllLevels
				where (e.GetUpgradeLevelMask() & this._currentSettlementUpgradeLevelMask) == this._currentSettlementUpgradeLevelMask
				select e).ToArray<GameEntity>();
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x00023B20 File Offset: 0x00021D20
		private void RefreshWallState()
		{
			if (this._defenderBreachableWallEntitiesForAllLevels != null)
			{
				PartyBase mapEntity = base.MapEntity;
				MBReadOnlyList<float> mbreadOnlyList;
				if (((mapEntity != null) ? mapEntity.Settlement : null) == null || (base.MapEntity.Settlement != null && !base.MapEntity.Settlement.IsFortification))
				{
					mbreadOnlyList = null;
				}
				else
				{
					mbreadOnlyList = base.MapEntity.Settlement.SettlementWallSectionHitPointsRatioList;
				}
				if (mbreadOnlyList != null)
				{
					if (mbreadOnlyList.Count == 0)
					{
						Debug.FailedAssert(string.Concat(new object[]
						{
							"Town (",
							base.MapEntity.Settlement.Name.ToString(),
							") doesn't have wall entities defined for it's current level(",
							base.MapEntity.Settlement.Town.GetWallLevel(),
							")"
						}), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Visuals\\SettlementVisual.cs", "RefreshWallState", 1303);
						return;
					}
					for (int i = 0; i < this._defenderBreachableWallEntitiesForAllLevels.Length; i++)
					{
						bool flag = mbreadOnlyList[i % mbreadOnlyList.Count] <= 0f;
						foreach (WeakGameEntity weakGameEntity in this._defenderBreachableWallEntitiesForAllLevels[i].WeakEntity.GetChildren())
						{
							if (weakGameEntity.HasTag("map_solid_wall"))
							{
								weakGameEntity.SetVisibilityExcludeParents(!flag);
							}
							else if (weakGameEntity.HasTag("map_broken_wall"))
							{
								weakGameEntity.SetVisibilityExcludeParents(flag);
							}
						}
					}
				}
			}
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x00023CA8 File Offset: 0x00021EA8
		private void RefreshTownPhysicalEntitiesState(PartyBase party)
		{
			if (((party != null) ? party.Settlement : null) != null && party.Settlement.IsFortification && this.TownPhysicalEntities != null)
			{
				if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSiegeEvent.BesiegedSettlement == party.Settlement)
				{
					this.TownPhysicalEntities.ForEach(delegate(GameEntity p)
					{
						p.AddBodyFlags(BodyFlags.Disabled, true);
					});
					return;
				}
				this.TownPhysicalEntities.ForEach(delegate(GameEntity p)
				{
					p.RemoveBodyFlags(BodyFlags.Disabled, true);
				});
			}
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x00023D4C File Offset: 0x00021F4C
		private void RefreshSiegePreparations(PartyBase party)
		{
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			this.StrategicEntity.WeakEntity.GetChildrenRecursive(ref list);
			List<WeakGameEntity> list2 = list.FindAll((WeakGameEntity x) => x.HasTag("siege_preparation"));
			bool flag = false;
			if (party.Settlement != null && party.Settlement.IsUnderSiege)
			{
				SiegeEvent.SiegeEngineConstructionProgress siegePreparations = party.Settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.SiegePreparations;
				if (siegePreparations != null && siegePreparations.Progress >= 1f)
				{
					flag = true;
					foreach (WeakGameEntity weakGameEntity in list2)
					{
						weakGameEntity.SetVisibilityExcludeParents(true);
					}
				}
			}
			if (!flag)
			{
				foreach (WeakGameEntity weakGameEntity2 in list2)
				{
					weakGameEntity2.SetVisibilityExcludeParents(false);
				}
			}
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x00023E6C File Offset: 0x0002206C
		public MatrixFrame[] GetAttackerTowerSiegeEngineFrames()
		{
			MatrixFrame[] array = new MatrixFrame[this._attackerSiegeTowerSpawnEntities.Length];
			for (int i = 0; i < this._attackerSiegeTowerSpawnEntities.Length; i++)
			{
				array[i] = this._attackerSiegeTowerSpawnEntities[i].GetGlobalFrame();
			}
			return array;
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x00023EB0 File Offset: 0x000220B0
		public MatrixFrame[] GetAttackerBatteringRamSiegeEngineFrames()
		{
			MatrixFrame[] array = new MatrixFrame[this._attackerBatteringRamSpawnEntities.Length];
			for (int i = 0; i < this._attackerBatteringRamSpawnEntities.Length; i++)
			{
				array[i] = this._attackerBatteringRamSpawnEntities[i].GetGlobalFrame();
			}
			return array;
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x00023EF4 File Offset: 0x000220F4
		public MatrixFrame[] GetAttackerRangedSiegeEngineFrames()
		{
			MatrixFrame[] array = new MatrixFrame[this._attackerRangedEngineSpawnEntities.Length];
			for (int i = 0; i < this._attackerRangedEngineSpawnEntities.Length; i++)
			{
				array[i] = this._attackerRangedEngineSpawnEntities[i].GetGlobalFrame();
			}
			return array;
		}

		// Token: 0x0600044C RID: 1100 RVA: 0x00023F38 File Offset: 0x00022138
		public MatrixFrame[] GetDefenderRangedSiegeEngineFrames()
		{
			MatrixFrame[] array = new MatrixFrame[this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length];
			for (int i = 0; i < this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length; i++)
			{
				array[i] = this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[i].GetGlobalFrame();
			}
			return array;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x00023F7C File Offset: 0x0002217C
		public MatrixFrame[] GetBreachableWallFrames()
		{
			MatrixFrame[] array = new MatrixFrame[this._defenderBreachableWallEntitiesCacheForCurrentLevel.Length];
			for (int i = 0; i < this._defenderBreachableWallEntitiesCacheForCurrentLevel.Length; i++)
			{
				array[i] = this._defenderBreachableWallEntitiesCacheForCurrentLevel[i].GetGlobalFrame();
			}
			return array;
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x00023FC0 File Offset: 0x000221C0
		private void CalculateDataAndDurationsForSiegeMachine(int machineSlotIndex, SiegeEngineType machineType, BattleSideEnum side, SiegeBombardTargets targetType, int targetSlotIndex, out SettlementVisual.SiegeBombardmentData bombardmentData)
		{
			bombardmentData = default(SettlementVisual.SiegeBombardmentData);
			MatrixFrame shooterGlobalFrame = ((side == BattleSideEnum.Defender) ? this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[machineSlotIndex].GetGlobalFrame() : this._attackerRangedEngineSpawnEntities[machineSlotIndex].GetGlobalFrame());
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
			if (targetType == SiegeBombardTargets.RangedEngines)
			{
				bombardmentData.TargetPosition = ((side == BattleSideEnum.Attacker) ? this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[targetSlotIndex].GetGlobalFrame().origin : this._attackerRangedEngineSpawnEntities[targetSlotIndex].GetGlobalFrame().origin);
			}
			else if (targetType == SiegeBombardTargets.Wall)
			{
				bombardmentData.TargetPosition = this._defenderBreachableWallEntitiesCacheForCurrentLevel[targetSlotIndex].GlobalPosition;
			}
			else if (targetSlotIndex == -1)
			{
				bombardmentData.TargetPosition = Vec3.Zero;
			}
			else
			{
				bombardmentData.TargetPosition = ((side == BattleSideEnum.Attacker) ? this._defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[targetSlotIndex].GetGlobalFrame().origin : this._attackerRangedEngineSpawnEntities[targetSlotIndex].GetGlobalFrame().origin);
				bombardmentData.TargetPosition += (bombardmentData.TargetPosition - bombardmentData.ShooterGlobalFrame.origin).NormalizedCopy() * 2f;
				IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
				CampaignVec2 campaignVec = new CampaignVec2(bombardmentData.TargetPosition.AsVec2, true);
				mapSceneWrapper.GetHeightAtPoint(campaignVec, ref bombardmentData.TargetPosition.z);
			}
			bombardmentData.TargetAlignedShooterGlobalFrame = bombardmentData.ShooterGlobalFrame;
			bombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.AsVec2 = (bombardmentData.TargetPosition - bombardmentData.ShooterGlobalFrame.origin).AsVec2;
			bombardmentData.TargetAlignedShooterGlobalFrame.rotation.f.NormalizeWithoutChangingZ();
			bombardmentData.TargetAlignedShooterGlobalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			bombardmentData.LaunchGlobalPosition = bombardmentData.TargetAlignedShooterGlobalFrame.TransformToParent(base.MapScreen.PrefabEntityCache.GetLaunchEntitialFrameForSiegeEngine(machineType, side).origin);
			float lengthSquared = (bombardmentData.LaunchGlobalPosition.AsVec2 - bombardmentData.TargetPosition.AsVec2).LengthSquared;
			float num = MathF.Sqrt(lengthSquared);
			float num2 = bombardmentData.LaunchGlobalPosition.z - bombardmentData.TargetPosition.z;
			float num3 = bombardmentData.MissileSpeed * bombardmentData.MissileSpeed;
			float num4 = num3 * num3;
			float num5 = num4 - bombardmentData.Gravity * (bombardmentData.Gravity * lengthSquared - 2f * num2 * num3);
			if (num5 >= 0f)
			{
				bombardmentData.LaunchAngle = MathF.Atan((num3 - MathF.Sqrt(num5)) / (bombardmentData.Gravity * num));
			}
			else
			{
				bombardmentData.Gravity = 1f;
				num5 = num4 - bombardmentData.Gravity * (bombardmentData.Gravity * lengthSquared - 2f * num2 * num3);
				bombardmentData.LaunchAngle = MathF.Atan((num3 - MathF.Sqrt(num5)) / (bombardmentData.Gravity * num));
			}
			float num6 = bombardmentData.MissileSpeed * MathF.Cos(bombardmentData.LaunchAngle);
			bombardmentData.FlightDuration = num / num6;
			bombardmentData.TotalDuration = bombardmentData.RotationDuration + bombardmentData.ReloadDuration + bombardmentData.AimingDuration + bombardmentData.MissileLaunchDuration + bombardmentData.FlightDuration;
		}

		// Token: 0x0400020A RID: 522
		private const string CircleTag = "map_settlement_circle";

		// Token: 0x0400020B RID: 523
		private const string BannerPlaceHolderTag = "map_banner_placeholder";

		// Token: 0x0400020C RID: 524
		private const string MapSiegeEngineTag = "map_siege_engine";

		// Token: 0x0400020D RID: 525
		private const string MapBreachableWallTag = "map_breachable_wall";

		// Token: 0x0400020E RID: 526
		private const string MapDefenderEngineTag = "map_defensive_engine";

		// Token: 0x0400020F RID: 527
		private const string MapSiegeEngineRamTag = "map_siege_ram";

		// Token: 0x04000210 RID: 528
		private const string TownPhysicalTag = "bo_town";

		// Token: 0x04000211 RID: 529
		private const string MapSiegeEngineTowerTag = "map_siege_tower";

		// Token: 0x04000212 RID: 530
		private const string MapPreparationTag = "siege_preparation";

		// Token: 0x04000213 RID: 531
		private const string BurnedTag = "looted";

		// Token: 0x04000214 RID: 532
		private GameEntity[] _attackerRangedEngineSpawnEntities;

		// Token: 0x04000215 RID: 533
		private GameEntity[] _attackerBatteringRamSpawnEntities;

		// Token: 0x04000216 RID: 534
		private GameEntity[] _defenderBreachableWallEntitiesCacheForCurrentLevel;

		// Token: 0x04000217 RID: 535
		private GameEntity[] _attackerSiegeTowerSpawnEntities;

		// Token: 0x04000218 RID: 536
		private GameEntity[] _defenderRangedEngineSpawnEntitiesForAllLevels;

		// Token: 0x04000219 RID: 537
		private GameEntity[] _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel;

		// Token: 0x0400021A RID: 538
		private GameEntity[] _defenderBreachableWallEntitiesForAllLevels;

		// Token: 0x0400021C RID: 540
		private readonly List<ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>> _siegeRangedMachineEntities;

		// Token: 0x0400021D RID: 541
		private readonly List<ValueTuple<GameEntity, BattleSideEnum, int, MatrixFrame, GameEntity>> _siegeMeleeMachineEntities;

		// Token: 0x0400021E RID: 542
		private readonly List<ValueTuple<GameEntity, BattleSideEnum, int>> _siegeMissileEntities;

		// Token: 0x0400021F RID: 543
		private Dictionary<int, List<GameEntity>> _gateBannerEntitiesWithLevels;

		// Token: 0x04000220 RID: 544
		private uint _currentLevelMask;

		// Token: 0x04000221 RID: 545
		private MatrixFrame _hoveredSiegeEntityFrame = MatrixFrame.Identity;

		// Token: 0x04000222 RID: 546
		private GameEntity.UpgradeLevelMask _currentSettlementUpgradeLevelMask;

		// Token: 0x04000223 RID: 547
		private Scene _mapScene;

		// Token: 0x020000B5 RID: 181
		private struct SiegeBombardmentData
		{
			// Token: 0x04000392 RID: 914
			public Vec3 LaunchGlobalPosition;

			// Token: 0x04000393 RID: 915
			public Vec3 TargetPosition;

			// Token: 0x04000394 RID: 916
			public MatrixFrame ShooterGlobalFrame;

			// Token: 0x04000395 RID: 917
			public MatrixFrame TargetAlignedShooterGlobalFrame;

			// Token: 0x04000396 RID: 918
			public float MissileSpeed;

			// Token: 0x04000397 RID: 919
			public float Gravity;

			// Token: 0x04000398 RID: 920
			public float LaunchAngle;

			// Token: 0x04000399 RID: 921
			public float RotationDuration;

			// Token: 0x0400039A RID: 922
			public float ReloadDuration;

			// Token: 0x0400039B RID: 923
			public float AimingDuration;

			// Token: 0x0400039C RID: 924
			public float MissileLaunchDuration;

			// Token: 0x0400039D RID: 925
			public float FireDuration;

			// Token: 0x0400039E RID: 926
			public float FlightDuration;

			// Token: 0x0400039F RID: 927
			public float TotalDuration;
		}
	}
}

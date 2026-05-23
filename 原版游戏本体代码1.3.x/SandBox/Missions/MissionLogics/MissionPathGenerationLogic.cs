using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200007B RID: 123
	public class MissionPathGenerationLogic : MissionLogic
	{
		// Token: 0x060004FC RID: 1276 RVA: 0x000201A0 File Offset: 0x0001E3A0
		public MissionPathGenerationLogic(CharacterObject defaultDisguiseCharacter)
		{
			this._defaultDisguiseCharacter = defaultDisguiseCharacter;
			this._selectedPath = null;
			this._nearbyLeftSideUsableMachinesCache = new List<MissionPathGenerationLogic.UsableMachineData>();
			this._nearbyRightSideUsableMachinesCache = new List<MissionPathGenerationLogic.UsableMachineData>();
			this._allTargetAgentPointOfInterest = new List<MissionPathGenerationLogic.PointOfInterestBaseData>();
			this._crossRoadAgentData = new Dictionary<Agent, bool>();
			this._visitBarrelEntities = new List<GameEntity>();
			this._startAndFinishPointPool = new List<GameEntity>();
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x00020230 File Offset: 0x0001E430
		public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
		{
			if (userAgent.IsMainAgent)
			{
				GameEntity item = GameEntity.CreateFromWeakEntity(usedObject.GameEntity);
				if (this._visitBarrelEntities.Contains(item))
				{
					userAgent.SetActionChannel(0, ActionIndexCache.act_smithing_machine_anvil_start, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					this._visitBarrelEntities.Remove(item);
				}
			}
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x000202A0 File Offset: 0x0001E4A0
		private void SpawnDisguiseAgents()
		{
			foreach (MissionPathGenerationLogic.PointOfInterestBaseData pointOfInterestBaseData in this._selectedPath.Data)
			{
				MissionPathGenerationLogic.CrossRoadScoreData selectedCrossRoad;
				MissionPathGenerationLogic.StandingGuardSpawnData standingGuardSpawnPoint;
				MissionPathGenerationLogic.VisitPointNodeScoreData visitPointNodeScoreData;
				MissionPathGenerationLogic.LookBackPointData item;
				if ((selectedCrossRoad = pointOfInterestBaseData as MissionPathGenerationLogic.CrossRoadScoreData) != null)
				{
					this.SpawnCrossRoadAgents(selectedCrossRoad);
				}
				else if ((standingGuardSpawnPoint = pointOfInterestBaseData as MissionPathGenerationLogic.StandingGuardSpawnData) != null)
				{
					this.SpawnStandingGuards(standingGuardSpawnPoint);
				}
				else if ((visitPointNodeScoreData = pointOfInterestBaseData as MissionPathGenerationLogic.VisitPointNodeScoreData) != null)
				{
					this.SpawnVisitPointGuardsAndBlendPoints(visitPointNodeScoreData, true);
					this._allTargetAgentPointOfInterest.Add(visitPointNodeScoreData);
				}
				else if ((item = pointOfInterestBaseData as MissionPathGenerationLogic.LookBackPointData) != null)
				{
					this._allTargetAgentPointOfInterest.Add(item);
				}
			}
			this._allTargetAgentPointOfInterest = (from x in this._allTargetAgentPointOfInterest
				orderby x.GetLocationRatio()
				select x).ToList<MissionPathGenerationLogic.PointOfInterestBaseData>();
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x00020388 File Offset: 0x0001E588
		private void SpawnVisitPointGuardsAndBlendPoints(MissionPathGenerationLogic.VisitPointNodeScoreData visitPointData, bool useAsBarrelPoint)
		{
			this.FadeOutUserAgentsInUsableMachine(visitPointData.VisitPointData.MissionObject as UsableMachine);
			MatrixFrame globalFrame = visitPointData.VisitPointData.MissionObject.GameEntity.GetGlobalFrame();
			WorldFrame worldFrame = new WorldFrame(globalFrame.rotation, new WorldPosition(visitPointData.VisitPointData.MissionObject.GameEntity.Scene, globalFrame.origin));
			if (useAsBarrelPoint)
			{
				Vec3 groundVec = worldFrame.Origin.GetGroundVec3();
				float num = float.MaxValue;
				Vec3 vec = Vec3.Zero;
				int num2 = 0;
				while ((float)num2 < 360f)
				{
					worldFrame.Rotation.RotateAboutUp(0.017453292f);
					Vec3 lastPointOnNavigationMeshFromWorldPositionToDestination = Mission.Current.Scene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref worldFrame.Origin, worldFrame.Origin.AsVec2 + worldFrame.Rotation.f.AsVec2 * 30f);
					float num3 = worldFrame.Origin.AsVec2.Distance(lastPointOnNavigationMeshFromWorldPositionToDestination.AsVec2);
					if (num3 < num)
					{
						num = num3;
						vec = lastPointOnNavigationMeshFromWorldPositionToDestination;
					}
					num2++;
				}
				PathFaceRecord pathFaceRecord = new PathFaceRecord(-1, -1, -1);
				Mission.Current.Scene.GetNavMeshFaceIndex(ref pathFaceRecord, vec, true);
				Vec3 zero = Vec3.Zero;
				Mission.Current.Scene.GetNavMeshCenterPosition(pathFaceRecord.FaceIndex, ref zero);
				worldFrame.Origin.SetVec2(vec.AsVec2 + (zero.AsVec2 - vec.AsVec2) * 0.25f);
				float f = Vec3.AngleBetweenTwoVectors(groundVec - vec, worldFrame.Rotation.f);
				worldFrame.Rotation.RotateAboutUp(f.ToRadians());
				GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, "disguise_mission_interactable_barrel", worldFrame.ToGroundMatrixFrame(), true, "");
				this._visitBarrelEntities.Add(gameEntity);
				visitPointData.UsingAsInteractablePoint = true;
				visitPointData.VisitPointData.MissionObject = gameEntity.GetFirstScriptOfType<UsableMissionObject>();
			}
			else
			{
				Vec3 initialPosition = worldFrame.Origin.GetGroundVec3() - worldFrame.Rotation.f;
				Agent agent = this._disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(this._defaultDisguiseCharacter, initialPosition, worldFrame.Rotation.f.AsVec2.Normalized(), "_hideout_bandit", true);
				UsableMachine usableMachine = visitPointData.VisitPointData.MissionObject as UsableMachine;
				AnimationPoint animationPoint;
				if (usableMachine.StandingPoints.Any<StandingPoint>() && (animationPoint = usableMachine.StandingPoints[0] as AnimationPoint) != null)
				{
					Agent agent2 = agent;
					int channelNo = 0;
					ActionIndexCache actionIndexCache = ActionIndexCache.Create(animationPoint.LoopStartAction);
					agent2.SetActionChannel(channelNo, actionIndexCache, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
				}
			}
			Vec2 vec2 = visitPointData.ClosestPointToBlendPoint.AsVec2 - visitPointData.PossibleBlendPointPosition.AsVec2;
			this._disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(Settlement.CurrentSettlement.Culture.Beggar, visitPointData.PossibleBlendPointPosition.GetNavMeshVec3(), vec2.Normalized(), "_hideout_bandit", false).SetActionChannel(0, ActionIndexCache.act_beggar_idle, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x000206E8 File Offset: 0x0001E8E8
		private void SpawnStandingGuards(MissionPathGenerationLogic.StandingGuardSpawnData standingGuardSpawnPoint)
		{
			this.FadeOutUserAgentsInUsableMachine(standingGuardSpawnPoint.GuardPointData.MissionObject as UsableMachine);
			MatrixFrame globalFrame = standingGuardSpawnPoint.GuardPointData.MissionObject.GameEntity.GetGlobalFrame();
			this._disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(this._defaultDisguiseCharacter, globalFrame.origin, standingGuardSpawnPoint.SpawnDirection.Normalized(), "_hideout_bandit", true);
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x00020750 File Offset: 0x0001E950
		private void SpawnCrossRoadAgents(MissionPathGenerationLogic.CrossRoadScoreData selectedCrossRoad)
		{
			this.FadeOutUserAgentsInUsableMachine(selectedCrossRoad.LeftNode.MissionObject as UsableMachine);
			this.FadeOutUserAgentsInUsableMachine(selectedCrossRoad.RightNode.MissionObject as UsableMachine);
			MatrixFrame matrixFrame = ((MBRandom.RandomFloat < 0.5f) ? selectedCrossRoad.LeftNode.MissionObject.GameEntity.GetGlobalFrame() : selectedCrossRoad.RightNode.MissionObject.GameEntity.GetGlobalFrame());
			Agent agent = this._disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(this._defaultDisguiseCharacter, matrixFrame.origin, matrixFrame.rotation.f.AsVec2.Normalized(), "_hideout_bandit", true);
			this._crossRoadAgentData.Add(agent, false);
			ScriptBehavior.AddTargetWithDelegate(agent, this.CrossRoadAgentSelectTargetDelegate(selectedCrossRoad), new ScriptBehavior.OnTargetReachedWaitDelegate(this.CrossRoadAgentWaitDelegate), new ScriptBehavior.OnTargetReachedDelegate(this.CrossRoadAgentOnTargetReachDelegate), 0f);
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x00020836 File Offset: 0x0001EA36
		private void CrossRoadAgentWaitDelegate(Agent agent, ref float waitTimeInSeconds)
		{
			waitTimeInSeconds = (float)MBRandom.RandomInt(6, 30);
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x00020843 File Offset: 0x0001EA43
		private bool CrossRoadAgentOnTargetReachDelegate(Agent agent1, ref Agent targetAgent, ref UsableMachine machine, ref WorldFrame frame)
		{
			this._crossRoadAgentData[agent1] = !this._crossRoadAgentData[agent1];
			return true;
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x00020861 File Offset: 0x0001EA61
		private ScriptBehavior.SelectTargetDelegate CrossRoadAgentSelectTargetDelegate(MissionPathGenerationLogic.CrossRoadScoreData selectedCrossRoad)
		{
			return delegate(Agent agent1, ref Agent targetAgent, ref UsableMachine machine, ref WorldFrame frame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
			{
				customTargetReachedRangeThreshold = 2.5f;
				customTargetReachedRotationThreshold = 0.8f;
				if (this._crossRoadAgentData[agent1])
				{
					WorldPosition origin = new WorldPosition(Mission.Current.Scene, selectedCrossRoad.LeftNode.MissionObject.GameEntity.GlobalPosition);
					frame = new WorldFrame(selectedCrossRoad.LeftNode.MissionObject.GameEntity.GetGlobalFrame().rotation, origin);
				}
				else
				{
					WorldPosition origin2 = new WorldPosition(Mission.Current.Scene, selectedCrossRoad.RightNode.MissionObject.GameEntity.GlobalPosition);
					frame = new WorldFrame(selectedCrossRoad.RightNode.MissionObject.GameEntity.GetGlobalFrame().rotation, origin2);
				}
				return true;
			};
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x00020884 File Offset: 0x0001EA84
		private float CalculateCrossRoadScoreForUsableMachines(MissionPathGenerationLogic.UsableMachineData leftSideUsableMachineData, MissionPathGenerationLogic.UsableMachineData rightSideUsableMachineData, NavigationPath originalPath, WorldPosition pathNodeStartPosition, WorldPosition pathNodeEndPosition)
		{
			if (leftSideUsableMachineData.PathDistanceRatio < 0.1f || rightSideUsableMachineData.PathDistanceRatio < 0.1f)
			{
				return 0f;
			}
			if (leftSideUsableMachineData.ClosestPointToPath.Distance(rightSideUsableMachineData.ClosestPointToPath) > pathNodeStartPosition.GetNavMeshVec3().Distance(pathNodeEndPosition.GetNavMeshVec3()))
			{
				return 0f;
			}
			this._tempWorldPosition.SetVec2(leftSideUsableMachineData.MissionObject.GameEntity.GlobalPosition.AsVec2);
			this._tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition = this._tempWorldPosition;
			this._tempWorldPosition.SetVec2(rightSideUsableMachineData.MissionObject.GameEntity.GlobalPosition.AsVec2);
			this._tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition2 = this._tempWorldPosition;
			float num;
			Mission.Current.Scene.GetPathDistanceBetweenPositions(ref tempWorldPosition, ref tempWorldPosition2, 0.37f, out num);
			if (num > (float)this.CrossRoadMaximumDistance || num < (float)this.CrossRoadMinimumDistance)
			{
				return 0f;
			}
			this._tempWorldPosition.SetVec2(leftSideUsableMachineData.ClosestPointToPath);
			this._tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition3 = this._tempWorldPosition;
			this._tempWorldPosition.SetVec2(rightSideUsableMachineData.ClosestPointToPath);
			this._tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition4 = this._tempWorldPosition;
			float num2;
			base.Mission.Scene.GetPathDistanceBetweenPositions(ref tempWorldPosition, ref tempWorldPosition2, 0.37f, out num2);
			pathNodeStartPosition.AsVec2.Distance(pathNodeEndPosition.AsVec2);
			float num3;
			Mission.Current.Scene.GetPathDistanceBetweenPositions(ref tempWorldPosition, ref tempWorldPosition3, 0.37f, out num3);
			float num4;
			Mission.Current.Scene.GetPathDistanceBetweenPositions(ref tempWorldPosition4, ref tempWorldPosition2, 0.37f, out num4);
			if (num3 > num4 && num4 / num3 < 0.2f)
			{
				return 0f;
			}
			if (num4 > num3 && num3 / num4 < 0.2f)
			{
				return 0f;
			}
			float value = (pathNodeEndPosition.AsVec2 - pathNodeStartPosition.AsVec2).AngleBetween(tempWorldPosition.AsVec2 - tempWorldPosition2.AsVec2).ToDegrees();
			if (Math.Abs(value) > 150f || Math.Abs(value) < 30f)
			{
				return 0f;
			}
			float num5;
			if (Math.Abs(value) > 90f)
			{
				num5 = MBMath.Map(Math.Abs(value), 90f, 150f, 1f, 0f);
			}
			else
			{
				num5 = MBMath.Map(Math.Abs(value), 30f, 90f, 0f, 1f);
			}
			float num6 = MBMath.Map(num3 + num4, 0f, 20f, 0f, 1f);
			return num5 + num6;
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x00020B48 File Offset: 0x0001ED48
		private float CalculateVisitPointScore(MissionPathGenerationLogic.UsableMachineData usableMachineData, NavigationPath originalPath, WorldPosition pathNodeStart, WorldPosition pathNodeEnd, out Vec3 possibleBlendPointPosition, out float startingAngle, out Vec2 pathToVisitPointZero, out Vec2 closestPointToPath)
		{
			possibleBlendPointPosition = Vec3.Invalid;
			startingAngle = 0f;
			pathToVisitPointZero = Vec2.Zero;
			closestPointToPath = Vec2.Invalid;
			if (usableMachineData.PathDistanceRatio < 0.2f || usableMachineData.PathDistanceRatio > 0.9f)
			{
				return 0f;
			}
			WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, usableMachineData.MissionObject.GameEntity.GlobalPosition);
			this._tempWorldPosition.SetVec2(usableMachineData.ClosestPointToPath);
			this._tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition = this._tempWorldPosition;
			NavigationPath navigationPath = new NavigationPath();
			base.Mission.Scene.GetPathBetweenAIFaces(pathNodeStart.GetNearestNavMesh(), worldPosition.GetNearestNavMesh(), pathNodeStart.AsVec2, worldPosition.AsVec2, 0f, navigationPath, new int[] { this._disabledFaceId });
			Vec2 vec = pathNodeStart.AsVec2 + (pathNodeEnd.AsVec2 - pathNodeStart.AsVec2) * 0.5f;
			this._tempWorldPosition.SetVec2(vec);
			this._tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition2 = this._tempWorldPosition;
			Vec2 vec2 = navigationPath[0];
			float num = vec2.Distance(vec);
			for (int i = 0; i < navigationPath.Size - 1; i++)
			{
				Vec2 vec3 = navigationPath[i];
				Vec2 v = navigationPath[i + 1];
				num += vec3.Distance(v);
			}
			if (num < (float)this.MinimumVisitPointDistance || num > (float)this.MaximumVisitPointDistance)
			{
				return 0f;
			}
			float num2 = 0f;
			vec2 = pathNodeEnd.GetNavMeshVec3().AsVec2 - pathNodeStart.GetNavMeshVec3().AsVec2;
			startingAngle = vec2.AngleBetween(navigationPath[0] - tempWorldPosition2.GetNavMeshVec3().AsVec2).ToDegrees();
			pathToVisitPointZero = navigationPath[0];
			if (Math.Abs(startingAngle) < 90f && Math.Abs(startingAngle) > 30f)
			{
				for (int j = 0; j < navigationPath.Size - 1; j++)
				{
					Vec2 v2 = ((j == 0) ? tempWorldPosition.AsVec2 : navigationPath[j - 1]);
					Vec2 vec4 = navigationPath[j];
					Vec2 v3 = navigationPath[j + 1];
					vec2 = vec4 - v2;
					float value = vec2.AngleBetween(v3 - vec4).ToDegrees();
					num2 += MBMath.Map(Math.Abs(value), 0f, 90f, 1f, 0f);
					if ((float)j > (float)(navigationPath.Size - 1) * 0.25f && !possibleBlendPointPosition.IsValid)
					{
						this._tempWorldPosition.SetVec2(vec4);
						this._tempWorldPosition.GetNavMeshZ();
						WorldPosition tempWorldPosition3 = this._tempWorldPosition;
						Vec3 navMeshVec = tempWorldPosition3.GetNavMeshVec3();
						Vec3 vec5 = Vec3.Invalid;
						PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
						int num3 = 0;
						float num4 = float.MaxValue;
						do
						{
							num3++;
							if (num3 > 150)
							{
								break;
							}
							vec5 = base.Mission.GetRandomPositionAroundPoint(navMeshVec, 2f, 6f, true);
							base.Mission.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, vec5, true);
							if (nullFaceRecord.FaceGroupIndex == this._disabledFaceId)
							{
								for (int k = 0; k < navigationPath.Size - 1; k++)
								{
									Vec2 vec6 = navigationPath[k];
									Vec2 vec7 = navigationPath[k + 1];
									vec2 = vec5.AsVec2;
									Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(vec6, vec7, vec2);
									vec2 = vec5.AsVec2;
									float num5 = vec2.Distance(closestPointOnLineSegmentToPoint);
									if (num5 < num4)
									{
										closestPointToPath = closestPointOnLineSegmentToPoint;
										num4 = num5;
									}
								}
							}
						}
						while (nullFaceRecord.FaceGroupIndex != this._disabledFaceId || num4 < 1.5f);
						if (num3 < 150)
						{
							possibleBlendPointPosition = vec5;
						}
					}
				}
				num2 /= (float)navigationPath.Size;
				if (possibleBlendPointPosition.IsValid)
				{
					vec2 = possibleBlendPointPosition.AsVec2;
					if (vec2.Distance(worldPosition.AsVec2) >= this.MinimumDistanceToBlendPointToVisitPoint)
					{
						for (int l = 0; l < originalPath.Size - 1; l++)
						{
							Vec2 v4 = originalPath[l];
							for (int m = 0; m < navigationPath.Size - 1; m++)
							{
								vec2 = navigationPath[m];
								if (vec2.Distance(v4) < 2f)
								{
									return 0f;
								}
							}
						}
						float num6 = (float)(this.MaximumVisitPointDistance + this.MinimumVisitPointDistance) * 0.5f;
						float num7;
						if (num > num6)
						{
							num7 = MBMath.Map(num, num6, (float)this.MaximumVisitPointDistance, 0.5f, 1f);
						}
						else
						{
							num7 = MBMath.Map(num, (float)this.MinimumVisitPointDistance, num6, 0f, 0.5f);
						}
						float num8 = 0f;
						UsableMachine usableMachine = usableMachineData.MissionObject as UsableMachine;
						if (usableMachine.StandingPoints.Count > 0)
						{
							if (usableMachine.StandingPoints.Count != usableMachine.StandingPoints.Count((StandingPoint x) => x.HasAlternative()))
							{
								if (usableMachine.StandingPoints.Count(delegate(StandingPoint x)
								{
									AnimationPoint animationPoint;
									return (animationPoint = x as AnimationPoint) != null && animationPoint.PairEntity != null;
								}) == 2)
								{
									num8 = 2f;
								}
							}
						}
						float num9 = ((usableMachineData.PathDistanceRatio > 0.75f) ? MBMath.Map(usableMachineData.PathDistanceRatio, 0.75f, 1f, 1f, 0f) : MBMath.Map(usableMachineData.PathDistanceRatio, 0f, 0.75f, 0f, 1f));
						return 5f + num2 + num7 + num9 + num8;
					}
				}
				return 0f;
			}
			return 0f;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x0002113C File Offset: 0x0001F33C
		private float CalculateSpawnGuardScore(MissionPathGenerationLogic.UsableMachineData guardSpawnPointData, out Vec2 spawnRotation)
		{
			spawnRotation = Vec2.Zero;
			UsableMachine usableMachine = guardSpawnPointData.MissionObject as UsableMachine;
			if (usableMachine.PilotAgent != null)
			{
				return 0f;
			}
			using (List<StandingPoint>.Enumerator enumerator = usableMachine.StandingPoints.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.UserAgent != null)
					{
						return 0f;
					}
				}
			}
			if (guardSpawnPointData.PathDistanceRatio < MissionPathGenerationLogic.MinimumGuardSpawnPathRatio)
			{
				return 0f;
			}
			float num = guardSpawnPointData.ClosestPointToPath.Distance(guardSpawnPointData.MissionObject.GameEntity.GlobalPosition.AsVec2);
			if (num < 3f)
			{
				return 0f;
			}
			float result;
			if (num > 5f)
			{
				result = MBMath.Map(num, 5f, 30f, 1f, 0f);
			}
			else
			{
				result = MBMath.Map(num, 3f, 5f, 0f, 1f);
			}
			spawnRotation = guardSpawnPointData.ClosestPointToPath - guardSpawnPointData.MissionObject.GameEntity.GlobalPosition.AsVec2;
			return result;
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x00021284 File Offset: 0x0001F484
		protected override void OnEndMission()
		{
			this._nearbyLeftSideUsableMachinesCache = null;
			this._nearbyRightSideUsableMachinesCache = null;
			this._allTargetAgentPointOfInterest = null;
			this._crossRoadAgentData = null;
			this._startAndFinishPointPool = null;
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x000212AC File Offset: 0x0001F4AC
		public void InitializeBehavior()
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
			if (gameEntity != null)
			{
				NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
				this._disabledFaceId = firstScriptOfType.DisableFaceWithId;
			}
			this._disguiseMissionLogic = Mission.Current.GetMissionBehavior<DisguiseMissionLogic>();
			Mission.Current.Scene.GetAllEntitiesWithScriptComponent<PassageUsePoint>(ref this._startAndFinishPointPool);
			for (int i = this._startAndFinishPointPool.Count - 1; i >= 0; i--)
			{
				Location toLocation = this._startAndFinishPointPool[i].GetFirstScriptOfType<PassageUsePoint>().ToLocation;
				string text = ((toLocation != null) ? toLocation.StringId : null);
				if (text == null || text == "lordshall" || text == "prison")
				{
					this._startAndFinishPointPool.RemoveAt(i);
				}
			}
			Mission.Current.Scene.GetAllEntitiesWithScriptComponent<CastleGate>(ref this._startAndFinishPointPool);
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_player_conversation"))
			{
				this._startAndFinishPointPool.Add(item);
			}
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x000213E4 File Offset: 0x0001F5E4
		public override void OnMissionTick(float dt)
		{
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x000213E8 File Offset: 0x0001F5E8
		private void FadeOutUserAgentsInUsableMachine(UsableMachine usableMachine)
		{
			if (usableMachine.PilotAgent != null)
			{
				usableMachine.PilotAgent.FadeOut(true, true);
			}
			foreach (StandingPoint standingPoint in usableMachine.StandingPoints)
			{
				if (standingPoint.UserAgent != null)
				{
					standingPoint.UserAgent.FadeOut(true, true);
				}
			}
			usableMachine.SetDisabled(true);
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x00021468 File Offset: 0x0001F668
		private MissionPathGenerationLogic.PointOfInterestScorePair CreatePathScorePair(MissionPathGenerationLogic.NavigationPathData pathData)
		{
			List<MissionPathGenerationLogic.VisitPointNodeScoreData> list = this.GetVisitPoints(pathData);
			List<MissionPathGenerationLogic.CrossRoadScoreData> list2 = this.GetCrossRoadPoints(pathData);
			if (list.Count == 0 && list2.Count == 0)
			{
				return null;
			}
			List<MissionPathGenerationLogic.PointOfInterestBaseData> list3 = new List<MissionPathGenerationLogic.PointOfInterestBaseData>();
			list.Shuffle<MissionPathGenerationLogic.VisitPointNodeScoreData>();
			list2.Shuffle<MissionPathGenerationLogic.CrossRoadScoreData>();
			if (list2.Count > 20)
			{
				list2 = (from x in list2
					orderby x.Score descending
					select x).Take(20).ToList<MissionPathGenerationLogic.CrossRoadScoreData>();
				list2.Shuffle<MissionPathGenerationLogic.CrossRoadScoreData>();
			}
			if (list.Count > 10)
			{
				list = (from x in list
					orderby x.Score descending
					select x).Take(10).ToList<MissionPathGenerationLogic.VisitPointNodeScoreData>();
				list.Shuffle<MissionPathGenerationLogic.VisitPointNodeScoreData>();
			}
			list3.AddRange(list);
			list3.AddRange(list2);
			list3.Shuffle<MissionPathGenerationLogic.PointOfInterestBaseData>();
			Stack<ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>> stack = new Stack<ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>>();
			stack.Push(new ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>(new MissionPathGenerationLogic.PointOfInterestScorePair(pathData, new List<MissionPathGenerationLogic.PointOfInterestBaseData>(), 0f), 0));
			return this.CreatePathDataWith(stack, list3);
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x0002156C File Offset: 0x0001F76C
		private MissionPathGenerationLogic.PointOfInterestScorePair CreatePathDataWith(Stack<ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>> stack, List<MissionPathGenerationLogic.PointOfInterestBaseData> pointOfInterestData)
		{
			MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair = null;
			while (stack.Count > 0)
			{
				ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int> valueTuple = stack.Pop();
				int i = valueTuple.Item2;
				while (i < pointOfInterestData.Count)
				{
					MissionPathGenerationLogic.PointOfInterestBaseData data = pointOfInterestData[i];
					if (valueTuple.Item1.Data.All((MissionPathGenerationLogic.PointOfInterestBaseData x) => !x.IsInRadius(data)))
					{
						MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair2 = valueTuple.Item1.Clone();
						pointOfInterestScorePair2.AddToData(data);
						if (i + 1 < pointOfInterestData.Count)
						{
							stack.Push(new ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>(valueTuple.Item1, i + 1));
							stack.Push(new ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>(pointOfInterestScorePair2, i + 1));
						}
						if (pointOfInterestScorePair == null || pointOfInterestScorePair2.IsBetterThan(pointOfInterestScorePair))
						{
							pointOfInterestScorePair = pointOfInterestScorePair2;
						}
						if (pointOfInterestScorePair2.IsSufficient())
						{
							return pointOfInterestScorePair2;
						}
						i++;
						valueTuple = new ValueTuple<MissionPathGenerationLogic.PointOfInterestScorePair, int>(pointOfInterestScorePair2, i);
						break;
					}
					else
					{
						i++;
					}
				}
				if (i == pointOfInterestData.Count && valueTuple.Item1.IsSufficient())
				{
					return valueTuple.Item1;
				}
			}
			return pointOfInterestScorePair;
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x00021674 File Offset: 0x0001F874
		private MissionPathGenerationLogic.PointOfInterestScorePair GetRandomPath()
		{
			MissionPathGenerationLogic.PointOfInterestScorePair pathInternal = this.GetPathInternal();
			if (pathInternal != null)
			{
				if (MissionPathGenerationLogic.MaximumStandingGuardCountInPath > 0)
				{
					this.AddStandingGuardsToThePath(pathInternal);
				}
				if (MissionPathGenerationLogic.MaximumLookBackPointCountInPath > 0)
				{
					this.AddLookBackPointsToThePath(pathInternal);
				}
			}
			return pathInternal;
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x000216AC File Offset: 0x0001F8AC
		private void AddLookBackPointsToThePath(MissionPathGenerationLogic.PointOfInterestScorePair path)
		{
			Dictionary<int, float> dictionary = new Dictionary<int, float>();
			int num = (int)((float)path.PathData.Path.Size * 0.25f);
			while ((float)num < (float)path.PathData.Path.Size * 0.9f)
			{
				Vec2 key = path.PathData.Path[num];
				Vec2 key2 = path.PathData.Path[num + 1];
				if (key.IsNonZero() && key2.IsNonZero())
				{
					float num2 = path.PathData.PathNodeAndDistances[key] / path.PathData.TotalDistance;
					float num3 = path.PathData.PathNodeAndDistances[key2] / path.PathData.TotalDistance;
					float num4 = (num2 + num3) * 0.5f;
					float num5 = 0f;
					int num6 = 0;
					foreach (MissionPathGenerationLogic.PointOfInterestBaseData pointOfInterestBaseData in path.Data)
					{
						float locationRatio = pointOfInterestBaseData.GetLocationRatio();
						if (locationRatio > num4 - 0.1f && locationRatio < num4 + 0.1f)
						{
							num5 += Math.Abs(num4 - locationRatio);
							num6++;
						}
					}
					if (num6 > 0)
					{
						num5 /= (float)num6;
					}
					dictionary.Add(num, num5);
				}
				num++;
			}
			if (dictionary.Any<KeyValuePair<int, float>>())
			{
				List<KeyValuePair<int, float>> list = (from x in dictionary
					orderby x.Value descending
					select x).ToList<KeyValuePair<int, float>>();
				int num7 = 0;
				int num8 = ((MissionPathGenerationLogic.MaximumLookBackPointCountInPath > 0) ? MBRandom.RandomInt((int)((float)MissionPathGenerationLogic.MaximumLookBackPointCountInPath * 0.5f), MissionPathGenerationLogic.MaximumLookBackPointCountInPath) : 0);
				if (num8 > 0)
				{
					this._tempWorldPosition = new WorldPosition(Mission.Current.Scene, path.PathData.StartingGameEntity.GlobalPosition);
					this._tempWorldPosition.GetNavMeshZ();
					for (int i = 0; i < path.PathData.Path.Size - 1; i++)
					{
						Vec2 vec = path.PathData.Path[i];
						Vec2 vec2 = path.PathData.Path[i + 1];
						this._tempWorldPosition.SetVec2(vec);
						this._tempWorldPosition.GetNavMeshZ();
						this._tempWorldPosition.SetVec2(vec2);
						this._tempWorldPosition.GetNavMeshZ();
						if (num7 != num8)
						{
							foreach (KeyValuePair<int, float> keyValuePair in list)
							{
								int key3 = keyValuePair.Key;
								if (i == key3)
								{
									Vec2 vec3 = (vec + vec2) * 0.5f;
									float pathDistanceRatio = (path.PathData.PathNodeAndDistances[vec] / path.PathData.TotalDistance + path.PathData.PathNodeAndDistances[vec2] / path.PathData.TotalDistance) * 0.5f;
									Vec2 vec4 = vec3 + (vec2 - vec).Normalized();
									this._tempWorldPosition.SetVec2(vec3);
									this._tempWorldPosition.GetNavMeshZ();
									WorldPosition tempWorldPosition = this._tempWorldPosition;
									this._tempWorldPosition.SetVec2(vec4);
									this._tempWorldPosition.GetNavMeshZ();
									WorldPosition tempWorldPosition2 = this._tempWorldPosition;
									MissionPathGenerationLogic.LookBackPointData newData = new MissionPathGenerationLogic.LookBackPointData(tempWorldPosition, tempWorldPosition2, pathDistanceRatio);
									if (path.Data.All((MissionPathGenerationLogic.PointOfInterestBaseData x) => !x.IsInRadius(newData)))
									{
										path.AddToData(newData);
										num7++;
									}
									if (num7 == num8)
									{
										break;
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x00021AA8 File Offset: 0x0001FCA8
		private void AddStandingGuardsToThePath(MissionPathGenerationLogic.PointOfInterestScorePair path)
		{
			int num = (int)(path.PathData.TotalDistance / 10f);
			num = MBMath.ClampInt(num, MissionPathGenerationLogic.MinimumStandingGuardCountInPath, MissionPathGenerationLogic.MaximumStandingGuardCountInPath);
			List<MissionPathGenerationLogic.StandingGuardSpawnData> guardSpawnPoints = this.GetGuardSpawnPoints(path.PathData);
			int num2 = 0;
			Func<MissionPathGenerationLogic.StandingGuardSpawnData, bool> <>9__0;
			for (int i = 0; i < guardSpawnPoints.Count; i++)
			{
				IReadOnlyList<MissionPathGenerationLogic.StandingGuardSpawnData> e = guardSpawnPoints;
				Func<MissionPathGenerationLogic.StandingGuardSpawnData, bool> predicate;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = (MissionPathGenerationLogic.StandingGuardSpawnData x) => path.Data.All((MissionPathGenerationLogic.PointOfInterestBaseData y) => !y.IsInRadius(x)));
				}
				MissionPathGenerationLogic.StandingGuardSpawnData randomElementWithPredicate = e.GetRandomElementWithPredicate(predicate);
				if (randomElementWithPredicate != null)
				{
					path.AddToData(randomElementWithPredicate);
					num2++;
					if (num2 >= num)
					{
						break;
					}
				}
			}
		}

		// Token: 0x06000511 RID: 1297 RVA: 0x00021B58 File Offset: 0x0001FD58
		public List<MissionPathGenerationLogic.PointOfInterestScorePair> GetAllPossiblePaths()
		{
			List<MissionPathGenerationLogic.PointOfInterestScorePair> list = new List<MissionPathGenerationLogic.PointOfInterestScorePair>();
			List<UsableMachine> usablePoints = base.Mission.GetMissionBehavior<MissionAgentHandler>().UsablePoints;
			for (int i = 0; i < this._startAndFinishPointPool.Count - 1; i++)
			{
				for (int j = i + 1; j < this._startAndFinishPointPool.Count; j++)
				{
					GameEntity gameEntity = this._startAndFinishPointPool[i];
					GameEntity gameEntity2 = this._startAndFinishPointPool[j];
					MissionPathGenerationLogic.NavigationPathData navigationPathData = new MissionPathGenerationLogic.NavigationPathData(usablePoints, gameEntity, gameEntity2, this._disabledFaceId);
					this._tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity.GlobalPosition);
					this._tempWorldPosition.GetNavMeshZ();
					if (navigationPathData.TotalDistance < (float)MissionPathGenerationLogic.MaximumPathDistance && navigationPathData.TotalDistance > (float)MissionPathGenerationLogic.MinimumPathDistance)
					{
						MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair = this.CreatePathScorePair(navigationPathData);
						if (pointOfInterestScorePair != null && pointOfInterestScorePair.Score > (float)MissionPathGenerationLogic.ScoreToAchieve)
						{
							if (MissionPathGenerationLogic.MaximumStandingGuardCountInPath > 0)
							{
								this.AddStandingGuardsToThePath(pointOfInterestScorePair);
							}
							if (MissionPathGenerationLogic.MaximumLookBackPointCountInPath > 0)
							{
								this.AddLookBackPointsToThePath(pointOfInterestScorePair);
							}
							list.Add(pointOfInterestScorePair);
						}
					}
					MissionPathGenerationLogic.NavigationPathData navigationPathData2 = navigationPathData.ReverseClone();
					this._tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity2.GlobalPosition);
					this._tempWorldPosition.GetNavMeshZ();
					if (navigationPathData2.TotalDistance < (float)MissionPathGenerationLogic.MaximumPathDistance && navigationPathData2.TotalDistance > (float)MissionPathGenerationLogic.MinimumPathDistance)
					{
						MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair2 = this.CreatePathScorePair(navigationPathData2);
						if (pointOfInterestScorePair2 != null && pointOfInterestScorePair2.Score > (float)MissionPathGenerationLogic.ScoreToAchieve)
						{
							if (MissionPathGenerationLogic.MaximumStandingGuardCountInPath > 0)
							{
								this.AddStandingGuardsToThePath(pointOfInterestScorePair2);
							}
							if (MissionPathGenerationLogic.MaximumLookBackPointCountInPath > 0)
							{
								this.AddLookBackPointsToThePath(pointOfInterestScorePair2);
							}
							list.Add(pointOfInterestScorePair2);
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x00021D06 File Offset: 0x0001FF06
		public bool IsOnLeftSide(Vec2 lineA, Vec2 lineB, Vec2 point)
		{
			return (lineB.x - lineA.x) * (point.y - lineA.y) - (lineB.y - lineA.y) * (point.x - lineA.x) > 0f;
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x00021D48 File Offset: 0x0001FF48
		private MissionPathGenerationLogic.PointOfInterestScorePair GetPathInternal()
		{
			List<UsableMachine> usablePoints = base.Mission.GetMissionBehavior<MissionAgentHandler>().UsablePoints;
			MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair = null;
			for (int i = 0; i < this._startAndFinishPointPool.Count - 1; i++)
			{
				for (int j = i + 1; j < this._startAndFinishPointPool.Count; j++)
				{
					GameEntity gameEntity = this._startAndFinishPointPool[i];
					GameEntity gameEntity2 = this._startAndFinishPointPool[j];
					MissionPathGenerationLogic.NavigationPathData navigationPathData = new MissionPathGenerationLogic.NavigationPathData(usablePoints, gameEntity, gameEntity2, this._disabledFaceId);
					this._tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity.GlobalPosition);
					this._tempWorldPosition.GetNavMeshZ();
					if (navigationPathData.TotalDistance < (float)MissionPathGenerationLogic.MaximumPathDistance && navigationPathData.TotalDistance > (float)MissionPathGenerationLogic.MinimumPathDistance)
					{
						MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair2 = this.CreatePathScorePair(navigationPathData);
						if (pointOfInterestScorePair2 != null)
						{
							if (pointOfInterestScorePair2.IsSufficient())
							{
								this._currentStarting = gameEntity;
								this._currentEnding = gameEntity2;
								return pointOfInterestScorePair2;
							}
							if (pointOfInterestScorePair == null || pointOfInterestScorePair2.IsBetterThan(pointOfInterestScorePair))
							{
								pointOfInterestScorePair = pointOfInterestScorePair2;
							}
						}
					}
					MissionPathGenerationLogic.NavigationPathData navigationPathData2 = navigationPathData.ReverseClone();
					this._tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity2.GlobalPosition);
					this._tempWorldPosition.GetNavMeshZ();
					if (navigationPathData2.TotalDistance < (float)MissionPathGenerationLogic.MaximumPathDistance && navigationPathData2.TotalDistance > (float)MissionPathGenerationLogic.MinimumPathDistance)
					{
						MissionPathGenerationLogic.PointOfInterestScorePair pointOfInterestScorePair3 = this.CreatePathScorePair(navigationPathData2);
						if (pointOfInterestScorePair3 != null)
						{
							if (pointOfInterestScorePair3.IsSufficient())
							{
								this._currentStarting = gameEntity2;
								this._currentEnding = gameEntity;
								return pointOfInterestScorePair3;
							}
							if (pointOfInterestScorePair == null || pointOfInterestScorePair3.IsBetterThan(pointOfInterestScorePair))
							{
								pointOfInterestScorePair = pointOfInterestScorePair3;
							}
						}
					}
				}
			}
			if (pointOfInterestScorePair != null)
			{
				this._currentStarting = pointOfInterestScorePair.PathData.StartingGameEntity;
				this._currentEnding = pointOfInterestScorePair.PathData.EndingGameEntity;
			}
			return pointOfInterestScorePair;
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x00021F04 File Offset: 0x00020104
		private List<MissionPathGenerationLogic.StandingGuardSpawnData> GetGuardSpawnPoints(MissionPathGenerationLogic.NavigationPathData pathData)
		{
			List<MissionPathGenerationLogic.StandingGuardSpawnData> list = new List<MissionPathGenerationLogic.StandingGuardSpawnData>();
			foreach (MissionPathGenerationLogic.UsableMachineData usableMachineData in pathData.ValidUsableMachinesData)
			{
				Vec2 spawnDirection;
				float num = this.CalculateSpawnGuardScore(usableMachineData, out spawnDirection);
				if (num > 0f)
				{
					list.Add(new MissionPathGenerationLogic.StandingGuardSpawnData(usableMachineData, spawnDirection, num));
				}
			}
			return list;
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x00021F78 File Offset: 0x00020178
		private List<MissionPathGenerationLogic.VisitPointNodeScoreData> GetVisitPoints(MissionPathGenerationLogic.NavigationPathData pathData)
		{
			List<MissionPathGenerationLogic.VisitPointNodeScoreData> list = new List<MissionPathGenerationLogic.VisitPointNodeScoreData>();
			NavigationPath path = pathData.Path;
			for (int i = 0; i < path.Size - 1; i++)
			{
				Vec2 vec = path[i];
				Vec2 vec2 = path[i + 1];
				this._tempWorldPosition.SetVec2(vec);
				this._tempWorldPosition.GetNavMeshZ();
				WorldPosition tempWorldPosition = this._tempWorldPosition;
				this._tempWorldPosition.SetVec2(vec2);
				this._tempWorldPosition.GetNavMeshZ();
				WorldPosition tempWorldPosition2 = this._tempWorldPosition;
				foreach (MissionPathGenerationLogic.UsableMachineData usableMachineData in pathData.ValidUsableMachinesData)
				{
					if (!usableMachineData.IsAlreadyAddedToPath)
					{
						Vec3 vec3;
						float startingAngle;
						Vec2 pathToVisitPoint;
						Vec2 vec4;
						float num = this.CalculateVisitPointScore(usableMachineData, path, tempWorldPosition, tempWorldPosition2, out vec3, out startingAngle, out pathToVisitPoint, out vec4);
						if (num > 0f)
						{
							Vec2 vec5 = vec + (vec2 - vec) * 0.5f;
							this._tempWorldPosition.SetVec2(vec);
							this._tempWorldPosition.GetNavMeshZ();
							this._tempWorldPosition.SetVec2(vec5);
							this._tempWorldPosition.GetNavMeshZ();
							WorldPosition tempWorldPosition3 = this._tempWorldPosition;
							this._tempWorldPosition.SetVec2(vec2);
							this._tempWorldPosition.GetNavMeshVec3();
							this._tempWorldPosition.SetVec2(vec3.AsVec2);
							this._tempWorldPosition.GetNavMeshZ();
							WorldPosition tempWorldPosition4 = this._tempWorldPosition;
							this._tempWorldPosition.SetVec2(vec4);
							this._tempWorldPosition.GetNavMeshZ();
							WorldPosition tempWorldPosition5 = this._tempWorldPosition;
							float visitPointPathStartPointPathRatio = (pathData.PathNodeAndDistances[vec] / pathData.TotalDistance + pathData.PathNodeAndDistances[vec2] / pathData.TotalDistance) * 0.5f;
							list.Add(new MissionPathGenerationLogic.VisitPointNodeScoreData(usableMachineData, tempWorldPosition4, tempWorldPosition3, visitPointPathStartPointPathRatio, num, startingAngle, tempWorldPosition, tempWorldPosition2, pathToVisitPoint, tempWorldPosition5));
							usableMachineData.IsAlreadyAddedToPath = true;
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x0002218C File Offset: 0x0002038C
		private List<MissionPathGenerationLogic.CrossRoadScoreData> GetCrossRoadPoints(MissionPathGenerationLogic.NavigationPathData pathData)
		{
			List<MissionPathGenerationLogic.CrossRoadScoreData> list = new List<MissionPathGenerationLogic.CrossRoadScoreData>();
			for (int i = 0; i < pathData.Path.Size - 1; i++)
			{
				this._nearbyLeftSideUsableMachinesCache.Clear();
				this._nearbyRightSideUsableMachinesCache.Clear();
				Vec2 vec = pathData.Path[i];
				Vec2 vec2 = pathData.Path[i + 1];
				this._tempWorldPosition.SetVec2(vec);
				this._tempWorldPosition.GetNavMeshZ();
				WorldPosition tempWorldPosition = this._tempWorldPosition;
				this._tempWorldPosition.SetVec2(vec2);
				this._tempWorldPosition.GetNavMeshZ();
				WorldPosition tempWorldPosition2 = this._tempWorldPosition;
				float num = vec2.DistanceSquared(vec);
				if (num > 25f && num < 100f)
				{
					foreach (MissionPathGenerationLogic.UsableMachineData usableMachineData in pathData.ValidUsableMachinesData)
					{
						if (!usableMachineData.IsAlreadyAddedToPath)
						{
							if (this.IsOnLeftSide(vec, vec2, usableMachineData.MissionObject.GameEntity.GlobalPosition.AsVec2))
							{
								this._nearbyLeftSideUsableMachinesCache.Add(usableMachineData);
							}
							else
							{
								this._nearbyRightSideUsableMachinesCache.Add(usableMachineData);
							}
						}
					}
					foreach (MissionPathGenerationLogic.UsableMachineData usableMachineData2 in this._nearbyLeftSideUsableMachinesCache)
					{
						foreach (MissionPathGenerationLogic.UsableMachineData usableMachineData3 in this._nearbyRightSideUsableMachinesCache)
						{
							usableMachineData2.MissionObject.GameEntity.GlobalPosition.Distance(usableMachineData3.MissionObject.GameEntity.GlobalPosition);
							if (!usableMachineData2.IsAlreadyAddedToPath && !usableMachineData3.IsAlreadyAddedToPath)
							{
								float num2 = this.CalculateCrossRoadScoreForUsableMachines(usableMachineData2, usableMachineData3, pathData.Path, tempWorldPosition, tempWorldPosition2);
								if (num2 > 0f)
								{
									list.Add(new MissionPathGenerationLogic.CrossRoadScoreData(usableMachineData2, usableMachineData3, num2));
									usableMachineData2.IsAlreadyAddedToPath = true;
									usableMachineData3.IsAlreadyAddedToPath = true;
								}
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x000223EC File Offset: 0x000205EC
		private void ShowMissionFailedPopup()
		{
			object obj = new TextObject("{=CMu4B9fZ}Mission Failed", null);
			TextObject textObject = new TextObject("{=RcY8uZA1}You have lost the target.", null);
			TextObject textObject2 = new TextObject("{=DM6luo3c}Continue", null);
			InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, false, textObject2.ToString(), null, delegate()
			{
				Mission.Current.EndMission();
			}, null, "", 0f, null, null, null), Campaign.Current.GameMode == CampaignGameMode.Campaign, false);
		}

		// Token: 0x04000295 RID: 661
		private const float MaximumPathNodeDistanceSquaredToCheckForCrossRoads = 100f;

		// Token: 0x04000296 RID: 662
		private const float MinimumPathNodeDistanceSquaredToCheckForCrossRoads = 25f;

		// Token: 0x04000297 RID: 663
		private const float StandingGuardCountPerXMeter = 10f;

		// Token: 0x04000298 RID: 664
		private const float HumanMonsterCapsuleRadius = 0.37f;

		// Token: 0x04000299 RID: 665
		private const float MinimumStandingGuardSpawnDistance = 3f;

		// Token: 0x0400029A RID: 666
		private const float OptimumStandingGuardSpawnDistance = 5f;

		// Token: 0x0400029B RID: 667
		private const float MaximumStandingGuardSpawnDistance = 30f;

		// Token: 0x0400029C RID: 668
		private const float DoNotSpawnVisitPointPathRatioMin = 0.2f;

		// Token: 0x0400029D RID: 669
		private const float DoNotSpawnVisitPointPathRatioMax = 0.9f;

		// Token: 0x0400029E RID: 670
		private const float OptimumPathIndexRatioForVisitPoint = 0.75f;

		// Token: 0x0400029F RID: 671
		private const float FilterPadding = 20f;

		// Token: 0x040002A0 RID: 672
		private const string VisitBarrelPrefabName = "disguise_mission_interactable_barrel";

		// Token: 0x040002A1 RID: 673
		private const bool PlayerCompromised = false;

		// Token: 0x040002A2 RID: 674
		private readonly CharacterObject _defaultDisguiseCharacter;

		// Token: 0x040002A3 RID: 675
		private int _disabledFaceId;

		// Token: 0x040002A4 RID: 676
		public static int MinimumPathDistance = 200;

		// Token: 0x040002A5 RID: 677
		public static int MaximumPathDistance = 600;

		// Token: 0x040002A6 RID: 678
		public float MinimumDistanceToBlendPointToVisitPoint = 5f;

		// Token: 0x040002A7 RID: 679
		private MissionPathGenerationLogic.PointOfInterestScorePair _selectedPath;

		// Token: 0x040002A8 RID: 680
		public static int MinimumVisitPointCountInPath = 2;

		// Token: 0x040002A9 RID: 681
		public static int MaximumVisitPointCountInPath = 10;

		// Token: 0x040002AA RID: 682
		public static int MinimumCrossRoadCountInPath = 2;

		// Token: 0x040002AB RID: 683
		public static int MaximumCrossRoadCountInPath = 10;

		// Token: 0x040002AC RID: 684
		public static int MinimumStandingGuardCountInPath = 5;

		// Token: 0x040002AD RID: 685
		public static int MaximumStandingGuardCountInPath = 50;

		// Token: 0x040002AE RID: 686
		public static float MinimumGuardSpawnPathRatio = 0.15f;

		// Token: 0x040002AF RID: 687
		public static int MaximumLookBackPointCountInPath;

		// Token: 0x040002B0 RID: 688
		public static int ScoreToAchieve;

		// Token: 0x040002B1 RID: 689
		private Dictionary<Agent, bool> _crossRoadAgentData;

		// Token: 0x040002B2 RID: 690
		private DisguiseMissionLogic _disguiseMissionLogic;

		// Token: 0x040002B3 RID: 691
		private readonly List<GameEntity> _visitBarrelEntities;

		// Token: 0x040002B4 RID: 692
		public List<GameEntity> _startAndFinishPointPool;

		// Token: 0x040002B5 RID: 693
		private GameEntity _currentStarting;

		// Token: 0x040002B6 RID: 694
		private GameEntity _currentEnding;

		// Token: 0x040002B7 RID: 695
		public int CrossRoadMaximumDistance = 30;

		// Token: 0x040002B8 RID: 696
		public int CrossRoadMinimumDistance = 10;

		// Token: 0x040002B9 RID: 697
		public int MinimumVisitPointDistance = 10;

		// Token: 0x040002BA RID: 698
		public int MaximumVisitPointDistance = 40;

		// Token: 0x040002BB RID: 699
		private List<MissionPathGenerationLogic.UsableMachineData> _nearbyLeftSideUsableMachinesCache;

		// Token: 0x040002BC RID: 700
		private List<MissionPathGenerationLogic.UsableMachineData> _nearbyRightSideUsableMachinesCache;

		// Token: 0x040002BD RID: 701
		private List<MissionPathGenerationLogic.PointOfInterestBaseData> _allTargetAgentPointOfInterest;

		// Token: 0x040002BE RID: 702
		private WorldPosition _tempWorldPosition;

		// Token: 0x02000172 RID: 370
		public enum PointOfInterests
		{
			// Token: 0x0400070E RID: 1806
			VisitPoint,
			// Token: 0x0400070F RID: 1807
			CrossRoadPoint,
			// Token: 0x04000710 RID: 1808
			GuardSpawnPoint,
			// Token: 0x04000711 RID: 1809
			LookBackPoint
		}

		// Token: 0x02000173 RID: 371
		public class UsableMachineData
		{
			// Token: 0x06000E5D RID: 3677 RVA: 0x000649F4 File Offset: 0x00062BF4
			public UsableMachineData(SynchedMissionObject missionObject, Vec2 closestPointToPath, float pathDistanceRatio)
			{
				this.MissionObject = missionObject;
				this.ClosestPointToPath = closestPointToPath;
				this.PathDistanceRatio = pathDistanceRatio;
				this.IsAlreadyAddedToPath = false;
			}

			// Token: 0x04000712 RID: 1810
			public SynchedMissionObject MissionObject;

			// Token: 0x04000713 RID: 1811
			public Vec2 ClosestPointToPath;

			// Token: 0x04000714 RID: 1812
			public float PathDistanceRatio;

			// Token: 0x04000715 RID: 1813
			public bool IsAlreadyAddedToPath;
		}

		// Token: 0x02000174 RID: 372
		public class NavigationPathData
		{
			// Token: 0x06000E5E RID: 3678 RVA: 0x00064A18 File Offset: 0x00062C18
			public NavigationPathData(List<UsableMachine> allUsablePoints, GameEntity startingEntity, GameEntity endingEntity, int disabledFaceId)
			{
				this.ValidUsableMachinesData = new List<MissionPathGenerationLogic.UsableMachineData>();
				this.StartingGameEntity = startingEntity;
				this.EndingGameEntity = endingEntity;
				this.Path = new NavigationPath();
				PathFaceRecord pathFaceRecord = new PathFaceRecord(-1, -1, -1);
				Mission.Current.Scene.GetNavMeshFaceIndex(ref pathFaceRecord, startingEntity.GlobalPosition, true);
				PathFaceRecord pathFaceRecord2 = new PathFaceRecord(-1, -1, -1);
				Mission.Current.Scene.GetNavMeshFaceIndex(ref pathFaceRecord2, endingEntity.GlobalPosition, true);
				Mission.Current.Scene.GetPathBetweenAIFaces(pathFaceRecord.FaceIndex, pathFaceRecord2.FaceIndex, startingEntity.GlobalPosition.AsVec2, endingEntity.GlobalPosition.AsVec2, 0f, this.Path, new int[] { disabledFaceId }, 1f);
				this.PathNodeAndDistances = new Dictionary<Vec2, float>();
				this.PathNodeAndDistances.Add(this.Path[0], 0f);
				float num = 0f;
				for (int i = 0; i < this.Path.Size - 1; i++)
				{
					Vec2 vec = this.Path[i];
					Vec2 vec2 = this.Path[i + 1];
					num += vec.Distance(vec2);
					this.PathNodeAndDistances.Add(vec2, num);
				}
				this.TotalDistance = num;
				this.InitializeUsablePoints(allUsablePoints);
			}

			// Token: 0x06000E5F RID: 3679 RVA: 0x00064B78 File Offset: 0x00062D78
			private NavigationPathData(MissionPathGenerationLogic.NavigationPathData navigationPathData)
			{
				this.Path = new NavigationPath();
				this.Path.Size = navigationPathData.Path.Size;
				for (int i = 0; i < navigationPathData.Path.Size; i++)
				{
					this.Path.PathPoints[i] = navigationPathData.Path.PathPoints[this.Path.Size - 1 - i];
				}
				this.TotalDistance = navigationPathData.TotalDistance;
				this.PathNodeAndDistances = new Dictionary<Vec2, float>();
				foreach (KeyValuePair<Vec2, float> keyValuePair in navigationPathData.PathNodeAndDistances)
				{
					this.PathNodeAndDistances.Add(keyValuePair.Key, this.TotalDistance - keyValuePair.Value);
				}
				this.ValidUsableMachinesData = new List<MissionPathGenerationLogic.UsableMachineData>();
				foreach (MissionPathGenerationLogic.UsableMachineData usableMachineData in navigationPathData.ValidUsableMachinesData)
				{
					this.ValidUsableMachinesData.Add(new MissionPathGenerationLogic.UsableMachineData(usableMachineData.MissionObject, usableMachineData.ClosestPointToPath, 1f - usableMachineData.PathDistanceRatio));
				}
				this.StartingGameEntity = navigationPathData.EndingGameEntity;
				this.EndingGameEntity = navigationPathData.StartingGameEntity;
			}

			// Token: 0x06000E60 RID: 3680 RVA: 0x00064CF4 File Offset: 0x00062EF4
			public MissionPathGenerationLogic.NavigationPathData ReverseClone()
			{
				return new MissionPathGenerationLogic.NavigationPathData(this);
			}

			// Token: 0x06000E61 RID: 3681 RVA: 0x00064CFC File Offset: 0x00062EFC
			private bool GetPositionData(Vec2 position, out Vec2 closestPointToPath, out float pathDistanceRatio)
			{
				bool result = false;
				closestPointToPath = Vec2.Invalid;
				pathDistanceRatio = 0f;
				float num = float.MaxValue;
				for (int i = 0; i < this.Path.Size - 1; i++)
				{
					Vec2 key = this.Path[i];
					Vec2 vec = this.Path[i + 1];
					Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(key, vec, position);
					float num2 = position.DistanceSquared(closestPointOnLineSegmentToPoint);
					if (num2 < 2f)
					{
						result = false;
						break;
					}
					if (num2 < 400f)
					{
						result = true;
						if (num2 < num)
						{
							closestPointToPath = closestPointOnLineSegmentToPoint;
							num = num2;
							pathDistanceRatio = (this.PathNodeAndDistances[key] + key.Distance(closestPointOnLineSegmentToPoint)) / this.TotalDistance;
						}
					}
				}
				return result;
			}

			// Token: 0x06000E62 RID: 3682 RVA: 0x00064DC0 File Offset: 0x00062FC0
			public void InitializeUsablePoints(List<UsableMachine> allUsableMachines)
			{
				float num = float.MaxValue;
				float num2 = float.MaxValue;
				float num3 = float.MinValue;
				float num4 = float.MinValue;
				for (int i = 0; i < this.Path.Size; i++)
				{
					Vec2 vec = this.Path[i];
					if (vec.X > num3)
					{
						num3 = vec.X;
					}
					if (vec.X < num)
					{
						num = vec.X;
					}
					if (vec.Y > num4)
					{
						num4 = vec.Y;
					}
					if (vec.Y < num2)
					{
						num2 = vec.Y;
					}
				}
				num3 += 20f;
				num4 += 20f;
				num -= 20f;
				num2 -= 20f;
				foreach (UsableMachine usableMachine in allUsableMachines)
				{
					Vec2 closestPointToPath;
					float pathDistanceRatio;
					if (usableMachine.GameEntity.GlobalPosition.X <= num3 && usableMachine.GameEntity.GlobalPosition.X >= num && usableMachine.GameEntity.GlobalPosition.Y <= num4 && usableMachine.GameEntity.GlobalPosition.Y >= num2 && !(usableMachine is Chair) && this.GetPositionData(usableMachine.GameEntity.GlobalPosition.AsVec2, out closestPointToPath, out pathDistanceRatio))
					{
						this.ValidUsableMachinesData.Add(new MissionPathGenerationLogic.UsableMachineData(usableMachine, closestPointToPath, pathDistanceRatio));
					}
				}
			}

			// Token: 0x04000716 RID: 1814
			public GameEntity StartingGameEntity;

			// Token: 0x04000717 RID: 1815
			public GameEntity EndingGameEntity;

			// Token: 0x04000718 RID: 1816
			public NavigationPath Path;

			// Token: 0x04000719 RID: 1817
			public Dictionary<Vec2, float> PathNodeAndDistances;

			// Token: 0x0400071A RID: 1818
			public List<MissionPathGenerationLogic.UsableMachineData> ValidUsableMachinesData;

			// Token: 0x0400071B RID: 1819
			public float TotalDistance;
		}

		// Token: 0x02000175 RID: 373
		public abstract class PointOfInterestBaseData
		{
			// Token: 0x06000E63 RID: 3683
			public abstract MissionPathGenerationLogic.PointOfInterests GetPointOfInterestType();

			// Token: 0x06000E64 RID: 3684
			public abstract List<ValueTuple<Vec2, float>> GetPositionAndRadiusPairs();

			// Token: 0x06000E65 RID: 3685
			public abstract bool IsInRadius(MissionPathGenerationLogic.PointOfInterestBaseData otherPointOfInterest);

			// Token: 0x06000E66 RID: 3686
			public abstract float GetLocationRatio();

			// Token: 0x0400071C RID: 1820
			public float Score;
		}

		// Token: 0x02000176 RID: 374
		public class LookBackPointData : MissionPathGenerationLogic.PointOfInterestBaseData
		{
			// Token: 0x06000E68 RID: 3688 RVA: 0x00064F78 File Offset: 0x00063178
			public LookBackPointData(WorldPosition position, WorldPosition direction, float pathDistanceRatio)
			{
				this.WorldPosition = position;
				this.PathDistanceRatio = pathDistanceRatio;
				this.DirectionWorldPosition = direction;
			}

			// Token: 0x06000E69 RID: 3689 RVA: 0x00064F95 File Offset: 0x00063195
			public override MissionPathGenerationLogic.PointOfInterests GetPointOfInterestType()
			{
				return MissionPathGenerationLogic.PointOfInterests.LookBackPoint;
			}

			// Token: 0x06000E6A RID: 3690 RVA: 0x00064F98 File Offset: 0x00063198
			public override List<ValueTuple<Vec2, float>> GetPositionAndRadiusPairs()
			{
				return new List<ValueTuple<Vec2, float>>
				{
					new ValueTuple<Vec2, float>(this.WorldPosition.GetNavMeshVec3().AsVec2, 10f)
				};
			}

			// Token: 0x06000E6B RID: 3691 RVA: 0x00064FD0 File Offset: 0x000631D0
			public override bool IsInRadius(MissionPathGenerationLogic.PointOfInterestBaseData otherPointOfInterest)
			{
				if (otherPointOfInterest is MissionPathGenerationLogic.LookBackPointData)
				{
					foreach (ValueTuple<Vec2, float> valueTuple in this.GetPositionAndRadiusPairs())
					{
						foreach (ValueTuple<Vec2, float> valueTuple2 in otherPointOfInterest.GetPositionAndRadiusPairs())
						{
							Vec2 item = valueTuple.Item1;
							if (item.Distance(valueTuple2.Item1) < 25f)
							{
								return true;
							}
						}
					}
					return false;
				}
				return false;
			}

			// Token: 0x06000E6C RID: 3692 RVA: 0x00065084 File Offset: 0x00063284
			public override float GetLocationRatio()
			{
				return this.PathDistanceRatio;
			}

			// Token: 0x0400071D RID: 1821
			public WorldPosition WorldPosition;

			// Token: 0x0400071E RID: 1822
			public WorldPosition DirectionWorldPosition;

			// Token: 0x0400071F RID: 1823
			public float PathDistanceRatio;
		}

		// Token: 0x02000177 RID: 375
		public class VisitPointNodeScoreData : MissionPathGenerationLogic.PointOfInterestBaseData
		{
			// Token: 0x06000E6D RID: 3693 RVA: 0x0006508C File Offset: 0x0006328C
			public VisitPointNodeScoreData(MissionPathGenerationLogic.UsableMachineData visitPointData, WorldPosition possibleBlendPointPosition, WorldPosition visitPointPathStartPoint, float visitPointPathStartPointPathRatio, float score, float startingAngle, WorldPosition fWP, WorldPosition sWP, Vec2 pathToVisitPoint, WorldPosition closestPointToBlendPoint)
			{
				this.VisitPointData = visitPointData;
				this.PossibleBlendPointPosition = possibleBlendPointPosition;
				this.VisitPointPathStartPoint = visitPointPathStartPoint;
				this.Score = score;
				this.PathToVisitPoint = pathToVisitPoint;
				this.SWP = sWP;
				this.FWP = fWP;
				this.ClosestPointToBlendPoint = closestPointToBlendPoint;
				this.VisitPointPathStartPointPathRatio = visitPointPathStartPointPathRatio;
				this.StartingAngle = startingAngle;
				this.PositionAndRadiusPairs = new List<ValueTuple<Vec2, float>>();
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(visitPointData.MissionObject.GameEntity.GlobalPosition.AsVec2, 7f));
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.PossibleBlendPointPosition.AsVec2, 3f));
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.VisitPointPathStartPoint.AsVec2, 3f));
				this.UsingAsInteractablePoint = false;
			}

			// Token: 0x06000E6E RID: 3694 RVA: 0x0006516E File Offset: 0x0006336E
			public override MissionPathGenerationLogic.PointOfInterests GetPointOfInterestType()
			{
				return MissionPathGenerationLogic.PointOfInterests.VisitPoint;
			}

			// Token: 0x06000E6F RID: 3695 RVA: 0x00065171 File Offset: 0x00063371
			public override List<ValueTuple<Vec2, float>> GetPositionAndRadiusPairs()
			{
				return this.PositionAndRadiusPairs;
			}

			// Token: 0x06000E70 RID: 3696 RVA: 0x0006517C File Offset: 0x0006337C
			public override bool IsInRadius(MissionPathGenerationLogic.PointOfInterestBaseData otherPointOfInterest)
			{
				float num = 1f;
				if (otherPointOfInterest is MissionPathGenerationLogic.VisitPointNodeScoreData)
				{
					num = 2f;
				}
				else if (otherPointOfInterest is MissionPathGenerationLogic.CrossRoadScoreData)
				{
					num = 0.5f;
				}
				foreach (ValueTuple<Vec2, float> valueTuple in this.PositionAndRadiusPairs)
				{
					Vec2 item = valueTuple.Item1;
					float item2 = valueTuple.Item2;
					foreach (ValueTuple<Vec2, float> valueTuple2 in otherPointOfInterest.GetPositionAndRadiusPairs())
					{
						Vec2 item3 = valueTuple2.Item1;
						float item4 = valueTuple2.Item2;
						if (item.Distance(item3) < (item2 + item4) * num)
						{
							return true;
						}
					}
				}
				return false;
			}

			// Token: 0x06000E71 RID: 3697 RVA: 0x0006525C File Offset: 0x0006345C
			public override float GetLocationRatio()
			{
				return this.VisitPointData.PathDistanceRatio;
			}

			// Token: 0x04000720 RID: 1824
			public MissionPathGenerationLogic.UsableMachineData VisitPointData;

			// Token: 0x04000721 RID: 1825
			public bool UsingAsInteractablePoint;

			// Token: 0x04000722 RID: 1826
			public WorldPosition PossibleBlendPointPosition;

			// Token: 0x04000723 RID: 1827
			public List<ValueTuple<Vec2, float>> PositionAndRadiusPairs;

			// Token: 0x04000724 RID: 1828
			public WorldPosition VisitPointPathStartPoint;

			// Token: 0x04000725 RID: 1829
			public float VisitPointPathStartPointPathRatio;

			// Token: 0x04000726 RID: 1830
			public WorldPosition ClosestPointToBlendPoint;

			// Token: 0x04000727 RID: 1831
			public WorldPosition FWP;

			// Token: 0x04000728 RID: 1832
			public WorldPosition SWP;

			// Token: 0x04000729 RID: 1833
			public float StartingAngle;

			// Token: 0x0400072A RID: 1834
			public Vec2 PathToVisitPoint;
		}

		// Token: 0x02000178 RID: 376
		public class CrossRoadScoreData : MissionPathGenerationLogic.PointOfInterestBaseData
		{
			// Token: 0x06000E72 RID: 3698 RVA: 0x0006526C File Offset: 0x0006346C
			public CrossRoadScoreData(MissionPathGenerationLogic.UsableMachineData leftNode, MissionPathGenerationLogic.UsableMachineData rightNode, float score)
			{
				this.LeftNode = leftNode;
				this.RightNode = rightNode;
				this.Score = score;
				this.PositionAndRadiusPairs = new List<ValueTuple<Vec2, float>>();
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.LeftNode.MissionObject.GameEntity.GlobalPosition.AsVec2, 1f));
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.RightNode.MissionObject.GameEntity.GlobalPosition.AsVec2, 1f));
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.RightNode.ClosestPointToPath, 1f));
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.LeftNode.ClosestPointToPath, 1f));
			}

			// Token: 0x06000E73 RID: 3699 RVA: 0x00065349 File Offset: 0x00063549
			public override MissionPathGenerationLogic.PointOfInterests GetPointOfInterestType()
			{
				return MissionPathGenerationLogic.PointOfInterests.CrossRoadPoint;
			}

			// Token: 0x06000E74 RID: 3700 RVA: 0x0006534C File Offset: 0x0006354C
			public override List<ValueTuple<Vec2, float>> GetPositionAndRadiusPairs()
			{
				return this.PositionAndRadiusPairs;
			}

			// Token: 0x06000E75 RID: 3701 RVA: 0x00065354 File Offset: 0x00063554
			public override bool IsInRadius(MissionPathGenerationLogic.PointOfInterestBaseData otherPointOfInterest)
			{
				foreach (ValueTuple<Vec2, float> valueTuple in this.PositionAndRadiusPairs)
				{
					Vec2 item = valueTuple.Item1;
					float item2 = valueTuple.Item2;
					foreach (ValueTuple<Vec2, float> valueTuple2 in otherPointOfInterest.GetPositionAndRadiusPairs())
					{
						Vec2 item3 = valueTuple2.Item1;
						float item4 = valueTuple2.Item2;
						if (item.Distance(item3) < item2 + item4)
						{
							return true;
						}
					}
				}
				return false;
			}

			// Token: 0x06000E76 RID: 3702 RVA: 0x00065410 File Offset: 0x00063610
			public override float GetLocationRatio()
			{
				return (this.LeftNode.PathDistanceRatio + this.RightNode.PathDistanceRatio) * 0.5f;
			}

			// Token: 0x0400072B RID: 1835
			public MissionPathGenerationLogic.UsableMachineData LeftNode;

			// Token: 0x0400072C RID: 1836
			public MissionPathGenerationLogic.UsableMachineData RightNode;

			// Token: 0x0400072D RID: 1837
			public List<ValueTuple<Vec2, float>> PositionAndRadiusPairs;
		}

		// Token: 0x02000179 RID: 377
		public class StandingGuardSpawnData : MissionPathGenerationLogic.PointOfInterestBaseData
		{
			// Token: 0x06000E77 RID: 3703 RVA: 0x00065430 File Offset: 0x00063630
			public StandingGuardSpawnData(MissionPathGenerationLogic.UsableMachineData guardPointData, Vec2 spawnDirection, float score)
			{
				this.GuardPointData = guardPointData;
				this.SpawnDirection = spawnDirection;
				this.Score = score;
				this.PositionAndRadiusPairs = new List<ValueTuple<Vec2, float>>();
				this.PositionAndRadiusPairs.Add(new ValueTuple<Vec2, float>(this.GuardPointData.MissionObject.GameEntity.GlobalPosition.AsVec2, 2f));
			}

			// Token: 0x06000E78 RID: 3704 RVA: 0x00065498 File Offset: 0x00063698
			public override MissionPathGenerationLogic.PointOfInterests GetPointOfInterestType()
			{
				return MissionPathGenerationLogic.PointOfInterests.GuardSpawnPoint;
			}

			// Token: 0x06000E79 RID: 3705 RVA: 0x0006549B File Offset: 0x0006369B
			public override List<ValueTuple<Vec2, float>> GetPositionAndRadiusPairs()
			{
				return this.PositionAndRadiusPairs;
			}

			// Token: 0x06000E7A RID: 3706 RVA: 0x000654A4 File Offset: 0x000636A4
			public override bool IsInRadius(MissionPathGenerationLogic.PointOfInterestBaseData otherPointOfInterest)
			{
				foreach (ValueTuple<Vec2, float> valueTuple in this.PositionAndRadiusPairs)
				{
					Vec2 item = valueTuple.Item1;
					float item2 = valueTuple.Item2;
					foreach (ValueTuple<Vec2, float> valueTuple2 in otherPointOfInterest.GetPositionAndRadiusPairs())
					{
						Vec2 item3 = valueTuple2.Item1;
						float item4 = valueTuple2.Item2;
						if (item.Distance(item3) < item2 + item4)
						{
							return true;
						}
					}
				}
				return false;
			}

			// Token: 0x06000E7B RID: 3707 RVA: 0x00065560 File Offset: 0x00063760
			public override float GetLocationRatio()
			{
				return this.GuardPointData.PathDistanceRatio;
			}

			// Token: 0x0400072E RID: 1838
			public MissionPathGenerationLogic.UsableMachineData GuardPointData;

			// Token: 0x0400072F RID: 1839
			public Vec2 SpawnDirection;

			// Token: 0x04000730 RID: 1840
			public List<ValueTuple<Vec2, float>> PositionAndRadiusPairs;
		}

		// Token: 0x0200017A RID: 378
		public class PointOfInterestScorePair
		{
			// Token: 0x17000126 RID: 294
			// (get) Token: 0x06000E7C RID: 3708 RVA: 0x0006556D File Offset: 0x0006376D
			public List<MissionPathGenerationLogic.PointOfInterestBaseData> Data
			{
				get
				{
					return this._data;
				}
			}

			// Token: 0x06000E7D RID: 3709 RVA: 0x00065578 File Offset: 0x00063778
			public PointOfInterestScorePair(MissionPathGenerationLogic.NavigationPathData pathData, List<MissionPathGenerationLogic.PointOfInterestBaseData> data, float score)
			{
				this.PathData = pathData;
				this._data = data;
				this.Score = score;
				this.PointOfInterestCount = new Dictionary<MissionPathGenerationLogic.PointOfInterests, int>();
				foreach (MissionPathGenerationLogic.PointOfInterests key in (MissionPathGenerationLogic.PointOfInterests[])Enum.GetValues(typeof(MissionPathGenerationLogic.PointOfInterests)))
				{
					this.PointOfInterestCount.Add(key, 0);
				}
				foreach (MissionPathGenerationLogic.PointOfInterestBaseData pointOfInterestBaseData in this._data)
				{
					Dictionary<MissionPathGenerationLogic.PointOfInterests, int> pointOfInterestCount = this.PointOfInterestCount;
					MissionPathGenerationLogic.PointOfInterests pointOfInterestType = pointOfInterestBaseData.GetPointOfInterestType();
					int i = pointOfInterestCount[pointOfInterestType];
					pointOfInterestCount[pointOfInterestType] = i + 1;
				}
			}

			// Token: 0x06000E7E RID: 3710 RVA: 0x00065644 File Offset: 0x00063844
			private PointOfInterestScorePair(MissionPathGenerationLogic.PointOfInterestScorePair otherPair)
			{
				this.PathData = otherPair.PathData;
				this._data = otherPair._data.ToList<MissionPathGenerationLogic.PointOfInterestBaseData>();
				this.Score = otherPair.Score;
				this.PointOfInterestCount = otherPair.PointOfInterestCount.ToDictionary((KeyValuePair<MissionPathGenerationLogic.PointOfInterests, int> x) => x.Key, (KeyValuePair<MissionPathGenerationLogic.PointOfInterests, int> x) => x.Value);
			}

			// Token: 0x06000E7F RID: 3711 RVA: 0x000656CF File Offset: 0x000638CF
			public MissionPathGenerationLogic.PointOfInterestScorePair Clone()
			{
				return new MissionPathGenerationLogic.PointOfInterestScorePair(this);
			}

			// Token: 0x06000E80 RID: 3712 RVA: 0x000656D8 File Offset: 0x000638D8
			public void AddToData(MissionPathGenerationLogic.PointOfInterestBaseData pointOfInterestToAdd)
			{
				Dictionary<MissionPathGenerationLogic.PointOfInterests, int> pointOfInterestCount = this.PointOfInterestCount;
				MissionPathGenerationLogic.PointOfInterests pointOfInterestType = pointOfInterestToAdd.GetPointOfInterestType();
				int num = pointOfInterestCount[pointOfInterestType];
				pointOfInterestCount[pointOfInterestType] = num + 1;
				this._data.Add(pointOfInterestToAdd);
				this.Score += pointOfInterestToAdd.Score;
			}

			// Token: 0x06000E81 RID: 3713 RVA: 0x00065724 File Offset: 0x00063924
			public bool IsDataEqualTo(MissionPathGenerationLogic.PointOfInterestScorePair other, MissionPathGenerationLogic.PointOfInterestBaseData newDataToAdd)
			{
				if (this.PathData != other.PathData || other.Data.Count + 1 != this.Data.Count || !this.Score.ApproximatelyEqualsTo(other.Score + newDataToAdd.Score, 1E-05f) || this.Data[this.Data.Count - 1] != newDataToAdd)
				{
					return false;
				}
				for (int i = other.Data.Count - 1; i >= 0; i--)
				{
					if (other.Data[i] != this.Data[i])
					{
						return false;
					}
				}
				return true;
			}

			// Token: 0x06000E82 RID: 3714 RVA: 0x000657CC File Offset: 0x000639CC
			public bool IsBetterThan(MissionPathGenerationLogic.PointOfInterestScorePair other)
			{
				float num = (float)(MissionPathGenerationLogic.MaximumVisitPointCountInPath + MissionPathGenerationLogic.MinimumVisitPointCountInPath) * 0.5f;
				float num2 = Math.Abs((float)this.PointOfInterestCount[MissionPathGenerationLogic.PointOfInterests.VisitPoint] - num);
				float num3 = Math.Abs((float)other.PointOfInterestCount[MissionPathGenerationLogic.PointOfInterests.VisitPoint] - num);
				float num4 = 0.5f;
				float num5 = ((this.Score >= other.Score) ? 0.2f : (-0.2f));
				float num6 = ((num3 >= num2) ? 0.2f : (-0.2f));
				return num4 + num5 + num6 > 0.5f;
			}

			// Token: 0x06000E83 RID: 3715 RVA: 0x00065854 File Offset: 0x00063A54
			public bool IsSufficient()
			{
				int num = this.PointOfInterestCount[MissionPathGenerationLogic.PointOfInterests.VisitPoint];
				int num2 = this.PointOfInterestCount[MissionPathGenerationLogic.PointOfInterests.CrossRoadPoint];
				return this.Score >= (float)MissionPathGenerationLogic.ScoreToAchieve && this.PathData.TotalDistance >= (float)MissionPathGenerationLogic.MinimumPathDistance && this.PathData.TotalDistance <= (float)MissionPathGenerationLogic.MaximumPathDistance && num >= MissionPathGenerationLogic.MinimumVisitPointCountInPath && num <= MissionPathGenerationLogic.MaximumVisitPointCountInPath && num2 >= MissionPathGenerationLogic.MinimumCrossRoadCountInPath && num2 <= MissionPathGenerationLogic.MaximumCrossRoadCountInPath;
			}

			// Token: 0x06000E84 RID: 3716 RVA: 0x000658D4 File Offset: 0x00063AD4
			public void ReOrderDataAccordingToPathRatios()
			{
				this._data = (from x in this._data
					orderby x.GetLocationRatio()
					select x).ToList<MissionPathGenerationLogic.PointOfInterestBaseData>();
			}

			// Token: 0x04000731 RID: 1841
			public MissionPathGenerationLogic.NavigationPathData PathData;

			// Token: 0x04000732 RID: 1842
			private List<MissionPathGenerationLogic.PointOfInterestBaseData> _data;

			// Token: 0x04000733 RID: 1843
			public Dictionary<MissionPathGenerationLogic.PointOfInterests, int> PointOfInterestCount;

			// Token: 0x04000734 RID: 1844
			public float Score;
		}
	}
}

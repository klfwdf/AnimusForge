using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects;
using SandBox.Objects.AreaMarkers;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Missions.NameMarkers
{
	// Token: 0x0200002F RID: 47
	public class DefaultMissionNameMarkerHandler : MissionNameMarkerProvider
	{
		// Token: 0x06000193 RID: 403 RVA: 0x00012923 File Offset: 0x00010B23
		protected override void OnInitialize(Mission mission)
		{
			base.OnInitialize(mission);
			this._disguiseMissionLogic = mission.GetMissionBehavior<DisguiseMissionLogic>();
			this._lastMissionMode = mission.Mode;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00012944 File Offset: 0x00010B44
		protected override void OnDestroy(Mission mission)
		{
			base.OnDestroy(mission);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00012950 File Offset: 0x00010B50
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			MissionMode lastMissionMode = this._lastMissionMode;
			Mission mission = Mission.Current;
			MissionMode? missionMode = ((mission != null) ? new MissionMode?(mission.Mode) : null);
			if (!((lastMissionMode == missionMode.GetValueOrDefault()) & (missionMode != null)))
			{
				base.SetMarkersDirty();
				this._lastMissionMode = Mission.Current.Mode;
			}
		}

		// Token: 0x06000196 RID: 406 RVA: 0x000129B4 File Offset: 0x00010BB4
		public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
		{
			Mission mission = Mission.Current;
			if (mission.MainAgent == null || mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.Deployment)
			{
				return;
			}
			List<MissionAgentMarkerTargetVM> list = new List<MissionAgentMarkerTargetVM>();
			foreach (Agent agent in mission.Agents)
			{
				this.AddAgentTarget(agent, list, false);
			}
			for (int i = 0; i < list.Count; i++)
			{
				markers.Add(list[i]);
			}
			if (Hero.MainHero.CurrentSettlement != null)
			{
				List<CommonAreaMarker> list2 = (from x in mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
					where x.GameEntity.HasTag("alley_marker")
					select x).ToList<CommonAreaMarker>();
				if (Hero.MainHero.CurrentSettlement.Alleys.Count > 0)
				{
					foreach (CommonAreaMarker commonAreaMarker in list2)
					{
						Alley alley = commonAreaMarker.GetAlley();
						if (alley != null && alley.Owner != null)
						{
							markers.Add(new MissionCommonAreaMarkerTargetVM(commonAreaMarker));
						}
					}
				}
				IEnumerable<PassageUsePoint> source = mission.ActiveMissionObjects.FindAllWithType<PassageUsePoint>().ToList<PassageUsePoint>();
				List<string> passagePointFilter = new List<string> { "Empty Shop" };
				Func<PassageUsePoint, bool> predicate;
				Func<PassageUsePoint, bool> <>9__1;
				if ((predicate = <>9__1) == null)
				{
					predicate = (<>9__1 = (PassageUsePoint passage) => passage.ToLocation != null && !passagePointFilter.Exists((string s) => passage.ToLocation.Name.Contains(s)));
				}
				foreach (PassageUsePoint passageUsePoint in source.Where(predicate))
				{
					if (!passageUsePoint.ToLocation.CanBeReserved || passageUsePoint.ToLocation.IsReserved)
					{
						markers.Add(new MissionPassageUsePointNameMarkerTargetVM(passageUsePoint));
					}
				}
				foreach (BasicAreaIndicator basicAreaIndicator in from b in mission.ActiveMissionObjects.FindAllWithType<BasicAreaIndicator>().ToList<BasicAreaIndicator>()
					where b.IsActive
					select b)
				{
					markers.Add(new MissionBasicAreaIndicatorMarkerTargetVM(basicAreaIndicator, basicAreaIndicator.GetPosition()));
				}
				if (mission.HasMissionBehavior<WorkshopMissionHandler>())
				{
					foreach (Tuple<Workshop, GameEntity> tuple in from s in mission.GetMissionBehavior<WorkshopMissionHandler>().WorkshopSignEntities.ToList<Tuple<Workshop, GameEntity>>()
						where s.Item1.WorkshopType != null
						select s)
					{
						markers.Add(new MissionWorkshopNameMarkerTargetVM(tuple.Item1, tuple.Item2.GlobalPosition - MissionNameMarkerHelper.DefaultHeightOffset));
					}
				}
			}
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00012CDC File Offset: 0x00010EDC
		private void AddAgentTarget(Agent agent, List<MissionAgentMarkerTargetVM> markers, bool isAdditional = false)
		{
			Agent agent2 = agent;
			if (((agent2 != null) ? agent2.Character : null) != null && agent != Agent.Main && agent.IsActive() && !markers.Any((MissionAgentMarkerTargetVM t) => t.Target == agent))
			{
				bool flag5;
				if (!isAdditional && !agent.Character.IsHero)
				{
					Settlement currentSettlement = Settlement.CurrentSettlement;
					bool flag;
					if (currentSettlement == null)
					{
						flag = false;
					}
					else
					{
						LocationComplex locationComplex = currentSettlement.LocationComplex;
						bool? flag2;
						if (locationComplex == null)
						{
							flag2 = null;
						}
						else
						{
							LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
							flag2 = ((locationCharacter != null) ? new bool?(locationCharacter.IsVisualTracked) : null);
						}
						bool? flag3 = flag2;
						bool flag4 = true;
						flag = (flag3.GetValueOrDefault() == flag4) & (flag3 != null);
					}
					CharacterObject characterObject;
					if (!flag && ((characterObject = agent.Character as CharacterObject) == null || (characterObject.Occupation != Occupation.RansomBroker && characterObject.Occupation != Occupation.Tavernkeeper)))
					{
						BasicCharacterObject character = agent.Character;
						Settlement currentSettlement2 = Settlement.CurrentSettlement;
						object obj;
						if (currentSettlement2 == null)
						{
							obj = null;
						}
						else
						{
							CultureObject culture = currentSettlement2.Culture;
							obj = ((culture != null) ? culture.Blacksmith : null);
						}
						if (character != obj)
						{
							BasicCharacterObject character2 = agent.Character;
							Settlement currentSettlement3 = Settlement.CurrentSettlement;
							object obj2;
							if (currentSettlement3 == null)
							{
								obj2 = null;
							}
							else
							{
								CultureObject culture2 = currentSettlement3.Culture;
								obj2 = ((culture2 != null) ? culture2.Barber : null);
							}
							if (character2 != obj2)
							{
								BasicCharacterObject character3 = agent.Character;
								Settlement currentSettlement4 = Settlement.CurrentSettlement;
								object obj3;
								if (currentSettlement4 == null)
								{
									obj3 = null;
								}
								else
								{
									CultureObject culture3 = currentSettlement4.Culture;
									obj3 = ((culture3 != null) ? culture3.TavernGamehost : null);
								}
								if (character3 != obj3)
								{
									BasicCharacterObject character4 = agent.Character;
									Settlement currentSettlement5 = Settlement.CurrentSettlement;
									object obj4;
									if (currentSettlement5 == null)
									{
										obj4 = null;
									}
									else
									{
										CultureObject culture4 = currentSettlement5.Culture;
										obj4 = ((culture4 != null) ? culture4.Merchant : null);
									}
									if (character4 != obj4 && !(agent.Character.StringId == "sp_hermit"))
									{
										BasicCharacterObject character5 = agent.Character;
										Settlement currentSettlement6 = Settlement.CurrentSettlement;
										object obj5;
										if (currentSettlement6 == null)
										{
											obj5 = null;
										}
										else
										{
											CultureObject culture5 = currentSettlement6.Culture;
											obj5 = ((culture5 != null) ? culture5.Shipwright : null);
										}
										if (character5 != obj5)
										{
											DisguiseMissionLogic disguiseMissionLogic = this._disguiseMissionLogic;
											flag5 = disguiseMissionLogic != null && disguiseMissionLogic.IsContactAgentTracked(agent);
											goto IL_215;
										}
									}
								}
							}
						}
					}
				}
				flag5 = true;
				IL_215:
				if (flag5)
				{
					MissionAgentMarkerTargetVM item = new MissionAgentMarkerTargetVM(agent);
					markers.Add(item);
				}
			}
		}

		// Token: 0x040000F9 RID: 249
		private MissionMode _lastMissionMode;

		// Token: 0x040000FA RID: 250
		private DisguiseMissionLogic _disguiseMissionLogic;
	}
}

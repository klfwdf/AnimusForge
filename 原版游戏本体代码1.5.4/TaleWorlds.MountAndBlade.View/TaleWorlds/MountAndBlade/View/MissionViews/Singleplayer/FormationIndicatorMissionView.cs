using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class FormationIndicatorMissionView : MissionView
{
	public class Indicator
	{
		public MissionScreen missionScreen;

		public bool indicatorVisible;

		public MatrixFrame indicatorFrame;

		public bool firstTime = true;

		public GameEntity indicatorEntity;

		public Vec3 nextIndicatorPosition;

		public Vec3 prevIndicatorPosition;

		public float indicatorAlpha = 1f;

		private float _drawIndicatorElapsedTime;

		private const float IndicatorExpireTime = 0.5f;

		private bool _isSeenByPlayer = true;

		internal bool _isMovingTooFast;

		private Vec3? GetCurrentPosition()
		{
			if (Mission.Current.MainAgent != null)
			{
				return Mission.Current.MainAgent.AgentVisuals.GetGlobalFrame().origin + new Vec3(0f, 0f, 1f);
			}
			if (missionScreen.CombatCamera != null)
			{
				return missionScreen.CombatCamera.Position;
			}
			return null;
		}

		public void DetermineIndicatorState(float dt, Vec3 position)
		{
			Mission current = Mission.Current;
			Vec3? currentPosition = GetCurrentPosition();
			if (!currentPosition.HasValue)
			{
				indicatorVisible = false;
				return;
			}
			if (firstTime)
			{
				prevIndicatorPosition = position;
				nextIndicatorPosition = position;
				firstTime = false;
			}
			Vec3 vec;
			if (nextIndicatorPosition.Distance(prevIndicatorPosition) / 0.5f > 30f)
			{
				vec = position;
				_isMovingTooFast = true;
			}
			else
			{
				vec = Vec3.Lerp(prevIndicatorPosition, nextIndicatorPosition, MBMath.ClampFloat(_drawIndicatorElapsedTime / 0.5f, 0f, 1f));
			}
			float num = currentPosition.Value.Distance(vec);
			if (_drawIndicatorElapsedTime < 0.5f)
			{
				_drawIndicatorElapsedTime += dt;
			}
			else
			{
				prevIndicatorPosition = nextIndicatorPosition;
				nextIndicatorPosition = position;
				_isSeenByPlayer = num < 60f && (num < 15f || current.Scene.CheckPointCanSeePoint(currentPosition.Value, position) || current.Scene.CheckPointCanSeePoint(currentPosition.Value + new Vec3(0f, 0f, 2f), position + new Vec3(0f, 0f, 2f)));
				_drawIndicatorElapsedTime = 0f;
			}
			if (!_isSeenByPlayer)
			{
				float num2 = MBMath.ClampFloat(num * 0.02f, 1f, 10f) * 3f;
				MatrixFrame identity = MatrixFrame.Identity;
				identity.origin = vec;
				identity.origin.z += num2 * 0.75f;
				identity.rotation.ApplyScaleLocal(num2);
				indicatorFrame = identity;
				if (_isMovingTooFast)
				{
					if (indicatorEntity != null && indicatorEntity.IsVisibleIncludeParents())
					{
						indicatorVisible = false;
						return;
					}
					_isMovingTooFast = false;
					indicatorVisible = true;
				}
				else
				{
					indicatorVisible = true;
				}
			}
			else
			{
				indicatorVisible = false;
				if (!_isMovingTooFast && indicatorEntity != null && indicatorEntity.IsVisibleIncludeParents())
				{
					float num3 = MBMath.ClampFloat(num * 0.02f, 1f, 10f) * 3f;
					MatrixFrame identity2 = MatrixFrame.Identity;
					identity2.origin = vec;
					identity2.origin.z += num3 * 0.75f;
					identity2.rotation.ApplyScaleLocal(num3);
					indicatorFrame = identity2;
				}
			}
		}
	}

	private Indicator[,] _indicators;

	private Mission mission;

	private bool _isEnabled;

	public override void AfterStart()
	{
		base.AfterStart();
		mission = Mission.Current;
		_indicators = new Indicator[mission.Teams.Count, 9];
		for (int i = 0; i < mission.Teams.Count; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				_indicators[i, j] = new Indicator
				{
					missionScreen = base.MissionScreen
				};
			}
		}
	}

	private GameEntity CreateBannerEntity(Formation formation)
	{
		GameEntity gameEntity = GameEntity.CreateEmpty(mission.Scene);
		gameEntity.EntityFlags |= EntityFlags.NoOcclusionCulling;
		uint color = 4278190080u;
		uint color2 = ((!formation.Team.IsPlayerAlly) ? 2144798212u : 2130747904u);
		gameEntity.AddMultiMesh(MetaMesh.GetCopy("billboard_unit_mesh"));
		gameEntity.GetFirstMesh().Color = uint.MaxValue;
		Material formationMaterial = Material.GetFromResource("formation_icon").CreateCopy();
		if (formation.Team != null)
		{
			Banner banner = ((formation.Captain != null) ? formation.Captain.Origin.Banner : formation.Team.Banner);
			if (banner != null)
			{
				BannerVisualExtensions.GetTableauTextureLarge(setAction: delegate(Texture tex)
				{
					formationMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
				}, banner: banner, debugInfo: BannerDebugInfo.CreateManual(GetType().Name));
			}
			else
			{
				Texture fromResource = Texture.GetFromResource("plain_red");
				formationMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource);
			}
		}
		else
		{
			Texture fromResource2 = Texture.GetFromResource("plain_red");
			formationMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource2);
		}
		int num = (int)formation.FormationIndex % 4;
		gameEntity.GetFirstMesh().SetMaterial(formationMaterial);
		gameEntity.GetFirstMesh().Color = color2;
		gameEntity.GetFirstMesh().Color2 = color;
		gameEntity.GetFirstMesh().SetVectorArgument(0f, 1f, num, 1f);
		return gameEntity;
	}

	private int GetFormationTeamIndex(Formation formation)
	{
		if (mission.Teams.Count > 2 && (formation.Team == mission.AttackerAllyTeam || formation.Team == mission.DefenderAllyTeam))
		{
			return (mission.Teams.Count == 3) ? 2 : ((formation.Team == mission.DefenderAllyTeam) ? 2 : 3);
		}
		return (int)formation.Team.Side;
	}

	public override void OnMissionScreenTick(float dt)
	{
		OnMissionTick(dt);
		bool flag;
		if (base.Input.IsGameKeyDown(5))
		{
			flag = false;
			_isEnabled = false;
		}
		else
		{
			flag = false;
		}
		IEnumerable<Formation> enumerable = mission.Teams.SelectMany((Team t) => t.FormationsIncludingEmpty);
		if (flag)
		{
			IEnumerable<Formation> enumerable2 = mission.Teams.SelectMany((Team t) => t.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0));
			foreach (Formation item in enumerable2)
			{
				int formationTeamIndex = GetFormationTeamIndex(item);
				Indicator indicator = _indicators[formationTeamIndex, (int)item.FormationIndex];
				indicator.DetermineIndicatorState(dt, item.CachedMedianPosition.GetGroundVec3());
				if (indicator.indicatorEntity == null)
				{
					if (indicator.indicatorVisible)
					{
						GameEntity gameEntity = CreateBannerEntity(item);
						gameEntity.SetFrame(ref indicator.indicatorFrame);
						gameEntity.SetVisibilityExcludeParents(visible: true);
						_indicators[formationTeamIndex, (int)item.FormationIndex].indicatorEntity = gameEntity;
						gameEntity.SetAlpha(0f);
						_indicators[formationTeamIndex, (int)item.FormationIndex].indicatorAlpha = 0f;
					}
					continue;
				}
				if (indicator.indicatorEntity.IsVisibleIncludeParents() != indicator.indicatorVisible)
				{
					if (!indicator.indicatorVisible)
					{
						if (indicator.indicatorAlpha > 0f)
						{
							indicator.indicatorAlpha -= 0.01f;
							if (indicator.indicatorAlpha < 0f)
							{
								indicator.indicatorAlpha = 0f;
							}
							indicator.indicatorEntity.SetAlpha(indicator.indicatorAlpha);
						}
						else
						{
							indicator.indicatorEntity.SetVisibilityExcludeParents(indicator.indicatorVisible);
						}
					}
					else
					{
						indicator.indicatorEntity.SetVisibilityExcludeParents(indicator.indicatorVisible);
					}
				}
				if (indicator.indicatorVisible && indicator.indicatorAlpha < 1f)
				{
					indicator.indicatorAlpha += 0.01f;
					if (indicator.indicatorAlpha > 1f)
					{
						indicator.indicatorAlpha = 1f;
					}
					indicator.indicatorEntity.SetAlpha(indicator.indicatorAlpha);
				}
				if (!indicator._isMovingTooFast && indicator.indicatorEntity.IsVisibleIncludeParents())
				{
					indicator.indicatorEntity.SetFrame(ref indicator.indicatorFrame);
				}
			}
			{
				foreach (Formation item2 in enumerable.Except(enumerable2))
				{
					if (item2.FormationIndex >= FormationClass.NumberOfRegularFormations)
					{
						break;
					}
					Indicator indicator2 = _indicators[GetFormationTeamIndex(item2), (int)item2.FormationIndex];
					if (indicator2.indicatorEntity != null && indicator2.indicatorEntity.IsVisibleIncludeParents())
					{
						indicator2.indicatorEntity.SetVisibilityExcludeParents(visible: false);
						indicator2.indicatorVisible = false;
					}
				}
				return;
			}
		}
		if (!_isEnabled)
		{
			return;
		}
		foreach (Formation item3 in enumerable)
		{
			int formationTeamIndex2 = GetFormationTeamIndex(item3);
			Indicator indicator3 = _indicators[formationTeamIndex2, (int)item3.FormationIndex];
			if (indicator3 != null && indicator3.indicatorEntity != null)
			{
				indicator3.indicatorAlpha = 0f;
				indicator3.indicatorEntity.SetVisibilityExcludeParents(visible: false);
			}
		}
		_isEnabled = false;
	}
}

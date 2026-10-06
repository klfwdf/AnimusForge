using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox;

public class CampaignMapSiegePrefabEntityCache : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "")]
	private string _attackerBallistaPrefab = "ballista_a_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _defenderBallistaPrefab = "ballista_b_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _attackerFireBallistaPrefab = "ballista_a_fire_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _defenderFireBallistaPrefab = "ballista_b_fire_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _attackerMangonelPrefab = "mangonel_a_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _defenderMangonelPrefab = "mangonel_b_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _attackerFireMangonelPrefab = "mangonel_a_fire_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _defenderFireMangonelPrefab = "mangonel_b_fire_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _attackerTrebuchetPrefab = "trebuchet_a_mapicon";

	[EditableScriptComponentVariable(true, "")]
	private string _defenderTrebuchetPrefab = "trebuchet_b_mapicon";

	private MatrixFrame _attackerBallistaLaunchEntitialFrame;

	private MatrixFrame _defenderBallistaLaunchEntitialFrame;

	private MatrixFrame _attackerFireBallistaLaunchEntitialFrame;

	private MatrixFrame _defenderFireBallistaLaunchEntitialFrame;

	private MatrixFrame _attackerMangonelLaunchEntitialFrame;

	private MatrixFrame _defenderMangonelLaunchEntitialFrame;

	private MatrixFrame _attackerFireMangonelLaunchEntitialFrame;

	private MatrixFrame _defenderFireMangonelLaunchEntitialFrame;

	private MatrixFrame _attackerTrebuchetLaunchEntitialFrame;

	private MatrixFrame _defenderTrebuchetLaunchEntitialFrame;

	private Vec3 _attackerBallistaScale;

	private Vec3 _defenderBallistaScale;

	private Vec3 _attackerFireBallistaScale;

	private Vec3 _defenderFireBallistaScale;

	private Vec3 _attackerMangonelScale;

	private Vec3 _defenderMangonelScale;

	private Vec3 _attackerFireMangonelScale;

	private Vec3 _defenderFireMangonelScale;

	private Vec3 _attackerTrebuchetScale;

	private Vec3 _defenderTrebuchetScale;

	protected override void OnInit()
	{
		base.OnInit();
		GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _attackerBallistaPrefab, callScriptCallbacks: true);
		_attackerBallistaLaunchEntitialFrame = gameEntity.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_attackerBallistaScale = gameEntity.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _defenderBallistaPrefab, callScriptCallbacks: true);
		_defenderBallistaLaunchEntitialFrame = gameEntity2.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_defenderBallistaScale = gameEntity2.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity3 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _attackerFireBallistaPrefab, callScriptCallbacks: true);
		_attackerFireBallistaLaunchEntitialFrame = gameEntity3.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_attackerFireBallistaScale = gameEntity3.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity4 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _defenderFireBallistaPrefab, callScriptCallbacks: true);
		_defenderFireBallistaLaunchEntitialFrame = gameEntity4.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_defenderFireBallistaScale = gameEntity4.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity5 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _attackerMangonelPrefab, callScriptCallbacks: true);
		_attackerMangonelLaunchEntitialFrame = gameEntity5.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_attackerMangonelScale = gameEntity5.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity6 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _defenderMangonelPrefab, callScriptCallbacks: true);
		_defenderMangonelLaunchEntitialFrame = gameEntity6.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_defenderMangonelScale = gameEntity6.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity7 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _attackerFireMangonelPrefab, callScriptCallbacks: true);
		_attackerFireMangonelLaunchEntitialFrame = gameEntity7.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_attackerFireMangonelScale = gameEntity7.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity8 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _defenderFireMangonelPrefab, callScriptCallbacks: true);
		_defenderFireMangonelLaunchEntitialFrame = gameEntity8.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_defenderFireMangonelScale = gameEntity8.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity9 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _attackerTrebuchetPrefab, callScriptCallbacks: true);
		_attackerTrebuchetLaunchEntitialFrame = gameEntity9.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_attackerTrebuchetScale = gameEntity9.GetChild(0).GetFrame().rotation.GetScaleVector();
		GameEntity gameEntity10 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, _defenderTrebuchetPrefab, callScriptCallbacks: true);
		_defenderTrebuchetLaunchEntitialFrame = gameEntity10.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
		_defenderTrebuchetScale = gameEntity10.GetChild(0).GetFrame().rotation.GetScaleVector();
	}

	public MatrixFrame GetLaunchEntitialFrameForSiegeEngine(SiegeEngineType type, BattleSideEnum side)
	{
		MatrixFrame result = MatrixFrame.Identity;
		if (type == DefaultSiegeEngineTypes.Onager)
		{
			result = _attackerMangonelLaunchEntitialFrame;
		}
		else if (type == DefaultSiegeEngineTypes.FireOnager)
		{
			result = _attackerFireMangonelLaunchEntitialFrame;
		}
		else if (type == DefaultSiegeEngineTypes.Catapult)
		{
			result = _defenderMangonelLaunchEntitialFrame;
		}
		else if (type == DefaultSiegeEngineTypes.FireCatapult)
		{
			result = _defenderFireMangonelLaunchEntitialFrame;
		}
		else if (type == DefaultSiegeEngineTypes.Ballista)
		{
			result = ((side == BattleSideEnum.Attacker) ? _attackerBallistaLaunchEntitialFrame : _defenderBallistaLaunchEntitialFrame);
		}
		else if (type == DefaultSiegeEngineTypes.FireBallista)
		{
			result = ((side == BattleSideEnum.Attacker) ? _attackerFireBallistaLaunchEntitialFrame : _defenderFireBallistaLaunchEntitialFrame);
		}
		else if (type == DefaultSiegeEngineTypes.Trebuchet)
		{
			result = _attackerTrebuchetLaunchEntitialFrame;
		}
		else if (type == DefaultSiegeEngineTypes.Bricole)
		{
			result = _defenderTrebuchetLaunchEntitialFrame;
		}
		return result;
	}

	public Vec3 GetScaleForSiegeEngine(SiegeEngineType type, BattleSideEnum side)
	{
		Vec3 result = Vec3.Zero;
		if (type == DefaultSiegeEngineTypes.Onager)
		{
			result = _attackerMangonelScale;
		}
		else if (type == DefaultSiegeEngineTypes.FireOnager)
		{
			result = _attackerFireMangonelScale;
		}
		else if (type == DefaultSiegeEngineTypes.Catapult)
		{
			result = _defenderMangonelScale;
		}
		else if (type == DefaultSiegeEngineTypes.FireCatapult)
		{
			result = _defenderFireMangonelScale;
		}
		else if (type == DefaultSiegeEngineTypes.Ballista)
		{
			result = ((side == BattleSideEnum.Attacker) ? _attackerBallistaScale : _defenderBallistaScale);
		}
		else if (type == DefaultSiegeEngineTypes.FireBallista)
		{
			result = ((side == BattleSideEnum.Attacker) ? _attackerFireBallistaScale : _defenderFireBallistaScale);
		}
		else if (type == DefaultSiegeEngineTypes.Trebuchet)
		{
			result = _attackerTrebuchetScale;
		}
		else if (type == DefaultSiegeEngineTypes.Bricole)
		{
			result = _defenderTrebuchetScale;
		}
		return result;
	}
}

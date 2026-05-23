using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox
{
	// Token: 0x02000005 RID: 5
	public class CampaignMapSiegePrefabEntityCache : ScriptComponentBehavior
	{
		// Token: 0x06000008 RID: 8 RVA: 0x00002D48 File Offset: 0x00000F48
		protected override void OnInit()
		{
			base.OnInit();
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._attackerBallistaPrefab, true, true, "");
			this._attackerBallistaLaunchEntitialFrame = gameEntity.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			MatrixFrame frame = gameEntity.GetChild(0).GetFrame();
			this._attackerBallistaScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._defenderBallistaPrefab, true, true, "");
			this._defenderBallistaLaunchEntitialFrame = gameEntity2.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity2.GetChild(0).GetFrame();
			this._defenderBallistaScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity3 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._attackerFireBallistaPrefab, true, true, "");
			this._attackerFireBallistaLaunchEntitialFrame = gameEntity3.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity3.GetChild(0).GetFrame();
			this._attackerFireBallistaScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity4 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._defenderFireBallistaPrefab, true, true, "");
			this._defenderFireBallistaLaunchEntitialFrame = gameEntity4.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity4.GetChild(0).GetFrame();
			this._defenderFireBallistaScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity5 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._attackerMangonelPrefab, true, true, "");
			this._attackerMangonelLaunchEntitialFrame = gameEntity5.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity5.GetChild(0).GetFrame();
			this._attackerMangonelScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity6 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._defenderMangonelPrefab, true, true, "");
			this._defenderMangonelLaunchEntitialFrame = gameEntity6.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity6.GetChild(0).GetFrame();
			this._defenderMangonelScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity7 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._attackerFireMangonelPrefab, true, true, "");
			this._attackerFireMangonelLaunchEntitialFrame = gameEntity7.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity7.GetChild(0).GetFrame();
			this._attackerFireMangonelScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity8 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._defenderFireMangonelPrefab, true, true, "");
			this._defenderFireMangonelLaunchEntitialFrame = gameEntity8.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity8.GetChild(0).GetFrame();
			this._defenderFireMangonelScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity9 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._attackerTrebuchetPrefab, true, true, "");
			this._attackerTrebuchetLaunchEntitialFrame = gameEntity9.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity9.GetChild(0).GetFrame();
			this._attackerTrebuchetScale = frame.rotation.GetScaleVector();
			GameEntity gameEntity10 = TaleWorlds.Engine.GameEntity.Instantiate(((MapScene)Campaign.Current.MapSceneWrapper).Scene, this._defenderTrebuchetPrefab, true, true, "");
			this._defenderTrebuchetLaunchEntitialFrame = gameEntity10.GetChild(0).GetFirstChildEntityWithTag("projectile_position").GetGlobalFrame();
			frame = gameEntity10.GetChild(0).GetFrame();
			this._defenderTrebuchetScale = frame.rotation.GetScaleVector();
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000314C File Offset: 0x0000134C
		public MatrixFrame GetLaunchEntitialFrameForSiegeEngine(SiegeEngineType type, BattleSideEnum side)
		{
			MatrixFrame result = MatrixFrame.Identity;
			if (type == DefaultSiegeEngineTypes.Onager)
			{
				result = this._attackerMangonelLaunchEntitialFrame;
			}
			else if (type == DefaultSiegeEngineTypes.FireOnager)
			{
				result = this._attackerFireMangonelLaunchEntitialFrame;
			}
			else if (type == DefaultSiegeEngineTypes.Catapult)
			{
				result = this._defenderMangonelLaunchEntitialFrame;
			}
			else if (type == DefaultSiegeEngineTypes.FireCatapult)
			{
				result = this._defenderFireMangonelLaunchEntitialFrame;
			}
			else if (type == DefaultSiegeEngineTypes.Ballista)
			{
				result = ((side == BattleSideEnum.Attacker) ? this._attackerBallistaLaunchEntitialFrame : this._defenderBallistaLaunchEntitialFrame);
			}
			else if (type == DefaultSiegeEngineTypes.FireBallista)
			{
				result = ((side == BattleSideEnum.Attacker) ? this._attackerFireBallistaLaunchEntitialFrame : this._defenderFireBallistaLaunchEntitialFrame);
			}
			else if (type == DefaultSiegeEngineTypes.Trebuchet)
			{
				result = this._attackerTrebuchetLaunchEntitialFrame;
			}
			else if (type == DefaultSiegeEngineTypes.Bricole)
			{
				result = this._defenderTrebuchetLaunchEntitialFrame;
			}
			return result;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00003204 File Offset: 0x00001404
		public Vec3 GetScaleForSiegeEngine(SiegeEngineType type, BattleSideEnum side)
		{
			Vec3 result = Vec3.Zero;
			if (type == DefaultSiegeEngineTypes.Onager)
			{
				result = this._attackerMangonelScale;
			}
			else if (type == DefaultSiegeEngineTypes.FireOnager)
			{
				result = this._attackerFireMangonelScale;
			}
			else if (type == DefaultSiegeEngineTypes.Catapult)
			{
				result = this._defenderMangonelScale;
			}
			else if (type == DefaultSiegeEngineTypes.FireCatapult)
			{
				result = this._defenderFireMangonelScale;
			}
			else if (type == DefaultSiegeEngineTypes.Ballista)
			{
				result = ((side == BattleSideEnum.Attacker) ? this._attackerBallistaScale : this._defenderBallistaScale);
			}
			else if (type == DefaultSiegeEngineTypes.FireBallista)
			{
				result = ((side == BattleSideEnum.Attacker) ? this._attackerFireBallistaScale : this._defenderFireBallistaScale);
			}
			else if (type == DefaultSiegeEngineTypes.Trebuchet)
			{
				result = this._attackerTrebuchetScale;
			}
			else if (type == DefaultSiegeEngineTypes.Bricole)
			{
				result = this._defenderTrebuchetScale;
			}
			return result;
		}

		// Token: 0x04000001 RID: 1
		[EditableScriptComponentVariable(true, "")]
		private string _attackerBallistaPrefab = "ballista_a_mapicon";

		// Token: 0x04000002 RID: 2
		[EditableScriptComponentVariable(true, "")]
		private string _defenderBallistaPrefab = "ballista_b_mapicon";

		// Token: 0x04000003 RID: 3
		[EditableScriptComponentVariable(true, "")]
		private string _attackerFireBallistaPrefab = "ballista_a_fire_mapicon";

		// Token: 0x04000004 RID: 4
		[EditableScriptComponentVariable(true, "")]
		private string _defenderFireBallistaPrefab = "ballista_b_fire_mapicon";

		// Token: 0x04000005 RID: 5
		[EditableScriptComponentVariable(true, "")]
		private string _attackerMangonelPrefab = "mangonel_a_mapicon";

		// Token: 0x04000006 RID: 6
		[EditableScriptComponentVariable(true, "")]
		private string _defenderMangonelPrefab = "mangonel_b_mapicon";

		// Token: 0x04000007 RID: 7
		[EditableScriptComponentVariable(true, "")]
		private string _attackerFireMangonelPrefab = "mangonel_a_fire_mapicon";

		// Token: 0x04000008 RID: 8
		[EditableScriptComponentVariable(true, "")]
		private string _defenderFireMangonelPrefab = "mangonel_b_fire_mapicon";

		// Token: 0x04000009 RID: 9
		[EditableScriptComponentVariable(true, "")]
		private string _attackerTrebuchetPrefab = "trebuchet_a_mapicon";

		// Token: 0x0400000A RID: 10
		[EditableScriptComponentVariable(true, "")]
		private string _defenderTrebuchetPrefab = "trebuchet_b_mapicon";

		// Token: 0x0400000B RID: 11
		private MatrixFrame _attackerBallistaLaunchEntitialFrame;

		// Token: 0x0400000C RID: 12
		private MatrixFrame _defenderBallistaLaunchEntitialFrame;

		// Token: 0x0400000D RID: 13
		private MatrixFrame _attackerFireBallistaLaunchEntitialFrame;

		// Token: 0x0400000E RID: 14
		private MatrixFrame _defenderFireBallistaLaunchEntitialFrame;

		// Token: 0x0400000F RID: 15
		private MatrixFrame _attackerMangonelLaunchEntitialFrame;

		// Token: 0x04000010 RID: 16
		private MatrixFrame _defenderMangonelLaunchEntitialFrame;

		// Token: 0x04000011 RID: 17
		private MatrixFrame _attackerFireMangonelLaunchEntitialFrame;

		// Token: 0x04000012 RID: 18
		private MatrixFrame _defenderFireMangonelLaunchEntitialFrame;

		// Token: 0x04000013 RID: 19
		private MatrixFrame _attackerTrebuchetLaunchEntitialFrame;

		// Token: 0x04000014 RID: 20
		private MatrixFrame _defenderTrebuchetLaunchEntitialFrame;

		// Token: 0x04000015 RID: 21
		private Vec3 _attackerBallistaScale;

		// Token: 0x04000016 RID: 22
		private Vec3 _defenderBallistaScale;

		// Token: 0x04000017 RID: 23
		private Vec3 _attackerFireBallistaScale;

		// Token: 0x04000018 RID: 24
		private Vec3 _defenderFireBallistaScale;

		// Token: 0x04000019 RID: 25
		private Vec3 _attackerMangonelScale;

		// Token: 0x0400001A RID: 26
		private Vec3 _defenderMangonelScale;

		// Token: 0x0400001B RID: 27
		private Vec3 _attackerFireMangonelScale;

		// Token: 0x0400001C RID: 28
		private Vec3 _defenderFireMangonelScale;

		// Token: 0x0400001D RID: 29
		private Vec3 _attackerTrebuchetScale;

		// Token: 0x0400001E RID: 30
		private Vec3 _defenderTrebuchetScale;
	}
}

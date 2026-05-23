using System;
using TaleWorlds.InputSystem;

namespace TaleWorlds.Engine.InputSystem
{
	// Token: 0x020000AD RID: 173
	public class CheatsHotKeyCategory : GameKeyContext
	{
		// Token: 0x06000F77 RID: 3959 RVA: 0x00012718 File Offset: 0x00010918
		public CheatsHotKeyCategory()
			: base("Cheats", 0, GameKeyContext.GameKeyContextType.Default)
		{
			this.RegisterCheatHotkey("Pause", InputKey.F11, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyIncreaseCameraSpeed", InputKey.Up, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyDecreaseCameraSpeed", InputKey.Down, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("ResetCameraSpeed", InputKey.MiddleMouseButton, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyIncreaseSlowMotionFactor", InputKey.NumpadPlus, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyDecreaseSlowMotionFactor", InputKey.NumpadMinus, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("EnterSlowMotion", InputKey.F9, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeySwitchAgentToAi", InputKey.F5, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyControlFollowedAgent", InputKey.Numpad5, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyHealYourSelf", InputKey.H, HotKey.Modifiers.Control, HotKey.Modifiers.Shift);
			this.RegisterCheatHotkey("MissionScreenHotkeyHealYourHorse", InputKey.H, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillEnemyAgent", InputKey.F4, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillAllEnemyAgents", InputKey.F4, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillEnemyHorse", InputKey.F4, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillAllEnemyHorses", InputKey.F4, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillFriendlyAgent", InputKey.F2, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillAllFriendlyAgents", InputKey.F2, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillFriendlyHorse", InputKey.F2, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillAllFriendlyHorses", InputKey.F2, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillYourSelf", InputKey.F3, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyKillYourHorse", InputKey.F3, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyGhostCam", InputKey.K, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterCheatHotkey("MissionScreenHotkeyTeleportMainAgent", InputKey.X, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
		}

		// Token: 0x06000F78 RID: 3960 RVA: 0x00012894 File Offset: 0x00010A94
		private void RegisterCheatHotkey(string id, InputKey hotkeyKey, HotKey.Modifiers modifiers, HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
		{
			base.RegisterHotKey(new HotKey(id, "Cheats", hotkeyKey, modifiers, negativeModifiers), true);
		}

		// Token: 0x04000224 RID: 548
		public const string CategoryId = "Cheats";

		// Token: 0x04000225 RID: 549
		public const string MissionScreenHotkeyIncreaseCameraSpeed = "MissionScreenHotkeyIncreaseCameraSpeed";

		// Token: 0x04000226 RID: 550
		public const string MissionScreenHotkeyDecreaseCameraSpeed = "MissionScreenHotkeyDecreaseCameraSpeed";

		// Token: 0x04000227 RID: 551
		public const string ResetCameraSpeed = "ResetCameraSpeed";

		// Token: 0x04000228 RID: 552
		public const string MissionScreenHotkeyIncreaseSlowMotionFactor = "MissionScreenHotkeyIncreaseSlowMotionFactor";

		// Token: 0x04000229 RID: 553
		public const string MissionScreenHotkeyDecreaseSlowMotionFactor = "MissionScreenHotkeyDecreaseSlowMotionFactor";

		// Token: 0x0400022A RID: 554
		public const string EnterSlowMotion = "EnterSlowMotion";

		// Token: 0x0400022B RID: 555
		public const string Pause = "Pause";

		// Token: 0x0400022C RID: 556
		public const string MissionScreenHotkeyHealYourSelf = "MissionScreenHotkeyHealYourSelf";

		// Token: 0x0400022D RID: 557
		public const string MissionScreenHotkeyHealYourHorse = "MissionScreenHotkeyHealYourHorse";

		// Token: 0x0400022E RID: 558
		public const string MissionScreenHotkeyKillEnemyAgent = "MissionScreenHotkeyKillEnemyAgent";

		// Token: 0x0400022F RID: 559
		public const string MissionScreenHotkeyKillAllEnemyAgents = "MissionScreenHotkeyKillAllEnemyAgents";

		// Token: 0x04000230 RID: 560
		public const string MissionScreenHotkeyKillEnemyHorse = "MissionScreenHotkeyKillEnemyHorse";

		// Token: 0x04000231 RID: 561
		public const string MissionScreenHotkeyKillAllEnemyHorses = "MissionScreenHotkeyKillAllEnemyHorses";

		// Token: 0x04000232 RID: 562
		public const string MissionScreenHotkeyKillFriendlyAgent = "MissionScreenHotkeyKillFriendlyAgent";

		// Token: 0x04000233 RID: 563
		public const string MissionScreenHotkeyKillAllFriendlyAgents = "MissionScreenHotkeyKillAllFriendlyAgents";

		// Token: 0x04000234 RID: 564
		public const string MissionScreenHotkeyKillFriendlyHorse = "MissionScreenHotkeyKillFriendlyHorse";

		// Token: 0x04000235 RID: 565
		public const string MissionScreenHotkeyKillAllFriendlyHorses = "MissionScreenHotkeyKillAllFriendlyHorses";

		// Token: 0x04000236 RID: 566
		public const string MissionScreenHotkeyKillYourSelf = "MissionScreenHotkeyKillYourSelf";

		// Token: 0x04000237 RID: 567
		public const string MissionScreenHotkeyKillYourHorse = "MissionScreenHotkeyKillYourHorse";

		// Token: 0x04000238 RID: 568
		public const string MissionScreenHotkeyGhostCam = "MissionScreenHotkeyGhostCam";

		// Token: 0x04000239 RID: 569
		public const string MissionScreenHotkeySwitchAgentToAi = "MissionScreenHotkeySwitchAgentToAi";

		// Token: 0x0400023A RID: 570
		public const string MissionScreenHotkeyControlFollowedAgent = "MissionScreenHotkeyControlFollowedAgent";

		// Token: 0x0400023B RID: 571
		public const string MissionScreenHotkeyTeleportMainAgent = "MissionScreenHotkeyTeleportMainAgent";
	}
}

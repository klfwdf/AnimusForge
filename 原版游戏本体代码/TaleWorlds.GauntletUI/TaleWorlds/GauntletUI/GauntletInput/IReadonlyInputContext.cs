using System;
using System.Numerics;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.GauntletInput
{
	// Token: 0x02000049 RID: 73
	public interface IReadonlyInputContext
	{
		// Token: 0x0600044F RID: 1103
		bool GetIsMouseActive();

		// Token: 0x06000450 RID: 1104
		Vector2 GetMousePosition();

		// Token: 0x06000451 RID: 1105
		Vector2 GetMouseMovement();

		// Token: 0x06000452 RID: 1106
		InputKey[] GetClickKeys();

		// Token: 0x06000453 RID: 1107
		InputKey[] GetAlternateClickKeys();

		// Token: 0x06000454 RID: 1108
		Vector2 GetControllerLeftStickState();

		// Token: 0x06000455 RID: 1109
		Vector2 GetControllerRightStickState();

		// Token: 0x06000456 RID: 1110
		float GetMouseScrollDelta();
	}
}

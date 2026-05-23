using System;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000009 RID: 9
	public interface IInputContext
	{
		// Token: 0x060000AE RID: 174
		int GetPointerX();

		// Token: 0x060000AF RID: 175
		int GetPointerY();

		// Token: 0x060000B0 RID: 176
		Vector2 GetPointerPosition();

		// Token: 0x060000B1 RID: 177
		bool IsGameKeyDown(int gameKey);

		// Token: 0x060000B2 RID: 178
		bool IsGameKeyDownImmediate(int gameKey);

		// Token: 0x060000B3 RID: 179
		bool IsGameKeyReleased(int gameKey);

		// Token: 0x060000B4 RID: 180
		bool IsGameKeyPressed(int gameKey);

		// Token: 0x060000B5 RID: 181
		float GetGameKeyAxis(string gameKey);

		// Token: 0x060000B6 RID: 182
		bool IsHotKeyDown(string gameKey);

		// Token: 0x060000B7 RID: 183
		bool IsHotKeyReleased(string gameKey);

		// Token: 0x060000B8 RID: 184
		bool IsHotKeyPressed(string gameKey);

		// Token: 0x060000B9 RID: 185
		bool IsHotKeyDoublePressed(string gameKey);

		// Token: 0x060000BA RID: 186
		bool IsKeyDown(InputKey key);

		// Token: 0x060000BB RID: 187
		bool IsKeyPressed(InputKey key);

		// Token: 0x060000BC RID: 188
		bool IsKeyReleased(InputKey key);

		// Token: 0x060000BD RID: 189
		Vec2 GetKeyState(InputKey key);

		// Token: 0x060000BE RID: 190
		float GetMouseMoveX();

		// Token: 0x060000BF RID: 191
		float GetMouseMoveY();

		// Token: 0x060000C0 RID: 192
		Vec2 GetControllerRightStickState();

		// Token: 0x060000C1 RID: 193
		Vec2 GetControllerLeftStickState();

		// Token: 0x060000C2 RID: 194
		float GetDeltaMouseScroll();

		// Token: 0x060000C3 RID: 195
		bool GetIsControllerConnected();

		// Token: 0x060000C4 RID: 196
		bool GetIsMouseActive();

		// Token: 0x060000C5 RID: 197
		Vec2 GetMousePositionRanged();

		// Token: 0x060000C6 RID: 198
		Vec2 GetMousePositionPixel();

		// Token: 0x060000C7 RID: 199
		float GetMouseSensitivity();

		// Token: 0x060000C8 RID: 200
		bool IsControlDown();

		// Token: 0x060000C9 RID: 201
		bool IsShiftDown();

		// Token: 0x060000CA RID: 202
		bool IsAltDown();

		// Token: 0x060000CB RID: 203
		InputKey[] GetClickKeys();
	}
}

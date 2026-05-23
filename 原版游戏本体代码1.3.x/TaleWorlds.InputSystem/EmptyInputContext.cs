using System;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000002 RID: 2
	public sealed class EmptyInputContext : IInputContext
	{
		// Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
		public int GetPointerX()
		{
			return 0;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002053 File Offset: 0x00000253
		public int GetPointerY()
		{
			return 0;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002056 File Offset: 0x00000256
		public Vector2 GetPointerPosition()
		{
			return new Vector2(0f, 0f);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002067 File Offset: 0x00000267
		public bool IsGameKeyDown(int gameKey)
		{
			return false;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000206A File Offset: 0x0000026A
		public bool IsGameKeyDownImmediate(int gameKey)
		{
			return false;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000206D File Offset: 0x0000026D
		public bool IsGameKeyPressed(int gameKey)
		{
			return false;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002070 File Offset: 0x00000270
		public bool IsGameKeyReleased(int gameKey)
		{
			return false;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002073 File Offset: 0x00000273
		public float GetGameKeyAxis(string gameAxisKey)
		{
			return 0f;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000207A File Offset: 0x0000027A
		public bool IsHotKeyDown(string hotKey)
		{
			return false;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x0000207D File Offset: 0x0000027D
		public bool IsHotKeyReleased(string hotKey)
		{
			return false;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002080 File Offset: 0x00000280
		public bool IsHotKeyPressed(string hotKey)
		{
			return false;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002083 File Offset: 0x00000283
		public bool IsHotKeyDoublePressed(string hotKey)
		{
			return false;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002086 File Offset: 0x00000286
		public Vec2 GetKeyState(InputKey key)
		{
			return new Vec2(0f, 0f);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002097 File Offset: 0x00000297
		public bool IsKeyDown(InputKey key)
		{
			return false;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000209A File Offset: 0x0000029A
		public bool IsKeyPressed(InputKey key)
		{
			return false;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000209D File Offset: 0x0000029D
		public bool IsKeyReleased(InputKey key)
		{
			return false;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000020A0 File Offset: 0x000002A0
		public float GetMouseMoveX()
		{
			return 0f;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000020A7 File Offset: 0x000002A7
		public float GetMouseMoveY()
		{
			return 0f;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000020AE File Offset: 0x000002AE
		public bool GetIsMouseActive()
		{
			return false;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000020B4 File Offset: 0x000002B4
		public Vec2 GetMousePositionPixel()
		{
			return default(Vec2);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000020CA File Offset: 0x000002CA
		public float GetDeltaMouseScroll()
		{
			return 0f;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000020D1 File Offset: 0x000002D1
		public bool GetIsControllerConnected()
		{
			return false;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000020D4 File Offset: 0x000002D4
		public Vec2 GetMousePositionRanged()
		{
			return default(Vec2);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000020EA File Offset: 0x000002EA
		public float GetMouseSensitivity()
		{
			return 0f;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000020F1 File Offset: 0x000002F1
		public bool IsControlDown()
		{
			return false;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000020F4 File Offset: 0x000002F4
		public bool IsShiftDown()
		{
			return false;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000020F7 File Offset: 0x000002F7
		public bool IsAltDown()
		{
			return false;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000020FC File Offset: 0x000002FC
		public Vec2 GetControllerRightStickState()
		{
			return default(Vec2);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002114 File Offset: 0x00000314
		public Vec2 GetControllerLeftStickState()
		{
			return default(Vec2);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000212A File Offset: 0x0000032A
		public InputKey[] GetClickKeys()
		{
			return new InputKey[]
			{
				InputKey.LeftMouseButton,
				InputKey.ControllerRDown
			};
		}
	}
}

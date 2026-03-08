using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000005 RID: 5
	public class GameAxisKey
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00002762 File Offset: 0x00000962
		// (set) Token: 0x06000063 RID: 99 RVA: 0x0000276A File Offset: 0x0000096A
		public string Id { get; private set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00002773 File Offset: 0x00000973
		// (set) Token: 0x06000065 RID: 101 RVA: 0x0000277B File Offset: 0x0000097B
		public Key AxisKey { get; internal set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000066 RID: 102 RVA: 0x00002784 File Offset: 0x00000984
		// (set) Token: 0x06000067 RID: 103 RVA: 0x0000278C File Offset: 0x0000098C
		public Key DefaultAxisKey { get; private set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00002795 File Offset: 0x00000995
		// (set) Token: 0x06000069 RID: 105 RVA: 0x0000279D File Offset: 0x0000099D
		public GameKey PositiveKey { get; internal set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600006A RID: 106 RVA: 0x000027A6 File Offset: 0x000009A6
		// (set) Token: 0x0600006B RID: 107 RVA: 0x000027AE File Offset: 0x000009AE
		public GameKey NegativeKey { get; internal set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600006C RID: 108 RVA: 0x000027B7 File Offset: 0x000009B7
		// (set) Token: 0x0600006D RID: 109 RVA: 0x000027BF File Offset: 0x000009BF
		public GameAxisKey.AxisType Type { get; private set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600006E RID: 110 RVA: 0x000027C8 File Offset: 0x000009C8
		// (set) Token: 0x0600006F RID: 111 RVA: 0x000027D0 File Offset: 0x000009D0
		internal bool IsBinded { get; private set; }

		// Token: 0x06000070 RID: 112 RVA: 0x000027DC File Offset: 0x000009DC
		public GameAxisKey(string id, InputKey axisKey, GameKey positiveKey, GameKey negativeKey, GameAxisKey.AxisType type = GameAxisKey.AxisType.X)
		{
			this.Id = id;
			this.AxisKey = new Key(axisKey);
			this.DefaultAxisKey = new Key(axisKey);
			this.PositiveKey = positiveKey;
			this.NegativeKey = negativeKey;
			this.Type = type;
			this.IsBinded = this.PositiveKey != null || this.NegativeKey != null;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x0000283F File Offset: 0x00000A3F
		private bool IsKeyAllowed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return (isKeysAllowed || !key.IsKeyboardInput) && (isMouseButtonAllowed || !key.IsMouseButtonInput) && (isMouseWheelAllowed || !key.IsMouseWheelInput) && (isControllerAllowed || !key.IsControllerInput);
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00002874 File Offset: 0x00000A74
		public float GetAxisState(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			GameKey positiveKey = this.PositiveKey;
			bool flag = positiveKey != null && positiveKey.IsDown(isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed, false);
			GameKey negativeKey = this.NegativeKey;
			bool flag2 = negativeKey != null && negativeKey.IsDown(isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed, false);
			if (flag || flag2)
			{
				return (flag ? 1f : 0f) - (flag2 ? 1f : 0f);
			}
			Vec2 keyState = new Vec2(0f, 0f);
			if (this.AxisKey != null && this.IsKeyAllowed(this.AxisKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				keyState = this.AxisKey.GetKeyState();
			}
			if (this.Type == GameAxisKey.AxisType.X)
			{
				return keyState.X;
			}
			if (this.Type == GameAxisKey.AxisType.Y)
			{
				return keyState.Y;
			}
			return 0f;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x0000293C File Offset: 0x00000B3C
		public override string ToString()
		{
			string result = "";
			if (this.AxisKey != null)
			{
				result = this.AxisKey.ToString();
			}
			return result;
		}

		// Token: 0x02000011 RID: 17
		public enum AxisType
		{
			// Token: 0x04000151 RID: 337
			X,
			// Token: 0x04000152 RID: 338
			Y
		}
	}
}

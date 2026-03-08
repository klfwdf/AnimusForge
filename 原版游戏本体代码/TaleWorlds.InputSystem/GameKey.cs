using System;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000004 RID: 4
	public class GameKey
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000046 RID: 70 RVA: 0x000021F3 File Offset: 0x000003F3
		// (set) Token: 0x06000047 RID: 71 RVA: 0x000021FB File Offset: 0x000003FB
		public int Id { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000048 RID: 72 RVA: 0x00002204 File Offset: 0x00000404
		// (set) Token: 0x06000049 RID: 73 RVA: 0x0000220C File Offset: 0x0000040C
		public string StringId { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00002215 File Offset: 0x00000415
		// (set) Token: 0x0600004B RID: 75 RVA: 0x0000221D File Offset: 0x0000041D
		public string GroupId { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00002226 File Offset: 0x00000426
		// (set) Token: 0x0600004D RID: 77 RVA: 0x0000222E File Offset: 0x0000042E
		public string MainCategoryId { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600004E RID: 78 RVA: 0x00002237 File Offset: 0x00000437
		// (set) Token: 0x0600004F RID: 79 RVA: 0x0000223F File Offset: 0x0000043F
		public Key KeyboardKey { get; internal set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000050 RID: 80 RVA: 0x00002248 File Offset: 0x00000448
		// (set) Token: 0x06000051 RID: 81 RVA: 0x00002250 File Offset: 0x00000450
		public Key DefaultKeyboardKey { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00002259 File Offset: 0x00000459
		// (set) Token: 0x06000053 RID: 83 RVA: 0x00002261 File Offset: 0x00000461
		public Key ControllerKey { get; internal set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000054 RID: 84 RVA: 0x0000226A File Offset: 0x0000046A
		// (set) Token: 0x06000055 RID: 85 RVA: 0x00002272 File Offset: 0x00000472
		public Key DefaultControllerKey { get; internal set; }

		// Token: 0x06000056 RID: 86 RVA: 0x0000227C File Offset: 0x0000047C
		public GameKey(int id, string stringId, string groupId, InputKey defaultKeyboardKey, InputKey defaultControllerKey, string mainCategoryId = "")
		{
			this.Id = id;
			this.StringId = stringId;
			this.GroupId = groupId;
			this.MainCategoryId = mainCategoryId;
			this.KeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
			this.DefaultKeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
			this.ControllerKey = ((defaultControllerKey != InputKey.Invalid) ? new Key(defaultControllerKey) : null);
			this.DefaultControllerKey = ((defaultControllerKey != InputKey.Invalid) ? new Key(defaultControllerKey) : null);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00002300 File Offset: 0x00000500
		public GameKey(int id, string stringId, string groupId, InputKey defaultKeyboardKey, string mainCategoryId = "")
		{
			this.Id = id;
			this.StringId = stringId;
			this.GroupId = groupId;
			this.MainCategoryId = mainCategoryId;
			this.KeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
			this.DefaultKeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
			this.ControllerKey = new Key(InputKey.Invalid);
			this.DefaultControllerKey = new Key(InputKey.Invalid);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002372 File Offset: 0x00000572
		private bool IsKeyAllowed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return (isKeysAllowed || !key.IsKeyboardInput) && (isMouseButtonAllowed || !key.IsMouseButtonInput) && (isMouseWheelAllowed || !key.IsMouseWheelInput) && (isControllerAllowed || !key.IsControllerInput);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000023A8 File Offset: 0x000005A8
		internal bool IsUp(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			bool flag = false;
			if (this.KeyboardKey != null && this.IsKeyAllowed(this.KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || !this.KeyboardKey.IsDown();
			}
			if (this.ControllerKey != null && this.IsKeyAllowed(this.ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || !this.ControllerKey.IsDown();
			}
			return flag;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00002424 File Offset: 0x00000624
		internal bool IsDown(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed, bool checkControllerKey = true)
		{
			bool flag = false;
			if (this.KeyboardKey != null && this.IsKeyAllowed(this.KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.KeyboardKey.IsDown();
			}
			if (checkControllerKey && this.ControllerKey != null && this.IsKeyAllowed(this.ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.ControllerKey.IsDown();
			}
			return flag;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x000024A0 File Offset: 0x000006A0
		internal bool IsDownImmediate(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			bool flag = false;
			if (this.KeyboardKey != null && this.IsKeyAllowed(this.KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.KeyboardKey.IsDownImmediate();
			}
			if (this.ControllerKey != null && this.IsKeyAllowed(this.ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.ControllerKey.IsDownImmediate();
			}
			return flag;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002518 File Offset: 0x00000718
		internal bool IsPressed(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			bool flag = false;
			if (this.KeyboardKey != null && this.IsKeyAllowed(this.KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.KeyboardKey.IsPressed();
			}
			if (this.ControllerKey != null && this.IsKeyAllowed(this.ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.ControllerKey.IsPressed();
			}
			return flag;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002590 File Offset: 0x00000790
		internal bool IsReleased(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			bool flag = false;
			if (this.KeyboardKey != null && this.IsKeyAllowed(this.KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.KeyboardKey.IsReleased();
			}
			if (this.ControllerKey != null && this.IsKeyAllowed(this.ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				flag = flag || this.ControllerKey.IsReleased();
			}
			return flag;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002608 File Offset: 0x00000808
		internal float GetKeyState(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			float num = 0f;
			if (this.KeyboardKey != null && this.IsKeyAllowed(this.KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				num = this.KeyboardKey.GetKeyState().X;
			}
			if (num == 0f && this.ControllerKey != null && this.IsKeyAllowed(this.ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				num = this.ControllerKey.GetKeyState().X;
			}
			return num;
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002690 File Offset: 0x00000890
		public override string ToString()
		{
			string result = "invalid";
			bool flag = Input.IsControllerConnected && !Input.IsMouseActive;
			if (!flag && this.KeyboardKey != null)
			{
				result = this.KeyboardKey.ToString();
			}
			else if (flag && this.ControllerKey != null)
			{
				result = this.ControllerKey.ToString();
			}
			return result;
		}

		// Token: 0x06000060 RID: 96 RVA: 0x000026F4 File Offset: 0x000008F4
		public override bool Equals(object obj)
		{
			GameKey gameKey = obj as GameKey;
			return gameKey != null && gameKey.Id.Equals(this.Id) && gameKey.GroupId.Equals(this.GroupId) && gameKey.KeyboardKey == this.KeyboardKey && gameKey.ControllerKey == this.ControllerKey;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x0000275A File Offset: 0x0000095A
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}

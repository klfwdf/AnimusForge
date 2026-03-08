using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x0200000E RID: 14
	public class MouseWidget : Widget
	{
		// Token: 0x060000C9 RID: 201 RVA: 0x00005733 File Offset: 0x00003933
		public MouseWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x060000CA RID: 202 RVA: 0x0000573C File Offset: 0x0000393C
		protected override void OnUpdate(float dt)
		{
			if (base.IsVisible)
			{
				this.UpdatePressedKeys();
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000574C File Offset: 0x0000394C
		public void UpdatePressedKeys()
		{
			Color color = new Color(1f, 0f, 0f, 1f);
			this.LeftMouseButton.Color = Color.White;
			this.RightMouseButton.Color = Color.White;
			this.MiddleMouseButton.Color = Color.White;
			this.MouseX1Button.Color = Color.White;
			this.MouseX2Button.Color = Color.White;
			this.MouseScrollUp.IsVisible = false;
			this.MouseScrollDown.IsVisible = false;
			this.KeyboardKeys.Text = "";
			if (Input.IsKeyDown(InputKey.LeftMouseButton))
			{
				this.LeftMouseButton.Color = color;
			}
			if (Input.IsKeyDown(InputKey.RightMouseButton))
			{
				this.RightMouseButton.Color = color;
			}
			if (Input.IsKeyDown(InputKey.MiddleMouseButton))
			{
				this.MiddleMouseButton.Color = color;
			}
			if (Input.IsKeyDown(InputKey.X1MouseButton))
			{
				this.MouseX1Button.Color = color;
			}
			if (Input.IsKeyDown(InputKey.X2MouseButton))
			{
				this.MouseX2Button.Color = color;
			}
			if (Input.IsKeyDown(InputKey.MouseScrollUp))
			{
				this.MouseScrollUp.IsVisible = true;
			}
			if (Input.IsKeyDown(InputKey.MouseScrollDown))
			{
				this.MouseScrollDown.IsVisible = true;
			}
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, "UpdatePressedKeys");
			for (int i = 0; i < 256; i++)
			{
				if (Key.GetInputType((InputKey)i) == Key.InputType.Keyboard && Input.IsKeyDown((InputKey)i))
				{
					InputKey inputKey = (InputKey)i;
					mbstringBuilder.Append<string>(inputKey.ToString());
					mbstringBuilder.Append<string>(", ");
				}
			}
			this.KeyboardKeys.Text = mbstringBuilder.ToStringAndRelease().TrimEnd(MouseWidget._trimChars);
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060000CC RID: 204 RVA: 0x0000590A File Offset: 0x00003B0A
		// (set) Token: 0x060000CD RID: 205 RVA: 0x00005912 File Offset: 0x00003B12
		public Widget LeftMouseButton { get; set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060000CE RID: 206 RVA: 0x0000591B File Offset: 0x00003B1B
		// (set) Token: 0x060000CF RID: 207 RVA: 0x00005923 File Offset: 0x00003B23
		public Widget RightMouseButton { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060000D0 RID: 208 RVA: 0x0000592C File Offset: 0x00003B2C
		// (set) Token: 0x060000D1 RID: 209 RVA: 0x00005934 File Offset: 0x00003B34
		public Widget MiddleMouseButton { get; set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x060000D2 RID: 210 RVA: 0x0000593D File Offset: 0x00003B3D
		// (set) Token: 0x060000D3 RID: 211 RVA: 0x00005945 File Offset: 0x00003B45
		public Widget MouseX1Button { get; set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060000D4 RID: 212 RVA: 0x0000594E File Offset: 0x00003B4E
		// (set) Token: 0x060000D5 RID: 213 RVA: 0x00005956 File Offset: 0x00003B56
		public Widget MouseX2Button { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060000D6 RID: 214 RVA: 0x0000595F File Offset: 0x00003B5F
		// (set) Token: 0x060000D7 RID: 215 RVA: 0x00005967 File Offset: 0x00003B67
		public Widget MouseScrollUp { get; set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x00005970 File Offset: 0x00003B70
		// (set) Token: 0x060000D9 RID: 217 RVA: 0x00005978 File Offset: 0x00003B78
		public Widget MouseScrollDown { get; set; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060000DA RID: 218 RVA: 0x00005981 File Offset: 0x00003B81
		// (set) Token: 0x060000DB RID: 219 RVA: 0x00005989 File Offset: 0x00003B89
		public TextWidget KeyboardKeys { get; set; }

		// Token: 0x04000058 RID: 88
		private static readonly char[] _trimChars = new char[] { ' ', ',' };
	}
}

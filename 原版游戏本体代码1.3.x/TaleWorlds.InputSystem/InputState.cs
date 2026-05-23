using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x0200000F RID: 15
	public class InputState
	{
		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000174 RID: 372 RVA: 0x00004EB4 File Offset: 0x000030B4
		public Vec2 NativeResolution
		{
			get
			{
				return Input.Resolution;
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000175 RID: 373 RVA: 0x00004EBB File Offset: 0x000030BB
		// (set) Token: 0x06000176 RID: 374 RVA: 0x00004EC4 File Offset: 0x000030C4
		public Vec2 MousePositionRanged
		{
			get
			{
				return this._mousePositionRanged;
			}
			set
			{
				this._mousePositionRanged = value;
				this._mousePositionPixel = new Vec2(this._mousePositionRanged.x * this.NativeResolution.x, this._mousePositionRanged.y * this.NativeResolution.y);
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000177 RID: 375 RVA: 0x00004F11 File Offset: 0x00003111
		// (set) Token: 0x06000178 RID: 376 RVA: 0x00004F19 File Offset: 0x00003119
		public Vec2 OldMousePositionRanged { get; private set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000179 RID: 377 RVA: 0x00004F22 File Offset: 0x00003122
		// (set) Token: 0x0600017A RID: 378 RVA: 0x00004F2A File Offset: 0x0000312A
		public bool MousePositionChanged { get; private set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x0600017B RID: 379 RVA: 0x00004F33 File Offset: 0x00003133
		// (set) Token: 0x0600017C RID: 380 RVA: 0x00004F3C File Offset: 0x0000313C
		public Vec2 MousePositionPixel
		{
			get
			{
				return this._mousePositionPixel;
			}
			set
			{
				this._mousePositionPixel = value;
				this._mousePositionRanged = new Vec2(this._mousePositionPixel.x / Input.Resolution.x, this._mousePositionPixel.y / this.NativeResolution.y);
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x0600017D RID: 381 RVA: 0x00004F88 File Offset: 0x00003188
		// (set) Token: 0x0600017E RID: 382 RVA: 0x00004F90 File Offset: 0x00003190
		public Vec2 OldMousePositionPixel { get; private set; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600017F RID: 383 RVA: 0x00004F99 File Offset: 0x00003199
		// (set) Token: 0x06000180 RID: 384 RVA: 0x00004FA1 File Offset: 0x000031A1
		public float MouseScrollValue { get; private set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000181 RID: 385 RVA: 0x00004FAA File Offset: 0x000031AA
		// (set) Token: 0x06000182 RID: 386 RVA: 0x00004FB2 File Offset: 0x000031B2
		public bool MouseScrollChanged { get; private set; }

		// Token: 0x06000183 RID: 387 RVA: 0x00004FBC File Offset: 0x000031BC
		public InputState()
		{
			this.MousePositionRanged = default(Vec2);
			this.OldMousePositionRanged = default(Vec2);
			this.MousePositionPixel = default(Vec2);
			this.OldMousePositionPixel = default(Vec2);
			this._mousePositionRanged = new Vec2(0f, 0f);
			this._mousePositionPixel = new Vec2(0f, 0f);
			this._mousePositionPixelDevice = new Vec2(0f, 0f);
			this._mousePositionRangedDevice = new Vec2(0f, 0f);
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00005060 File Offset: 0x00003260
		public bool UpdateMousePosition(float mousePositionX, float mousePositionY)
		{
			this.OldMousePositionRanged = new Vec2(this._mousePositionRangedDevice.x, this._mousePositionRangedDevice.y);
			this._mousePositionRangedDevice = new Vec2(mousePositionX, mousePositionY);
			this.OldMousePositionPixel = new Vec2(this._mousePositionPixelDevice.x, this._mousePositionPixelDevice.y);
			this._mousePositionPixelDevice = new Vec2(this._mousePositionRangedDevice.x * this.NativeResolution.x, this._mousePositionRangedDevice.y * this.NativeResolution.y);
			if (this._mousePositionRangedDevice.x == this.OldMousePositionRanged.x && this._mousePositionRangedDevice.y == this.OldMousePositionRanged.y)
			{
				this.MousePositionChanged = false;
			}
			else
			{
				this.MousePositionChanged = true;
				this.MousePositionPixel = this._mousePositionPixelDevice;
				this.MousePositionRanged = this._mousePositionRangedDevice;
			}
			return this.MousePositionChanged;
		}

		// Token: 0x06000185 RID: 389 RVA: 0x00005154 File Offset: 0x00003354
		public bool UpdateMouseScroll(float mouseScrollValue)
		{
			if (!this.MouseScrollValue.Equals(mouseScrollValue))
			{
				this.MouseScrollValue = mouseScrollValue;
				this.MouseScrollChanged = true;
			}
			else
			{
				this.MouseScrollChanged = false;
			}
			return this.MouseScrollChanged;
		}

		// Token: 0x04000142 RID: 322
		private Vec2 _mousePositionRanged;

		// Token: 0x04000144 RID: 324
		private Vec2 _mousePositionRangedDevice;

		// Token: 0x04000146 RID: 326
		private Vec2 _mousePositionPixel;

		// Token: 0x04000147 RID: 327
		private Vec2 _mousePositionPixelDevice;
	}
}

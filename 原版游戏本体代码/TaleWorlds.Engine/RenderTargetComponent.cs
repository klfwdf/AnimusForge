using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200007D RID: 125
	public sealed class RenderTargetComponent : DotNetObject
	{
		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000AA6 RID: 2726 RVA: 0x0000AF5A File Offset: 0x0000915A
		public Texture RenderTarget
		{
			get
			{
				return (Texture)this._renderTargetWeakReference.GetNativeObject();
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000AA7 RID: 2727 RVA: 0x0000AF6C File Offset: 0x0000916C
		// (set) Token: 0x06000AA8 RID: 2728 RVA: 0x0000AF74 File Offset: 0x00009174
		public object UserData { get; internal set; }

		// Token: 0x06000AA9 RID: 2729 RVA: 0x0000AF7D File Offset: 0x0000917D
		internal RenderTargetComponent(Texture renderTarget)
		{
			this._renderTargetWeakReference = new WeakNativeObjectReference(renderTarget);
		}

		// Token: 0x06000AAA RID: 2730 RVA: 0x0000AF91 File Offset: 0x00009191
		internal void OnTargetReleased()
		{
			this.PaintNeeded = null;
		}

		// Token: 0x06000AAB RID: 2731 RVA: 0x0000AF9A File Offset: 0x0000919A
		[EngineCallback(null, false)]
		internal static RenderTargetComponent CreateRenderTargetComponent(Texture renderTarget)
		{
			return new RenderTargetComponent(renderTarget);
		}

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x06000AAC RID: 2732 RVA: 0x0000AFA4 File Offset: 0x000091A4
		// (remove) Token: 0x06000AAD RID: 2733 RVA: 0x0000AFDC File Offset: 0x000091DC
		internal event RenderTargetComponent.TextureUpdateEventHandler PaintNeeded;

		// Token: 0x06000AAE RID: 2734 RVA: 0x0000B011 File Offset: 0x00009211
		[EngineCallback(null, false)]
		internal void OnPaintNeeded()
		{
			RenderTargetComponent.TextureUpdateEventHandler paintNeeded = this.PaintNeeded;
			if (paintNeeded == null)
			{
				return;
			}
			paintNeeded(this.RenderTarget, EventArgs.Empty);
		}

		// Token: 0x04000193 RID: 403
		private readonly WeakNativeObjectReference _renderTargetWeakReference;

		// Token: 0x020000CE RID: 206
		// (Invoke) Token: 0x06000FF3 RID: 4083
		public delegate void TextureUpdateEventHandler(Texture sender, EventArgs e);
	}
}

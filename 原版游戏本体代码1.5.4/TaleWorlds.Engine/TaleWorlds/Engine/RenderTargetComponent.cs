using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

public sealed class RenderTargetComponent : DotNetObject
{
	public delegate void TextureUpdateEventHandler(Texture sender, EventArgs e);

	private readonly WeakNativeObjectReference _renderTargetWeakReference;

	public Texture RenderTarget => (Texture)_renderTargetWeakReference.GetNativeObject();

	public object UserData { get; internal set; }

	internal event TextureUpdateEventHandler PaintNeeded;

	internal RenderTargetComponent(Texture renderTarget)
	{
		_renderTargetWeakReference = new WeakNativeObjectReference(renderTarget);
	}

	internal void OnTargetReleased()
	{
		this.PaintNeeded = null;
	}

	[EngineCallback(null, false)]
	internal static RenderTargetComponent CreateRenderTargetComponent(Texture renderTarget)
	{
		return new RenderTargetComponent(renderTarget);
	}

	[EngineCallback(null, false)]
	internal void OnPaintNeeded()
	{
		this.PaintNeeded?.Invoke(RenderTarget, EventArgs.Empty);
	}
}

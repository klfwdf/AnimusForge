using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglTexture_view")]
public sealed class TextureView : View
{
	internal TextureView(UIntPtr meshPointer)
		: base(meshPointer)
	{
	}

	public static TextureView CreateTextureView()
	{
		return EngineApplicationInterface.ITextureView.CreateTextureView();
	}

	public void SetTexture(Texture texture)
	{
		EngineApplicationInterface.ITextureView.SetTexture(base.Pointer, texture.Pointer);
	}
}

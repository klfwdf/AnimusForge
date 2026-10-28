using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglTableau_view")]
public sealed class TableauView : SceneView
{
	internal TableauView(UIntPtr meshPointer)
		: base(meshPointer)
	{
	}

	public static TableauView CreateTableauView(string viewName)
	{
		return EngineApplicationInterface.ITableauView.CreateTableauView(viewName);
	}

	public void SetSortingEnabled(bool value)
	{
		EngineApplicationInterface.ITableauView.SetSortingEnabled(base.Pointer, value);
	}

	public void SetContinuousRendering(bool value)
	{
		EngineApplicationInterface.ITableauView.SetContinousRendering(base.Pointer, value);
	}

	public void SetDoNotRenderThisFrame(bool value)
	{
		EngineApplicationInterface.ITableauView.SetDoNotRenderThisFrame(base.Pointer, value);
	}

	public void SetDeleteAfterRendering(bool value)
	{
		EngineApplicationInterface.ITableauView.SetDeleteAfterRendering(base.Pointer, value);
	}

	public static Texture AddTableau(string name, RenderTargetComponent.TextureUpdateEventHandler eventHandler, object objectRef, int tableauSizeX, int tableauSizeY)
	{
		Texture texture = Texture.CreateTableauTexture(name, eventHandler, objectRef, tableauSizeX, tableauSizeY);
		texture.TableauView.SetRenderOnDemand(value: false);
		return texture;
	}
}

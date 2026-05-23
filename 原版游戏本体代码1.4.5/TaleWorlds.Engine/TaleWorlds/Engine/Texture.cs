using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public sealed class Texture : Resource
{
	public bool IsReleased { get; private set; }

	public int Width => EngineApplicationInterface.ITexture.GetWidth(base.Pointer);

	public int Height => EngineApplicationInterface.ITexture.GetHeight(base.Pointer);

	public int MemorySize => EngineApplicationInterface.ITexture.GetMemorySize(base.Pointer);

	public bool IsRenderTarget => EngineApplicationInterface.ITexture.IsRenderTarget(base.Pointer);

	public string Name
	{
		get
		{
			return EngineApplicationInterface.ITexture.GetName(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.ITexture.SetName(base.Pointer, value);
		}
	}

	public RenderTargetComponent RenderTargetComponent => EngineApplicationInterface.ITexture.GetRenderTargetComponent(base.Pointer);

	public TableauView TableauView => EngineApplicationInterface.ITexture.GetTableauView(base.Pointer);

	public object UserData => RenderTargetComponent.UserData;

	private Texture()
	{
	}

	internal Texture(UIntPtr ptr)
		: base(ptr)
	{
	}

	public static Texture CreateTextureFromPath(PlatformFilePath filePath)
	{
		return EngineApplicationInterface.ITexture.CreateTextureFromPath(filePath);
	}

	public void GetPixelData(byte[] bytes)
	{
		EngineApplicationInterface.ITexture.GetPixelData(base.Pointer, bytes);
	}

	public void TransformRenderTargetToResource(string name)
	{
		EngineApplicationInterface.ITexture.TransformRenderTargetToResourceTexture(base.Pointer, name);
	}

	public static Texture GetFromResource(string resourceName)
	{
		return EngineApplicationInterface.ITexture.GetFromResource(resourceName);
	}

	public bool IsLoaded()
	{
		return EngineApplicationInterface.ITexture.IsLoaded(base.Pointer);
	}

	public void GetSDFBoundingBoxData(ref Vec3 min, ref Vec3 max)
	{
		EngineApplicationInterface.ITexture.GetSDFBoundingBoxData(base.Pointer, ref min, ref max);
	}

	public static Texture CheckAndGetFromResource(string resourceName)
	{
		return EngineApplicationInterface.ITexture.CheckAndGetFromResource(resourceName);
	}

	public static void ScaleTextureWithRatio(ref int tableauSizeX, ref int tableauSizeY)
	{
		float num = tableauSizeX;
		float num2 = tableauSizeY;
		int num3 = (int)TaleWorlds.Library.MathF.Log(num, 2f) + 2;
		float num4 = TaleWorlds.Library.MathF.Pow(2f, num3) / num;
		tableauSizeX = (int)(num * num4);
		tableauSizeY = (int)(num2 * num4);
	}

	public void PreloadTexture(bool blocking)
	{
		EngineApplicationInterface.ITexture.GetCurObject(base.Pointer, blocking);
	}

	public void Release()
	{
		IsReleased = true;
		RenderTargetComponent.OnTargetReleased();
		ManualInvalidate();
	}

	public void ReleaseImmediately()
	{
		IsReleased = true;
		RenderTargetComponent.OnTargetReleased();
		EngineApplicationInterface.ITexture.Release(base.Pointer);
	}

	public void ReleaseAfterNumberOfFrames(int frameCount)
	{
		RenderTargetComponent.OnTargetReleased();
		EngineApplicationInterface.ITexture.ReleaseAfterNumberOfFrames(base.Pointer, frameCount);
	}

	public static Texture LoadTextureFromPath(string fileName, string folder)
	{
		return EngineApplicationInterface.ITexture.LoadTextureFromPath(fileName, folder);
	}

	public static Texture CreateDepthTarget(string name, int width, int height)
	{
		return EngineApplicationInterface.ITexture.CreateDepthTarget(name, width, height);
	}

	public static Texture CreateFromByteArray(byte[] data, int width, int height)
	{
		return EngineApplicationInterface.ITexture.CreateFromByteArray(data, width, height);
	}

	public void SaveToFile(string path, bool isRelativePath)
	{
		EngineApplicationInterface.ITexture.SaveToFile(base.Pointer, path, isRelativePath);
	}

	public void SetTextureAsAlwaysValid()
	{
		EngineApplicationInterface.ITexture.SaveTextureAsAlwaysValid(base.Pointer);
	}

	public static Texture CreateFromMemory(byte[] data)
	{
		return EngineApplicationInterface.ITexture.CreateFromMemory(data);
	}

	public static void ReleaseGpuMemories()
	{
		EngineApplicationInterface.ITexture.ReleaseGpuMemories();
	}

	private void SetTableauView(TableauView tableauView)
	{
		EngineApplicationInterface.ITexture.SetTableauView(base.Pointer, tableauView.Pointer);
	}

	public static Texture CreateTableauTexture(string name, RenderTargetComponent.TextureUpdateEventHandler eventHandler, object objectRef, int tableauSizeX, int tableauSizeY)
	{
		Texture texture = CreateRenderTarget(name, tableauSizeX, tableauSizeY, autoMipmaps: true, isTableau: true);
		RenderTargetComponent renderTargetComponent = texture.RenderTargetComponent;
		renderTargetComponent.PaintNeeded += eventHandler;
		renderTargetComponent.UserData = objectRef;
		TableauView tableauView = TableauView.CreateTableauView(name);
		tableauView.SetRenderTarget(texture);
		tableauView.SetAutoDepthTargetCreation(value: true);
		tableauView.SetSceneUsesSkybox(value: false);
		tableauView.SetClearColor(4294902015u);
		texture.SetTableauView(tableauView);
		return texture;
	}

	public static Texture CreateRenderTarget(string name, int width, int height, bool autoMipmaps, bool isTableau, bool createUninitialized = false, bool always_valid = false)
	{
		return EngineApplicationInterface.ITexture.CreateRenderTarget(name, width, height, autoMipmaps, isTableau, createUninitialized, always_valid);
	}
}

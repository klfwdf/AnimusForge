using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000091 RID: 145
	public sealed class Texture : Resource
	{
		// Token: 0x17000094 RID: 148
		// (get) Token: 0x06000CDF RID: 3295 RVA: 0x0000E48C File Offset: 0x0000C68C
		// (set) Token: 0x06000CE0 RID: 3296 RVA: 0x0000E494 File Offset: 0x0000C694
		public bool IsReleased { get; private set; }

		// Token: 0x06000CE1 RID: 3297 RVA: 0x0000E49D File Offset: 0x0000C69D
		private Texture()
		{
		}

		// Token: 0x06000CE2 RID: 3298 RVA: 0x0000E4A5 File Offset: 0x0000C6A5
		internal Texture(UIntPtr ptr)
			: base(ptr)
		{
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x06000CE3 RID: 3299 RVA: 0x0000E4AE File Offset: 0x0000C6AE
		public int Width
		{
			get
			{
				return EngineApplicationInterface.ITexture.GetWidth(base.Pointer);
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x06000CE4 RID: 3300 RVA: 0x0000E4C0 File Offset: 0x0000C6C0
		public int Height
		{
			get
			{
				return EngineApplicationInterface.ITexture.GetHeight(base.Pointer);
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x06000CE5 RID: 3301 RVA: 0x0000E4D2 File Offset: 0x0000C6D2
		public int MemorySize
		{
			get
			{
				return EngineApplicationInterface.ITexture.GetMemorySize(base.Pointer);
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x06000CE6 RID: 3302 RVA: 0x0000E4E4 File Offset: 0x0000C6E4
		public bool IsRenderTarget
		{
			get
			{
				return EngineApplicationInterface.ITexture.IsRenderTarget(base.Pointer);
			}
		}

		// Token: 0x06000CE7 RID: 3303 RVA: 0x0000E4F6 File Offset: 0x0000C6F6
		public static Texture CreateTextureFromPath(PlatformFilePath filePath)
		{
			return EngineApplicationInterface.ITexture.CreateTextureFromPath(filePath);
		}

		// Token: 0x06000CE8 RID: 3304 RVA: 0x0000E503 File Offset: 0x0000C703
		public void GetPixelData(byte[] bytes)
		{
			EngineApplicationInterface.ITexture.GetPixelData(base.Pointer, bytes);
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x06000CE9 RID: 3305 RVA: 0x0000E516 File Offset: 0x0000C716
		// (set) Token: 0x06000CEA RID: 3306 RVA: 0x0000E528 File Offset: 0x0000C728
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

		// Token: 0x06000CEB RID: 3307 RVA: 0x0000E53B File Offset: 0x0000C73B
		public void TransformRenderTargetToResource(string name)
		{
			EngineApplicationInterface.ITexture.TransformRenderTargetToResourceTexture(base.Pointer, name);
		}

		// Token: 0x06000CEC RID: 3308 RVA: 0x0000E54E File Offset: 0x0000C74E
		public static Texture GetFromResource(string resourceName)
		{
			return EngineApplicationInterface.ITexture.GetFromResource(resourceName);
		}

		// Token: 0x06000CED RID: 3309 RVA: 0x0000E55B File Offset: 0x0000C75B
		public bool IsLoaded()
		{
			return EngineApplicationInterface.ITexture.IsLoaded(base.Pointer);
		}

		// Token: 0x06000CEE RID: 3310 RVA: 0x0000E56D File Offset: 0x0000C76D
		public void GetSDFBoundingBoxData(ref Vec3 min, ref Vec3 max)
		{
			EngineApplicationInterface.ITexture.GetSDFBoundingBoxData(base.Pointer, ref min, ref max);
		}

		// Token: 0x06000CEF RID: 3311 RVA: 0x0000E581 File Offset: 0x0000C781
		public static Texture CheckAndGetFromResource(string resourceName)
		{
			return EngineApplicationInterface.ITexture.CheckAndGetFromResource(resourceName);
		}

		// Token: 0x06000CF0 RID: 3312 RVA: 0x0000E590 File Offset: 0x0000C790
		public static void ScaleTextureWithRatio(ref int tableauSizeX, ref int tableauSizeY)
		{
			float num = (float)tableauSizeX;
			float num2 = (float)tableauSizeY;
			int num3 = (int)MathF.Log(num, 2f) + 2;
			float num4 = MathF.Pow(2f, (float)num3) / num;
			tableauSizeX = (int)(num * num4);
			tableauSizeY = (int)(num2 * num4);
		}

		// Token: 0x06000CF1 RID: 3313 RVA: 0x0000E5CF File Offset: 0x0000C7CF
		public void PreloadTexture(bool blocking)
		{
			EngineApplicationInterface.ITexture.GetCurObject(base.Pointer, blocking);
		}

		// Token: 0x06000CF2 RID: 3314 RVA: 0x0000E5E2 File Offset: 0x0000C7E2
		public void Release()
		{
			this.IsReleased = true;
			this.RenderTargetComponent.OnTargetReleased();
			base.ManualInvalidate();
		}

		// Token: 0x06000CF3 RID: 3315 RVA: 0x0000E5FC File Offset: 0x0000C7FC
		public void ReleaseImmediately()
		{
			this.IsReleased = true;
			this.RenderTargetComponent.OnTargetReleased();
			EngineApplicationInterface.ITexture.Release(base.Pointer);
		}

		// Token: 0x06000CF4 RID: 3316 RVA: 0x0000E620 File Offset: 0x0000C820
		public void ReleaseAfterNumberOfFrames(int frameCount)
		{
			this.RenderTargetComponent.OnTargetReleased();
			EngineApplicationInterface.ITexture.ReleaseAfterNumberOfFrames(base.Pointer, frameCount);
		}

		// Token: 0x06000CF5 RID: 3317 RVA: 0x0000E63E File Offset: 0x0000C83E
		public static Texture LoadTextureFromPath(string fileName, string folder)
		{
			return EngineApplicationInterface.ITexture.LoadTextureFromPath(fileName, folder);
		}

		// Token: 0x06000CF6 RID: 3318 RVA: 0x0000E64C File Offset: 0x0000C84C
		public static Texture CreateDepthTarget(string name, int width, int height)
		{
			return EngineApplicationInterface.ITexture.CreateDepthTarget(name, width, height);
		}

		// Token: 0x06000CF7 RID: 3319 RVA: 0x0000E65B File Offset: 0x0000C85B
		public static Texture CreateFromByteArray(byte[] data, int width, int height)
		{
			return EngineApplicationInterface.ITexture.CreateFromByteArray(data, width, height);
		}

		// Token: 0x06000CF8 RID: 3320 RVA: 0x0000E66A File Offset: 0x0000C86A
		public void SaveToFile(string path, bool isRelativePath)
		{
			EngineApplicationInterface.ITexture.SaveToFile(base.Pointer, path, isRelativePath);
		}

		// Token: 0x06000CF9 RID: 3321 RVA: 0x0000E67E File Offset: 0x0000C87E
		public void SetTextureAsAlwaysValid()
		{
			EngineApplicationInterface.ITexture.SaveTextureAsAlwaysValid(base.Pointer);
		}

		// Token: 0x06000CFA RID: 3322 RVA: 0x0000E690 File Offset: 0x0000C890
		public static Texture CreateFromMemory(byte[] data)
		{
			return EngineApplicationInterface.ITexture.CreateFromMemory(data);
		}

		// Token: 0x06000CFB RID: 3323 RVA: 0x0000E69D File Offset: 0x0000C89D
		public static void ReleaseGpuMemories()
		{
			EngineApplicationInterface.ITexture.ReleaseGpuMemories();
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x06000CFC RID: 3324 RVA: 0x0000E6A9 File Offset: 0x0000C8A9
		public RenderTargetComponent RenderTargetComponent
		{
			get
			{
				return EngineApplicationInterface.ITexture.GetRenderTargetComponent(base.Pointer);
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x06000CFD RID: 3325 RVA: 0x0000E6BB File Offset: 0x0000C8BB
		public TableauView TableauView
		{
			get
			{
				return EngineApplicationInterface.ITexture.GetTableauView(base.Pointer);
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x06000CFE RID: 3326 RVA: 0x0000E6CD File Offset: 0x0000C8CD
		public object UserData
		{
			get
			{
				return this.RenderTargetComponent.UserData;
			}
		}

		// Token: 0x06000CFF RID: 3327 RVA: 0x0000E6DA File Offset: 0x0000C8DA
		private void SetTableauView(TableauView tableauView)
		{
			EngineApplicationInterface.ITexture.SetTableauView(base.Pointer, tableauView.Pointer);
		}

		// Token: 0x06000D00 RID: 3328 RVA: 0x0000E6F4 File Offset: 0x0000C8F4
		public static Texture CreateTableauTexture(string name, RenderTargetComponent.TextureUpdateEventHandler eventHandler, object objectRef, int tableauSizeX, int tableauSizeY)
		{
			Texture texture = Texture.CreateRenderTarget(name, tableauSizeX, tableauSizeY, true, true, false, false);
			RenderTargetComponent renderTargetComponent = texture.RenderTargetComponent;
			renderTargetComponent.PaintNeeded += eventHandler;
			renderTargetComponent.UserData = objectRef;
			TableauView tableauView = TableauView.CreateTableauView(name);
			tableauView.SetRenderTarget(texture);
			tableauView.SetAutoDepthTargetCreation(true);
			tableauView.SetSceneUsesSkybox(false);
			tableauView.SetClearColor(4294902015U);
			texture.SetTableauView(tableauView);
			return texture;
		}

		// Token: 0x06000D01 RID: 3329 RVA: 0x0000E751 File Offset: 0x0000C951
		public static Texture CreateRenderTarget(string name, int width, int height, bool autoMipmaps, bool isTableau, bool createUninitialized = false, bool always_valid = false)
		{
			return EngineApplicationInterface.ITexture.CreateRenderTarget(name, width, height, autoMipmaps, isTableau, createUninitialized, always_valid);
		}
	}
}

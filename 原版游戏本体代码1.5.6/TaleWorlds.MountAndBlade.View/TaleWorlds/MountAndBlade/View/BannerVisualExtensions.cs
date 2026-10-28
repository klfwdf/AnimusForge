using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View;

public static class BannerVisualExtensions
{
	public static Texture GetTableauTextureSmallForBannerEditor(this Banner banner, in BannerDebugInfo debugInfo, Action<Texture> setAction, out BannerEditorTextureCreationData textureCreationData)
	{
		textureCreationData = new BannerEditorTextureCreationData(banner, setAction, null, debugInfo, isTableauOrNineGrid: true, isLarge: false);
		return ThumbnailCacheManager.Current.CreateTexture(textureCreationData).Texture;
	}

	public static Texture GetTableauTextureLargeForBannerEditor(this Banner banner, in BannerDebugInfo debugInfo, Action<Texture> setAction, out BannerEditorTextureCreationData textureCreationData)
	{
		textureCreationData = new BannerEditorTextureCreationData(banner, setAction, null, debugInfo, isTableauOrNineGrid: true, isLarge: true);
		return ThumbnailCacheManager.Current.CreateTexture(textureCreationData).Texture;
	}

	public static Texture GetTableauTextureSmall(this Banner banner, in BannerDebugInfo debugInfo, Action<Texture> setAction)
	{
		return ((BannerVisual)banner.BannerVisual).GetTableauTextureSmall(in debugInfo, setAction);
	}

	public static Texture GetTableauTextureLarge(this Banner banner, in BannerDebugInfo debugInfo, Action<Texture> setAction)
	{
		return ((BannerVisual)banner.BannerVisual).GetTableauTextureLarge(in debugInfo, setAction);
	}

	public static Texture GetTableauTextureLarge(this Banner banner, in BannerDebugInfo debugInfo, Action<Texture> setAction, out BannerTextureCreationData creationData)
	{
		return ((BannerVisual)banner.BannerVisual).GetTableauTextureLarge(in debugInfo, setAction, out creationData);
	}

	public static MetaMesh ConvertToMultiMesh(this Banner banner)
	{
		return ((BannerVisual)banner.BannerVisual).ConvertToMultiMesh();
	}
}

using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class BannerTextureCreationData : BannerThumbnailCreationBaseData
{
	public BannerTextureCreationData(Banner banner, Action<Texture> setAction, Action cancelAction, BannerDebugInfo debugInfo, bool isTableauOrNineGrid, bool isLarge)
		: base(banner, setAction, cancelAction, debugInfo, isTableauOrNineGrid, isLarge)
	{
		base.RenderId = "Mesh_" + base.RenderId;
	}
}

using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public abstract class BannerThumbnailCreationBaseData : ThumbnailCreationData
{
	public Banner Banner { get; private set; }

	public BannerDebugInfo DebugInfo { get; private set; }

	public bool IsTableauOrNineGrid { get; private set; }

	public bool IsLarge { get; private set; }

	public BannerThumbnailCreationBaseData(Banner banner, Action<Texture> setAction, Action cancelAction, BannerDebugInfo debugInfo, bool isTableauOrNineGrid, bool isLarge)
		: base("", setAction, cancelAction)
	{
		Banner = banner;
		DebugInfo = debugInfo;
		IsTableauOrNineGrid = isTableauOrNineGrid;
		IsLarge = isLarge;
		base.RenderId = CreateRenderId();
	}

	private string CreateRenderId()
	{
		string text = "BannerThumbnail";
		if (IsTableauOrNineGrid)
		{
			text = ((!IsLarge) ? "BannerTableauSmall" : "BannerTableauLarge");
		}
		return text + ":" + Banner.BannerCode;
	}
}

using System;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public abstract class ThumbnailCreationData
{
	public readonly Action<Texture> SetAction;

	public readonly Action CancelAction;

	public bool IsProcessed { get; internal set; }

	public string RenderId { get; protected set; }

	public ThumbnailCreationData(string renderId, Action<Texture> setAction, Action cancelAction)
	{
		RenderId = renderId;
		SetAction = setAction;
		CancelAction = cancelAction;
	}
}

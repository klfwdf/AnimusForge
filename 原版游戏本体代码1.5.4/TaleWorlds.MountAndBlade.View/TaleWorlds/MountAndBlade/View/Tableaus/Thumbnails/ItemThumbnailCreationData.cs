using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class ItemThumbnailCreationData : ThumbnailCreationData
{
	public ItemObject ItemObject { get; private set; }

	public string AdditionalArgs { get; private set; }

	public ItemThumbnailCreationData(ItemObject itemObject, string additionalArgs, Action<Texture> setAction, Action cancelAction)
		: base(itemObject.StringId, setAction, cancelAction)
	{
		ItemObject = itemObject;
		AdditionalArgs = additionalArgs;
	}
}

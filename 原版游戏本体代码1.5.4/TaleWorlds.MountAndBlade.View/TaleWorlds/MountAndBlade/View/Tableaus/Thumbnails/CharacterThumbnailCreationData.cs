using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class CharacterThumbnailCreationData : ThumbnailCreationData
{
	public CharacterCode CharacterCode { get; private set; }

	public bool IsBig { get; private set; }

	public int CustomSizeX { get; private set; }

	public int CustomSizeY { get; private set; }

	public CharacterThumbnailCreationData(CharacterCode characterCode, Action<Texture> setAction, Action cancelAction, bool isBig, int customSizeX = -1, int customSizeY = -1)
		: base("", setAction, cancelAction)
	{
		characterCode.BodyProperties = new BodyProperties(new DynamicBodyProperties((int)characterCode.BodyProperties.Age, (int)characterCode.BodyProperties.Weight, (int)characterCode.BodyProperties.Build), characterCode.BodyProperties.StaticProperties);
		base.RenderId = characterCode.CreateNewCodeString();
		base.RenderId += (isBig ? "1" : "0");
		if (customSizeX > 0)
		{
			base.RenderId += $"_x:{customSizeX}";
		}
		if (customSizeY > 0)
		{
			base.RenderId += $"_y:{customSizeY}";
		}
		CharacterCode = characterCode;
		IsBig = isBig;
		CustomSizeX = customSizeX;
		CustomSizeY = customSizeY;
	}
}

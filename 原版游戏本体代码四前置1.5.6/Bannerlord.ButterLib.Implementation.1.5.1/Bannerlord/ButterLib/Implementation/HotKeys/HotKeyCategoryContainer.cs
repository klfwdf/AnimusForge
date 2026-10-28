using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.HotKeys;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.Implementation.HotKeys;

internal sealed class HotKeyCategoryContainer : GameKeyContext
{
	private static readonly int ListCapacity = Enum.GetValues(typeof(GameKeyDefinition)).Cast<int>().Max() + 200;

	public HotKeyCategoryContainer(string categoryId, string categoryName, IEnumerable<HotKeyBase> keys)
		: base(categoryId, ListCapacity)
	{
		GameText gameText = Module.CurrentModule.GlobalTextManager.AddGameText("str_key_category_name");
		gameText.AddVariationWithId(categoryId, new TextObject(categoryName), new List<GameTextManager.ChoiceTag>());
		GameText gameText2 = Module.CurrentModule.GlobalTextManager.AddGameText("str_key_name");
		GameText gameText3 = Module.CurrentModule.GlobalTextManager.AddGameText("str_key_description");
		foreach (HotKeyBase key in keys)
		{
			string variationId = $"{categoryId}_{(GameKeyDefinition)key.Id}";
			gameText2.AddVariationWithId(variationId, new TextObject(key.DisplayName), new List<GameTextManager.ChoiceTag>());
			gameText3.AddVariationWithId(variationId, new TextObject(key.Description), new List<GameTextManager.ChoiceTag>());
			RegisterGameKey(new GameKey(key.Id, key.Uid, categoryId, key.DefaultKey, key.Category));
		}
	}
}

using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.Localization;

public class SaveableLocalizationTypeDefiner : SaveableTypeDefiner
{
	public SaveableLocalizationTypeDefiner()
		: base(20000)
	{
	}

	protected override void DefineClassTypes()
	{
		AddClassDefinition(typeof(TextObject), 1);
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(Dictionary<string, TextObject>));
	}
}

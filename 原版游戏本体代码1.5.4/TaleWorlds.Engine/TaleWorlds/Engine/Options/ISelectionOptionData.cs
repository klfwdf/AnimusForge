using System.Collections.Generic;

namespace TaleWorlds.Engine.Options;

public interface ISelectionOptionData : IOptionData
{
	int GetSelectableOptionsLimit();

	IEnumerable<SelectionData> GetSelectableOptionNames();
}

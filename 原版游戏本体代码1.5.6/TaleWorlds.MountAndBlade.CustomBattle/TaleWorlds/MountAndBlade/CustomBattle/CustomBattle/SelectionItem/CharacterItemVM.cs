using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class CharacterItemVM : SelectorItemVM
{
	public BasicCharacterObject Character { get; private set; }

	public CharacterItemVM(BasicCharacterObject character)
		: base(character.Name.ToString())
	{
		Character = character;
	}
}

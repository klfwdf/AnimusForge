using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public class CharacterImageIdentifierVM : ImageIdentifierVM
{
	public CharacterImageIdentifierVM(CharacterCode characterCode)
	{
		base.ImageIdentifier = new CharacterImageIdentifier(characterCode);
	}
}

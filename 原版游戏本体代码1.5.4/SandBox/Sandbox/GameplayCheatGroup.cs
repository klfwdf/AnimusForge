using System.Collections.Generic;

namespace SandBox;

public abstract class GameplayCheatGroup : GameplayCheatBase
{
	public abstract IEnumerable<GameplayCheatBase> GetCheats();
}

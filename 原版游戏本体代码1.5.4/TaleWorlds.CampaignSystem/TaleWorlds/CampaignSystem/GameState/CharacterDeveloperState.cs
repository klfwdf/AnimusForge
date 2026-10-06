using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class CharacterDeveloperState : TaleWorlds.Core.GameState
{
	private ICharacterDeveloperStateHandler _handler;

	public override bool IsMenuState => true;

	public Hero InitialSelectedHero { get; private set; }

	public ICharacterDeveloperStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public CharacterDeveloperState()
	{
	}

	public CharacterDeveloperState(Hero initialSelectedHero)
	{
		InitialSelectedHero = initialSelectedHero;
	}
}

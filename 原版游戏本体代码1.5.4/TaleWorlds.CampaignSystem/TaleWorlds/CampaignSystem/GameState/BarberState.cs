using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class BarberState : TaleWorlds.Core.GameState
{
	public BasicCharacterObject Character;

	public override bool IsMenuState => true;

	public IFaceGeneratorCustomFilter Filter { get; private set; }

	public BarberState()
	{
	}

	public BarberState(BasicCharacterObject character, IFaceGeneratorCustomFilter filter)
	{
		Character = character;
		Filter = filter;
	}
}

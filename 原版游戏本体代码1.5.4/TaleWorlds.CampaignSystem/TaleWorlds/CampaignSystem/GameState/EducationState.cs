using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameState;

public class EducationState : TaleWorlds.Core.GameState
{
	private IEducationStateHandler _handler;

	public override bool IsMenuState => true;

	public Hero Child { get; private set; }

	public IEducationStateHandler Handler
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

	public EducationState()
	{
		Debug.FailedAssert("Do not use EducationState with default constructor!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameState\\EducationState.cs", ".ctor", 22);
	}

	public EducationState(Hero child)
	{
		Child = child;
	}
}

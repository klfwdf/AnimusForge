using System;

namespace SandBox.GauntletUI.Tutorial;

public class TutorialAttribute : Attribute
{
	public readonly string TutorialIdentifier;

	public TutorialAttribute(string tutorialIdentifier)
	{
		TutorialIdentifier = tutorialIdentifier;
	}
}

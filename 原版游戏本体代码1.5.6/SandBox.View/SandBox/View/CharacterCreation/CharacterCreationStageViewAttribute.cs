using System;

namespace SandBox.View.CharacterCreation;

public sealed class CharacterCreationStageViewAttribute : Attribute
{
	public readonly Type StageType;

	public CharacterCreationStageViewAttribute(Type stageType)
	{
		StageType = stageType;
	}
}

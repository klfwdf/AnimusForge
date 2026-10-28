namespace TaleWorlds.Core;

public readonly struct UndoRedoKey(int gender, int race, BodyProperties bodyProperties)
{
	public readonly int Gender = gender;

	public readonly int Race = race;

	public readonly BodyProperties BodyProperties = bodyProperties;
}

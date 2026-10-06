namespace TaleWorlds.Core;

public readonly struct UndoRedoKey
{
	public readonly int Gender;

	public readonly int Race;

	public readonly BodyProperties BodyProperties;

	public UndoRedoKey(int gender, int race, BodyProperties bodyProperties)
	{
		Gender = gender;
		Race = race;
		BodyProperties = bodyProperties;
	}
}

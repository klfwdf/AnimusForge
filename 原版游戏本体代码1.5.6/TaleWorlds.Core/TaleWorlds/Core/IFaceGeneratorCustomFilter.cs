namespace TaleWorlds.Core;

public interface IFaceGeneratorCustomFilter
{
	int[] GetHaircutIndices(BasicCharacterObject character);

	int[] GetFacialHairIndices(BasicCharacterObject character);

	FaceGeneratorStage[] GetAvailableStages();
}

using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BodyGenerator
{
	public const string FaceGenTeethAnimationName = "facegen_teeth";

	public BodyProperties CurrentBodyProperties;

	public BodyProperties BodyPropertiesMin;

	public BodyProperties BodyPropertiesMax;

	public int Race;

	public bool IsFemale;

	public BasicCharacterObject Character { get; private set; }

	public BodyGenerator(BasicCharacterObject troop)
	{
		Character = troop;
		MBDebug.Print("FaceGen set character> character face key: " + troop.GetBodyProperties(troop.Equipment));
		Race = Character.Race;
		IsFemale = Character.IsFemale;
	}

	public FaceGenerationParams InitBodyGenerator(bool isDressed)
	{
		CurrentBodyProperties = Character.GetBodyProperties(Character.Equipment);
		FaceGenerationParams faceGenerationParams = FaceGenerationParams.Create();
		faceGenerationParams.CurrentRace = Character.Race;
		faceGenerationParams.CurrentGender = (Character.IsFemale ? 1 : 0);
		faceGenerationParams.CurrentAge = Character.Age;
		MBBodyProperties.GetParamsFromKey(ref faceGenerationParams, CurrentBodyProperties, isDressed && Character.Equipment.EarsAreHidden, isDressed && Character.Equipment.MouthIsHidden);
		faceGenerationParams.SetRaceGenderAndAdjustParams(faceGenerationParams.CurrentRace, faceGenerationParams.CurrentGender, (int)faceGenerationParams.CurrentAge);
		return faceGenerationParams;
	}

	public void RefreshFace(FaceGenerationParams faceGenerationParams, bool hasEquipment)
	{
		MBBodyProperties.ProduceNumericKeyWithParams(faceGenerationParams, hasEquipment && Character.Equipment.EarsAreHidden, hasEquipment && Character.Equipment.MouthIsHidden, ref CurrentBodyProperties);
		Race = faceGenerationParams.CurrentRace;
		IsFemale = faceGenerationParams.CurrentGender == 1;
	}

	public void SaveCurrentCharacter()
	{
		Character.UpdatePlayerCharacterBodyProperties(CurrentBodyProperties, Race, IsFemale);
	}
}

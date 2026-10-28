using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IAgentVisual
{
	void SetAction(in ActionIndexCache actionName, float startProgress = 0f, bool forceFaceMorphRestart = true);

	MBAgentVisuals GetVisuals();

	MatrixFrame GetFrame();

	BodyProperties GetBodyProperties();

	void SetBodyProperties(BodyProperties bodyProperties);

	bool GetIsFemale();

	string GetCharacterObjectID();

	void SetCharacterObjectID(string id);

	Equipment GetEquipment();

	void SetClothingColors(uint color1, uint color2);

	void GetClothingColors(out uint color1, out uint color2);

	AgentVisualsData GetCopyAgentVisualsData();

	void Refresh(bool needBatchedVersionForWeaponMeshes, AgentVisualsData data, bool forceUseFaceCache = false);
}

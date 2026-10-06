using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class AgentVisualHolder : IAgentVisual
{
	private MatrixFrame _frame;

	private Equipment _equipment;

	private string _characterObjectStringID;

	private BodyProperties _bodyProperties;

	public AgentVisualHolder(MatrixFrame frame, Equipment equipment, string name, BodyProperties bodyProperties)
	{
		SetFrame(ref frame);
		_equipment = equipment;
		_characterObjectStringID = name;
		_bodyProperties = bodyProperties;
	}

	public void SetAction(in ActionIndexCache actionName, float startProgress = 0f, bool forceFaceMorphRestart = true)
	{
	}

	public GameEntity GetEntity()
	{
		return null;
	}

	public MBAgentVisuals GetVisuals()
	{
		return null;
	}

	public void SetFrame(ref MatrixFrame frame)
	{
		_frame = frame;
	}

	public MatrixFrame GetFrame()
	{
		return _frame;
	}

	public BodyProperties GetBodyProperties()
	{
		return _bodyProperties;
	}

	public void SetBodyProperties(BodyProperties bodyProperties)
	{
		_bodyProperties = bodyProperties;
	}

	public bool GetIsFemale()
	{
		return false;
	}

	public string GetCharacterObjectID()
	{
		return _characterObjectStringID;
	}

	public void SetCharacterObjectID(string id)
	{
		_characterObjectStringID = id;
	}

	public Equipment GetEquipment()
	{
		return _equipment;
	}

	public void RefreshWithNewEquipment(Equipment equipment)
	{
		_equipment = equipment;
	}

	public void SetClothingColors(uint color1, uint color2)
	{
	}

	public void GetClothingColors(out uint color1, out uint color2)
	{
		color1 = uint.MaxValue;
		color2 = uint.MaxValue;
	}

	public AgentVisualsData GetCopyAgentVisualsData()
	{
		return null;
	}

	public void Refresh(bool needBatchedVersionForWeaponMeshes, AgentVisualsData data, bool forceUseFaceCache = false)
	{
	}

	void IAgentVisual.SetAction(in ActionIndexCache actionName, float startProgress, bool forceFaceMorphRestart)
	{
		SetAction(in actionName, startProgress, forceFaceMorphRestart);
	}
}

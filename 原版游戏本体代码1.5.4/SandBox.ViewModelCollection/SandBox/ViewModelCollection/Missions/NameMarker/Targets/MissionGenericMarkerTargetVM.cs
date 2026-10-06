using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionGenericMarkerTargetVM : MissionNameMarkerTargetBaseVM
{
	public readonly string Identifier;

	private readonly Vec3 _position;

	private readonly TextObject _name;

	public MissionGenericMarkerTargetVM(string identifier, string nameType, string iconType, Vec3 position, TextObject name)
	{
		Identifier = identifier;
		base.NameType = nameType;
		base.IconType = iconType;
		_position = position;
		_name = name;
		RefreshValues();
	}

	public override bool Equals(MissionNameMarkerTargetBaseVM other)
	{
		if (other is MissionGenericMarkerTargetVM missionGenericMarkerTargetVM)
		{
			return missionGenericMarkerTargetVM.Identifier == Identifier;
		}
		return false;
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, _position + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		return _name;
	}
}

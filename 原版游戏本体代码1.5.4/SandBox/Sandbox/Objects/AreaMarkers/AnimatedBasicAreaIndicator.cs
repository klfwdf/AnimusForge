using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers;

public class AnimatedBasicAreaIndicator : AreaMarker
{
	public string NameStringId = "";

	public string Type;

	[EditorVisibleScriptComponentVariable(false)]
	private TextObject _name;

	private TextObject _overriddenName;

	public bool IsActive { get; private set; } = true;

	protected override void OnInit()
	{
		_name = (string.IsNullOrEmpty(NameStringId) ? TextObject.GetEmpty() : GameTexts.FindText(NameStringId));
	}

	public void SetIsActive(bool isActive)
	{
		IsActive = isActive;
		Campaign.Current.VisualTrackerManager.SetDirty();
	}

	public void SetOverriddenName(TextObject name)
	{
		_overriddenName = name;
	}

	public override TextObject GetName()
	{
		if (!TextObject.IsNullOrEmpty(_overriddenName))
		{
			return _overriddenName;
		}
		return _name;
	}
}

using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Quest;

public class QuestMarkerBrushWidget : BrushWidget
{
	private int _questMarkerType;

	public int QuestMarkerType
	{
		get
		{
			return _questMarkerType;
		}
		set
		{
			if (value != _questMarkerType)
			{
				_questMarkerType = value;
				UpdateMarkerState(_questMarkerType);
			}
		}
	}

	public QuestMarkerBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateMarkerState(int type)
	{
		string text = "";
		text = type switch
		{
			0 => "None", 
			1 => "AvailableIssue", 
			2 => "ActiveIssue", 
			4 => "ActiveStoryQuest", 
			8 => "TrackedIssue", 
			16 => "TrackedStoryQuest", 
			_ => "None", 
		};
		if (text != null)
		{
			SetState(text);
			Sprite sprite = base.Brush.GetLayer(text).Sprite;
			if (sprite != null)
			{
				float num = base.SuggestedHeight / (float)sprite.Height;
				base.SuggestedWidth = (float)sprite.Width * num;
			}
		}
	}
}

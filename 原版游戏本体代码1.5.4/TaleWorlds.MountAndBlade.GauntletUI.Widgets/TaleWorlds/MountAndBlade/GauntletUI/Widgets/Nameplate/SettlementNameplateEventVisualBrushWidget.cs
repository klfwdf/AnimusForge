using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

public class SettlementNameplateEventVisualBrushWidget : BrushWidget
{
	private bool _determinedVisual;

	private int _type = -1;

	private string _additionalParameters;

	[Editor(false)]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
			}
		}
	}

	[Editor(false)]
	public string AdditionalParameters
	{
		get
		{
			return _additionalParameters;
		}
		set
		{
			if (_additionalParameters != value)
			{
				_additionalParameters = value;
				OnPropertyChanged(value, "AdditionalParameters");
			}
		}
	}

	public SettlementNameplateEventVisualBrushWidget(UIContext context)
		: base(context)
	{
		base.EventManager.AddLateUpdateAction(this, LateUpdateAction, 1);
	}

	private void LateUpdateAction(float dt)
	{
		if (!_determinedVisual)
		{
			this.RegisterBrushStatesOfWidget();
			UpdateVisual(Type);
			_determinedVisual = true;
		}
	}

	private void UpdateVisual(int type)
	{
		switch (type)
		{
		case 0:
			SetState("Tournament");
			break;
		case 1:
			SetState("AvailableIssue");
			break;
		case 2:
			SetState("ActiveQuest");
			break;
		case 3:
			SetState("ActiveStoryQuest");
			break;
		case 4:
			SetState("TrackedIssue");
			break;
		case 5:
			SetState("TrackedStoryQuest");
			break;
		case 6:
			SetState(AdditionalParameters);
			base.MarginLeft = 2f;
			base.MarginRight = 2f;
			break;
		}
		Sprite sprite = base.Brush?.GetStyle(base.CurrentState)?.GetLayer(0)?.Sprite;
		if (sprite != null)
		{
			base.SuggestedWidth = base.SuggestedHeight / (float)sprite.Height * (float)sprite.Width;
		}
	}
}

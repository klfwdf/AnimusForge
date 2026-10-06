using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate.Notifications;

public class NameplateNotificationListPanel : ListPanel
{
	private static readonly Color NegativeRelationColor = Color.ConvertStringToColor("#D6543BFF");

	private static readonly Color NeutralRelationColor = Color.ConvertStringToColor("#ECB05BFF");

	private static readonly Color PositiveRelationColor = Color.ConvertStringToColor("#98CA3AFF");

	private float _totalDt;

	private bool _isFirstFrame = true;

	private Widget _relationVisualWidget;

	private float _stayAmount = 2f;

	private float _fadeTime = 1f;

	private int _relationType = -2;

	public Widget RelationVisualWidget
	{
		get
		{
			return _relationVisualWidget;
		}
		set
		{
			if (_relationVisualWidget != value)
			{
				_relationVisualWidget = value;
				OnPropertyChanged(value, "RelationVisualWidget");
			}
		}
	}

	public int RelationType
	{
		get
		{
			return _relationType;
		}
		set
		{
			if (_relationType != value)
			{
				_relationType = value;
				OnPropertyChanged(value, "RelationType");
			}
		}
	}

	public float StayAmount
	{
		get
		{
			return _stayAmount;
		}
		set
		{
			if (_stayAmount != value)
			{
				_stayAmount = value;
				OnPropertyChanged(value, "StayAmount");
			}
		}
	}

	public float FadeTime
	{
		get
		{
			return _fadeTime;
		}
		set
		{
			if (_fadeTime != value)
			{
				_fadeTime = value;
				OnPropertyChanged(value, "FadeTime");
			}
		}
	}

	public NameplateNotificationListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_isFirstFrame)
		{
			switch (RelationType)
			{
			case -1:
				RelationVisualWidget.Color = NegativeRelationColor;
				break;
			case 0:
				RelationVisualWidget.Color = NeutralRelationColor;
				break;
			case 1:
				RelationVisualWidget.Color = PositiveRelationColor;
				break;
			}
			_isFirstFrame = false;
		}
		_totalDt += dt;
		if (base.AlphaFactor <= 0f || _totalDt > _stayAmount + _fadeTime)
		{
			EventFired("OnRemove");
		}
		else if (_totalDt > _stayAmount)
		{
			float alphaFactor = 1f - (_totalDt - _stayAmount) / _fadeTime;
			this.SetGlobalAlphaRecursively(alphaFactor);
		}
	}
}

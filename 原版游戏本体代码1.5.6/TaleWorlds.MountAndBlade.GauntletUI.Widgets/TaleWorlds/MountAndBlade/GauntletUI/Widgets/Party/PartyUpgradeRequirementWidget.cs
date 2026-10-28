using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyUpgradeRequirementWidget : Widget
{
	private bool _requiresRefresh = true;

	private string _requirementId;

	private bool _isSufficient;

	private bool _isPerkRequirement;

	private Color _normalColor;

	private Color _insufficientColor;

	[Editor(false)]
	public string RequirementId
	{
		get
		{
			return _requirementId;
		}
		set
		{
			if (value != _requirementId)
			{
				_requirementId = value;
				OnPropertyChanged(value, "RequirementId");
				_requiresRefresh = true;
			}
		}
	}

	[Editor(false)]
	public bool IsSufficient
	{
		get
		{
			return _isSufficient;
		}
		set
		{
			if (value != _isSufficient)
			{
				_isSufficient = value;
				OnPropertyChanged(value, "IsSufficient");
				_requiresRefresh = true;
			}
		}
	}

	[Editor(false)]
	public bool IsPerkRequirement
	{
		get
		{
			return _isPerkRequirement;
		}
		set
		{
			if (value != _isPerkRequirement)
			{
				_isPerkRequirement = value;
				OnPropertyChanged(value, "IsPerkRequirement");
				_requiresRefresh = true;
			}
		}
	}

	public Color NormalColor
	{
		get
		{
			return _normalColor;
		}
		set
		{
			if (value != _normalColor)
			{
				_normalColor = value;
				_requiresRefresh = true;
			}
		}
	}

	public Color InsufficientColor
	{
		get
		{
			return _insufficientColor;
		}
		set
		{
			if (value != _insufficientColor)
			{
				_insufficientColor = value;
				_requiresRefresh = true;
			}
		}
	}

	public PartyUpgradeRequirementWidget(UIContext context)
		: base(context)
	{
		NormalColor = new Color(1f, 1f, 1f);
		InsufficientColor = new Color(0.753f, 0.071f, 0.098f);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_requiresRefresh)
		{
			if (RequirementId != null)
			{
				string text = (IsPerkRequirement ? "SPGeneral\\Skills\\gui_skills_icon_" : "StdAssets\\ItemIcons\\");
				string text2 = (IsPerkRequirement ? "_tiny" : "");
				base.Sprite = base.Context.SpriteData.GetSprite(text + RequirementId + text2);
			}
			base.Color = (IsSufficient ? NormalColor : InsufficientColor);
			_requiresRefresh = false;
		}
	}
}

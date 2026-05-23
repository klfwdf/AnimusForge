using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class SkillIconVisualWidget : Widget
{
	private bool _requiresRefresh = true;

	private string _skillId;

	private bool _useSmallVariation;

	private bool _useSmallestVariation;

	[Editor(false)]
	public string SkillId
	{
		get
		{
			return _skillId;
		}
		set
		{
			if (_skillId != value)
			{
				_skillId = value;
				OnPropertyChanged(value, "SkillId");
				_requiresRefresh = true;
			}
		}
	}

	[Editor(false)]
	public bool UseSmallVariation
	{
		get
		{
			return _useSmallVariation;
		}
		set
		{
			if (_useSmallVariation != value)
			{
				_useSmallVariation = value;
				OnPropertyChanged(value, "UseSmallVariation");
				_requiresRefresh = true;
			}
		}
	}

	[Editor(false)]
	public bool UseSmallestVariation
	{
		get
		{
			return _useSmallestVariation;
		}
		set
		{
			if (_useSmallestVariation != value)
			{
				_useSmallestVariation = value;
				OnPropertyChanged(value, "UseSmallestVariation");
				_requiresRefresh = true;
			}
		}
	}

	public SkillIconVisualWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_requiresRefresh)
		{
			return;
		}
		if (SkillId == null)
		{
			Debug.FailedAssert("SkillIconVisualWidget.OnLateUpdate called before SkillId has been set, or SkillId is set to null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\SkillIconVisualWidget.cs", "OnLateUpdate", 21);
			_requiresRefresh = false;
			return;
		}
		string text = "SPGeneral\\Skills\\gui_skills_icon_" + SkillId.ToLower();
		if (UseSmallestVariation && base.Context.SpriteData.GetSprite(text + "_tiny") != null)
		{
			base.Sprite = base.Context.SpriteData.GetSprite(text + "_tiny");
		}
		else if (UseSmallVariation && base.Context.SpriteData.GetSprite(text + "_small") != null)
		{
			base.Sprite = base.Context.SpriteData.GetSprite(text + "_small");
		}
		else if (base.Context.SpriteData.GetSprite(text) != null)
		{
			base.Sprite = base.Context.SpriteData.GetSprite(text);
		}
		_requiresRefresh = false;
	}
}

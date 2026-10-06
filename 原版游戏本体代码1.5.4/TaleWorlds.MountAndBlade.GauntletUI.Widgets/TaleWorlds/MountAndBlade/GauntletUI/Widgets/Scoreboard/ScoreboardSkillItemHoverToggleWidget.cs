using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardSkillItemHoverToggleWidget : HoverToggleWidget
{
	private bool _isHoverEndHandled;

	private bool _isHoverBeginHandled;

	public ScoreboardGainedSkillsListPanel SkillsShowWidget { get; set; }

	public ListPanel GainedSkillsList { get; set; }

	public ScoreboardSkillItemHoverToggleWidget(UIContext context)
		: base(context)
	{
	}

	public List<Widget> GetAllSkillWidgets()
	{
		return GainedSkillsList.Children;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsOverWidget && !_isHoverBeginHandled)
		{
			SkillsShowWidget.SetCurrentUnit(this);
			_isHoverBeginHandled = true;
			_isHoverEndHandled = true;
		}
		else if (!base.IsOverWidget && _isHoverEndHandled)
		{
			SkillsShowWidget.SetCurrentUnit(null);
			_isHoverEndHandled = false;
			_isHoverBeginHandled = false;
		}
	}
}

using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;

public class ConversationItemImageWidget : ImageWidget
{
	private bool _isInitialized;

	public Brush NormalBrush { get; set; }

	public Brush SpecialBrush { get; set; }

	public bool IsSpecial { get; set; }

	public ConversationItemImageWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isInitialized)
		{
			base.Brush = (IsSpecial ? SpecialBrush : NormalBrush);
			_isInitialized = true;
		}
	}
}

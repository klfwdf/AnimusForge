using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.BoardGame;

public class BoardGameInstructionVisualWidget : Widget
{
	private const float ScaleCoeff = 0.5f;

	private string _gameType;

	[Editor(false)]
	public string GameType
	{
		get
		{
			return _gameType;
		}
		set
		{
			if (_gameType != value)
			{
				_gameType = value;
			}
		}
	}

	public BoardGameInstructionVisualWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.Sprite == null)
		{
			int siblingIndex = base.ParentWidget.ParentWidget.GetSiblingIndex();
			if (!string.IsNullOrEmpty(GameType))
			{
				base.Sprite = base.Context.SpriteData.GetSprite(GameType + siblingIndex);
			}
		}
		if (base.Sprite != null)
		{
			base.SuggestedWidth = (float)base.Sprite.Width * 0.5f;
			base.SuggestedHeight = (float)base.Sprite.Height * 0.5f;
		}
	}
}

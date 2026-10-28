using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerPlayerBadgeVisualWidget : Widget
{
	private float _forcedWidth;

	private float _forcedHeight;

	private bool _hasForcedSize;

	private string _badgeId;

	public string BadgeId
	{
		get
		{
			return _badgeId;
		}
		set
		{
			if (value != _badgeId)
			{
				_badgeId = value;
				OnPropertyChanged(value, "BadgeId");
				UpdateVisual(value);
			}
		}
	}

	public MultiplayerPlayerBadgeVisualWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateVisual(string badgeId)
	{
		if (badgeId == "badge_official_server_admin")
		{
			badgeId = "badge_taleworlds_dev";
		}
		base.Sprite = base.Context.SpriteData.GetSprite("MPPlayerBadges\\" + badgeId);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_hasForcedSize)
		{
			base.SuggestedWidth = _forcedWidth;
			base.SuggestedHeight = _forcedHeight;
		}
	}

	public void SetForcedSize(float width, float height)
	{
		_forcedWidth = width;
		_forcedHeight = height;
		_hasForcedSize = true;
	}
}

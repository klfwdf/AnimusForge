using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.AdminMessage;

public class MultiplayerAdminMessageWidget : Widget
{
	private float _currentTextOnScreenTime;

	public TextWidget MessageTextWidget { get; set; }

	public float MessageOnScreenStayTime => 5f;

	public float MessageFadeInTime => 0.4f;

	public float MessageFadeOutTime => 0.2f;

	public MultiplayerAdminMessageWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.ChildCount > 0)
		{
			_currentTextOnScreenTime += dt;
			if (_currentTextOnScreenTime < MessageFadeInTime)
			{
				float alphaFactor = MathF.Lerp(0f, 1f, _currentTextOnScreenTime / MessageFadeInTime);
				base.Children[0].SetGlobalAlphaRecursively(alphaFactor);
				base.Children[0].IsVisible = true;
			}
			else if (_currentTextOnScreenTime > MessageFadeInTime && _currentTextOnScreenTime < MessageOnScreenStayTime + MessageFadeInTime)
			{
				base.Children[0].SetGlobalAlphaRecursively(1f);
			}
			else if (_currentTextOnScreenTime < MessageFadeInTime + MessageOnScreenStayTime + MessageFadeOutTime)
			{
				float alphaFactor2 = MathF.Lerp(1f, 0f, (_currentTextOnScreenTime - (MessageFadeInTime + MessageOnScreenStayTime)) / MessageFadeOutTime);
				base.Children[0].SetGlobalAlphaRecursively(alphaFactor2);
			}
			else
			{
				(base.Children[0] as MultiplayerAdminMessageItemWidget)?.Remove();
				_currentTextOnScreenTime = 0f;
			}
		}
		else
		{
			_currentTextOnScreenTime = 0f;
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
	}
}

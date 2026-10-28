using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyBattleRewardWidget : Widget
{
	private const string _rewardImpactSoundEventName = "inventory/perk";

	private float _buttonAnimationStartWidth;

	private float _buttonAnimationStartHeight;

	private float _buttonAnimationEndWidth;

	private float _buttonAnimationEndHeight;

	private float _iconAnimationStartWidget;

	private float _iconAnimationStartHeight;

	private float _iconAnimationEndWidth;

	private float _iconAnimationEndHeight;

	private ButtonWidget _rewardIconButton;

	private Widget _rewardIcon;

	private TextWidget _rewardTextDescription;

	private ValueBasedVisibilityWidget _rewardToShow;

	private bool _isAnimationStarted;

	private bool _isTextAnimationStarted;

	private bool _isInPreAnimationState;

	private float _animationStartTime;

	private float _textAnimationStartTime;

	public float AnimationDuration { get; set; } = 0.1f;

	public float TextRevealAnimationDuration { get; set; } = 0.05f;

	public float AnimationInitialScaleMultiplier { get; set; } = 2f;

	public MultiplayerLobbyBattleRewardWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isInPreAnimationState)
		{
			foreach (Widget child in base.Children)
			{
				if (child is ValueBasedVisibilityWidget)
				{
					child.IsVisible = false;
				}
			}
		}
		bool flag = false;
		if (_isAnimationStarted && base.EventManager.Time - _animationStartTime < AnimationDuration)
		{
			float amount = (base.EventManager.Time - _animationStartTime) / AnimationDuration;
			_rewardIconButton.SuggestedWidth = Mathf.Lerp(_buttonAnimationStartWidth, _buttonAnimationEndWidth, amount);
			_rewardIconButton.SuggestedHeight = Mathf.Lerp(_buttonAnimationStartHeight, _buttonAnimationEndHeight, amount);
			_rewardIcon.SuggestedWidth = Mathf.Lerp(_iconAnimationStartWidget, _iconAnimationEndWidth, amount);
			_rewardIcon.SuggestedHeight = Mathf.Lerp(_iconAnimationStartHeight, _iconAnimationEndHeight, amount);
			_rewardIconButton.SetGlobalAlphaRecursively(Mathf.Lerp(0f, 1f, amount));
			_rewardIcon.SetGlobalAlphaRecursively(Mathf.Lerp(0f, 1f, amount));
			_rewardToShow.IsVisible = true;
			flag = true;
		}
		if (!_isTextAnimationStarted && _isAnimationStarted && base.EventManager.Time - _animationStartTime >= AnimationDuration)
		{
			_textAnimationStartTime = base.EventManager.Time;
			_isTextAnimationStarted = true;
		}
		if (_isTextAnimationStarted && base.EventManager.Time - _textAnimationStartTime < TextRevealAnimationDuration)
		{
			float amount2 = (base.EventManager.Time - _textAnimationStartTime) / TextRevealAnimationDuration;
			_rewardTextDescription.SetGlobalAlphaRecursively(Mathf.Lerp(0f, 1f, amount2));
			flag = true;
		}
		if (_isAnimationStarted && _isTextAnimationStarted && !flag)
		{
			EndAnimation();
		}
	}

	public void StartAnimation()
	{
		_isInPreAnimationState = false;
		foreach (Widget child in base.Children)
		{
			if (child is ValueBasedVisibilityWidget valueBasedVisibilityWidget && valueBasedVisibilityWidget.IndexToWatch == valueBasedVisibilityWidget.IndexToBeVisible)
			{
				_rewardToShow = valueBasedVisibilityWidget;
				_rewardIconButton = child.Children[0].Children[0] as ButtonWidget;
				_rewardIcon = _rewardIconButton.Children[0];
				_rewardTextDescription = child.Children[0].Children[1] as TextWidget;
				_buttonAnimationStartWidth = _rewardIconButton.SuggestedWidth * AnimationInitialScaleMultiplier;
				_buttonAnimationStartHeight = _rewardIconButton.SuggestedHeight * AnimationInitialScaleMultiplier;
				_buttonAnimationEndWidth = _rewardIconButton.SuggestedWidth;
				_buttonAnimationEndHeight = _rewardIconButton.SuggestedHeight;
				_iconAnimationStartWidget = _rewardIcon.SuggestedWidth * AnimationInitialScaleMultiplier;
				_iconAnimationStartHeight = _rewardIcon.SuggestedHeight * AnimationInitialScaleMultiplier;
				_iconAnimationEndWidth = _rewardIcon.SuggestedWidth;
				_iconAnimationEndHeight = _rewardIcon.SuggestedHeight;
				_rewardTextDescription.SetGlobalAlphaRecursively(0f);
			}
		}
		_isAnimationStarted = true;
		_animationStartTime = base.EventManager.Time;
		base.Context.TwoDimensionContext.PlaySound("inventory/perk");
	}

	public void StartPreAnimation()
	{
		_isInPreAnimationState = true;
		base.IsVisible = true;
	}

	public void EndAnimation()
	{
		_rewardIconButton.SetGlobalAlphaRecursively(1f);
		_rewardIcon.SetGlobalAlphaRecursively(1f);
		_rewardTextDescription.SetGlobalAlphaRecursively(1f);
		_rewardIconButton.SuggestedWidth = _buttonAnimationEndWidth;
		_rewardIconButton.SuggestedHeight = _buttonAnimationEndHeight;
		_rewardIcon.SuggestedWidth = _iconAnimationEndWidth;
		_rewardIcon.SuggestedHeight = _iconAnimationEndHeight;
		_isAnimationStarted = false;
		_isTextAnimationStarted = false;
	}
}

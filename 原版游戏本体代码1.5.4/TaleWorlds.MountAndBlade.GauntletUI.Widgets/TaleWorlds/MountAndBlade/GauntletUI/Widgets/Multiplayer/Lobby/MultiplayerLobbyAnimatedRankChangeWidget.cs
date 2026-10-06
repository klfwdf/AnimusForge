using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyAnimatedRankChangeWidget : Widget
{
	private float _animationTimeElapsed;

	private float _animationDuration = 0.25f;

	private float _preAnimationTimeElapsed;

	private float _animationDelay = 0.5f;

	private bool _isAnimationRequested;

	private bool _isPromoted;

	private TextWidget _oldRankName;

	private TextWidget _newRankName;

	private MultiplayerLobbyRankItemButtonWidget _oldRankSprite;

	private MultiplayerLobbyRankItemButtonWidget _newRankSprite;

	[Editor(false)]
	public bool IsAnimationRequested
	{
		get
		{
			return _isAnimationRequested;
		}
		set
		{
			if (value != _isAnimationRequested)
			{
				_isAnimationRequested = value;
				OnPropertyChanged(value, "IsAnimationRequested");
				StartAnimation();
			}
		}
	}

	[Editor(false)]
	public bool IsPromoted
	{
		get
		{
			return _isPromoted;
		}
		set
		{
			if (value != _isPromoted)
			{
				_isPromoted = value;
				OnPropertyChanged(value, "IsPromoted");
			}
		}
	}

	[Editor(false)]
	public TextWidget OldRankName
	{
		get
		{
			return _oldRankName;
		}
		set
		{
			if (value != _oldRankName)
			{
				_oldRankName = value;
				OnPropertyChanged(value, "OldRankName");
			}
		}
	}

	[Editor(false)]
	public TextWidget NewRankName
	{
		get
		{
			return _newRankName;
		}
		set
		{
			if (value != _newRankName)
			{
				_newRankName = value;
				OnPropertyChanged(value, "NewRankName");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyRankItemButtonWidget OldRankSprite
	{
		get
		{
			return _oldRankSprite;
		}
		set
		{
			if (value != _oldRankSprite)
			{
				_oldRankSprite = value;
				OnPropertyChanged(value, "OldRankSprite");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyRankItemButtonWidget NewRankSprite
	{
		get
		{
			return _newRankSprite;
		}
		set
		{
			if (value != _newRankSprite)
			{
				_newRankSprite = value;
				OnPropertyChanged(value, "NewRankSprite");
			}
		}
	}

	public MultiplayerLobbyAnimatedRankChangeWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsAnimationRequested)
		{
			if (_preAnimationTimeElapsed < _animationDelay)
			{
				_preAnimationTimeElapsed += dt;
			}
			else if (_animationTimeElapsed >= _animationDuration)
			{
				NewRankName.SetGlobalAlphaRecursively(1f);
				NewRankSprite.SetGlobalAlphaRecursively(1f);
				OldRankName.SetGlobalAlphaRecursively(0f);
				OldRankSprite.SetGlobalAlphaRecursively(0f);
				NewRankSprite.ScaledSuggestedWidth = base.ScaledSuggestedWidth / 2f;
				NewRankSprite.ScaledSuggestedHeight = base.ScaledSuggestedHeight / 2f;
			}
			else
			{
				float num = MathF.Lerp(0f, 1f, _animationTimeElapsed / _animationDuration);
				OldRankSprite.SetGlobalAlphaRecursively(1f - num);
				OldRankName.SetGlobalAlphaRecursively(1f - num);
				NewRankSprite.SetGlobalAlphaRecursively(num);
				NewRankName.SetGlobalAlphaRecursively(num);
				NewRankSprite.ScaledSuggestedWidth = MathF.Lerp(base.ScaledSuggestedWidth, base.ScaledSuggestedWidth / 2f, _animationTimeElapsed / _animationDuration);
				NewRankSprite.ScaledSuggestedHeight = MathF.Lerp(base.ScaledSuggestedHeight, base.ScaledSuggestedHeight / 2f, _animationTimeElapsed / _animationDuration);
				_animationTimeElapsed += dt;
			}
		}
	}

	private void StartAnimation()
	{
		NewRankName.SetGlobalAlphaRecursively(0f);
		NewRankSprite.SetGlobalAlphaRecursively(0f);
		OldRankName.SetGlobalAlphaRecursively(1f);
		OldRankSprite.SetGlobalAlphaRecursively(1f);
		OldRankSprite.ScaledSuggestedWidth = base.ScaledSuggestedWidth / 2f;
		OldRankSprite.ScaledSuggestedHeight = base.ScaledSuggestedHeight / 2f;
		_preAnimationTimeElapsed = 0f;
		_animationTimeElapsed = 0f;
	}
}

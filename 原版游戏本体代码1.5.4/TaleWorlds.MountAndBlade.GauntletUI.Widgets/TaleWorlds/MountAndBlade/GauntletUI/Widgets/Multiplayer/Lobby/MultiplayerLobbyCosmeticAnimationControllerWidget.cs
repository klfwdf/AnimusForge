using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyCosmeticAnimationControllerWidget : Widget
{
	private static readonly Color DefaultColor = Color.FromUint(0u);

	private int _cosmeticRarity;

	private float _minAlphaChangeDuration = 1.5f;

	private float _maxAlphaChangeDuration = 2.5f;

	private float _minAlphaLowerBound = 0.4f;

	private float _minAlphaUpperBound = 0.6f;

	private float _maxAlphaLowerBound = 0.6f;

	private float _maxAlphaUpperBound = 0.8f;

	private Color _rarityCommonColor;

	private Color _rarityRareColor;

	private Color _rarityUniqueColor;

	private BasicContainer _animationPartContainer;

	[Editor(false)]
	public int CosmeticRarity
	{
		get
		{
			return _cosmeticRarity;
		}
		set
		{
			if (value != _cosmeticRarity)
			{
				_cosmeticRarity = value;
				OnPropertyChanged(value, "CosmeticRarity");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public float MinAlphaChangeDuration
	{
		get
		{
			return _minAlphaChangeDuration;
		}
		set
		{
			if (_minAlphaChangeDuration != value)
			{
				_minAlphaChangeDuration = value;
				OnPropertyChanged(value, "MinAlphaChangeDuration");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public float MaxAlphaChangeDuration
	{
		get
		{
			return _maxAlphaChangeDuration;
		}
		set
		{
			if (_maxAlphaChangeDuration != value)
			{
				_maxAlphaChangeDuration = value;
				OnPropertyChanged(value, "MaxAlphaChangeDuration");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public float MinAlphaLowerBound
	{
		get
		{
			return _minAlphaLowerBound;
		}
		set
		{
			if (_minAlphaLowerBound != value)
			{
				_minAlphaLowerBound = value;
				OnPropertyChanged(value, "MinAlphaLowerBound");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public float MinAlphaUpperBound
	{
		get
		{
			return _minAlphaUpperBound;
		}
		set
		{
			if (_minAlphaUpperBound != value)
			{
				_minAlphaUpperBound = value;
				OnPropertyChanged(value, "MinAlphaUpperBound");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public float MaxAlphaLowerBound
	{
		get
		{
			return _maxAlphaLowerBound;
		}
		set
		{
			if (_maxAlphaLowerBound != value)
			{
				_maxAlphaLowerBound = value;
				OnPropertyChanged(value, "MaxAlphaLowerBound");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public float MaxAlphaUpperBound
	{
		get
		{
			return _maxAlphaUpperBound;
		}
		set
		{
			if (_maxAlphaUpperBound != value)
			{
				_maxAlphaUpperBound = value;
				OnPropertyChanged(value, "MaxAlphaUpperBound");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public Color RarityCommonColor
	{
		get
		{
			return _rarityCommonColor;
		}
		set
		{
			if (_rarityCommonColor != value)
			{
				_rarityCommonColor = value;
				OnPropertyChanged(value, "RarityCommonColor");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public Color RarityRareColor
	{
		get
		{
			return _rarityRareColor;
		}
		set
		{
			if (_rarityRareColor != value)
			{
				_rarityRareColor = value;
				OnPropertyChanged(value, "RarityRareColor");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public Color RarityUniqueColor
	{
		get
		{
			return _rarityUniqueColor;
		}
		set
		{
			if (_rarityUniqueColor != value)
			{
				_rarityUniqueColor = value;
				OnPropertyChanged(value, "RarityUniqueColor");
				RestartAllAnimations();
			}
		}
	}

	[Editor(false)]
	public BasicContainer AnimationPartContainer
	{
		get
		{
			return _animationPartContainer;
		}
		set
		{
			if (value != _animationPartContainer)
			{
				if (_animationPartContainer != null)
				{
					_animationPartContainer.ItemAddEventHandlers.Remove(OnAnimationPartAdded);
				}
				_animationPartContainer = value;
				if (_animationPartContainer != null)
				{
					_animationPartContainer.ItemAddEventHandlers.Add(OnAnimationPartAdded);
				}
				OnPropertyChanged(value, "AnimationPartContainer");
				RestartAllAnimations();
			}
		}
	}

	private double GetRandomDoubleBetween(double min, double max)
	{
		return base.Context.UIRandom.NextDouble() * (max - min) + max;
	}

	public MultiplayerLobbyCosmeticAnimationControllerWidget(UIContext context)
		: base(context)
	{
	}

	private void RestartAllAnimations()
	{
		SetAllAnimationPartColors();
		StopAllAnimations();
		StartAllAnimations();
	}

	private void SetAllAnimationPartColors()
	{
		ApplyActionOnAllAnimations(SetColorOfPart);
	}

	private void StartAllAnimations()
	{
		ApplyActionOnAllAnimations(StartAnimationOfPart);
	}

	private void StopAllAnimations()
	{
		ApplyActionOnAllAnimations(StopAnimationOfPart);
	}

	private void StartAnimationOfPart(MultiplayerLobbyCosmeticAnimationPartWidget part)
	{
		double randomDoubleBetween = GetRandomDoubleBetween(MinAlphaChangeDuration, MaxAlphaChangeDuration);
		double randomDoubleBetween2 = GetRandomDoubleBetween(MinAlphaLowerBound, MinAlphaUpperBound);
		double randomDoubleBetween3 = GetRandomDoubleBetween(MaxAlphaLowerBound, MaxAlphaUpperBound);
		part.StartAnimation((float)randomDoubleBetween, (float)randomDoubleBetween2, (float)randomDoubleBetween3);
	}

	private void StopAnimationOfPart(MultiplayerLobbyCosmeticAnimationPartWidget part)
	{
		part.StopAnimation();
	}

	private void SetColorOfPart(MultiplayerLobbyCosmeticAnimationPartWidget part)
	{
		switch (CosmeticRarity)
		{
		case 0:
		case 1:
			part.Color = RarityCommonColor;
			break;
		case 2:
			part.Color = RarityRareColor;
			break;
		case 3:
			part.Color = RarityUniqueColor;
			break;
		default:
			part.Color = DefaultColor;
			break;
		}
	}

	private void ApplyActionOnAllAnimations(Action<MultiplayerLobbyCosmeticAnimationPartWidget> action)
	{
		AnimationPartContainer?.Children.ForEach(delegate(Widget c)
		{
			action(c as MultiplayerLobbyCosmeticAnimationPartWidget);
		});
	}

	private void OnAnimationPartAdded(Widget parent, Widget child)
	{
		MultiplayerLobbyCosmeticAnimationPartWidget multiplayerLobbyCosmeticAnimationPartWidget = child as MultiplayerLobbyCosmeticAnimationPartWidget;
		SetColorOfPart(multiplayerLobbyCosmeticAnimationPartWidget);
		StartAnimationOfPart(multiplayerLobbyCosmeticAnimationPartWidget);
	}
}

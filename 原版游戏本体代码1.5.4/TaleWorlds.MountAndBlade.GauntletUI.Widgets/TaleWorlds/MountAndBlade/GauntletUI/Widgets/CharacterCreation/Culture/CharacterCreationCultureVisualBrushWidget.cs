using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterCreation.Culture;

public class CharacterCreationCultureVisualBrushWidget : BrushWidget
{
	private float _alphaTarget;

	private bool _isFirstFrame = true;

	private string _currentCultureId;

	private bool _isBig;

	public bool UseSmallVisuals { get; set; } = true;

	public ParallaxItemBrushWidget Layer1Widget { get; set; }

	public ParallaxItemBrushWidget Layer2Widget { get; set; }

	public ParallaxItemBrushWidget Layer3Widget { get; set; }

	public ParallaxItemBrushWidget Layer4Widget { get; set; }

	[Editor(false)]
	public string CurrentCultureId
	{
		get
		{
			return _currentCultureId;
		}
		set
		{
			if (_currentCultureId != value)
			{
				_currentCultureId = value;
				OnPropertyChanged(value, "CurrentCultureId");
				SetCultureVisual(value);
				this.SetGlobalAlphaRecursively(1f);
			}
		}
	}

	[Editor(false)]
	public bool IsBig
	{
		get
		{
			return _isBig;
		}
		set
		{
			if (_isBig != value)
			{
				_isBig = value;
				OnPropertyChanged(value, "IsBig");
			}
		}
	}

	public CharacterCreationCultureVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isFirstFrame)
		{
			_alphaTarget = ((!string.IsNullOrEmpty(CurrentCultureId)) ? 1 : 0);
			this.SetGlobalAlphaRecursively(_alphaTarget);
			Layer1Widget?.RegisterBrushStatesOfWidget();
			Layer2Widget?.RegisterBrushStatesOfWidget();
			Layer3Widget?.RegisterBrushStatesOfWidget();
			Layer4Widget?.RegisterBrushStatesOfWidget();
			_isFirstFrame = false;
		}
		this.SetGlobalAlphaRecursively(Mathf.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, _alphaTarget, dt * 10f));
	}

	private void SetCultureVisual(string newCultureId)
	{
		if (string.IsNullOrEmpty(newCultureId))
		{
			_alphaTarget = 0f;
			return;
		}
		if (UseSmallVisuals)
		{
			Sprite sprite = base.Context.SpriteData.GetSprite("CharacterCreation\\Culture\\" + newCultureId);
			if (sprite == null)
			{
				sprite = base.Context.SpriteData.GetSprite("CharacterCreation\\Culture\\blank_culture");
			}
			foreach (Style style in base.Brush.Styles)
			{
				StyleLayer[] layers = style.GetLayers();
				for (int i = 0; i < layers.Length; i++)
				{
					layers[i].Sprite = sprite;
				}
			}
		}
		else
		{
			Layer1Widget?.SetState(newCultureId);
			Layer2Widget?.SetState(newCultureId);
			Layer3Widget?.SetState(newCultureId);
			Layer4Widget?.SetState(newCultureId);
		}
		_alphaTarget = 1f;
	}
}

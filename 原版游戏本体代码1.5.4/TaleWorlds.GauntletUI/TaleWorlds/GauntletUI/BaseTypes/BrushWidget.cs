using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class BrushWidget : Widget
{
	private Brush _originalBrush;

	private Brush _clonedBrush;

	private bool _animRestarted;

	[Editor(false)]
	public Brush Brush
	{
		get
		{
			if (_originalBrush == null)
			{
				_originalBrush = base.Context.DefaultBrush;
				_clonedBrush = _originalBrush.Clone();
				BrushRenderer.Brush = ReadOnlyBrush;
			}
			else if (_clonedBrush == null)
			{
				_clonedBrush = _originalBrush.Clone();
				BrushRenderer.Brush = ReadOnlyBrush;
			}
			return _clonedBrush;
		}
		set
		{
			if (_originalBrush != value)
			{
				_originalBrush = value;
				_clonedBrush = null;
				OnBrushChanged();
				OnPropertyChanged(value, "Brush");
			}
		}
	}

	public Brush ReadOnlyBrush
	{
		get
		{
			if (_clonedBrush != null)
			{
				return _clonedBrush;
			}
			if (_originalBrush == null)
			{
				_originalBrush = base.Context.DefaultBrush;
			}
			return _originalBrush;
		}
	}

	[Editor(false)]
	public new Sprite Sprite
	{
		get
		{
			return ReadOnlyBrush.DefaultStyle.GetLayer("Default").Sprite;
		}
		set
		{
			Brush.DefaultStyle.GetLayer("Default").Sprite = value;
		}
	}

	public BrushRenderer BrushRenderer { get; private set; }

	public BrushWidget(UIContext context)
		: base(context)
	{
		BrushRenderer = new BrushRenderer();
		base.EventFire += BrushWidget_EventFire;
	}

	private void BrushWidget_EventFire(Widget arg1, string eventName, object[] arg3)
	{
		if (ReadOnlyBrush != null)
		{
			AudioProperty eventAudioProperty = ReadOnlyBrush.SoundProperties.GetEventAudioProperty(eventName);
			if (eventAudioProperty != null && eventAudioProperty.AudioName != null && !eventAudioProperty.AudioName.Equals(""))
			{
				base.EventManager.Context.TwoDimensionContext.PlaySound(eventAudioProperty.AudioName);
			}
		}
	}

	public override void UpdateBrushes(float dt)
	{
		UpdateBrushRendererInternal(dt);
		if (!IsBrushUpdateNeeded())
		{
			UnRegisterUpdateBrushes();
		}
	}

	protected bool IsBrushUpdateNeeded()
	{
		_ = Brush;
		if (base.IsVisible && BrushRenderer.IsUpdateNeeded())
		{
			return AreaRect.IsCollide(in base.EventManager.AreaRectangle);
		}
		return false;
	}

	protected void UpdateBrushRendererInternal(float dt)
	{
		if (base.Context?.TwoDimensionContext?.Platform == null)
		{
			Debug.FailedAssert("Trying to update brush renderer after context or platform is finalized", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\BaseTypes\\BrushWidget.cs", "UpdateBrushRendererInternal", 129);
			return;
		}
		BrushRenderer.ForcePixelPerfectPlacement = base.ForcePixelPerfectRenderPlacement;
		BrushRenderer.UseLocalTimer = !base.UseGlobalTimeForAnimation;
		BrushRenderer.Brush = ReadOnlyBrush;
		BrushRenderer.CurrentState = base.CurrentState;
		BrushRenderer.Update(base.EventManager.LocalFrameNumber, base.Context.TwoDimensionContext.Platform.ApplicationTime, dt);
		if (!base.RestartAnimationFirstFrame || _animRestarted)
		{
			return;
		}
		base.EventManager.AddLateUpdateAction(this, delegate
		{
			if (base.RestartAnimationFirstFrame)
			{
				BrushRenderer.RestartAnimation();
			}
		}, 5);
		_animRestarted = true;
	}

	public override void SetState(string stateName)
	{
		if (base.CurrentState != stateName)
		{
			if (base.EventManager != null && ReadOnlyBrush != null)
			{
				AudioProperty stateAudioProperty = ReadOnlyBrush.SoundProperties.GetStateAudioProperty(stateName);
				if (stateAudioProperty != null)
				{
					if (stateAudioProperty.AudioName != null && !stateAudioProperty.AudioName.Equals(""))
					{
						base.EventManager.Context.TwoDimensionContext.PlaySound(stateAudioProperty.AudioName);
					}
					else
					{
						Debug.FailedAssert("Widget with id \"" + base.Id + "\" has a sound having no audioName for event \"" + stateName + "\"!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\BaseTypes\\BrushWidget.cs", "SetState", 169);
					}
				}
			}
			RegisterUpdateBrushes();
		}
		base.SetState(stateName);
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		RegisterUpdateBrushes();
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (IsBrushUpdateNeeded() && base.EventManager.LocalFrameNumber != BrushRenderer.LastUpdatedFrameNumber)
		{
			RegisterUpdateBrushes();
			UpdateBrushRendererInternal(base.EventManager.CachedDt);
		}
		BrushRenderer.Render(drawContext, in AreaRect, base._scaleToUse, base.Context.ContextAlpha);
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		BrushRenderer.SetSeed(_seed);
	}

	public override void UpdateAnimationPropertiesSubTask(float alphaFactor)
	{
		Brush.GlobalAlphaFactor = alphaFactor;
		foreach (Widget child in base.Children)
		{
			child.UpdateAnimationPropertiesSubTask(alphaFactor);
		}
	}

	public virtual void OnBrushChanged()
	{
		RegisterUpdateBrushes();
	}

	protected void RegisterUpdateBrushes()
	{
		base.EventManager.RegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, this);
	}

	protected void UnRegisterUpdateBrushes()
	{
		base.EventManager.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, this);
	}
}

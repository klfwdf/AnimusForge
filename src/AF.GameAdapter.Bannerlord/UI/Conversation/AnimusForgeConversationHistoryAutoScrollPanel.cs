using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

public class AnimusForgeConversationHistoryAutoScrollPanel : ScrollablePanel
{
	private const int InitialAutoScrollFrames = 24;
	private const int StableScrollbarFramesBeforeStop = 2;
	private const float ScrollbarValueEpsilon = 0.01f;
	// A 0.66-second easing keeps new-message navigation clearly visible without making the timeline feel delayed.
	private const float SmoothAutoScrollDurationSeconds = 0.66f;

	private int _remainingAutoScrollFrames = InitialAutoScrollFrames;
	private int _remainingTopAutoScrollFrames;
	private int _stableScrollbarFrames;
	private int _autoScrollRequestVersion;
	private int _autoScrollTopRequestVersion;
	private float _lastObservedScrollbarMaximum = float.NaN;
	private bool _smoothAutoScroll;
	private bool _isSmoothAutoScrollActive;
	private float _smoothAutoScrollStartValue;
	private float _smoothAutoScrollTargetValue;
	private float _smoothAutoScrollElapsedSeconds;

	public AnimusForgeConversationHistoryAutoScrollPanel(UIContext context)
		: base(context)
	{
	}

	/// <summary>
	/// Receives a monotonically increasing request from the world-message VM whenever
	/// a newly published, currently visible record is appended to the timeline.
	/// </summary>
	public int AutoScrollRequestVersion
	{
		get => _autoScrollRequestVersion;
		set
		{
			if (_autoScrollRequestVersion == value)
			{
				return;
			}
			_autoScrollRequestVersion = value;
			RequestBottomScrollAfterLayout();
		}
	}

	/// <summary>
	/// Receives a monotonically increasing request to reveal the top of a replacement page.
	/// This keeps previous/next page navigation continuous in chronological history lists.
	/// </summary>
	public int AutoScrollTopRequestVersion
	{
		get => _autoScrollTopRequestVersion;
		set
		{
			if (_autoScrollTopRequestVersion == value)
			{
				return;
			}
			_autoScrollTopRequestVersion = value;
			RequestTopScrollAfterLayout();
		}
	}

	/// <summary>
	/// Enables a short easing animation for automatic bottom-scroll requests.
	/// The world-message timeline opts in while older conversation history keeps its existing instant behavior.
	/// </summary>
	public bool SmoothAutoScroll
	{
		get => _smoothAutoScroll;
		set
		{
			_smoothAutoScroll = value;
			if (!value)
			{
				// Disabling the feature must never leave an unfinished transform animation running.
				_isSmoothAutoScrollActive = false;
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);

		if (VerticalScrollbar == null)
		{
			return;
		}
		if (_remainingTopAutoScrollFrames > 0)
		{
			UpdateAutomaticTopScrollTargetAfterLayout();
			return;
		}
		if (_remainingAutoScrollFrames > 0)
		{
			UpdateAutomaticScrollTargetAfterLayout();
		}
		AdvanceSmoothAutoScroll(dt);
	}

	private void UpdateAutomaticScrollTargetAfterLayout()
	{
		float maximum = VerticalScrollbar.MaxValue;
		if (maximum <= 0f)
		{
			// A fresh binding list can report zero until its children receive their first layout pass.
			_remainingAutoScrollFrames--;
			return;
		}

		bool maximumChanged = float.IsNaN(_lastObservedScrollbarMaximum)
			|| Math.Abs(maximum - _lastObservedScrollbarMaximum) > ScrollbarValueEpsilon;
		if (maximumChanged)
		{
			_lastObservedScrollbarMaximum = maximum;
			_stableScrollbarFrames = 0;
			MoveScrollbarToBottomIfNeeded(maximum);
		}
		else
		{
			_stableScrollbarFrames++;
		}

		_remainingAutoScrollFrames--;
		if (_stableScrollbarFrames >= StableScrollbarFramesBeforeStop || _remainingAutoScrollFrames <= 0)
		{
			// Stop retargeting after layout settles; the easing routine below completes the final movement without text-position jitter.
			MoveScrollbarToBottomIfNeeded(maximum);
			_remainingAutoScrollFrames = 0;
		}
	}

	private void RequestBottomScrollAfterLayout()
	{
		// A bounded retry covers deferred Gauntlet measurement without continually forcing the content transform.
		_remainingTopAutoScrollFrames = 0;
		_remainingAutoScrollFrames = InitialAutoScrollFrames;
		_stableScrollbarFrames = 0;
		_lastObservedScrollbarMaximum = float.NaN;
		_isSmoothAutoScrollActive = false;
	}

	private void RequestTopScrollAfterLayout()
	{
		// The replacement page may bind before its rows receive height; keep the top anchored through that short layout window.
		_remainingTopAutoScrollFrames = InitialAutoScrollFrames;
		_remainingAutoScrollFrames = 0;
		_isSmoothAutoScrollActive = false;
	}

	private void UpdateAutomaticTopScrollTargetAfterLayout()
	{
		// Zero is valid before and after measurement, so no maximum-value probe is needed for a deterministic top anchor.
		VerticalScrollbar.ValueFloat = 0f;
		_remainingTopAutoScrollFrames--;
	}

	private void MoveScrollbarToBottomIfNeeded(float maximum)
	{
		if (_smoothAutoScroll
			&& _isSmoothAutoScrollActive
			&& Math.Abs(_smoothAutoScrollTargetValue - maximum) <= ScrollbarValueEpsilon)
		{
			// A stable layout must not restart the same easing animation from its intermediate position.
			return;
		}
		float currentValue = VerticalScrollbar.ValueFloat;
		if (Math.Abs(currentValue - maximum) <= ScrollbarValueEpsilon)
		{
			_isSmoothAutoScrollActive = false;
			return;
		}
		if (!_smoothAutoScroll)
		{
			VerticalScrollbar.ValueFloat = maximum;
			return;
		}
		// Retarget from the current visual position so newly measured content extends the same smooth downward motion.
		_smoothAutoScrollStartValue = currentValue;
		_smoothAutoScrollTargetValue = maximum;
		_smoothAutoScrollElapsedSeconds = 0f;
		_isSmoothAutoScrollActive = true;
	}

	private void AdvanceSmoothAutoScroll(float dt)
	{
		if (!_isSmoothAutoScrollActive || !_smoothAutoScroll)
		{
			return;
		}
		float maximum = VerticalScrollbar.MaxValue;
		float target = Math.Max(0f, Math.Min(_smoothAutoScrollTargetValue, maximum));
		if (Math.Abs(VerticalScrollbar.ValueFloat - target) <= ScrollbarValueEpsilon)
		{
			VerticalScrollbar.ValueFloat = target;
			_isSmoothAutoScrollActive = false;
			return;
		}
		_smoothAutoScrollElapsedSeconds += Math.Max(0f, dt);
		float progress = Math.Min(1f, _smoothAutoScrollElapsedSeconds / SmoothAutoScrollDurationSeconds);
		// Cubic ease-out starts responsively and settles gently instead of visibly snapping the text canvas.
		float inverseProgress = 1f - progress;
		float easedProgress = 1f - inverseProgress * inverseProgress * inverseProgress;
		VerticalScrollbar.ValueFloat = _smoothAutoScrollStartValue
			+ (target - _smoothAutoScrollStartValue) * easedProgress;
		if (progress >= 1f || Math.Abs(VerticalScrollbar.ValueFloat - target) <= ScrollbarValueEpsilon)
		{
			VerticalScrollbar.ValueFloat = target;
			_isSmoothAutoScrollActive = false;
		}
	}

	protected override void OnMouseScroll()
	{
		CancelAutomaticScrolling();
		base.OnMouseScroll();
	}

	protected override void OnRightStickMovement()
	{
		CancelAutomaticScrolling();
		base.OnRightStickMovement();
	}

	private void CancelAutomaticScrolling()
	{
		// Any explicit player scroll wins over pending automatic positioning and its easing animation.
		_remainingTopAutoScrollFrames = 0;
		_remainingAutoScrollFrames = 0;
		_isSmoothAutoScrollActive = false;
	}
}

using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

public class ObjectiveMarkerWidget : Widget
{
	private const float BoundaryOffset = 50f;

	private int _distance;

	private int _combinedSiblingsCount;

	private TextWidget _nameTextWidget;

	private TextWidget _combinationCountWidget;

	private Widget _mainContainer;

	private Widget _questIconWidget;

	private Widget _distanceContainerWidget;

	private Widget _distanceIconWidget;

	private Widget _distanceTextWidget;

	private Vec2 _position;

	private Vec2 _combinedAveragePosition;

	private bool _isMainCombinationMarker;

	private bool _isDistanceRelevant;

	private bool _isMarkerEnabled;

	private bool _isMarkerActive;

	private bool _isFocused;

	public bool IsCombinedWithOtherMarkers => CombinedSiblingsCount > 0;

	public float FarAlphaTarget { get; set; } = 0.2f;

	public float FarDistanceCutoff { get; set; } = 50f;

	public float CloseDistanceCutoff { get; set; } = 25f;

	public MarkerRect Rect { get; private set; }

	public bool IsInScreenBoundaries { get; private set; }

	[DataSourceProperty]
	public TextWidget NameTextWidget
	{
		get
		{
			return _nameTextWidget;
		}
		set
		{
			if (_nameTextWidget != value)
			{
				_nameTextWidget = value;
				OnPropertyChanged(value, "NameTextWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public TextWidget CombinationCountWidget
	{
		get
		{
			return _combinationCountWidget;
		}
		set
		{
			if (_combinationCountWidget != value)
			{
				_combinationCountWidget = value;
				OnPropertyChanged(value, "CombinationCountWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public Widget QuestIconWidget
	{
		get
		{
			return _questIconWidget;
		}
		set
		{
			if (_questIconWidget != value)
			{
				_questIconWidget = value;
				OnPropertyChanged(value, "QuestIconWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public Widget MainContainer
	{
		get
		{
			return _mainContainer;
		}
		set
		{
			if (_mainContainer != value)
			{
				_mainContainer = value;
				OnPropertyChanged(value, "MainContainer");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public Widget DistanceContainerWidget
	{
		get
		{
			return _distanceContainerWidget;
		}
		set
		{
			if (_distanceContainerWidget != value)
			{
				_distanceContainerWidget = value;
				OnPropertyChanged(value, "DistanceContainerWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public Widget DistanceIconWidget
	{
		get
		{
			return _distanceIconWidget;
		}
		set
		{
			if (_distanceIconWidget != value)
			{
				_distanceIconWidget = value;
				OnPropertyChanged(value, "DistanceIconWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public Widget DistanceTextWidget
	{
		get
		{
			return _distanceTextWidget;
		}
		set
		{
			if (_distanceTextWidget != value)
			{
				_distanceTextWidget = value;
				OnPropertyChanged(value, "DistanceTextWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 CombinedAveragePosition
	{
		get
		{
			return _combinedAveragePosition;
		}
		set
		{
			if (_combinedAveragePosition != value)
			{
				_combinedAveragePosition = value;
				OnPropertyChanged(value, "CombinedAveragePosition");
			}
		}
	}

	[DataSourceProperty]
	public int Distance
	{
		get
		{
			return _distance;
		}
		set
		{
			if (_distance != value)
			{
				_distance = value;
				OnPropertyChanged(value, "Distance");
			}
		}
	}

	[DataSourceProperty]
	public int CombinedSiblingsCount
	{
		get
		{
			return _combinedSiblingsCount;
		}
		set
		{
			if (_combinedSiblingsCount != value)
			{
				_combinedSiblingsCount = value;
				OnPropertyChanged(value, "CombinedSiblingsCount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainCombinationMarker
	{
		get
		{
			return _isMainCombinationMarker;
		}
		set
		{
			if (_isMainCombinationMarker != value)
			{
				_isMainCombinationMarker = value;
				OnPropertyChanged(value, "IsMainCombinationMarker");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDistanceRelevant
	{
		get
		{
			return _isDistanceRelevant;
		}
		set
		{
			if (_isDistanceRelevant != value)
			{
				_isDistanceRelevant = value;
				OnPropertyChanged(value, "IsDistanceRelevant");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMarkerEnabled
	{
		get
		{
			return _isMarkerEnabled;
		}
		set
		{
			if (_isMarkerEnabled != value)
			{
				_isMarkerEnabled = value;
				OnPropertyChanged(value, "IsMarkerEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMarkerActive
	{
		get
		{
			return _isMarkerActive;
		}
		set
		{
			if (_isMarkerActive != value)
			{
				_isMarkerActive = value;
				OnPropertyChanged(value, "IsMarkerActive");
			}
		}
	}

	[Editor(false)]
	public new bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChanged(value, "IsFocused");
				base.RenderLate = value;
			}
		}
	}

	public ObjectiveMarkerWidget(UIContext context)
		: base(context)
	{
		Rect = new MarkerRect();
	}

	public void Update(float dt)
	{
		float transitionRatio = TaleWorlds.Library.MathF.Clamp(dt * 12f, 0f, 1f);
		float num = ((IsMarkerEnabled && IsMarkerActive) ? GetDistanceRelatedAlphaTarget(Distance) : 0f);
		float distanceAlpha = ((IsFocused && (!IsCombinedWithOtherMarkers || IsMainCombinationMarker)) ? num : 0f);
		float targetAlpha = ((IsFocused && !IsCombinedWithOtherMarkers) ? num : 0f);
		DistanceContainerWidget.ApplyActionForThisAndAllChildren(delegate(Widget w)
		{
			UpdateAlpha(w, distanceAlpha, transitionRatio);
		});
		UpdateAlpha(NameTextWidget, targetAlpha, transitionRatio);
		UpdateAlpha(QuestIconWidget, num, transitionRatio);
		UpdateAlpha(CombinationCountWidget, num, transitionRatio);
		base.ScaledPositionYOffset = Position.Y - base.Size.Y / 2f;
		base.ScaledPositionXOffset = Position.X - base.Size.X / 2f;
		CombinationCountWidget.Text = (IsMainCombinationMarker ? (CombinedSiblingsCount + 1).ToString() : string.Empty);
		Vec2 vec = (IsCombinedWithOtherMarkers ? (CombinedAveragePosition - Position) : Vec2.Zero);
		MainContainer.ScaledPositionXOffset = TaleWorlds.Library.MathF.Lerp(MainContainer.ScaledPositionXOffset, vec.x, transitionRatio);
		MainContainer.ScaledPositionYOffset = TaleWorlds.Library.MathF.Lerp(MainContainer.ScaledPositionYOffset, vec.y, transitionRatio);
		UpdateRectangle();
	}

	private static void UpdateAlpha(Widget item, float targetAlpha, float transitionRatio)
	{
		if (item != null)
		{
			float num = LocalLerp(item.AlphaFactor, targetAlpha, transitionRatio);
			item.SetAlpha(num);
			item.IsVisible = num > 1E-05f;
		}
	}

	public void UpdateRectangle()
	{
		Rect.Reset();
		Rect.UpdatePoints(base.ScaledPositionXOffset, base.ScaledPositionXOffset + base.Size.X, base.ScaledPositionYOffset, base.ScaledPositionYOffset + base.Size.Y);
		IsInScreenBoundaries = Rect.Left > -50f && Rect.Right < base.EventManager.PageSize.X + 50f && Rect.Top > -50f && Rect.Bottom < base.EventManager.PageSize.Y + 50f;
	}

	private float GetDistanceRelatedAlphaTarget(int distance)
	{
		if (IsCombinedWithOtherMarkers)
		{
			if (IsMainCombinationMarker)
			{
				return 1f;
			}
			if (!((CombinedAveragePosition - Position).Distance(new Vec2(MainContainer.ScaledPositionXOffset, MainContainer.ScaledPositionYOffset)) > 5f))
			{
				return 0f;
			}
			return 1f;
		}
		if (IsFocused)
		{
			return 1f;
		}
		if ((float)distance > FarDistanceCutoff)
		{
			return FarAlphaTarget;
		}
		if ((float)distance <= FarDistanceCutoff && (float)distance >= CloseDistanceCutoff)
		{
			float amount = (float)Math.Pow(((float)distance - CloseDistanceCutoff) / (FarDistanceCutoff - CloseDistanceCutoff), 1.0 / 3.0);
			return TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Lerp(1f, FarAlphaTarget, amount), FarAlphaTarget, 1f);
		}
		return 1f;
	}

	private static float LocalLerp(float start, float end, float delta)
	{
		if (Math.Abs(start - end) > float.Epsilon)
		{
			return (end - start) * delta + start;
		}
		return end;
	}

	private void OnStateChanged()
	{
	}
}

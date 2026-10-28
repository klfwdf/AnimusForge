using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

public class NameMarkerListPanel : ListPanel
{
	private Widget _parentScreenWidget;

	private const float BoundaryOffset = 50f;

	private float _transitionDT;

	private float _targetAlpha;

	private string _iconType = string.Empty;

	private string _nameType = string.Empty;

	private int _distance;

	private TextWidget _nameTextWidget;

	private BrushWidget _typeVisualWidget;

	private BrushWidget _distanceIconWidget;

	private TextWidget _distanceTextWidget;

	private Vec2 _position;

	private Color _issueNotificationColor;

	private Color _mainQuestNotificationColor;

	private Color _enemyColor;

	private Color _friendlyColor;

	private bool _isMarkerEnabled;

	private bool _isMarkerPersistent;

	private bool _hasIssue;

	private bool _hasMainQuest;

	private bool _isEnemy;

	private bool _isFriendly;

	private bool _isFocused;

	public float FarAlphaTarget { get; set; } = 0.2f;

	public float FarDistanceCutoff { get; set; } = 50f;

	public float CloseDistanceCutoff { get; set; } = 25f;

	public bool HasTypeMarker { get; private set; }

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
	public BrushWidget TypeVisualWidget
	{
		get
		{
			return _typeVisualWidget;
		}
		set
		{
			if (_typeVisualWidget != value)
			{
				_typeVisualWidget = value;
				OnPropertyChanged(value, "TypeVisualWidget");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public BrushWidget DistanceIconWidget
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
	public TextWidget DistanceTextWidget
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

	[Editor(false)]
	public Color IssueNotificationColor
	{
		get
		{
			return _issueNotificationColor;
		}
		set
		{
			if (value != _issueNotificationColor)
			{
				_issueNotificationColor = value;
				OnPropertyChanged(value, "IssueNotificationColor");
				OnStateChanged();
			}
		}
	}

	[Editor(false)]
	public Color MainQuestNotificationColor
	{
		get
		{
			return _mainQuestNotificationColor;
		}
		set
		{
			if (value != _mainQuestNotificationColor)
			{
				_mainQuestNotificationColor = value;
				OnPropertyChanged(value, "MainQuestNotificationColor");
				OnStateChanged();
			}
		}
	}

	[Editor(false)]
	public Color EnemyColor
	{
		get
		{
			return _enemyColor;
		}
		set
		{
			if (value != _enemyColor)
			{
				_enemyColor = value;
				OnPropertyChanged(value, "EnemyColor");
				OnStateChanged();
			}
		}
	}

	[Editor(false)]
	public Color FriendlyColor
	{
		get
		{
			return _friendlyColor;
		}
		set
		{
			if (value != _friendlyColor)
			{
				_friendlyColor = value;
				OnPropertyChanged(value, "FriendlyColor");
				OnStateChanged();
			}
		}
	}

	[Editor(false)]
	public string IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (value != _iconType)
			{
				_iconType = value;
				OnPropertyChanged(value, "IconType");
				OnStateChanged();
			}
		}
	}

	[Editor(false)]
	public string NameType
	{
		get
		{
			return _nameType;
		}
		set
		{
			if (value != _nameType)
			{
				_nameType = value;
				OnPropertyChanged(value, "NameType");
				OnStateChanged();
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
	public bool IsMarkerPersistent
	{
		get
		{
			return _isMarkerPersistent;
		}
		set
		{
			if (_isMarkerPersistent != value)
			{
				_isMarkerPersistent = value;
				OnPropertyChanged(value, "IsMarkerPersistent");
			}
		}
	}

	[DataSourceProperty]
	public bool HasIssue
	{
		get
		{
			return _hasIssue;
		}
		set
		{
			if (_hasIssue != value)
			{
				_hasIssue = value;
				OnPropertyChanged(value, "HasIssue");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public bool HasMainQuest
	{
		get
		{
			return _hasMainQuest;
		}
		set
		{
			if (_hasMainQuest != value)
			{
				_hasMainQuest = value;
				OnPropertyChanged(value, "HasMainQuest");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnemy
	{
		get
		{
			return _isEnemy;
		}
		set
		{
			if (_isEnemy != value)
			{
				_isEnemy = value;
				OnPropertyChanged(value, "IsEnemy");
				OnStateChanged();
			}
		}
	}

	[DataSourceProperty]
	public bool IsFriendly
	{
		get
		{
			return _isFriendly;
		}
		set
		{
			if (_isFriendly != value)
			{
				_isFriendly = value;
				OnPropertyChanged(value, "IsFriendly");
				OnStateChanged();
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
				if (!value && (IsMarkerEnabled || IsMarkerPersistent))
				{
					NameTextWidget?.SetAlpha(0f);
					DistanceTextWidget?.SetAlpha(0f);
					DistanceIconWidget?.SetAlpha(0f);
				}
				else if (value && (IsMarkerEnabled || IsMarkerPersistent))
				{
					NameTextWidget?.SetAlpha(1f);
					DistanceTextWidget?.SetAlpha(1f);
					DistanceIconWidget?.SetAlpha(1f);
				}
				base.RenderLate = value;
			}
		}
	}

	public NameMarkerListPanel(UIContext context)
		: base(context)
	{
		_parentScreenWidget = base.EventManager.Root.GetChild(0).GetChild(0);
		Rect = new MarkerRect();
	}

	public void Update(float dt)
	{
		_transitionDT = TaleWorlds.Library.MathF.Clamp(dt * 12f, 0f, 1f);
		_targetAlpha = ((IsMarkerEnabled || IsMarkerPersistent) ? GetDistanceRelatedAlphaTarget(Distance) : 0f);
		this.ApplyActionForThisAndAllChildren(UpdateAlpha);
		TextWidget nameTextWidget = NameTextWidget;
		if ((nameTextWidget != null && nameTextWidget.IsVisible) || TypeVisualWidget.IsVisible)
		{
			base.ScaledPositionYOffset = Position.y - base.Size.Y / 2f;
			base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
		}
		UpdateRectangle();
	}

	private void UpdateAlpha(Widget item)
	{
		if ((item != NameTextWidget && item != DistanceTextWidget && item != DistanceIconWidget) || !HasTypeMarker || IsFocused)
		{
			float alphaFactor = LocalLerp(item.AlphaFactor, _targetAlpha, _transitionDT);
			item.SetAlpha(alphaFactor);
			item.IsVisible = (double)item.AlphaFactor > 0.05;
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

	private float LocalLerp(float start, float end, float delta)
	{
		if (Math.Abs(start - end) > float.Epsilon)
		{
			return (end - start) * delta + start;
		}
		return end;
	}

	private void OnStateChanged()
	{
		if (NameTextWidget != null)
		{
			NameTextWidget.SetState(NameType);
		}
		if (TypeVisualWidget != null)
		{
			TypeVisualWidget.SetState(IconType);
		}
		HasTypeMarker = IconType != string.Empty;
		if (HasTypeMarker && IsFocused)
		{
			NameTextWidget?.SetAlpha(1f);
			DistanceTextWidget?.SetAlpha(1f);
			DistanceIconWidget?.SetAlpha(1f);
		}
		else if (HasTypeMarker && !IsFocused)
		{
			NameTextWidget?.SetAlpha(0f);
			DistanceTextWidget?.SetAlpha(0f);
			DistanceIconWidget?.SetAlpha(0f);
		}
		if (IsEnemy)
		{
			TypeVisualWidget.Brush.GlobalColor = EnemyColor;
		}
		else if (IsFriendly)
		{
			TypeVisualWidget.Brush.GlobalColor = FriendlyColor;
		}
		else if (HasMainQuest)
		{
			TypeVisualWidget.Brush.GlobalColor = MainQuestNotificationColor;
		}
		else if (HasIssue)
		{
			TypeVisualWidget.Brush.GlobalColor = IssueNotificationColor;
		}
		Sprite sprite = TypeVisualWidget?.Brush.GetStyle(IconType)?.GetLayer(0)?.Sprite;
		if (sprite != null)
		{
			base.SuggestedWidth = base.SuggestedHeight / (float)sprite.Height * (float)sprite.Width;
		}
	}
}

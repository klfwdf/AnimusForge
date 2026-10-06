using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class FormationMarkerListPanel : ListPanel
{
	public const int VisibilityStateNeutral = -1;

	public const int VisibilityStateHidden = 0;

	public const int VisibilityStateDistanceScaled = 1;

	public const int VisibilityStateAlwaysVisible = 2;

	private bool _isMarkersDirty = true;

	private Vec2 _markerDefaultSize = Vec2.Invalid;

	private const float MinimumVisibleAlpha = 0.05f;

	private const float UnevaluatedAlpha = -1f;

	private bool _isMarkerEnabled;

	private bool _isTargetingAFormation;

	public bool _showDistanceTexts;

	private bool _isActive = true;

	private int _teamType;

	private int _wSign;

	private float _distance;

	private float _farAlphaTarget = 0.2f;

	private float _farDistanceCutoff = 50f;

	private float _closeDistanceCutoff = 25f;

	private float _closestFadeoutRange = 3f;

	private float _visibilityRatioTarget = 1f;

	private float _alwaysOnDistance = 25f;

	private int _visibilityState = -1;

	private string _markerType;

	private Vec2 _position;

	private Brush _iconBrush;

	private Widget _formationTypeMarker;

	private Widget _teamTypeMarker;

	private TextWidget _nameTextWidget;

	private float _smoothedAlpha = -1f;

	public float FarScaleTarget { get; set; } = 0.5f;

	public float CloseScaleTarget { get; set; } = 1.4f;

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
	public bool IsTargetingAFormation
	{
		get
		{
			return _isTargetingAFormation;
		}
		set
		{
			if (_isTargetingAFormation != value)
			{
				_isTargetingAFormation = value;
				OnPropertyChanged(value, "IsTargetingAFormation");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowDistanceTexts
	{
		get
		{
			return _showDistanceTexts;
		}
		set
		{
			if (_showDistanceTexts != value)
			{
				_showDistanceTexts = value;
				OnPropertyChanged(value, "ShowDistanceTexts");
			}
		}
	}

	[DataSourceProperty]
	public int TeamType
	{
		get
		{
			return _teamType;
		}
		set
		{
			if (_teamType != value)
			{
				_teamType = value;
				OnPropertyChanged(value, "TeamType");
				_isMarkersDirty = true;
			}
		}
	}

	[DataSourceProperty]
	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (_wSign != value)
			{
				_wSign = value;
				OnPropertyChanged(value, "WSign");
			}
		}
	}

	[DataSourceProperty]
	public float Distance
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
	public float FarAlphaTarget
	{
		get
		{
			return _farAlphaTarget;
		}
		set
		{
			if (_farAlphaTarget != value)
			{
				_farAlphaTarget = value;
				OnPropertyChanged(value, "FarAlphaTarget");
			}
		}
	}

	[DataSourceProperty]
	public float FarDistanceCutoff
	{
		get
		{
			return _farDistanceCutoff;
		}
		set
		{
			if (_farDistanceCutoff != value)
			{
				_farDistanceCutoff = value;
				OnPropertyChanged(value, "FarDistanceCutoff");
			}
		}
	}

	[DataSourceProperty]
	public float CloseDistanceCutoff
	{
		get
		{
			return _closeDistanceCutoff;
		}
		set
		{
			if (_closeDistanceCutoff != value)
			{
				_closeDistanceCutoff = value;
				OnPropertyChanged(value, "CloseDistanceCutoff");
			}
		}
	}

	[DataSourceProperty]
	public float ClosestFadeoutRange
	{
		get
		{
			return _closestFadeoutRange;
		}
		set
		{
			if (_closestFadeoutRange != value)
			{
				_closestFadeoutRange = value;
				OnPropertyChanged(value, "ClosestFadeoutRange");
			}
		}
	}

	[DataSourceProperty]
	public float VisibilityRatio
	{
		get
		{
			return _visibilityRatioTarget;
		}
		set
		{
			float num = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
			if (!_visibilityRatioTarget.ApproximatelyEqualsTo(num))
			{
				_visibilityRatioTarget = num;
				OnPropertyChanged(value, "VisibilityRatio");
			}
		}
	}

	[DataSourceProperty]
	public float AlwaysOnDistance
	{
		get
		{
			return _alwaysOnDistance;
		}
		set
		{
			if (!_alwaysOnDistance.ApproximatelyEqualsTo(value))
			{
				_alwaysOnDistance = value;
				OnPropertyChanged(value, "AlwaysOnDistance");
			}
		}
	}

	[DataSourceProperty]
	public int VisibilityState
	{
		get
		{
			return _visibilityState;
		}
		set
		{
			if (value != _visibilityState)
			{
				_visibilityState = value;
				OnPropertyChanged(value, "VisibilityState");
			}
		}
	}

	[DataSourceProperty]
	public string MarkerType
	{
		get
		{
			return _markerType;
		}
		set
		{
			if (_markerType != value)
			{
				_markerType = value;
				OnPropertyChanged(value, "MarkerType");
				_isMarkersDirty = true;
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
	public Brush IconBrush
	{
		get
		{
			return _iconBrush;
		}
		set
		{
			if (_iconBrush != value)
			{
				_iconBrush = value;
				OnPropertyChanged(value, "IconBrush");
			}
		}
	}

	[DataSourceProperty]
	public Widget FormationTypeMarker
	{
		get
		{
			return _formationTypeMarker;
		}
		set
		{
			if (_formationTypeMarker != value)
			{
				_formationTypeMarker = value;
				OnPropertyChanged(value, "FormationTypeMarker");
				_isMarkersDirty = true;
			}
		}
	}

	[DataSourceProperty]
	public Widget TeamTypeMarker
	{
		get
		{
			return _teamTypeMarker;
		}
		set
		{
			if (_teamTypeMarker != value)
			{
				_teamTypeMarker = value;
				OnPropertyChanged(value, "TeamTypeMarker");
				_isMarkersDirty = true;
			}
		}
	}

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
			}
		}
	}

	public FormationMarkerListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		float amount = TaleWorlds.Library.MathF.Clamp(dt * 12f, 0f, 1f);
		if (_isMarkersDirty)
		{
			Sprite sprite = null;
			if (!string.IsNullOrEmpty(MarkerType) && IconBrush != null)
			{
				sprite = IconBrush.GetLayer(MarkerType)?.Sprite;
			}
			if (sprite != null && FormationTypeMarker != null)
			{
				FormationTypeMarker.Sprite = sprite;
			}
			else
			{
				Debug.FailedAssert("Couldn't find formation marker type image", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Mission\\FormationMarkerListPanel.cs", "OnLateUpdate", 55);
			}
			if (TeamTypeMarker != null)
			{
				TeamTypeMarker.RegisterBrushStatesOfWidget();
				if (TeamType == 0)
				{
					TeamTypeMarker.SetState("Player");
				}
				else if (TeamType == 1)
				{
					TeamTypeMarker.SetState("Ally");
				}
				else
				{
					TeamTypeMarker.SetState("Enemy");
				}
			}
			_isMarkersDirty = false;
		}
		float num;
		if (IsMarkerEnabled)
		{
			num = GetTargetAlpha(Distance);
			if (!IsActive)
			{
				num *= 0.5f;
			}
			if (!_markerDefaultSize.IsValid)
			{
				_markerDefaultSize = new Vec2(TeamTypeMarker.SuggestedWidth, TeamTypeMarker.SuggestedHeight);
			}
			float distanceRelatedScale = GetDistanceRelatedScale(Distance);
			TeamTypeMarker.SuggestedWidth = _markerDefaultSize.X * distanceRelatedScale;
			TeamTypeMarker.SuggestedHeight = _markerDefaultSize.Y * distanceRelatedScale;
		}
		else
		{
			num = 0f;
		}
		if (_smoothedAlpha == -1f)
		{
			_smoothedAlpha = num;
		}
		else
		{
			_smoothedAlpha = TaleWorlds.Library.MathF.Lerp(_smoothedAlpha, num, amount);
		}
		this.SetGlobalAlphaRecursively(_smoothedAlpha);
		UpdateScreenPosition();
		if (_smoothedAlpha <= 0.05f)
		{
			base.IsVisible = false;
		}
	}

	private void UpdateScreenPosition()
	{
		float num = Position.X - base.Size.X / 2f;
		float num2 = Position.X + base.Size.X / 2f;
		float num3 = Position.Y - base.Size.Y / 2f;
		float num4 = Position.Y + base.Size.Y / 2f;
		bool flag = WSign > 0 && num > 0f && num2 < base.Context.EventManager.PageSize.X && num3 > 0f && num4 < base.Context.EventManager.PageSize.Y;
		bool flag2 = WSign > 0 && (num2 > 0f || num < base.Context.EventManager.PageSize.X) && (num4 > 0f || num3 < base.Context.EventManager.PageSize.Y);
		if (!flag && IsTargetingAFormation)
		{
			base.IsVisible = true;
			Vec2 vec = new Vec2(num, num3);
			Vector2 vector = base.Context.EventManager.PageSize - base.Size;
			Vec2 vec2 = vector / 2f;
			vec -= vec2;
			if (WSign < 0)
			{
				vec *= -1f;
			}
			float radian = Mathf.Atan2(vec.y, vec.x) - System.MathF.PI / 2f;
			float num5 = Mathf.Cos(radian);
			float num6 = Mathf.Sin(radian);
			float num7 = num5 / num6;
			Vec2 vec3 = vec2 * 1f;
			vec = ((num5 > 0f) ? new Vec2((0f - vec3.y) / num7, vec2.y) : new Vec2(vec3.y / num7, 0f - vec2.y));
			if (vec.x > vec3.x)
			{
				vec = new Vec2(vec3.x, (0f - vec3.x) * num7);
			}
			else if (vec.x < 0f - vec3.x)
			{
				vec = new Vec2(0f - vec3.x, vec3.x * num7);
			}
			vec += vec2;
			base.ScaledPositionXOffset = Mathf.Clamp(vec.x, 0f, vector.X);
			base.ScaledPositionYOffset = Mathf.Clamp(vec.y, 0f, vector.Y);
		}
		else if (flag || flag2)
		{
			base.IsVisible = true;
			base.ScaledPositionXOffset = num;
			base.ScaledPositionYOffset = num3;
		}
		else
		{
			base.IsVisible = false;
		}
	}

	private float GetDistanceRelatedScale(float distance)
	{
		if (ShowDistanceTexts)
		{
			return 1f;
		}
		if (distance > FarDistanceCutoff)
		{
			return FarScaleTarget;
		}
		if (distance <= FarDistanceCutoff && distance >= CloseDistanceCutoff)
		{
			if (!HasUsableFadeRange())
			{
				return FarScaleTarget;
			}
			float amount = (float)Math.Pow((distance - CloseDistanceCutoff) / (FarDistanceCutoff - CloseDistanceCutoff), 1.0 / 3.0);
			return TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Lerp(CloseScaleTarget, FarScaleTarget, amount), FarScaleTarget, CloseScaleTarget);
		}
		return CloseScaleTarget;
	}

	private bool HasUsableFadeRange()
	{
		return FarDistanceCutoff - CloseDistanceCutoff > 0f;
	}

	private float GetTargetAlpha(float distance)
	{
		if (VisibilityState == 0)
		{
			return 0f;
		}
		if (VisibilityState == 2)
		{
			return 1f;
		}
		if (VisibilityState == -1)
		{
			return GetLegacyDistanceAlpha(distance) * _visibilityRatioTarget;
		}
		return GetDistanceRelatedAlphaTarget(distance);
	}

	private float GetDistanceRelatedAlphaTarget(float distance)
	{
		if (distance <= AlwaysOnDistance)
		{
			return 1f;
		}
		if (distance >= FarDistanceCutoff)
		{
			return FarAlphaTarget;
		}
		float num = FarDistanceCutoff - AlwaysOnDistance;
		if (num <= 0f)
		{
			return FarAlphaTarget;
		}
		float amount = (float)Math.Pow((distance - AlwaysOnDistance) / num, 1.0 / 3.0);
		return TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Lerp(1f, FarAlphaTarget, amount), FarAlphaTarget, 1f);
	}

	private float GetLegacyDistanceAlpha(float distance)
	{
		if (distance > FarDistanceCutoff)
		{
			return FarAlphaTarget;
		}
		if (distance >= CloseDistanceCutoff)
		{
			if (!HasUsableFadeRange())
			{
				return FarAlphaTarget;
			}
			float amount = (float)Math.Pow((distance - CloseDistanceCutoff) / (FarDistanceCutoff - CloseDistanceCutoff), 1.0 / 3.0);
			return TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Lerp(1f, FarAlphaTarget, amount), FarAlphaTarget, 1f);
		}
		if (distance > CloseDistanceCutoff - ClosestFadeoutRange)
		{
			float amount2 = (distance - (CloseDistanceCutoff - ClosestFadeoutRange)) / ClosestFadeoutRange;
			return TaleWorlds.Library.MathF.Lerp(0f, 1f, amount2);
		}
		return 0f;
	}
}

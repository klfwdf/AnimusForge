using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ShipThumbnailWidget : Widget
{
	private readonly BrushWidget _childWidget;

	private bool _shouldUpdateSprite;

	private Vec2 _previousSize;

	private string _prefabId;

	private Brush _spriteBrush;

	private Brush _styleBrush;

	[Editor(false)]
	public string PrefabId
	{
		get
		{
			return _prefabId;
		}
		set
		{
			if (_prefabId != value)
			{
				_prefabId = value;
				OnPropertyChanged(value, "PrefabId");
				_shouldUpdateSprite = true;
			}
		}
	}

	[Editor(false)]
	public Brush SpriteBrush
	{
		get
		{
			return _spriteBrush;
		}
		set
		{
			if (_spriteBrush != value)
			{
				_spriteBrush = value;
				OnPropertyChanged(value, "SpriteBrush");
				_shouldUpdateSprite = true;
			}
		}
	}

	[Editor(false)]
	public Brush StyleBrush
	{
		get
		{
			return _styleBrush;
		}
		set
		{
			if (_styleBrush != value)
			{
				_styleBrush = value;
				OnPropertyChanged(value, "StyleBrush");
				_childWidget.Brush = value;
				_shouldUpdateSprite = true;
			}
		}
	}

	public ShipThumbnailWidget(UIContext context)
		: base(context)
	{
		base.ClipContents = true;
		base.DoNotPassEventsToChildren = true;
		base.UpdateChildrenStates = true;
		_childWidget = new BrushWidget(context)
		{
			IsVisible = false,
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.Fixed,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		AddChild(_childWidget);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.Size != _previousSize)
		{
			_previousSize = base.Size;
			_shouldUpdateSprite = true;
		}
		if (_shouldUpdateSprite && (base.Size.X != 0f || base.WidthSizePolicy == SizePolicy.CoverChildren) && (base.Size.Y != 0f || base.HeightSizePolicy == SizePolicy.CoverChildren))
		{
			_shouldUpdateSprite = false;
			UpdateSprite();
		}
	}

	private void UpdateSprite()
	{
		Sprite sprite = (string.IsNullOrEmpty(PrefabId) ? null : SpriteBrush?.GetLayer(PrefabId)?.Sprite) ?? SpriteBrush?.DefaultLayer?.Sprite;
		_childWidget.Brush.DefaultLayer.Sprite = sprite;
		if (sprite != null)
		{
			_childWidget.IsVisible = true;
			float num;
			float num2;
			if (base.WidthSizePolicy == SizePolicy.CoverChildren && base.HeightSizePolicy == SizePolicy.CoverChildren)
			{
				num = sprite.Width;
				num2 = sprite.Height;
			}
			else if (base.WidthSizePolicy == SizePolicy.CoverChildren)
			{
				num2 = base.Size.Y * base._inverseScaleToUse;
				num = num2 * (float)sprite.Width / (float)sprite.Height;
			}
			else if (base.HeightSizePolicy == SizePolicy.CoverChildren)
			{
				num = base.Size.X * base._inverseScaleToUse;
				num2 = num * (float)sprite.Height / (float)sprite.Width;
			}
			else
			{
				float num3 = base.Size.X * base._inverseScaleToUse;
				float num4 = base.Size.Y * base._inverseScaleToUse;
				float val = num3 / (float)sprite.Width;
				float val2 = num4 / (float)sprite.Height;
				float num5 = Math.Max(val, val2);
				num = (float)sprite.Width * num5;
				num2 = (float)sprite.Height * num5;
			}
			_childWidget.SuggestedWidth = num;
			_childWidget.SuggestedHeight = num2;
		}
		else
		{
			_childWidget.IsVisible = false;
		}
	}
}

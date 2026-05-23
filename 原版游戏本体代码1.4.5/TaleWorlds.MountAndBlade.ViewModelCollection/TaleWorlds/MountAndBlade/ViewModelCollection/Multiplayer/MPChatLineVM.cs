using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;

public class MPChatLineVM : ViewModel
{
	private bool _forcedVisible;

	private string _category;

	private const float ChatVisibilityDuration = 10f;

	private const float ChatFadeOutDuration = 0.5f;

	private float _timeSinceCreation;

	private string _chatLine;

	private Color _color;

	private float _alpha;

	[DataSourceProperty]
	public string ChatLine
	{
		get
		{
			return _chatLine;
		}
		set
		{
			if (_chatLine != value)
			{
				_chatLine = value;
				OnPropertyChangedWithValue(value, "ChatLine");
			}
		}
	}

	[DataSourceProperty]
	public Color Color
	{
		get
		{
			return _color;
		}
		set
		{
			if (_color != value)
			{
				_color = value;
				OnPropertyChangedWithValue(value, "Color");
			}
		}
	}

	[DataSourceProperty]
	public float Alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			if (_alpha != value)
			{
				_alpha = value;
				OnPropertyChangedWithValue(value, "Alpha");
			}
		}
	}

	[DataSourceProperty]
	public string Category
	{
		get
		{
			return _category;
		}
		set
		{
			if (_category != value)
			{
				_category = value;
				OnPropertyChangedWithValue(value, "Category");
			}
		}
	}

	public MPChatLineVM(string chatLine, Color color, string category)
	{
		ChatLine = chatLine;
		Color = color;
		Alpha = 1f;
		Category = category;
	}

	public void HandleFading(float dt)
	{
		_timeSinceCreation += dt;
		RefreshAlpha();
	}

	private void RefreshAlpha()
	{
		if (_forcedVisible)
		{
			Alpha = 1f;
		}
		else
		{
			Alpha = GetActualAlpha();
		}
	}

	public void ForceInvisible()
	{
		_timeSinceCreation = 10.5f;
		Alpha = 0f;
	}

	private float GetActualAlpha()
	{
		if (_timeSinceCreation >= 10f)
		{
			return MBMath.ClampFloat(1f - (_timeSinceCreation - 10f) / 0.5f, 0f, 1f);
		}
		return 1f;
	}

	public void ToggleForceVisible(bool visible)
	{
		_forcedVisible = visible;
		RefreshAlpha();
	}
}

using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.Compass;

public class CompassTargetVM : ViewModel
{
	private int _distance;

	private string _color;

	private string _color2;

	private BannerImageIdentifierVM _banner;

	private string _iconType;

	private string _iconSpriteType;

	private string _letterCode;

	private float _position;

	private float _fullPosition;

	private bool _isAttacker;

	private bool _isEnemy;

	private bool _isFlag;

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner
	{
		get
		{
			return _banner;
		}
		set
		{
			if (value != _banner && (value == null || _banner == null || _banner.Id != value.Id))
			{
				_banner = value;
				OnPropertyChangedWithValue(value, "Banner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFlag
	{
		get
		{
			return _isFlag;
		}
		set
		{
			if (value != _isFlag)
			{
				_isFlag = value;
				OnPropertyChangedWithValue(value, "IsFlag");
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
			if (value != _distance)
			{
				_distance = value;
				OnPropertyChangedWithValue(value, "Distance");
			}
		}
	}

	[DataSourceProperty]
	public string Color2
	{
		get
		{
			return _color2;
		}
		set
		{
			if (value != _color2)
			{
				_color2 = value;
				OnPropertyChangedWithValue(value, "Color2");
			}
		}
	}

	[DataSourceProperty]
	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			if (value != _color)
			{
				_color = value;
				OnPropertyChangedWithValue(value, "Color");
			}
		}
	}

	[DataSourceProperty]
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
				OnPropertyChangedWithValue(value, "IconType");
				IconSpriteType = value;
			}
		}
	}

	[DataSourceProperty]
	public string IconSpriteType
	{
		get
		{
			return _iconSpriteType;
		}
		set
		{
			if (value != _iconSpriteType)
			{
				_iconSpriteType = value;
				OnPropertyChangedWithValue(value, "IconSpriteType");
			}
		}
	}

	[DataSourceProperty]
	public string LetterCode
	{
		get
		{
			return _letterCode;
		}
		set
		{
			if (value != _letterCode)
			{
				_letterCode = value;
				OnPropertyChangedWithValue(value, "LetterCode");
			}
		}
	}

	[DataSourceProperty]
	public float FullPosition
	{
		get
		{
			return _fullPosition;
		}
		set
		{
			if (MathF.Abs(value - _fullPosition) > float.Epsilon)
			{
				_fullPosition = value;
				OnPropertyChangedWithValue(value, "FullPosition");
			}
		}
	}

	[DataSourceProperty]
	public float Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (MathF.Abs(value - _position) > float.Epsilon)
			{
				_position = value;
				OnPropertyChangedWithValue(value, "Position");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAttacker
	{
		get
		{
			return _isAttacker;
		}
		set
		{
			if (value != _isAttacker)
			{
				_isAttacker = value;
				OnPropertyChangedWithValue(value, "IsAttacker");
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
			if (value != _isEnemy)
			{
				_isEnemy = value;
				OnPropertyChangedWithValue(value, "IsEnemy");
			}
		}
	}

	public CompassTargetVM(TargetIconType iconType, uint color, uint color2, Banner banner, bool isAttacker, bool isAlly)
	{
		IconType = iconType.ToString();
		LetterCode = GetLetterCode(iconType);
		RefreshColor(color, color2);
		IsFlag = iconType >= TargetIconType.Flag_A && iconType <= TargetIconType.Flag_I;
		IsAttacker = isAttacker;
		IsEnemy = !isAlly;
		if (banner == null)
		{
			Banner = new BannerImageIdentifierVM(null);
		}
		else
		{
			Banner = new BannerImageIdentifierVM(banner);
		}
	}

	private string GetLetterCode(TargetIconType iconType)
	{
		return iconType switch
		{
			TargetIconType.Flag_A => "A", 
			TargetIconType.Flag_B => "B", 
			TargetIconType.Flag_C => "C", 
			TargetIconType.Flag_D => "D", 
			TargetIconType.Flag_E => "E", 
			TargetIconType.Flag_F => "F", 
			TargetIconType.Flag_G => "G", 
			TargetIconType.Flag_H => "H", 
			TargetIconType.Flag_I => "I", 
			_ => "", 
		};
	}

	public void RefreshColor(uint color, uint color2)
	{
		if (color != 0)
		{
			string text = color.ToString("X");
			char c = text[0];
			char c2 = text[1];
			text = text.Remove(0, 2);
			text = text.Add(c.ToString() + c2, newLine: false);
			Color = "#" + text;
		}
		else
		{
			Color = "#FFFFFFFF";
		}
		if (color2 != 0)
		{
			string text2 = color2.ToString("X");
			char c3 = text2[0];
			char c4 = text2[1];
			text2 = text2.Remove(0, 2);
			text2 = text2.Add(c3.ToString() + c4, newLine: false);
			Color2 = "#" + text2;
		}
		else
		{
			Color2 = "#FFFFFFFF";
		}
	}

	public virtual void Refresh(float circleX, float x, float distance)
	{
		FullPosition = circleX;
		Position = x;
		Distance = MathF.Round(distance);
	}
}

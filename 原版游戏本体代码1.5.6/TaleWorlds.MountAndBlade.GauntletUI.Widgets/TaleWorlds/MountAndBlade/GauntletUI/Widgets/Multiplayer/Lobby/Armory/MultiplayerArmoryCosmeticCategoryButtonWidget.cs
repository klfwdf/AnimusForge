using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerArmoryCosmeticCategoryButtonWidget : ButtonWidget
{
	private const string _clothingTypeName = "Clothing";

	private const string _tauntTypeName = "Taunt";

	private Brush _clothingCategorySpriteBrush;

	private Brush _tauntCategorySpriteBrush;

	private string _cosmeticTypeName;

	private string _cosmeticCategoryName;

	[DataSourceProperty]
	public Brush ClothingCategorySpriteBrush
	{
		get
		{
			return _clothingCategorySpriteBrush;
		}
		set
		{
			if (value != _clothingCategorySpriteBrush)
			{
				_clothingCategorySpriteBrush = value;
				OnPropertyChanged(value, "ClothingCategorySpriteBrush");
				UpdateCategorySprite();
			}
		}
	}

	[DataSourceProperty]
	public Brush TauntCategorySpriteBrush
	{
		get
		{
			return _tauntCategorySpriteBrush;
		}
		set
		{
			if (value != _tauntCategorySpriteBrush)
			{
				_tauntCategorySpriteBrush = value;
				OnPropertyChanged(value, "TauntCategorySpriteBrush");
				UpdateCategorySprite();
			}
		}
	}

	[DataSourceProperty]
	public string CosmeticTypeName
	{
		get
		{
			return _cosmeticTypeName;
		}
		set
		{
			if (value != _cosmeticTypeName)
			{
				_cosmeticTypeName = value;
				OnPropertyChanged(value, "CosmeticTypeName");
				UpdateCategorySprite();
			}
		}
	}

	[DataSourceProperty]
	public string CosmeticCategoryName
	{
		get
		{
			return _cosmeticCategoryName;
		}
		set
		{
			if (value != _cosmeticCategoryName)
			{
				_cosmeticCategoryName = value;
				OnPropertyChanged(value, "CosmeticCategoryName");
				UpdateCategorySprite();
			}
		}
	}

	public MultiplayerArmoryCosmeticCategoryButtonWidget(UIContext context)
		: base(context)
	{
		CosmeticTypeName = string.Empty;
		CosmeticCategoryName = string.Empty;
	}

	private void UpdateCategorySprite()
	{
		if (!string.IsNullOrEmpty(CosmeticCategoryName) && !string.IsNullOrEmpty(CosmeticTypeName))
		{
			Sprite sprite = null;
			if (CosmeticTypeName == "Clothing")
			{
				sprite = GetClothingCategorySprite(CosmeticCategoryName);
			}
			else if (CosmeticTypeName == "Taunt")
			{
				sprite = GetTauntCategorySprite(CosmeticCategoryName);
			}
			if (sprite != null)
			{
				base.Brush.DefaultLayer.Sprite = sprite;
				base.Brush.Sprite = sprite;
			}
		}
	}

	private Sprite GetClothingCategorySprite(string clothingCategory)
	{
		return ClothingCategorySpriteBrush?.GetLayer(clothingCategory)?.Sprite;
	}

	private Sprite GetTauntCategorySprite(string tauntCategory)
	{
		return TauntCategorySpriteBrush?.GetLayer(tauntCategory)?.Sprite;
	}
}

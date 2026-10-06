using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class ItemTableauTextureProvider : TextureProvider
{
	private readonly ItemTableau _itemTableau;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	public string ItemModifierId
	{
		set
		{
			_itemTableau.SetItemModifierId(value);
		}
	}

	public string StringId
	{
		set
		{
			_itemTableau.SetStringId(value);
		}
	}

	public ItemRosterElement Item
	{
		set
		{
			_itemTableau.SetItem(value);
		}
	}

	public int Ammo
	{
		set
		{
			_itemTableau.SetAmmo(value);
		}
	}

	public int AverageUnitCost
	{
		set
		{
			_itemTableau.SetAverageUnitCost(value);
		}
	}

	public string BannerCode
	{
		set
		{
			_itemTableau.SetBannerCode(value);
		}
	}

	public bool CurrentlyRotating
	{
		set
		{
			_itemTableau.RotateItem(value);
		}
	}

	public float RotateItemVertical
	{
		set
		{
			_itemTableau.RotateItemVerticalWithAmount(value);
		}
	}

	public float RotateItemHorizontal
	{
		set
		{
			_itemTableau.RotateItemHorizontalWithAmount(value);
		}
	}

	public float InitialTiltRotation
	{
		set
		{
			_itemTableau.SetInitialTiltRotation(value);
		}
	}

	public float InitialPanRotation
	{
		set
		{
			_itemTableau.SetInitialPanRotation(value);
		}
	}

	public float CurrentZoom
	{
		set
		{
			_itemTableau.Zoom(value);
		}
	}

	public ItemTableauTextureProvider()
	{
		_itemTableau = new ItemTableau();
	}

	public override void Clear(bool clearNextFrame)
	{
		_itemTableau.OnFinalize();
		base.Clear(clearNextFrame);
	}

	private void CheckTexture()
	{
		if (_texture != _itemTableau.Texture)
		{
			_texture = _itemTableau.Texture;
			if (_texture != null)
			{
				EngineTexture platformTexture = new EngineTexture(_texture);
				_providedTexture = new TaleWorlds.TwoDimension.Texture(platformTexture);
			}
			else
			{
				_providedTexture = null;
			}
		}
	}

	protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		CheckTexture();
		return _providedTexture;
	}

	public override void SetTargetSize(int width, int height)
	{
		base.SetTargetSize(width, height);
		_itemTableau.SetTargetSize(width, height);
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		CheckTexture();
		_itemTableau.OnTick(dt);
	}
}

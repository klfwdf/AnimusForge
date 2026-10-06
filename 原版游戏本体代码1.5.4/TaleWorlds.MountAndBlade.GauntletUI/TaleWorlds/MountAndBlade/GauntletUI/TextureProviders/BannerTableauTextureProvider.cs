using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class BannerTableauTextureProvider : TextureProvider
{
	private BannerTableau _bannerTableau;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	private bool _isHidden;

	public string BannerCodeText
	{
		set
		{
			_bannerTableau.SetBannerCode(value);
		}
	}

	public bool IsNineGrid
	{
		set
		{
			_bannerTableau.SetIsNineGrid(value);
		}
	}

	public float CustomRenderScale
	{
		set
		{
			_bannerTableau.SetCustomRenderScale(value);
		}
	}

	public Vec2 UpdatePositionValueManual
	{
		set
		{
			_bannerTableau.SetUpdatePositionValueManual(value);
		}
	}

	public Vec2 UpdateSizeValueManual
	{
		set
		{
			_bannerTableau.SetUpdateSizeValueManual(value);
		}
	}

	public (float, bool) UpdateRotationValueManualWithMirror
	{
		set
		{
			_bannerTableau.SetUpdateRotationValueManual(value);
		}
	}

	public int MeshIndexToUpdate
	{
		set
		{
			_bannerTableau.SetMeshIndexToUpdate(value);
		}
	}

	public bool IsHidden
	{
		get
		{
			return _isHidden;
		}
		set
		{
			if (_isHidden != value)
			{
				_isHidden = value;
			}
		}
	}

	public BannerTableauTextureProvider()
	{
		_bannerTableau = new BannerTableau();
	}

	public override void Clear(bool clearNextFrame)
	{
		_bannerTableau.OnFinalize();
		base.Clear(clearNextFrame);
	}

	private void CheckTexture()
	{
		if (_texture != _bannerTableau.Texture)
		{
			_texture = _bannerTableau.Texture;
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
		_bannerTableau.SetTargetSize(width, height);
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		CheckTexture();
		_bannerTableau.OnTick(dt);
	}
}

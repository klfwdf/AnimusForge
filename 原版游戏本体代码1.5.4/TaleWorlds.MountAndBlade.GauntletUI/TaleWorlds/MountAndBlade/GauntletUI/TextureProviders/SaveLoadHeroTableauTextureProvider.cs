using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class SaveLoadHeroTableauTextureProvider : TextureProvider
{
	private string _characterCode;

	private BasicCharacterTableau _tableau;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	public string HeroVisualCode
	{
		set
		{
			_characterCode = value;
			DeserializeCharacterCode(_characterCode);
		}
	}

	public string BannerCode
	{
		set
		{
			_tableau.SetBannerCode(value);
		}
	}

	public bool IsVersionCompatible => _tableau.IsVersionCompatible;

	public bool CurrentlyRotating
	{
		set
		{
			_tableau.RotateCharacter(value);
		}
	}

	public SaveLoadHeroTableauTextureProvider()
	{
		_tableau = new BasicCharacterTableau();
	}

	public override void Tick(float dt)
	{
		CheckTexture();
		_tableau.OnTick(dt);
	}

	public override void SetTargetSize(int width, int height)
	{
		base.SetTargetSize(width, height);
		_tableau.SetTargetSize(width, height);
	}

	private void DeserializeCharacterCode(string characterCode)
	{
		if (!string.IsNullOrEmpty(characterCode))
		{
			_tableau.DeserializeCharacterCode(characterCode);
		}
	}

	private void CheckTexture()
	{
		if (_texture != _tableau.Texture)
		{
			_texture = _tableau.Texture;
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

	public override void Clear(bool clearNextFrame)
	{
		_tableau.OnFinalize();
		base.Clear(clearNextFrame);
	}

	protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		CheckTexture();
		return _providedTexture;
	}
}

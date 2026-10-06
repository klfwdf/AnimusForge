using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class BrightnessDemoTextureProvider : TextureProvider
{
	private BrightnessDemoTableau _sceneTableau;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	private EngineTexture wrappedTexture;

	public int DemoType
	{
		set
		{
			_sceneTableau.SetDemoType(value);
		}
	}

	public BrightnessDemoTextureProvider()
	{
		_sceneTableau = new BrightnessDemoTableau();
	}

	private void CheckTexture()
	{
		if (_sceneTableau != null)
		{
			if (_texture != _sceneTableau.Texture)
			{
				_texture = _sceneTableau.Texture;
				if (_texture != null)
				{
					wrappedTexture = new EngineTexture(_texture);
					_providedTexture = new TaleWorlds.TwoDimension.Texture(wrappedTexture);
				}
				else
				{
					_providedTexture = null;
				}
			}
		}
		else
		{
			_providedTexture = null;
		}
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		CheckTexture();
		_sceneTableau?.OnTick(dt);
	}

	public override void Clear(bool clearNextFrame)
	{
		_sceneTableau?.OnFinalize();
		base.Clear(clearNextFrame);
	}

	public override void SetTargetSize(int width, int height)
	{
		base.SetTargetSize(width, height);
		_sceneTableau.SetTargetSize(width, height);
	}

	protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		CheckTexture();
		return _providedTexture;
	}
}

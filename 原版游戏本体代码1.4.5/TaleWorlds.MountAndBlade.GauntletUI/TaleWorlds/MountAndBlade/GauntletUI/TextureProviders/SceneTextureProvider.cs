using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class SceneTextureProvider : TextureProvider
{
	private SceneTableau _sceneTableau;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	private EngineTexture wrappedTexture;

	public Scene WantedScene { get; private set; }

	public bool? IsReady => _sceneTableau?.IsReady;

	public object Scene
	{
		set
		{
			if (value != null)
			{
				_sceneTableau = new SceneTableau();
				_sceneTableau.SetScene(value);
			}
			else
			{
				_sceneTableau.OnFinalize();
				_sceneTableau = null;
			}
		}
	}

	public SceneTextureProvider()
	{
		_sceneTableau = new SceneTableau();
	}

	private void CheckTexture()
	{
		if (_sceneTableau != null)
		{
			if (_texture != _sceneTableau._texture)
			{
				_texture = _sceneTableau._texture;
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

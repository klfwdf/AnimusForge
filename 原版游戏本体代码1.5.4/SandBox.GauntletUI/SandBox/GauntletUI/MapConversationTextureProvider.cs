using SandBox.View.Map;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

public class MapConversationTextureProvider : TextureProvider
{
	private MapConversationTableau _mapConversationTableau;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	public object Data
	{
		set
		{
			_mapConversationTableau.SetData(value);
		}
	}

	public bool IsEnabled
	{
		set
		{
			_mapConversationTableau.SetEnabled(value);
		}
	}

	public MapConversationTextureProvider()
	{
		_mapConversationTableau = new MapConversationTableau();
	}

	public override void Clear(bool clearNextFrame)
	{
		_mapConversationTableau.OnFinalize(clearNextFrame);
		base.Clear(clearNextFrame);
	}

	private void CheckTexture()
	{
		if (_texture != _mapConversationTableau.Texture)
		{
			_texture = _mapConversationTableau.Texture;
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
		_mapConversationTableau.SetTargetSize(width, height);
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		CheckTexture();
		_mapConversationTableau.OnTick(dt);
	}
}

using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI;

public class EngineTexture : ITexture
{
	public Texture Texture { get; private set; }

	bool ITexture.IsValid
	{
		get
		{
			if (Texture.IsValid)
			{
				return !Texture.IsReleased;
			}
			return false;
		}
	}

	int ITexture.Width => Texture.Width;

	int ITexture.Height => Texture.Height;

	string ITexture.Name
	{
		get
		{
			return Texture.Name;
		}
		set
		{
			Texture.Name = value;
		}
	}

	public EngineTexture(Texture engineTexture)
	{
		Texture = engineTexture;
	}

	bool ITexture.IsLoaded()
	{
		return Texture.IsLoaded();
	}

	void ITexture.Release()
	{
		Texture.ReleaseImmediately();
		Texture = null;
	}

	public override int GetHashCode()
	{
		return ((((0x5BDE4B ^ Texture.GetHashCode()) * 8713) ^ ((ITexture)this).Width.GetHashCode()) * 8713) ^ ((ITexture)this).Height.GetHashCode();
	}
}

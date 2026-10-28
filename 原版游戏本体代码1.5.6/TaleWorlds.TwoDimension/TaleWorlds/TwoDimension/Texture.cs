namespace TaleWorlds.TwoDimension;

public class Texture
{
	public ITexture PlatformTexture { get; private set; }

	public bool IsValid => PlatformTexture.IsValid;

	public int Width => PlatformTexture.Width;

	public int Height => PlatformTexture.Height;

	public Texture(ITexture platformTexture)
	{
		PlatformTexture = platformTexture;
	}

	public bool IsLoaded()
	{
		return PlatformTexture.IsLoaded();
	}
}

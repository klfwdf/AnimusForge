namespace TaleWorlds.TwoDimension;

public interface ITexture
{
	bool IsValid { get; }

	int Width { get; }

	int Height { get; }

	string Name { get; set; }

	void Release();

	bool IsLoaded();
}

using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public struct TextureCreationInfo
{
	public bool IsValid;

	public bool CreatedNewTexture;

	public bool UsingExistingTexture;

	public Texture Texture;

	public bool IsSuccess
	{
		get
		{
			if (IsValid)
			{
				if (!CreatedNewTexture)
				{
					return UsingExistingTexture;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsFail
	{
		get
		{
			if (IsValid && !CreatedNewTexture)
			{
				return !UsingExistingTexture;
			}
			return false;
		}
	}

	public static TextureCreationInfo WithNewTexture(Texture texture = null)
	{
		return new TextureCreationInfo
		{
			IsValid = true,
			CreatedNewTexture = true,
			Texture = texture
		};
	}

	public static TextureCreationInfo WithExistingTexture(Texture texture)
	{
		return new TextureCreationInfo
		{
			IsValid = true,
			UsingExistingTexture = true,
			Texture = texture
		};
	}

	public static TextureCreationInfo Fail()
	{
		return new TextureCreationInfo
		{
			IsValid = true
		};
	}
}

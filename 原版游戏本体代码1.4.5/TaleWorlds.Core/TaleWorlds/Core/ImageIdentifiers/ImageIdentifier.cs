namespace TaleWorlds.Core.ImageIdentifiers;

public abstract class ImageIdentifier
{
	public string Id { get; set; }

	public string TextureProviderName { get; protected set; }

	public string AdditionalArgs { get; protected set; }

	public bool Equals(ImageIdentifier other)
	{
		if (other != null && Id.Equals(other.Id) && AdditionalArgs.Equals(other.AdditionalArgs))
		{
			return TextureProviderName.Equals(other.TextureProviderName);
		}
		return false;
	}
}

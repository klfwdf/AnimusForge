using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class AvatarThumbnailCreationData : ThumbnailCreationData
{
	public string AvatarID { get; private set; }

	public byte[] AvatarBytes { get; private set; }

	public uint Width { get; private set; }

	public uint Height { get; private set; }

	public AvatarData.ImageType ImageType { get; private set; }

	public AvatarThumbnailCreationData(string avatarID, byte[] avatarBytes, uint width, uint height, AvatarData.ImageType imageType)
		: base(avatarID, null, null)
	{
		AvatarID = avatarID;
		AvatarBytes = avatarBytes;
		Width = width;
		Height = height;
		ImageType = imageType;
	}
}

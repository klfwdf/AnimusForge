using TaleWorlds.Engine;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class AvatarThumbnailCache : ThumbnailCache<AvatarThumbnailCreationData>
{
	public static AvatarThumbnailCache Current { get; private set; }

	public AvatarThumbnailCache(int capacity)
		: base(capacity)
	{
		Current = this;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		Current = null;
	}

	protected override TextureCreationInfo OnCreateTexture(AvatarThumbnailCreationData thumbnailCreationData)
	{
		((IThumbnailCache)this).GetValue(thumbnailCreationData.AvatarID, out Texture texture);
		if (texture == null)
		{
			if (thumbnailCreationData.AvatarBytes == null || thumbnailCreationData.AvatarBytes.Length == 0)
			{
				return TextureCreationInfo.WithNewTexture();
			}
			if (thumbnailCreationData.ImageType == AvatarData.ImageType.Image)
			{
				texture = Texture.CreateFromMemory(thumbnailCreationData.AvatarBytes);
				texture.Name = ThumbnailDebugUtility.CreateDebugIdFrom(thumbnailCreationData.AvatarID, "avatar", "byte_array");
			}
			else if (thumbnailCreationData.ImageType == AvatarData.ImageType.Raw)
			{
				texture = Texture.CreateFromByteArray(thumbnailCreationData.AvatarBytes, (int)thumbnailCreationData.Width, (int)thumbnailCreationData.Height);
				texture.Name = ThumbnailDebugUtility.CreateDebugIdFrom(thumbnailCreationData.AvatarID, "avatar", "raw_data");
			}
			((IThumbnailCache)this).Add(thumbnailCreationData.AvatarID, texture);
			((IThumbnailCache)this).AddReference(thumbnailCreationData.AvatarID);
			return TextureCreationInfo.WithNewTexture(texture);
		}
		((IThumbnailCache)this).AddReference(thumbnailCreationData.AvatarID);
		return TextureCreationInfo.WithExistingTexture(texture);
	}

	protected override bool OnReleaseTexture(AvatarThumbnailCreationData thumbnailCreationData)
	{
		return true;
	}

	public void FlushCache()
	{
		((IThumbnailCache)this).Clear(releaseImmediately: true);
	}
}

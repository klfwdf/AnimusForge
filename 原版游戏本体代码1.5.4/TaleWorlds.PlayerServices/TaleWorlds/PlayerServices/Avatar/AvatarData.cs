namespace TaleWorlds.PlayerServices.Avatar;

public class AvatarData
{
	public enum ImageType
	{
		Image,
		Raw
	}

	public enum DataStatus
	{
		NotReady,
		Ready,
		Failed
	}

	public byte[] Image { get; private set; }

	public uint Width { get; private set; }

	public uint Height { get; private set; }

	public ImageType Type { get; private set; }

	public DataStatus Status { get; private set; }

	public AvatarData()
	{
		Status = DataStatus.NotReady;
	}

	public AvatarData(byte[] image, uint width, uint height)
	{
		SetImageData(image, width, height);
	}

	public AvatarData(byte[] image)
	{
		SetImageData(image);
	}

	public void SetImageData(byte[] image, uint width, uint height)
	{
		Image = image;
		Width = width;
		Height = height;
		Type = ImageType.Raw;
		Status = DataStatus.Ready;
	}

	public void SetImageData(byte[] image)
	{
		Image = image;
		Type = ImageType.Image;
		Status = DataStatus.Ready;
	}

	public void SetFailed()
	{
		Status = DataStatus.Failed;
	}
}

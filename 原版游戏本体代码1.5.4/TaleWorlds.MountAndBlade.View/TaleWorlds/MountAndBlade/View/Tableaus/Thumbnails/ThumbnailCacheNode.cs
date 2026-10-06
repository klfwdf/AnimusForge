using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class ThumbnailCacheNode
{
	public string Key;

	public Texture Value;

	public int FrameNo;

	public int ReferenceCount;

	public ThumbnailCacheNode()
	{
	}

	public ThumbnailCacheNode(string key, Texture value, int frameNo)
	{
		Key = key;
		Value = value;
		FrameNo = frameNo;
		ReferenceCount = 0;
	}
}

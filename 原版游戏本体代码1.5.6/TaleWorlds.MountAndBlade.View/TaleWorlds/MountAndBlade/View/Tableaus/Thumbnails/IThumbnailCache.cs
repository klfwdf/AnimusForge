using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public interface IThumbnailCache
{
	int Count { get; }

	int RenderCallbackCount { get; }

	void Initialize(ThumbnailCreatorView thumnbailCreatorView);

	void Destroy();

	void Clear(bool releaseImmediately);

	bool GetValue(string key, out Texture texture);

	bool AddReference(string key);

	bool RemoveReference(string key);

	bool OnThumbnailRenderCompleted(string renderId, Texture renderTarget);

	void ClearUnusedCache();

	void Tick(float dt);

	void Add(string key, Texture value);

	void PrintToImgui();

	TextureCreationInfo CreateTexture(ThumbnailCreationData thumbnailCreationData);

	bool ReleaseTexture(ThumbnailCreationData thumbnailCreationData);
}

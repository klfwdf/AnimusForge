using System;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class BannerEditorTextureCache : ThumbnailCache<BannerEditorTextureCreationData>
{
	public static BannerEditorTextureCache Current { get; private set; }

	public BannerEditorTextureCache(int capacity)
		: base(capacity)
	{
		Current = this;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		Current = null;
	}

	protected override TextureCreationInfo OnCreateTexture(BannerEditorTextureCreationData textureCreationData)
	{
		Action<Texture> setAction = textureCreationData.SetAction;
		Action cancelAction = textureCreationData.CancelAction;
		string renderId = textureCreationData.RenderId;
		if (((IThumbnailCache)this).GetValue(renderId, out Texture texture))
		{
			if (_renderCallbacks.ContainsKey(renderId))
			{
				_renderCallbacks[renderId].SetActions.Add(setAction);
				_renderCallbacks[renderId].CancelActions.Add(cancelAction);
			}
			else
			{
				setAction?.Invoke(texture);
			}
			((IThumbnailCache)this).AddReference(renderId);
			return TextureCreationInfo.WithExistingTexture(texture);
		}
		Texture texture2 = BannerTextureCreator.CreateTexture(textureCreationData);
		((IThumbnailCache)this).Add(renderId, texture2);
		((IThumbnailCache)this).AddReference(renderId);
		if (!_renderCallbacks.ContainsKey(renderId))
		{
			_renderCallbacks.Add(renderId, RenderCallbackCollection.CreateEmpty());
		}
		_renderCallbacks[renderId].SetActions.Add(setAction);
		_renderCallbacks[renderId].CancelActions.Add(cancelAction);
		return TextureCreationInfo.WithNewTexture(texture2);
	}

	protected override bool OnReleaseTexture(BannerEditorTextureCreationData thumbnailCreationData)
	{
		string renderId = thumbnailCreationData.RenderId;
		return ((IThumbnailCache)this).RemoveReference(renderId);
	}

	public void FlushCache()
	{
		((IThumbnailCache)this).Clear(releaseImmediately: true);
	}
}

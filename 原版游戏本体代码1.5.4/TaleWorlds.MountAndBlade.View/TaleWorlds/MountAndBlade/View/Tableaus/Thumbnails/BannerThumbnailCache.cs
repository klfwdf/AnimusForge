using System;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class BannerThumbnailCache : ThumbnailCache<BannerThumbnailCreationData>
{
	public BannerThumbnailCache(int capacity)
		: base(capacity)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		BannerTextureCreator.Initialize(_thumbnailCreatorView);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		BannerTextureCreator.OnFinalize();
	}

	protected override TextureCreationInfo OnCreateTexture(BannerThumbnailCreationData thumbnailCreationData)
	{
		Action<Texture> setAction = thumbnailCreationData.SetAction;
		Action cancelAction = thumbnailCreationData.CancelAction;
		string renderId = thumbnailCreationData.RenderId;
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
		Texture texture2 = BannerTextureCreator.CreateTexture(thumbnailCreationData);
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

	protected override bool OnReleaseTexture(BannerThumbnailCreationData thumbnailCreationData)
	{
		string renderId = thumbnailCreationData.RenderId;
		return ((IThumbnailCache)this).RemoveReference(renderId);
	}
}

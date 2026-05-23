using System;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.ImageIdentifiers;

public abstract class ImageIdentifierTextureProvider : TextureProvider, IDisposable
{
	private bool _textureRequiresRefreshing;

	private bool _handleNewlyCreatedTexture;

	private TaleWorlds.Engine.Texture _texture;

	private TaleWorlds.TwoDimension.Texture _providedTexture;

	private string _imageId;

	private string _additionalArgs;

	private bool _isBig;

	private bool _isReleased;

	protected ThumbnailCreationData ThumbnailCreationData { get; set; }

	public bool IsReleased
	{
		get
		{
			return _isReleased;
		}
		set
		{
			if (_isReleased != value)
			{
				_isReleased = value;
				if (_isReleased)
				{
					ReleaseCache();
					_isReleased = false;
				}
			}
			_textureRequiresRefreshing = true;
			_handleNewlyCreatedTexture = true;
		}
	}

	public bool IsBig
	{
		get
		{
			return _isBig;
		}
		set
		{
			if (_isBig != value)
			{
				_isBig = value;
				_textureRequiresRefreshing = true;
			}
		}
	}

	public string ImageId
	{
		get
		{
			return _imageId;
		}
		set
		{
			if (_imageId != value)
			{
				_imageId = value;
				_textureRequiresRefreshing = true;
			}
		}
	}

	public string AdditionalArgs
	{
		get
		{
			return _additionalArgs;
		}
		set
		{
			if (_additionalArgs != value)
			{
				_additionalArgs = value;
				_textureRequiresRefreshing = true;
			}
		}
	}

	public ImageIdentifierTextureProvider()
	{
		_textureRequiresRefreshing = true;
	}

	~ImageIdentifierTextureProvider()
	{
		OnDisposed();
	}

	protected abstract void OnCreateImageWithId(string id, string additionalArgs);

	public override void Tick(float dt)
	{
		base.Tick(dt);
		CheckTexture();
	}

	private void ReleaseCache()
	{
		if (ThumbnailCreationData != null)
		{
			if (!ThumbnailCreationData.IsProcessed)
			{
				Debug.FailedAssert("Created thumbnail data but trying to release it before its processed", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\TextureProviders\\ImageIdentifiers\\ImageIdentifierTextureProvider.cs", "ReleaseCache", 50);
				return;
			}
			ThumbnailCacheManager.Current.DestroyTexture(ThumbnailCreationData);
			ThumbnailCreationData = null;
		}
	}

	public override void Clear(bool clearNextFrame)
	{
		base.Clear(clearNextFrame);
		_providedTexture = null;
		_textureRequiresRefreshing = true;
		ReleaseCache();
	}

	protected virtual bool GetCanForceCheckTexture()
	{
		return false;
	}

	protected virtual void OnCheckTexture()
	{
		CreateImageWithId(ImageId, AdditionalArgs);
	}

	protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		return _providedTexture;
	}

	protected void ForceRefreshTextures()
	{
		_textureRequiresRefreshing = true;
	}

	private void CheckTexture()
	{
		if (_textureRequiresRefreshing || GetCanForceCheckTexture())
		{
			_texture = null;
			ReleaseCache();
			OnCheckTexture();
			_textureRequiresRefreshing = false;
		}
		if (!_handleNewlyCreatedTexture)
		{
			return;
		}
		TaleWorlds.Engine.Texture texture = null;
		if (_providedTexture?.PlatformTexture is EngineTexture engineTexture)
		{
			texture = engineTexture.Texture;
		}
		if (_texture != texture)
		{
			if (_texture != null)
			{
				EngineTexture platformTexture = new EngineTexture(_texture);
				_providedTexture = new TaleWorlds.TwoDimension.Texture(platformTexture);
			}
			else
			{
				_providedTexture = null;
			}
		}
		_handleNewlyCreatedTexture = false;
	}

	public void CreateImageWithId(string id, string additionalArgs)
	{
		OnCreateImageWithId(id, additionalArgs ?? string.Empty);
	}

	protected void OnTextureCreated(TaleWorlds.Engine.Texture texture)
	{
		_texture = texture;
		_textureRequiresRefreshing = false;
		_handleNewlyCreatedTexture = true;
	}

	protected void OnTextureCreationCancelled()
	{
		_texture = null;
	}

	private void OnDisposed()
	{
		ReleaseCache();
	}

	void IDisposable.Dispose()
	{
		OnDisposed();
	}
}

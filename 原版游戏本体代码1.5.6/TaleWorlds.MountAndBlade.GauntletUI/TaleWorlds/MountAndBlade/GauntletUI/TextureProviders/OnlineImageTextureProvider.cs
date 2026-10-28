using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class OnlineImageTextureProvider : TextureProvider
{
	private const string DataFolder = "Online Images";

	private const int MaxRetryCount = 4;

	private const float RetryWaitTime = 1f;

	private static readonly PlatformDirectoryPath _onlineImageCacheFolderPath = new PlatformDirectoryPath(PlatformFileType.User, "Online Images");

	private static readonly Dictionary<string, TaleWorlds.Engine.Texture> _onlineImageTextureCache = new Dictionary<string, TaleWorlds.Engine.Texture>();

	private static readonly Dictionary<string, Task<byte[]>> _onlineImageDownloadTasks = new Dictionary<string, Task<byte[]>>();

	private static readonly object _onlineImageSaveLock = new object();

	private static Dictionary<string, PlatformFilePath> _onlineImageCache = new Dictionary<string, PlatformFilePath>();

	private static bool _isOnlineImageCachePopulated;

	private TaleWorlds.Engine.Texture _texture;

	private string _onlineSourceUrl;

	private bool _requiresRetry;

	private int _retryCount;

	private float _timeSinceRetryRequested;

	private float _currentRetryWaitTime = 1f;

	public string OnlineSourceUrl
	{
		set
		{
			_onlineSourceUrl = value;
			ResetRetryState();
			RefreshOnlineImage();
		}
	}

	public OnlineImageTextureProvider()
	{
		PopulateOnlineImageCache();
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		if (_requiresRetry)
		{
			_timeSinceRetryRequested += dt;
			if (_timeSinceRetryRequested >= _currentRetryWaitTime)
			{
				_requiresRetry = false;
				_currentRetryWaitTime *= 2f;
				RefreshOnlineImage();
			}
		}
	}

	private async void RefreshOnlineImage()
	{
		if (string.IsNullOrEmpty(_onlineSourceUrl))
		{
			ResetRetryState();
			return;
		}
		string url = _onlineSourceUrl;
		string guidOfRequestedURL = ToGuid(url).ToString();
		try
		{
			if (_onlineImageTextureCache.TryGetValue(guidOfRequestedURL, out var value))
			{
				OnTextureCreated(value);
				ResetRetryState();
				return;
			}
			bool isConsole = ApplicationPlatform.IsPlatformConsole();
			if (!isConsole && _onlineImageCache.TryGetValue(guidOfRequestedURL, out var value2))
			{
				TaleWorlds.Engine.Texture texture = TaleWorlds.Engine.Texture.CreateTextureFromPath(value2);
				if (texture != null)
				{
					texture.Name = "online_image_" + guidOfRequestedURL;
					_onlineImageTextureCache[guidOfRequestedURL] = texture;
					OnTextureCreated(texture);
					ResetRetryState();
					return;
				}
				_onlineImageCache.Remove(guidOfRequestedURL);
			}
			bool isDownloadOwner = !_onlineImageDownloadTasks.TryGetValue(guidOfRequestedURL, out var value3);
			if (isDownloadOwner)
			{
				value3 = HttpHelper.DownloadDataTaskAsync(url);
				_onlineImageDownloadTasks[guidOfRequestedURL] = value3;
			}
			byte[] downloadedData;
			try
			{
				downloadedData = await value3;
			}
			finally
			{
				if (isDownloadOwner)
				{
					_onlineImageDownloadTasks.Remove(guidOfRequestedURL);
				}
			}
			if (_onlineSourceUrl != url)
			{
				return;
			}
			if (_onlineImageTextureCache.TryGetValue(guidOfRequestedURL, out value))
			{
				OnTextureCreated(value);
				ResetRetryState();
				return;
			}
			if (downloadedData == null || downloadedData.Length == 0)
			{
				RequestRetry();
				return;
			}
			TaleWorlds.Engine.Texture texture2;
			if (isConsole || !isDownloadOwner)
			{
				texture2 = TaleWorlds.Engine.Texture.CreateFromMemory(downloadedData);
			}
			else
			{
				PlatformFilePath pathOfTheDownloadedImage = new PlatformFilePath(_onlineImageCacheFolderPath, guidOfRequestedURL + ".png");
				SaveResult saveResult = await Task.Run(delegate
				{
					lock (_onlineImageSaveLock)
					{
						return FileHelper.SaveFile(pathOfTheDownloadedImage, downloadedData);
					}
				});
				if (_onlineSourceUrl != url)
				{
					return;
				}
				if (_onlineImageTextureCache.TryGetValue(guidOfRequestedURL, out value))
				{
					OnTextureCreated(value);
					ResetRetryState();
					return;
				}
				if (saveResult == SaveResult.Success)
				{
					_onlineImageCache[guidOfRequestedURL] = pathOfTheDownloadedImage;
					texture2 = TaleWorlds.Engine.Texture.CreateTextureFromPath(pathOfTheDownloadedImage);
					if (texture2 == null)
					{
						texture2 = TaleWorlds.Engine.Texture.CreateFromMemory(downloadedData);
					}
				}
				else
				{
					Debug.Print($"FAILED TO SAVE ONLINE IMAGE: {url} | SAVE RESULT: {saveResult}", 0, Debug.DebugColor.Red);
					texture2 = TaleWorlds.Engine.Texture.CreateFromMemory(downloadedData);
				}
			}
			if (texture2 != null)
			{
				texture2.Name = "online_image_" + guidOfRequestedURL;
				_onlineImageTextureCache[guidOfRequestedURL] = texture2;
				OnTextureCreated(texture2);
				ResetRetryState();
			}
			else
			{
				Debug.Print("FAILED TO CREATE TEXTURE FROM DOWNLOADED DATA: " + url, 0, Debug.DebugColor.Red);
			}
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Error while trying to get image online: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\TextureProviders\\OnlineImageTextureProvider.cs", "RefreshOnlineImage", 208);
			RequestRetry();
		}
	}

	private void RequestRetry()
	{
		_retryCount++;
		if (_retryCount >= 4)
		{
			_requiresRetry = false;
			return;
		}
		_timeSinceRetryRequested = 0f;
		_requiresRetry = true;
		Debug.Print($"RETRYING TO DOWNLOAD: {_onlineSourceUrl} | RETRY COUNT: {_retryCount}", 0, Debug.DebugColor.Red);
	}

	private void ResetRetryState()
	{
		_retryCount = 0;
		_currentRetryWaitTime = 1f;
		_timeSinceRetryRequested = 0f;
		_requiresRetry = false;
	}

	protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		if (_texture != null)
		{
			return new TaleWorlds.TwoDimension.Texture(new EngineTexture(_texture));
		}
		return null;
	}

	private static void PopulateOnlineImageCache()
	{
		if (!_isOnlineImageCachePopulated && !ApplicationPlatform.IsPlatformConsole())
		{
			_isOnlineImageCachePopulated = true;
			PlatformFilePath[] files = FileHelper.GetFiles(_onlineImageCacheFolderPath, "*.png", SearchOption.AllDirectories);
			for (int i = 0; i < files.Length; i++)
			{
				PlatformFilePath value = files[i];
				string fileNameWithoutExtension = value.GetFileNameWithoutExtension();
				_onlineImageCache[fileNameWithoutExtension] = value;
			}
		}
	}

	private static Guid ToGuid(string src)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(src);
		byte[] array = new SHA1CryptoServiceProvider().ComputeHash(bytes);
		Array.Resize(ref array, 16);
		return new Guid(array);
	}

	private void OnTextureCreated(TaleWorlds.Engine.Texture texture)
	{
		_texture = texture;
	}
}

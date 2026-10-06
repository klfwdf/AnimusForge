using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

public class OnlineImageTextureProvider : TextureProvider
{
	private Dictionary<string, PlatformFilePath> _onlineImageCache;

	private readonly string DataFolder = "Online Images";

	private readonly PlatformDirectoryPath _onlineImageCacheFolderPath;

	private TaleWorlds.Engine.Texture _texture;

	private bool _requiresRetry;

	private int _retryCount;

	private const int _maxRetryCount = 10;

	private string _onlineSourceUrl;

	public string OnlineSourceUrl
	{
		set
		{
			_onlineSourceUrl = value;
			RefreshOnlineImage();
		}
	}

	public OnlineImageTextureProvider()
	{
		_onlineImageCache = new Dictionary<string, PlatformFilePath>();
		_onlineImageCacheFolderPath = new PlatformDirectoryPath(PlatformFileType.User, DataFolder);
		PopulateOnlineImageCache();
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		if (_requiresRetry)
		{
			if (10 < _retryCount)
			{
				_requiresRetry = false;
				return;
			}
			_retryCount++;
			RefreshOnlineImage();
		}
	}

	private async void RefreshOnlineImage()
	{
		if (string.IsNullOrEmpty(_onlineSourceUrl))
		{
			_requiresRetry = false;
		}
		else
		{
			if (_retryCount >= 10)
			{
				return;
			}
			try
			{
				string guidOfRequestedURL = ToGuid(_onlineSourceUrl).ToString();
				if (!_onlineImageCache.ContainsKey(guidOfRequestedURL))
				{
					PlatformFilePath pathOfTheDownloadedImage = new PlatformFilePath(_onlineImageCacheFolderPath, guidOfRequestedURL + ".png");
					byte[] array = await HttpHelper.DownloadDataTaskAsync(_onlineSourceUrl);
					if (array != null)
					{
						FileHelper.SaveFile(pathOfTheDownloadedImage, array);
						_onlineImageCache.Add(guidOfRequestedURL, pathOfTheDownloadedImage);
					}
				}
				if (_onlineImageCache.TryGetValue(guidOfRequestedURL, out var value))
				{
					TaleWorlds.Engine.Texture texture = TaleWorlds.Engine.Texture.CreateTextureFromPath(value);
					if (texture == null)
					{
						_onlineImageCache.Remove(guidOfRequestedURL);
						Debug.Print($"RETRYING TO DOWNLOAD: {_onlineSourceUrl} | RETRY COUNT: {_retryCount}", 0, Debug.DebugColor.Red);
						_requiresRetry = true;
					}
					else
					{
						OnTextureCreated(texture);
						_requiresRetry = false;
					}
				}
				else
				{
					Debug.Print($"RETRYING TO DOWNLOAD: {_onlineSourceUrl} | RETRY COUNT: {_retryCount}", 0, Debug.DebugColor.Red);
					_requiresRetry = true;
				}
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Error while trying to get image online: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\TextureProviders\\OnlineImageTextureProvider.cs", "RefreshOnlineImage", 115);
				Debug.Print($"RETRYING TO DOWNLOAD: {_onlineSourceUrl} | RETRY COUNT: {_retryCount}", 0, Debug.DebugColor.Red);
				_requiresRetry = true;
			}
		}
	}

	protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		if (_texture != null)
		{
			return new TaleWorlds.TwoDimension.Texture(new EngineTexture(_texture));
		}
		return null;
	}

	private void PopulateOnlineImageCache()
	{
		PlatformFilePath[] files = FileHelper.GetFiles(_onlineImageCacheFolderPath, "*.png", SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; i++)
		{
			PlatformFilePath value = files[i];
			string fileNameWithoutExtension = value.GetFileNameWithoutExtension();
			_onlineImageCache.Add(fileNameWithoutExtension, value);
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

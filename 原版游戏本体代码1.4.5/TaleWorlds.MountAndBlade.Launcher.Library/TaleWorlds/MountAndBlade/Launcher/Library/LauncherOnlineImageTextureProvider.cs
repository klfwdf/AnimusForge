using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using TaleWorlds.TwoDimension.Standalone;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherOnlineImageTextureProvider : TextureProvider
{
	private Dictionary<string, string> _onlineImageCache;

	private const string DataFolder = "Mount and Blade II Bannerlord/Online Images/";

	private readonly string _onlineImageCacheFolderPath;

	private Texture _texture;

	private bool _requiresRetry;

	private int _retryCount;

	private const int _maxRetryCount = 10;

	private string _onlineSourceUrl;

	public string OnlineSourceUrl
	{
		set
		{
			_onlineSourceUrl = value;
			if (!string.IsNullOrEmpty(_onlineSourceUrl))
			{
				RefreshOnlineImage();
			}
		}
	}

	public LauncherOnlineImageTextureProvider()
	{
		_onlineImageCache = new Dictionary<string, string>();
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		_onlineImageCacheFolderPath = Path.Combine(folderPath, "Mount and Blade II Bannerlord/Online Images/");
		if (!Directory.Exists(_onlineImageCacheFolderPath))
		{
			try
			{
				Directory.CreateDirectory(_onlineImageCacheFolderPath);
			}
			catch (Exception ex)
			{
				Debug.Print("Could not create directory for launcher images at \"" + _onlineImageCacheFolderPath + "\": " + ex.Message);
			}
		}
		PopulateOnlineImageCache();
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		if (_requiresRetry)
		{
			if (_retryCount > 10)
			{
				Debug.FailedAssert("Couldn't download " + _onlineSourceUrl, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Launcher.Library\\LauncherOnlineImageTextureProvider.cs", "Tick", 68);
				_requiresRetry = false;
			}
			else
			{
				_retryCount++;
				RefreshOnlineImage();
			}
		}
	}

	private async void RefreshOnlineImage()
	{
		if (_retryCount >= 10)
		{
			return;
		}
		try
		{
			_texture = null;
			string guidOfRequestedURL = ToGuid(_onlineSourceUrl).ToString();
			if (!_onlineImageCache.ContainsKey(guidOfRequestedURL))
			{
				using WebClient client = new WebClient();
				string pathOfTheDownloadedImage = _onlineImageCacheFolderPath + guidOfRequestedURL + ".png";
				Task downloadTask = client.DownloadFileTaskAsync(new Uri(_onlineSourceUrl), pathOfTheDownloadedImage);
				await downloadTask;
				if (downloadTask.Status == TaskStatus.RanToCompletion)
				{
					_onlineImageCache.Add(guidOfRequestedURL, pathOfTheDownloadedImage);
				}
			}
			if (_onlineImageCache.TryGetValue(guidOfRequestedURL, out var value))
			{
				OpenGLTexture openGLTexture = OpenGLTexture.FromFile(value);
				if (openGLTexture == null)
				{
					_onlineImageCache.Remove(guidOfRequestedURL);
					Debug.Print($"RETRYING TO DOWNLOAD: {_onlineSourceUrl} | RETRY COUNT: {_retryCount}", 0, Debug.DebugColor.Red);
					_requiresRetry = true;
				}
				else
				{
					openGLTexture.ClampToEdge = true;
					Texture texture = new Texture(openGLTexture);
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
			Debug.FailedAssert("Error while trying to get image online: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Launcher.Library\\LauncherOnlineImageTextureProvider.cs", "RefreshOnlineImage", 130);
		}
	}

	protected override Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		return _texture;
	}

	private void PopulateOnlineImageCache()
	{
		if (Directory.Exists(_onlineImageCacheFolderPath))
		{
			string[] array = new string[0];
			try
			{
				array = Directory.GetFiles(_onlineImageCacheFolderPath, "*.png");
			}
			catch (Exception ex)
			{
				Debug.Print("Could not load launcher images at \"" + _onlineImageCacheFolderPath + "\": " + ex.Message);
			}
			string[] array2 = array;
			foreach (string text in array2)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				_onlineImageCache.Add(fileNameWithoutExtension, text);
			}
		}
	}

	private static Guid ToGuid(string src)
	{
		if (!string.IsNullOrEmpty(src))
		{
			byte[] bytes = Encoding.UTF8.GetBytes(src);
			byte[] array = new SHA1CryptoServiceProvider().ComputeHash(bytes);
			Array.Resize(ref array, 16);
			return new Guid(array);
		}
		return Guid.Empty;
	}

	private void OnTextureCreated(Texture texture)
	{
		_texture = texture;
	}
}

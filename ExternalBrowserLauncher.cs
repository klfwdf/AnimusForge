using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace AnimusForge;

/// <summary>
/// 打开受信任的 HTTP(S) 链接；当 Windows 缺少默认 URL 关联时，使用有界的本机浏览器查找兜底。
/// </summary>
internal static class ExternalBrowserLauncher
{
	private static readonly string[] BrowserExecutableNames = new string[11]
	{
		"msedge.exe",
		"chrome.exe",
		"firefox.exe",
		"brave.exe",
		"vivaldi.exe",
		"opera.exe",
		"QQBrowser.exe",
		"SogouExplorer.exe",
		"360se.exe",
		"360chrome.exe",
		"Maxthon.exe"
	};

	private static readonly string[] ProgramFilesBrowserRelativePaths = new string[15]
	{
		"Microsoft\\Edge\\Application\\msedge.exe",
		"Google\\Chrome\\Application\\chrome.exe",
		"Google\\Chrome SxS\\Application\\chrome.exe",
		"Mozilla Firefox\\firefox.exe",
		"BraveSoftware\\Brave-Browser\\Application\\brave.exe",
		"Vivaldi\\Application\\vivaldi.exe",
		"Opera\\launcher.exe",
		"Opera GX\\launcher.exe",
		"Tencent\\QQBrowser\\QQBrowser.exe",
		"SogouExplorer\\SogouExplorer.exe",
		"360\\360se6\\Application\\360se.exe",
		"360\\360Chrome\\Chrome\\Application\\360chrome.exe",
		"Maxthon\\Application\\Maxthon.exe",
		"Chromium\\Application\\chrome.exe",
		"Microsoft\\Edge Beta\\Application\\msedge.exe"
	};

	private static readonly string[] LocalAppDataBrowserRelativePaths = new string[14]
	{
		"Microsoft\\Edge\\Application\\msedge.exe",
		"Google\\Chrome\\Application\\chrome.exe",
		"Google\\Chrome SxS\\Application\\chrome.exe",
		"Mozilla Firefox\\firefox.exe",
		"Programs\\Mozilla Firefox\\firefox.exe",
		"BraveSoftware\\Brave-Browser\\Application\\brave.exe",
		"Vivaldi\\Application\\vivaldi.exe",
		"Programs\\Opera\\launcher.exe",
		"Programs\\Opera GX\\launcher.exe",
		"Tencent\\QQBrowser\\QQBrowser.exe",
		"SogouExplorer\\SogouExplorer.exe",
		"360\\360se6\\Application\\360se.exe",
		"360\\360Chrome\\Chrome\\Application\\360chrome.exe",
		"Maxthon\\Application\\Maxthon.exe"
	};

	private static readonly object CachedBrowserPathLock = new object();
	private static readonly RegistryHive[] BrowserRegistryHives = new RegistryHive[2]
	{
		RegistryHive.CurrentUser,
		RegistryHive.LocalMachine
	};

	private static readonly RegistryView[] BrowserRegistryViews = Environment.Is64BitOperatingSystem ? new RegistryView[2]
	{
		RegistryView.Registry64,
		RegistryView.Registry32
	} : new RegistryView[1]
	{
		RegistryView.Default
	};

	private static readonly char[] UnsafeUrlArgumentCharacters = new char[3]
	{
		'\"',
		'\r',
		'\n'
	};

	private static string _cachedBrowserPath;

	internal static bool TryOpen(string url, out bool usedLocalBrowserFallback, out string failureMessage)
	{
		usedLocalBrowserFallback = false;
		failureMessage = "";
		if (!TryNormalizeHttpUrl(url, out string normalizedUrl))
		{
			failureMessage = "链接格式无效，只能打开 HTTP 或 HTTPS 页面。";
			return false;
		}
		try
		{
			using (Process process = Process.Start(new ProcessStartInfo(normalizedUrl)
			{
				UseShellExecute = true
			}))
			{
			}
			return true;
		}
		catch (Exception ex)
		{
			// 性能：注册表和磁盘候选只在默认关联启动失败后扫描；首次成功路径会缓存，后续点击无需重复扫描。
			foreach (string item in EnumerateBrowserPaths())
			{
				if (TryStartBrowser(item, normalizedUrl))
				{
					CacheBrowserPath(item);
					usedLocalBrowserFallback = true;
					return true;
				}
				ClearCachedBrowserPathIfMatching(item);
			}
			failureMessage = "系统默认浏览器无法启动，且未能启动注册表或常见安装位置中的本机浏览器。请安装或设置 Chrome、Edge、Firefox 等浏览器后重试。原因：" + ex.Message;
			return false;
		}
	}

	private static bool TryNormalizeHttpUrl(string url, out string normalizedUrl)
	{
		normalizedUrl = "";
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result) || (!string.Equals(result.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && !string.Equals(result.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		normalizedUrl = result.AbsoluteUri;
		// 参数会直接传给本机浏览器；拒绝命令行结构字符，避免旧框架 Arguments 字符串解析歧义。
		return normalizedUrl.IndexOfAny(UnsafeUrlArgumentCharacters) < 0;
	}

	private static IEnumerable<string> EnumerateBrowserPaths()
	{
		HashSet<string> yieldedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string cachedBrowserPath = GetCachedBrowserPath();
		if (TryYieldBrowserPath(cachedBrowserPath, yieldedPaths, out string item))
		{
			yield return item;
		}
		if (TryYieldBrowserPath(GetDefaultBrowserPath(), yieldedPaths, out item))
		{
			yield return item;
		}
		foreach (string registeredBrowserClientPath in EnumerateRegisteredBrowserClientPaths())
		{
			if (TryYieldBrowserPath(registeredBrowserClientPath, yieldedPaths, out item))
			{
				yield return item;
			}
		}
		foreach (string browserExecutableName in BrowserExecutableNames)
		{
			foreach (string registeredBrowserPath in EnumerateRegisteredAppPaths(browserExecutableName))
			{
				if (TryYieldBrowserPath(registeredBrowserPath, yieldedPaths, out item))
				{
					yield return item;
				}
			}
		}
		foreach (string knownBrowserPath in EnumerateKnownBrowserPaths())
		{
			if (TryYieldBrowserPath(knownBrowserPath, yieldedPaths, out item))
			{
				yield return item;
			}
		}
	}

	private static bool TryYieldBrowserPath(string candidatePath, HashSet<string> yieldedPaths, out string browserPath)
	{
		return TryNormalizeLocalExecutablePath(candidatePath, out browserPath) && File.Exists(browserPath) && yieldedPaths.Add(browserPath);
	}

	private static string GetCachedBrowserPath()
	{
		lock (CachedBrowserPathLock)
		{
			return _cachedBrowserPath;
		}
	}

	private static void CacheBrowserPath(string browserPath)
	{
		lock (CachedBrowserPathLock)
		{
			_cachedBrowserPath = browserPath;
		}
	}

	private static void ClearCachedBrowserPathIfMatching(string browserPath)
	{
		lock (CachedBrowserPathLock)
		{
			if (string.Equals(_cachedBrowserPath, browserPath, StringComparison.OrdinalIgnoreCase))
			{
				_cachedBrowserPath = "";
			}
		}
	}

	private static bool TryStartBrowser(string browserPath, string url)
	{
		try
		{
			using (Process process = Process.Start(new ProcessStartInfo(browserPath)
			{
				Arguments = QuoteArgument(url),
				CreateNoWindow = true,
				UseShellExecute = false
			}))
			{
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string QuoteArgument(string value)
	{
		return "\"" + value + "\"";
	}

	private static string GetDefaultBrowserPath()
	{
		string text = GetDefaultBrowserPathForScheme("https");
		return !string.IsNullOrWhiteSpace(text) ? text : GetDefaultBrowserPathForScheme("http");
	}

	private static string GetDefaultBrowserPathForScheme(string scheme)
	{
		foreach (RegistryView item in BrowserRegistryViews)
		{
			string userChoiceProgId = ReadRegistryString(RegistryHive.CurrentUser, item, "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\" + scheme + "\\UserChoice", "ProgId");
			if (string.IsNullOrWhiteSpace(userChoiceProgId))
			{
				continue;
			}
			string text = ReadRegistryString(RegistryHive.ClassesRoot, item, userChoiceProgId + "\\shell\\open\\command", null);
			string normalizedExecutablePath = NormalizeExecutablePath(text);
			if (!string.IsNullOrWhiteSpace(normalizedExecutablePath))
			{
				return normalizedExecutablePath;
			}
		}
		return "";
	}

	private static IEnumerable<string> EnumerateRegisteredAppPaths(string browserExecutableName)
	{
		foreach (RegistryHive item in BrowserRegistryHives)
		{
			foreach (RegistryView item2 in BrowserRegistryViews)
			{
				string text = ReadRegistryString(item, item2, "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + browserExecutableName, null);
				if (!string.IsNullOrWhiteSpace(text))
				{
					yield return text;
				}
			}
		}
	}

	private static IEnumerable<string> EnumerateRegisteredBrowserClientPaths()
	{
		foreach (RegistryHive item in BrowserRegistryHives)
		{
			foreach (RegistryView item2 in BrowserRegistryViews)
			{
				foreach (string registeredBrowserClientCommand in GetRegisteredBrowserClientCommands(item, item2))
				{
					yield return registeredBrowserClientCommand;
				}
			}
		}
	}

	private static IEnumerable<string> EnumerateKnownBrowserPaths()
	{
		foreach (string item in EnumeratePaths(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ProgramFilesBrowserRelativePaths))
		{
			yield return item;
		}
		foreach (string item2 in EnumeratePaths(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), ProgramFilesBrowserRelativePaths))
		{
			yield return item2;
		}
		foreach (string item3 in EnumeratePaths(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalAppDataBrowserRelativePaths))
		{
			yield return item3;
		}
	}

	private static IEnumerable<string> EnumeratePaths(string rootPath, IEnumerable<string> relativePaths)
	{
		if (string.IsNullOrWhiteSpace(rootPath))
		{
			yield break;
		}
		foreach (string relativePath in relativePaths)
		{
			string text = "";
			try
			{
				text = Path.Combine(rootPath, relativePath);
			}
			catch
			{
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				yield return text;
			}
		}
	}

	private static string[] GetRegisteredBrowserClientCommands(RegistryHive hive, RegistryView view)
	{
		List<string> list = new List<string>();
		try
		{
			using (RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, view))
			using (RegistryKey registryKey2 = registryKey.OpenSubKey("Software\\Clients\\StartMenuInternet", writable: false))
			{
				if (registryKey2 == null)
				{
					return list.ToArray();
				}
				int num = 0;
				foreach (string subKeyName in registryKey2.GetSubKeyNames())
				{
					// 性能与安全：注册表理论可被异常扩充，最多检查常见浏览器注册槽位的前 32 项。
					if (num++ >= 32)
					{
						break;
					}
					using (RegistryKey registryKey3 = registryKey2.OpenSubKey(subKeyName + "\\shell\\open\\command", writable: false))
					{
						string text = registryKey3?.GetValue(null) as string;
						if (!string.IsNullOrWhiteSpace(text))
						{
							list.Add(text);
						}
					}
				}
			}
		}
		catch
		{
		}
		return list.ToArray();
	}

	private static string ReadRegistryString(RegistryHive hive, RegistryView view, string subKeyPath, string valueName)
	{
		try
		{
			using (RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, view))
			using (RegistryKey registryKey2 = registryKey.OpenSubKey(subKeyPath, writable: false))
			{
				return registryKey2?.GetValue(valueName) as string ?? "";
			}
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizeExecutablePath(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		string text = Environment.ExpandEnvironmentVariables(value.Trim());
		if (text.Length > 1 && text[0] == '\"')
		{
			int num = text.IndexOf('\"', 1);
			return num > 1 ? text.Substring(1, num - 1).Trim() : "";
		}
		int num2 = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
		return num2 >= 0 ? text.Substring(0, num2 + 4).Trim() : text;
	}

	private static bool TryNormalizeLocalExecutablePath(string value, out string browserPath)
	{
		browserPath = NormalizeExecutablePath(value);
		if (string.IsNullOrWhiteSpace(browserPath) || browserPath.StartsWith("\\\\", StringComparison.Ordinal) || !Path.IsPathRooted(browserPath))
		{
			return false;
		}
		try
		{
			browserPath = Path.GetFullPath(browserPath);
			// 安全：映射网络盘不是本机浏览器候选，避免故障默认关联导致网络访问或卡顿。
			if (new DriveInfo(Path.GetPathRoot(browserPath)).DriveType == DriveType.Network)
			{
				browserPath = "";
				return false;
			}
			return string.Equals(Path.GetExtension(browserPath), ".exe", StringComparison.OrdinalIgnoreCase) && browserPath.Length >= 3 && browserPath[1] == ':' && (browserPath[2] == '\\' || browserPath[2] == '/');
		}
		catch
		{
			browserPath = "";
			return false;
		}
	}
}

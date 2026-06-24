using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge;

internal static class AnimusForgePlayerNotorietyUiSprites
{
	public const string BackgroundSpriteName = Category + "\\af_player_notoriety_background";
	public const string FillWorldSpriteName = Category + "\\af_player_notoriety_fill_world";
	public const string FillMaskSpriteName = Category + "\\af_player_notoriety_fill_mask";
	public const string FillCultureSpriteName = Category + "\\af_player_notoriety_fill_culture";
	public const string BarFrameSpriteName = Category + "\\af_player_notoriety_bar_frame";
	public const string BarFrameOverlaySpriteName = Category + "\\af_player_notoriety_bar_frame_overlay";
	public const string CulturePatchSpriteName = Category + "\\af_player_notoriety_culture_patch";
	public const string CenturyPatchSpriteName = Category + "\\af_player_notoriety_century_patch";

	private const string Source = "PlayerNotorietyUiSprites";
	private const string Prefix = "[AF-PLAYER-NOTORIETY-UI]";
	private const string Category = "af_player_notoriety";

	private static readonly Dictionary<string, string> FilesBySpriteName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		[BackgroundSpriteName] = "af_player_notoriety_background.png",
		[FillWorldSpriteName] = "af_player_notoriety_fill_world.png",
		[FillMaskSpriteName] = "af_player_notoriety_fill_mask.png",
		[FillCultureSpriteName] = "af_player_notoriety_fill_culture.png",
		[BarFrameSpriteName] = "af_player_notoriety_bar_frame.png",
		[BarFrameOverlaySpriteName] = "af_player_notoriety_bar_frame_overlay.png",
		[CulturePatchSpriteName] = "af_player_notoriety_culture_patch.png",
		[CenturyPatchSpriteName] = "af_player_notoriety_century_patch.png"
	};

	private static readonly Dictionary<string, BannerlordUiSprite> RuntimeSpritesByName = new Dictionary<string, BannerlordUiSprite>(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static bool _patched;
	private static bool _installLogged;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.player.notoriety.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		EnsureInstalled();
	}

	public static void EnsureInstalled()
	{
		TryInstallRuntimeSprites();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprites();
	}

	private static void TryPatch(Harmony harmony, string targetName, string postfixName)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(UIResourceManager), targetName);
			if (target == null)
			{
				LogOnce("patch-missing-" + targetName, "UIResourceManager." + targetName + " not found; player notoriety runtime sprites will run on demand.");
				return;
			}
			harmony.Patch(target, postfix: new HarmonyMethod(typeof(AnimusForgePlayerNotorietyUiSprites), postfixName));
		}
		catch (Exception ex)
		{
			LogOnce("patch-error-" + targetName, "Failed to patch UIResourceManager." + targetName + ": " + ex.Message);
		}
	}

	private static void TryInstallRuntimeSprites()
	{
		try
		{
			if (UIResourceManager.SpriteData == null)
			{
				return;
			}
			int installed = 0;
			foreach (KeyValuePair<string, string> pair in FilesBySpriteName)
			{
				if (TryInstallRuntimeSprite(pair.Key, pair.Value))
				{
					installed++;
				}
			}
			if (installed > 0 && !_installLogged)
			{
				_installLogged = true;
				Log("Runtime PNG sprites installed for player notoriety UI.");
			}
		}
		catch (Exception ex)
		{
			LogOnce("install-exception", "Runtime PNG sprite install failed: " + ex.Message);
		}
	}

	private static bool TryInstallRuntimeSprite(string spriteName, string fileName)
	{
		if (UIResourceManager.SpriteData.Sprites.TryGetValue(spriteName, out BannerlordUiSprite existing) && existing is RuntimeTextureSprite)
		{
			RuntimeSpritesByName[spriteName] = existing;
			return true;
		}
		if (!TryCreateSprite(spriteName, fileName, out BannerlordUiSprite sprite, out string failureReason))
		{
			LogOnce("create-" + spriteName, "Failed to load " + fileName + ": " + failureReason);
			return false;
		}
		UIResourceManager.SpriteData.Sprites[spriteName] = sprite;
		RuntimeSpritesByName[spriteName] = sprite;
		return true;
	}

	private static bool TryCreateSprite(string spriteName, string fileName, out BannerlordUiSprite sprite, out string failureReason)
	{
		sprite = null;
		string filePath = GetSpriteFilePath(fileName);
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			failureReason = "file not found at " + filePath;
			return false;
		}
		TryReadPngSize(filePath, out int pngWidth, out int pngHeight);
		BannerlordEngineTexture engineTexture = TryLoadEngineTexture(filePath, out failureReason);
		if (engineTexture == null)
		{
			return false;
		}
		try
		{
			engineTexture.Name = spriteName;
			engineTexture.SetTextureAsAlwaysValid();
			engineTexture.PreloadTexture(true);
		}
		catch
		{
		}
		int width = engineTexture.Width > 0 ? engineTexture.Width : (pngWidth > 0 ? pngWidth : 1024);
		int height = engineTexture.Height > 0 ? engineTexture.Height : (pngHeight > 0 ? pngHeight : 559);
		BannerlordUiTexture uiTexture = new BannerlordUiTexture(new EngineTexture(engineTexture));
		sprite = new RuntimeTextureSprite(spriteName, uiTexture, width, height);
		return true;
	}

	private static BannerlordEngineTexture TryLoadEngineTexture(string filePath, out string failureReason)
	{
		failureReason = "";
		try
		{
			byte[] bytes = File.ReadAllBytes(filePath);
			BannerlordEngineTexture texture = BannerlordEngineTexture.CreateFromMemory(bytes);
			if (texture != null)
			{
				return texture;
			}
		}
		catch (Exception ex)
		{
			failureReason = "CreateFromMemory: " + ex.Message;
		}
		try
		{
			BannerlordEngineTexture texture = BannerlordEngineTexture.LoadTextureFromPath(Path.GetFileName(filePath), Path.GetDirectoryName(filePath));
			if (texture != null)
			{
				failureReason = "";
				return texture;
			}
		}
		catch (Exception ex)
		{
			failureReason = string.IsNullOrWhiteSpace(failureReason) ? "LoadTextureFromPath: " + ex.Message : failureReason + "; LoadTextureFromPath: " + ex.Message;
		}
		if (string.IsNullOrWhiteSpace(failureReason))
		{
			failureReason = "native texture loader returned null";
		}
		return null;
	}

	private static string GetSpriteFilePath(string fileName)
	{
		string assemblyDir = Path.GetDirectoryName(typeof(SubModule).Assembly.Location) ?? "";
		string moduleRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", ".."));
		return Path.Combine(moduleRoot, "GUI", "SpriteParts", Category, fileName);
	}

	private static bool TryReadPngSize(string filePath, out int width, out int height)
	{
		width = 0;
		height = 0;
		try
		{
			byte[] header = new byte[24];
			using (FileStream stream = File.OpenRead(filePath))
			{
				if (stream.Read(header, 0, header.Length) != header.Length)
				{
					return false;
				}
			}
			if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47)
			{
				return false;
			}
			width = ReadBigEndianInt32(header, 16);
			height = ReadBigEndianInt32(header, 20);
			return width > 0 && height > 0;
		}
		catch
		{
			return false;
		}
	}

	private static int ReadBigEndianInt32(byte[] bytes, int offset)
	{
		return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
	}

	private static void LogOnce(string key, string message)
	{
		if (LoggedFailures.Add(key))
		{
			Log(message);
		}
	}

	private static void Log(string message)
	{
		Logger.Log(Source, Prefix + " " + message);
	}

	private sealed class RuntimeTextureSprite : BannerlordUiSprite
	{
		private readonly BannerlordUiTexture _texture;

		public RuntimeTextureSprite(string name, BannerlordUiTexture texture, int width, int height)
			: base(name, width, height, TaleWorlds.TwoDimension.SpriteNinePatchParameters.Empty)
		{
			_texture = texture;
		}

		public override BannerlordUiTexture Texture => _texture;

		public override Vec2 GetMinUvs()
		{
			return Vec2.Zero;
		}

		public override Vec2 GetMaxUvs()
		{
			return Vec2.One;
		}
	}
}

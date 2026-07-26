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

internal static class AnimusForgePlayerRpForgeUiSprites
{
	public const string BackgroundSpriteName = Category + "\\af_player_rp_forge_background";

	private const string Source = "PlayerRpForgeUiSprites";

	private const string Prefix = "[AF-PLAYER-RP-FORGE-UI]";

	private const string Category = "af_player_rp_forge";

	private const string BackgroundFileName = "af_player_rp_forge_background.png";

	private static readonly Dictionary<string, BannerlordUiSprite> RuntimeSpritesByName =
		new Dictionary<string, BannerlordUiSprite>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> LoggedFailures =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static bool _patched;

	private static bool _installLogged;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.player.rp.forge.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		EnsureInstalled();
	}

	public static void EnsureInstalled()
	{
		TryInstallRuntimeSprite();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprite();
	}

	private static void TryPatch(Harmony harmony, string targetName, string postfixName)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(UIResourceManager), targetName);
			if (target == null)
			{
				LogOnce(
					"patch-missing-" + targetName,
					"UIResourceManager." + targetName
						+ " not found; player RP forge runtime sprite will run on demand.");
				return;
			}
			harmony.Patch(
				target,
				postfix: new HarmonyMethod(typeof(AnimusForgePlayerRpForgeUiSprites), postfixName));
		}
		catch (Exception ex)
		{
			LogOnce(
				"patch-error-" + targetName,
				"Failed to patch UIResourceManager." + targetName + ": " + ex.Message);
		}
	}

	private static void TryInstallRuntimeSprite()
	{
		try
		{
			if (UIResourceManager.SpriteData == null)
			{
				return;
			}
			if (UIResourceManager.SpriteData.Sprites.TryGetValue(
				BackgroundSpriteName,
				out BannerlordUiSprite existing)
				&& existing is RuntimeTextureSprite)
			{
				RuntimeSpritesByName[BackgroundSpriteName] = existing;
				return;
			}
			if (RuntimeSpritesByName.TryGetValue(
				BackgroundSpriteName,
				out BannerlordUiSprite cached)
				&& cached != null)
			{
				UIResourceManager.SpriteData.Sprites[BackgroundSpriteName] = cached;
				return;
			}
			if (!TryCreateSprite(out BannerlordUiSprite sprite, out string failureReason))
			{
				LogOnce(
					"create-" + BackgroundSpriteName,
					"Failed to load " + BackgroundFileName + ": " + failureReason);
				return;
			}
			UIResourceManager.SpriteData.Sprites[BackgroundSpriteName] = sprite;
			RuntimeSpritesByName[BackgroundSpriteName] = sprite;
			if (!_installLogged)
			{
				_installLogged = true;
				Log("Runtime PNG sprite installed for player RP forge UI.");
			}
		}
		catch (Exception ex)
		{
			LogOnce("install-exception", "Runtime PNG sprite install failed: " + ex.Message);
		}
	}

	private static bool TryCreateSprite(
		out BannerlordUiSprite sprite,
		out string failureReason)
	{
		sprite = null;
		string filePath = GetSpriteFilePath();
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
			engineTexture.Name = BackgroundSpriteName;
			engineTexture.SetTextureAsAlwaysValid();
			engineTexture.PreloadTexture(true);
		}
		catch
		{
		}
		int width = engineTexture.Width > 0
			? engineTexture.Width
			: (pngWidth > 0 ? pngWidth : 1008);
		int height = engineTexture.Height > 0
			? engineTexture.Height
			: (pngHeight > 0 ? pngHeight : 559);
		BannerlordUiTexture uiTexture = new BannerlordUiTexture(new EngineTexture(engineTexture));
		sprite = new RuntimeTextureSprite(BackgroundSpriteName, uiTexture, width, height);
		return true;
	}

	private static BannerlordEngineTexture TryLoadEngineTexture(
		string filePath,
		out string failureReason)
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
			BannerlordEngineTexture texture = BannerlordEngineTexture.LoadTextureFromPath(
				Path.GetFileName(filePath),
				Path.GetDirectoryName(filePath));
			if (texture != null)
			{
				failureReason = "";
				return texture;
			}
		}
		catch (Exception ex)
		{
			failureReason = string.IsNullOrWhiteSpace(failureReason)
				? "LoadTextureFromPath: " + ex.Message
				: failureReason + "; LoadTextureFromPath: " + ex.Message;
		}
		if (string.IsNullOrWhiteSpace(failureReason))
		{
			failureReason = "native texture loader returned null";
		}
		return null;
	}

	private static string GetSpriteFilePath()
	{
		string moduleRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
		return Path.Combine(
			moduleRoot,
			"GUI",
			"SpriteParts",
			Category,
			BackgroundFileName);
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
			if (header[0] != 0x89
				|| header[1] != 0x50
				|| header[2] != 0x4E
				|| header[3] != 0x47)
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
		return (bytes[offset] << 24)
			| (bytes[offset + 1] << 16)
			| (bytes[offset + 2] << 8)
			| bytes[offset + 3];
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

		public RuntimeTextureSprite(
			string name,
			BannerlordUiTexture texture,
			int width,
			int height)
			: base(
				name,
				width,
				height,
				TaleWorlds.TwoDimension.SpriteNinePatchParameters.Empty)
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

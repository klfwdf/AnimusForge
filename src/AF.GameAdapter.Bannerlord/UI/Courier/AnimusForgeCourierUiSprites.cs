using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge;

internal static class AnimusForgeCourierUiSprites
{
	public const string ScrollSpriteName = Category + "\\af_courier_scroll";
	public const string ReplyNoticeIdentifier = "af_courier_reply_notice";
	public const string ReplyNoticeSpriteName = Category + "\\" + ReplyNoticeIdentifier;
	public const string ScrollBaseSpriteName = Category + "\\af_courier_scroll_base";
	private const string ScrollBaseFileName = "af_courier_scroll_base.png";
	public const string ButtonBandSpriteName = Category + "\\af_courier_button_band";
	private const string ButtonBandFileName = "af_courier_button_band.png";
	// Declared in AFCourierLetterBrushes.xml with a placeholder sprite; the runtime band PNG replaces it before the movie loads.
	private const string ButtonBrushName = "AFCourierLetter.Band.Button";
	private const string Source = "CourierUiSprites";
	private const string Prefix = "[AF-COURIER-UI]";
	private const string Category = "af_courier";
	private const string ScrollFileName = "af_courier_scroll.png";
	private const string ReplyNoticeFileName = "af_courier_reply_notice.png";
	private const string BrushName = "Map.Notification.Type.Circle.Image";

	private static readonly Dictionary<string, BannerlordUiSprite> RuntimeSpritesByName = new Dictionary<string, BannerlordUiSprite>(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static bool _patched;
	private static bool _installLogged;
	private static bool _brushLogged;

	public static void EnsureInstalled()
	{
		TryInstallRuntimeSprites();
	}

	// File names: af_courier_pattern_left_1.png / af_courier_pattern_right_3.png / af_courier_seal_vlandia.png
	public static string PatternSpriteName(string side, int index)
	{
		return Category + "\\af_courier_pattern_" + side + "_" + index;
	}

	public static string SealSpriteName(string sealKey)
	{
		return Category + "\\af_courier_seal_" + sealKey;
	}

	// Themed layers are loaded only for the preset actually shown, so unused presets never occupy texture memory.
	public static void EnsureThemeInstalled(CourierLetterTheme theme)
	{
		EnsureInstalled();
		if (theme == null || UIResourceManager.SpriteData == null)
		{
			return;
		}
		try
		{
			TryInstallRuntimeSprite(ScrollBaseSpriteName, ScrollBaseFileName);
			TryInstallRuntimeSprite(theme.LeftSpriteName, FileNameOf(theme.LeftSpriteName));
			TryInstallRuntimeSprite(theme.RightSpriteName, FileNameOf(theme.RightSpriteName));
			TryInstallRuntimeSprite(theme.SealSpriteName, FileNameOf(theme.SealSpriteName));
		}
		catch (Exception ex)
		{
			LogOnce("theme-" + theme.Id, "Theme sprite install failed for " + theme.Id + ": " + ex.Message);
		}
		TryApplyButtonBandSprite();
	}

	// Brush layers keep the Sprite object they were parsed with, so the band must be pushed into the brush itself.
	// Widgets clone their brush on load, which is why this runs before LoadMovie and again after every brush refresh.
	private static void TryApplyButtonBandSprite()
	{
		try
		{
			if (UIResourceManager.SpriteData == null || !TryInstallRuntimeSprite(ButtonBandSpriteName, ButtonBandFileName))
			{
				return;
			}
			Brush brush = UIResourceManager.BrushFactory?.GetBrush(ButtonBrushName);
			if (brush == null || !RuntimeSpritesByName.TryGetValue(ButtonBandSpriteName, out BannerlordUiSprite sprite))
			{
				LogOnce("band-brush-missing", "Brush " + ButtonBrushName + " not found; courier buttons keep the placeholder sprite.");
				return;
			}
			foreach (BrushLayer layer in brush.Layers)
			{
				if (layer != null)
				{
					layer.Sprite = sprite;
					// The placeholder tint would darken the band texture; the PNG already carries its own color and alpha.
					layer.Color = Color.White;
				}
			}
			foreach (Style style in brush.Styles)
			{
				if (style == null)
				{
					continue;
				}
				foreach (StyleLayer styleLayer in style.GetLayers())
				{
					if (styleLayer != null)
					{
						styleLayer.Sprite = sprite;
					}
				}
			}
		}
		catch (Exception ex)
		{
			LogOnce("band-brush-exception", "Failed to apply courier button band sprite: " + ex.Message);
		}
	}

	private static string FileNameOf(string spriteName)
	{
		return spriteName.Substring(Category.Length + 1) + ".png";
	}

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.courier.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		TryPatch(patcher, "RefreshBrushFactory", nameof(RefreshBrushFactoryPostfix));
		EnsureInstalledForNotificationUi();
	}

	public static void EnsureInstalledForNotificationUi()
	{
		TryInstallRuntimeSprites();
		TryApplyBrushLayerSprite();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprites();
	}

	public static void RefreshBrushFactoryPostfix()
	{
		TryInstallRuntimeSprites();
		TryApplyBrushLayerSprite();
		TryApplyButtonBandSprite();
	}

	private static void TryPatch(Harmony harmony, string targetName, string postfixName)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(UIResourceManager), targetName);
			if (target == null)
			{
				LogOnce("patch-missing-" + targetName, "UIResourceManager." + targetName + " not found; courier runtime sprite fallback will run on demand.");
				return;
			}
			harmony.Patch(target, postfix: new HarmonyMethod(typeof(AnimusForgeCourierUiSprites), postfixName));
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
			if (TryInstallRuntimeSprite(ScrollSpriteName, ScrollFileName))
			{
				installed++;
			}
			if (TryInstallRuntimeSprite(ReplyNoticeSpriteName, ReplyNoticeFileName))
			{
				installed++;
			}
			if (installed > 0 && !_installLogged)
			{
				_installLogged = true;
				Log("Runtime PNG sprites installed for courier letter UI.");
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

	private static void TryApplyBrushLayerSprite()
	{
		try
		{
			Brush brush = UIResourceManager.BrushFactory?.GetBrush(BrushName);
			if (brush == null || !RuntimeSpritesByName.TryGetValue(ReplyNoticeSpriteName, out BannerlordUiSprite sprite))
			{
				return;
			}
			if (!AnimusForgeRuntimeBrushSpriteGuard.TryApplyLayerStyle(brush, ReplyNoticeIdentifier, sprite, out string failureReason))
			{
				LogOnce("brush-apply-" + ReplyNoticeIdentifier, "Skipped courier reply brush layer apply: " + failureReason);
				return;
			}
			if (!_brushLogged)
			{
				_brushLogged = true;
				Log("Applied courier reply runtime PNG sprite to " + BrushName + ".");
			}
		}
		catch (Exception ex)
		{
			LogOnce("brush-exception", "Failed to apply courier reply runtime PNG sprite to brush layer: " + ex.Message);
		}
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
			// Native texture validity can be reported lazily while still rendering correctly later.
		}
		int width = engineTexture.Width > 0 ? engineTexture.Width : (pngWidth > 0 ? pngWidth : 1024);
		int height = engineTexture.Height > 0 ? engineTexture.Height : (pngHeight > 0 ? pngHeight : 640);
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
		string moduleRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
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

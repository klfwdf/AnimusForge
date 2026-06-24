using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge;

internal sealed class AnimusForgeWeeklyReportMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string EventId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeWeeklyReportMapNotification(string eventId, string titleText, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "点击查看当前周报。" : descriptionText))
	{
		EventId = (eventId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(titleText) ? "周报已生成" : titleText);
	}

	public override bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(EventId);
	}
}

internal sealed class AnimusForgeWeeklyReportMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeWeeklyReportMapNotificationItemVM(AnimusForgeWeeklyReportMapNotification data)
		: base(data)
	{
		AnimusForgeWeeklyReportUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = "af_weekly_report";
		_onInspect = delegate
		{
			if (MyBehavior.Instance?.OpenWeeklyReportNoticeFromMap(data.EventId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal static class AnimusForgeWeeklyReportUiSprites
{
	private const string Source = "WeeklyReportUiSprites";
	private const string Prefix = "[AF-WEEKLY-REPORT-UI]";
	private const string Category = "af_weekly_report";
	private const string LayerName = "af_weekly_report";
	private const string FileName = "af_weekly_report.png";
	private const string ChronicleCategory = "af_weekly_chronicle";
	private const string ChronicleFileName = "af_weekly_chronicle_clean.png";
	private const string ChronicleSpriteName = ChronicleCategory + "\\af_weekly_chronicle_clean";
	private const string BrushName = "Map.Notification.Type.Circle.Image";
	private static readonly string SpriteName = Category + "\\" + Path.GetFileNameWithoutExtension(FileName);
	private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static BannerlordUiSprite _runtimeSprite;
	private static bool _patched;
	private static bool _installLogged;
	private static bool _chronicleInstallLogged;
	private static bool _brushLogged;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.weeklyreport.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		TryPatch(patcher, "RefreshBrushFactory", nameof(RefreshBrushFactoryPostfix));
		EnsureInstalledForNotificationUi();
	}

	public static void EnsureInstalledForNotificationUi()
	{
		TryInstallRuntimeSprite();
		TryInstallChronicleRuntimeSprite();
		TryApplyBrushLayerSprite();
	}

	public static void EnsureInstalledForPopupUi()
	{
		TryInstallRuntimeSprite();
		TryInstallChronicleRuntimeSprite();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprite();
		TryInstallChronicleRuntimeSprite();
	}

	public static void RefreshBrushFactoryPostfix()
	{
		TryInstallRuntimeSprite();
		TryApplyBrushLayerSprite();
	}

	private static void TryPatch(Harmony harmony, string targetName, string postfixName)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(UIResourceManager), targetName);
			if (target == null)
			{
				LogOnce("patch-missing-" + targetName, "UIResourceManager." + targetName + " not found; runtime sprite fallback will only run when weekly notices are created.");
				return;
			}
			harmony.Patch(target, postfix: new HarmonyMethod(typeof(AnimusForgeWeeklyReportUiSprites), postfixName));
		}
		catch (Exception ex)
		{
			LogOnce("patch-error-" + targetName, "Failed to patch UIResourceManager." + targetName + ": " + ex.Message);
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
			if (UIResourceManager.SpriteData.Sprites.TryGetValue(SpriteName, out BannerlordUiSprite existing) && existing is RuntimeTextureSprite)
			{
				_runtimeSprite = existing;
				return;
			}
			if (!TryCreateSprite(out BannerlordUiSprite sprite, out string failureReason))
			{
				LogOnce("create-" + SpriteName, "Failed to load " + FileName + ": " + failureReason);
				return;
			}
			UIResourceManager.SpriteData.Sprites[SpriteName] = sprite;
			_runtimeSprite = sprite;
			if (!_installLogged)
			{
				_installLogged = true;
				Log("Runtime PNG sprite installed for weekly report map notifications.");
			}
		}
		catch (Exception ex)
		{
			LogOnce("install-exception", "Runtime PNG sprite install failed: " + ex.Message);
		}
	}

	private static void TryInstallChronicleRuntimeSprite()
	{
		try
		{
			if (UIResourceManager.SpriteData == null)
			{
				return;
			}
			if (UIResourceManager.SpriteData.Sprites.TryGetValue(ChronicleSpriteName, out BannerlordUiSprite existing) && existing is RuntimeTextureSprite)
			{
				return;
			}
			if (!TryCreateSprite(ChronicleSpriteName, ChronicleCategory, ChronicleFileName, 1450, 804, out BannerlordUiSprite sprite, out string failureReason))
			{
				LogOnce("create-" + ChronicleSpriteName, "Failed to load " + ChronicleFileName + ": " + failureReason);
				return;
			}
			UIResourceManager.SpriteData.Sprites[ChronicleSpriteName] = sprite;
			if (!_chronicleInstallLogged)
			{
				_chronicleInstallLogged = true;
				Log("Runtime PNG sprite installed for weekly chronicle popup.");
			}
		}
		catch (Exception ex)
		{
			LogOnce("chronicle-install-exception", "Runtime PNG chronicle sprite install failed: " + ex.Message);
		}
	}

	private static void TryApplyBrushLayerSprite()
	{
		try
		{
			Brush brush = UIResourceManager.BrushFactory?.GetBrush(BrushName);
			if (brush == null || _runtimeSprite == null)
			{
				return;
			}
			BrushLayer layer = EnsureBrushLayer(brush, _runtimeSprite);
			layer.Sprite = _runtimeSprite;
			EnsureBrushStyle(brush);
			if (!_brushLogged)
			{
				_brushLogged = true;
				Log("Applied weekly report runtime PNG sprite to " + BrushName + ".");
			}
		}
		catch (Exception ex)
		{
			LogOnce("brush-exception", "Failed to apply weekly report runtime PNG sprite to brush layer: " + ex.Message);
		}
	}

	private static BrushLayer EnsureBrushLayer(Brush brush, BannerlordUiSprite sprite)
	{
		BrushLayer layer = brush.GetLayer(LayerName);
		if (layer != null)
		{
			return layer;
		}
		layer = new BrushLayer
		{
			Name = LayerName,
			Sprite = sprite,
			Color = Color.White,
			ColorFactor = 1f,
			AlphaFactor = 1f,
			IsHidden = true
		};
		brush.AddLayer(layer);
		LogOnce("layer-created-" + LayerName, "Created runtime brush layer: " + LayerName);
		return layer;
	}

	private static void EnsureBrushStyle(Brush brush)
	{
		Style style = brush.GetStyle(LayerName);
		if (style == null)
		{
			style = new Style(brush.Layers)
			{
				DefaultStyle = brush.DefaultStyle,
				Name = LayerName
			};
			brush.AddStyle(style);
			LogOnce("style-created-" + LayerName, "Created runtime brush style: " + LayerName);
		}
		else if (style.DefaultStyle == null)
		{
			style.DefaultStyle = brush.DefaultStyle;
		}
		StyleLayer styleLayer = style.GetLayer(LayerName);
		if (styleLayer == null)
		{
			BrushLayer sourceLayer = brush.GetLayer(LayerName);
			if (sourceLayer == null)
			{
				return;
			}
			styleLayer = new StyleLayer(sourceLayer);
			style.AddLayer(styleLayer);
		}
		styleLayer.IsHidden = false;
	}

	private static bool TryCreateSprite(out BannerlordUiSprite sprite, out string failureReason)
	{
		return TryCreateSprite(SpriteName, Category, FileName, 256, 256, out sprite, out failureReason);
	}

	private static bool TryCreateSprite(string spriteName, string category, string fileName, int fallbackWidth, int fallbackHeight, out BannerlordUiSprite sprite, out string failureReason)
	{
		sprite = null;
		string filePath = GetSpriteFilePath(category, fileName);
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
		int width = engineTexture.Width > 0 ? engineTexture.Width : (pngWidth > 0 ? pngWidth : fallbackWidth);
		int height = engineTexture.Height > 0 ? engineTexture.Height : (pngHeight > 0 ? pngHeight : fallbackHeight);
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

	private static string GetSpriteFilePath(string category, string fileName)
	{
		string assemblyDir = Path.GetDirectoryName(typeof(SubModule).Assembly.Location) ?? "";
		string moduleRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", ".."));
		return Path.Combine(moduleRoot, "GUI", "SpriteParts", category, fileName);
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

internal sealed class AnimusForgeCourierReplyMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string SenderName { get; }

	public string ReplyText { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeCourierReplyMapNotification(string senderName, string replyText)
		: base(new TextObject("点击查看" + NormalizeSenderName(senderName) + "的回信。"))
	{
		SenderName = NormalizeSenderName(senderName);
		ReplyText = (replyText ?? "").Trim();
		_titleText = new TextObject("信使带回了回信");
	}

	public override bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(ReplyText);
	}

	internal bool OpenReply()
	{
		string body = string.IsNullOrWhiteSpace(ReplyText) ? "（无回信正文）" : ReplyText.Trim();
		try
		{
			if (CourierLetterReplyPopup.Show(SenderName, body))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			InformationManager.ShowInquiry(new InquiryData("信使带回了回信", SenderName + "写道：\n\n" + body, true, false, "ESC关闭", "", null, null), true);
			return true;
		}
		catch
		{
			InformationManager.DisplayMessage(new InformationMessage("信使带回了回信：" + body));
			return true;
		}
	}

	private static string NormalizeSenderName(string senderName)
	{
		string value = (senderName ?? "").Trim();
		return string.IsNullOrWhiteSpace(value) ? "NPC" : value;
	}
}

internal sealed class AnimusForgeCourierReplyMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeCourierReplyMapNotificationItemVM(AnimusForgeCourierReplyMapNotification data)
		: base(data)
	{
		AnimusForgeCourierUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = AnimusForgeCourierUiSprites.ReplyNoticeIdentifier;
		_onInspect = delegate
		{
			if (data.OpenReply())
			{
				ExecuteRemove();
			}
		};
	}
}

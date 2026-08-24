using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge;

internal enum AfVassalageType
{
	// Legacy first-version values. Keep numeric compatibility for old saves.
	Military = 0,
	Protectorate = 1,
	Tributary = 2,
	Garrison = 3,
	Vassal = 4
}

internal sealed class VassalageAgreement
{
	public string SuzerainKingdomId { get; set; } = "";

	public string VassalKingdomId { get; set; } = "";

	public AfVassalageType Type { get; set; }

	public int CreatedDay { get; set; }

	public string NegotiatedByHeroId { get; set; } = "";

	public bool EstablishedNoticeShown { get; set; }

	[JsonIgnore]
	public string AgreementId => BuildAgreementId(SuzerainKingdomId, VassalKingdomId);

	public static string BuildAgreementId(string suzerainKingdomId, string vassalKingdomId)
	{
		return ((suzerainKingdomId ?? "").Trim() + "->" + (vassalKingdomId ?? "").Trim()).Trim();
	}

	public bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(SuzerainKingdomId)
			&& !string.IsNullOrWhiteSpace(VassalKingdomId)
			&& !string.Equals(SuzerainKingdomId.Trim(), VassalKingdomId.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	public Kingdom ResolveSuzerain()
	{
		return VassalageBehavior.ResolveKingdomById(SuzerainKingdomId);
	}

	public Kingdom ResolveVassal()
	{
		return VassalageBehavior.ResolveKingdomById(VassalKingdomId);
	}
}

internal enum VassalPolicyExternalCommitResultKind
{
	Committed = 0,
	AlreadyCommitted = 1,
	Unchanged = 2,
	Conflict = 3,
	Unknown = 4
}

internal sealed class VassalPolicyExternalCommitPlan
{
	internal string TransactionId { get; set; } = string.Empty;
	internal string IdempotencyKey { get; set; } = string.Empty;
	internal string AgreementId { get; set; } = string.Empty;
	internal string VassalKingdomId { get; set; } = string.Empty;
	internal int IndependenceBefore { get; set; }
	internal int IndependenceExpected { get; set; }
	internal bool BreakawayExpected { get; set; }
	internal int PublicationCost { get; set; }
	internal int QualityDelta { get; set; }
}

internal sealed class VassalPolicyExternalCommitObservation
{
	internal bool Observable { get; set; }
	internal bool AgreementPresent { get; set; }
	internal bool AgreementMatches { get; set; }
	internal int IndependenceActual { get; set; }
	internal bool BreakawayActual { get; set; }
}

internal sealed class VassalPolicyExternalCommitResult
{
	internal VassalPolicyExternalCommitResultKind Kind { get; set; }
	internal VassalPolicyExternalCommitObservation Observation { get; set; }
	internal string Error { get; set; } = string.Empty;
}

internal sealed class TributaryPaymentNoticeRecord
{
	public string NoticeId { get; set; } = "";

	public string AgreementId { get; set; } = "";

	public string TributaryKingdomId { get; set; } = "";

	public string TributaryName { get; set; } = "";

	public int SettlementDay { get; set; }

	public float TributaryStrength { get; set; }

	public int PlayerTownCount { get; set; }

	public int PlayerCastleCount { get; set; }

	public int PlayerVillageCount { get; set; }

	public int TributaryTownCount { get; set; }

	public int TributaryCastleCount { get; set; }

	public int TributaryVillageCount { get; set; }

	public int TownProsperityGainPerFief { get; set; }

	public int TownFoodGainPerFief { get; set; }

	public int CastleProsperityGainPerFief { get; set; }

	public int CastleFoodGainPerFief { get; set; }

	public int VillageHearthGainPerFief { get; set; }

	public float PlayerProsperityGain { get; set; }

	public float PlayerFoodGain { get; set; }

	public float PlayerHearthGain { get; set; }

	public float PlayerTownProsperityGain { get; set; }

	public float PlayerTownFoodGain { get; set; }

	public float PlayerCastleProsperityGain { get; set; }

	public float PlayerCastleFoodGain { get; set; }

	public float PlayerVillageHearthGain { get; set; }

	public float PlannedPlayerProsperityGain { get; set; }

	public float PlannedPlayerFoodGain { get; set; }

	public float PlannedPlayerHearthGain { get; set; }

	public float PlannedPlayerTownProsperityGain { get; set; }

	public float PlannedPlayerTownFoodGain { get; set; }

	public float PlannedPlayerCastleProsperityGain { get; set; }

	public float PlannedPlayerCastleFoodGain { get; set; }

	public float PlannedPlayerVillageHearthGain { get; set; }

	public float ProsperityPaymentRatio { get; set; }

	public float FoodPaymentRatio { get; set; }

	public float HearthPaymentRatio { get; set; }

	public float TributaryProsperityLoss { get; set; }

	public float TributaryFoodLoss { get; set; }

	public float TributaryHearthLoss { get; set; }

	public float TributaryTownProsperityLoss { get; set; }

	public float TributaryTownFoodLoss { get; set; }

	public float TributaryCastleProsperityLoss { get; set; }

	public float TributaryCastleFoodLoss { get; set; }

	public float TributaryVillageHearthLoss { get; set; }

	public List<string> PlayerSettlementGainLines { get; set; } = new List<string>();

	public bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(NoticeId)
			&& !string.IsNullOrWhiteSpace(AgreementId)
			&& !string.IsNullOrWhiteSpace(TributaryKingdomId);
	}
}

internal sealed class VassalageInfoNoticeRecord
{
	public string NoticeId { get; set; } = "";

	public string Category { get; set; } = "";

	public string Title { get; set; } = "";

	public string Summary { get; set; } = "";

	public string Detail { get; set; } = "";

	public int CreatedDay { get; set; }

	public bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(NoticeId)
			&& !string.IsNullOrWhiteSpace(Title)
			&& (!string.IsNullOrWhiteSpace(Summary) || !string.IsNullOrWhiteSpace(Detail));
	}
}

public sealed class TerminalVassalageManagementData
{
	public string TitleText { get; set; } = "臣属国管理";

	public string DescriptionText { get; set; } = "";

	public List<TerminalVassalageSubjectData> Subjects { get; } = new List<TerminalVassalageSubjectData>();
}

public sealed class TerminalVassalageSubjectData
{
	public string AgreementId { get; set; } = "";

	public string VassalKingdomId { get; set; } = "";

	public string VassalName { get; set; } = "";

	public string TypeName { get; set; } = "";

	public string CreatedDateText { get; set; } = "";

	public string ElapsedDaysText { get; set; } = "";

	public string ObedienceText { get; set; } = "";

	public bool IsTributePaying { get; set; }

	public int TributeRecordCount { get; set; }

	public string EntryTitleText { get; set; } = "";

	public string EntryHintText { get; set; } = "";
}

public sealed class TerminalTributaryPaymentHistoryData
{
	public string TitleText { get; set; } = "贡赋记录";

	public string SubtitleText { get; set; } = "";

	public string EmptyStateText { get; set; } = "尚无贡赋入库记录。";

	public string CloseText { get; set; } = "返回臣属国管理";

	public List<TerminalTributaryPaymentRecordData> Records { get; } = new List<TerminalTributaryPaymentRecordData>();
}

public sealed class TerminalTributaryPaymentRecordData
{
	public string DateText { get; set; } = "";

	public string TributeValueText { get; set; } = "";

	public string PlayerGainSummaryText { get; set; } = "";

	public string PlayerSettlementGainText { get; set; } = "";

	public string TributaryCostText { get; set; } = "";
}

internal sealed class AnimusForgeVassalageEstablishedMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string AgreementId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeVassalageEstablishedMapNotification(string agreementId, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "使节呈上盖有王印的臣属条约。" : descriptionText))
	{
		AgreementId = (agreementId ?? "").Trim();
		_titleText = new TextObject("臣属条约签署");
	}

	public override bool IsValid()
	{
		return VassalageBehavior.Instance?.HasAgreement(AgreementId) == true;
	}
}

internal sealed class AnimusForgeVassalageEstablishedMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeVassalageEstablishedMapNotificationItemVM(AnimusForgeVassalageEstablishedMapNotification data)
		: base(data)
	{
		AnimusForgeVassalageUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = "af_vassalage_contract";
		_onInspect = delegate
		{
			if (VassalageBehavior.Instance?.OpenEstablishedNoticeFromMap(data.AgreementId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal sealed class AnimusForgeNpcTributaryVassalageMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string NoticeId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeNpcTributaryVassalageMapNotification(string noticeId, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "诸国使节送来朝贡条约的消息。" : descriptionText))
	{
		NoticeId = (noticeId ?? "").Trim();
		_titleText = new TextObject("诸国朝贡条约");
	}

	public override bool IsValid()
	{
		return VassalageBehavior.Instance?.HasPendingNpcTributaryVassalageNotice(NoticeId) == true;
	}
}

internal sealed class AnimusForgeNpcTributaryVassalageMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeNpcTributaryVassalageMapNotificationItemVM(AnimusForgeNpcTributaryVassalageMapNotification data)
		: base(data)
	{
		AnimusForgeVassalageUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = "af_npc_tributary_vassalage";
		_onInspect = delegate
		{
			if (VassalageBehavior.Instance?.OpenNpcTributaryVassalageNoticeFromMap(data.NoticeId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal sealed class AnimusForgeVassalageInfoMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string NoticeId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeVassalageInfoMapNotification(string noticeId, string titleText, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "边境使者送来一份臣属事务急报。" : descriptionText))
	{
		NoticeId = (noticeId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(titleText) ? "臣属事务急报" : titleText);
	}

	public override bool IsValid()
	{
		return VassalageBehavior.Instance?.HasPendingInfoNotice(NoticeId) == true;
	}
}

internal sealed class AnimusForgeVassalageInfoMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeVassalageInfoMapNotificationItemVM(AnimusForgeVassalageInfoMapNotification data)
		: base(data)
	{
		AnimusForgeVassalageUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = "af_vassalage_breach";
		_onInspect = delegate
		{
			if (VassalageBehavior.Instance?.OpenInfoNoticeFromMap(data.NoticeId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal sealed class AnimusForgeVassalageProtectionMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string NoticeId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeVassalageProtectionMapNotification(string noticeId, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "臣属国派来急使，请求宗主国裁断战争义务。" : descriptionText))
	{
		NoticeId = (noticeId ?? "").Trim();
		_titleText = new TextObject("臣属国求援");
	}

	public override bool IsValid()
	{
		return VassalageBehavior.Instance?.HasPendingProtectionNotice(NoticeId) == true;
	}
}

internal sealed class AnimusForgeVassalageProtectionMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeVassalageProtectionMapNotificationItemVM(AnimusForgeVassalageProtectionMapNotification data)
		: base(data)
	{
		AnimusForgeVassalageUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = "af_vassalage_protection";
		_onInspect = delegate
		{
			if (VassalageBehavior.Instance?.OpenProtectionNoticeFromMap(data.NoticeId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal sealed class AnimusForgeTributaryPaymentMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string NoticeId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeTributaryPaymentMapNotification(string noticeId, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "朝贡车队抵达宫廷，贡赋已经入库。" : descriptionText))
	{
		NoticeId = (noticeId ?? "").Trim();
		_titleText = new TextObject("贡赋入库");
	}

	public override bool IsValid()
	{
		return VassalageBehavior.Instance?.HasPendingTributaryPaymentNotice(NoticeId) == true;
	}
}

internal sealed class AnimusForgeTributaryPaymentMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeTributaryPaymentMapNotificationItemVM(AnimusForgeTributaryPaymentMapNotification data)
		: base(data)
	{
		AnimusForgeVassalageUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = "af_vassalage_tribute";
		_onInspect = delegate
		{
			if (VassalageBehavior.Instance?.OpenTributaryPaymentNoticeFromMap(data.NoticeId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal static class AnimusForgeVassalageUiSprites
{
	private const string Source = "VassalageUiSprites";
	private const string Prefix = "[AF-VASSALAGE-UI]";
	private const string Category = "af_vassalage_notifications";
	private const string BrushName = "Map.Notification.Type.Circle.Image";
	private static readonly VassalageUiSpriteInfo[] SpriteInfos =
	{
		new VassalageUiSpriteInfo("af_npc_tributary_vassalage", "af_npc_tributary_vassalage.png"),
		new VassalageUiSpriteInfo("af_vassalage_contract", "af_vassalage_contract.png"),
		new VassalageUiSpriteInfo("af_vassalage_breach", "af_vassalage_breach.png"),
		new VassalageUiSpriteInfo("af_vassalage_protection", "af_vassalage_protection.png"),
		new VassalageUiSpriteInfo("af_vassalage_tribute", "af_vassalage_tribute.png")
	};

	private static readonly Dictionary<string, BannerlordUiSprite> RuntimeSpritesByName = new Dictionary<string, BannerlordUiSprite>(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static bool _patched;
	private static bool _installLogged;
	private static bool _brushLogged;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.vassalage.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		TryPatch(patcher, "RefreshBrushFactory", nameof(RefreshBrushFactoryPostfix));
		EnsureInstalledForNotificationUi();
	}

	public static void EnsureInstalledForNotificationUi()
	{
		TryInstallRuntimeSprites();
		TryApplyBrushLayerSprites();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprites();
	}

	public static void RefreshBrushFactoryPostfix()
	{
		TryInstallRuntimeSprites();
		TryApplyBrushLayerSprites();
	}

	private static void TryPatch(Harmony harmony, string targetName, string postfixName)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(UIResourceManager), targetName);
			if (target == null)
			{
				LogOnce("patch-missing-" + targetName, "UIResourceManager." + targetName + " not found; runtime sprite fallback will only run when notices are created.");
				return;
			}
			harmony.Patch(target, postfix: new HarmonyMethod(typeof(AnimusForgeVassalageUiSprites), postfixName));
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
			foreach (VassalageUiSpriteInfo info in SpriteInfos)
			{
				if (UIResourceManager.SpriteData.Sprites.TryGetValue(info.SpriteName, out BannerlordUiSprite existing) && existing is RuntimeTextureSprite)
				{
					RuntimeSpritesByName[info.SpriteName] = existing;
					installed++;
					continue;
				}
				if (!TryCreateSprite(info, out BannerlordUiSprite sprite, out string failureReason))
				{
					LogOnce("create-" + info.SpriteName, "Failed to load " + info.FileName + ": " + failureReason);
					continue;
				}
				UIResourceManager.SpriteData.Sprites[info.SpriteName] = sprite;
				RuntimeSpritesByName[info.SpriteName] = sprite;
				installed++;
			}
			if (installed == SpriteInfos.Length && !_installLogged)
			{
				_installLogged = true;
				Log("Runtime PNG sprites installed for vassalage map notifications.");
			}
		}
		catch (Exception ex)
		{
			LogOnce("install-exception", "Runtime PNG sprite install failed: " + ex.Message);
		}
	}

	private static void TryApplyBrushLayerSprites()
	{
		try
		{
			Brush brush = UIResourceManager.BrushFactory?.GetBrush(BrushName);
			if (brush == null)
			{
				return;
			}
			int applied = 0;
			foreach (VassalageUiSpriteInfo info in SpriteInfos)
			{
				if (!RuntimeSpritesByName.TryGetValue(info.SpriteName, out BannerlordUiSprite sprite))
				{
					continue;
				}
				if (!AnimusForgeRuntimeBrushSpriteGuard.TryApplyLayerStyle(brush, info.LayerName, sprite, out string failureReason))
				{
					LogOnce("brush-apply-" + info.LayerName, "Skipped vassalage brush layer apply: " + failureReason);
					continue;
				}
				applied++;
			}
			if (applied > 0 && !_brushLogged)
			{
				_brushLogged = true;
				Log("Applied runtime PNG sprites to " + BrushName + ".");
			}
		}
		catch (Exception ex)
		{
			LogOnce("brush-exception", "Failed to apply runtime PNG sprites to brush layers: " + ex.Message);
		}
	}

	private static bool TryCreateSprite(VassalageUiSpriteInfo info, out BannerlordUiSprite sprite, out string failureReason)
	{
		sprite = null;
		string filePath = GetSpriteFilePath(info.FileName);
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
			engineTexture.Name = info.SpriteName;
			engineTexture.SetTextureAsAlwaysValid();
			engineTexture.PreloadTexture(true);
		}
		catch
		{
			// Some texture loaders report validity lazily. The sprite can still render if the native texture is valid later.
		}
		int width = engineTexture.Width > 0 ? engineTexture.Width : (pngWidth > 0 ? pngWidth : 256);
		int height = engineTexture.Height > 0 ? engineTexture.Height : (pngHeight > 0 ? pngHeight : 256);
		BannerlordUiTexture uiTexture = new BannerlordUiTexture(new EngineTexture(engineTexture));
		sprite = new RuntimeTextureSprite(info.SpriteName, uiTexture, width, height);
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

	private readonly struct VassalageUiSpriteInfo
	{
		public VassalageUiSpriteInfo(string layerName, string fileName)
		{
			LayerName = layerName;
			FileName = fileName;
			SpriteName = Category + "\\" + Path.GetFileNameWithoutExtension(fileName);
		}

		public readonly string LayerName;
		public readonly string FileName;
		public readonly string SpriteName;
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

internal sealed class VassalageBehavior : CampaignBehaviorBase
{
	private const string SaveKeyAgreements = "_afVassalageAgreements_v1";
	private const string SaveKeyPendingInfoNotice = "_afVassalagePendingInfoNotice_v1";
	private const string SaveKeyPendingProtection = "_afVassalagePendingProtection_v1";
	private const string SaveKeyPendingNpcTributaryVassalageNotice = "_afVassalagePendingNpcTributaryVassalageNotice_v1";
	private const string SaveKeyPendingDiplomacySync = "_afVassalagePendingDiplomacySync_v1";
	private const string SaveKeyGarrisonObedience = "_afVassalageGarrisonObedience_v1";
	private const string SaveKeyProtectedTributaryWars = "_afVassalageProtectedTributaryWars_v1";
	private const string SaveKeyTributaryPaymentLastSettlementDay = "_afVassalageTributaryPaymentLastSettlementDay_v1";
	private const string SaveKeyPendingTributaryPayment = "_afVassalagePendingTributaryPayment_v1";
	private const string SaveKeyTributaryPaymentHistory = "_afVassalageTributaryPaymentHistory_v1";
	private const int TributaryPaymentIntervalDays = 21;
	private const float TributaryProsperityLossRatio = 0.4f;
	private const float TributaryFoodLossRatio = 0.35f;
	private const float TributaryHearthLossRatio = 0.45f;
	private const float TributaryProsperityFloor = 100f;
	private const float TributaryHearthFloor = 10f;
	private const int MilitaryStabilityFloor = 55;
	private const int ProtectorateStabilityFloor = 60;
	private const int SubjectObedienceMinValue = 0;
	private const int SubjectObedienceMaxValue = 100;
	private const int InitialSubjectObedience = 70;
	private const int SubjectRulerRelationMinValue = -100;
	private const int SubjectRulerRelationMaxValue = 100;
	private const int SubjectBreakawayThresholdMinValue = 60;
	private const int SubjectBreakawayThresholdNeutralValue = 80;
	private const int SubjectBreakawayThresholdMaxValue = 100;
	private const int VassalPolicyQualityDeltaMinValue = -15;
	private const int VassalPolicyQualityDeltaMaxValue = 15;
	internal const int VassalPolicyPublicationCostMinimum = 5;
	internal const int VassalPolicyPublicationCostMaximumInclusive = 10;
	private const int GarrisonRefuseProtectionWeakDelta = -50;
	private const int GarrisonRefuseProtectionEqualDelta = -35;
	private const int GarrisonRefuseProtectionStrongDelta = -22;
	private const int GarrisonRefuseProtectionOverwhelmingDelta = -12;
	private const int GarrisonProtectionWeakDelta = 5;
	private const int GarrisonProtectionEqualDelta = 10;
	private const int GarrisonProtectionStrongDelta = 18;
	private const int GarrisonProtectionOverwhelmingDelta = 25;
	private const float GarrisonStrengthAdvantageWeak = -1500f;
	private const float GarrisonStrengthAdvantageEqual = 0f;
	private const float GarrisonStrengthAdvantageStrong = 2500f;
	private const float GarrisonStrengthAdvantageOverwhelming = 6000f;

	private struct TributaryPaymentTier
	{
		public int TownProsperity;
		public int TownFood;
		public int CastleProsperity;
		public int CastleFood;
		public int VillageHearth;
		public string TierName;
	}

	private sealed class TributaryPaymentTotals
	{
		public int TownCount;
		public int CastleCount;
		public int VillageCount;
		public float Prosperity;
		public float Food;
		public float Hearth;
		public float TownProsperity;
		public float TownFood;
		public float CastleProsperity;
		public float CastleFood;
		public float VillageHearth;
		public readonly List<string> Details = new List<string>();
		public readonly List<string> NoticeLines = new List<string>();
	}

	public static VassalageBehavior Instance { get; private set; }

	private readonly Dictionary<string, VassalageAgreement> _agreementsByVassalId = new Dictionary<string, VassalageAgreement>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _agreementStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _pendingInfoNotices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _pendingProtectionNotices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _pendingNpcTributaryVassalageNotices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _pendingTributaryPaymentNotices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _pendingDiplomacySyncs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _protectedTributaryWars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _tributaryPaymentLastSettlementDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _tributaryPaymentLastSettlementDayStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _tributaryPaymentHistory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _garrisonObedienceValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _garrisonObedienceStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _establishedNoticesShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _infoNoticesShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _protectionNoticesShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _protectionNoticesOpenedFromMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _npcTributaryVassalageNoticesShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _tributaryPaymentNoticesShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _subjectBreakawayChecksInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly object _noticePublishLock = new object();
	private long _nextNoticePublishRetryUtcTicks;
	private long _nextDiplomacySyncRetryUtcTicks;

	private MapNotificationView _registeredMapNotificationView;
	private bool _isApplyingVassalageDiplomacy;

	internal static bool IsApplyingVassalageDiplomacy => Instance?._isApplyingVassalageDiplomacy == true;
	internal static bool CanApplyVassalageDiplomacyNowForExternal => Instance?.CanApplyVassalageDiplomacyNow() == true;

	internal static List<Kingdom> GetPlayerDirectVassalKingdomsForExternal()
	{
		return Instance?.GetPlayerVassalAgreements()
			.Where(x => x != null && NormalizeVassalageType(x.Type) == AfVassalageType.Vassal)
			.Select(x => x.ResolveVassal())
			.Where(IsValidKingdom)
			.Distinct()
			.ToList() ?? new List<Kingdom>();
	}

	internal static bool TryGetDirectVassalIndependenceForExternal(string vassalKingdomId, out int independence)
	{
		independence = 0;
		return Instance?.TryGetDirectVassalIndependence(vassalKingdomId, out independence) == true;
	}

	internal static bool TryGetDirectVassalIndependenceStatusForExternal(string vassalKingdomId, out int independence, out int breakawayThreshold, out int rulerRelation, out string rulerName)
	{
		independence = 0;
		breakawayThreshold = CalculateSubjectBreakawayThreshold(0);
		rulerRelation = 0;
		rulerName = "无有效统治者";
		return Instance?.TryGetDirectVassalIndependenceStatus(vassalKingdomId, out independence, out breakawayThreshold, out rulerRelation, out rulerName) == true;
	}

	internal static bool TryApplyDirectVassalPolicyIndependenceForExternal(string vassalKingdomId, int publicationCost, int qualityDelta, string policyName, out int before, out int after, out bool brokeAway)
	{
		before = 0;
		after = 0;
		brokeAway = false;
		return Instance?.TryApplyDirectVassalPolicyIndependence(vassalKingdomId, publicationCost, qualityDelta, policyName, out before, out after, out brokeAway) == true;
	}

	internal static bool TryPrepareDirectVassalPolicyIndependenceForExternal(
		string transactionId,
		string vassalKingdomId,
		int publicationCost,
		int qualityDelta,
		out VassalPolicyExternalCommitPlan plan,
		out string error)
	{
		plan = null;
		error = string.Empty;
		return Instance?.TryPrepareDirectVassalPolicyIndependence(
			transactionId, vassalKingdomId, publicationCost, qualityDelta, out plan, out error) == true;
	}

	internal static VassalPolicyExternalCommitObservation ObserveDirectVassalPolicyIndependenceForExternal(
		VassalPolicyExternalCommitPlan plan)
	{
		return Instance?.ObserveDirectVassalPolicyIndependence(plan)
			?? new VassalPolicyExternalCommitObservation();
	}

	internal static VassalPolicyExternalCommitResult CommitDirectVassalPolicyIndependenceForExternal(
		VassalPolicyExternalCommitPlan plan,
		string policyName)
	{
		return Instance?.CommitDirectVassalPolicyIndependence(plan, policyName)
			?? new VassalPolicyExternalCommitResult
			{
				Kind = VassalPolicyExternalCommitResultKind.Unknown,
				Error = "VassalageBehavior is unavailable"
			};
	}

	internal static bool ShouldAllowCampaignLogNotification(LogEntry log)
	{
		return Instance?.ShouldAllowCampaignLogNotificationCore(log) ?? true;
	}

	public VassalageBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
		CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
		MBInformationManager.OnRemoveMapNotice -= OnMapNoticeRemoved;
		MBInformationManager.OnRemoveMapNotice += OnMapNoticeRemoved;
		VassalageDiagnosticLog.Event("behavior.register_events", new Dictionary<string, object>
		{
			["logPath"] = VassalageDiagnosticLog.GetDiagnosticLogPath()
		});
	}

	public void OnEngineTick()
	{
		if (!HasPendingNoticeForMap() && _pendingDiplomacySyncs.Count == 0)
		{
			return;
		}
		long ticks = DateTime.UtcNow.Ticks;
		if (ticks < _nextNoticePublishRetryUtcTicks)
		{
			return;
		}
		_nextNoticePublishRetryUtcTicks = ticks + TimeSpan.FromSeconds(1.0).Ticks;
		TryPublishPendingNotices();
		ProcessPendingDiplomacySyncs();
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			_agreementStorage.Clear();
			foreach (VassalageAgreement agreement in _agreementsByVassalId.Values.Where((VassalageAgreement x) => x != null && x.IsValid()))
			{
				_agreementStorage[agreement.AgreementId] = JsonConvert.SerializeObject(agreement);
			}
			Dictionary<string, string> agreementStore = CampaignSaveChunkHelper.FlattenStringDictionary(_agreementStorage);
			dataStore.SyncData(SaveKeyAgreements, ref agreementStore);
			Dictionary<string, string> pendingInfoNoticeStore = CampaignSaveChunkHelper.FlattenStringDictionary(_pendingInfoNotices);
			dataStore.SyncData(SaveKeyPendingInfoNotice, ref pendingInfoNoticeStore);
			Dictionary<string, string> pendingProtectionStore = CampaignSaveChunkHelper.FlattenStringDictionary(_pendingProtectionNotices);
			dataStore.SyncData(SaveKeyPendingProtection, ref pendingProtectionStore);
			Dictionary<string, string> pendingNpcTributaryVassalageNoticeStore = CampaignSaveChunkHelper.FlattenStringDictionary(_pendingNpcTributaryVassalageNotices);
			dataStore.SyncData(SaveKeyPendingNpcTributaryVassalageNotice, ref pendingNpcTributaryVassalageNoticeStore);
			Dictionary<string, string> pendingDiplomacyStore = CampaignSaveChunkHelper.FlattenStringDictionary(_pendingDiplomacySyncs);
			dataStore.SyncData(SaveKeyPendingDiplomacySync, ref pendingDiplomacyStore);
			Dictionary<string, string> protectedTributaryWarStore = CampaignSaveChunkHelper.FlattenStringDictionary(_protectedTributaryWars);
			dataStore.SyncData(SaveKeyProtectedTributaryWars, ref protectedTributaryWarStore);
			_tributaryPaymentLastSettlementDayStorage.Clear();
			foreach (KeyValuePair<string, int> item in _tributaryPaymentLastSettlementDays)
			{
				string key = (item.Key ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(key))
				{
					_tributaryPaymentLastSettlementDayStorage[key] = Math.Max(0, item.Value).ToString(CultureInfo.InvariantCulture);
				}
			}
			Dictionary<string, string> tributaryPaymentDayStore = CampaignSaveChunkHelper.FlattenStringDictionary(_tributaryPaymentLastSettlementDayStorage);
			dataStore.SyncData(SaveKeyTributaryPaymentLastSettlementDay, ref tributaryPaymentDayStore);
			Dictionary<string, string> pendingTributaryPaymentStore = CampaignSaveChunkHelper.FlattenStringDictionary(_pendingTributaryPaymentNotices);
			dataStore.SyncData(SaveKeyPendingTributaryPayment, ref pendingTributaryPaymentStore);
			Dictionary<string, string> tributaryPaymentHistoryStore = CampaignSaveChunkHelper.FlattenStringDictionary(_tributaryPaymentHistory);
			dataStore.SyncData(SaveKeyTributaryPaymentHistory, ref tributaryPaymentHistoryStore);
			_garrisonObedienceStorage.Clear();
			foreach (KeyValuePair<string, int> item in _garrisonObedienceValues)
			{
				string key = (item.Key ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(key))
				{
					_garrisonObedienceStorage[key] = ClampSubjectObedienceValue(item.Value).ToString(CultureInfo.InvariantCulture);
				}
			}
			Dictionary<string, string> obedienceStore = CampaignSaveChunkHelper.FlattenStringDictionary(_garrisonObedienceStorage);
			dataStore.SyncData(SaveKeyGarrisonObedience, ref obedienceStore);
			VassalageDiagnosticLog.Event("save.sync.write", new Dictionary<string, object>
			{
				["agreementCount"] = _agreementStorage.Count,
				["pendingInfoNoticeCount"] = _pendingInfoNotices.Count,
				["pendingProtectionCount"] = _pendingProtectionNotices.Count,
				["pendingNpcTributaryVassalageNoticeCount"] = _pendingNpcTributaryVassalageNotices.Count,
				["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count,
				["tributaryPaymentLastSettlementDayCount"] = _tributaryPaymentLastSettlementDays.Count,
				["pendingTributaryPaymentCount"] = _pendingTributaryPaymentNotices.Count,
				["tributaryPaymentHistoryCount"] = _tributaryPaymentHistory.Count,
				["garrisonObedienceCount"] = _garrisonObedienceStorage.Count,
				["saveKeyAgreements"] = SaveKeyAgreements,
				["saveKeyPendingInfoNotice"] = SaveKeyPendingInfoNotice,
				["saveKeyPendingNpcTributaryVassalageNotice"] = SaveKeyPendingNpcTributaryVassalageNotice,
				["saveKeyPendingDiplomacySync"] = SaveKeyPendingDiplomacySync,
				["saveKeyProtectedTributaryWars"] = SaveKeyProtectedTributaryWars,
				["saveKeyTributaryPaymentLastSettlementDay"] = SaveKeyTributaryPaymentLastSettlementDay,
				["saveKeyPendingTributaryPayment"] = SaveKeyPendingTributaryPayment,
				["saveKeyTributaryPaymentHistory"] = SaveKeyTributaryPaymentHistory,
				["saveKeyGarrisonObedience"] = SaveKeyGarrisonObedience
			});
			return;
		}
		_agreementsByVassalId.Clear();
		_agreementStorage.Clear();
		Dictionary<string, string> storedAgreements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyAgreements, ref storedAgreements);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedAgreements, "VassalageAgreement"))
		{
			try
			{
				VassalageAgreement agreement = JsonConvert.DeserializeObject<VassalageAgreement>(item.Value ?? "");
				if (agreement != null && agreement.IsValid())
				{
					_agreementsByVassalId[agreement.VassalKingdomId.Trim()] = agreement;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Vassalage", "[WARN] load agreement failed key=" + (item.Key ?? "") + ": " + ex.Message);
			}
		}
		_pendingInfoNotices.Clear();
		Dictionary<string, string> storedInfoNotices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPendingInfoNotice, ref storedInfoNotices);
		foreach (KeyValuePair<string, string> item2 in CampaignSaveChunkHelper.RestoreStringDictionary(storedInfoNotices, "VassalageInfoNotice"))
		{
			string key = (item2.Key ?? "").Trim();
			string value = (item2.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_pendingInfoNotices[key] = value;
			}
		}
		_pendingProtectionNotices.Clear();
		Dictionary<string, string> storedProtection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPendingProtection, ref storedProtection);
		foreach (KeyValuePair<string, string> item3 in CampaignSaveChunkHelper.RestoreStringDictionary(storedProtection, "VassalageProtection"))
		{
			string key = (item3.Key ?? "").Trim();
			string value = (item3.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_pendingProtectionNotices[key] = value;
			}
		}
		_pendingNpcTributaryVassalageNotices.Clear();
		Dictionary<string, string> storedNpcTributaryVassalageNotices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPendingNpcTributaryVassalageNotice, ref storedNpcTributaryVassalageNotices);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedNpcTributaryVassalageNotices, "NpcTributaryVassalageNotice"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_pendingNpcTributaryVassalageNotices[key] = value;
			}
		}
		_pendingDiplomacySyncs.Clear();
		Dictionary<string, string> storedPendingDiplomacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPendingDiplomacySync, ref storedPendingDiplomacy);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedPendingDiplomacy, "VassalageDiplomacySync"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_pendingDiplomacySyncs[key] = value;
			}
		}
		_protectedTributaryWars.Clear();
		Dictionary<string, string> storedProtectedTributaryWars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyProtectedTributaryWars, ref storedProtectedTributaryWars);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedProtectedTributaryWars, "VassalageProtectedTributaryWar"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_protectedTributaryWars[key] = value;
			}
		}
		_tributaryPaymentLastSettlementDays.Clear();
		_tributaryPaymentLastSettlementDayStorage.Clear();
		Dictionary<string, string> storedTributaryPaymentDays = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyTributaryPaymentLastSettlementDay, ref storedTributaryPaymentDays);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedTributaryPaymentDays, "VassalageTributaryPaymentDay"))
		{
			string key = (item.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && int.TryParse((item.Value ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
			{
				_tributaryPaymentLastSettlementDays[key] = Math.Max(0, value);
			}
		}
		_pendingTributaryPaymentNotices.Clear();
		Dictionary<string, string> storedPendingTributaryPayments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPendingTributaryPayment, ref storedPendingTributaryPayments);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedPendingTributaryPayments, "VassalageTributaryPaymentNotice"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_pendingTributaryPaymentNotices[key] = value;
			}
		}
		_tributaryPaymentHistory.Clear();
		Dictionary<string, string> storedTributaryPaymentHistory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyTributaryPaymentHistory, ref storedTributaryPaymentHistory);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedTributaryPaymentHistory, "VassalageTributaryPaymentHistory"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				_tributaryPaymentHistory[key] = value;
			}
		}
		_garrisonObedienceValues.Clear();
		_garrisonObedienceStorage.Clear();
		Dictionary<string, string> storedObedience = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyGarrisonObedience, ref storedObedience);
		foreach (KeyValuePair<string, string> item4 in CampaignSaveChunkHelper.RestoreStringDictionary(storedObedience, "VassalageObedience"))
		{
			string key = (item4.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && int.TryParse((item4.Value ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
			{
				_garrisonObedienceValues[key] = ClampSubjectObedienceValue(value);
			}
		}
		VassalageDiagnosticLog.Event("save.sync.read", new Dictionary<string, object>
		{
			["agreementCount"] = _agreementsByVassalId.Count,
			["pendingInfoNoticeCount"] = _pendingInfoNotices.Count,
			["pendingProtectionCount"] = _pendingProtectionNotices.Count,
			["pendingNpcTributaryVassalageNoticeCount"] = _pendingNpcTributaryVassalageNotices.Count,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["protectedTributaryWarCount"] = _protectedTributaryWars.Count,
			["tributaryPaymentLastSettlementDayCount"] = _tributaryPaymentLastSettlementDays.Count,
			["pendingTributaryPaymentCount"] = _pendingTributaryPaymentNotices.Count,
			["tributaryPaymentHistoryCount"] = _tributaryPaymentHistory.Count,
			["garrisonObedienceCount"] = _garrisonObedienceValues.Count,
			["agreementIds"] = _agreementsByVassalId.Values.Select((VassalageAgreement x) => x?.AgreementId ?? "").ToList()
		});
	}

	public bool TryApplyVassalageAction(Hero negotiatedWith, string actionToken, string typeToken, string kingdomToken, out string statusText)
	{
		statusText = "";
		string action = (actionToken ?? "").Trim();
		VassalageDiagnosticLog.Event("action.apply.start", new Dictionary<string, object>
		{
			["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith),
			["actionToken"] = actionToken ?? "",
			["typeToken"] = typeToken ?? "",
			["kingdomToken"] = kingdomToken ?? ""
		});
		if (!string.Equals(action, "SUBMIT", StringComparison.OrdinalIgnoreCase))
		{
			statusText = "臣属条约未执行：当前谈判结果不属于可签署的臣属条约。";
			VassalageDiagnosticLog.Event("action.apply.reject", new Dictionary<string, object>
			{
				["reason"] = "unsupported_action",
				["statusText"] = statusText,
				["actionToken"] = action
			});
			return false;
		}
		if (!TryParseVassalageType(typeToken, out var type))
		{
			statusText = "臣属条约未执行：未能识别约定的条约类型。";
			VassalageDiagnosticLog.Event("action.apply.reject", new Dictionary<string, object>
			{
				["reason"] = "invalid_type",
				["statusText"] = statusText,
				["typeToken"] = typeToken ?? ""
			});
			return false;
		}
		Kingdom targetKingdom = ResolveKingdomByToken(kingdomToken, negotiatedWith);
		VassalageDiagnosticLog.Event("action.apply.resolved", new Dictionary<string, object>
		{
			["type"] = type,
			["normalizedType"] = NormalizeVassalageType(type),
			["kingdomToken"] = kingdomToken ?? "",
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith)
		});
		bool result = TryCreatePlayerVassalage(negotiatedWith, targetKingdom, type, out statusText);
		VassalageDiagnosticLog.Event("action.apply.done", new Dictionary<string, object>
		{
			["ok"] = result,
			["type"] = type,
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["statusText"] = statusText
		});
		return result;
	}

	public bool HasAgreement(string agreementId)
	{
		string id = (agreementId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(id) && _agreementsByVassalId.Values.Any((VassalageAgreement x) => x != null && string.Equals(x.AgreementId, id, StringComparison.OrdinalIgnoreCase));
	}

	public bool HasPendingInfoNotice(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(key) && _pendingInfoNotices.ContainsKey(key) && TryResolvePendingInfoNotice(key, out var _);
	}

	public bool HasPendingProtectionNotice(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(key) && _pendingProtectionNotices.ContainsKey(key) && TryResolvePendingProtectionNotice(key, out var _, out var _, out var _);
	}

	public bool HasPendingNpcTributaryVassalageNotice(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(key) && _pendingNpcTributaryVassalageNotices.ContainsKey(key) && TryResolvePendingNpcTributaryVassalageNotice(key, out var _);
	}

	public bool HasPendingTributaryPaymentNotice(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(key) && _pendingTributaryPaymentNotices.ContainsKey(key) && TryResolvePendingTributaryPaymentNotice(key, out var _);
	}

	public bool OpenEstablishedNoticeFromMap(string agreementId)
	{
		VassalageAgreement agreement = FindAgreementById(agreementId);
		if (agreement == null)
		{
			return true;
		}
		MarkEstablishedNoticeShown(agreement.AgreementId);
		string text = BuildEstablishedNoticeDetail(agreement);
		VassalageDiagnosticLog.Event("notice.open_established", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["createdDay"] = agreement.CreatedDay,
			["campaignDate"] = FormatCampaignDate(agreement.CreatedDay),
			["textLen"] = text?.Length ?? 0
		});
		InformationManager.ShowInquiry(new InquiryData("臣属条约签署", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "我知道了", "", null, null));
		return true;
	}

	public bool OpenInfoNoticeFromMap(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		if (!TryResolvePendingInfoNotice(key, out var record))
		{
			RemovePendingInfoNotice(key);
			return true;
		}
		RemovePendingInfoNotice(key);
		string title = string.IsNullOrWhiteSpace(record.Title) ? "臣属事务急报" : record.Title.Trim();
		string detail = string.IsNullOrWhiteSpace(record.Detail) ? record.Summary : record.Detail;
		VassalageDiagnosticLog.Event("notice.open_info", new Dictionary<string, object>
		{
			["noticeId"] = key,
			["category"] = record.Category ?? "",
			["title"] = title,
			["createdDay"] = record.CreatedDay,
			["textLen"] = detail?.Length ?? 0
		});
		InformationManager.ShowInquiry(new InquiryData(title, detail, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "我知道了", "", null, null));
		return true;
	}

	public bool OpenNpcTributaryVassalageNoticeFromMap(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		if (!TryResolvePendingNpcTributaryVassalageNotice(key, out var agreement))
		{
			RemovePendingNpcTributaryVassalageNotice(key);
			return true;
		}
		RemovePendingNpcTributaryVassalageNotice(key);
		string text = BuildEstablishedNoticeDetail(agreement);
		NpcTributeVassalageDiagnosticLog.Event("notice_open", new Dictionary<string, object>
		{
			["noticeId"] = key,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["createdDay"] = agreement.CreatedDay,
			["campaignDate"] = FormatCampaignDate(agreement.CreatedDay),
			["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(agreement.ResolveVassal()),
			["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(agreement.ResolveSuzerain()),
			["textLen"] = text?.Length ?? 0
		});
		InformationManager.ShowInquiry(new InquiryData("诸国朝贡条约", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "我知道了", "", null, null));
		return true;
	}

	public bool OpenTributaryPaymentNoticeFromMap(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		if (!TryResolvePendingTributaryPaymentNotice(key, out var record))
		{
			RemovePendingTributaryPaymentNotice(key);
			return true;
		}
		RemovePendingTributaryPaymentNotice(key);
		string detail = BuildTributaryPaymentNoticeDetail(record);
		VassalageDiagnosticLog.Event("tributary_payment.notice_open", new Dictionary<string, object>
		{
			["noticeId"] = key,
			["agreementId"] = record.AgreementId,
			["tributaryKingdomId"] = record.TributaryKingdomId,
			["settlementDay"] = record.SettlementDay,
			["plannedPlayerProsperityGain"] = record.PlannedPlayerProsperityGain,
			["plannedPlayerFoodGain"] = record.PlannedPlayerFoodGain,
			["plannedPlayerHearthGain"] = record.PlannedPlayerHearthGain,
			["prosperityPaymentRatio"] = record.ProsperityPaymentRatio,
			["foodPaymentRatio"] = record.FoodPaymentRatio,
			["hearthPaymentRatio"] = record.HearthPaymentRatio,
			["playerProsperityGain"] = record.PlayerProsperityGain,
			["playerFoodGain"] = record.PlayerFoodGain,
			["playerHearthGain"] = record.PlayerHearthGain,
			["tributaryProsperityLoss"] = record.TributaryProsperityLoss,
			["tributaryFoodLoss"] = record.TributaryFoodLoss,
			["tributaryHearthLoss"] = record.TributaryHearthLoss
		});
		InformationManager.ShowInquiry(new InquiryData("贡赋入库", detail, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "我知道了", "", null, null));
		return true;
	}

	public bool OpenProtectionNoticeFromMap(string noticeId)
	{
		string key = (noticeId ?? "").Trim();
		if (!TryResolvePendingProtectionNotice(key, out var agreement, out var vassal, out var enemy))
		{
			VassalageDiagnosticLog.Event("notice.open_protection.invalid", new Dictionary<string, object>
			{
				["noticeId"] = key
			});
			RemovePendingProtectionNotice(key);
			return true;
		}
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		_protectionNoticesOpenedFromMap.Add(key);
		VassalageDiagnosticLog.Event("notice.open_protection", new Dictionary<string, object>
		{
			["noticeId"] = key,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = type,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
		});
		string subjectText = BuildPlayerSubjectWarNoticeName(vassal, type);
		string attackerText = BuildWarNoticeKingdomName(enemy);
		if (type == AfVassalageType.Vassal)
		{
			string message = attackerText + "已经向" + subjectText
				+ "宣战。附庸国处于宗主国的直接保护之下；宫廷已按条约自动介入战争。";
			InformationManager.ShowInquiry(new InquiryData("附庸国遭到宣战", message, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "我知道了", "", delegate
			{
				CompleteProtectionNoticeAcknowledgement(key, "vassal_notice_acknowledged");
			}, null));
			return true;
		}
		if (type == AfVassalageType.Tributary)
		{
			string message = subjectText + "遭到" + attackerText
				+ "宣战。朝贡国以贡赋换取宗主庇护，但不承担出兵义务。\n\n是否履行庇护，向进攻者宣战？若拒绝，朝贡条约将立即终止。";
			InformationManager.ShowInquiry(new InquiryData("朝贡国请求庇护", message, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "履行庇护", "拒绝庇护", delegate
			{
				CompleteProtectionNoticeDecision(key, true, "tributary_protection_accepted");
			}, delegate
			{
				CompleteProtectionNoticeDecision(key, false, "tributary_protection_refused");
			}));
			return true;
		}
		string garrisonMessage = subjectText + "遭到" + attackerText
			+ "宣战。卫戍国是宗主国的军事屏障，承担随军作战义务，也期待宗主出兵保护。\n\n是否保护卫戍国？若拒绝，忠诚度将大幅下降；过低时将脱离臣属关系。";
		InformationManager.ShowInquiry(new InquiryData("卫戍国请求保护", garrisonMessage, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "出兵保护", "拒绝出兵", delegate
		{
			CompleteProtectionNoticeDecision(key, true, "garrison_protection_accepted");
		}, delegate
		{
			CompleteProtectionNoticeDecision(key, false, "garrison_protection_refused");
		}));
		return true;
	}

	public int GetPlayerVassalStabilityFloor(Kingdom kingdom)
	{
		return 0;
	}

	public bool IsPlayerVassalKingdom(Kingdom kingdom)
	{
		return GetPlayerVassalAgreement(kingdom) != null;
	}

	internal bool TryGetPlayerVassalageType(Kingdom kingdom, out AfVassalageType type)
	{
		VassalageAgreement agreement = GetPlayerVassalAgreement(kingdom);
		if (agreement == null)
		{
			type = AfVassalageType.Tributary;
			return false;
		}
		type = NormalizeVassalageType(agreement.Type);
		return true;
	}

	public bool IsKingdomSuzerainOfPlayerForDiagnostics(Kingdom kingdom)
	{
		try
		{
			string suzerainId = (kingdom?.StringId ?? "").Trim();
			Kingdom playerKingdom = GetPlayerKingdom();
			string playerId = (playerKingdom?.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(suzerainId) || string.IsNullOrWhiteSpace(playerId))
			{
				return false;
			}
			if (!_agreementsByVassalId.TryGetValue(playerId, out var agreement) || agreement == null)
			{
				return false;
			}
			return string.Equals((agreement.SuzerainKingdomId ?? "").Trim(), suzerainId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public bool IsMilitaryPlayerVassalKingdom(Kingdom kingdom)
	{
		AfVassalageType type = NormalizeVassalageType(GetPlayerVassalAgreement(kingdom)?.Type ?? AfVassalageType.Tributary);
		return type == AfVassalageType.Garrison || type == AfVassalageType.Vassal;
	}

	public static string BuildKingdomVassalageRelationPromptLineForExternal(Kingdom observerKingdom, Kingdom counterpartKingdom)
	{
		try
		{
			return BuildKingdomVassalageRelationPromptLineForExternal(observerKingdom, counterpartKingdom, "面前此人所在王国");
		}
		catch
		{
			return "";
		}
	}

	public static string BuildKingdomVassalageRelationPromptLineForExternal(Kingdom observerKingdom, Kingdom counterpartKingdom, string counterpartKingdomLabel)
	{
		try
		{
			return Instance?.BuildKingdomVassalageRelationPromptLine(observerKingdom, counterpartKingdom, counterpartKingdomLabel) ?? "";
		}
		catch
		{
			return "";
		}
	}

	private string BuildKingdomVassalageRelationPromptLine(Kingdom observerKingdom, Kingdom counterpartKingdom, string counterpartKingdomLabel)
	{
		if (!IsValidKingdom(observerKingdom) || !IsValidKingdom(counterpartKingdom))
		{
			return "";
		}
		string counterpartLabel = (counterpartKingdomLabel ?? "").Trim();
		if (string.IsNullOrWhiteSpace(counterpartLabel))
		{
			counterpartLabel = "对方所在王国";
		}
		string observerId = (observerKingdom.StringId ?? "").Trim();
		string counterpartId = (counterpartKingdom.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(observerId)
			|| string.IsNullOrWhiteSpace(counterpartId)
			|| string.Equals(observerId, counterpartId, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		foreach (VassalageAgreement agreement in _agreementsByVassalId.Values)
		{
			if (agreement == null || !agreement.IsValid())
			{
				continue;
			}
			string suzerainId = (agreement.SuzerainKingdomId ?? "").Trim();
			string vassalId = (agreement.VassalKingdomId ?? "").Trim();
			AfVassalageType type = NormalizeVassalageType(agreement.Type);
			string typeText = GetVassalageTypeDisplayName(type);
			string clause = BuildVassalagePromptClause(type);
			if (string.Equals(observerId, vassalId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(counterpartId, suzerainId, StringComparison.OrdinalIgnoreCase))
			{
				Kingdom vassal = agreement.ResolveVassal() ?? observerKingdom;
				Kingdom suzerain = agreement.ResolveSuzerain() ?? counterpartKingdom;
				if (!IsValidKingdom(vassal) || !IsValidKingdom(suzerain))
				{
					continue;
				}
				return "【当前臣属关系】你所在的王国（" + GetKingdomDisplayName(vassal, "你的王国") + "）是" + counterpartLabel + "（" + GetKingdomDisplayName(suzerain, "对方王国") + "）的" + typeText + "；" + clause;
			}
			if (string.Equals(observerId, suzerainId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(counterpartId, vassalId, StringComparison.OrdinalIgnoreCase))
			{
				Kingdom suzerain = agreement.ResolveSuzerain() ?? observerKingdom;
				Kingdom vassal = agreement.ResolveVassal() ?? counterpartKingdom;
				if (!IsValidKingdom(suzerain) || !IsValidKingdom(vassal))
				{
					continue;
				}
				return "【当前臣属关系】你所在的王国（" + GetKingdomDisplayName(suzerain, "你的王国") + "）是" + counterpartLabel + "（" + GetKingdomDisplayName(vassal, "对方王国") + "）的宗主国；对方王国是你的" + typeText + "；" + clause;
			}
		}
		return "";
	}

	private static string BuildVassalagePromptClause(AfVassalageType type)
	{
		switch (NormalizeVassalageType(type))
		{
		case AfVassalageType.Tributary:
			return "朝贡国缴纳贡赋换取庇护，通常保留军事自主权。";
		case AfVassalageType.Garrison:
			return "卫戍国接受宗主军事号令，并承担出兵义务。";
		default:
			return "附庸国外交军事受宗主控制，并承担贡赋与出兵义务。";
		}
	}

	public TerminalVassalageManagementData BuildTerminalVassalageManagementData()
	{
		TerminalVassalageManagementData data = new TerminalVassalageManagementData();
		try
		{
			Kingdom playerKingdom = GetPlayerKingdom();
			if (!IsValidKingdom(playerKingdom))
			{
				data.DescriptionText = "你尚未拥有王国。";
				return data;
			}
			string playerKingdomName = GetKingdomDisplayName(playerKingdom, "玩家王国");
			if (!IsPlayerRuler(playerKingdom))
			{
				data.DescriptionText = "你的当前王国：" + playerKingdomName + "\n\n你不是该王国的国王，不能查阅宗主国臣属名册。";
				return data;
			}
			int today = GetCurrentCampaignDay();
			var agreements = GetPlayerVassalAgreements()
				.Select((VassalageAgreement agreement) => new
				{
					Agreement = agreement,
					Vassal = agreement?.ResolveVassal()
				})
				.Where(x => x.Agreement != null && IsValidKingdom(x.Vassal))
				.OrderBy(x => GetKingdomDisplayName(x.Vassal, "臣属国"), StringComparer.OrdinalIgnoreCase)
				.ToList();
			data.DescriptionText = "宗主国：" + playerKingdomName
				+ "\n选择有贡赋义务的臣属国，查看贡赋入库记录。";
			if (agreements.Count <= 0)
			{
				data.DescriptionText += "\n\n你的王国尚无臣属国。";
				return data;
			}
			foreach (var item in agreements)
			{
				data.Subjects.Add(BuildTerminalVassalageSubjectData(item.Agreement, item.Vassal, today));
			}
			return data;
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[WARN] build terminal vassalage management data failed: " + ex.Message);
			data.DescriptionText = "臣属名册生成失败，请稍后再试。";
			data.Subjects.Clear();
			return data;
		}
	}

	public string BuildTerminalVassalageManagementReport()
	{
		TerminalVassalageManagementData data = BuildTerminalVassalageManagementData();
		if (data.Subjects.Count <= 0)
		{
			return data.DescriptionText ?? "";
		}
		List<string> lines = new List<string>
		{
			data.DescriptionText ?? "",
			"",
			"臣属国名册："
		};
		for (int i = 0; i < data.Subjects.Count; i++)
		{
			TerminalVassalageSubjectData subject = data.Subjects[i];
			lines.Add((i + 1).ToString(CultureInfo.InvariantCulture) + ". " + subject.VassalName);
			lines.Add("   条约：" + subject.TypeName);
			lines.Add("   立约日：" + subject.CreatedDateText);
			lines.Add("   履约时长：" + subject.ElapsedDaysText);
			if (!string.IsNullOrWhiteSpace(subject.ObedienceText))
			{
				lines.Add("   独立度：" + subject.ObedienceText);
			}
			lines.Add(subject.IsTributePaying
				? "   贡赋记录：请在臣属国管理列表中选择“查看贡赋记录”（当前 " + subject.TributeRecordCount.ToString(CultureInfo.InvariantCulture) + " 条）。"
				: "   贡赋记录：该臣属类型不交贡。");
		}
		return string.Join("\n", lines).TrimEnd();
	}

	public TerminalTributaryPaymentHistoryData BuildTerminalTributaryPaymentHistoryData(string agreementId)
	{
		TerminalTributaryPaymentHistoryData data = new TerminalTributaryPaymentHistoryData();
		try
		{
			Kingdom playerKingdom = GetPlayerKingdom();
			if (!IsValidKingdom(playerKingdom))
			{
				data.SubtitleText = "你尚未拥有王国。";
				data.EmptyStateText = "尚无贡赋入库记录。";
				return data;
			}
			if (!IsPlayerRuler(playerKingdom))
			{
				data.SubtitleText = "你不是当前王国的国王。";
				data.EmptyStateText = "不能查阅宗主国臣属贡赋簿册。";
				return data;
			}
			VassalageAgreement agreement = FindAgreementById(agreementId);
			if (agreement == null)
			{
				string vassalId = (agreementId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(vassalId) && _agreementsByVassalId.TryGetValue(vassalId, out var agreementByVassalId))
				{
					agreement = agreementByVassalId;
				}
			}
			if (agreement == null || !agreement.IsValid() || !string.Equals(agreement.SuzerainKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase))
			{
				data.SubtitleText = "未找到该臣属国条约。";
				data.EmptyStateText = "尚无贡赋入库记录。";
				return data;
			}
			Kingdom vassal = agreement.ResolveVassal();
			AfVassalageType normalizedType = NormalizeVassalageType(agreement.Type);
			string vassalName = GetKingdomDisplayName(vassal, "臣属国");
			int createdDay = Math.Max(0, agreement.CreatedDay);
			int elapsedDays = Math.Max(0, GetCurrentCampaignDay() - createdDay);
			data.TitleText = vassalName + " · 贡赋记录";
			data.SubtitleText = GetVassalageTypeDisplayName(normalizedType)
				+ " · 立约日：" + FormatCampaignDate(createdDay)
				+ " · 履约：" + elapsedDays.ToString(CultureInfo.InvariantCulture) + "天";
			if (!IsTributePayingSubjectType(normalizedType))
			{
				data.EmptyStateText = "该臣属类型不产生贡赋记录。";
				return data;
			}
			foreach (TributaryPaymentNoticeRecord record in GetTributaryPaymentHistoryForAgreement(agreement))
			{
				data.Records.Add(BuildTerminalTributaryPaymentRecordData(record));
			}
			if (data.Records.Count <= 0)
			{
				data.EmptyStateText = "尚无贡赋入库记录。";
			}
			return data;
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[WARN] build terminal tributary payment history failed: " + ex.Message);
			data.SubtitleText = "贡赋簿册生成失败。";
			data.EmptyStateText = "尚无贡赋入库记录。";
			data.Records.Clear();
			return data;
		}
	}

	private TerminalVassalageSubjectData BuildTerminalVassalageSubjectData(VassalageAgreement agreement, Kingdom vassal, int today)
	{
		AfVassalageType normalizedType = NormalizeVassalageType(agreement.Type);
		string vassalName = GetKingdomDisplayName(vassal, "臣属国");
		string typeName = GetVassalageTypeDisplayName(normalizedType);
		int createdDay = Math.Max(0, agreement.CreatedDay);
		int elapsedDays = Math.Max(0, today - createdDay);
		bool isTributePaying = IsTributePayingSubjectType(normalizedType);
		int recordCount = isTributePaying ? GetTributaryPaymentHistoryForAgreement(agreement).Count : 0;
		string obedienceText = "";
		string obedienceShortText = "";
		if (UsesSubjectIndependence(normalizedType))
		{
			TryGetSubjectIndependenceStatus(agreement, out int independence, out int breakawayThreshold, out int rulerRelation, out string rulerName);
			obedienceText = independence.ToString(CultureInfo.InvariantCulture) + "/100；脱离阈值 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "；" + rulerName + "关系 " + FormatSignedRelation(rulerRelation);
			obedienceShortText = independence.ToString(CultureInfo.InvariantCulture) + "/100";
		}
		string elapsedText = elapsedDays.ToString(CultureInfo.InvariantCulture) + "天";
		string title = vassalName + " · " + typeName + " · " + elapsedText;
		if (!string.IsNullOrWhiteSpace(obedienceShortText))
		{
			title += " · 独立" + obedienceShortText;
		}
		title += isTributePaying
			? " · 贡赋" + recordCount.ToString(CultureInfo.InvariantCulture) + "条"
			: " · 不交贡";
		return new TerminalVassalageSubjectData
		{
			AgreementId = agreement.AgreementId ?? "",
			VassalKingdomId = agreement.VassalKingdomId ?? "",
			VassalName = vassalName,
			TypeName = typeName,
			CreatedDateText = FormatCampaignDate(createdDay),
			ElapsedDaysText = elapsedText,
			ObedienceText = obedienceText,
			IsTributePaying = isTributePaying,
			TributeRecordCount = recordCount,
			EntryTitleText = title,
			EntryHintText = "条约：" + typeName
				+ "\n立约日：" + FormatCampaignDate(createdDay)
				+ "\n履约时长：" + elapsedText
				+ (string.IsNullOrWhiteSpace(obedienceText) ? "" : "\n独立度：" + obedienceText)
				+ (isTributePaying ? "\n点击查看详细贡赋记录。" : "\n该臣属类型不产生贡赋记录。")
		};
	}

	private List<TributaryPaymentNoticeRecord> GetTributaryPaymentHistoryForAgreement(VassalageAgreement agreement)
	{
		List<TributaryPaymentNoticeRecord> records = new List<TributaryPaymentNoticeRecord>();
		if (agreement == null || !agreement.IsValid() || !IsTributePayingSubjectType(agreement.Type))
		{
			return records;
		}
		foreach (KeyValuePair<string, string> item in _tributaryPaymentHistory)
		{
			if (TryDeserializeTributaryPaymentRecord(item.Key, item.Value, out var record) && IsTributaryPaymentRecordForAgreement(record, agreement))
			{
				records.Add(record);
			}
		}
		return records
			.OrderByDescending((TributaryPaymentNoticeRecord x) => x.SettlementDay)
			.ThenByDescending((TributaryPaymentNoticeRecord x) => x.NoticeId ?? "", StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static string BuildTerminalTributaryPaymentHistoryLine(TributaryPaymentNoticeRecord record)
	{
		if (record == null)
		{
			return "- 贡赋记录已经失效。";
		}
		return "- " + FormatCampaignDate(record.SettlementDay)
			+ "，臣属国力：" + FormatTributaryPaymentNumber(record.TributaryStrength)
			+ "；入库：繁荣 +" + FormatTributaryPaymentNumber(record.PlayerProsperityGain)
			+ "、食物库存 +" + FormatTributaryPaymentNumber(record.PlayerFoodGain)
			+ "、村庄户数 +" + FormatTributaryPaymentNumber(record.PlayerHearthGain)
			+ "；贡赋代价：繁荣 -" + FormatTributaryPaymentNumber(Math.Abs(record.TributaryProsperityLoss))
			+ "、食物库存 -" + FormatTributaryPaymentNumber(Math.Abs(record.TributaryFoodLoss))
			+ "、村庄户数 -" + FormatTributaryPaymentNumber(Math.Abs(record.TributaryHearthLoss));
	}

	private static TerminalTributaryPaymentRecordData BuildTerminalTributaryPaymentRecordData(TributaryPaymentNoticeRecord record)
	{
		if (record == null)
		{
			return new TerminalTributaryPaymentRecordData
			{
				DateText = "未知日期",
				TributeValueText = "贡赋记录已经失效。",
				PlayerGainSummaryText = "",
				PlayerSettlementGainText = "本条贡赋记录已经失效。",
				TributaryCostText = ""
			};
		}
		string playerGainText = (BuildTributaryPaymentSettlementGainText(record) ?? "").TrimEnd();
		if (string.IsNullOrWhiteSpace(playerGainText))
		{
			playerGainText = "本次贡赋未能分配到宗主国领地。";
		}
		string tributaryCostText = BuildTributaryPaymentClassifiedTributaryCostText(record);
		if (string.IsNullOrWhiteSpace(tributaryCostText))
		{
			tributaryCostText = "繁荣度 -" + FormatTributaryPaymentNumber(Math.Abs(record.TributaryProsperityLoss))
				+ "（城镇 " + record.TributaryTownCount.ToString(CultureInfo.InvariantCulture)
				+ "，城堡 " + record.TributaryCastleCount.ToString(CultureInfo.InvariantCulture) + "）\n"
				+ "粮食 -" + FormatTributaryPaymentNumber(Math.Abs(record.TributaryFoodLoss)) + "\n"
				+ "户数 -" + FormatTributaryPaymentNumber(Math.Abs(record.TributaryHearthLoss))
				+ "（村庄 " + record.TributaryVillageCount.ToString(CultureInfo.InvariantCulture) + "）";
		}
		return new TerminalTributaryPaymentRecordData
		{
			DateText = FormatCampaignDate(record.SettlementDay),
			TributeValueText = "贡赋价值：" + FormatTributaryPaymentNumber(record.TributaryStrength),
			PlayerGainSummaryText = "本次贡赋已入库，宗主国各领地所得如下：",
			PlayerSettlementGainText = playerGainText,
			TributaryCostText = tributaryCostText.TrimEnd()
		};
	}

	private static bool IsTributePayingSubjectType(AfVassalageType type)
	{
		AfVassalageType normalized = NormalizeVassalageType(type);
		return normalized == AfVassalageType.Tributary || normalized == AfVassalageType.Vassal;
	}

	public bool ShouldAllowDeclareWarAction(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		if (_isApplyingVassalageDiplomacy)
		{
			return true;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom declaringKingdom = ResolveFactionKingdom(faction1, playerKingdom);
		Kingdom targetKingdom = ResolveFactionKingdom(faction2, playerKingdom);
		if (declaringKingdom == null || targetKingdom == null || declaringKingdom == targetKingdom)
		{
			return true;
		}
		bool declaringIsPlayer = IsPlayerFactionForDiplomacy(faction1, declaringKingdom, playerKingdom);
		bool targetIsPlayer = IsPlayerFactionForDiplomacy(faction2, targetKingdom, playerKingdom);
		VassalageAgreement targetAgreement = GetPlayerVassalAgreement(targetKingdom);
		VassalageAgreement declaringAgreement = GetPlayerVassalAgreement(declaringKingdom);
		VassalageAgreement targetAnyAgreement = GetAnyVassalAgreement(targetKingdom);
		VassalageAgreement declaringAnyAgreement = GetAnyVassalAgreement(declaringKingdom);
		VassalageDiagnosticLog.Event("diplomacy.declare_war.guard.observe", new Dictionary<string, object>
		{
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
			["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
			["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["declaringIsPlayer"] = declaringIsPlayer,
			["targetIsPlayer"] = targetIsPlayer,
			["declaringAgreement"] = DescribeAgreementForDiagnostics(declaringAgreement),
			["targetAgreement"] = DescribeAgreementForDiagnostics(targetAgreement),
			["declaringAnyAgreement"] = DescribeAgreementForDiagnostics(declaringAnyAgreement),
			["targetAnyAgreement"] = DescribeAgreementForDiagnostics(targetAnyAgreement),
			["detail"] = detail
		});
		if (declaringIsPlayer && targetAgreement != null)
		{
			if (detail != DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility)
			{
				VassalageDiagnosticLog.Event("diplomacy.declare_war.block", new Dictionary<string, object>
				{
					["reason"] = "player_kingdom_decision_against_subject_blocked",
					["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
					["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
					["type"] = targetAgreement.Type,
					["detail"] = detail
				});
				InformationManager.DisplayMessage(new InformationMessage("王国议会不能绕过臣属条约向" + GetKingdomDisplayName(targetKingdom, GetVassalageTypeDisplayName(targetAgreement.Type)) + "宣战。若要解除臣属关系，请先通过正式谈判处置条约。", Color.FromUint(4294936661u)));
				return false;
			}
			bool allowWar = HandlePlayerWarAgainstSubject(targetAgreement, detail);
			VassalageDiagnosticLog.Event(allowWar ? "diplomacy.declare_war.allow" : "diplomacy.declare_war.block", new Dictionary<string, object>
			{
				["reason"] = allowWar ? "player_declared_war_on_subject_released" : "player_declaring_war_against_subject",
				["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
				["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["type"] = targetAgreement.Type,
				["detail"] = detail
			});
			return allowWar;
		}
		Kingdom declaringSuzerain = declaringAnyAgreement?.ResolveSuzerain();
		if (declaringAnyAgreement != null && IsValidKingdom(declaringSuzerain) && declaringSuzerain == targetKingdom)
		{
			Logger.Log("Vassalage", "Blocked subject war against suzerain subject=" + (declaringKingdom.StringId ?? "") + " target=" + (targetKingdom.StringId ?? "") + " detail=" + detail);
			VassalageDiagnosticLog.Event("diplomacy.declare_war.block", new Dictionary<string, object>
			{
				["reason"] = "subject_declaring_war_against_suzerain",
				["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
				["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["agreement"] = DescribeAgreementForDiagnostics(declaringAnyAgreement),
				["type"] = declaringAnyAgreement.Type,
				["detail"] = detail
			});
			return false;
		}
		Kingdom targetSuzerain = targetAnyAgreement?.ResolveSuzerain();
		if (targetAnyAgreement != null && IsValidKingdom(targetSuzerain) && targetSuzerain == declaringKingdom)
		{
			Logger.Log("Vassalage", "Blocked suzerain war against subject suzerain=" + (declaringKingdom.StringId ?? "") + " subject=" + (targetKingdom.StringId ?? "") + " detail=" + detail);
			VassalageDiagnosticLog.Event("diplomacy.declare_war.block", new Dictionary<string, object>
			{
				["reason"] = "suzerain_declaring_war_against_subject",
				["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
				["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["agreement"] = DescribeAgreementForDiagnostics(targetAnyAgreement),
				["type"] = targetAnyAgreement.Type,
				["detail"] = detail
			});
			return false;
		}
		if (declaringAgreement != null && targetIsPlayer)
		{
			Logger.Log("Vassalage", "Blocked subject war against suzerain subject=" + (declaringKingdom.StringId ?? "") + " target=" + (targetKingdom.StringId ?? "") + " detail=" + detail);
			VassalageDiagnosticLog.Event("diplomacy.declare_war.block", new Dictionary<string, object>
			{
				["reason"] = "subject_declaring_war_against_suzerain",
				["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
				["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["type"] = declaringAgreement.Type,
				["detail"] = detail
			});
			return false;
		}
		if (declaringAgreement != null)
		{
			AfVassalageType declaringType = NormalizeVassalageType(declaringAgreement.Type);
			if (declaringType == AfVassalageType.Garrison || declaringType == AfVassalageType.Vassal)
			{
				Logger.Log("Vassalage", "Blocked controlled subject independent war declaration subject=" + (declaringKingdom.StringId ?? "") + " target=" + (targetKingdom.StringId ?? "") + " detail=" + detail);
				VassalageDiagnosticLog.Event("diplomacy.declare_war.block", new Dictionary<string, object>
				{
					["reason"] = "controlled_subject_independent_war",
					["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
					["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
					["type"] = declaringAgreement.Type,
					["detail"] = detail
				});
				return false;
			}
			if (declaringType == AfVassalageType.Tributary && targetAgreement != null)
			{
				AfVassalageType targetType = NormalizeVassalageType(targetAgreement.Type);
				if (targetType == AfVassalageType.Garrison || targetType == AfVassalageType.Vassal)
				{
					VassalageDiagnosticLog.Event("diplomacy.declare_war.allow", new Dictionary<string, object>
					{
						["reason"] = "tributary_declared_war_on_controlled_subject_defer_to_event",
						["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
						["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
						["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
						["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
						["declaringType"] = declaringAgreement.Type,
						["targetType"] = targetAgreement.Type,
						["detail"] = detail
					});
					return true;
				}
				if (targetType == AfVassalageType.Tributary)
				{
					VassalageDiagnosticLog.Event("diplomacy.declare_war.allow", new Dictionary<string, object>
					{
						["reason"] = "tributary_declared_war_on_tributary_notice_only_defer_to_event",
						["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
						["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
						["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
						["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
						["declaringType"] = declaringAgreement.Type,
						["targetType"] = targetAgreement.Type,
						["detail"] = detail
					});
					return true;
				}
			}
		}
		VassalageDiagnosticLog.Event("diplomacy.declare_war.allow", new Dictionary<string, object>
		{
			["reason"] = "vanilla_declare_war_allowed",
			["faction1"] = DescribeFactionForDiagnostics(faction1, declaringKingdom),
			["faction2"] = DescribeFactionForDiagnostics(faction2, targetKingdom),
			["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
			["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["declaringAgreement"] = DescribeAgreementForDiagnostics(declaringAgreement),
			["targetAgreement"] = DescribeAgreementForDiagnostics(targetAgreement),
			["detail"] = detail
		});
		return true;
	}

	public bool ShouldAllowMakePeaceAction(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		if (_isApplyingVassalageDiplomacy)
		{
			return true;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom kingdom1 = ResolveFactionKingdom(faction1, playerKingdom);
		Kingdom kingdom2 = ResolveFactionKingdom(faction2, playerKingdom);
		if (kingdom1 == null || kingdom2 == null || kingdom1 == kingdom2)
		{
			return true;
		}
		bool side1IsPlayer = kingdom1 == playerKingdom || IsPlayerFactionForDiplomacy(faction1, kingdom1, playerKingdom);
		bool side2IsPlayer = kingdom2 == playerKingdom || IsPlayerFactionForDiplomacy(faction2, kingdom2, playerKingdom);
		Kingdom playerPeaceEnemy = side1IsPlayer ? kingdom2 : (side2IsPlayer ? kingdom1 : null);
		VassalageDiagnosticLog.Event("make_peace.guard.observe", new Dictionary<string, object>
		{
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
			["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["side1IsPlayer"] = side1IsPlayer,
			["side2IsPlayer"] = side2IsPlayer,
			["playerPeaceEnemy"] = VassalageDiagnosticLog.DescribeKingdom(playerPeaceEnemy),
			["protectedSubjectWarCount"] = _protectedTributaryWars.Count,
			["detail"] = detail
		});
		if (TryFindProtectedSuzerainWarByParties(kingdom1, kingdom2, requireSubjectWar: true, out var suzerainProtectedKey, out var suzerainProtectedAgreement, out var protectedSuzerain, out var suzerainProtectedSubject, out var suzerainProtectedEnemy))
		{
			bool enemyIsOfferingPeaceToSuzerain = kingdom1 == suzerainProtectedEnemy && kingdom2 == protectedSuzerain;
			if (enemyIsOfferingPeaceToSuzerain)
			{
				if (protectedSuzerain == playerKingdom)
				{
					InformationManager.DisplayMessage(new InformationMessage("此战因保护" + GetKingdomDisplayName(suzerainProtectedSubject, "朝贡国") + "而起；敌国不能绕过受保护方直接向宗主国单独求和。", Color.FromUint(4294936661u)));
				}
				Logger.Log("Vassalage", "Blocked enemy peace offer to suzerain for protected subject war suzerain=" + (protectedSuzerain?.StringId ?? "") + " subject=" + (suzerainProtectedSubject?.StringId ?? "") + " enemy=" + (suzerainProtectedEnemy?.StringId ?? "") + " detail=" + detail);
				VassalageDiagnosticLog.Event("diplomacy.make_peace.block", new Dictionary<string, object>
				{
					["reason"] = "protected_subject_enemy_offer_to_suzerain_blocked",
					["protectedKey"] = suzerainProtectedKey ?? "",
					["agreementId"] = suzerainProtectedAgreement?.AgreementId ?? "",
					["type"] = suzerainProtectedAgreement?.Type ?? AfVassalageType.Tributary,
					["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(protectedSuzerain),
					["subject"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedSubject),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedEnemy),
					["subjectAtWar"] = IsAtWar(suzerainProtectedSubject, suzerainProtectedEnemy),
					["suzerainAtWar"] = IsAtWar(protectedSuzerain, suzerainProtectedEnemy),
					["detail"] = detail
				});
				return false;
			}
			VassalageDiagnosticLog.Event("diplomacy.make_peace.allow", new Dictionary<string, object>
			{
				["reason"] = "protected_subject_suzerain_side_peace_will_sync_subject",
				["protectedKey"] = suzerainProtectedKey ?? "",
				["agreementId"] = suzerainProtectedAgreement?.AgreementId ?? "",
				["type"] = suzerainProtectedAgreement?.Type ?? AfVassalageType.Tributary,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(protectedSuzerain),
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedSubject),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedEnemy),
				["subjectAtWar"] = IsAtWar(suzerainProtectedSubject, suzerainProtectedEnemy),
				["suzerainAtWar"] = IsAtWar(protectedSuzerain, suzerainProtectedEnemy),
				["detail"] = detail
			});
			return true;
		}
		if (playerPeaceEnemy != null)
		{
			if (TryFindActiveProtectedSubjectWar(null, playerPeaceEnemy, requirePlayerWar: true, out var protectedKey, out var protectedAgreement, out var protectedSubject, out var protectedEnemy))
			{
				bool enemyIsOfferingPeaceToPlayer = side2IsPlayer && !side1IsPlayer;
				string subjectName = GetKingdomDisplayName(protectedSubject, "朝贡国");
				if (enemyIsOfferingPeaceToPlayer)
				{
					InformationManager.DisplayMessage(new InformationMessage("此战因保护" + subjectName + "而起；敌国不能绕过受保护方直接向宗主国单独求和。", Color.FromUint(4294936661u)));
					Logger.Log("Vassalage", "Blocked enemy peace offer to player for protected subject war subject=" + (protectedSubject?.StringId ?? "") + " enemy=" + (protectedEnemy?.StringId ?? "") + " detail=" + detail);
					VassalageDiagnosticLog.Event("diplomacy.make_peace.block", new Dictionary<string, object>
					{
						["reason"] = "protected_subject_enemy_offer_to_suzerain_blocked",
						["protectedKey"] = protectedKey ?? "",
						["agreementId"] = protectedAgreement?.AgreementId ?? "",
						["type"] = protectedAgreement?.Type ?? AfVassalageType.Tributary,
						["subject"] = VassalageDiagnosticLog.DescribeKingdom(protectedSubject),
						["enemy"] = VassalageDiagnosticLog.DescribeKingdom(protectedEnemy),
						["subjectAtWar"] = IsAtWar(protectedSubject, protectedEnemy),
						["playerAtWar"] = IsAtWar(playerKingdom, protectedEnemy),
						["detail"] = detail
					});
					return false;
				}
				InformationManager.DisplayMessage(new InformationMessage("此战因保护" + subjectName + "而起；宗主国和平将同步受保护朝贡国停战。", Color.FromUint(4294936661u)));
				Logger.Log("Vassalage", "Allowed player-side peace for protected subject war subject=" + (protectedSubject?.StringId ?? "") + " enemy=" + (protectedEnemy?.StringId ?? "") + " detail=" + detail + " willSyncSubject=true");
				VassalageDiagnosticLog.Event("diplomacy.make_peace.allow", new Dictionary<string, object>
				{
					["reason"] = "protected_subject_player_side_peace_will_sync_subject",
					["protectedKey"] = protectedKey ?? "",
					["agreementId"] = protectedAgreement?.AgreementId ?? "",
					["type"] = protectedAgreement?.Type ?? AfVassalageType.Tributary,
					["subject"] = VassalageDiagnosticLog.DescribeKingdom(protectedSubject),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(protectedEnemy),
					["subjectAtWar"] = IsAtWar(protectedSubject, protectedEnemy),
					["playerAtWar"] = IsAtWar(playerKingdom, protectedEnemy),
					["detail"] = detail
				});
				return true;
			}
			VassalageDiagnosticLog.Event("diplomacy.make_peace.allow", new Dictionary<string, object>
			{
				["reason"] = "player_peace_no_active_protected_subject_war",
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(playerPeaceEnemy),
				["protectedSubjectWarCount"] = _protectedTributaryWars.Count,
				["detail"] = detail
			});
			return true;
		}
		if (TryFindProtectedSubjectWarByParties(kingdom1, kingdom2, requireSubjectWar: true, requirePlayerWar: true, out var anchorKey, out var anchorAgreement, out var anchorSubject, out var anchorEnemy))
		{
			VassalageDiagnosticLog.Event("diplomacy.make_peace.allow", new Dictionary<string, object>
			{
				["reason"] = "protected_subject_anchor_peace",
				["protectedKey"] = anchorKey ?? "",
				["agreementId"] = anchorAgreement?.AgreementId ?? "",
				["type"] = anchorAgreement?.Type ?? AfVassalageType.Tributary,
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(anchorSubject),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(anchorEnemy),
				["subjectAtWar"] = IsAtWar(anchorSubject, anchorEnemy),
				["playerAtWar"] = IsAtWar(playerKingdom, anchorEnemy),
				["detail"] = detail
			});
			return true;
		}
		VassalageAgreement agreement1 = GetPlayerVassalAgreement(kingdom1);
		VassalageAgreement agreement2 = GetPlayerVassalAgreement(kingdom2);
		VassalageAgreement vassalAgreement = agreement1 ?? agreement2;
		Kingdom vassal = agreement1 != null ? kingdom1 : (agreement2 != null ? kingdom2 : null);
		Kingdom other = vassal == kingdom1 ? kingdom2 : (vassal == kingdom2 ? kingdom1 : null);
		if (vassal == null || other == null)
		{
			return true;
		}
		AfVassalageType type = NormalizeVassalageType(vassalAgreement.Type);
		if (type == AfVassalageType.Vassal)
		{
			Logger.Log("Vassalage", "Blocked vassal separate peace vassal=" + (vassal.StringId ?? "") + " enemy=" + (other.StringId ?? "") + " detail=" + detail);
			VassalageDiagnosticLog.Event("diplomacy.make_peace.block", new Dictionary<string, object>
			{
				["reason"] = "vassal_separate_peace",
				["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(other),
				["detail"] = detail
			});
			return false;
		}
		if (type != AfVassalageType.Garrison)
		{
			return true;
		}
		if (!IsAtWar(playerKingdom, other))
		{
			return true;
		}
		Logger.Log("Vassalage", "Blocked military vassal separate peace vassal=" + (vassal.StringId ?? "") + " enemy=" + (other.StringId ?? "") + " detail=" + detail);
		VassalageDiagnosticLog.Event("diplomacy.make_peace.block", new Dictionary<string, object>
		{
			["reason"] = "military_vassal_separate_peace_while_player_at_war",
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(other),
			["detail"] = detail
		});
		return false;
	}

	private bool ShouldAllowCampaignLogNotificationCore(LogEntry log)
	{
		if (log == null)
		{
			return true;
		}
		IFaction faction1 = null;
		IFaction faction2 = null;
		string action = "";
		if (log is DeclareWarLogEntry declareWarLog)
		{
			faction1 = declareWarLog.Faction1;
			faction2 = declareWarLog.Faction2;
			action = "declare_war";
		}
		else if (log is MakePeaceLogEntry makePeaceLog)
		{
			faction1 = makePeaceLog.Faction1;
			faction2 = makePeaceLog.Faction2;
			action = "make_peace";
		}
		else
		{
			return true;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom kingdom1 = ResolveFactionKingdom(faction1, playerKingdom);
		Kingdom kingdom2 = ResolveFactionKingdom(faction2, playerKingdom);
		if (!IsValidKingdom(playerKingdom) || !IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2))
		{
			return true;
		}
		bool playerInvolved = kingdom1 == playerKingdom
			|| kingdom2 == playerKingdom
			|| IsPlayerFactionForDiplomacy(faction1, kingdom1, playerKingdom)
			|| IsPlayerFactionForDiplomacy(faction2, kingdom2, playerKingdom);
		if (playerInvolved)
		{
			return true;
		}
		VassalageAgreement agreement1 = GetPlayerVassalAgreement(kingdom1);
		VassalageAgreement agreement2 = GetPlayerVassalAgreement(kingdom2);
		bool side1IsControlledSubject = IsControlledSubjectWithoutMilitaryAutonomy(agreement1);
		bool side2IsControlledSubject = IsControlledSubjectWithoutMilitaryAutonomy(agreement2);
		if (!side1IsControlledSubject && !side2IsControlledSubject)
		{
			return true;
		}
		VassalageDiagnosticLog.Event("diplomacy.log_notification.suppress", new Dictionary<string, object>
		{
			["reason"] = "controlled_subject_no_military_autonomy",
			["action"] = action,
			["logType"] = log.GetType().Name,
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
			["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["agreement1"] = DescribeAgreementForDiagnostics(agreement1),
			["agreement2"] = DescribeAgreementForDiagnostics(agreement2),
			["side1IsControlledSubject"] = side1IsControlledSubject,
			["side2IsControlledSubject"] = side2IsControlledSubject,
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["isApplyingVassalageDiplomacy"] = _isApplyingVassalageDiplomacy
		});
		return false;
	}

	private static bool IsControlledSubjectWithoutMilitaryAutonomy(VassalageAgreement agreement)
	{
		if (agreement == null)
		{
			return false;
		}
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		return type == AfVassalageType.Garrison || type == AfVassalageType.Vassal;
	}

	public static Kingdom ResolveKingdomById(string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	public static bool CanInjectVassalageRuleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		bool result = TryBuildVassalageRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var playerKingdom, out var targetKingdom, out var speaker);
		VassalageDiagnosticLog.Event("runtime.can_inject", new Dictionary<string, object>
		{
			["ok"] = result,
			["speaker"] = VassalageDiagnosticLog.DescribeHero(speaker),
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["targetCharacterId"] = targetCharacter?.StringId ?? ""
		});
		return result;
	}

	public static string BuildRuntimeVassalageInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		if (!TryBuildVassalageRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var playerKingdom, out var targetKingdom, out var speaker))
		{
			VassalageDiagnosticLog.Event("runtime.instruction", new Dictionary<string, object>
			{
				["ok"] = false,
				["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
				["targetCharacterId"] = targetCharacter?.StringId ?? ""
			});
			return "";
		}
		VassalageDiagnosticLog.Event("runtime.instruction", new Dictionary<string, object>
		{
			["ok"] = true,
			["speaker"] = VassalageDiagnosticLog.DescribeHero(speaker),
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom)
		});
		return "【AF臣属国谈判事实】\n"
			+ "玩家当前是" + GetKingdomDisplayName(playerKingdom, "玩家王国") + "的国王。\n"
			+ GetHeroDisplayName(speaker, "对话对象") + "当前是" + GetKingdomDisplayName(targetKingdom, "目标王国") + "的国王。\n"
			+ "本轮只允许谈判“" + GetKingdomDisplayName(targetKingdom, "目标王国") + "向玩家王国臣服”，不得让玩家加入或臣服于对方。\n"
			+ "目标王国ID必须写作：" + (targetKingdom.StringId ?? "") + "。\n"
			+ "第二版臣属国分三类：TRIBUTARY=朝贡国，交钱买保护但不出兵，保留军事自主权；GARRISON=卫戍国，军事臣属，出兵，不交钱，没有军事自主权，并使用0-100忠诚度；VASSAL=附庸国，也就是傀儡国，出钱、出兵，外交军事受宗主控制。\n"
			+ "注意：“附庸国”这个词只表示VASSAL傀儡国，不要写成完全附庸国。若只泛称臣属、称臣、归顺或承认宗主地位但没有明确朝贡/卫戍/附庸，应追随语义：交钱求保护=TRIBUTARY；军事臣属/卫戍/出兵=GARRISON；傀儡、附庸国、外交军事由宗主控制=VASSAL。\n"
			+ "若目标王国已经是玩家臣属，本轮仍可由当前国王同意改订为另一种臣属类型；后处理继续输出对应的新类型标签。";
	}

	public static string BuildRuntimeVassalageConstraintHintForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		if (!TryBuildVassalageRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var _, out var targetKingdom, out var _))
		{
			return "";
		}
		return "臣属国标签只可在对方国王明确接受臣属条约后使用，kingdomId 必须为 " + (targetKingdom.StringId ?? "") + "。三类为 TRIBUTARY 朝贡国、GARRISON 卫戍国、VASSAL 附庸国/傀儡国；“附庸国”只对应 VASSAL。";
	}

	public static List<PostprocessRuleEntry> BuildRuntimeVassalagePostprocessRulesForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		List<PostprocessRuleEntry> result = new List<PostprocessRuleEntry>();
		if (!TryBuildVassalageRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var _, out var targetKingdom, out var _))
		{
			VassalageDiagnosticLog.Event("postprocess.rules.build", new Dictionary<string, object>
			{
				["ok"] = false,
				["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
				["targetCharacterId"] = targetCharacter?.StringId ?? ""
			});
			return result;
		}
		string kingdomId = (targetKingdom.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return result;
		}
		foreach (PostprocessRuleEntry rule in AIConfigHandler.GetGuardrailRulePostprocessRules("kingdom_vassalage") ?? new List<PostprocessRuleEntry>())
		{
			string tag = (rule?.Tag ?? "").Trim();
			if (string.IsNullOrWhiteSpace(tag))
			{
				continue;
			}
			tag = tag.Replace("{kingdomId}", kingdomId).Replace("{targetKingdomId}", kingdomId);
			result.Add(new PostprocessRuleEntry
			{
				Tag = tag,
				Description = (rule.Description ?? "").Replace("{kingdomId}", kingdomId).Replace("{targetKingdomId}", kingdomId)
			});
		}
		VassalageDiagnosticLog.Event("postprocess.rules.build", new Dictionary<string, object>
		{
			["ok"] = result.Count > 0,
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["kingdomId"] = kingdomId,
			["ruleCount"] = result.Count,
			["tags"] = result.Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList()
		});
		return result;
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		VassalageDiagnosticLog.Event("behavior.session_launched", new Dictionary<string, object>
		{
			["logPath"] = VassalageDiagnosticLog.GetDiagnosticLogPath(),
			["agreementCount"] = _agreementsByVassalId.Count,
			["pendingInfoNoticeCount"] = _pendingInfoNotices.Count,
			["pendingProtectionCount"] = _pendingProtectionNotices.Count,
			["pendingNpcTributaryVassalageNoticeCount"] = _pendingNpcTributaryVassalageNotices.Count,
			["pendingTributaryPaymentCount"] = _pendingTributaryPaymentNotices.Count,
			["tributaryPaymentLastSettlementDayCount"] = _tributaryPaymentLastSettlementDays.Count,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["protectedTributaryWarCount"] = _protectedTributaryWars.Count
		});
		ProcessPendingDiplomacySyncs();
		TryPublishPendingNotices();
	}

	private void OnGameLoadFinished()
	{
		RemoveInvalidAgreements();
		EnsureGarrisonObedienceForLoadedAgreements();
		CheckLoadedSubjectBreakawayThresholds();
		VassalageDiagnosticLog.Event("behavior.game_load_finished", new Dictionary<string, object>
		{
			["agreementCount"] = _agreementsByVassalId.Count,
			["pendingInfoNoticeCount"] = _pendingInfoNotices.Count,
			["pendingProtectionCount"] = _pendingProtectionNotices.Count,
			["pendingNpcTributaryVassalageNoticeCount"] = _pendingNpcTributaryVassalageNotices.Count,
			["pendingTributaryPaymentCount"] = _pendingTributaryPaymentNotices.Count,
			["tributaryPaymentLastSettlementDayCount"] = _tributaryPaymentLastSettlementDays.Count,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["protectedTributaryWarCount"] = _protectedTributaryWars.Count,
			["garrisonObedienceCount"] = _garrisonObedienceValues.Count,
			["agreementIds"] = _agreementsByVassalId.Values.Select((VassalageAgreement x) => x?.AgreementId ?? "").ToList()
		});
		ProcessPendingDiplomacySyncs();
		TryPublishPendingNotices();
	}

	private void OnCampaignTick(float dt)
	{
		if (!HasPendingNoticeForMap() && _pendingDiplomacySyncs.Count == 0)
		{
			return;
		}
		ProcessPendingDiplomacySyncs();
		TryPublishPendingNotices();
	}

	private void OnDailyTick()
	{
		ProcessTributaryPayments();
	}

	private void CheckLoadedSubjectBreakawayThresholds()
	{
		foreach (VassalageAgreement agreement in GetPlayerVassalAgreements()
			.Where(x => x != null && UsesSubjectIndependence(NormalizeVassalageType(x.Type)))
			.ToList())
		{
			TryBreakSubjectAtCurrentThreshold(agreement, "subject_ruler_relation_threshold", "存档载入复核");
		}
	}

	private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
		Hero player = Hero.MainHero;
		if (player == null)
		{
			return;
		}
		if (IsSameHero(effectiveHero, player))
		{
			TryCheckSubjectBreakawayForPotentialRuler(effectiveHeroGainedRelationWith, "关系变化");
		}
		if (IsSameHero(effectiveHeroGainedRelationWith, player))
		{
			TryCheckSubjectBreakawayForPotentialRuler(effectiveHero, "关系变化");
		}
		if (IsSameHero(originalHero, player))
		{
			TryCheckSubjectBreakawayForPotentialRuler(originalGainedRelationWith, "关系变化");
		}
		if (IsSameHero(originalGainedRelationWith, player))
		{
			TryCheckSubjectBreakawayForPotentialRuler(originalHero, "关系变化");
		}
	}

	private void OnRulingClanChanged(Kingdom kingdom, Clan changedClan)
	{
		TryCheckSubjectBreakawayForKingdom(kingdom, "统治氏族更替");
	}

	private void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		TryCheckSubjectBreakawayForPotentialRuler(newLeader, "统治者更替");
	}

	private void TryCheckSubjectBreakawayForPotentialRuler(Hero hero, string trigger)
	{
		Kingdom kingdom = ResolveHeroKingdom(hero);
		if (!IsValidKingdom(kingdom) || !IsKingdomRuler(hero, kingdom))
		{
			return;
		}
		TryCheckSubjectBreakawayForKingdom(kingdom, trigger);
	}

	private void TryCheckSubjectBreakawayForKingdom(Kingdom kingdom, string trigger)
	{
		string kingdomId = (kingdom?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(kingdomId)
			|| !_agreementsByVassalId.TryGetValue(kingdomId, out VassalageAgreement agreement)
			|| agreement == null)
		{
			return;
		}
		TryBreakSubjectAtCurrentThreshold(agreement, "subject_ruler_relation_threshold", trigger);
	}

	private static Kingdom ResolveHeroKingdom(Hero hero)
	{
		return hero?.Clan?.Kingdom ?? hero?.MapFaction as Kingdom;
	}

	private static bool IsSameHero(Hero first, Hero second)
	{
		if (first == null || second == null)
		{
			return false;
		}
		return first == second || string.Equals(first.StringId ?? "", second.StringId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private void ProcessTributaryPayments()
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		bool hasValidPlayerKingdom = IsValidKingdom(playerKingdom);
		int today = GetCurrentCampaignDay();
		List<VassalageAgreement> tributePayingAgreements = GetTributePayingAgreements().ToList();
		int playerSuzerainAgreementCount = hasValidPlayerKingdom
			? tributePayingAgreements.Count((VassalageAgreement x) => string.Equals(x.SuzerainKingdomId ?? "", playerKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			: 0;
		VassalageDiagnosticLog.Event("tributary_payment.daily_check", new Dictionary<string, object>
		{
			["today"] = today,
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["hasValidPlayerKingdom"] = hasValidPlayerKingdom,
			["totalAgreementCount"] = _agreementsByVassalId.Count,
			["tributaryAgreementCount"] = tributePayingAgreements.Count,
			["tributePayingAgreementCount"] = tributePayingAgreements.Count,
			["playerSuzerainAgreementCount"] = playerSuzerainAgreementCount,
			["npcSuzerainAgreementCount"] = Math.Max(0, tributePayingAgreements.Count - playerSuzerainAgreementCount),
			["pendingTributaryPaymentCount"] = _pendingTributaryPaymentNotices.Count,
			["lastSettlementDayCount"] = _tributaryPaymentLastSettlementDays.Count,
			["canPublishMapNotification"] = CanPublishMapNotification(),
			["agreementIds"] = tributePayingAgreements.Select((VassalageAgreement x) => x?.AgreementId ?? "").ToList(),
			["tributePayingAgreements"] = tributePayingAgreements.Select(DescribeAgreementForDiagnostics).ToList()
		});
		foreach (VassalageAgreement agreement in tributePayingAgreements)
		{
			string agreementId = agreement.AgreementId ?? "";
			if (string.IsNullOrWhiteSpace(agreementId))
			{
				VassalageDiagnosticLog.Event("tributary_payment.evaluate.skip", new Dictionary<string, object>
				{
					["reason"] = "empty_agreement_id",
					["vassalKingdomId"] = agreement.VassalKingdomId ?? "",
					["suzerainKingdomId"] = agreement.SuzerainKingdomId ?? ""
				});
				continue;
			}
			Kingdom suzerainKingdom = agreement.ResolveSuzerain();
			Kingdom tributaryKingdom = agreement.ResolveVassal();
			int lastSettlementDay = GetTributaryPaymentLastSettlementDay(agreement);
			int daysSinceLastSettlement = today - lastSettlementDay;
			bool isDue = daysSinceLastSettlement >= TributaryPaymentIntervalDays;
			bool queuePlayerNotice = hasValidPlayerKingdom && suzerainKingdom == playerKingdom;
			VassalageDiagnosticLog.Event("tributary_payment.evaluate", new Dictionary<string, object>
			{
				["agreementId"] = agreementId,
				["type"] = agreement.Type,
				["normalizedType"] = NormalizeVassalageType(agreement.Type),
				["createdDay"] = agreement.CreatedDay,
				["today"] = today,
				["lastSettlementDay"] = lastSettlementDay,
				["daysSinceLastSettlement"] = daysSinceLastSettlement,
				["daysUntilNextSettlement"] = Math.Max(0, TributaryPaymentIntervalDays - daysSinceLastSettlement),
				["intervalDays"] = TributaryPaymentIntervalDays,
				["isDue"] = isDue,
				["queuePlayerNotice"] = queuePlayerNotice,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributaryKingdom)
			});
			if (!isDue)
			{
				continue;
			}
			if (!IsValidKingdom(suzerainKingdom))
			{
				VassalageDiagnosticLog.Event("tributary_payment.evaluate.skip", new Dictionary<string, object>
				{
					["reason"] = "invalid_suzerain",
					["agreementId"] = agreementId,
					["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
					["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributaryKingdom)
				});
				continue;
			}
			try
			{
				TrySettleTributaryPayment(agreement, suzerainKingdom, today, lastSettlementDay, queuePlayerNotice);
			}
			catch (Exception ex)
			{
				Logger.Log("Vassalage", "[ERROR] tributary payment settlement failed agreement=" + agreementId + ": " + ex);
				VassalageDiagnosticLog.Event("tributary_payment.error", new Dictionary<string, object>
				{
					["agreementId"] = agreementId,
					["today"] = today,
					["lastSettlementDay"] = lastSettlementDay,
					["exception"] = ex.ToString()
				});
			}
		}
	}

	private bool TrySettleTributaryPayment(VassalageAgreement agreement, Kingdom suzerainKingdom, int today, int lastSettlementDay, bool queuePlayerNotice)
	{
		if (agreement == null || !IsValidKingdom(suzerainKingdom))
		{
			return false;
		}
		AfVassalageType normalizedType = NormalizeVassalageType(agreement.Type);
		if (!IsTributePayingSubjectType(normalizedType))
		{
			return false;
		}
		Kingdom tributary = agreement.ResolveVassal();
		if (!IsValidKingdom(tributary) || tributary == suzerainKingdom)
		{
			VassalageDiagnosticLog.Event("tributary_payment.settlement_skip", new Dictionary<string, object>
			{
				["agreementId"] = agreement.AgreementId,
				["type"] = agreement.Type,
				["normalizedType"] = normalizedType,
				["reason"] = "invalid_tributary_or_self",
				["suzerainKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
				["today"] = today,
				["lastSettlementDay"] = lastSettlementDay
			});
			return false;
		}
		List<Settlement> suzerainSettlements = GetKingdomSettlements(suzerainKingdom);
		List<Settlement> tributarySettlements = GetKingdomSettlements(tributary);
		VassalageDiagnosticLog.Event("tributary_payment.settlement_begin", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = normalizedType,
			["suzerainKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
			["today"] = today,
			["lastSettlementDay"] = lastSettlementDay,
			["queuePlayerNotice"] = queuePlayerNotice,
			["suzerainSettlementCount"] = suzerainSettlements.Count,
			["playerSettlementCount"] = suzerainSettlements.Count,
			["suzerainTownCount"] = suzerainSettlements.Count((Settlement s) => s != null && s.IsTown),
			["suzerainCastleCount"] = suzerainSettlements.Count((Settlement s) => s != null && s.IsCastle),
			["suzerainVillageCount"] = suzerainSettlements.Count((Settlement s) => s != null && s.IsVillage),
			["tributarySettlementCount"] = tributarySettlements.Count,
			["tributaryTownCount"] = tributarySettlements.Count((Settlement s) => s != null && s.IsTown),
			["tributaryCastleCount"] = tributarySettlements.Count((Settlement s) => s != null && s.IsCastle),
			["tributaryVillageCount"] = tributarySettlements.Count((Settlement s) => s != null && s.IsVillage),
			["suzerainSettlements"] = suzerainSettlements.Select(VassalageDiagnosticLog.DescribeSettlement).ToList(),
			["tributarySettlements"] = tributarySettlements.Select(VassalageDiagnosticLog.DescribeSettlement).ToList()
		});
		if (suzerainSettlements.Count == 0 || tributarySettlements.Count == 0)
		{
			VassalageDiagnosticLog.Event("tributary_payment.skip", new Dictionary<string, object>
			{
				["agreementId"] = agreement.AgreementId,
				["suzerainKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
				["today"] = today,
				["lastSettlementDay"] = lastSettlementDay,
				["suzerainSettlementCount"] = suzerainSettlements.Count,
				["playerSettlementCount"] = suzerainSettlements.Count,
				["tributarySettlementCount"] = tributarySettlements.Count,
				["reason"] = "missing_settlements"
			});
			return false;
		}
		RefreshKingdomCurrentStrength(tributary);
		float rawStrength = tributary.CurrentTotalStrength;
		float strength = ClampTributaryStrength(rawStrength);
		TributaryPaymentTier tier = GetTributaryPaymentTier(strength);
		VassalageDiagnosticLog.Event("tributary_payment.tier_selected", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = normalizedType,
			["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
			["rawStrength"] = rawStrength,
			["clampedStrength"] = strength,
			["tier"] = tier.TierName,
			["townProsperityGainPerFief"] = tier.TownProsperity,
			["townFoodGainPerFief"] = tier.TownFood,
			["castleProsperityGainPerFief"] = tier.CastleProsperity,
			["castleFoodGainPerFief"] = tier.CastleFood,
			["villageHearthGainPerFief"] = tier.VillageHearth,
			["strengthClampMax"] = 10000f
		});
		TributaryPaymentTotals plannedSuzerainGain = CalculateTributaryPaymentPotentialBenefits(suzerainSettlements, tier);
		TributaryPaymentTotals tributaryLoss = ApplyTributaryPaymentCosts(tributarySettlements, plannedSuzerainGain);
		float prosperityPaymentRatio = CalculateTributaryPaymentRatio(plannedSuzerainGain.Prosperity * TributaryProsperityLossRatio, tributaryLoss.Prosperity);
		float foodPaymentRatio = CalculateTributaryPaymentRatio(plannedSuzerainGain.Food * TributaryFoodLossRatio, tributaryLoss.Food);
		float hearthPaymentRatio = CalculateTributaryPaymentRatio(plannedSuzerainGain.Hearth * TributaryHearthLossRatio, tributaryLoss.Hearth);
		VassalageDiagnosticLog.Event("tributary_payment.payment_ratio", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = normalizedType,
			["plannedSuzerainProsperityGain"] = plannedSuzerainGain.Prosperity,
			["plannedSuzerainFoodGain"] = plannedSuzerainGain.Food,
			["plannedSuzerainHearthGain"] = plannedSuzerainGain.Hearth,
			["requestedTributaryProsperityLoss"] = plannedSuzerainGain.Prosperity * TributaryProsperityLossRatio,
			["requestedTributaryFoodLoss"] = plannedSuzerainGain.Food * TributaryFoodLossRatio,
			["requestedTributaryHearthLoss"] = plannedSuzerainGain.Hearth * TributaryHearthLossRatio,
			["tributaryProsperityLoss"] = tributaryLoss.Prosperity,
			["tributaryFoodLoss"] = tributaryLoss.Food,
			["tributaryHearthLoss"] = tributaryLoss.Hearth,
			["prosperityPaymentRatio"] = prosperityPaymentRatio,
			["foodPaymentRatio"] = foodPaymentRatio,
			["hearthPaymentRatio"] = hearthPaymentRatio
		});
		TributaryPaymentTotals suzerainGain = ApplyTributaryPaymentBenefits(suzerainSettlements, tier, prosperityPaymentRatio, foodPaymentRatio, hearthPaymentRatio);
		SetTributaryPaymentLastSettlementDay(agreement, today);
		TributaryPaymentNoticeRecord record = new TributaryPaymentNoticeRecord
		{
			NoticeId = BuildTributaryPaymentNoticeId(agreement.VassalKingdomId, today),
			AgreementId = agreement.AgreementId,
			TributaryKingdomId = tributary.StringId ?? "",
			TributaryName = GetKingdomDisplayName(tributary, GetVassalageTypeDisplayName(normalizedType)),
			SettlementDay = today,
			TributaryStrength = strength,
			PlayerTownCount = suzerainGain.TownCount,
			PlayerCastleCount = suzerainGain.CastleCount,
			PlayerVillageCount = suzerainGain.VillageCount,
			TributaryTownCount = tributaryLoss.TownCount,
			TributaryCastleCount = tributaryLoss.CastleCount,
			TributaryVillageCount = tributaryLoss.VillageCount,
			TownProsperityGainPerFief = tier.TownProsperity,
			TownFoodGainPerFief = tier.TownFood,
			CastleProsperityGainPerFief = tier.CastleProsperity,
			CastleFoodGainPerFief = tier.CastleFood,
			VillageHearthGainPerFief = tier.VillageHearth,
			PlannedPlayerProsperityGain = plannedSuzerainGain.Prosperity,
			PlannedPlayerFoodGain = plannedSuzerainGain.Food,
			PlannedPlayerHearthGain = plannedSuzerainGain.Hearth,
			PlannedPlayerTownProsperityGain = plannedSuzerainGain.TownProsperity,
			PlannedPlayerTownFoodGain = plannedSuzerainGain.TownFood,
			PlannedPlayerCastleProsperityGain = plannedSuzerainGain.CastleProsperity,
			PlannedPlayerCastleFoodGain = plannedSuzerainGain.CastleFood,
			PlannedPlayerVillageHearthGain = plannedSuzerainGain.VillageHearth,
			ProsperityPaymentRatio = prosperityPaymentRatio,
			FoodPaymentRatio = foodPaymentRatio,
			HearthPaymentRatio = hearthPaymentRatio,
			PlayerProsperityGain = suzerainGain.Prosperity,
			PlayerFoodGain = suzerainGain.Food,
			PlayerHearthGain = suzerainGain.Hearth,
			PlayerTownProsperityGain = suzerainGain.TownProsperity,
			PlayerTownFoodGain = suzerainGain.TownFood,
			PlayerCastleProsperityGain = suzerainGain.CastleProsperity,
			PlayerCastleFoodGain = suzerainGain.CastleFood,
			PlayerVillageHearthGain = suzerainGain.VillageHearth,
			TributaryProsperityLoss = tributaryLoss.Prosperity,
			TributaryFoodLoss = tributaryLoss.Food,
			TributaryHearthLoss = tributaryLoss.Hearth,
			TributaryTownProsperityLoss = tributaryLoss.TownProsperity,
			TributaryTownFoodLoss = tributaryLoss.TownFood,
			TributaryCastleProsperityLoss = tributaryLoss.CastleProsperity,
			TributaryCastleFoodLoss = tributaryLoss.CastleFood,
			TributaryVillageHearthLoss = tributaryLoss.VillageHearth,
			PlayerSettlementGainLines = suzerainGain.NoticeLines.ToList()
		};
		if (queuePlayerNotice)
		{
			QueueTributaryPaymentNotice(record);
		}
		else
		{
			StoreTributaryPaymentRecord(record, false);
		}
		VassalageDiagnosticLog.Event("tributary_payment.settled", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = normalizedType,
			["noticeId"] = record.NoticeId,
			["suzerainKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
			["today"] = today,
			["lastSettlementDay"] = lastSettlementDay,
			["queuePlayerNotice"] = queuePlayerNotice,
			["strength"] = strength,
			["tier"] = tier.TierName,
			["plannedSuzerainProsperityGain"] = plannedSuzerainGain.Prosperity,
			["plannedSuzerainFoodGain"] = plannedSuzerainGain.Food,
			["plannedSuzerainHearthGain"] = plannedSuzerainGain.Hearth,
			["prosperityPaymentRatio"] = prosperityPaymentRatio,
			["foodPaymentRatio"] = foodPaymentRatio,
			["hearthPaymentRatio"] = hearthPaymentRatio,
			["suzerainProsperityGain"] = suzerainGain.Prosperity,
			["suzerainFoodGain"] = suzerainGain.Food,
			["suzerainHearthGain"] = suzerainGain.Hearth,
			["suzerainTownProsperityGain"] = suzerainGain.TownProsperity,
			["suzerainTownFoodGain"] = suzerainGain.TownFood,
			["suzerainCastleProsperityGain"] = suzerainGain.CastleProsperity,
			["suzerainCastleFoodGain"] = suzerainGain.CastleFood,
			["suzerainVillageHearthGain"] = suzerainGain.VillageHearth,
			["tributaryProsperityLoss"] = tributaryLoss.Prosperity,
			["tributaryFoodLoss"] = tributaryLoss.Food,
			["tributaryHearthLoss"] = tributaryLoss.Hearth,
			["tributaryTownProsperityLoss"] = tributaryLoss.TownProsperity,
			["tributaryTownFoodLoss"] = tributaryLoss.TownFood,
			["tributaryCastleProsperityLoss"] = tributaryLoss.CastleProsperity,
			["tributaryCastleFoodLoss"] = tributaryLoss.CastleFood,
			["tributaryVillageHearthLoss"] = tributaryLoss.VillageHearth,
			["plannedSuzerainChangeDetails"] = plannedSuzerainGain.Details,
			["suzerainChangeDetails"] = suzerainGain.Details,
			["tributaryChangeDetails"] = tributaryLoss.Details
		});
		return true;
	}

	private int GetTributaryPaymentLastSettlementDay(VassalageAgreement agreement)
	{
		string key = agreement?.AgreementId ?? "";
		if (!string.IsNullOrWhiteSpace(key) && _tributaryPaymentLastSettlementDays.TryGetValue(key, out var value))
		{
			return Math.Max(0, value);
		}
		return Math.Max(0, agreement?.CreatedDay ?? GetCurrentCampaignDay());
	}

	private void SetTributaryPaymentLastSettlementDay(VassalageAgreement agreement, int day)
	{
		string key = agreement?.AgreementId ?? "";
		if (!string.IsNullOrWhiteSpace(key))
		{
			_tributaryPaymentLastSettlementDays[key] = Math.Max(0, day);
		}
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static void RefreshKingdomCurrentStrength(Kingdom kingdom)
	{
		try
		{
			if (kingdom?.Clans == null)
			{
				return;
			}
			foreach (Clan clan in kingdom.Clans)
			{
				clan?.UpdateCurrentStrength();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[WARN] refresh kingdom strength failed: " + ex.Message);
		}
	}

	private static float ClampTributaryStrength(float strength)
	{
		if (float.IsNaN(strength) || float.IsInfinity(strength))
		{
			return 0f;
		}
		return Math.Max(0f, Math.Min(10000f, strength));
	}

	private static TributaryPaymentTier GetTributaryPaymentTier(float strength)
	{
		strength = ClampTributaryStrength(strength);
		if (strength <= 500f)
		{
			return InterpolateTributaryPaymentTier(strength, 0f, 500f,
				new TributaryPaymentTier { TownProsperity = 5, TownFood = 25, CastleProsperity = 3, CastleFood = 20, VillageHearth = 2, TierName = "微薄贡赋" },
				new TributaryPaymentTier { TownProsperity = 15, TownFood = 50, CastleProsperity = 10, CastleFood = 40, VillageHearth = 6, TierName = "小额贡赋" });
		}
		if (strength <= 1000f)
		{
			return InterpolateTributaryPaymentTier(strength, 500f, 1000f,
				new TributaryPaymentTier { TownProsperity = 15, TownFood = 50, CastleProsperity = 10, CastleFood = 40, VillageHearth = 6, TierName = "小额贡赋" },
				new TributaryPaymentTier { TownProsperity = 35, TownFood = 100, CastleProsperity = 22, CastleFood = 70, VillageHearth = 12, TierName = "稳固贡赋" });
		}
		if (strength <= 3000f)
		{
			return InterpolateTributaryPaymentTier(strength, 1000f, 3000f,
				new TributaryPaymentTier { TownProsperity = 35, TownFood = 100, CastleProsperity = 22, CastleFood = 70, VillageHearth = 12, TierName = "稳固贡赋" },
				new TributaryPaymentTier { TownProsperity = 90, TownFood = 180, CastleProsperity = 55, CastleFood = 120, VillageHearth = 28, TierName = "丰厚贡赋" });
		}
		if (strength <= 7000f)
		{
			return InterpolateTributaryPaymentTier(strength, 3000f, 7000f,
				new TributaryPaymentTier { TownProsperity = 90, TownFood = 180, CastleProsperity = 55, CastleFood = 120, VillageHearth = 28, TierName = "丰厚贡赋" },
				new TributaryPaymentTier { TownProsperity = 180, TownFood = 300, CastleProsperity = 110, CastleFood = 220, VillageHearth = 55, TierName = "巨额贡赋" });
		}
		return InterpolateTributaryPaymentTier(strength, 7000f, 10000f,
			new TributaryPaymentTier { TownProsperity = 180, TownFood = 300, CastleProsperity = 110, CastleFood = 220, VillageHearth = 55, TierName = "巨额贡赋" },
			new TributaryPaymentTier { TownProsperity = 220, TownFood = 450, CastleProsperity = 140, CastleFood = 320, VillageHearth = 75, TierName = "王国贡赋" });
	}

	private static TributaryPaymentTier InterpolateTributaryPaymentTier(float strength, float minStrength, float maxStrength, TributaryPaymentTier minTier, TributaryPaymentTier maxTier)
	{
		float range = Math.Max(1f, maxStrength - minStrength);
		float ratio = Math.Max(0f, Math.Min(1f, (strength - minStrength) / range));
		return new TributaryPaymentTier
		{
			TownProsperity = LerpTributaryPaymentValue(minTier.TownProsperity, maxTier.TownProsperity, ratio),
			TownFood = LerpTributaryPaymentValue(minTier.TownFood, maxTier.TownFood, ratio),
			CastleProsperity = LerpTributaryPaymentValue(minTier.CastleProsperity, maxTier.CastleProsperity, ratio),
			CastleFood = LerpTributaryPaymentValue(minTier.CastleFood, maxTier.CastleFood, ratio),
			VillageHearth = LerpTributaryPaymentValue(minTier.VillageHearth, maxTier.VillageHearth, ratio),
			TierName = "动态国力 " + FormatTributaryPaymentDiagnosticNumber(strength)
		};
	}

	private static int LerpTributaryPaymentValue(int minValue, int maxValue, float ratio)
	{
		float value = minValue + (maxValue - minValue) * Math.Max(0f, Math.Min(1f, ratio));
		return Math.Max(0, (int)Math.Round(value, MidpointRounding.AwayFromZero));
	}

	private static List<Settlement> GetKingdomSettlements(Kingdom kingdom)
	{
		List<Settlement> settlements = new List<Settlement>();
		if (!IsValidKingdom(kingdom))
		{
			return settlements;
		}
		try
		{
			foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
			{
				if (IsSettlementOwnedByKingdom(settlement, kingdom))
				{
					settlements.Add(settlement);
				}
			}
		}
		catch
		{
		}
		return settlements;
	}

	private static bool IsSettlementOwnedByKingdom(Settlement settlement, Kingdom kingdom)
	{
		try
		{
			return settlement != null
				&& IsValidKingdom(kingdom)
				&& (settlement.IsTown || settlement.IsCastle || settlement.IsVillage)
				&& settlement.MapFaction == kingdom;
		}
		catch
		{
			return false;
		}
	}

	private static float CalculateTributaryPaymentRatio(float requestedLoss, float actualLoss)
	{
		if (requestedLoss <= 0f)
		{
			return 1f;
		}
		return ClampTributaryPaymentRatio(actualLoss / requestedLoss);
	}

	private static float ClampTributaryPaymentRatio(float ratio)
	{
		if (float.IsNaN(ratio) || float.IsInfinity(ratio))
		{
			return 0f;
		}
		return Math.Max(0f, Math.Min(1f, ratio));
	}

	private static TributaryPaymentTotals CalculateTributaryPaymentPotentialBenefits(List<Settlement> settlements, TributaryPaymentTier tier)
	{
		TributaryPaymentTotals totals = new TributaryPaymentTotals();
		foreach (Settlement settlement in settlements ?? new List<Settlement>())
		{
			try
			{
				if ((settlement.IsTown || settlement.IsCastle) && settlement.Town != null)
				{
					Town town = settlement.Town;
					bool isCastle = settlement.IsCastle;
					if (isCastle)
					{
						totals.CastleCount++;
					}
					else
					{
						totals.TownCount++;
					}
					int prosperityGain = isCastle ? tier.CastleProsperity : tier.TownProsperity;
					int foodGain = isCastle ? tier.CastleFood : tier.TownFood;
					float prosperityBefore = town.Prosperity;
					float foodBefore = town.FoodStocks;
					float foodUpperLimit = Math.Max(0, town.FoodStocksUpperLimit());
					float plannedFoodGain = Math.Max(0f, foodGain);
					float foodCapacityRemaining = Math.Max(0f, foodUpperLimit - foodBefore);
					totals.Prosperity += Math.Max(0f, prosperityGain);
					totals.Food += plannedFoodGain;
					if (isCastle)
					{
						totals.CastleProsperity += Math.Max(0f, prosperityGain);
						totals.CastleFood += plannedFoodGain;
					}
					else
					{
						totals.TownProsperity += Math.Max(0f, prosperityGain);
						totals.TownFood += plannedFoodGain;
					}
					totals.Details.Add("benefit_plan_fortification;"
						+ VassalageDiagnosticLog.DescribeSettlement(settlement)
						+ ";isCastle=" + (isCastle ? "true" : "false")
						+ ";requestedProsperityGain=" + prosperityGain.ToString(CultureInfo.InvariantCulture)
						+ ";potentialProsperityGain=" + FormatTributaryPaymentDiagnosticNumber(prosperityGain)
						+ ";requestedFoodGain=" + foodGain.ToString(CultureInfo.InvariantCulture)
						+ ";foodBefore=" + FormatTributaryPaymentDiagnosticNumber(foodBefore)
						+ ";foodCapacityRemaining=" + FormatTributaryPaymentDiagnosticNumber(foodCapacityRemaining)
						+ ";plannedFoodGain=" + FormatTributaryPaymentDiagnosticNumber(plannedFoodGain)
						+ ";foodUpperLimit=" + FormatTributaryPaymentDiagnosticNumber(foodUpperLimit));
				}
				else if (settlement.IsVillage && settlement.Village != null)
				{
					Village village = settlement.Village;
					totals.VillageCount++;
					totals.Hearth += Math.Max(0f, tier.VillageHearth);
					totals.VillageHearth += Math.Max(0f, tier.VillageHearth);
					totals.Details.Add("benefit_plan_village;"
						+ VassalageDiagnosticLog.DescribeSettlement(settlement)
						+ ";requestedHearthGain=" + tier.VillageHearth.ToString(CultureInfo.InvariantCulture)
						+ ";potentialHearthGain=" + FormatTributaryPaymentDiagnosticNumber(tier.VillageHearth));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Vassalage", "[WARN] calculate tributary payment benefit failed: " + ex.Message);
			}
		}
		return totals;
	}

	private static TributaryPaymentTotals ApplyTributaryPaymentBenefits(List<Settlement> settlements, TributaryPaymentTier tier, float prosperityPaymentRatio, float foodPaymentRatio, float hearthPaymentRatio)
	{
		prosperityPaymentRatio = ClampTributaryPaymentRatio(prosperityPaymentRatio);
		foodPaymentRatio = ClampTributaryPaymentRatio(foodPaymentRatio);
		hearthPaymentRatio = ClampTributaryPaymentRatio(hearthPaymentRatio);
		TributaryPaymentTotals totals = new TributaryPaymentTotals();
		foreach (Settlement settlement in settlements ?? new List<Settlement>())
		{
			try
			{
				if ((settlement.IsTown || settlement.IsCastle) && settlement.Town != null)
				{
					Town town = settlement.Town;
					bool isCastle = settlement.IsCastle;
					if (isCastle)
					{
						totals.CastleCount++;
					}
					else
					{
						totals.TownCount++;
					}
					int requestedProsperityGain = isCastle ? tier.CastleProsperity : tier.TownProsperity;
					int requestedFoodGain = isCastle ? tier.CastleFood : tier.TownFood;
					float appliedProsperityGain = Math.Max(0f, requestedProsperityGain * prosperityPaymentRatio);
					float appliedFoodGain = Math.Max(0f, requestedFoodGain * foodPaymentRatio);
					float prosperityBefore = town.Prosperity;
					town.Prosperity = prosperityBefore + appliedProsperityGain;
					float prosperityActualGain = Math.Max(0f, town.Prosperity - prosperityBefore);
					totals.Prosperity += prosperityActualGain;
					float foodBefore = town.FoodStocks;
					float foodUpperLimit = Math.Max(0, town.FoodStocksUpperLimit());
					town.FoodStocks = Math.Max(0f, Math.Min(foodUpperLimit, foodBefore + appliedFoodGain));
					float foodActualGain = Math.Max(0f, town.FoodStocks - foodBefore);
					totals.Food += foodActualGain;
					if (isCastle)
					{
						totals.CastleProsperity += prosperityActualGain;
						totals.CastleFood += foodActualGain;
					}
					else
					{
						totals.TownProsperity += prosperityActualGain;
						totals.TownFood += foodActualGain;
					}
					totals.Details.Add("benefit_fortification;"
						+ VassalageDiagnosticLog.DescribeSettlement(settlement)
						+ ";isCastle=" + (isCastle ? "true" : "false")
						+ ";requestedProsperityGain=" + requestedProsperityGain.ToString(CultureInfo.InvariantCulture)
						+ ";prosperityPaymentRatio=" + FormatTributaryPaymentDiagnosticNumber(prosperityPaymentRatio)
						+ ";appliedProsperityGain=" + FormatTributaryPaymentDiagnosticNumber(appliedProsperityGain)
						+ ";prosperityBefore=" + FormatTributaryPaymentDiagnosticNumber(prosperityBefore)
						+ ";prosperityAfter=" + FormatTributaryPaymentDiagnosticNumber(town.Prosperity)
						+ ";actualProsperityGain=" + FormatTributaryPaymentDiagnosticNumber(prosperityActualGain)
						+ ";requestedFoodGain=" + requestedFoodGain.ToString(CultureInfo.InvariantCulture)
						+ ";foodPaymentRatio=" + FormatTributaryPaymentDiagnosticNumber(foodPaymentRatio)
						+ ";appliedFoodGain=" + FormatTributaryPaymentDiagnosticNumber(appliedFoodGain)
						+ ";foodBefore=" + FormatTributaryPaymentDiagnosticNumber(foodBefore)
						+ ";foodAfter=" + FormatTributaryPaymentDiagnosticNumber(town.FoodStocks)
						+ ";actualFoodGain=" + FormatTributaryPaymentDiagnosticNumber(foodActualGain)
						+ ";foodUpperLimit=" + FormatTributaryPaymentDiagnosticNumber(foodUpperLimit));
					totals.NoticeLines.Add(BuildTributaryPaymentFortificationGainLine(settlement, prosperityActualGain, foodActualGain));
				}
				else if (settlement.IsVillage && settlement.Village != null)
				{
					Village village = settlement.Village;
					totals.VillageCount++;
					float appliedHearthGain = Math.Max(0f, tier.VillageHearth * hearthPaymentRatio);
					int oldLevel = village.GetHearthLevel();
					float hearthBefore = village.Hearth;
					village.Hearth = hearthBefore + appliedHearthGain;
					float hearthActualGain = Math.Max(0f, village.Hearth - hearthBefore);
					totals.Hearth += hearthActualGain;
					totals.VillageHearth += hearthActualGain;
					if (oldLevel != village.GetHearthLevel())
					{
						settlement.Party?.SetLevelMaskIsDirty();
					}
					totals.Details.Add("benefit_village;"
						+ VassalageDiagnosticLog.DescribeSettlement(settlement)
						+ ";requestedHearthGain=" + tier.VillageHearth.ToString(CultureInfo.InvariantCulture)
						+ ";hearthPaymentRatio=" + FormatTributaryPaymentDiagnosticNumber(hearthPaymentRatio)
						+ ";appliedHearthGain=" + FormatTributaryPaymentDiagnosticNumber(appliedHearthGain)
						+ ";hearthBefore=" + FormatTributaryPaymentDiagnosticNumber(hearthBefore)
						+ ";hearthAfter=" + FormatTributaryPaymentDiagnosticNumber(village.Hearth)
						+ ";actualHearthGain=" + FormatTributaryPaymentDiagnosticNumber(hearthActualGain)
						+ ";hearthLevelBefore=" + oldLevel.ToString(CultureInfo.InvariantCulture)
						+ ";hearthLevelAfter=" + village.GetHearthLevel().ToString(CultureInfo.InvariantCulture));
					totals.NoticeLines.Add(BuildTributaryPaymentVillageGainLine(settlement, hearthActualGain));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Vassalage", "[WARN] apply tributary payment benefit failed: " + ex.Message);
			}
		}
		return totals;
	}

	private static TributaryPaymentTotals ApplyTributaryPaymentCosts(List<Settlement> settlements, TributaryPaymentTotals playerGain)
	{
		TributaryPaymentTotals totals = new TributaryPaymentTotals();
		List<Town> fortifications = new List<Town>();
		List<Village> villages = new List<Village>();
		foreach (Settlement settlement in settlements ?? new List<Settlement>())
		{
			try
			{
				if ((settlement.IsTown || settlement.IsCastle) && settlement.Town != null)
				{
					fortifications.Add(settlement.Town);
					if (settlement.IsCastle)
					{
						totals.CastleCount++;
					}
					else
					{
						totals.TownCount++;
					}
				}
				else if (settlement.IsVillage && settlement.Village != null)
				{
					villages.Add(settlement.Village);
					totals.VillageCount++;
				}
			}
			catch
			{
			}
		}
		float requestedProsperityLoss = Math.Max(0f, playerGain?.Prosperity ?? 0f) * TributaryProsperityLossRatio;
		float requestedFoodLoss = Math.Max(0f, playerGain?.Food ?? 0f) * TributaryFoodLossRatio;
		float requestedHearthLoss = Math.Max(0f, playerGain?.Hearth ?? 0f) * TributaryHearthLossRatio;
		totals.Details.Add("cost_request;prosperityLossRatio=" + FormatTributaryPaymentDiagnosticNumber(TributaryProsperityLossRatio)
			+ ";foodLossRatio=" + FormatTributaryPaymentDiagnosticNumber(TributaryFoodLossRatio)
			+ ";hearthLossRatio=" + FormatTributaryPaymentDiagnosticNumber(TributaryHearthLossRatio)
			+ ";requestedProsperityLoss=" + FormatTributaryPaymentDiagnosticNumber(requestedProsperityLoss)
			+ ";requestedFoodLoss=" + FormatTributaryPaymentDiagnosticNumber(requestedFoodLoss)
			+ ";requestedHearthLoss=" + FormatTributaryPaymentDiagnosticNumber(requestedHearthLoss)
			+ ";prosperityFloor=" + FormatTributaryPaymentDiagnosticNumber(TributaryProsperityFloor)
			+ ";hearthFloor=" + FormatTributaryPaymentDiagnosticNumber(TributaryHearthFloor));
		totals.Prosperity = ApplyProsperityLoss(fortifications, requestedProsperityLoss, totals.Details, totals);
		totals.Food = ApplyFoodLoss(fortifications, requestedFoodLoss, totals.Details, totals);
		totals.Hearth = ApplyHearthLoss(villages, requestedHearthLoss, totals.Details, totals);
		return totals;
	}

	private static float ApplyProsperityLoss(List<Town> towns, float totalLoss, List<string> details, TributaryPaymentTotals totals = null)
	{
		if (towns == null || towns.Count == 0 || totalLoss <= 0f)
		{
			return 0f;
		}
		float perTown = totalLoss / towns.Count;
		float actualLoss = 0f;
		foreach (Town town in towns)
		{
			if (town == null || town.Prosperity <= TributaryProsperityFloor)
			{
				if (town != null)
				{
					details?.Add("cost_prosperity_skipped;"
						+ VassalageDiagnosticLog.DescribeSettlement(town.Settlement)
						+ ";reason=at_or_below_floor"
						+ ";prosperity=" + FormatTributaryPaymentDiagnosticNumber(town.Prosperity)
						+ ";floor=" + FormatTributaryPaymentDiagnosticNumber(TributaryProsperityFloor));
				}
				continue;
			}
			float before = town.Prosperity;
			town.Prosperity = Math.Max(TributaryProsperityFloor, before - perTown);
			float loss = Math.Max(0f, before - town.Prosperity);
			actualLoss += loss;
			if (totals != null)
			{
				if (town.Settlement?.IsCastle == true)
				{
					totals.CastleProsperity += loss;
				}
				else
				{
					totals.TownProsperity += loss;
				}
			}
			details?.Add("cost_prosperity;"
				+ VassalageDiagnosticLog.DescribeSettlement(town.Settlement)
				+ ";requestedLoss=" + FormatTributaryPaymentDiagnosticNumber(perTown)
				+ ";prosperityBefore=" + FormatTributaryPaymentDiagnosticNumber(before)
				+ ";prosperityAfter=" + FormatTributaryPaymentDiagnosticNumber(town.Prosperity)
				+ ";actualLoss=" + FormatTributaryPaymentDiagnosticNumber(loss)
				+ ";floor=" + FormatTributaryPaymentDiagnosticNumber(TributaryProsperityFloor));
		}
		return actualLoss;
	}

	private static float ApplyFoodLoss(List<Town> towns, float totalLoss, List<string> details, TributaryPaymentTotals totals = null)
	{
		if (towns == null || towns.Count == 0 || totalLoss <= 0f)
		{
			return 0f;
		}
		float perTown = totalLoss / towns.Count;
		float actualLoss = 0f;
		foreach (Town town in towns)
		{
			if (town == null || town.FoodStocks <= 0f)
			{
				if (town != null)
				{
					details?.Add("cost_food_skipped;"
						+ VassalageDiagnosticLog.DescribeSettlement(town.Settlement)
						+ ";reason=no_food"
						+ ";foodStocks=" + FormatTributaryPaymentDiagnosticNumber(town.FoodStocks));
				}
				continue;
			}
			float before = town.FoodStocks;
			town.FoodStocks = Math.Max(0f, before - perTown);
			float loss = Math.Max(0f, before - town.FoodStocks);
			actualLoss += loss;
			if (totals != null)
			{
				if (town.Settlement?.IsCastle == true)
				{
					totals.CastleFood += loss;
				}
				else
				{
					totals.TownFood += loss;
				}
			}
			details?.Add("cost_food;"
				+ VassalageDiagnosticLog.DescribeSettlement(town.Settlement)
				+ ";requestedLoss=" + FormatTributaryPaymentDiagnosticNumber(perTown)
				+ ";foodBefore=" + FormatTributaryPaymentDiagnosticNumber(before)
				+ ";foodAfter=" + FormatTributaryPaymentDiagnosticNumber(town.FoodStocks)
				+ ";actualLoss=" + FormatTributaryPaymentDiagnosticNumber(loss));
		}
		return actualLoss;
	}

	private static float ApplyHearthLoss(List<Village> villages, float totalLoss, List<string> details, TributaryPaymentTotals totals = null)
	{
		if (villages == null || villages.Count == 0 || totalLoss <= 0f)
		{
			return 0f;
		}
		float perVillage = totalLoss / villages.Count;
		float actualLoss = 0f;
		foreach (Village village in villages)
		{
			if (village == null || village.Hearth <= TributaryHearthFloor)
			{
				if (village != null)
				{
					details?.Add("cost_hearth_skipped;"
						+ VassalageDiagnosticLog.DescribeSettlement(village.Settlement)
						+ ";reason=at_or_below_floor"
						+ ";hearth=" + FormatTributaryPaymentDiagnosticNumber(village.Hearth)
						+ ";floor=" + FormatTributaryPaymentDiagnosticNumber(TributaryHearthFloor));
				}
				continue;
			}
			int oldLevel = village.GetHearthLevel();
			float before = village.Hearth;
			village.Hearth = Math.Max(TributaryHearthFloor, before - perVillage);
			float loss = Math.Max(0f, before - village.Hearth);
			actualLoss += loss;
			if (totals != null)
			{
				totals.VillageHearth += loss;
			}
			if (oldLevel != village.GetHearthLevel())
			{
				village.Settlement?.Party?.SetLevelMaskIsDirty();
			}
			details?.Add("cost_hearth;"
				+ VassalageDiagnosticLog.DescribeSettlement(village.Settlement)
				+ ";requestedLoss=" + FormatTributaryPaymentDiagnosticNumber(perVillage)
				+ ";hearthBefore=" + FormatTributaryPaymentDiagnosticNumber(before)
				+ ";hearthAfter=" + FormatTributaryPaymentDiagnosticNumber(village.Hearth)
				+ ";actualLoss=" + FormatTributaryPaymentDiagnosticNumber(loss)
				+ ";floor=" + FormatTributaryPaymentDiagnosticNumber(TributaryHearthFloor)
				+ ";hearthLevelBefore=" + oldLevel.ToString(CultureInfo.InvariantCulture)
				+ ";hearthLevelAfter=" + village.GetHearthLevel().ToString(CultureInfo.InvariantCulture));
		}
		return actualLoss;
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		bool isApplyingVassalageDiplomacy = _isApplyingVassalageDiplomacy;
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom kingdom1 = ResolveFactionKingdom(faction1, playerKingdom);
		Kingdom kingdom2 = ResolveFactionKingdom(faction2, playerKingdom);
		bool side1IsPlayer = IsPlayerFactionForDiplomacy(faction1, kingdom1, playerKingdom);
		bool side2IsPlayer = IsPlayerFactionForDiplomacy(faction2, kingdom2, playerKingdom);
		Kingdom enemy = side1IsPlayer ? kingdom2 : (side2IsPlayer ? kingdom1 : null);
		VassalageAgreement declaringSubjectAgreement = GetPlayerVassalAgreement(kingdom1);
		VassalageAgreement targetSubjectAgreement = GetPlayerVassalAgreement(kingdom2);
		VassalageAgreement declaringNpcTributaryAgreement = GetNpcTributaryAgreement(kingdom1);
		VassalageAgreement targetNpcTributaryAgreement = GetNpcTributaryAgreement(kingdom2);
		bool involvesPlayerSubject = declaringSubjectAgreement != null || targetSubjectAgreement != null;
		bool involvesNpcTributary = declaringNpcTributaryAgreement != null || targetNpcTributaryAgreement != null;
		bool subjectSystemRelevant = enemy != null || involvesPlayerSubject || involvesNpcTributary;
		VassalageDiagnosticLog.Event("war_declared.raw", new Dictionary<string, object>
		{
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
			["faction1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["faction2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["detail"] = detail,
			["isApplyingVassalageDiplomacy"] = isApplyingVassalageDiplomacy,
			["side1IsPlayer"] = side1IsPlayer,
			["side2IsPlayer"] = side2IsPlayer,
			["playerInvolved"] = enemy != null,
			["subjectInvolved"] = involvesPlayerSubject,
			["npcTributaryInvolved"] = involvesNpcTributary,
			["subjectSystemRelevant"] = subjectSystemRelevant,
			["declaringSubjectAgreement"] = DescribeAgreementForDiagnostics(declaringSubjectAgreement),
			["targetSubjectAgreement"] = DescribeAgreementForDiagnostics(targetSubjectAgreement),
			["declaringNpcTributaryAgreement"] = DescribeAgreementForDiagnostics(declaringNpcTributaryAgreement),
			["targetNpcTributaryAgreement"] = DescribeAgreementForDiagnostics(targetNpcTributaryAgreement),
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
		});
		if (isApplyingVassalageDiplomacy)
		{
			VassalageDiagnosticLog.Event("war_declared.ignored_internal", new Dictionary<string, object>
			{
				["reason"] = playerKingdom == null || kingdom1 == null || kingdom2 == null ? "invalid_context" : "internal_vassalage_diplomacy_guard",
				["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
				["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
				["faction1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["faction2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["detail"] = detail,
				["playerInvolved"] = enemy != null,
				["subjectInvolved"] = involvesPlayerSubject,
				["subjectSystemRelevant"] = subjectSystemRelevant,
				["cascadeHandledByInternalSync"] = enemy != null
			});
			LogProtectionSkipReason("internal_vassalage_diplomacy_guard", null, null, enemy, kingdom1, kingdom2, faction1, faction2, detail, "internal_sync", true, "");
			return;
		}
		if (kingdom1 == null || kingdom2 == null)
		{
			VassalageDiagnosticLog.Event("war_declared.ignored", new Dictionary<string, object>
			{
				["reason"] = "invalid_context",
				["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
				["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
				["faction1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["faction2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["detail"] = detail
			});
			LogProtectionSkipReason("invalid_war_context", null, null, null, kingdom1, kingdom2, faction1, faction2, detail, "unknown", false, "");
			return;
		}
		if (subjectSystemRelevant)
		{
			VassalageDiagnosticLog.Event("war_declared.observed", new Dictionary<string, object>
			{
				["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
				["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
				["faction1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["faction2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["detail"] = detail,
				["playerInvolved"] = enemy != null,
				["subjectInvolved"] = involvesPlayerSubject,
				["npcTributaryInvolved"] = involvesNpcTributary
			});
		}
		bool canInferProtectionWarRole = CanInferProtectionWarRole(detail);
		if (declaringSubjectAgreement != null
			&& targetSubjectAgreement != null
			&& NormalizeVassalageType(declaringSubjectAgreement.Type) == AfVassalageType.Tributary)
		{
			LogProtectionClassify(declaringSubjectAgreement, kingdom1, kingdom2, kingdom1, kingdom2, faction1, faction2, detail, "declared_war", "subject_subject_existing_rule", canInferProtectionWarRole, false, false, "");
			LogProtectionSkipReason("tributary_declared_war_on_subject_existing_rule", declaringSubjectAgreement, kingdom1, kingdom2, kingdom1, kingdom2, faction1, faction2, detail, "declared_war", canInferProtectionWarRole, "");
			HandleTributarySubjectWarDeclared(kingdom1, kingdom2, detail);
			return;
		}
		if (enemy != null)
		{
			List<VassalageAgreement> controlledSubjects = GetControlledSubjectAgreementsForWarSync().ToList();
			VassalageDiagnosticLog.Event("war_declared.suzerain_sync_requested", new Dictionary<string, object>
			{
				["reason"] = "suzerain_war_declared_" + detail,
				["detail"] = detail,
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["controlledSubjectCount"] = controlledSubjects.Count,
				["controlledSubjects"] = controlledSubjects.Select(DescribeAgreementForDiagnostics).ToList(),
				["canApplyDiplomacyNow"] = CanApplyVassalageDiplomacyNow(),
				["pendingDiplomacySyncCountBefore"] = _pendingDiplomacySyncs.Count
			});
			BringControlledSubjectsIntoWar(enemy, "suzerain_war_declared_" + detail);
		}
		NpcTributeVassalageBehavior.Instance?.HandleWarDeclared(kingdom1, kingdom2, faction1, faction2, detail, canInferProtectionWarRole);
		foreach (VassalageAgreement agreement in GetPlayerVassalAgreements().ToList())
		{
			Kingdom vassal = agreement.ResolveVassal();
			if (!IsValidKingdom(vassal) || vassal == playerKingdom)
			{
				LogProtectionSkipReason("invalid_subject_context", agreement, vassal, null, kingdom1, kingdom2, faction1, faction2, detail, "unknown", canInferProtectionWarRole, "");
				continue;
			}
			AfVassalageType type = NormalizeVassalageType(agreement.Type);
			bool subjectIsDeclarer = kingdom1 == vassal;
			bool subjectIsTarget = kingdom2 == vassal;
			if (!subjectIsDeclarer && !subjectIsTarget)
			{
				continue;
			}
			string subjectWarRole = subjectIsTarget ? "was_declared_on" : "declared_war";
			Kingdom externalEnemy = subjectIsTarget ? kingdom1 : kingdom2;
			if (!canInferProtectionWarRole)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "cannot_classify_declarer", canInferProtectionWarRole, false, false, "");
				LogProtectionSkipReason("cannot_classify_declarer", agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, canInferProtectionWarRole, "");
				continue;
			}
			if (!IsValidKingdom(externalEnemy) || externalEnemy == playerKingdom || externalEnemy == vassal)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "invalid_external_enemy", canInferProtectionWarRole, false, false, "");
				LogProtectionSkipReason("invalid_external_enemy", agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, canInferProtectionWarRole, "");
				continue;
			}
			string pendingNoticeId = BuildProtectionNoticeId(agreement.VassalKingdomId, externalEnemy.StringId);
			bool alreadyPending = !string.IsNullOrWhiteSpace(pendingNoticeId) && _pendingProtectionNotices.ContainsKey(pendingNoticeId);
			bool alreadyProtected = TryFindActiveProtectedSubjectWar(vassal, externalEnemy, requirePlayerWar: false, out var existingProtectedKey, out var _, out var _, out var _);
			if (subjectIsDeclarer)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "subject_declared_war_skip", canInferProtectionWarRole, alreadyPending, alreadyProtected, pendingNoticeId);
				if (type == AfVassalageType.Tributary)
				{
					VassalageDiagnosticLog.Event("war_declared.tributary_autonomous_war", new Dictionary<string, object>
					{
						["agreementId"] = agreement.AgreementId,
						["type"] = agreement.Type,
						["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
						["enemy"] = VassalageDiagnosticLog.DescribeKingdom(externalEnemy),
						["detail"] = detail
					});
					LogProtectionSkipReason("tributary_declared_war_autonomous", agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, canInferProtectionWarRole, pendingNoticeId);
				}
				else
				{
					LogProtectionSkipReason("controlled_subject_declared_war_unexpected", agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, canInferProtectionWarRole, pendingNoticeId);
				}
				continue;
			}
			if (alreadyPending)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "pending_notice_already_exists", canInferProtectionWarRole, alreadyPending, alreadyProtected, pendingNoticeId);
				LogProtectionSkipReason("pending_notice_already_exists", agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, canInferProtectionWarRole, pendingNoticeId);
				continue;
			}
			if (alreadyProtected)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "protected_subject_war_already_recorded", canInferProtectionWarRole, alreadyPending, alreadyProtected, pendingNoticeId);
				LogProtectionSkipReason("protected_subject_war_already_recorded", agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, canInferProtectionWarRole, existingProtectedKey);
				continue;
			}
			if (type == AfVassalageType.Vassal)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "vassal_default_protection", canInferProtectionWarRole, alreadyPending, alreadyProtected, pendingNoticeId);
				ApplyProtectionWar(agreement, vassal, externalEnemy, "vassal_default_protection");
				QueueProtectionNotice(agreement, externalEnemy);
				continue;
			}
			if (type == AfVassalageType.Tributary || type == AfVassalageType.Garrison)
			{
				LogProtectionClassify(agreement, vassal, externalEnemy, kingdom1, kingdom2, faction1, faction2, detail, subjectWarRole, "queue_protection_notice", canInferProtectionWarRole, alreadyPending, alreadyProtected, pendingNoticeId);
				VassalageDiagnosticLog.Event("war_declared.queue_protection", new Dictionary<string, object>
				{
					["agreementId"] = agreement.AgreementId,
					["type"] = agreement.Type,
					["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(externalEnemy),
					["detail"] = detail
				});
				QueueProtectionNotice(agreement, externalEnemy);
			}
		}
	}

	private static bool CanInferProtectionWarRole(DeclareWarAction.DeclareWarDetail detail)
	{
		try
		{
			return Enum.IsDefined(typeof(DeclareWarAction.DeclareWarDetail), detail);
		}
		catch
		{
			return false;
		}
	}

	private void LogProtectionClassify(VassalageAgreement agreement, Kingdom subject, Kingdom externalEnemy, Kingdom kingdom1, Kingdom kingdom2, IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail, string subjectWarRole, string action, bool canInferDeclarer, bool alreadyPending, bool alreadyProtected, string existingKeyOrNoticeId)
	{
		AfVassalageType normalizedType = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Military);
		VassalageDiagnosticLog.Event("protection.classify", new Dictionary<string, object>
		{
			["action"] = action ?? "",
			["agreementId"] = agreement?.AgreementId ?? "",
			["type"] = agreement?.Type ?? AfVassalageType.Military,
			["normalizedType"] = normalizedType,
			["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(externalEnemy),
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
			["faction1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["faction2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["inferredDeclarer"] = VassalageDiagnosticLog.DescribeKingdom(canInferDeclarer ? kingdom1 : null),
			["inferredTarget"] = VassalageDiagnosticLog.DescribeKingdom(canInferDeclarer ? kingdom2 : null),
			["subjectWarRole"] = subjectWarRole ?? "",
			["canInferDeclarer"] = canInferDeclarer,
			["classificationBasis"] = canInferDeclarer ? "DeclareWarAction.ApplyInternal faction1/faction2 order" : "unknown_declare_war_detail",
			["alreadyPending"] = alreadyPending,
			["alreadyProtected"] = alreadyProtected,
			["existingKeyOrNoticeId"] = existingKeyOrNoticeId ?? "",
			["detail"] = detail
		});
	}

	private void LogProtectionSkipReason(string reason, VassalageAgreement agreement, Kingdom subject, Kingdom externalEnemy, Kingdom kingdom1, Kingdom kingdom2, IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail, string subjectWarRole, bool canInferDeclarer, string existingKeyOrNoticeId)
	{
		AfVassalageType normalizedType = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Military);
		VassalageDiagnosticLog.Event("protection.skip.reason", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["agreementId"] = agreement?.AgreementId ?? "",
			["type"] = agreement?.Type ?? AfVassalageType.Military,
			["normalizedType"] = normalizedType,
			["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(externalEnemy),
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
			["faction1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["faction2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["inferredDeclarer"] = VassalageDiagnosticLog.DescribeKingdom(canInferDeclarer ? kingdom1 : null),
			["inferredTarget"] = VassalageDiagnosticLog.DescribeKingdom(canInferDeclarer ? kingdom2 : null),
			["subjectWarRole"] = subjectWarRole ?? "",
			["canInferDeclarer"] = canInferDeclarer,
			["classificationBasis"] = canInferDeclarer ? "DeclareWarAction.ApplyInternal faction1/faction2 order" : "unknown_declare_war_detail",
			["existingKeyOrNoticeId"] = existingKeyOrNoticeId ?? "",
			["detail"] = detail
		});
	}

	private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		if (_isApplyingVassalageDiplomacy)
		{
			return;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom kingdom1 = ResolveFactionKingdom(faction1, playerKingdom);
		Kingdom kingdom2 = ResolveFactionKingdom(faction2, playerKingdom);
		if (kingdom1 == null || kingdom2 == null)
		{
			return;
		}
		if (TryFindProtectedSuzerainWarByParties(kingdom1, kingdom2, requireSubjectWar: true, out var suzerainProtectedKey, out var suzerainProtectedAgreement, out var protectedSuzerainForPeace, out var suzerainProtectedSubject, out var suzerainProtectedEnemy))
		{
			VassalageDiagnosticLog.Event("make_peace.protected_suzerain_anchor.detected", new Dictionary<string, object>
			{
				["protectedKey"] = suzerainProtectedKey ?? "",
				["agreementId"] = suzerainProtectedAgreement?.AgreementId ?? "",
				["type"] = suzerainProtectedAgreement?.Type ?? AfVassalageType.Tributary,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(protectedSuzerainForPeace),
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedSubject),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedEnemy),
				["subjectAtWarBeforeSync"] = IsAtWar(suzerainProtectedSubject, suzerainProtectedEnemy),
				["suzerainAtWarBeforeEventCompleted"] = IsAtWar(protectedSuzerainForPeace, suzerainProtectedEnemy),
				["protectedSubjectWarCount"] = _protectedTributaryWars.Count,
				["detail"] = detail
			});
			int syncedSubjectPeaceCount = SynchronizeProtectedTributaryPeaceForSuzerain(protectedSuzerainForPeace, suzerainProtectedEnemy, detail, "protected_subject_suzerain_peace");
			if (protectedSuzerainForPeace == playerKingdom)
			{
				SynchronizeControlledSubjectsPeaceWithEnemy(suzerainProtectedEnemy, detail, "protected_subject_suzerain_peace_sync_controlled_subject");
			}
			VassalageDiagnosticLog.Event("make_peace.protected_suzerain_anchor", new Dictionary<string, object>
			{
				["protectedKey"] = suzerainProtectedKey ?? "",
				["agreementId"] = suzerainProtectedAgreement?.AgreementId ?? "",
				["type"] = suzerainProtectedAgreement?.Type ?? AfVassalageType.Tributary,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(protectedSuzerainForPeace),
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedSubject),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(suzerainProtectedEnemy),
				["detail"] = detail,
				["syncedSubjectPeaceCount"] = syncedSubjectPeaceCount,
				["protectedSubjectWarCount"] = _protectedTributaryWars.Count
			});
			return;
		}
		if (TryFindProtectedSubjectWarByParties(kingdom1, kingdom2, requireSubjectWar: false, requirePlayerWar: false, out var protectedKey, out var protectedAgreement, out var protectedSubject, out var protectedEnemy))
		{
			Kingdom protectedSuzerain = protectedAgreement?.ResolveSuzerain();
			VassalageDiagnosticLog.Event("make_peace.protected_subject_anchor.detected", new Dictionary<string, object>
			{
				["protectedKey"] = protectedKey ?? "",
				["agreementId"] = protectedAgreement?.AgreementId ?? "",
				["type"] = protectedAgreement?.Type ?? AfVassalageType.Tributary,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(protectedSuzerain),
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(protectedSubject),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(protectedEnemy),
				["subjectAtWarBeforeEventCompleted"] = IsAtWar(protectedSubject, protectedEnemy),
				["suzerainAtWarBeforeSync"] = IsAtWar(protectedSuzerain, protectedEnemy),
				["playerAtWarBeforeSync"] = IsAtWar(playerKingdom, protectedEnemy),
				["protectedSubjectWarCount"] = _protectedTributaryWars.Count,
				["detail"] = detail
			});
			_protectedTributaryWars.Remove(protectedKey);
			int removedPendingSuzerainDeclareWar = RemovePendingDeclareWarSyncsByParties(protectedSuzerain, protectedEnemy, "protected_subject_anchor_peace_cancel_suzerain_declare");
			int removedPendingSubjectDeclareWar = RemovePendingDeclareWarSyncsByParties(protectedSubject, protectedEnemy, "protected_subject_anchor_peace_cancel_subject_declare");
			int pendingBeforePlayerPeace = _pendingDiplomacySyncs.Count;
			bool suzerainWasAtWar = IsAtWar(protectedSuzerain, protectedEnemy);
			bool suzerainPeaceAppliedNow = suzerainWasAtWar && MakePeaceIfNeeded(protectedSuzerain, protectedEnemy, "protected_subject_peace_sync_suzerain");
			if (protectedSuzerain == playerKingdom)
			{
				SynchronizeControlledSubjectsPeaceWithEnemy(protectedEnemy, detail, "protected_subject_peace_sync_controlled_subject");
			}
			VassalageDiagnosticLog.Event("make_peace.protected_subject_anchor", new Dictionary<string, object>
			{
				["protectedKey"] = protectedKey ?? "",
				["agreementId"] = protectedAgreement?.AgreementId ?? "",
				["type"] = protectedAgreement?.Type ?? AfVassalageType.Tributary,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(protectedSuzerain),
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(protectedSubject),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(protectedEnemy),
				["detail"] = detail,
				["suzerainWasAtWar"] = suzerainWasAtWar,
				["suzerainPeaceAppliedNow"] = suzerainPeaceAppliedNow,
				["suzerainPeaceQueued"] = suzerainWasAtWar && !suzerainPeaceAppliedNow && _pendingDiplomacySyncs.Count > pendingBeforePlayerPeace,
				["removedPendingSuzerainDeclareWar"] = removedPendingSuzerainDeclareWar,
				["removedPendingSubjectDeclareWar"] = removedPendingSubjectDeclareWar,
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count
			});
			return;
		}
		bool side1IsPlayer = kingdom1 == playerKingdom || IsPlayerFactionForDiplomacy(faction1, kingdom1, playerKingdom);
		bool side2IsPlayer = kingdom2 == playerKingdom || IsPlayerFactionForDiplomacy(faction2, kingdom2, playerKingdom);
		Kingdom formerEnemy = side1IsPlayer ? kingdom2 : (side2IsPlayer ? kingdom1 : null);
		if (formerEnemy == null)
		{
			return;
		}
		VassalageDiagnosticLog.Event("make_peace.observed", new Dictionary<string, object>
		{
			["rawFaction1"] = DescribeFactionForDiagnostics(faction1, kingdom1),
			["rawFaction2"] = DescribeFactionForDiagnostics(faction2, kingdom2),
			["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(formerEnemy),
			["detail"] = detail
		});
		RemovePendingDeclareWarSyncsByParties(playerKingdom, formerEnemy, "player_peace_cancel_pending_declare");
		SynchronizeControlledSubjectsPeaceWithEnemy(formerEnemy, detail, "player_peace_sync_controlled_subject");
		SynchronizeProtectedTributaryPeace(formerEnemy, detail);
	}

	private void SynchronizeControlledSubjectsPeaceWithEnemy(Kingdom formerEnemy, MakePeaceAction.MakePeaceDetail detail, string reason)
	{
		if (!IsValidKingdom(formerEnemy))
		{
			return;
		}
		int syncedOrQueued = 0;
		int removedPendingDeclareWarCount = 0;
		int removedProtectedRecordCount = 0;
		foreach (VassalageAgreement agreement in GetPlayerVassalAgreements())
		{
			AfVassalageType type = NormalizeVassalageType(agreement.Type);
			if (type != AfVassalageType.Garrison && type != AfVassalageType.Vassal)
			{
				continue;
			}
			Kingdom vassal = agreement.ResolveVassal();
			if (!IsValidKingdom(vassal) || vassal == formerEnemy)
			{
				continue;
			}
			int removedPendingDeclareWar = RemovePendingDeclareWarSyncsByParties(vassal, formerEnemy, (reason ?? "player_peace_sync_controlled_subject") + "_cancel_pending_declare");
			removedPendingDeclareWarCount += removedPendingDeclareWar;
			if (!IsAtWar(vassal, formerEnemy))
			{
				continue;
			}
			int pendingBefore = _pendingDiplomacySyncs.Count;
			bool peaceAppliedNow = MakePeaceIfNeeded(vassal, formerEnemy, reason ?? "player_peace_sync_controlled_subject");
			int removedProtectedRecords = peaceAppliedNow && !IsAtWar(vassal, formerEnemy)
				? RemoveProtectedTributaryWarByParties(vassal, formerEnemy, (reason ?? "player_peace_sync_controlled_subject") + "_protected_record_cleanup")
				: 0;
			removedProtectedRecordCount += removedProtectedRecords;
			if (peaceAppliedNow || _pendingDiplomacySyncs.Count > pendingBefore)
			{
				syncedOrQueued++;
			}
			Logger.Log("Vassalage", "Synced or queued peace vassal=" + (vassal.StringId ?? "") + " enemy=" + (formerEnemy.StringId ?? "") + " playerPeaceDetail=" + detail + " reason=" + (reason ?? "") + " appliedNow=" + peaceAppliedNow);
			VassalageDiagnosticLog.Event("make_peace.sync_vassal", new Dictionary<string, object>
			{
				["agreementId"] = agreement.AgreementId,
				["type"] = agreement.Type,
				["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(formerEnemy),
				["detail"] = detail,
				["reason"] = reason ?? "",
				["peaceAppliedNow"] = peaceAppliedNow,
				["queued"] = !peaceAppliedNow && _pendingDiplomacySyncs.Count > pendingBefore,
				["removedPendingDeclareWar"] = removedPendingDeclareWar,
				["removedProtectedRecords"] = removedProtectedRecords
			});
		}
		VassalageDiagnosticLog.Event("make_peace.sync_controlled_subjects.done", new Dictionary<string, object>
		{
			["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(formerEnemy),
			["detail"] = detail,
			["reason"] = reason ?? "",
			["syncedOrQueued"] = syncedOrQueued,
			["removedPendingDeclareWarCount"] = removedPendingDeclareWarCount,
			["removedProtectedRecordCount"] = removedProtectedRecordCount,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
		});
	}

	private void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		string id = (destroyedKingdom?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return;
		}
		HashSet<string> endedVassalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (VassalageAgreement agreement in _agreementsByVassalId.Values.Where(x => x != null
			&& (string.Equals(x.VassalKingdomId, id, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.SuzerainKingdomId, id, StringComparison.OrdinalIgnoreCase))))
		{
			string vassalId = (agreement.VassalKingdomId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(vassalId))
			{
				endedVassalIds.Add(vassalId);
			}
		}
		foreach (string key in _agreementsByVassalId.Where((KeyValuePair<string, VassalageAgreement> x) => x.Value == null || string.Equals(x.Value.VassalKingdomId, id, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Value.SuzerainKingdomId, id, StringComparison.OrdinalIgnoreCase)).Select((KeyValuePair<string, VassalageAgreement> x) => x.Key).ToList())
		{
			_agreementsByVassalId.Remove(key);
		}
		foreach (string vassalId in endedVassalIds)
		{
			string reason = string.Equals(vassalId, id, StringComparison.OrdinalIgnoreCase)
				? "目标附庸国已经灭亡"
				: "宗主国已经灭亡";
			CustomPolicyBehavior.OnVassalRelationshipEndedForExternal(vassalId, reason);
		}
		foreach (string noticeId in _pendingInfoNotices.Keys.Where((string x) => (x ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 || (_pendingInfoNotices[x] ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
		{
			RemovePendingInfoNotice(noticeId);
		}
		foreach (string noticeId in _pendingProtectionNotices.Keys.Where((string x) => (x ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
		{
			RemovePendingProtectionNotice(noticeId);
		}
		foreach (string noticeId in _pendingNpcTributaryVassalageNotices.Keys.Where((string x) => (x ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 || (_pendingNpcTributaryVassalageNotices[x] ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
		{
			RemovePendingNpcTributaryVassalageNotice(noticeId);
		}
		foreach (string noticeId in _pendingTributaryPaymentNotices.Keys.Where((string x) => (x ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
		{
			RemovePendingTributaryPaymentNotice(noticeId);
		}
		foreach (string pendingKey in _pendingDiplomacySyncs.Keys.Where((string x) => (x ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
		{
			_pendingDiplomacySyncs.Remove(pendingKey);
		}
		foreach (string protectedKey in _protectedTributaryWars.Keys.Where((string x) => (x ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 || (_protectedTributaryWars[x] ?? "").IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0).ToList())
		{
			_protectedTributaryWars.Remove(protectedKey);
		}
		_garrisonObedienceValues.Remove(id);
		_garrisonObedienceStorage.Remove(id);
		_tributaryPaymentLastSettlementDays.Remove(id);
		_tributaryPaymentLastSettlementDayStorage.Remove(id);
	}

	private void OnMapNoticeRemoved(InformationData data)
	{
		if (!ReferenceEquals(Instance, this))
		{
			return;
		}
		if (data is AnimusForgeVassalageEstablishedMapNotification established)
		{
			VassalageDiagnosticLog.Event("notice.remove_established", new Dictionary<string, object>
			{
				["agreementId"] = established.AgreementId
			});
			MarkEstablishedNoticeShown(established.AgreementId);
		}
		else if (data is AnimusForgeVassalageInfoMapNotification info)
		{
			VassalageDiagnosticLog.Event("notice.remove_info", new Dictionary<string, object>
			{
				["noticeId"] = info.NoticeId
			});
			RemovePendingInfoNotice(info.NoticeId);
		}
		else if (data is AnimusForgeVassalageProtectionMapNotification protection)
		{
			HandleProtectionNoticeDismissed(protection.NoticeId);
		}
		else if (data is AnimusForgeNpcTributaryVassalageMapNotification npcTributaryVassalage)
		{
			NpcTributeVassalageDiagnosticLog.Event("notice_remove", new Dictionary<string, object>
			{
				["noticeId"] = npcTributaryVassalage.NoticeId
			});
			RemovePendingNpcTributaryVassalageNotice(npcTributaryVassalage.NoticeId);
		}
		else if (data is AnimusForgeTributaryPaymentMapNotification payment)
		{
			VassalageDiagnosticLog.Event("tributary_payment.notice_remove", new Dictionary<string, object>
			{
				["noticeId"] = payment.NoticeId
			});
			RemovePendingTributaryPaymentNotice(payment.NoticeId);
		}
	}

	private bool TryCreatePlayerVassalage(Hero negotiatedWith, Kingdom targetKingdom, AfVassalageType type, out string statusText)
	{
		statusText = "";
		Kingdom playerKingdom = GetPlayerKingdom();
		if (!IsValidKingdom(playerKingdom) || !IsPlayerRuler(playerKingdom))
		{
			statusText = "臣属条约未签署：你必须先成为自己王国的国王。";
			VassalageDiagnosticLog.Event("agreement.create.reject", new Dictionary<string, object>
			{
				["reason"] = "player_not_ruler",
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["statusText"] = statusText
			});
			return false;
		}
		if (!IsValidKingdom(targetKingdom))
		{
			statusText = "臣属条约未签署：找不到有效的目标王国。";
			VassalageDiagnosticLog.Event("agreement.create.reject", new Dictionary<string, object>
			{
				["reason"] = "invalid_target_kingdom",
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["statusText"] = statusText
			});
			return false;
		}
		if (targetKingdom == playerKingdom)
		{
			statusText = "臣属条约未签署：你的王国不能臣服于自己。";
			VassalageDiagnosticLog.Event("agreement.create.reject", new Dictionary<string, object>
			{
				["reason"] = "same_kingdom",
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["statusText"] = statusText
			});
			return false;
		}
		if (!IsKingdomRuler(negotiatedWith, targetKingdom))
		{
			statusText = "臣属条约未签署：对方不是目标王国的国王，不能代表整个王国立约。";
			VassalageDiagnosticLog.Event("agreement.create.reject", new Dictionary<string, object>
			{
				["reason"] = "speaker_not_target_ruler",
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["statusText"] = statusText
			});
			return false;
		}
		if (_agreementsByVassalId.TryGetValue(targetKingdom.StringId ?? "", out var existing) && existing != null)
		{
			if (string.Equals(existing.SuzerainKingdomId ?? "", playerKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				return TryRevisePlayerVassalage(negotiatedWith, targetKingdom, type, existing, out statusText);
			}
			statusText = GetKingdomDisplayName(targetKingdom, "该王国") + "已经承认" + GetKingdomDisplayName(existing.ResolveSuzerain(), "宗主国") + "的宗主权。";
			VassalageDiagnosticLog.Event("agreement.create.reject", new Dictionary<string, object>
			{
				["reason"] = "existing_agreement",
				["existingAgreementId"] = existing.AgreementId,
				["existingSuzerain"] = VassalageDiagnosticLog.DescribeKingdom(existing.ResolveSuzerain()),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["statusText"] = statusText
			});
			return false;
		}
		VassalageAgreement agreement = new VassalageAgreement
		{
			SuzerainKingdomId = playerKingdom.StringId ?? "",
			VassalKingdomId = targetKingdom.StringId ?? "",
			Type = type,
			CreatedDay = Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays)),
			NegotiatedByHeroId = negotiatedWith?.StringId ?? "",
			EstablishedNoticeShown = false
		};
		bool wasAtWarWithPlayer = IsAtWar(playerKingdom, targetKingdom);
		List<Kingdom> playerEnemies = GetKingdomWarEnemies(playerKingdom).Where((Kingdom x) => x != targetKingdom).ToList();
		List<Kingdom> targetEnemies = GetKingdomWarEnemies(targetKingdom).Where((Kingdom x) => x != playerKingdom).ToList();
		_agreementsByVassalId[agreement.VassalKingdomId] = agreement;
		if (UsesSubjectIndependence(NormalizeVassalageType(agreement.Type)))
		{
			EnsureGarrisonObedience(agreement);
		}
		QueueEstablishedNotice(agreement);
		int pendingDiplomacySyncCountBefore = _pendingDiplomacySyncs.Count;
		bool canApplyDiplomacyNow = CanApplyVassalageDiplomacyNow();
		int syncedPeaceCount = 0;
		if (wasAtWarWithPlayer)
		{
			VassalageDiagnosticLog.Event("agreement.create.peace_after_notice", new Dictionary<string, object>
			{
				["agreementId"] = agreement.AgreementId,
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["type"] = type,
				["canApplyNow"] = canApplyDiplomacyNow,
				["establishedNoticeShown"] = agreement.EstablishedNoticeShown
			});
			if (MakePeaceIfNeeded(playerKingdom, targetKingdom, "agreement_sync_player_subject_peace"))
			{
				syncedPeaceCount++;
			}
		}
		AfVassalageType normalizedNewType = NormalizeVassalageType(agreement.Type);
		int queuedWarSyncCount = 0;
		int syncedWarCount = SynchronizeCurrentWarsForNewAgreement(playerKingdom, targetKingdom, playerEnemies, targetEnemies, out queuedWarSyncCount);
		int queuedDiplomacySyncCount = Math.Max(0, _pendingDiplomacySyncs.Count - pendingDiplomacySyncCountBefore);
		int otherQueuedDiplomacySyncCount = Math.Max(0, queuedDiplomacySyncCount - queuedWarSyncCount);
		bool peaceAfterAgreement = !wasAtWarWithPlayer || !IsAtWar(playerKingdom, targetKingdom);
		string warSyncStatusText = normalizedNewType == AfVassalageType.Tributary
				? ((syncedWarCount > 0 || queuedWarSyncCount > 0)
					? "宗主国已接手朝贡国现有战事：" + syncedWarCount.ToString(CultureInfo.InvariantCulture) + "项已生效，" + queuedWarSyncCount.ToString(CultureInfo.InvariantCulture) + "项将在局势安全时生效。"
					: "")
			: ((syncedWarCount > 0 ? ("共同敌国已同步 " + syncedWarCount.ToString(CultureInfo.InvariantCulture) + " 项。") : "")
				+ (queuedWarSyncCount > 0 ? (queuedWarSyncCount.ToString(CultureInfo.InvariantCulture) + " 项共同敌国将在局势安全时同步。") : ""));
		statusText = GetKingdomDisplayName(targetKingdom, "该王国") + "已经承认" + GetKingdomDisplayName(playerKingdom, "玩家王国") + "的宗主权，条约类型：" + GetVassalageTypeDisplayName(type) + "。"
			+ (normalizedNewType == AfVassalageType.Tributary ? "朝贡国按期进贡，换取宗主庇护；朝贡国不随宗主国出征。" : "")
			+ (wasAtWarWithPlayer ? (peaceAfterAgreement ? "旧日敌对已经随条约停息。" : "旧日敌对将在安全时同步停息。") : "")
			+ warSyncStatusText
			+ (otherQueuedDiplomacySyncCount > 0 ? ("另有 " + otherQueuedDiplomacySyncCount.ToString(CultureInfo.InvariantCulture) + " 项外交安排将在局势安全时生效。") : "");
		Logger.Log("Vassalage", "Agreement created suzerain=" + agreement.SuzerainKingdomId + " vassal=" + agreement.VassalKingdomId + " type=" + agreement.Type);
		VassalageDiagnosticLog.Event("agreement.create.success", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith),
			["type"] = type,
			["wasAtWarWithPlayer"] = wasAtWarWithPlayer,
			["canApplyDiplomacyNow"] = canApplyDiplomacyNow,
			["playerEnemyCount"] = playerEnemies.Count,
			["targetEnemyCount"] = targetEnemies.Count,
			["syncedPeaceCount"] = syncedPeaceCount,
			["peaceAfterAgreement"] = peaceAfterAgreement,
			["syncedWarCount"] = syncedWarCount,
			["queuedWarSyncCount"] = queuedWarSyncCount,
			["scheduledWarSyncCount"] = queuedWarSyncCount,
			["totalWarSyncCount"] = syncedWarCount + queuedWarSyncCount,
			["queuedDiplomacySyncCount"] = queuedDiplomacySyncCount,
			["otherQueuedDiplomacySyncCount"] = otherQueuedDiplomacySyncCount,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["statusText"] = statusText
		});
		return true;
	}

	private bool TryRevisePlayerVassalage(Hero negotiatedWith, Kingdom targetKingdom, AfVassalageType type, VassalageAgreement existing, out string statusText)
	{
		statusText = "";
		Kingdom playerKingdom = GetPlayerKingdom();
		if (existing == null || !existing.IsValid() || !IsValidKingdom(playerKingdom) || !IsValidKingdom(targetKingdom))
		{
			statusText = "臣属条约未改订：找不到有效的既有臣属条约。";
			return false;
		}
		AfVassalageType oldType = NormalizeVassalageType(existing.Type);
		AfVassalageType newType = NormalizeVassalageType(type);
		bool preserveSubjectIndependence = UsesSubjectIndependence(oldType) && UsesSubjectIndependence(newType);
		if (oldType == newType)
		{
			statusText = GetKingdomDisplayName(targetKingdom, "该王国") + "已经是你的" + GetVassalageTypeDisplayName(newType) + "，条约无需重复签署。";
			VassalageDiagnosticLog.Event("agreement.revise.reject", new Dictionary<string, object>
			{
				["reason"] = "same_type",
				["agreementId"] = existing.AgreementId,
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["type"] = existing.Type,
				["createdDay"] = existing.CreatedDay,
				["statusText"] = statusText
			});
			return false;
		}
		List<Kingdom> playerEnemies = GetKingdomWarEnemies(playerKingdom).Where((Kingdom x) => x != targetKingdom).ToList();
		List<Kingdom> targetEnemies = GetKingdomWarEnemies(targetKingdom).Where((Kingdom x) => x != playerKingdom).ToList();
		bool wasAtWarWithPlayer = IsAtWar(playerKingdom, targetKingdom);
		int pendingDiplomacySyncCountBefore = _pendingDiplomacySyncs.Count;
		int syncedPeaceCount = 0;
		int oldCreatedDay = Math.Max(0, existing.CreatedDay);
		int newCreatedDay = GetCurrentCampaignDay();
		int removedProtectionNoticeCount = 0;
		foreach (string noticeId in _pendingProtectionNotices.Keys.Where((string x) => NoticeBelongsToAgreement(x, existing)).ToList())
		{
			RemovePendingProtectionNotice(noticeId);
			removedProtectionNoticeCount++;
		}
		int removedTributaryStateCount = ClearTributaryPaymentStateForAgreement(existing, "agreement_revised_reset");
		int removedPendingDiplomacySyncCount = RemovePendingDiplomacySyncsForAgreement(existing, "agreement_revised_reset");
		RemoveProtectedTributaryWarsForAgreement(existing, "agreement_revised_reset");
		string obedienceKey = (existing.VassalKingdomId ?? "").Trim();
		if (!preserveSubjectIndependence && !string.IsNullOrWhiteSpace(obedienceKey))
		{
			_garrisonObedienceValues.Remove(obedienceKey);
			_garrisonObedienceStorage.Remove(obedienceKey);
		}
		existing.Type = newType;
		existing.CreatedDay = newCreatedDay;
		existing.NegotiatedByHeroId = negotiatedWith?.StringId ?? "";
		if (UsesSubjectIndependence(newType))
		{
			EnsureGarrisonObedience(existing);
		}
		if (oldType == AfVassalageType.Vassal && newType != AfVassalageType.Vassal)
		{
			CustomPolicyBehavior.OnVassalRelationshipEndedForExternal(existing.VassalKingdomId, "臣属类型已改订为" + GetVassalageTypeDisplayName(newType));
		}
		pendingDiplomacySyncCountBefore = _pendingDiplomacySyncs.Count;
		if (wasAtWarWithPlayer && MakePeaceIfNeeded(playerKingdom, targetKingdom, "agreement_revise_player_subject_peace"))
		{
			syncedPeaceCount++;
		}
		int queuedWarSyncCount = 0;
		int syncedWarCount = SynchronizeCurrentWarsForNewAgreement(playerKingdom, targetKingdom, playerEnemies, targetEnemies, out queuedWarSyncCount);
		int queuedDiplomacySyncCount = Math.Max(0, _pendingDiplomacySyncs.Count - pendingDiplomacySyncCountBefore);
		int otherQueuedDiplomacySyncCount = Math.Max(0, queuedDiplomacySyncCount - queuedWarSyncCount);
		bool peaceAfterAgreement = !wasAtWarWithPlayer || !IsAtWar(playerKingdom, targetKingdom);
		string oldTypeText = GetVassalageTypeDisplayName(oldType);
		string newTypeText = GetVassalageTypeDisplayName(newType);
		string revisedWarSyncStatusText = newType == AfVassalageType.Tributary
				? ((syncedWarCount > 0 || queuedWarSyncCount > 0)
					? "宗主国已接手朝贡国现有战事：" + syncedWarCount.ToString(CultureInfo.InvariantCulture) + "项已生效，" + queuedWarSyncCount.ToString(CultureInfo.InvariantCulture) + "项将在局势安全时生效。"
					: "")
			: ((syncedWarCount > 0 ? ("已同步双方当前战争 " + syncedWarCount.ToString(CultureInfo.InvariantCulture) + " 项。") : "")
				+ (queuedWarSyncCount > 0 ? (queuedWarSyncCount.ToString(CultureInfo.InvariantCulture) + " 项当前战争将在局势安全时同步。") : ""));
		statusText = GetKingdomDisplayName(targetKingdom, "该王国") + "的臣属条约已由" + oldTypeText + "改订为" + newTypeText + "。"
			+ (newType == AfVassalageType.Tributary ? "朝贡国按期进贡，换取宗主庇护；朝贡国不随宗主国出征。" : "")
			+ (wasAtWarWithPlayer ? (peaceAfterAgreement ? "双方已在改订后停战。" : "双方停战将在安全时同步执行。") : "")
			+ revisedWarSyncStatusText
			+ (otherQueuedDiplomacySyncCount > 0 ? ("另有 " + otherQueuedDiplomacySyncCount.ToString(CultureInfo.InvariantCulture) + " 项外交安排将在局势安全时生效。") : "");
		string summary = GetKingdomDisplayName(targetKingdom, "臣属国") + "的臣属条约已由" + oldTypeText + "改订为" + newTypeText + "。";
		string detail = "宫廷书记官已重写臣属条约：\n\n"
			+ "臣属国：" + GetKingdomDisplayName(targetKingdom, "臣属国") + "\n"
			+ "原条约类型：" + oldTypeText + "\n"
			+ "新条约类型：" + newTypeText + "\n"
			+ "原立约日：" + FormatCampaignDate(oldCreatedDay) + "\n"
			+ "新立约日：" + FormatCampaignDate(newCreatedDay) + "\n\n"
			+ "旧约随之作废：立约日重置，旧约相关贡赋记录、忠诚度与保护安排均已归档。";
		QueueInfoNotice("agreement_revised", targetKingdom, playerKingdom, "臣属条约改订", summary, detail);
		Logger.Log("Vassalage", "Agreement revised suzerain=" + existing.SuzerainKingdomId + " vassal=" + existing.VassalKingdomId + " oldType=" + oldType + " newType=" + newType);
		VassalageDiagnosticLog.Event("agreement.revise.success", new Dictionary<string, object>
		{
			["agreementId"] = existing.AgreementId,
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["negotiatedWith"] = VassalageDiagnosticLog.DescribeHero(negotiatedWith),
			["oldType"] = oldType,
			["newType"] = newType,
			["oldCreatedDay"] = oldCreatedDay,
			["createdDay"] = existing.CreatedDay,
			["removedProtectionNoticeCount"] = removedProtectionNoticeCount,
			["removedTributaryStateCount"] = removedTributaryStateCount,
			["removedPendingDiplomacySyncCount"] = removedPendingDiplomacySyncCount,
			["wasAtWarWithPlayer"] = wasAtWarWithPlayer,
			["syncedPeaceCount"] = syncedPeaceCount,
			["peaceAfterAgreement"] = peaceAfterAgreement,
			["syncedWarCount"] = syncedWarCount,
			["queuedWarSyncCount"] = queuedWarSyncCount,
			["scheduledWarSyncCount"] = queuedWarSyncCount,
			["totalWarSyncCount"] = syncedWarCount + queuedWarSyncCount,
			["queuedDiplomacySyncCount"] = queuedDiplomacySyncCount,
			["otherQueuedDiplomacySyncCount"] = otherQueuedDiplomacySyncCount,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["statusText"] = statusText
		});
		return true;
	}

	private void QueueEstablishedNotice(VassalageAgreement agreement)
	{
		if (agreement == null || !agreement.IsValid())
		{
			return;
		}
		agreement.EstablishedNoticeShown = false;
		VassalageDiagnosticLog.Event("notice.queue_established", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["canPublishNow"] = CanPublishMapNotification(),
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(agreement.ResolveVassal()),
			["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(agreement.ResolveSuzerain())
		});
		ScheduleNoticePublish();
	}

	private void QueueInfoNotice(string category, Kingdom primaryKingdom, Kingdom secondaryKingdom, string title, string summary, string detail)
	{
		string noticeId = BuildInfoNoticeId(category, primaryKingdom?.StringId, secondaryKingdom?.StringId);
		if (string.IsNullOrWhiteSpace(noticeId))
		{
			return;
		}
		VassalageInfoNoticeRecord record = new VassalageInfoNoticeRecord
		{
			NoticeId = noticeId,
			Category = (category ?? "").Trim(),
			Title = string.IsNullOrWhiteSpace(title) ? "臣属事务急报" : title.Trim(),
			Summary = string.IsNullOrWhiteSpace(summary) ? (detail ?? "") : summary.Trim(),
			Detail = string.IsNullOrWhiteSpace(detail) ? (summary ?? "") : detail.Trim(),
			CreatedDay = GetCurrentCampaignDay()
		};
		if (!record.IsValid())
		{
			return;
		}
		_pendingInfoNotices[noticeId] = JsonConvert.SerializeObject(record);
		VassalageDiagnosticLog.Event("notice.queue_info", new Dictionary<string, object>
		{
			["noticeId"] = noticeId,
			["category"] = record.Category,
			["title"] = record.Title,
			["primaryKingdom"] = VassalageDiagnosticLog.DescribeKingdom(primaryKingdom),
			["secondaryKingdom"] = VassalageDiagnosticLog.DescribeKingdom(secondaryKingdom),
			["canPublishNow"] = CanPublishMapNotification()
		});
		ScheduleNoticePublish();
	}

	private void QueueProtectionNotice(VassalageAgreement agreement, Kingdom enemy)
	{
		if (agreement == null || enemy == null || !agreement.IsValid())
		{
			return;
		}
		string noticeId = BuildProtectionNoticeId(agreement.VassalKingdomId, enemy.StringId);
		if (string.IsNullOrWhiteSpace(noticeId))
		{
			return;
		}
		_pendingProtectionNotices[noticeId] = agreement.AgreementId + "|" + (enemy.StringId ?? "");
		VassalageDiagnosticLog.Event("notice.queue_protection", new Dictionary<string, object>
		{
			["noticeId"] = noticeId,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
		});
		ScheduleNoticePublish();
	}

	private void QueueNpcTributaryVassalageNotice(VassalageAgreement agreement)
	{
		if (agreement == null || !agreement.IsValid())
		{
			return;
		}
		if (!IsNpcTributaryVassalageAgreement(agreement))
		{
			return;
		}
		string noticeId = BuildNpcTributaryVassalageNoticeId(agreement.AgreementId);
		if (string.IsNullOrWhiteSpace(noticeId))
		{
			return;
		}
		_pendingNpcTributaryVassalageNotices[noticeId] = agreement.AgreementId;
		Logger.Log("NpcTributeVassalage", "Queued agreement notice agreement=" + agreement.AgreementId + " notice=" + noticeId);
		NpcTributeVassalageDiagnosticLog.Event("notice_queue", new Dictionary<string, object>
		{
			["noticeId"] = noticeId,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["canPublishNow"] = CanPublishMapNotification(),
			["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(agreement.ResolveVassal()),
			["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(agreement.ResolveSuzerain())
		});
		ScheduleNoticePublish();
	}

	private void QueueTributaryPaymentNotice(TributaryPaymentNoticeRecord record)
	{
		StoreTributaryPaymentRecord(record, true);
	}

	private void StoreTributaryPaymentRecord(TributaryPaymentNoticeRecord record, bool queueMapNotice)
	{
		if (record == null || !record.IsValid())
		{
			return;
		}
		string noticeId = (record.NoticeId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(noticeId))
		{
			return;
		}
		string serializedRecord = JsonConvert.SerializeObject(record);
		if (queueMapNotice)
		{
			_pendingTributaryPaymentNotices[noticeId] = serializedRecord;
		}
		_tributaryPaymentHistory[noticeId] = serializedRecord;
		VassalageDiagnosticLog.Event(queueMapNotice ? "tributary_payment.notice_queue" : "tributary_payment.history_store", new Dictionary<string, object>
		{
			["noticeId"] = noticeId,
			["agreementId"] = record.AgreementId,
			["tributaryKingdomId"] = record.TributaryKingdomId,
			["settlementDay"] = record.SettlementDay,
			["queueMapNotice"] = queueMapNotice,
			["pendingTributaryPaymentCount"] = _pendingTributaryPaymentNotices.Count,
			["plannedPlayerProsperityGain"] = record.PlannedPlayerProsperityGain,
			["plannedPlayerFoodGain"] = record.PlannedPlayerFoodGain,
			["plannedPlayerHearthGain"] = record.PlannedPlayerHearthGain,
			["prosperityPaymentRatio"] = record.ProsperityPaymentRatio,
			["foodPaymentRatio"] = record.FoodPaymentRatio,
			["hearthPaymentRatio"] = record.HearthPaymentRatio,
			["playerProsperityGain"] = record.PlayerProsperityGain,
			["playerFoodGain"] = record.PlayerFoodGain,
			["playerHearthGain"] = record.PlayerHearthGain,
			["playerTownProsperityGain"] = record.PlayerTownProsperityGain,
			["playerTownFoodGain"] = record.PlayerTownFoodGain,
			["playerCastleProsperityGain"] = record.PlayerCastleProsperityGain,
			["playerCastleFoodGain"] = record.PlayerCastleFoodGain,
			["playerVillageHearthGain"] = record.PlayerVillageHearthGain,
			["tributaryProsperityLoss"] = record.TributaryProsperityLoss,
			["tributaryFoodLoss"] = record.TributaryFoodLoss,
			["tributaryHearthLoss"] = record.TributaryHearthLoss,
			["tributaryTownProsperityLoss"] = record.TributaryTownProsperityLoss,
			["tributaryTownFoodLoss"] = record.TributaryTownFoodLoss,
			["tributaryCastleProsperityLoss"] = record.TributaryCastleProsperityLoss,
			["tributaryCastleFoodLoss"] = record.TributaryCastleFoodLoss,
			["tributaryVillageHearthLoss"] = record.TributaryVillageHearthLoss,
			["tributaryPaymentHistoryCount"] = _tributaryPaymentHistory.Count
		});
		if (queueMapNotice)
		{
			ScheduleNoticePublish();
		}
	}

	private void ScheduleNoticePublish()
	{
		_nextNoticePublishRetryUtcTicks = 0L;
		TryPublishPendingNotices();
	}

	private void TryPublishPendingNotices()
	{
		lock (_noticePublishLock)
		{
			if (!HasPendingNoticeForMap())
			{
				return;
			}
			if (!CanPublishMapNotification() || !TryEnsureMapNotificationRegistered())
			{
				return;
			}
			if (HasPendingEstablishedNotice())
			{
				foreach (VassalageAgreement agreement in GetPlayerVassalAgreements().Where((VassalageAgreement x) => x != null && !x.EstablishedNoticeShown).ToList())
				{
					string agreementId = agreement.AgreementId;
					if (string.IsNullOrWhiteSpace(agreementId) || _establishedNoticesShownThisSession.Contains(agreementId))
					{
						continue;
					}
					_establishedNoticesShownThisSession.Add(agreementId);
					MBInformationManager.AddNotice(new AnimusForgeVassalageEstablishedMapNotification(agreementId, BuildEstablishedNoticeDescription(agreement)));
					VassalageDiagnosticLog.Event("notice.publish_established", new Dictionary<string, object>
					{
						["agreementId"] = agreementId,
						["type"] = agreement.Type,
						["createdDay"] = agreement.CreatedDay,
						["campaignDate"] = FormatCampaignDate(agreement.CreatedDay),
						["vassal"] = VassalageDiagnosticLog.DescribeKingdom(agreement.ResolveVassal()),
						["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(agreement.ResolveSuzerain())
					});
				}
			}
			foreach (string noticeId in _pendingNpcTributaryVassalageNotices.Keys.ToList())
			{
				if (_npcTributaryVassalageNoticesShownThisSession.Contains(noticeId))
				{
					continue;
				}
				if (!TryResolvePendingNpcTributaryVassalageNotice(noticeId, out var agreement))
				{
					RemovePendingNpcTributaryVassalageNotice(noticeId);
					continue;
				}
				_npcTributaryVassalageNoticesShownThisSession.Add(noticeId);
				MBInformationManager.AddNotice(new AnimusForgeNpcTributaryVassalageMapNotification(noticeId, BuildNpcTributaryVassalageNoticeDescription(agreement)));
				NpcTributeVassalageDiagnosticLog.Event("notice_publish", new Dictionary<string, object>
				{
					["noticeId"] = noticeId,
					["agreementId"] = agreement.AgreementId,
					["type"] = agreement.Type,
					["createdDay"] = agreement.CreatedDay,
					["campaignDate"] = FormatCampaignDate(agreement.CreatedDay),
					["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(agreement.ResolveVassal()),
					["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(agreement.ResolveSuzerain())
				});
			}
			foreach (string noticeId in _pendingInfoNotices.Keys.ToList())
			{
				if (_infoNoticesShownThisSession.Contains(noticeId))
				{
					continue;
				}
				if (!TryResolvePendingInfoNotice(noticeId, out var record))
				{
					RemovePendingInfoNotice(noticeId);
					continue;
				}
				_infoNoticesShownThisSession.Add(noticeId);
				MBInformationManager.AddNotice(new AnimusForgeVassalageInfoMapNotification(noticeId, record.Title, record.Summary));
				VassalageDiagnosticLog.Event("notice.publish_info", new Dictionary<string, object>
				{
					["noticeId"] = noticeId,
					["category"] = record.Category ?? "",
					["title"] = record.Title ?? "",
					["createdDay"] = record.CreatedDay
				});
			}
			foreach (string noticeId in _pendingProtectionNotices.Keys.ToList())
			{
				if (_protectionNoticesShownThisSession.Contains(noticeId))
				{
					continue;
				}
				if (!TryResolvePendingProtectionNotice(noticeId, out var agreement, out var vassal, out var enemy))
				{
					RemovePendingProtectionNotice(noticeId);
					continue;
				}
				_protectionNoticesShownThisSession.Add(noticeId);
				MBInformationManager.AddNotice(new AnimusForgeVassalageProtectionMapNotification(noticeId, BuildProtectionNoticeDescription(agreement, vassal, enemy)));
				VassalageDiagnosticLog.Event("notice.publish_protection", new Dictionary<string, object>
				{
					["noticeId"] = noticeId,
					["agreementId"] = agreement.AgreementId,
					["type"] = agreement.Type,
					["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
			}
			foreach (string noticeId in _pendingTributaryPaymentNotices.Keys.ToList())
			{
				if (_tributaryPaymentNoticesShownThisSession.Contains(noticeId))
				{
					continue;
				}
				if (!TryResolvePendingTributaryPaymentNotice(noticeId, out var record))
				{
					RemovePendingTributaryPaymentNotice(noticeId);
					continue;
				}
				_tributaryPaymentNoticesShownThisSession.Add(noticeId);
				MBInformationManager.AddNotice(new AnimusForgeTributaryPaymentMapNotification(noticeId, BuildTributaryPaymentNoticeDescription(record)));
				VassalageDiagnosticLog.Event("tributary_payment.notice_publish", new Dictionary<string, object>
				{
					["noticeId"] = noticeId,
					["agreementId"] = record.AgreementId,
					["tributaryKingdomId"] = record.TributaryKingdomId,
					["settlementDay"] = record.SettlementDay,
					["plannedPlayerProsperityGain"] = record.PlannedPlayerProsperityGain,
					["plannedPlayerFoodGain"] = record.PlannedPlayerFoodGain,
					["plannedPlayerHearthGain"] = record.PlannedPlayerHearthGain,
					["prosperityPaymentRatio"] = record.ProsperityPaymentRatio,
					["foodPaymentRatio"] = record.FoodPaymentRatio,
					["hearthPaymentRatio"] = record.HearthPaymentRatio,
					["playerProsperityGain"] = record.PlayerProsperityGain,
					["playerFoodGain"] = record.PlayerFoodGain,
					["playerHearthGain"] = record.PlayerHearthGain,
					["playerTownProsperityGain"] = record.PlayerTownProsperityGain,
					["playerTownFoodGain"] = record.PlayerTownFoodGain,
					["playerCastleProsperityGain"] = record.PlayerCastleProsperityGain,
					["playerCastleFoodGain"] = record.PlayerCastleFoodGain,
					["playerVillageHearthGain"] = record.PlayerVillageHearthGain,
					["tributaryProsperityLoss"] = record.TributaryProsperityLoss,
					["tributaryFoodLoss"] = record.TributaryFoodLoss,
					["tributaryHearthLoss"] = record.TributaryHearthLoss,
					["tributaryTownProsperityLoss"] = record.TributaryTownProsperityLoss,
					["tributaryTownFoodLoss"] = record.TributaryTownFoodLoss,
					["tributaryCastleProsperityLoss"] = record.TributaryCastleProsperityLoss,
					["tributaryCastleFoodLoss"] = record.TributaryCastleFoodLoss,
					["tributaryVillageHearthLoss"] = record.TributaryVillageHearthLoss
				});
			}
		}
	}

	private bool HasPendingNoticeForMap()
	{
		try
		{
			bool hasEstablished = GetPlayerVassalAgreements().Any((VassalageAgreement x) => x != null && !x.EstablishedNoticeShown && !_establishedNoticesShownThisSession.Contains(x.AgreementId));
			return hasEstablished
				|| _pendingNpcTributaryVassalageNotices.Keys.Any((string x) => !_npcTributaryVassalageNoticesShownThisSession.Contains(x))
				|| _pendingInfoNotices.Keys.Any((string x) => !_infoNoticesShownThisSession.Contains(x))
				|| _pendingProtectionNotices.Keys.Any((string x) => !_protectionNoticesShownThisSession.Contains(x))
				|| _pendingTributaryPaymentNotices.Keys.Any((string x) => !_tributaryPaymentNoticesShownThisSession.Contains(x));
		}
		catch
		{
			return false;
		}
	}

	private bool HasPendingEstablishedNotice()
	{
		try
		{
			return GetPlayerVassalAgreements().Any((VassalageAgreement x) => x != null && !x.EstablishedNoticeShown);
		}
		catch
		{
			return false;
		}
	}

	private bool TryEnsureMapNotificationRegistered()
	{
		try
		{
			MapNotificationView mapNotificationView = MapScreen.Instance?.MapNotificationView;
			if (mapNotificationView == null)
			{
				return false;
			}
			if (!ReferenceEquals(_registeredMapNotificationView, mapNotificationView))
			{
				_establishedNoticesShownThisSession.Clear();
				_infoNoticesShownThisSession.Clear();
				_protectionNoticesShownThisSession.Clear();
				_npcTributaryVassalageNoticesShownThisSession.Clear();
				_tributaryPaymentNoticesShownThisSession.Clear();
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeVassalageEstablishedMapNotification), typeof(AnimusForgeVassalageEstablishedMapNotificationItemVM));
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeNpcTributaryVassalageMapNotification), typeof(AnimusForgeNpcTributaryVassalageMapNotificationItemVM));
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeVassalageInfoMapNotification), typeof(AnimusForgeVassalageInfoMapNotificationItemVM));
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeVassalageProtectionMapNotification), typeof(AnimusForgeVassalageProtectionMapNotificationItemVM));
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeTributaryPaymentMapNotification), typeof(AnimusForgeTributaryPaymentMapNotificationItemVM));
				_registeredMapNotificationView = mapNotificationView;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[WARN] register map notification failed: " + ex.Message);
			return false;
		}
	}

	private static bool CanPublishMapNotification()
	{
		try
		{
			return Mission.Current == null && Game.Current?.GameStateManager?.ActiveState is MapState && MapScreen.Instance?.MapNotificationView != null;
		}
		catch
		{
			return false;
		}
	}

	private void ApplyProtectionWar(VassalageAgreement agreement, Kingdom vassal, Kingdom enemy, string reason)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		VassalageDiagnosticLog.Event("protection.apply.start", new Dictionary<string, object>
		{
			["agreementId"] = agreement?.AgreementId ?? "",
			["type"] = agreement?.Type ?? AfVassalageType.Military,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["alreadyAtWar"] = IsAtWar(playerKingdom, enemy),
			["reason"] = reason ?? ""
		});
		if (agreement == null || !IsValidKingdom(playerKingdom) || !IsValidKingdom(vassal) || !IsValidKingdom(enemy) || enemy == playerKingdom)
		{
			VassalageDiagnosticLog.Event("protection.apply.reject", new Dictionary<string, object>
			{
				["reason"] = "invalid_context",
				["agreementId"] = agreement?.AgreementId ?? "",
				["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom)
			});
			return;
		}
		bool alreadyAtWar = IsAtWar(playerKingdom, enemy);
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		string protectionReason = (reason ?? "").Trim();
		bool shouldRecordProtectedTributaryWar =
			(type == AfVassalageType.Tributary && string.Equals(protectionReason, "tributary_protection_accepted", StringComparison.OrdinalIgnoreCase))
			|| (type == AfVassalageType.Garrison && string.Equals(protectionReason, "garrison_protection_accepted", StringComparison.OrdinalIgnoreCase));
		string protectedWarKey = BuildProtectedTributaryWarKey(vassal.StringId, enemy.StringId);
		if (!alreadyAtWar)
		{
			DeclareWarIfNeeded(playerKingdom, enemy, reason ?? "subject_protection");
		}
		if (shouldRecordProtectedTributaryWar)
		{
			RecordProtectedTributaryWar(agreement, vassal, enemy, reason);
		}
		BringControlledSubjectsIntoWar(enemy, reason ?? "subject_protection");
		InformationManager.DisplayMessage(new InformationMessage(
			GetKingdomDisplayName(playerKingdom, "宗主国") + "履行了对" + BuildPlayerSubjectWarNoticeName(vassal, type) + "的保护义务，已对" + BuildWarNoticeKingdomName(enemy) + (alreadyAtWar ? "维持战争状态。" : "宣战。"),
			Color.FromUint(4278242559u)));
		VassalageDiagnosticLog.Event("protection.apply", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["alreadyAtWar"] = alreadyAtWar,
			["playerAtWarAfter"] = IsAtWar(playerKingdom, enemy),
			["vassalAtWarAfter"] = IsAtWar(vassal, enemy),
			["recordProtectedSubjectWar"] = shouldRecordProtectedTributaryWar,
			["protectedWarKey"] = protectedWarKey ?? "",
			["protectedSubjectWarCount"] = _protectedTributaryWars.Count,
			["reason"] = reason ?? ""
		});
	}

	private void BringControlledSubjectsIntoWar(Kingdom enemy, string reason)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		if (!IsValidKingdom(playerKingdom) || !IsValidKingdom(enemy) || enemy == playerKingdom)
		{
			VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.reject", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["rejectReason"] = "invalid_context",
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["enemyIsPlayerKingdom"] = enemy != null && enemy == playerKingdom
			});
			LogWarSyncCascade("reject", "skipped", reason, playerKingdom, null, null, enemy, "invalid_context");
			return;
		}
		List<VassalageAgreement> agreements = GetPlayerVassalAgreements().ToList();
		int controlledCandidateCount = 0;
		int skippedTributaryOrUnsupported = 0;
		int skippedInvalidVassal = 0;
		int skippedSelfEnemy = 0;
		int alreadyAtWarCount = 0;
		int declaredNowCount = 0;
		int queuedCount = 0;
		int attemptedNoChangeCount = 0;
		VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.start", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["agreementCount"] = agreements.Count,
			["agreements"] = agreements.Select(DescribeAgreementForDiagnostics).ToList(),
			["canApplyDiplomacyNow"] = CanApplyVassalageDiplomacyNow(),
			["pendingDiplomacySyncCountBefore"] = _pendingDiplomacySyncs.Count
		});
		LogWarSyncCascade("start", "started", reason, playerKingdom, null, null, enemy);
		foreach (VassalageAgreement agreement in agreements)
		{
			AfVassalageType type = NormalizeVassalageType(agreement.Type);
			if (type != AfVassalageType.Garrison && type != AfVassalageType.Vassal)
			{
				skippedTributaryOrUnsupported++;
				VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.skip", new Dictionary<string, object>
				{
					["skipReason"] = "not_controlled_subject_type",
					["reason"] = reason ?? "",
					["agreement"] = DescribeAgreementForDiagnostics(agreement),
					["type"] = agreement.Type,
					["normalizedType"] = type,
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
				LogWarSyncCascade("skip", "skipped", reason, playerKingdom, agreement, agreement.ResolveVassal(), enemy, "not_controlled_subject_type");
				continue;
			}
			controlledCandidateCount++;
			Kingdom vassal = agreement.ResolveVassal();
			if (!IsValidKingdom(vassal))
			{
				skippedInvalidVassal++;
				VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.skip", new Dictionary<string, object>
				{
					["skipReason"] = "invalid_vassal",
					["reason"] = reason ?? "",
					["agreement"] = DescribeAgreementForDiagnostics(agreement),
					["type"] = agreement.Type,
					["normalizedType"] = type,
					["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
				LogWarSyncCascade("skip", "skipped", reason, playerKingdom, agreement, vassal, enemy, "invalid_vassal");
				continue;
			}
			if (vassal == enemy)
			{
				skippedSelfEnemy++;
				VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.skip", new Dictionary<string, object>
				{
					["skipReason"] = "subject_is_enemy",
					["reason"] = reason ?? "",
					["agreement"] = DescribeAgreementForDiagnostics(agreement),
					["type"] = agreement.Type,
					["normalizedType"] = type,
					["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
				LogWarSyncCascade("skip", "skipped", reason, playerKingdom, agreement, vassal, enemy, "subject_is_enemy");
				continue;
			}
			bool wasAtWar = IsAtWar(vassal, enemy);
			if (wasAtWar)
			{
				alreadyAtWarCount++;
				VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.skip", new Dictionary<string, object>
				{
					["skipReason"] = "already_at_war",
					["reason"] = reason ?? "",
					["agreement"] = DescribeAgreementForDiagnostics(agreement),
					["type"] = agreement.Type,
					["normalizedType"] = type,
					["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
					["wasAtWar"] = true
				});
				LogWarSyncCascade("skip", "already_at_war", reason, playerKingdom, agreement, vassal, enemy, "already_at_war", atWarAfter: true);
				continue;
			}
			int pendingBefore = _pendingDiplomacySyncs.Count;
			bool declaredNow = DeclareWarIfNeeded(vassal, enemy, reason ?? "suzerain_war_sync");
			int pendingAfter = _pendingDiplomacySyncs.Count;
			bool queued = !declaredNow && pendingAfter > pendingBefore;
			bool atWarAfter = IsAtWar(vassal, enemy);
			if (declaredNow)
			{
				declaredNowCount++;
			}
			else if (queued)
			{
				queuedCount++;
			}
			else
			{
				attemptedNoChangeCount++;
			}
			VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.attempt", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["agreementId"] = agreement.AgreementId,
				["agreement"] = DescribeAgreementForDiagnostics(agreement),
				["type"] = agreement.Type,
				["normalizedType"] = type,
				["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["wasAtWar"] = wasAtWar,
				["declaredNow"] = declaredNow,
				["queued"] = queued,
				["atWarAfter"] = atWarAfter,
				["pendingDiplomacySyncCountBefore"] = pendingBefore,
				["pendingDiplomacySyncCountAfter"] = pendingAfter,
				["canApplyDiplomacyNow"] = CanApplyVassalageDiplomacyNow()
			});
			LogWarSyncCascade("attempt", declaredNow ? "declared_now" : (queued ? "queued" : "no_change"), reason, playerKingdom, agreement, vassal, enemy, declaredNow || queued ? "" : "declare_war_no_effect", declaredNow, queued, atWarAfter, pendingBefore, pendingAfter);
			if (declaredNow)
			{
				VassalageDiagnosticLog.Event("war_declared.sync_controlled_subject", new Dictionary<string, object>
				{
					["agreementId"] = agreement.AgreementId,
					["type"] = agreement.Type,
					["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
					["reason"] = reason ?? ""
				});
			}
		}
		VassalageDiagnosticLog.Event("war_declared.sync_controlled_subjects.done", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["agreementCount"] = agreements.Count,
			["controlledCandidateCount"] = controlledCandidateCount,
			["skippedTributaryOrUnsupported"] = skippedTributaryOrUnsupported,
			["skippedInvalidVassal"] = skippedInvalidVassal,
			["skippedSelfEnemy"] = skippedSelfEnemy,
			["alreadyAtWarCount"] = alreadyAtWarCount,
			["declaredNowCount"] = declaredNowCount,
			["queuedCount"] = queuedCount,
			["attemptedNoChangeCount"] = attemptedNoChangeCount,
			["pendingDiplomacySyncCountAfter"] = _pendingDiplomacySyncs.Count
		});
		VassalageDiagnosticLog.Event("war_sync.cascade", new Dictionary<string, object>
		{
			["phase"] = "done",
			["result"] = "done",
			["reason"] = reason ?? "",
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["agreementCount"] = agreements.Count,
			["controlledCandidateCount"] = controlledCandidateCount,
			["skippedTributaryOrUnsupported"] = skippedTributaryOrUnsupported,
			["skippedInvalidVassal"] = skippedInvalidVassal,
			["skippedSelfEnemy"] = skippedSelfEnemy,
			["alreadyAtWarCount"] = alreadyAtWarCount,
			["declaredNowCount"] = declaredNowCount,
			["queuedCount"] = queuedCount,
			["attemptedNoChangeCount"] = attemptedNoChangeCount,
			["pendingDiplomacySyncCountAfter"] = _pendingDiplomacySyncs.Count
		});
		int syncedControlledSubjectCount = declaredNowCount + queuedCount + alreadyAtWarCount;
		bool shouldDisplayControlledSubjectWarSyncNotice = controlledCandidateCount > 0 && syncedControlledSubjectCount > 0
			&& !((reason ?? "").Trim().EndsWith("_controlled_subject_cascade", StringComparison.OrdinalIgnoreCase));
		if (shouldDisplayControlledSubjectWarSyncNotice)
		{
			string subjectWarSyncNoticePrefix = declaredNowCount + queuedCount > 0 ? "宗主国战争状态已同步：" : "宗主国战争状态已确认：";
			InformationManager.DisplayMessage(new InformationMessage(
				subjectWarSyncNoticePrefix + "附庸国/卫戍国 " + declaredNowCount.ToString(CultureInfo.InvariantCulture) + " 个已参战，" + queuedCount.ToString(CultureInfo.InvariantCulture) + " 个将在局势安全时参战，" + alreadyAtWarCount.ToString(CultureInfo.InvariantCulture) + " 个原本已在战。",
				Color.FromUint(4278242559u)));
		}
	}

	private void CascadeControlledSubjectsForPlayerWar(Kingdom kingdom1, Kingdom kingdom2, string reason, string trigger)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom enemy = null;
		if (IsValidKingdom(playerKingdom))
		{
			if (kingdom1 == playerKingdom)
			{
				enemy = kingdom2;
			}
			else if (kingdom2 == playerKingdom)
			{
				enemy = kingdom1;
			}
		}
		if (!IsValidKingdom(enemy) || enemy == playerKingdom)
		{
			VassalageDiagnosticLog.Event("war_sync.cascade", new Dictionary<string, object>
			{
				["phase"] = "trigger",
				["result"] = "skipped",
				["skipReason"] = "not_player_suzerain_war",
				["reason"] = reason ?? "",
				["trigger"] = trigger ?? "",
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["isApplyingVassalageDiplomacy"] = _isApplyingVassalageDiplomacy
			});
			return;
		}
		VassalageDiagnosticLog.Event("war_sync.cascade", new Dictionary<string, object>
		{
			["phase"] = "trigger",
			["result"] = "triggered",
			["reason"] = reason ?? "",
			["trigger"] = trigger ?? "",
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["isApplyingVassalageDiplomacy"] = _isApplyingVassalageDiplomacy
		});
		BringControlledSubjectsIntoWar(enemy, (string.IsNullOrWhiteSpace(reason) ? "internal_player_war" : reason.Trim()) + "_controlled_subject_cascade");
	}

	private static void LogWarSyncCascade(string phase, string result, string reason, Kingdom playerKingdom, VassalageAgreement agreement, Kingdom subject, Kingdom enemy, string skipReason = "", bool declaredNow = false, bool queued = false, bool atWarAfter = false, int pendingBefore = -1, int pendingAfter = -1)
	{
		Dictionary<string, object> fields = new Dictionary<string, object>
		{
			["phase"] = phase ?? "",
			["result"] = result ?? "",
			["reason"] = reason ?? "",
			["skipReason"] = skipReason ?? "",
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["agreementId"] = agreement?.AgreementId ?? "",
			["agreement"] = DescribeAgreementForDiagnostics(agreement),
			["type"] = agreement != null ? agreement.Type.ToString() : "",
			["normalizedType"] = agreement != null ? NormalizeVassalageType(agreement.Type).ToString() : "",
			["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["declaredNow"] = declaredNow,
			["queued"] = queued,
			["atWarAfter"] = atWarAfter
		};
		if (pendingBefore >= 0)
		{
			fields["pendingDiplomacySyncCountBefore"] = pendingBefore;
		}
		if (pendingAfter >= 0)
		{
			fields["pendingDiplomacySyncCountAfter"] = pendingAfter;
		}
		VassalageDiagnosticLog.Event("war_sync.cascade", fields);
	}

	private int SynchronizeCurrentWarsForNewAgreement(Kingdom playerKingdom, Kingdom targetKingdom, List<Kingdom> playerEnemies, List<Kingdom> targetEnemies, out int queuedWarSyncCount, bool forceQueue = false)
	{
		int syncedWarCount = 0;
		queuedWarSyncCount = 0;
		int attemptedWarSyncCount = 0;
		int existingSubjectConflictPeaceCount = 0;
		VassalageAgreement targetAgreement = GetPlayerVassalAgreement(targetKingdom);
		AfVassalageType targetType = NormalizeVassalageType(targetAgreement?.Type ?? AfVassalageType.Tributary);
		if (targetType == AfVassalageType.Tributary)
		{
			foreach (Kingdom enemy in targetEnemies ?? new List<Kingdom>())
			{
				if (!IsValidKingdom(enemy) || enemy == targetKingdom || enemy == playerKingdom)
				{
					continue;
				}
				if (IsPlayerVassalKingdom(enemy))
				{
					if (MakePeaceIfNeeded(targetKingdom, enemy, "tributary_treaty_existing_subject_conflict", forceQueue))
					{
						existingSubjectConflictPeaceCount++;
					}
					continue;
				}
				string syncReason = "tributary_treaty_protection_accepted";
				int pendingBefore = _pendingDiplomacySyncs.Count;
				bool declaredNow = DeclareWarIfNeeded(playerKingdom, enemy, syncReason, forceQueue);
				bool queuedOrScheduled = !declaredNow && HasPendingDeclareWarSync(playerKingdom, enemy, syncReason);
				bool protectedRecordCreated = false;
				if (IsAtWar(targetKingdom, enemy) && (declaredNow || queuedOrScheduled || IsAtWar(playerKingdom, enemy)))
				{
					RecordProtectedTributaryWar(targetAgreement, targetKingdom, enemy, syncReason);
					protectedRecordCreated = true;
				}
				if (declaredNow)
				{
					syncedWarCount++;
				}
				else if (queuedOrScheduled)
				{
					queuedWarSyncCount++;
				}
				attemptedWarSyncCount++;
				VassalageDiagnosticLog.Event("agreement.create.sync_war.attempt", new Dictionary<string, object>
				{
					["direction"] = "suzerain_protects_tributary_existing_war",
					["reason"] = syncReason,
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
					["tributary"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
					["declaredNow"] = declaredNow,
					["queuedOrScheduled"] = queuedOrScheduled,
					["tributaryAtWarAfter"] = IsAtWar(targetKingdom, enemy),
					["playerAtWarAfter"] = IsAtWar(playerKingdom, enemy),
					["protectedRecordCreated"] = protectedRecordCreated,
					["pendingDiplomacySyncCountBefore"] = pendingBefore,
					["pendingDiplomacySyncCountAfter"] = _pendingDiplomacySyncs.Count
				});
			}
			VassalageDiagnosticLog.Event("agreement.create.sync_wars", new Dictionary<string, object>
			{
				["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["agreementId"] = targetAgreement?.AgreementId ?? "",
				["type"] = targetAgreement?.Type ?? AfVassalageType.Tributary,
				["normalizedType"] = targetType,
				["playerEnemyCount"] = playerEnemies?.Count ?? 0,
				["targetEnemyCount"] = targetEnemies?.Count ?? 0,
				["syncedWarCount"] = syncedWarCount,
				["queuedWarSyncCount"] = queuedWarSyncCount,
				["scheduledWarSyncCount"] = queuedWarSyncCount,
				["totalWarSyncCount"] = syncedWarCount + queuedWarSyncCount,
				["attemptedWarSyncCount"] = attemptedWarSyncCount,
				["existingSubjectConflictPeaceCount"] = existingSubjectConflictPeaceCount,
				["forceQueue"] = forceQueue,
				["skipReason"] = "tributary_does_not_follow_suzerain_wars",
				["policy"] = "suzerain_protects_tributary_existing_wars_only"
			});
			return syncedWarCount;
		}
		foreach (Kingdom enemy in playerEnemies ?? new List<Kingdom>())
		{
			if (!IsValidKingdom(enemy) || enemy == targetKingdom || enemy == playerKingdom || IsPlayerVassalKingdom(enemy))
			{
				continue;
			}
			string syncReason = "agreement_sync_suzerain_war";
			int pendingBefore = _pendingDiplomacySyncs.Count;
			bool declaredNow = DeclareWarIfNeeded(targetKingdom, enemy, syncReason, forceQueue);
			bool queuedOrScheduled = !declaredNow && HasPendingDeclareWarSync(targetKingdom, enemy, syncReason);
			bool protectedRecordCreated = RecordProtectedTributaryWarForSynchronizedSuzerainWarIfNeeded(targetAgreement, targetKingdom, enemy, syncReason, "agreement_create_or_revise_sync_war_attempt");
			if (declaredNow)
			{
				syncedWarCount++;
			}
			else if (queuedOrScheduled)
			{
				queuedWarSyncCount++;
			}
			attemptedWarSyncCount++;
			VassalageDiagnosticLog.Event("agreement.create.sync_war.attempt", new Dictionary<string, object>
			{
				["direction"] = "subject_follows_suzerain_war",
				["reason"] = syncReason,
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["declaredNow"] = declaredNow,
				["queuedOrScheduled"] = queuedOrScheduled,
				["subjectAtWarAfter"] = IsAtWar(targetKingdom, enemy),
				["protectedRecordCreated"] = protectedRecordCreated,
				["pendingDiplomacySyncCountBefore"] = pendingBefore,
				["pendingDiplomacySyncCountAfter"] = _pendingDiplomacySyncs.Count
			});
		}
		foreach (Kingdom enemy in targetEnemies ?? new List<Kingdom>())
		{
			if (!IsValidKingdom(enemy) || enemy == targetKingdom || enemy == playerKingdom)
			{
				continue;
			}
			if (IsPlayerVassalKingdom(enemy))
			{
				if (MakePeaceIfNeeded(targetKingdom, enemy, "agreement_sync_existing_subject_conflict", forceQueue))
				{
					existingSubjectConflictPeaceCount++;
				}
				continue;
			}
			string syncReason = "agreement_sync_subject_war";
			int pendingBefore = _pendingDiplomacySyncs.Count;
			bool declaredNow = DeclareWarIfNeeded(playerKingdom, enemy, syncReason, forceQueue);
			bool queuedOrScheduled = !declaredNow && HasPendingDeclareWarSync(playerKingdom, enemy, syncReason);
			if (declaredNow)
			{
				syncedWarCount++;
			}
			else if (queuedOrScheduled)
			{
				queuedWarSyncCount++;
			}
			attemptedWarSyncCount++;
			VassalageDiagnosticLog.Event("agreement.create.sync_war.attempt", new Dictionary<string, object>
			{
				["direction"] = "suzerain_follows_subject_war",
				["reason"] = syncReason,
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["declaredNow"] = declaredNow,
				["queuedOrScheduled"] = queuedOrScheduled,
				["pendingDiplomacySyncCountBefore"] = pendingBefore,
				["pendingDiplomacySyncCountAfter"] = _pendingDiplomacySyncs.Count
			});
		}
		VassalageDiagnosticLog.Event("agreement.create.sync_wars", new Dictionary<string, object>
		{
			["playerKingdom"] = VassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["targetKingdom"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["playerEnemyCount"] = playerEnemies?.Count ?? 0,
			["targetEnemyCount"] = targetEnemies?.Count ?? 0,
			["syncedWarCount"] = syncedWarCount,
			["queuedWarSyncCount"] = queuedWarSyncCount,
			["scheduledWarSyncCount"] = queuedWarSyncCount,
			["totalWarSyncCount"] = syncedWarCount + queuedWarSyncCount,
			["attemptedWarSyncCount"] = attemptedWarSyncCount,
			["existingSubjectConflictPeaceCount"] = existingSubjectConflictPeaceCount,
			["forceQueue"] = forceQueue
		});
		return syncedWarCount;
	}

	private bool HasPendingDeclareWarSync(Kingdom declaringKingdom, Kingdom targetKingdom, string reason)
	{
		if (!IsValidKingdom(declaringKingdom) || !IsValidKingdom(targetKingdom) || declaringKingdom == targetKingdom)
		{
			return false;
		}
		string declaringId = (declaringKingdom.StringId ?? "").Trim();
		string targetId = (targetKingdom.StringId ?? "").Trim();
		string key = BuildPendingDiplomacySyncKey("declare_war", declaringId, targetId, reason);
		return !string.IsNullOrWhiteSpace(key) && _pendingDiplomacySyncs.ContainsKey(key);
	}

	private bool DeclareWarIfNeeded(Kingdom declaringKingdom, Kingdom targetKingdom, string reason, bool forceQueue = false)
	{
		if (IsValidKingdom(declaringKingdom)
			&& IsValidKingdom(targetKingdom)
			&& declaringKingdom != targetKingdom
			&& ShouldBlockInternalWarForCurrentVassalage(declaringKingdom, targetKingdom, out string vassalageBlockReason, out var declaringAgreement, out var targetAgreement))
		{
			bool wasAtWar = IsAtWar(declaringKingdom, targetKingdom);
			int removedPendingDeclareWarCount = RemovePendingDeclareWarSyncsByParties(declaringKingdom, targetKingdom, "current_vassalage_forbids_war");
			bool peaceAppliedNow = wasAtWar && MakePeaceIfNeeded(declaringKingdom, targetKingdom, "current_vassalage_forbids_war_reconcile", forceQueue);
			bool peaceAfterReconcile = !IsAtWar(declaringKingdom, targetKingdom);
			VassalageDiagnosticLog.Event("diplomacy.declare_war.skip", new Dictionary<string, object>
			{
				["reason"] = "current_vassalage_forbids_war",
				["syncReason"] = reason ?? "",
				["vassalageBlockReason"] = vassalageBlockReason,
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["declaringAgreement"] = DescribeAgreementForDiagnostics(declaringAgreement),
				["targetAgreement"] = DescribeAgreementForDiagnostics(targetAgreement),
				["wasAtWar"] = wasAtWar,
				["peaceAppliedNow"] = peaceAppliedNow,
				["peaceAfterReconcile"] = peaceAfterReconcile,
				["removedPendingDeclareWarCount"] = removedPendingDeclareWarCount
			});
			return false;
		}
		if (!IsValidKingdom(declaringKingdom) || !IsValidKingdom(targetKingdom) || declaringKingdom == targetKingdom || IsAtWar(declaringKingdom, targetKingdom))
		{
			VassalageDiagnosticLog.Event("diplomacy.declare_war.skip", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["alreadyAtWar"] = IsAtWar(declaringKingdom, targetKingdom)
			});
			return false;
		}
		if (forceQueue || !CanApplyVassalageDiplomacyNow())
		{
			QueuePendingDeclareWarSync(declaringKingdom, targetKingdom, reason);
			return false;
		}
		VassalageDiagnosticLog.Event("diplomacy.declare_war.apply.start", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
			["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom)
		});
		ApplyVassalageDiplomacy(delegate
		{
			DeclareWarAction.ApplyByCallToWarAgreement(declaringKingdom, targetKingdom);
		});
		bool result = IsAtWar(declaringKingdom, targetKingdom);
		Kingdom playerKingdom = GetPlayerKingdom();
		if (result && IsValidKingdom(playerKingdom) && (declaringKingdom == playerKingdom || targetKingdom == playerKingdom))
		{
			CascadeControlledSubjectsForPlayerWar(declaringKingdom, targetKingdom, reason, "declare_war_if_needed_apply_done");
		}
		Logger.Log("Vassalage", "Declare war sync reason=" + (reason ?? "") + " declaring=" + (declaringKingdom.StringId ?? "") + " target=" + (targetKingdom.StringId ?? "") + " result=" + result);
		VassalageDiagnosticLog.Event("diplomacy.declare_war.apply.done", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
			["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["isAtWarAfter"] = result
		});
		return result;
	}

	private bool MakePeaceIfNeeded(Kingdom kingdom1, Kingdom kingdom2, string reason, bool forceQueue = false)
	{
		if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2) || kingdom1 == kingdom2 || !IsAtWar(kingdom1, kingdom2))
		{
			if (IsValidKingdom(kingdom1) && IsValidKingdom(kingdom2) && kingdom1 != kingdom2 && !IsAtWar(kingdom1, kingdom2))
			{
				RemovePendingDeclareWarSyncsByParties(kingdom1, kingdom2, (reason ?? "make_peace_skip") + "_already_at_peace_cancel_declare");
			}
			VassalageDiagnosticLog.Event("diplomacy.make_peace.skip", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["wasAtWar"] = IsAtWar(kingdom1, kingdom2)
			});
			return false;
		}
		if (forceQueue || !CanApplyVassalageDiplomacyNow())
		{
			QueuePendingMakePeaceSync(kingdom1, kingdom2, reason);
			return false;
		}
		VassalageDiagnosticLog.Event("diplomacy.make_peace.apply.start", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2)
		});
		ApplyVassalageDiplomacy(delegate
		{
			MakePeaceAction.Apply(kingdom1, kingdom2);
		});
		bool result = !IsAtWar(kingdom1, kingdom2);
		if (result)
		{
			RemovePendingDeclareWarSyncsByParties(kingdom1, kingdom2, (reason ?? "make_peace") + "_applied_cancel_declare");
		}
		Logger.Log("Vassalage", "Make peace sync reason=" + (reason ?? "") + " kingdom1=" + (kingdom1.StringId ?? "") + " kingdom2=" + (kingdom2.StringId ?? "") + " result=" + result);
		VassalageDiagnosticLog.Event("diplomacy.make_peace.apply.done", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["peaceAfter"] = result
		});
		return result;
	}

	private bool HandlePlayerWarAgainstSubject(VassalageAgreement agreement, DeclareWarAction.DeclareWarDetail detail)
	{
		if (agreement == null)
		{
			return false;
		}
		Kingdom vassal = agreement.ResolveVassal();
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		string detailText = detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility ? "袭击了" : "向";
		string actionText = detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility ? "的领主" : "宣战";
		BreakAgreement(agreement, "player_broke_subject_agreement", "你" + detailText + GetKingdomDisplayName(vassal, GetVassalageTypeDisplayName(type)) + actionText + "。臣属誓约已被撕毁，战争将继续。");
		return true;
	}

	private void AdjustGarrisonObedienceOrBreak(VassalageAgreement agreement, int delta, string reason, float playerStrength = 0f, float subjectStrength = 0f, float strengthRatio = 0f, float strengthAdvantage = 0f)
	{
		if (agreement == null || NormalizeVassalageType(agreement.Type) != AfVassalageType.Garrison)
		{
			return;
		}
		Kingdom vassal = agreement.ResolveVassal();
		string vassalName = GetKingdomDisplayName(vassal, "该卫戍国");
		int before = EnsureGarrisonObedience(agreement);
		int after = ClampSubjectObedienceValue(before + delta);
		int independenceBefore = IndependenceFromSubjectObedience(before);
		int independenceAfter = IndependenceFromSubjectObedience(after);
		_garrisonObedienceValues[(agreement.VassalKingdomId ?? "").Trim()] = after;
		VassalageDiagnosticLog.Event("obedience.adjust", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["before"] = before,
			["delta"] = delta,
			["after"] = after,
			["independenceBefore"] = independenceBefore,
			["independenceAfter"] = independenceAfter,
			["playerStrength"] = playerStrength,
			["subjectStrength"] = subjectStrength,
			["strengthRatio"] = strengthRatio,
			["strengthAdvantage"] = strengthAdvantage,
			["tier"] = GetSubjectObedienceTierText(after),
			["reason"] = reason ?? ""
		});
		if (TryBreakSubjectAtCurrentThreshold(agreement, reason ?? "garrison_obedience_collapsed", "卫戍国独立度变化"))
		{
			return;
		}
		TryGetSubjectIndependenceStatus(agreement, out _, out int breakawayThreshold, out int rulerRelation, out string rulerName);
		InformationManager.DisplayMessage(new InformationMessage(vassalName + "的独立度由 " + independenceBefore.ToString(CultureInfo.InvariantCulture) + " 变为 " + independenceAfter.ToString(CultureInfo.InvariantCulture) + "/100；当前脱离阈值为 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（" + rulerName + "关系 " + FormatSignedRelation(rulerRelation) + "）。", Color.FromUint(4294936661u)));
	}

	private bool TryGetDirectVassalIndependence(string vassalKingdomId, out int independence)
	{
		return TryGetDirectVassalIndependenceStatus(vassalKingdomId, out independence, out _, out _, out _);
	}

	private bool TryGetDirectVassalIndependenceStatus(string vassalKingdomId, out int independence, out int breakawayThreshold, out int rulerRelation, out string rulerName)
	{
		independence = 0;
		breakawayThreshold = CalculateSubjectBreakawayThreshold(0);
		rulerRelation = 0;
		rulerName = "无有效统治者";
		string id = (vassalKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)
			|| !_agreementsByVassalId.TryGetValue(id, out VassalageAgreement agreement)
			|| agreement == null
			|| NormalizeVassalageType(agreement.Type) != AfVassalageType.Vassal)
		{
			return false;
		}
		return TryGetSubjectIndependenceStatus(agreement, out independence, out breakawayThreshold, out rulerRelation, out rulerName);
	}

	private bool TryPrepareDirectVassalPolicyIndependence(
		string transactionId,
		string vassalKingdomId,
		int publicationCost,
		int qualityDelta,
		out VassalPolicyExternalCommitPlan plan,
		out string error)
	{
		plan = null;
		error = string.Empty;
		string normalizedTransactionId = (transactionId ?? string.Empty).Trim();
		string id = (vassalKingdomId ?? string.Empty).Trim();
		if (normalizedTransactionId.Length == 0)
		{
			error = "external transaction id is required";
			return false;
		}
		if (!_agreementsByVassalId.TryGetValue(id, out VassalageAgreement agreement)
			|| agreement == null
			|| string.IsNullOrWhiteSpace(agreement.AgreementId)
			|| NormalizeVassalageType(agreement.Type) != AfVassalageType.Vassal
			|| !TryGetSubjectIndependenceStatus(agreement, out int before, out int threshold, out _, out _))
		{
			error = "direct vassal agreement is unavailable";
			return false;
		}
		int normalizedQuality = NormalizeVassalPolicyIndependenceDelta(qualityDelta);
		int expected = ApplySubjectIndependenceChange(before, publicationCost, normalizedQuality);
		plan = new VassalPolicyExternalCommitPlan
		{
			TransactionId = normalizedTransactionId,
			IdempotencyKey = normalizedTransactionId + ":" + agreement.AgreementId,
			AgreementId = agreement.AgreementId,
			VassalKingdomId = id,
			IndependenceBefore = before,
			IndependenceExpected = expected,
			BreakawayExpected = expected >= threshold,
			PublicationCost = Math.Max(0, publicationCost),
			QualityDelta = normalizedQuality
		};
		return true;
	}

	private VassalPolicyExternalCommitObservation ObserveDirectVassalPolicyIndependence(
		VassalPolicyExternalCommitPlan plan)
	{
		VassalPolicyExternalCommitObservation observation = new VassalPolicyExternalCommitObservation();
		if (plan == null || string.IsNullOrWhiteSpace(plan.VassalKingdomId))
		{
			return observation;
		}
		if (!_agreementsByVassalId.TryGetValue(plan.VassalKingdomId, out VassalageAgreement agreement)
			|| agreement == null)
		{
			observation.Observable = true;
			observation.AgreementPresent = false;
			observation.BreakawayActual = true;
			observation.IndependenceActual = plan.IndependenceExpected;
			return observation;
		}
		observation.AgreementPresent = true;
		observation.AgreementMatches = string.Equals(
			agreement.AgreementId, plan.AgreementId, StringComparison.OrdinalIgnoreCase);
		if (!observation.AgreementMatches
			|| !TryGetSubjectIndependenceStatus(agreement, out int actual, out _, out _, out _))
		{
			return observation;
		}
		observation.Observable = true;
		observation.IndependenceActual = actual;
		return observation;
	}

	private VassalPolicyExternalCommitResult CommitDirectVassalPolicyIndependence(
		VassalPolicyExternalCommitPlan plan,
		string policyName)
	{
		if (plan == null
			|| string.IsNullOrWhiteSpace(plan.TransactionId)
			|| string.IsNullOrWhiteSpace(plan.IdempotencyKey)
			|| string.IsNullOrWhiteSpace(plan.AgreementId)
			|| string.IsNullOrWhiteSpace(plan.VassalKingdomId))
		{
			return new VassalPolicyExternalCommitResult
			{
				Kind = VassalPolicyExternalCommitResultKind.Unknown,
				Error = "external vassal commit plan is incomplete"
			};
		}
		VassalPolicyExternalCommitObservation before = ObserveDirectVassalPolicyIndependence(plan);
		if (before.Observable && ((plan.BreakawayExpected && before.BreakawayActual)
			|| (!plan.BreakawayExpected && before.AgreementMatches
				&& before.IndependenceActual == plan.IndependenceExpected)))
		{
			return new VassalPolicyExternalCommitResult
			{
				Kind = VassalPolicyExternalCommitResultKind.AlreadyCommitted,
				Observation = before
			};
		}
		if (!before.Observable || !before.AgreementMatches
			|| before.IndependenceActual != plan.IndependenceBefore)
		{
			return new VassalPolicyExternalCommitResult
			{
				Kind = before.Observable ? VassalPolicyExternalCommitResultKind.Conflict : VassalPolicyExternalCommitResultKind.Unknown,
				Observation = before,
				Error = "external vassal state no longer matches the prepared snapshot"
			};
		}
		try
		{
			bool applied = TryApplyDirectVassalPolicyIndependence(
				plan.VassalKingdomId,
				plan.PublicationCost,
				plan.QualityDelta,
				policyName,
				out _, out _, out _);
			VassalPolicyExternalCommitObservation after = ObserveDirectVassalPolicyIndependence(plan);
			bool committed = after.Observable && ((plan.BreakawayExpected && after.BreakawayActual)
				|| (!plan.BreakawayExpected && after.AgreementMatches
					&& after.IndependenceActual == plan.IndependenceExpected));
			return new VassalPolicyExternalCommitResult
			{
				Kind = committed
					? VassalPolicyExternalCommitResultKind.Committed
					: applied ? VassalPolicyExternalCommitResultKind.Conflict : VassalPolicyExternalCommitResultKind.Unchanged,
				Observation = after,
				Error = committed ? string.Empty : "external vassal mutation result does not match its prepared contract"
			};
		}
		catch (Exception ex)
		{
			VassalPolicyExternalCommitObservation after = ObserveDirectVassalPolicyIndependence(plan);
			bool committed = after.Observable && ((plan.BreakawayExpected && after.BreakawayActual)
				|| (!plan.BreakawayExpected && after.AgreementMatches
					&& after.IndependenceActual == plan.IndependenceExpected));
			return new VassalPolicyExternalCommitResult
			{
				Kind = committed ? VassalPolicyExternalCommitResultKind.Committed : VassalPolicyExternalCommitResultKind.Unknown,
				Observation = after,
				Error = ex.GetType().Name + ": " + ex.Message
			};
		}
	}

	private bool TryApplyDirectVassalPolicyIndependence(string vassalKingdomId, int publicationCost, int qualityDelta, string policyName, out int before, out int after, out bool brokeAway)
	{
		before = 0;
		after = 0;
		brokeAway = false;
		string id = (vassalKingdomId ?? "").Trim();
		if (!TryGetDirectVassalIndependence(id, out before)
			|| !_agreementsByVassalId.TryGetValue(id, out VassalageAgreement agreement)
			|| agreement == null)
		{
			return false;
		}
		int normalizedQuality = NormalizeVassalPolicyIndependenceDelta(qualityDelta);
		after = ApplySubjectIndependenceChange(before, publicationCost, normalizedQuality);
		int afterObedience = SubjectObedienceFromIndependence(after);
		_garrisonObedienceValues[id] = afterObedience;
		Kingdom vassal = agreement.ResolveVassal();
		string vassalName = GetKingdomDisplayName(vassal, "该附庸国");
		TryGetSubjectIndependenceStatus(agreement, out _, out int breakawayThreshold, out int rulerRelation, out string rulerName);
		VassalageDiagnosticLog.Event("independence.vassal_policy", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["policyName"] = policyName ?? "",
			["before"] = before,
			["publicationCost"] = Math.Max(0, publicationCost),
			["qualityDelta"] = normalizedQuality,
			["after"] = after,
			["obedienceAfter"] = afterObedience,
			["breakawayThreshold"] = breakawayThreshold,
			["rulerName"] = rulerName,
			["rulerRelation"] = rulerRelation
		});
		brokeAway = ShouldSubjectBreakAway(after, rulerRelation);
		if (brokeAway)
		{
			BreakAgreement(agreement, "vassal_policy_independence_threshold", vassalName + "的独立度升至 " + after.ToString(CultureInfo.InvariantCulture) + "/100，已达到按统治者" + rulerName + "与玩家关系 " + FormatSignedRelation(rulerRelation) + " 计算的脱离阈值 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "，该国宣布脱离宗主控制。");
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(vassalName + "的独立度由 " + before.ToString(CultureInfo.InvariantCulture) + " 变为 " + after.ToString(CultureInfo.InvariantCulture) + "；当前脱离阈值为 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（统治者关系 " + FormatSignedRelation(rulerRelation) + "）。", Color.FromUint(4294936661u)));
		}
		return true;
	}

	private bool TryGetSubjectIndependenceStatus(VassalageAgreement agreement, out int independence, out int breakawayThreshold, out int rulerRelation, out string rulerName)
	{
		independence = 0;
		breakawayThreshold = CalculateSubjectBreakawayThreshold(0);
		rulerRelation = 0;
		rulerName = "无有效统治者";
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom vassal = agreement?.ResolveVassal();
		if (agreement == null
			|| !agreement.IsValid()
			|| !UsesSubjectIndependence(NormalizeVassalageType(agreement.Type))
			|| !IsValidKingdom(playerKingdom)
			|| !string.Equals(agreement.SuzerainKingdomId ?? "", playerKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase)
			|| !IsValidKingdom(vassal))
		{
			return false;
		}
		Hero ruler = GetCurrentKingdomRuler(vassal);
		rulerRelation = GetRulerRelationToPlayer(ruler);
		breakawayThreshold = CalculateSubjectBreakawayThreshold(rulerRelation);
		rulerName = GetHeroDisplayName(ruler, "无有效统治者");
		independence = IndependenceFromSubjectObedience(EnsureGarrisonObedience(agreement));
		return true;
	}

	private bool TryBreakSubjectAtCurrentThreshold(VassalageAgreement agreement, string reason, string trigger)
	{
		if (!TryGetSubjectIndependenceStatus(agreement, out int independence, out int breakawayThreshold, out int rulerRelation, out string rulerName)
			|| !ShouldSubjectBreakAway(independence, rulerRelation))
		{
			return false;
		}
		string vassalId = (agreement.VassalKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(vassalId) || !_subjectBreakawayChecksInProgress.Add(vassalId))
		{
			return false;
		}
		try
		{
			Kingdom vassal = agreement.ResolveVassal();
			string vassalName = GetKingdomDisplayName(vassal, GetVassalageTypeDisplayName(agreement.Type));
			VassalageDiagnosticLog.Event("independence.breakaway_threshold", new Dictionary<string, object>
			{
				["agreementId"] = agreement.AgreementId,
				["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
				["trigger"] = trigger ?? "",
				["independence"] = independence,
				["breakawayThreshold"] = breakawayThreshold,
				["rulerName"] = rulerName,
				["rulerRelation"] = rulerRelation
			});
			BreakAgreement(agreement, reason ?? "subject_ruler_relation_threshold", vassalName + "当前独立度为 " + independence.ToString(CultureInfo.InvariantCulture) + "/100，已达到按统治者" + rulerName + "与玩家关系 " + FormatSignedRelation(rulerRelation) + " 计算的脱离阈值 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "，该国宣布脱离宗主控制。");
			return true;
		}
		finally
		{
			_subjectBreakawayChecksInProgress.Remove(vassalId);
		}
	}

	private static Hero GetCurrentKingdomRuler(Kingdom kingdom)
	{
		try
		{
			return kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		}
		catch
		{
			return null;
		}
	}

	private static int GetRulerRelationToPlayer(Hero ruler)
	{
		Hero player = Hero.MainHero;
		if (ruler == null || player == null)
		{
			return 0;
		}
		try
		{
			return Math.Max(SubjectRulerRelationMinValue, Math.Min(SubjectRulerRelationMaxValue, ruler.GetRelation(player)));
		}
		catch
		{
			return 0;
		}
	}

	private static string FormatSignedRelation(int relation)
	{
		return relation > 0 ? "+" + relation.ToString(CultureInfo.InvariantCulture) : relation.ToString(CultureInfo.InvariantCulture);
	}

	private void IncreaseGarrisonObedienceAfterProtection(VassalageAgreement agreement)
	{
		if (agreement == null || NormalizeVassalageType(agreement.Type) != AfVassalageType.Garrison)
		{
			return;
		}
		int delta = CalculateGarrisonProtectionSuccessDelta(agreement, out float playerStrength, out float subjectStrength, out float strengthRatio, out float strengthAdvantage);
		Kingdom vassal = agreement.ResolveVassal();
		string vassalName = GetKingdomDisplayName(vassal, "该卫戍国");
		int before = EnsureGarrisonObedience(agreement);
		int after = ClampSubjectObedienceValue(before + delta);
		int independenceBefore = IndependenceFromSubjectObedience(before);
		int independenceAfter = IndependenceFromSubjectObedience(after);
		_garrisonObedienceValues[(agreement.VassalKingdomId ?? "").Trim()] = after;
		VassalageDiagnosticLog.Event("obedience.protection_success", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["before"] = before,
			["delta"] = delta,
			["after"] = after,
			["independenceBefore"] = independenceBefore,
			["independenceAfter"] = independenceAfter,
			["playerStrength"] = playerStrength,
			["subjectStrength"] = subjectStrength,
			["strengthRatio"] = strengthRatio,
			["strengthAdvantage"] = strengthAdvantage,
			["tier"] = GetSubjectObedienceTierText(after),
			["reason"] = "garrison_protection_accepted"
		});
		if (TryBreakSubjectAtCurrentThreshold(agreement, "garrison_obedience_collapsed", "卫戍国保护履约结算"))
		{
			return;
		}
		TryGetSubjectIndependenceStatus(agreement, out _, out int breakawayThreshold, out int rulerRelation, out string rulerName);
		InformationManager.DisplayMessage(new InformationMessage(vassalName + "因宗主国履行保护义务，独立度由 " + independenceBefore.ToString(CultureInfo.InvariantCulture) + " 降至 " + independenceAfter.ToString(CultureInfo.InvariantCulture) + "/100；当前脱离阈值为 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（" + rulerName + "关系 " + FormatSignedRelation(rulerRelation) + "）。", Color.FromUint(4278242559u)));
	}

	public int BreakAgreementsForAnnexedKingdom(Kingdom annexedKingdom, string reason = "kingdom_annexation", string message = null)
	{
		int removed = 0;
		string annexedId = (annexedKingdom?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(annexedId))
		{
			return 0;
		}
		try
		{
			List<VassalageAgreement> agreements = _agreementsByVassalId.Values
				.Where((VassalageAgreement x) => x != null
					&& (string.Equals((x.VassalKingdomId ?? "").Trim(), annexedId, StringComparison.OrdinalIgnoreCase)
						|| string.Equals((x.SuzerainKingdomId ?? "").Trim(), annexedId, StringComparison.OrdinalIgnoreCase)))
				.ToList();
			foreach (VassalageAgreement agreement in agreements)
			{
				BreakAgreement(agreement, reason ?? "kingdom_annexation", message ?? (GetKingdomDisplayName(annexedKingdom, "目标王国") + "已并入你的王国，旧臣属条约随之作废。"));
				removed++;
			}
			VassalageDiagnosticLog.Event("agreement.break_annexed_kingdom", new Dictionary<string, object>
			{
				["annexedKingdom"] = VassalageDiagnosticLog.DescribeKingdom(annexedKingdom),
				["reason"] = reason ?? "kingdom_annexation",
				["removedCount"] = removed
			});
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "BreakAgreementsForAnnexedKingdom failed: " + ex.Message);
		}
		return removed;
	}

	private void BreakAgreement(VassalageAgreement agreement, string reason, string message)
	{
		if (agreement == null)
		{
			return;
		}
		string vassalId = (agreement.VassalKingdomId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(vassalId))
		{
			_agreementsByVassalId.Remove(vassalId);
			_garrisonObedienceValues.Remove(vassalId);
			_garrisonObedienceStorage.Remove(vassalId);
			CustomPolicyBehavior.OnVassalRelationshipEndedForExternal(vassalId, reason ?? "臣属关系终止");
		}
		foreach (string noticeId in _pendingProtectionNotices.Keys.Where((string x) => NoticeBelongsToAgreement(x, agreement)).ToList())
		{
			RemovePendingProtectionNotice(noticeId);
		}
		foreach (string noticeId in _pendingNpcTributaryVassalageNotices.Keys.Where((string x) => NoticeBelongsToAgreement(x, agreement)).ToList())
		{
			RemovePendingNpcTributaryVassalageNotice(noticeId);
		}
		ClearTributaryPaymentStateForAgreement(agreement, reason ?? "agreement_broken");
		int removedPendingDiplomacySyncs = RemovePendingDiplomacySyncsForAgreement(agreement, reason ?? "agreement_broken");
		RemoveProtectedTributaryWarsForAgreement(agreement, reason ?? "agreement_broken");
		if (!string.IsNullOrWhiteSpace(message))
		{
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(4294936661u)));
		}
		if (!string.IsNullOrWhiteSpace(message))
		{
			QueueAgreementBrokenNotice(agreement, reason, message);
		}
		Logger.Log("Vassalage", "Agreement broken reason=" + (reason ?? "") + " agreement=" + (agreement.AgreementId ?? ""));
		VassalageDiagnosticLog.Event("agreement.break", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["vassalId"] = vassalId,
			["type"] = agreement.Type,
			["reason"] = reason ?? "",
			["message"] = message ?? "",
			["removedPendingDiplomacySyncCount"] = removedPendingDiplomacySyncs,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
		});
	}

	private void QueueAgreementBrokenNotice(VassalageAgreement agreement, string reason, string message)
	{
		if (agreement == null || !agreement.IsValid())
		{
			return;
		}
		Kingdom suzerain = agreement.ResolveSuzerain();
		Kingdom vassal = agreement.ResolveVassal();
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		string vassalName = GetKingdomDisplayName(vassal, GetVassalageTypeDisplayName(type));
		string typeName = GetVassalageTypeDisplayName(type);
		string summary = vassalName + "的" + typeName + "条约已经终止。";
		string detail = "宫廷急报：\n\n"
			+ "臣属国：" + vassalName + "\n"
			+ "条约类型：" + typeName + "\n"
			+ "立约日：" + FormatCampaignDate(agreement.CreatedDay) + "\n"
			+ "终止日：" + FormatCampaignDate(GetCurrentCampaignDay()) + "\n"
			+ "终止原因：" + GetAgreementBreakReasonDisplayText(reason) + "\n\n"
			+ message.Trim();
		VassalageDiagnosticLog.Event("notice.queue_agreement_broken", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["reason"] = reason ?? "",
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(suzerain),
			["canPublishNow"] = CanPublishMapNotification()
		});
		QueueInfoNotice("agreement_broken", vassal, suzerain, "臣属条约终止", summary, detail);
	}

	private static string GetAgreementBreakReasonDisplayText(string reason)
	{
		switch ((reason ?? "").Trim())
		{
		case "player_broke_subject_agreement":
			return "宗主国主动撕毁臣属誓约";
		case "tributary_protection_refused":
			return "宗主国拒绝履行朝贡庇护";
		case "tributary_protection_dismissed":
			return "宗主国未回应朝贡国求援";
		case "garrison_obedience_collapsed":
			return "卫戍国独立度达到脱离阈值";
		case "vassal_policy_independence_threshold":
			return "附庸国独立度达到脱离阈值";
		case "subject_ruler_relation_threshold":
			return "独立度达到当前统治者关系对应的脱离阈值";
		case "kingdom_annexation":
			return "王国被吞并";
		default:
			return string.IsNullOrWhiteSpace(reason) ? "臣属关系终止" : reason.Trim();
		}
	}

	private void HandleTributarySubjectWarDeclared(Kingdom declaringKingdom, Kingdom targetKingdom, DeclareWarAction.DeclareWarDetail detail)
	{
		VassalageAgreement declaringAgreement = GetPlayerVassalAgreement(declaringKingdom);
		VassalageAgreement targetAgreement = GetPlayerVassalAgreement(targetKingdom);
		AfVassalageType declaringType = NormalizeVassalageType(declaringAgreement?.Type ?? AfVassalageType.Vassal);
		AfVassalageType targetType = NormalizeVassalageType(targetAgreement?.Type ?? AfVassalageType.Vassal);
		if (declaringAgreement == null || targetAgreement == null || declaringType != AfVassalageType.Tributary)
		{
			return;
		}
		string declaringName = GetKingdomDisplayName(declaringKingdom, "朝贡国");
		string targetName = GetKingdomDisplayName(targetKingdom, "臣属国");
		if (targetType == AfVassalageType.Garrison || targetType == AfVassalageType.Vassal)
		{
			Kingdom playerKingdom = GetPlayerKingdom();
			string declaringAgreementId = declaringAgreement.AgreementId;
			BreakAgreement(declaringAgreement, "tributary_declared_war_on_controlled_subject", "");
			DeclareWarIfNeeded(playerKingdom, declaringKingdom, "tributary_declared_war_on_controlled_subject");
			BringControlledSubjectsIntoWar(declaringKingdom, "tributary_declared_war_on_controlled_subject");
			string declaringSubjectText = BuildPlayerSubjectWarNoticeName(declaringKingdom, declaringType);
			string targetSubjectText = BuildPlayerSubjectWarNoticeName(targetKingdom, targetType);
			string summary = declaringSubjectText + "宣战了" + targetSubjectText + "；朝贡条约已经终止，宗主国已自动维护臣属秩序。";
			string detailText = "边境急报：\n\n"
				+ "背约方：" + declaringSubjectText + "\n"
				+ "被宣战方：" + targetSubjectText + "\n\n"
				+ declaringName + "主动攻击受宗主控制的臣属国，朝贡条约已经终止。\n"
				+ GetKingdomDisplayName(playerKingdom, "玩家王国") + "已经自动保护" + targetName + "，并号令所有卫戍国与附庸国共同对原朝贡国开战。";
			QueueInfoNotice("tributary_controlled_subject_war", declaringKingdom, targetKingdom, "朝贡国背约", summary, detailText);
			VassalageDiagnosticLog.Event("war_declared.tributary_controlled_subject", new Dictionary<string, object>
			{
				["reason"] = "tributary_declared_war_on_controlled_subject",
				["declaringAgreementId"] = declaringAgreementId,
				["targetAgreementId"] = targetAgreement.AgreementId,
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["targetType"] = targetAgreement?.Type ?? AfVassalageType.Tributary,
				["playerAtWarAfter"] = IsAtWar(playerKingdom, declaringKingdom),
				["targetAtWarAfter"] = IsAtWar(targetKingdom, declaringKingdom),
				["detail"] = detail
			});
			return;
		}
		if (targetType == AfVassalageType.Tributary)
		{
			string declaringSubjectText = BuildPlayerSubjectWarNoticeName(declaringKingdom, declaringType);
			string targetSubjectText = BuildPlayerSubjectWarNoticeName(targetKingdom, targetType);
			string summary = declaringSubjectText + "宣战了" + targetSubjectText + "；此为朝贡国之间的自主战争，宗主国不介入。";
			string detailText = "边境急报：\n\n"
				+ "宣战方：" + declaringSubjectText + "\n"
				+ "被宣战方：" + targetSubjectText + "\n\n"
				+ "两个朝贡国均保留军事自主权。此战被视为朝贡国之间的自主战争，宗主国不会自动保护、不会自动参战，也不会解除任何朝贡条约。";
			QueueInfoNotice("tributary_tributary_war", declaringKingdom, targetKingdom, "朝贡国互相开战", summary, detailText);
			VassalageDiagnosticLog.Event("war_declared.tributary_tributary_notice", new Dictionary<string, object>
			{
				["reason"] = "tributary_declared_war_on_tributary_notice_only",
				["declaringAgreementId"] = declaringAgreement.AgreementId,
				["targetAgreementId"] = targetAgreement.AgreementId,
				["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["targetType"] = targetAgreement?.Type ?? AfVassalageType.Tributary,
				["detail"] = detail
			});
		}
	}

	private int ClearTributaryPaymentStateForAgreement(VassalageAgreement agreement, string reason)
	{
		if (agreement == null)
		{
			return 0;
		}
		int removedNoticeCount = 0;
		foreach (string noticeId in _pendingTributaryPaymentNotices.Keys.Where((string x) => NoticeBelongsToAgreement(x, agreement)).ToList())
		{
			RemovePendingTributaryPaymentNotice(noticeId);
			removedNoticeCount++;
		}
		int removedHistoryCount = ClearTributaryPaymentHistoryForAgreement(agreement);
		string agreementId = agreement.AgreementId ?? "";
		bool removedLastSettlementDay = false;
		if (!string.IsNullOrWhiteSpace(agreementId))
		{
			removedLastSettlementDay = _tributaryPaymentLastSettlementDays.Remove(agreementId) | _tributaryPaymentLastSettlementDayStorage.Remove(agreementId);
		}
		VassalageDiagnosticLog.Event("tributary_payment.state_clear", new Dictionary<string, object>
		{
			["agreementId"] = agreementId,
			["reason"] = reason ?? "",
			["removedNoticeCount"] = removedNoticeCount,
			["removedHistoryCount"] = removedHistoryCount,
			["removedLastSettlementDay"] = removedLastSettlementDay
		});
		return removedNoticeCount + removedHistoryCount;
	}

	private int ClearTributaryPaymentHistoryForAgreement(VassalageAgreement agreement)
	{
		if (agreement == null)
		{
			return 0;
		}
		string agreementId = (agreement.AgreementId ?? "").Trim();
		string vassalId = (agreement.VassalKingdomId ?? "").Trim();
		int removed = 0;
		foreach (KeyValuePair<string, string> item in _tributaryPaymentHistory.ToList())
		{
			bool belongs = false;
			if (TryDeserializeTributaryPaymentRecord(item.Key, item.Value, out var record))
			{
				belongs = string.Equals(record.AgreementId ?? "", agreementId, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(record.TributaryKingdomId ?? "", vassalId, StringComparison.OrdinalIgnoreCase);
			}
			else if (!string.IsNullOrWhiteSpace(vassalId))
			{
				belongs = (item.Key ?? "").IndexOf(vassalId, StringComparison.OrdinalIgnoreCase) >= 0;
			}
			if (belongs)
			{
				_tributaryPaymentHistory.Remove(item.Key);
				removed++;
			}
		}
		return removed;
	}

	private int RemovePendingDiplomacySyncsForAgreement(VassalageAgreement agreement, string breakReason)
	{
		string agreementId = agreement?.AgreementId ?? "";
		string vassalId = (agreement?.VassalKingdomId ?? "").Trim();
		int removed = 0;
		if (!string.IsNullOrWhiteSpace(vassalId) && _pendingDiplomacySyncs.Count > 0)
		{
			foreach (KeyValuePair<string, string> item in _pendingDiplomacySyncs.ToList())
			{
				string[] parts = (item.Value ?? "").Split('|');
				if (parts.Length < 3)
				{
					continue;
				}
				string action = (parts[0] ?? "").Trim();
				if (!string.Equals(action, "declare_war", StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(action, "make_peace", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string kingdom1Id = (parts[1] ?? "").Trim();
				string kingdom2Id = (parts[2] ?? "").Trim();
				if (!string.Equals(kingdom1Id, vassalId, StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(kingdom2Id, vassalId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (_pendingDiplomacySyncs.Remove(item.Key))
				{
					removed++;
				}
			}
		}
		VassalageDiagnosticLog.Event("pending_diplomacy.remove_for_broken_agreement", new Dictionary<string, object>
		{
			["agreementId"] = agreementId,
			["vassalId"] = vassalId,
			["breakReason"] = breakReason ?? "",
			["removedCount"] = removed,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
		});
		return removed;
	}

	private bool NoticeBelongsToAgreement(string noticeId, VassalageAgreement agreement)
	{
		if (agreement == null)
		{
			return false;
		}
		string key = (noticeId ?? "").Trim();
		string agreementId = agreement.AgreementId ?? "";
		if (_pendingProtectionNotices.TryGetValue(key, out var protectionValue) && (protectionValue ?? "").StartsWith(agreementId + "|", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (_pendingNpcTributaryVassalageNotices.TryGetValue(key, out var npcTributaryVassalageValue)
			&& string.Equals(npcTributaryVassalageValue ?? "", agreementId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (_pendingTributaryPaymentNotices.TryGetValue(key, out var tributaryPaymentValue))
		{
			try
			{
				TributaryPaymentNoticeRecord record = JsonConvert.DeserializeObject<TributaryPaymentNoticeRecord>(tributaryPaymentValue ?? "");
				if (record != null && string.Equals(record.AgreementId ?? "", agreementId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private void ApplyVassalageDiplomacy(Action action)
	{
		if (action == null)
		{
			return;
		}
		bool old = _isApplyingVassalageDiplomacy;
		_isApplyingVassalageDiplomacy = true;
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[ERROR] diplomacy action failed: " + ex);
			VassalageDiagnosticLog.Event("diplomacy.action.error", new Dictionary<string, object>
			{
				["exception"] = ex.ToString()
			});
		}
		finally
		{
			_isApplyingVassalageDiplomacy = old;
		}
	}

	private bool CanApplyVassalageDiplomacyNow()
	{
		try
		{
			if (MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Mission.Current != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static string BuildPendingDiplomacySyncKey(string action, string faction1Id, string faction2Id, string reason)
	{
		return ((action ?? "").Trim().ToLowerInvariant() + ":" + (faction1Id ?? "").Trim() + ":" + (faction2Id ?? "").Trim() + ":" + (reason ?? "").Trim()).Trim();
	}

	private void QueuePendingDeclareWarSync(Kingdom declaringKingdom, Kingdom targetKingdom, string reason)
	{
		if (!IsValidKingdom(declaringKingdom) || !IsValidKingdom(targetKingdom) || declaringKingdom == targetKingdom || IsAtWar(declaringKingdom, targetKingdom))
		{
			return;
		}
		string declaringId = (declaringKingdom.StringId ?? "").Trim();
		string targetId = (targetKingdom.StringId ?? "").Trim();
		string key = BuildPendingDiplomacySyncKey("declare_war", declaringId, targetId, reason);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		_pendingDiplomacySyncs[key] = "declare_war|" + declaringId + "|" + targetId + "|" + (reason ?? "");
		_nextDiplomacySyncRetryUtcTicks = 0L;
		_nextNoticePublishRetryUtcTicks = 0L;
		bool missionActive = false;
		bool meetingBlocked = false;
		try
		{
			missionActive = Mission.Current != null;
		}
		catch
		{
		}
		try
		{
			meetingBlocked = MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects;
		}
		catch
		{
		}
		VassalageDiagnosticLog.Event("diplomacy.declare_war.defer", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["declaring"] = VassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
			["target"] = VassalageDiagnosticLog.DescribeKingdom(targetKingdom),
			["pendingKey"] = key,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["missionActive"] = missionActive,
			["meetingBlocked"] = meetingBlocked
		});
	}

	private void QueuePendingMakePeaceSync(Kingdom kingdom1, Kingdom kingdom2, string reason)
	{
		if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2) || kingdom1 == kingdom2 || !IsAtWar(kingdom1, kingdom2))
		{
			return;
		}
		string kingdom1Id = (kingdom1.StringId ?? "").Trim();
		string kingdom2Id = (kingdom2.StringId ?? "").Trim();
		string key = BuildPendingDiplomacySyncKey("make_peace", kingdom1Id, kingdom2Id, reason);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		int removedPendingDeclareWarCount = RemovePendingDeclareWarSyncsByParties(kingdom1, kingdom2, (reason ?? "queued_peace") + "_queued_peace_cancel_pending_declare");
		_pendingDiplomacySyncs[key] = "make_peace|" + kingdom1Id + "|" + kingdom2Id + "|" + (reason ?? "");
		_nextDiplomacySyncRetryUtcTicks = 0L;
		_nextNoticePublishRetryUtcTicks = 0L;
		bool missionActive = false;
		bool meetingBlocked = false;
		try
		{
			missionActive = Mission.Current != null;
		}
		catch
		{
		}
		try
		{
			meetingBlocked = MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects;
		}
		catch
		{
		}
		VassalageDiagnosticLog.Event("diplomacy.make_peace.defer", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
			["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
			["pendingKey"] = key,
			["removedPendingDeclareWarCount"] = removedPendingDeclareWarCount,
			["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count,
			["missionActive"] = missionActive,
			["meetingBlocked"] = meetingBlocked
		});
	}

	private int RemovePendingDeclareWarSyncsByParties(Kingdom kingdom1, Kingdom kingdom2, string reason)
	{
		if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2) || kingdom1 == kingdom2 || _pendingDiplomacySyncs.Count == 0)
		{
			return 0;
		}
		string kingdom1Id = (kingdom1.StringId ?? "").Trim();
		string kingdom2Id = (kingdom2.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(kingdom1Id) || string.IsNullOrWhiteSpace(kingdom2Id))
		{
			return 0;
		}
		int removed = 0;
		List<string> removedDeclareWarSyncs = new List<string>();
		foreach (KeyValuePair<string, string> item in _pendingDiplomacySyncs.ToList())
		{
			string[] parts = (item.Value ?? "").Split('|');
			if (parts.Length < 3 || !string.Equals((parts[0] ?? "").Trim(), "declare_war", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string declaringId = (parts[1] ?? "").Trim();
			string targetId = (parts[2] ?? "").Trim();
			bool sameDirection = string.Equals(declaringId, kingdom1Id, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(targetId, kingdom2Id, StringComparison.OrdinalIgnoreCase);
			bool reverseDirection = string.Equals(declaringId, kingdom2Id, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(targetId, kingdom1Id, StringComparison.OrdinalIgnoreCase);
			if (!sameDirection && !reverseDirection)
			{
				continue;
			}
			if (_pendingDiplomacySyncs.Remove(item.Key))
			{
				removed++;
				string originalSyncReason = parts.Length >= 4 ? (parts[3] ?? "").Trim() : "";
				removedDeclareWarSyncs.Add(declaringId + "->" + targetId + ";syncReason=" + originalSyncReason + ";pendingKey=" + (item.Key ?? ""));
			}
		}
		if (removed > 0)
		{
			VassalageDiagnosticLog.Event("pending_diplomacy.remove_declare_war", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["kingdom1Agreement"] = DescribeAgreementForDiagnostics(GetAnyVassalAgreement(kingdom1)),
				["kingdom2Agreement"] = DescribeAgreementForDiagnostics(GetAnyVassalAgreement(kingdom2)),
				["removed"] = removed,
				["removedDeclareWarSyncs"] = removedDeclareWarSyncs,
				["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
			});
		}
		return removed;
	}

	private static bool IsProtectedSubjectDeclareWarReason(string reason)
	{
		string value = (reason ?? "").Trim();
		return string.Equals(value, "tributary_protection_accepted", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "tributary_treaty_protection_accepted", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "npc_tributary_protection_accepted", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "npc_tributary_treaty_protection_accepted", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "garrison_protection_accepted", StringComparison.OrdinalIgnoreCase);
	}

	private bool ShouldBlockInternalWarForCurrentVassalage(
		Kingdom declaringKingdom,
		Kingdom targetKingdom,
		out string blockReason,
		out VassalageAgreement declaringAgreement,
		out VassalageAgreement targetAgreement)
	{
		blockReason = "";
		declaringAgreement = GetAnyVassalAgreement(declaringKingdom);
		targetAgreement = GetAnyVassalAgreement(targetKingdom);
		if (!IsValidKingdom(declaringKingdom) || !IsValidKingdom(targetKingdom) || declaringKingdom == targetKingdom)
		{
			return false;
		}
		Kingdom declaringSuzerain = declaringAgreement?.ResolveSuzerain();
		Kingdom targetSuzerain = targetAgreement?.ResolveSuzerain();
		if (declaringAgreement != null && declaringSuzerain == targetKingdom)
		{
			blockReason = "subject_against_suzerain";
			return true;
		}
		if (targetAgreement != null && targetSuzerain == declaringKingdom)
		{
			blockReason = "suzerain_against_subject";
			return true;
		}
		if (declaringAgreement != null
			&& targetAgreement != null
			&& IsValidKingdom(declaringSuzerain)
			&& declaringSuzerain == targetSuzerain
			&& IsControlledSubjectWithoutMilitaryAutonomy(declaringAgreement)
			&& IsControlledSubjectWithoutMilitaryAutonomy(targetAgreement))
		{
			blockReason = "controlled_subjects_same_suzerain";
			return true;
		}
		return false;
	}

	private bool IsObsoleteTributarySuzerainWarSync(string action, Kingdom declaringKingdom, Kingdom targetKingdom, string reason)
	{
		if (!string.Equals((action ?? "").Trim(), "declare_war", StringComparison.OrdinalIgnoreCase)
			|| !string.Equals((reason ?? "").Trim(), "agreement_sync_suzerain_war", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		VassalageAgreement agreement = GetPlayerVassalAgreement(declaringKingdom);
		return agreement != null
			&& NormalizeVassalageType(agreement.Type) == AfVassalageType.Tributary
			&& IsValidKingdom(targetKingdom);
	}

	private void ProcessPendingDiplomacySyncs()
	{
		if (_pendingDiplomacySyncs.Count == 0)
		{
			return;
		}
		long ticks = DateTime.UtcNow.Ticks;
		if (ticks < _nextDiplomacySyncRetryUtcTicks)
		{
			return;
		}
		_nextDiplomacySyncRetryUtcTicks = ticks + TimeSpan.FromSeconds(1.0).Ticks;
		if (!CanApplyVassalageDiplomacyNow())
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in _pendingDiplomacySyncs.ToList())
		{
			if (!_pendingDiplomacySyncs.ContainsKey(item.Key))
			{
				continue;
			}
			string[] parts = (item.Value ?? "").Split('|');
			if (parts.Length < 3)
			{
				_pendingDiplomacySyncs.Remove(item.Key);
				VassalageDiagnosticLog.Event("pending_diplomacy.drop", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["reason"] = "invalid_payload",
					["payload"] = item.Value ?? ""
				});
				continue;
			}
			string action = (parts[0] ?? "").Trim();
			Kingdom kingdom1 = ResolveKingdomById(parts[1]);
			Kingdom kingdom2 = ResolveKingdomById(parts[2]);
			string reason = parts.Length >= 4 ? parts[3] : "";
			if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2) || kingdom1 == kingdom2)
			{
				_pendingDiplomacySyncs.Remove(item.Key);
				VassalageDiagnosticLog.Event("pending_diplomacy.drop", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["reason"] = "invalid_kingdom",
					["action"] = action,
					["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2)
				});
				continue;
			}
			if (string.Equals(action, "declare_war", StringComparison.OrdinalIgnoreCase)
				&& ShouldBlockInternalWarForCurrentVassalage(kingdom1, kingdom2, out string vassalageBlockReason, out var declaringAgreement, out var targetAgreement))
			{
				bool wasAtWar = IsAtWar(kingdom1, kingdom2);
				int removedPendingDeclareWarCount = RemovePendingDeclareWarSyncsByParties(kingdom1, kingdom2, "pending_current_vassalage_forbids_war");
				bool peaceAppliedNow = wasAtWar && MakePeaceIfNeeded(kingdom1, kingdom2, "pending_current_vassalage_forbids_war_reconcile");
				bool peaceAfterReconcile = !IsAtWar(kingdom1, kingdom2);
				VassalageDiagnosticLog.Event("pending_diplomacy.drop", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["reason"] = "current_vassalage_forbids_war",
					["syncReason"] = reason,
					["vassalageBlockReason"] = vassalageBlockReason,
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
					["declaringAgreement"] = DescribeAgreementForDiagnostics(declaringAgreement),
					["targetAgreement"] = DescribeAgreementForDiagnostics(targetAgreement),
					["wasAtWar"] = wasAtWar,
					["peaceAppliedNow"] = peaceAppliedNow,
					["peaceAfterReconcile"] = peaceAfterReconcile,
					["removedPendingDeclareWarCount"] = removedPendingDeclareWarCount,
					["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
				});
				continue;
			}
			if (IsObsoleteTributarySuzerainWarSync(action, kingdom1, kingdom2, reason))
			{
				_pendingDiplomacySyncs.Remove(item.Key);
				VassalageDiagnosticLog.Event("pending_diplomacy.drop", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["reason"] = "tributary_no_forced_suzerain_war_sync",
					["action"] = action,
					["syncReason"] = reason,
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
					["agreement"] = DescribeAgreementForDiagnostics(GetPlayerVassalAgreement(kingdom1)),
					["pendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
				});
				continue;
			}
			if (string.Equals(action, "declare_war", StringComparison.OrdinalIgnoreCase))
			{
				if (IsAtWar(kingdom1, kingdom2))
				{
					_pendingDiplomacySyncs.Remove(item.Key);
					Kingdom playerKingdom = GetPlayerKingdom();
					VassalageAgreement agreement = GetPlayerVassalAgreement(kingdom1);
					bool protectedRecordCreated = RecordProtectedTributaryWarForSynchronizedSuzerainWarIfNeeded(agreement, kingdom1, kingdom2, reason, "pending_diplomacy_already_at_war");
					if (IsValidKingdom(playerKingdom) && (kingdom1 == playerKingdom || kingdom2 == playerKingdom))
					{
						CascadeControlledSubjectsForPlayerWar(kingdom1, kingdom2, reason, "pending_diplomacy_already_at_war");
					}
					VassalageDiagnosticLog.Event("pending_diplomacy.done", new Dictionary<string, object>
					{
						["pendingKey"] = item.Key,
						["action"] = action,
						["reason"] = reason,
						["result"] = "already_at_war",
						["declaring"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
						["target"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
						["protectedRecordCreated"] = protectedRecordCreated
					});
					continue;
				}
				if (IsProtectedSubjectDeclareWarReason(reason)
					&& !TryFindProtectedSubjectWarForSuzerain(kingdom1, null, kingdom2, requireSubjectWar: true, requirePlayerWar: false, out var activeProtectedKey, out var activeProtectedAgreement, out var activeProtectedSubject, out var activeProtectedEnemy))
				{
					_pendingDiplomacySyncs.Remove(item.Key);
					VassalageDiagnosticLog.Event("pending_diplomacy.drop", new Dictionary<string, object>
					{
						["pendingKey"] = item.Key,
						["reason"] = "stale_protected_subject_declare_war",
						["syncReason"] = reason,
						["declaring"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
						["target"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
						["activeProtectedKey"] = activeProtectedKey ?? "",
						["activeProtectedAgreementId"] = activeProtectedAgreement?.AgreementId ?? "",
						["activeProtectedSubject"] = VassalageDiagnosticLog.DescribeKingdom(activeProtectedSubject),
						["activeProtectedEnemy"] = VassalageDiagnosticLog.DescribeKingdom(activeProtectedEnemy)
					});
					continue;
				}
				VassalageDiagnosticLog.Event("pending_diplomacy.apply.start", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["action"] = action,
					["reason"] = reason,
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2)
				});
				ApplyVassalageDiplomacy(delegate
				{
					DeclareWarAction.ApplyByCallToWarAgreement(kingdom1, kingdom2);
				});
				bool isAtWarAfter = IsAtWar(kingdom1, kingdom2);
				VassalageAgreement syncedAgreement = GetPlayerVassalAgreement(kingdom1);
				bool syncedProtectedRecordCreated = isAtWarAfter && RecordProtectedTributaryWarForSynchronizedSuzerainWarIfNeeded(syncedAgreement, kingdom1, kingdom2, reason, "pending_diplomacy_apply_done");
				if (isAtWarAfter)
				{
					_pendingDiplomacySyncs.Remove(item.Key);
					Kingdom playerKingdom = GetPlayerKingdom();
					if (IsValidKingdom(playerKingdom) && (kingdom1 == playerKingdom || kingdom2 == playerKingdom))
					{
						CascadeControlledSubjectsForPlayerWar(kingdom1, kingdom2, reason, "pending_diplomacy_apply_done");
					}
				}
				VassalageDiagnosticLog.Event("pending_diplomacy.apply.done", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["action"] = action,
					["reason"] = reason,
					["declaring"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["target"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
					["isAtWarAfter"] = isAtWarAfter,
					["protectedRecordCreated"] = syncedProtectedRecordCreated,
					["remainingPendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
				});
				continue;
			}
			if (string.Equals(action, "make_peace", StringComparison.OrdinalIgnoreCase))
			{
				if (!IsAtWar(kingdom1, kingdom2))
				{
					_pendingDiplomacySyncs.Remove(item.Key);
					RemovePendingDeclareWarSyncsByParties(kingdom1, kingdom2, "pending_make_peace_already_at_peace_cancel_declare");
					RemoveProtectedTributaryWarByParties(kingdom1, kingdom2, "pending_make_peace_already_at_peace");
					VassalageDiagnosticLog.Event("pending_diplomacy.done", new Dictionary<string, object>
					{
						["pendingKey"] = item.Key,
						["action"] = action,
						["reason"] = reason,
						["result"] = "already_at_peace",
						["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
						["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2)
					});
					continue;
				}
				VassalageDiagnosticLog.Event("pending_diplomacy.apply.start", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["action"] = action,
					["reason"] = reason,
					["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2)
				});
				ApplyVassalageDiplomacy(delegate
				{
					MakePeaceAction.Apply(kingdom1, kingdom2);
				});
				bool peaceAfter = !IsAtWar(kingdom1, kingdom2);
				if (peaceAfter)
				{
					_pendingDiplomacySyncs.Remove(item.Key);
					RemovePendingDeclareWarSyncsByParties(kingdom1, kingdom2, "pending_make_peace_applied_cancel_declare");
					RemoveProtectedTributaryWarByParties(kingdom1, kingdom2, "pending_make_peace_applied");
				}
				VassalageDiagnosticLog.Event("pending_diplomacy.apply.done", new Dictionary<string, object>
				{
					["pendingKey"] = item.Key,
					["action"] = action,
					["reason"] = reason,
					["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
					["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
					["peaceAfter"] = peaceAfter,
					["remainingPendingDiplomacySyncCount"] = _pendingDiplomacySyncs.Count
				});
				continue;
			}
			_pendingDiplomacySyncs.Remove(item.Key);
			VassalageDiagnosticLog.Event("pending_diplomacy.drop", new Dictionary<string, object>
			{
				["pendingKey"] = item.Key,
				["reason"] = "unknown_action",
				["action"] = action,
				["payload"] = item.Value ?? ""
			});
		}
	}

	private bool TryResolvePendingInfoNotice(string noticeId, out VassalageInfoNoticeRecord record)
	{
		record = null;
		string key = (noticeId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key) || !_pendingInfoNotices.TryGetValue(key, out var value))
		{
			return false;
		}
		try
		{
			record = JsonConvert.DeserializeObject<VassalageInfoNoticeRecord>(value ?? "");
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[WARN] parse info notice failed: " + ex.Message);
			record = null;
		}
		if (record == null)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(record.NoticeId))
		{
			record.NoticeId = key;
		}
		return string.Equals(record.NoticeId ?? "", key, StringComparison.OrdinalIgnoreCase) && record.IsValid();
	}

	private bool TryResolvePendingProtectionNotice(string noticeId, out VassalageAgreement agreement, out Kingdom vassal, out Kingdom enemy)
	{
		agreement = null;
		vassal = null;
		enemy = null;
		string key = (noticeId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key) || !_pendingProtectionNotices.TryGetValue(key, out var value))
		{
			return false;
		}
		string[] parts = (value ?? "").Split('|');
		if (parts.Length < 2)
		{
			return false;
		}
		agreement = FindAgreementById(parts[0]);
		enemy = ResolveKingdomById(parts[1]);
		vassal = agreement?.ResolveVassal();
		Kingdom playerKingdom = GetPlayerKingdom();
		return agreement != null
			&& IsValidKingdom(playerKingdom)
			&& IsValidKingdom(vassal)
			&& IsValidKingdom(enemy)
			&& enemy != playerKingdom
			&& vassal != enemy
			&& IsAtWar(vassal, enemy);
	}

	private bool TryResolvePendingNpcTributaryVassalageNotice(string noticeId, out VassalageAgreement agreement)
	{
		agreement = null;
		string key = (noticeId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key) || !_pendingNpcTributaryVassalageNotices.TryGetValue(key, out var agreementId))
		{
			return false;
		}
		agreement = FindAgreementById(agreementId);
		return IsNpcTributaryVassalageAgreement(agreement);
	}

	private bool TryResolvePendingTributaryPaymentNotice(string noticeId, out TributaryPaymentNoticeRecord record)
	{
		record = null;
		string key = (noticeId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key) || !_pendingTributaryPaymentNotices.TryGetValue(key, out var value))
		{
			return false;
		}
		if (!TryDeserializeTributaryPaymentRecord(key, value, out record))
		{
			return false;
		}
		VassalageAgreement agreement = FindAgreementById(record.AgreementId);
		if (!IsTributaryPaymentRecordForAgreement(record, agreement))
		{
			return false;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom suzerain = agreement.ResolveSuzerain();
		Kingdom tributary = agreement.ResolveVassal();
		return IsValidKingdom(playerKingdom)
			&& IsValidKingdom(suzerain)
			&& IsValidKingdom(tributary)
			&& suzerain == playerKingdom
			&& string.Equals(tributary.StringId ?? "", record.TributaryKingdomId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryDeserializeTributaryPaymentRecord(string key, string value, out TributaryPaymentNoticeRecord record)
	{
		record = null;
		try
		{
			record = JsonConvert.DeserializeObject<TributaryPaymentNoticeRecord>(value ?? "");
		}
		catch (Exception ex)
		{
			Logger.Log("Vassalage", "[WARN] parse tributary payment record failed: " + ex.Message);
			record = null;
		}
		if (record == null)
		{
			return false;
		}
		string normalizedKey = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(record.NoticeId))
		{
			record.NoticeId = normalizedKey;
		}
		return record.IsValid()
			&& (string.IsNullOrWhiteSpace(normalizedKey) || string.Equals(record.NoticeId ?? "", normalizedKey, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsTributaryPaymentRecordForAgreement(TributaryPaymentNoticeRecord record, VassalageAgreement agreement)
	{
		if (record == null || agreement == null || !agreement.IsValid() || !IsTributePayingSubjectType(agreement.Type))
		{
			return false;
		}
		return string.Equals(record.AgreementId ?? "", agreement.AgreementId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(record.TributaryKingdomId ?? "", agreement.VassalKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& record.SettlementDay >= Math.Max(0, agreement.CreatedDay);
	}

	private static string NormalizeNoticeId(string noticeId)
	{
		return (noticeId ?? "").Trim();
	}

	private static void RemovePendingNoticeCore(string noticeId, Dictionary<string, string> pendingNotices, HashSet<string> shownThisSession, HashSet<string> openedFromMap = null)
	{
		string key = NormalizeNoticeId(noticeId);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		pendingNotices.Remove(key);
		shownThisSession.Remove(key);
		openedFromMap?.Remove(key);
	}

	private void RemovePendingInfoNotice(string noticeId)
	{
		RemovePendingNoticeCore(noticeId, _pendingInfoNotices, _infoNoticesShownThisSession);
	}

	private void RemovePendingProtectionNotice(string noticeId)
	{
		RemovePendingNoticeCore(noticeId, _pendingProtectionNotices, _protectionNoticesShownThisSession, _protectionNoticesOpenedFromMap);
	}

	private void RemovePendingNpcTributaryVassalageNotice(string noticeId)
	{
		RemovePendingNoticeCore(noticeId, _pendingNpcTributaryVassalageNotices, _npcTributaryVassalageNoticesShownThisSession);
	}

	private void RemovePendingTributaryPaymentNotice(string noticeId)
	{
		RemovePendingNoticeCore(noticeId, _pendingTributaryPaymentNotices, _tributaryPaymentNoticesShownThisSession);
	}

	private void CompleteProtectionNoticeAcknowledgement(string noticeId, string reason)
	{
		string key = NormalizeNoticeId(noticeId);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		if (!TryResolvePendingProtectionNotice(key, out var agreement, out var vassal, out var enemy))
		{
			VassalageDiagnosticLog.Event("notice.protection.ack.invalid", new Dictionary<string, object>
			{
				["noticeId"] = key,
				["reason"] = reason ?? ""
			});
			RemovePendingProtectionNotice(key);
			return;
		}
		RemovePendingProtectionNotice(key);
		VassalageDiagnosticLog.Event("notice.protection.ack", new Dictionary<string, object>
		{
			["noticeId"] = key,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = NormalizeVassalageType(agreement.Type),
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["reason"] = reason ?? ""
		});
	}

	private void CompleteProtectionNoticeDecision(string noticeId, bool accept, string reason)
	{
		string key = NormalizeNoticeId(noticeId);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		string stage = accept ? "notice.protection.accept" : "notice.protection.decline";
		if (!TryResolvePendingProtectionNotice(key, out var agreement, out var vassal, out var enemy))
		{
			VassalageDiagnosticLog.Event(stage + ".invalid", new Dictionary<string, object>
			{
				["noticeId"] = key,
				["reason"] = reason ?? ""
			});
			RemovePendingProtectionNotice(key);
			return;
		}
		RemovePendingProtectionNotice(key);
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		VassalageDiagnosticLog.Event(stage, new Dictionary<string, object>
		{
			["noticeId"] = key,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = type,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["reason"] = reason ?? ""
		});
		if (accept)
		{
			ApplyProtectionWar(agreement, vassal, enemy, reason ?? "subject_protection_accepted");
			if (type == AfVassalageType.Garrison)
			{
				IncreaseGarrisonObedienceAfterProtection(agreement);
			}
			return;
		}
		if (type == AfVassalageType.Tributary)
		{
			BreakAgreement(agreement, reason ?? "tributary_protection_refused", "你拒绝履行对" + GetKingdomDisplayName(vassal, "朝贡国") + "的庇护义务，朝贡条约已经终止。");
		}
		else if (type == AfVassalageType.Garrison)
		{
			AdjustGarrisonObedienceOrBreak(agreement, CalculateGarrisonRefuseProtectionDelta(agreement, out float refusePlayerStrength, out float refuseSubjectStrength, out float refuseStrengthRatio, out float refuseStrengthAdvantage), reason ?? "garrison_protection_refused", refusePlayerStrength, refuseSubjectStrength, refuseStrengthRatio, refuseStrengthAdvantage);
		}
	}

	private void HandleProtectionNoticeDismissed(string noticeId)
	{
		string key = NormalizeNoticeId(noticeId);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		if (_protectionNoticesOpenedFromMap.Remove(key))
		{
			VassalageDiagnosticLog.Event("notice.protection.remove_after_open", new Dictionary<string, object>
			{
				["noticeId"] = key,
				["pendingStillExists"] = _pendingProtectionNotices.ContainsKey(key)
			});
			return;
		}
		if (!TryResolvePendingProtectionNotice(key, out var agreement, out var vassal, out var enemy))
		{
			VassalageDiagnosticLog.Event("notice.protection.dismiss.invalid", new Dictionary<string, object>
			{
				["noticeId"] = key
			});
			RemovePendingProtectionNotice(key);
			return;
		}
		RemovePendingProtectionNotice(key);
		AfVassalageType type = NormalizeVassalageType(agreement.Type);
		VassalageDiagnosticLog.Event("notice.protection.dismiss", new Dictionary<string, object>
		{
			["noticeId"] = key,
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = type,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
		});
		if (type == AfVassalageType.Tributary)
		{
			BreakAgreement(agreement, "tributary_protection_dismissed", "你未回应" + GetKingdomDisplayName(vassal, "朝贡国") + "的求援，朝贡条约已视为终止。");
		}
		else if (type == AfVassalageType.Garrison)
		{
			AdjustGarrisonObedienceOrBreak(agreement, CalculateGarrisonRefuseProtectionDelta(agreement, out float dismissedPlayerStrength, out float dismissedSubjectStrength, out float dismissedStrengthRatio, out float dismissedStrengthAdvantage), "garrison_protection_dismissed", dismissedPlayerStrength, dismissedSubjectStrength, dismissedStrengthRatio, dismissedStrengthAdvantage);
		}
	}

	private void MarkEstablishedNoticeShown(string agreementId)
	{
		VassalageAgreement agreement = FindAgreementById(agreementId);
		if (agreement != null)
		{
			agreement.EstablishedNoticeShown = true;
			_establishedNoticesShownThisSession.Remove(agreement.AgreementId);
		}
	}

	private VassalageAgreement FindAgreementById(string agreementId)
	{
		string id = (agreementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return _agreementsByVassalId.Values.FirstOrDefault((VassalageAgreement x) => x != null && string.Equals(x.AgreementId, id, StringComparison.OrdinalIgnoreCase));
	}

	private VassalageAgreement GetPlayerVassalAgreement(Kingdom kingdom)
	{
		string id = (kingdom?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_agreementsByVassalId.TryGetValue(id, out var agreement) || agreement == null)
		{
			return null;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		if (!IsValidKingdom(playerKingdom) || !string.Equals(agreement.SuzerainKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		if (!IsValidKingdom(agreement.ResolveVassal()))
		{
			return null;
		}
		return agreement;
	}

	private VassalageAgreement GetAnyVassalAgreement(Kingdom kingdom)
	{
		string id = (kingdom?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_agreementsByVassalId.TryGetValue(id, out var agreement) || agreement == null)
		{
			return null;
		}
		if (!agreement.IsValid() || !IsValidKingdom(agreement.ResolveSuzerain()) || !IsValidKingdom(agreement.ResolveVassal()))
		{
			return null;
		}
		return agreement;
	}

	private VassalageAgreement GetNpcTributaryAgreement(Kingdom kingdom)
	{
		VassalageAgreement agreement = GetAnyVassalAgreement(kingdom);
		if (agreement == null || NormalizeVassalageType(agreement.Type) != AfVassalageType.Tributary)
		{
			return null;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom suzerain = agreement.ResolveSuzerain();
		Kingdom vassal = agreement.ResolveVassal();
		if (IsValidKingdom(playerKingdom) && (suzerain == playerKingdom || vassal == playerKingdom))
		{
			return null;
		}
		return agreement;
	}

	internal static bool IsValidKingdomForVassalage(Kingdom kingdom)
	{
		return IsValidKingdom(kingdom);
	}

	internal Kingdom GetPlayerKingdomForVassalage()
	{
		return GetPlayerKingdom();
	}

	internal bool IsAtWarForBridge(Kingdom left, Kingdom right)
	{
		return IsAtWar(left, right);
	}

	internal VassalageAgreement GetAnyVassalageAgreementForBridge(Kingdom kingdom)
	{
		return GetAnyVassalAgreement(kingdom);
	}

	internal VassalageAgreement GetNonPlayerTributaryAgreementForBridge(Kingdom kingdom)
	{
		return GetNpcTributaryAgreement(kingdom);
	}

	internal bool WouldCreateVassalageCycleForBridge(string suzerainId, string vassalId, out string cycleChain)
	{
		return WouldCreateVassalageCycle(suzerainId, vassalId, out cycleChain);
	}

	internal List<Kingdom> GetKingdomWarEnemiesForBridge(Kingdom kingdom)
	{
		return GetKingdomWarEnemies(kingdom).ToList();
	}

	internal int GetCurrentCampaignDayForBridge()
	{
		return GetCurrentCampaignDay();
	}

	internal void StoreVassalageAgreementForBridge(VassalageAgreement agreement)
	{
		if (agreement == null || string.IsNullOrWhiteSpace(agreement.VassalKingdomId))
		{
			return;
		}
		_agreementsByVassalId[agreement.VassalKingdomId] = agreement;
	}

	internal void QueueNpcTributaryVassalageNoticeForBridge(VassalageAgreement agreement)
	{
		QueueNpcTributaryVassalageNotice(agreement);
	}

	internal bool TryFindActiveProtectedTributaryWarForBridge(Kingdom subject, Kingdom enemy, out string protectedKey)
	{
		return TryFindActiveProtectedSubjectWar(subject, enemy, requirePlayerWar: false, out protectedKey, out var _, out var _, out var _);
	}

	internal bool DeclareWarIfNeededForBridge(Kingdom declaringKingdom, Kingdom targetKingdom, string reason, bool forceQueue = false)
	{
		return DeclareWarIfNeeded(declaringKingdom, targetKingdom, reason, forceQueue);
	}

	internal bool MakePeaceIfNeededForBridge(Kingdom kingdom1, Kingdom kingdom2, string reason, bool forceQueue = false)
	{
		return MakePeaceIfNeeded(kingdom1, kingdom2, reason, forceQueue);
	}

	internal bool HasPendingDeclareWarSyncForBridge(Kingdom declaringKingdom, Kingdom targetKingdom, string reason)
	{
		return HasPendingDeclareWarSync(declaringKingdom, targetKingdom, reason);
	}

	internal void RecordProtectedTributaryWarForBridge(VassalageAgreement agreement, Kingdom vassal, Kingdom enemy, string reason)
	{
		RecordProtectedTributaryWar(agreement, vassal, enemy, reason);
	}

	internal int PendingDiplomacySyncCountForBridge => _pendingDiplomacySyncs.Count;

	internal string DescribeFactionForBridge(IFaction faction, Kingdom resolvedKingdom)
	{
		return DescribeFactionForDiagnostics(faction, resolvedKingdom);
	}

	private bool WouldCreateVassalageCycle(string suzerainId, string vassalId, out string cycleChain)
	{
		cycleChain = "";
		suzerainId = (suzerainId ?? "").Trim();
		vassalId = (vassalId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(suzerainId) || string.IsNullOrWhiteSpace(vassalId))
		{
			return false;
		}
		List<string> chain = new List<string> { vassalId, suzerainId };
		HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string currentId = suzerainId;
		while (!string.IsNullOrWhiteSpace(currentId))
		{
			if (string.Equals(currentId, vassalId, StringComparison.OrdinalIgnoreCase))
			{
				cycleChain = string.Join(" -> ", chain);
				return true;
			}
			if (!visited.Add(currentId))
			{
				cycleChain = string.Join(" -> ", chain);
				return true;
			}
			if (!_agreementsByVassalId.TryGetValue(currentId, out var existing) || existing == null || !existing.IsValid())
			{
				break;
			}
			string parentId = (existing.SuzerainKingdomId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(parentId))
			{
				break;
			}
			chain.Add(parentId);
			currentId = parentId;
		}
		cycleChain = string.Join(" -> ", chain);
		return false;
	}

	private IEnumerable<VassalageAgreement> GetTributePayingAgreements()
	{
		return _agreementsByVassalId.Values.Where((VassalageAgreement x) => x != null
			&& x.IsValid()
			&& IsTributePayingSubjectType(x.Type)
			&& IsValidKingdom(x.ResolveSuzerain())
			&& IsValidKingdom(x.ResolveVassal()));
	}

	private IEnumerable<VassalageAgreement> GetPlayerVassalAgreements()
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		if (!IsValidKingdom(playerKingdom))
		{
			return Enumerable.Empty<VassalageAgreement>();
		}
		return _agreementsByVassalId.Values.Where((VassalageAgreement x) => x != null && x.IsValid() && string.Equals(x.SuzerainKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase));
	}

	private static Kingdom ResolveFactionKingdom(IFaction faction, Kingdom playerKingdom = null)
	{
		if (faction == null)
		{
			return null;
		}
		try
		{
			if (faction is Kingdom kingdom)
			{
				return kingdom;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (playerClan != null && faction == playerClan)
			{
				return playerKingdom ?? GetPlayerKingdom();
			}
			if (faction is Clan clan)
			{
				Kingdom clanKingdom = clan.Kingdom ?? clan.MapFaction as Kingdom;
				if (clanKingdom != null)
				{
					return clanKingdom;
				}
			}
			return faction.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsPlayerFactionForDiplomacy(IFaction faction, Kingdom resolvedKingdom, Kingdom playerKingdom)
	{
		if (!IsValidKingdom(playerKingdom) || faction == null)
		{
			return false;
		}
		try
		{
			if (faction == playerKingdom)
			{
				return true;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (playerClan != null && faction == playerClan)
			{
				return true;
			}
			if (Hero.MainHero?.MapFaction != null && faction == Hero.MainHero.MapFaction && resolvedKingdom == playerKingdom)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string DescribeFactionForDiagnostics(IFaction faction, Kingdom resolvedKingdom)
	{
		if (faction == null)
		{
			return "null";
		}
		string type = "";
		string id = "";
		string name = "";
		string mapFactionId = "";
		string isKingdom = "";
		string isClan = "";
		try
		{
			type = faction.GetType().FullName ?? faction.GetType().Name;
			id = faction.StringId ?? "";
			name = faction.Name?.ToString() ?? "";
			mapFactionId = faction.MapFaction?.StringId ?? "";
			isKingdom = faction.IsKingdomFaction.ToString();
			isClan = faction.IsClan.ToString();
		}
		catch
		{
		}
		return "type=" + type
			+ ";id=" + id
			+ ";name=" + name
			+ ";mapFaction=" + mapFactionId
			+ ";isKingdom=" + isKingdom
			+ ";isClan=" + isClan
			+ ";resolved=" + (resolvedKingdom?.StringId ?? "");
	}

	private IEnumerable<VassalageAgreement> GetControlledSubjectAgreementsForWarSync()
	{
		return GetPlayerVassalAgreements()
			.Where((VassalageAgreement agreement) => agreement != null
				&& (NormalizeVassalageType(agreement.Type) == AfVassalageType.Garrison
					|| NormalizeVassalageType(agreement.Type) == AfVassalageType.Vassal));
	}

	private static string DescribeAgreementForDiagnostics(VassalageAgreement agreement)
	{
		if (agreement == null)
		{
			return "null";
		}
		try
		{
			Kingdom suzerain = agreement.ResolveSuzerain();
			Kingdom vassal = agreement.ResolveVassal();
			return "agreementId=" + (agreement.AgreementId ?? "")
				+ ";type=" + agreement.Type
				+ ";normalizedType=" + NormalizeVassalageType(agreement.Type)
				+ ";suzerainId=" + (agreement.SuzerainKingdomId ?? "")
				+ ";vassalId=" + (agreement.VassalKingdomId ?? "")
				+ ";createdDay=" + agreement.CreatedDay.ToString(CultureInfo.InvariantCulture)
				+ ";negotiatedByHeroId=" + (agreement.NegotiatedByHeroId ?? "")
				+ ";suzerain=" + VassalageDiagnosticLog.DescribeKingdom(suzerain)
				+ ";vassal=" + VassalageDiagnosticLog.DescribeKingdom(vassal);
		}
		catch (Exception ex)
		{
			return "agreementId=" + (agreement.AgreementId ?? "")
				+ ";type=" + agreement.Type
				+ ";suzerainId=" + (agreement.SuzerainKingdomId ?? "")
				+ ";vassalId=" + (agreement.VassalKingdomId ?? "")
				+ ";describeError=" + ex.GetType().Name + ":" + ex.Message;
		}
	}

	private static string BuildProtectedTributaryWarKey(string vassalKingdomId, string enemyKingdomId)
	{
		return ((vassalKingdomId ?? "").Trim() + "|" + (enemyKingdomId ?? "").Trim()).Trim();
	}

	private void RecordProtectedTributaryWar(VassalageAgreement agreement, Kingdom vassal, Kingdom enemy, string reason)
	{
		AfVassalageType type = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Tributary);
		if (agreement == null
			|| (type != AfVassalageType.Tributary && type != AfVassalageType.Garrison)
			|| !IsValidKingdom(vassal)
			|| !IsValidKingdom(enemy)
			|| vassal == enemy)
		{
			return;
		}
		string key = BuildProtectedTributaryWarKey(vassal.StringId, enemy.StringId);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		_protectedTributaryWars[key] = agreement.AgreementId + "|" + (enemy.StringId ?? "");
		VassalageDiagnosticLog.Event("protected_subject_war.record", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = type,
			["subject"] = VassalageDiagnosticLog.DescribeKingdom(vassal),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["reason"] = reason ?? "",
			["protectedTributaryWarCount"] = _protectedTributaryWars.Count
		});
	}

	private bool RecordProtectedTributaryWarForSynchronizedSuzerainWarIfNeeded(VassalageAgreement agreement, Kingdom tributary, Kingdom enemy, string syncReason, string source)
	{
		string reason = (syncReason ?? "").Trim();
		if (!string.Equals(reason, "agreement_sync_suzerain_war", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		AfVassalageType type = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Military);
		string skipReason = "";
		if (agreement == null)
		{
			skipReason = "missing_agreement";
		}
		else if (type != AfVassalageType.Tributary)
		{
			skipReason = "not_tributary";
		}
		else if (!IsValidKingdom(tributary) || !IsValidKingdom(enemy) || tributary == enemy)
		{
			skipReason = "invalid_context";
		}
		else if (!IsAtWar(tributary, enemy))
		{
			skipReason = "subject_not_at_war_yet";
		}
		if (!string.IsNullOrWhiteSpace(skipReason))
		{
			VassalageDiagnosticLog.Event("protected_subject_war.agreement_sync_skip", new Dictionary<string, object>
			{
				["reason"] = reason,
				["skipReason"] = skipReason,
				["source"] = source ?? "",
				["agreementId"] = agreement?.AgreementId ?? "",
				["type"] = agreement?.Type ?? AfVassalageType.Military,
				["normalizedType"] = type,
				["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
				["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["tributaryAtWar"] = IsAtWar(tributary, enemy),
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count
			});
			return false;
		}
		RecordProtectedTributaryWar(agreement, tributary, enemy, reason);
		VassalageDiagnosticLog.Event("protected_subject_war.agreement_sync_record", new Dictionary<string, object>
		{
			["reason"] = reason,
			["source"] = source ?? "",
			["agreementId"] = agreement.AgreementId,
			["type"] = agreement.Type,
			["normalizedType"] = type,
			["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
			["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
			["protectedKey"] = BuildProtectedTributaryWarKey(tributary.StringId, enemy.StringId),
			["protectedTributaryWarCount"] = _protectedTributaryWars.Count
		});
		return true;
	}

	private bool TryResolveProtectedTributaryWar(KeyValuePair<string, string> item, out VassalageAgreement agreement, out Kingdom vassal, out Kingdom enemy)
	{
		agreement = null;
		vassal = null;
		enemy = null;
		string[] parts = (item.Value ?? "").Split('|');
		if (parts.Length < 2)
		{
			return false;
		}
		agreement = FindAgreementById(parts[0]);
		AfVassalageType type = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Tributary);
		if (agreement == null || (type != AfVassalageType.Tributary && type != AfVassalageType.Garrison))
		{
			return false;
		}
		vassal = agreement.ResolveVassal();
		enemy = ResolveKingdomById(parts[1]);
		return IsValidKingdom(vassal) && IsValidKingdom(enemy) && vassal != enemy;
	}

	private bool TryFindActiveProtectedSubjectWar(Kingdom subjectFilter, Kingdom enemyFilter, bool requirePlayerWar, out string protectedKey, out VassalageAgreement agreement, out Kingdom subject, out Kingdom enemy)
	{
		return TryFindProtectedSubjectWar(subjectFilter, enemyFilter, requireSubjectWar: true, requirePlayerWar: requirePlayerWar, out protectedKey, out agreement, out subject, out enemy);
	}

	private bool TryFindProtectedSubjectWarByParties(Kingdom kingdom1, Kingdom kingdom2, bool requireSubjectWar, bool requirePlayerWar, out string protectedKey, out VassalageAgreement agreement, out Kingdom subject, out Kingdom enemy)
	{
		if (TryFindProtectedSubjectWar(kingdom1, kingdom2, requireSubjectWar, requirePlayerWar, out protectedKey, out agreement, out subject, out enemy))
		{
			return true;
		}
		return TryFindProtectedSubjectWar(kingdom2, kingdom1, requireSubjectWar, requirePlayerWar, out protectedKey, out agreement, out subject, out enemy);
	}

	private bool TryFindProtectedSuzerainWarByParties(Kingdom kingdom1, Kingdom kingdom2, bool requireSubjectWar, out string protectedKey, out VassalageAgreement agreement, out Kingdom suzerain, out Kingdom subject, out Kingdom enemy)
	{
		protectedKey = "";
		agreement = null;
		suzerain = null;
		subject = null;
		enemy = null;
		if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2) || _protectedTributaryWars.Count == 0)
		{
			return false;
		}
		foreach (KeyValuePair<string, string> item in _protectedTributaryWars.ToList())
		{
			if (!TryResolveProtectedTributaryWar(item, out var resolvedAgreement, out var resolvedSubject, out var resolvedEnemy))
			{
				continue;
			}
			Kingdom resolvedSuzerain = resolvedAgreement.ResolveSuzerain();
			if (!IsValidKingdom(resolvedSuzerain))
			{
				continue;
			}
			bool sameDirection = resolvedSuzerain == kingdom1 && resolvedEnemy == kingdom2;
			bool reverseDirection = resolvedSuzerain == kingdom2 && resolvedEnemy == kingdom1;
			if (!sameDirection && !reverseDirection)
			{
				continue;
			}
			if (requireSubjectWar && !IsAtWar(resolvedSubject, resolvedEnemy))
			{
				continue;
			}
			protectedKey = item.Key;
			agreement = resolvedAgreement;
			suzerain = resolvedSuzerain;
			subject = resolvedSubject;
			enemy = resolvedEnemy;
			return true;
		}
		return false;
	}

	private bool TryFindProtectedSubjectWar(Kingdom subjectFilter, Kingdom enemyFilter, bool requireSubjectWar, bool requirePlayerWar, out string protectedKey, out VassalageAgreement agreement, out Kingdom subject, out Kingdom enemy)
	{
		return TryFindProtectedSubjectWarForSuzerain(null, subjectFilter, enemyFilter, requireSubjectWar, requirePlayerWar, out protectedKey, out agreement, out subject, out enemy);
	}

	private bool TryFindProtectedSubjectWarForSuzerain(Kingdom suzerainFilter, Kingdom subjectFilter, Kingdom enemyFilter, bool requireSubjectWar, bool requirePlayerWar, out string protectedKey, out VassalageAgreement agreement, out Kingdom subject, out Kingdom enemy)
	{
		protectedKey = "";
		agreement = null;
		subject = null;
		enemy = null;
		if (_protectedTributaryWars.Count == 0)
		{
			return false;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		if (requirePlayerWar && !IsValidKingdom(playerKingdom))
		{
			return false;
		}
		foreach (KeyValuePair<string, string> item in _protectedTributaryWars.ToList())
		{
			if (!TryResolveProtectedTributaryWar(item, out var resolvedAgreement, out var resolvedSubject, out var resolvedEnemy))
			{
				continue;
			}
			Kingdom resolvedSuzerain = resolvedAgreement?.ResolveSuzerain();
			if (suzerainFilter != null && resolvedSuzerain != suzerainFilter)
			{
				continue;
			}
			if (subjectFilter != null && resolvedSubject != subjectFilter)
			{
				continue;
			}
			if (enemyFilter != null && resolvedEnemy != enemyFilter)
			{
				continue;
			}
			if (requireSubjectWar && !IsAtWar(resolvedSubject, resolvedEnemy))
			{
				continue;
			}
			if (requirePlayerWar && !IsAtWar(playerKingdom, resolvedEnemy))
			{
				continue;
			}
			protectedKey = item.Key;
			agreement = resolvedAgreement;
			subject = resolvedSubject;
			enemy = resolvedEnemy;
			return true;
		}
		return false;
	}

	private int SynchronizeProtectedTributaryPeaceForSuzerain(Kingdom suzerain, Kingdom formerEnemy, MakePeaceAction.MakePeaceDetail detail, string reason)
	{
		if (!IsValidKingdom(suzerain) || !IsValidKingdom(formerEnemy) || _protectedTributaryWars.Count == 0)
		{
			return 0;
		}
		int syncedOrQueued = 0;
		foreach (KeyValuePair<string, string> item in _protectedTributaryWars.ToList())
		{
			if (!TryResolveProtectedTributaryWar(item, out var agreement, out var subject, out var enemy))
			{
				_protectedTributaryWars.Remove(item.Key);
				VassalageDiagnosticLog.Event("protected_subject_war.drop", new Dictionary<string, object>
				{
					["protectedKey"] = item.Key,
					["reason"] = "invalid_record",
					["payload"] = item.Value ?? ""
				});
				continue;
			}
			Kingdom agreementSuzerain = agreement.ResolveSuzerain();
			if (agreementSuzerain != suzerain || enemy != formerEnemy)
			{
				continue;
			}
			if (!IsAtWar(subject, enemy))
			{
				_protectedTributaryWars.Remove(item.Key);
				RemovePendingDeclareWarSyncsByParties(subject, enemy, "protected_subject_suzerain_peace_subject_already_peace_cancel_declare");
				VassalageDiagnosticLog.Event("protected_subject_war.drop", new Dictionary<string, object>
				{
					["protectedKey"] = item.Key,
					["agreementId"] = agreement.AgreementId,
					["type"] = agreement.Type,
					["reason"] = "subject_already_at_peace",
					["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(suzerain),
					["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
				continue;
			}
			int pendingBefore = _pendingDiplomacySyncs.Count;
			bool peaceAppliedNow = MakePeaceIfNeeded(subject, enemy, reason ?? "protected_subject_suzerain_peace");
			bool stillAtWar = IsAtWar(subject, enemy);
			if (!stillAtWar)
			{
				_protectedTributaryWars.Remove(item.Key);
				RemovePendingDeclareWarSyncsByParties(subject, enemy, "protected_subject_suzerain_peace_applied_cancel_declare");
			}
			if (peaceAppliedNow || _pendingDiplomacySyncs.Count > pendingBefore)
			{
				syncedOrQueued++;
			}
			VassalageDiagnosticLog.Event("make_peace.sync_protected_subject_by_suzerain", new Dictionary<string, object>
			{
				["protectedKey"] = item.Key,
				["agreementId"] = agreement.AgreementId,
				["type"] = agreement.Type,
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(suzerain),
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["detail"] = detail,
				["peaceAppliedNow"] = peaceAppliedNow,
				["queued"] = !peaceAppliedNow && _pendingDiplomacySyncs.Count > pendingBefore,
				["stillAtWar"] = stillAtWar,
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count
			});
		}
		return syncedOrQueued;
	}

	private void SynchronizeProtectedTributaryPeace(Kingdom formerEnemy, MakePeaceAction.MakePeaceDetail detail)
	{
		if (!IsValidKingdom(formerEnemy) || _protectedTributaryWars.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in _protectedTributaryWars.ToList())
		{
			if (!TryResolveProtectedTributaryWar(item, out var agreement, out var subject, out var enemy))
			{
				_protectedTributaryWars.Remove(item.Key);
				VassalageDiagnosticLog.Event("protected_subject_war.drop", new Dictionary<string, object>
				{
					["protectedKey"] = item.Key,
					["reason"] = "invalid_record",
					["payload"] = item.Value ?? ""
				});
				continue;
			}
			if (enemy != formerEnemy)
			{
				continue;
			}
			if (!IsAtWar(subject, enemy))
			{
				_protectedTributaryWars.Remove(item.Key);
				RemovePendingDeclareWarSyncsByParties(subject, enemy, "protected_subject_already_at_peace_cancel_declare");
				VassalageDiagnosticLog.Event("protected_subject_war.drop", new Dictionary<string, object>
				{
					["protectedKey"] = item.Key,
					["agreementId"] = agreement.AgreementId,
					["type"] = agreement.Type,
					["reason"] = "subject_already_at_peace",
					["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
				continue;
			}
			int pendingBefore = _pendingDiplomacySyncs.Count;
			bool peaceAppliedNow = MakePeaceIfNeeded(subject, enemy, "protected_subject_player_peace");
			bool stillAtWar = IsAtWar(subject, enemy);
			if (!stillAtWar)
			{
				_protectedTributaryWars.Remove(item.Key);
			}
			VassalageDiagnosticLog.Event("make_peace.sync_protected_subject", new Dictionary<string, object>
			{
				["protectedKey"] = item.Key,
				["agreementId"] = agreement.AgreementId,
				["type"] = agreement.Type,
				["subject"] = VassalageDiagnosticLog.DescribeKingdom(subject),
				["formerEnemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy),
				["detail"] = detail,
				["peaceAppliedNow"] = peaceAppliedNow,
				["queued"] = !peaceAppliedNow && _pendingDiplomacySyncs.Count > pendingBefore,
				["stillAtWar"] = stillAtWar,
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count
			});
		}
	}

	private int RemoveProtectedTributaryWarByParties(Kingdom kingdom1, Kingdom kingdom2, string reason)
	{
		if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2))
		{
			return 0;
		}
		string key1 = BuildProtectedTributaryWarKey(kingdom1.StringId, kingdom2.StringId);
		string key2 = BuildProtectedTributaryWarKey(kingdom2.StringId, kingdom1.StringId);
		int removed = 0;
		if (!string.IsNullOrWhiteSpace(key1) && _protectedTributaryWars.Remove(key1))
		{
			removed++;
		}
		if (!string.Equals(key1, key2, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(key2) && _protectedTributaryWars.Remove(key2))
		{
			removed++;
		}
		if (removed > 0)
		{
			VassalageDiagnosticLog.Event("tributary_protection.remove", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["kingdom1"] = VassalageDiagnosticLog.DescribeKingdom(kingdom1),
				["kingdom2"] = VassalageDiagnosticLog.DescribeKingdom(kingdom2),
				["removed"] = removed,
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count
			});
		}
		return removed;
	}

	private void RemoveProtectedTributaryWarsForAgreement(VassalageAgreement agreement, string reason)
	{
		if (agreement == null)
		{
			return;
		}
		string agreementId = agreement.AgreementId ?? "";
		string vassalId = (agreement.VassalKingdomId ?? "").Trim();
		int removed = 0;
		foreach (string key in _protectedTributaryWars.Keys.Where((string x) =>
			(!string.IsNullOrWhiteSpace(agreementId) && (_protectedTributaryWars[x] ?? "").StartsWith(agreementId + "|", StringComparison.OrdinalIgnoreCase))
			|| (!string.IsNullOrWhiteSpace(vassalId) && (x ?? "").StartsWith(vassalId + "|", StringComparison.OrdinalIgnoreCase))).ToList())
		{
			_protectedTributaryWars.Remove(key);
			removed++;
		}
		if (removed > 0)
		{
			VassalageDiagnosticLog.Event("tributary_protection.remove", new Dictionary<string, object>
			{
				["reason"] = reason ?? "",
				["agreementId"] = agreement.AgreementId,
				["removed"] = removed,
				["protectedTributaryWarCount"] = _protectedTributaryWars.Count
			});
		}
	}

	private void RemoveInvalidAgreements()
	{
		foreach (string key in _agreementsByVassalId.Where((KeyValuePair<string, VassalageAgreement> x) => x.Value == null || !x.Value.IsValid() || !IsValidKingdom(x.Value.ResolveSuzerain()) || !IsValidKingdom(x.Value.ResolveVassal())).Select((KeyValuePair<string, VassalageAgreement> x) => x.Key).ToList())
		{
			_agreementsByVassalId.Remove(key);
		}
		foreach (string key in _tributaryPaymentLastSettlementDays.Keys.Where((string x) => FindAgreementById(x) == null).ToList())
		{
			_tributaryPaymentLastSettlementDays.Remove(key);
			_tributaryPaymentLastSettlementDayStorage.Remove(key);
		}
		foreach (string noticeId in _pendingTributaryPaymentNotices.Keys.ToList())
		{
			if (!TryResolvePendingTributaryPaymentNotice(noticeId, out var _))
			{
				RemovePendingTributaryPaymentNotice(noticeId);
			}
		}
		foreach (string noticeId in _pendingNpcTributaryVassalageNotices.Keys.ToList())
		{
			if (!TryResolvePendingNpcTributaryVassalageNotice(noticeId, out var _))
			{
				RemovePendingNpcTributaryVassalageNotice(noticeId);
			}
		}
		foreach (KeyValuePair<string, string> item in _tributaryPaymentHistory.ToList())
		{
			if (!TryDeserializeTributaryPaymentRecord(item.Key, item.Value, out var record)
				|| !IsTributaryPaymentRecordForAgreement(record, FindAgreementById(record.AgreementId)))
			{
				_tributaryPaymentHistory.Remove(item.Key);
			}
		}
		foreach (KeyValuePair<string, string> item in _protectedTributaryWars.ToList())
		{
			if (!TryResolveProtectedTributaryWar(item, out var agreement, out var tributary, out var enemy) || !IsAtWar(tributary, enemy))
			{
				_protectedTributaryWars.Remove(item.Key);
				VassalageDiagnosticLog.Event("tributary_protection.drop", new Dictionary<string, object>
				{
					["protectedKey"] = item.Key,
					["agreementId"] = agreement?.AgreementId ?? "",
					["reason"] = "invalid_or_inactive_record",
					["tributary"] = VassalageDiagnosticLog.DescribeKingdom(tributary),
					["enemy"] = VassalageDiagnosticLog.DescribeKingdom(enemy)
				});
			}
		}
	}

	private int CalculateGarrisonRefuseProtectionDelta(VassalageAgreement agreement, out float playerStrength, out float subjectStrength, out float strengthRatio, out float strengthAdvantage)
	{
		return CalculateAndLogGarrisonObedienceDelta(agreement,
			"garrison_protection_refused",
			GarrisonRefuseProtectionWeakDelta,
			GarrisonRefuseProtectionEqualDelta,
			GarrisonRefuseProtectionStrongDelta,
			GarrisonRefuseProtectionOverwhelmingDelta,
			out playerStrength,
			out subjectStrength,
			out strengthRatio,
			out strengthAdvantage);
	}

	private int CalculateGarrisonProtectionSuccessDelta(VassalageAgreement agreement, out float playerStrength, out float subjectStrength, out float strengthRatio, out float strengthAdvantage)
	{
		return CalculateAndLogGarrisonObedienceDelta(agreement,
			"garrison_protection_accepted",
			GarrisonProtectionWeakDelta,
			GarrisonProtectionEqualDelta,
			GarrisonProtectionStrongDelta,
			GarrisonProtectionOverwhelmingDelta,
			out playerStrength,
			out subjectStrength,
			out strengthRatio,
			out strengthAdvantage);
	}

	private int CalculateAndLogGarrisonObedienceDelta(
		VassalageAgreement agreement,
		string reason,
		int weakDelta,
		int equalDelta,
		int strongDelta,
		int overwhelmingDelta,
		out float playerStrength,
		out float subjectStrength,
		out float strengthRatio,
		out float strengthAdvantage)
	{
		CalculateGarrisonStrengthContext(agreement, out playerStrength, out subjectStrength, out strengthRatio, out strengthAdvantage);
		int delta = CalculateGarrisonObedienceDeltaFromAdvantage(strengthAdvantage, weakDelta, equalDelta, strongDelta, overwhelmingDelta);
		LogGarrisonObedienceDeltaSelection(agreement, reason, delta, playerStrength, subjectStrength, strengthRatio, strengthAdvantage);
		return delta;
	}

	private void CalculateGarrisonStrengthContext(VassalageAgreement agreement, out float playerStrength, out float subjectStrength, out float strengthRatio, out float strengthAdvantage)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom subjectKingdom = agreement?.ResolveVassal();
		playerStrength = GetRefreshedKingdomStrengthForVassalage(playerKingdom);
		subjectStrength = GetRefreshedKingdomStrengthForVassalage(subjectKingdom);
		strengthRatio = CalculateSuzerainSubjectStrengthRatio(playerStrength, subjectStrength);
		strengthAdvantage = CalculateSuzerainSubjectStrengthAdvantage(playerStrength, subjectStrength);
	}

	private static float CalculateSuzerainSubjectStrengthRatio(float suzerainStrength, float subjectStrength)
	{
		if (subjectStrength <= 1f)
		{
			return suzerainStrength > 0f ? 3f : 0f;
		}
		return Math.Max(0f, Math.Min(3f, suzerainStrength / Math.Max(1f, subjectStrength)));
	}

	private static float CalculateSuzerainSubjectStrengthAdvantage(float suzerainStrength, float subjectStrength)
	{
		return suzerainStrength - subjectStrength;
	}

	private void LogGarrisonObedienceDeltaSelection(VassalageAgreement agreement, string reason, int delta, float playerStrength, float subjectStrength, float strengthRatio, float strengthAdvantage)
	{
		VassalageDiagnosticLog.Event("obedience.delta_selected", new Dictionary<string, object>
		{
			["agreementId"] = agreement?.AgreementId ?? "",
			["type"] = agreement?.Type ?? AfVassalageType.Garrison,
			["vassal"] = VassalageDiagnosticLog.DescribeKingdom(agreement?.ResolveVassal()),
			["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(agreement?.ResolveSuzerain()),
			["reason"] = reason ?? "",
			["delta"] = delta,
			["playerStrength"] = playerStrength,
			["subjectStrength"] = subjectStrength,
			["strengthRatio"] = strengthRatio,
			["strengthAdvantage"] = strengthAdvantage,
			["weakAdvantage"] = GarrisonStrengthAdvantageWeak,
			["equalAdvantage"] = GarrisonStrengthAdvantageEqual,
			["strongAdvantage"] = GarrisonStrengthAdvantageStrong,
			["overwhelmingAdvantage"] = GarrisonStrengthAdvantageOverwhelming
		});
	}

	private static int CalculateGarrisonObedienceDeltaFromAdvantage(float strengthAdvantage, int weakDelta, int equalDelta, int strongDelta, int overwhelmingDelta)
	{
		if (strengthAdvantage <= GarrisonStrengthAdvantageWeak)
		{
			return weakDelta;
		}
		if (strengthAdvantage <= GarrisonStrengthAdvantageEqual)
		{
			return LerpGarrisonObedienceDelta(weakDelta, equalDelta, (strengthAdvantage - GarrisonStrengthAdvantageWeak) / (GarrisonStrengthAdvantageEqual - GarrisonStrengthAdvantageWeak));
		}
		if (strengthAdvantage <= GarrisonStrengthAdvantageStrong)
		{
			return LerpGarrisonObedienceDelta(equalDelta, strongDelta, (strengthAdvantage - GarrisonStrengthAdvantageEqual) / (GarrisonStrengthAdvantageStrong - GarrisonStrengthAdvantageEqual));
		}
		return LerpGarrisonObedienceDelta(strongDelta, overwhelmingDelta, (strengthAdvantage - GarrisonStrengthAdvantageStrong) / (GarrisonStrengthAdvantageOverwhelming - GarrisonStrengthAdvantageStrong));
	}

	private static int LerpGarrisonObedienceDelta(int minValue, int maxValue, float ratio)
	{
		float value = minValue + (maxValue - minValue) * Math.Max(0f, Math.Min(1f, ratio));
		return (int)Math.Round(value, MidpointRounding.AwayFromZero);
	}

	private void EnsureGarrisonObedienceForLoadedAgreements()
	{
		foreach (VassalageAgreement agreement in GetPlayerVassalAgreements().Where((VassalageAgreement x) => UsesSubjectIndependence(NormalizeVassalageType(x.Type))))
		{
			EnsureGarrisonObedience(agreement);
		}
	}

	private static bool UsesSubjectIndependence(AfVassalageType type)
	{
		AfVassalageType normalized = NormalizeVassalageType(type);
		return normalized == AfVassalageType.Garrison || normalized == AfVassalageType.Vassal;
	}

	private int EnsureGarrisonObedience(VassalageAgreement agreement)
	{
		string key = (agreement?.VassalKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return InitialSubjectObedience;
		}
		if (!_garrisonObedienceValues.TryGetValue(key, out var value))
		{
			value = CalculateInitialGarrisonObedience(agreement);
			_garrisonObedienceValues[key] = value;
			VassalageDiagnosticLog.Event("obedience.initialized", new Dictionary<string, object>
			{
				["agreementId"] = agreement?.AgreementId ?? "",
				["vassal"] = VassalageDiagnosticLog.DescribeKingdom(agreement?.ResolveVassal()),
				["suzerain"] = VassalageDiagnosticLog.DescribeKingdom(agreement?.ResolveSuzerain()),
				["value"] = value,
				["independence"] = IndependenceFromSubjectObedience(value),
				["tier"] = GetSubjectObedienceTierText(value),
				["initialRule"] = "fixed_30_independence"
			});
		}
		else
		{
			value = ClampSubjectObedienceValue(value);
			_garrisonObedienceValues[key] = value;
		}
		return value;
	}

	private int CalculateInitialGarrisonObedience(VassalageAgreement agreement)
	{
		return InitialSubjectObedience;
	}

	private static float GetRefreshedKingdomStrengthForVassalage(Kingdom kingdom)
	{
		if (!IsValidKingdom(kingdom))
		{
			return 0f;
		}
		RefreshKingdomCurrentStrength(kingdom);
		float strength = kingdom.CurrentTotalStrength;
		if (float.IsNaN(strength) || float.IsInfinity(strength))
		{
			return 0f;
		}
		return Math.Max(0f, strength);
	}

	private static int ClampSubjectObedienceValue(int value)
	{
		return Math.Max(SubjectObedienceMinValue, Math.Min(SubjectObedienceMaxValue, value));
	}

	private static int ClampSubjectIndependenceValue(int value)
	{
		return Math.Max(SubjectObedienceMinValue, Math.Min(SubjectObedienceMaxValue, value));
	}

	private static int IndependenceFromSubjectObedience(int obedience)
	{
		return SubjectObedienceMaxValue - ClampSubjectObedienceValue(obedience);
	}

	private static int SubjectObedienceFromIndependence(int independence)
	{
		return SubjectObedienceMaxValue - ClampSubjectIndependenceValue(independence);
	}

	internal static int NormalizeVassalPolicyIndependenceDelta(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0;
		}
		int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
		return Math.Max(VassalPolicyQualityDeltaMinValue, Math.Min(VassalPolicyQualityDeltaMaxValue, rounded));
	}

	private static int ApplySubjectIndependenceChange(int currentIndependence, int publicationCost, int qualityDelta)
	{
		int current = ClampSubjectIndependenceValue(currentIndependence);
		int cost = Math.Max(0, publicationCost);
		int quality = Math.Max(VassalPolicyQualityDeltaMinValue, Math.Min(VassalPolicyQualityDeltaMaxValue, qualityDelta));
		return ClampSubjectIndependenceValue(current + cost + quality);
	}

	private static int CalculateSubjectBreakawayThreshold(int rulerRelation)
	{
		int relation = Math.Max(SubjectRulerRelationMinValue, Math.Min(SubjectRulerRelationMaxValue, rulerRelation));
		double threshold = SubjectBreakawayThresholdNeutralValue + relation * 0.2d;
		int rounded = (int)Math.Round(threshold, MidpointRounding.AwayFromZero);
		return Math.Max(SubjectBreakawayThresholdMinValue, Math.Min(SubjectBreakawayThresholdMaxValue, rounded));
	}

	private static bool ShouldSubjectBreakAway(int independence, int rulerRelation)
	{
		return ClampSubjectIndependenceValue(independence) >= CalculateSubjectBreakawayThreshold(rulerRelation);
	}

	private static string GetSubjectObedienceTierText(int value)
	{
		int num = ClampSubjectObedienceValue(value);
		if (num >= 90)
		{
			return "极高";
		}
		if (num >= 75)
		{
			return "高";
		}
		if (num >= 60)
		{
			return "较高";
		}
		if (num >= 40)
		{
			return "一般";
		}
		if (num >= 25)
		{
			return "较差";
		}
		if (num >= 10)
		{
			return "很差";
		}
		return "极差";
	}

	private static AfVassalageType NormalizeVassalageType(AfVassalageType type)
	{
		if (type == AfVassalageType.Military)
		{
			return AfVassalageType.Garrison;
		}
		if (type == AfVassalageType.Protectorate)
		{
			return AfVassalageType.Tributary;
		}
		return type;
	}

	private static List<Kingdom> GetKingdomWarEnemies(Kingdom kingdom)
	{
		try
		{
			return (kingdom?.FactionsAtWarWith ?? Enumerable.Empty<IFaction>())
				.OfType<Kingdom>()
				.Where(IsValidKingdom)
				.Distinct()
				.ToList();
		}
		catch
		{
			return new List<Kingdom>();
		}
	}

	private static bool TryBuildVassalageRuntimeState(Hero targetHero, out Kingdom playerKingdom, out Kingdom targetKingdom, out Hero speaker)
	{
		playerKingdom = GetPlayerKingdom();
		targetKingdom = null;
		speaker = targetHero;
		if (!IsValidKingdom(playerKingdom) || !IsPlayerRuler(playerKingdom))
		{
			return false;
		}
		if (speaker == null)
		{
			return false;
		}
		targetKingdom = speaker.Clan?.Kingdom ?? speaker.MapFaction as Kingdom;
		if (!IsValidKingdom(targetKingdom) || targetKingdom == playerKingdom)
		{
			return false;
		}
		return IsKingdomRuler(speaker, targetKingdom);
	}

	private static bool TryParseVassalageType(string typeToken, out AfVassalageType type)
	{
		string text = (typeToken ?? "").Trim();
		if (string.Equals(text, "TRIBUTARY", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "PROTECTORATE", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "朝贡国", StringComparison.OrdinalIgnoreCase))
		{
			type = AfVassalageType.Tributary;
			return true;
		}
		if (string.Equals(text, "GARRISON", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "MILITARY", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "卫戍国", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "军事附庸", StringComparison.OrdinalIgnoreCase))
		{
			type = AfVassalageType.Garrison;
			return true;
		}
		if (string.Equals(text, "VASSAL", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "PUPPET", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "附庸国", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "傀儡国", StringComparison.OrdinalIgnoreCase))
		{
			type = AfVassalageType.Vassal;
			return true;
		}
		type = AfVassalageType.Vassal;
		return false;
	}

	private static Kingdom ResolveKingdomByToken(string kingdomToken, Hero fallbackHero)
	{
		string text = (kingdomToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text)
			|| text.Equals("self", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("npc", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("current", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("auto", StringComparison.OrdinalIgnoreCase))
		{
			return fallbackHero?.Clan?.Kingdom ?? fallbackHero?.MapFaction as Kingdom;
		}
		Kingdom kingdom = ResolveKingdomById(text);
		if (kingdom != null)
		{
			return kingdom;
		}
		try
		{
			return Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.Name?.ToString() ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return fallbackHero?.Clan?.Kingdom ?? fallbackHero?.MapFaction as Kingdom;
		}
	}

	private static Kingdom GetPlayerKingdom()
	{
		return Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
	}

	private static bool IsPlayerRuler(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && Clan.PlayerClan != null && (kingdom.RulingClan == Clan.PlayerClan || kingdom.Leader == Hero.MainHero);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsKingdomRuler(Hero hero, Kingdom kingdom)
	{
		try
		{
			return hero != null && kingdom != null && (kingdom.Leader == hero || kingdom.RulingClan?.Leader == hero);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsValidKingdom(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
		catch
		{
			return kingdom != null && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
	}

	private static bool IsNpcTributaryVassalageAgreement(VassalageAgreement agreement)
	{
		if (agreement == null || !agreement.IsValid() || NormalizeVassalageType(agreement.Type) != AfVassalageType.Tributary)
		{
			return false;
		}
		Kingdom suzerain = agreement.ResolveSuzerain();
		Kingdom vassal = agreement.ResolveVassal();
		if (!IsValidKingdom(suzerain) || !IsValidKingdom(vassal))
		{
			return false;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		return playerKingdom == null || (suzerain != playerKingdom && vassal != playerKingdom);
	}

	private static bool IsAtWar(Kingdom left, Kingdom right)
	{
		try
		{
			return left != null && right != null && left != right && FactionManager.IsAtWarAgainstFaction(left, right);
		}
		catch
		{
			return false;
		}
	}

	private static string BuildInfoNoticeId(string category, string primaryKingdomId, string secondaryKingdomId)
	{
		string kind = string.IsNullOrWhiteSpace(category) ? "general" : category.Trim();
		string primary = string.IsNullOrWhiteSpace(primaryKingdomId) ? "none" : primaryKingdomId.Trim();
		string secondary = string.IsNullOrWhiteSpace(secondaryKingdomId) ? "none" : secondaryKingdomId.Trim();
		return "info:" + kind + ":" + primary + ":" + secondary + ":" + GetCurrentCampaignDay().ToString(CultureInfo.InvariantCulture) + ":" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildProtectionNoticeId(string vassalKingdomId, string enemyKingdomId)
	{
		string vassal = (vassalKingdomId ?? "").Trim();
		string enemy = (enemyKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(vassal) || string.IsNullOrWhiteSpace(enemy))
		{
			return "";
		}
		return "protect:" + vassal + ":" + enemy;
	}

	private static string BuildNpcTributaryVassalageNoticeId(string agreementId)
	{
		string id = (agreementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return "";
		}
		return "npc_tribute_vassalage:" + id;
	}

	private static string BuildTributaryPaymentNoticeId(string tributaryKingdomId, int settlementDay)
	{
		string tributary = (tributaryKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(tributary))
		{
			return "";
		}
		return "tribute:" + tributary + ":" + Math.Max(0, settlementDay).ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildEstablishedNoticeDescription(VassalageAgreement agreement)
	{
		Kingdom vassal = agreement?.ResolveVassal();
		AfVassalageType type = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Military);
		string subjectName = GetKingdomDisplayName(vassal, "臣属国");
		switch (type)
		{
		case AfVassalageType.Tributary:
			return subjectName + "已立誓为朝贡国，按期进献贡赋，接受你的庇护。";
		case AfVassalageType.Garrison:
			return subjectName + "已立誓为卫戍国，接受宗主号令，承担边境守卫与随军作战义务。";
		default:
			return subjectName + "已立誓为附庸国，接受宗主统辖，承担贡赋与出兵义务。";
		}
	}

	private static string BuildNpcTributaryVassalageNoticeDescription(VassalageAgreement agreement)
	{
		Kingdom suzerain = agreement?.ResolveSuzerain();
		Kingdom vassal = agreement?.ResolveVassal();
		return GetKingdomDisplayName(vassal, "某国") + "已承认" + GetKingdomDisplayName(suzerain, "另一国") + "的宗主权，成为朝贡国。";
	}

	private static string BuildEstablishedNoticeDetail(VassalageAgreement agreement)
	{
		Kingdom suzerain = agreement?.ResolveSuzerain();
		Kingdom vassal = agreement?.ResolveVassal();
		AfVassalageType type = agreement?.Type ?? AfVassalageType.Military;
		string suzerainName = GetKingdomDisplayName(suzerain, "宗主国");
		string vassalName = GetKingdomDisplayName(vassal, "臣属国");
		string subjectTypeText = GetVassalageTypeDisplayName(type);

		return "使节呈上盖有王印的臣服誓书：\n\n"
			+ "【臣属条约】\n"
			+ "宗主国：" + suzerainName + "\n"
			+ "臣属国：" + vassalName + "\n"
			+ "条约类型：" + subjectTypeText + "\n"
			+ "立约日：" + FormatCampaignDate(agreement?.CreatedDay ?? 0) + "\n\n"
			+ "【基本条款】\n"
			+ "一、" + subjectTypeText + "承认" + suzerainName + "的宗主权，并以本条约身份继续存在。\n"
			+ "二、" + subjectTypeText + "保留王号、宫廷、家族与内部治理；本条约不视为吞并。\n"
			+ BuildTreatySpecificClauses(type, suzerainName);
	}

	private static string BuildTreatySpecificClauses(AfVassalageType type, string suzerainName)
	{
		switch (NormalizeVassalageType(type))
		{
		case AfVassalageType.Tributary:
			return "三、朝贡国应定期输送贡赋，以换取" + suzerainName + "的庇护。\n"
				+ "四、朝贡国保留军事自主权，不随宗主国出征；朝贡国主动挑起的战争，宗主国无义务介入。\n"
				+ "五、朝贡国受外敌进攻时，可请求宗主国履行庇护；宗主国拒绝庇护，则朝贡条约终止。\n"
				+ "六、立约时朝贡国已经承受的外敌战事，由宗主国按庇护义务接手处置。";
		case AfVassalageType.Garrison:
			return "三、卫戍国进入" + suzerainName + "的军事体系，承担随宗主作战与边境屏障义务，但不缴纳贡赋。\n"
				+ "四、卫戍国不得自行宣战，也不得绕开宗主国单独处理战争事务。\n"
				+ "五、宗主国拒绝防卫、攻击卫戍国或长期不能维护其安全，将削弱忠诚；忠诚崩溃时，卫戍国可宣布脱离宗主控制。";
		default:
			return "三、附庸国接受" + suzerainName + "控制，承担贡赋与出兵义务。\n"
				+ "四、附庸国不得独立宣战或单独媾和，默认跟随宗主国战争；遭宣战时宗主国自动保护。\n"
				+ "五、附庸国王权受宗主国约束；除宗主国主动放弃或另行议定外，不按卫戍国规则自动脱离。";
		}
	}

	private string BuildProtectionNoticeDescription(VassalageAgreement agreement, Kingdom vassal, Kingdom enemy)
	{
		AfVassalageType type = NormalizeVassalageType(agreement?.Type ?? AfVassalageType.Tributary);
		string subjectText = BuildPlayerSubjectWarNoticeName(vassal, type);
		string attackerText = BuildWarNoticeKingdomName(enemy);
		switch (type)
		{
		case AfVassalageType.Tributary:
			return subjectText + "遭到" + attackerText + "宣战，急使请求履行庇护。";
		case AfVassalageType.Garrison:
			return subjectText + "遭到" + attackerText + "宣战，宫廷需裁定是否出兵保护。";
		default:
			return subjectText + "遭到" + attackerText + "宣战；宗主国已按条约自动保护。";
		}
	}

	private static string BuildTributaryPaymentNoticeDescription(TributaryPaymentNoticeRecord record)
	{
		if (record == null)
		{
			return "朝贡车队抵达宫廷，贡赋已经入库。";
		}
		return (string.IsNullOrWhiteSpace(record.TributaryName) ? "朝贡国" : record.TributaryName)
			+ "按期送来贡赋，宗主国各领地已完成入账。";
	}

	private static string BuildTributaryPaymentNoticeDetail(TributaryPaymentNoticeRecord record)
	{
		if (record == null)
		{
			return "贡赋簿册已经失效。";
		}
		string tributaryName = string.IsNullOrWhiteSpace(record.TributaryName) ? "朝贡国" : record.TributaryName;
		string playerGainText = BuildTributaryPaymentSettlementGainText(record);
		string tributaryCostText = BuildTributaryPaymentClassifiedTributaryCostText(record);
		if (string.IsNullOrWhiteSpace(tributaryCostText))
		{
			tributaryCostText = "繁荣度 -" + FormatTributaryPaymentNumber(record.TributaryProsperityLoss) + "（城镇 " + record.TributaryTownCount.ToString(CultureInfo.InvariantCulture) + "，城堡 " + record.TributaryCastleCount.ToString(CultureInfo.InvariantCulture) + "）\n"
				+ "粮食 -" + FormatTributaryPaymentNumber(record.TributaryFoodLoss) + "\n"
				+ "户数 -" + FormatTributaryPaymentNumber(record.TributaryHearthLoss) + "（村庄 " + record.TributaryVillageCount.ToString(CultureInfo.InvariantCulture) + "）";
		}
		return "朝贡车队已抵达宫廷，贡赋入库簿册如下：\n\n"
			+ "臣属国：" + tributaryName + "\n"
			+ "入库日期：" + FormatCampaignDate(record.SettlementDay) + "\n"
			+ "贡赋价值：" + FormatTributaryPaymentNumber(record.TributaryStrength) + "\n\n"
			+ "【宗主国各领地所得】\n"
			+ playerGainText
			+ "\n"
			+ "【臣属国贡赋消耗】\n"
			+ tributaryCostText;
	}

	private static string BuildTributaryPaymentSettlementGainText(TributaryPaymentNoticeRecord record)
	{
		string classified = BuildTributaryPaymentClassifiedPlayerGainText(record, showPaymentRatio: true);
		if (!string.IsNullOrWhiteSpace(classified))
		{
			return classified;
		}
		if (record?.PlayerSettlementGainLines != null)
		{
			List<string> lines = record.PlayerSettlementGainLines
				.Where((string x) => !string.IsNullOrWhiteSpace(x))
				.Select((string x) => x.Trim())
				.ToList();
			if (lines.Count > 0)
			{
				return string.Join("\n", lines.Select((string x) => "- " + x)) + "\n";
			}
		}
		return "本次贡赋未能分配到宗主国领地。\n";
	}

	private static string BuildTributaryPaymentClassifiedPlayerGainText(TributaryPaymentNoticeRecord record, bool showPaymentRatio)
	{
		if (!HasTributaryPaymentClassifiedNoticeData(record))
		{
			return "";
		}
		string text = "";
		text += BuildTributaryPaymentPlayerFortificationTypeLine(
			"城镇",
			record.PlayerTownCount,
			record.TownProsperityGainPerFief,
			record.TownFoodGainPerFief,
			record.ProsperityPaymentRatio,
			record.FoodPaymentRatio,
			record.PlayerTownProsperityGain,
			record.PlayerTownFoodGain,
			showPaymentRatio);
		text += BuildTributaryPaymentPlayerFortificationTypeLine(
			"城堡",
			record.PlayerCastleCount,
			record.CastleProsperityGainPerFief,
			record.CastleFoodGainPerFief,
			record.ProsperityPaymentRatio,
			record.FoodPaymentRatio,
			record.PlayerCastleProsperityGain,
			record.PlayerCastleFoodGain,
			showPaymentRatio);
		text += BuildTributaryPaymentPlayerVillageTypeLine(
			record.PlayerVillageCount,
			record.VillageHearthGainPerFief,
			record.HearthPaymentRatio,
			record.PlayerVillageHearthGain,
			showPaymentRatio);
		return text;
	}

	private static string BuildTributaryPaymentPlayerFortificationTypeLine(string label, int count, int prosperityPerFief, int foodPerFief, float prosperityPaymentRatio, float foodPaymentRatio, float actualProsperityTotal, float actualFoodTotal, bool showPaymentRatio)
	{
		if (count <= 0)
		{
			return "";
		}
		float appliedProsperityPerFief = Math.Max(0f, prosperityPerFief * ClampTributaryPaymentRatio(prosperityPaymentRatio));
		float appliedFoodPerFief = Math.Max(0f, foodPerFief * ClampTributaryPaymentRatio(foodPaymentRatio));
		float expectedFoodTotal = Math.Max(0f, appliedFoodPerFief * count);
		return label + "（" + count.ToString(CultureInfo.InvariantCulture) + "）：每座入库 繁荣度 "
			+ BuildTributaryPaymentPerFiefGainText(appliedProsperityPerFief, prosperityPerFief, prosperityPaymentRatio, "座", showPaymentRatio)
			+ "，粮食 "
			+ BuildTributaryPaymentPerFiefGainText(appliedFoodPerFief, foodPerFief, foodPaymentRatio, "座", showPaymentRatio)
			+ BuildTributaryPaymentFoodLimitNote(actualFoodTotal, expectedFoodTotal)
			+ "\n";
	}

	private static string BuildTributaryPaymentPlayerVillageTypeLine(int count, int hearthPerFief, float hearthPaymentRatio, float actualHearthTotal, bool showPaymentRatio)
	{
		if (count <= 0)
		{
			return "";
		}
		float appliedHearthPerFief = Math.Max(0f, hearthPerFief * ClampTributaryPaymentRatio(hearthPaymentRatio));
		return "村庄（" + count.ToString(CultureInfo.InvariantCulture) + "）：每村增加 户数 "
			+ BuildTributaryPaymentPerFiefGainText(appliedHearthPerFief, hearthPerFief, hearthPaymentRatio, "村", showPaymentRatio)
			+ "\n";
	}

	private static string BuildTributaryPaymentPerFiefGainText(float appliedPerFief, float originalPerFief, float paymentRatio, string unitText, bool showPaymentRatio)
	{
		string text = "+" + FormatTributaryPaymentNumber(appliedPerFief) + "/" + unitText;
		if (showPaymentRatio && originalPerFief > 0f && ClampTributaryPaymentRatio(paymentRatio) < 0.999f)
		{
			text += "（原 +" + FormatTributaryPaymentNumber(originalPerFief) + "/" + unitText
				+ "，实缴率 " + FormatTributaryPaymentPercent(paymentRatio) + "）";
		}
		return text;
	}

	private static string BuildTributaryPaymentFoodLimitNote(float actualFoodTotal, float expectedFoodTotal)
	{
		return "";
	}

	private static string BuildTributaryPaymentClassifiedTributaryCostText(TributaryPaymentNoticeRecord record)
	{
		if (!HasTributaryPaymentClassifiedNoticeData(record))
		{
			return "";
		}
		string text = "";
		if (record.TributaryTownCount > 0)
		{
			text += "城镇（" + record.TributaryTownCount.ToString(CultureInfo.InvariantCulture) + "）：每座缴纳 繁荣度 -"
				+ FormatTributaryPaymentNumber(record.TributaryTownProsperityLoss / Math.Max(1, record.TributaryTownCount))
				+ "，食物库存 -" + FormatTributaryPaymentNumber(record.TributaryTownFoodLoss / Math.Max(1, record.TributaryTownCount)) + "\n";
		}
		if (record.TributaryCastleCount > 0)
		{
			text += "城堡（" + record.TributaryCastleCount.ToString(CultureInfo.InvariantCulture) + "）：每座缴纳 繁荣度 -"
				+ FormatTributaryPaymentNumber(record.TributaryCastleProsperityLoss / Math.Max(1, record.TributaryCastleCount))
				+ "，食物库存 -" + FormatTributaryPaymentNumber(record.TributaryCastleFoodLoss / Math.Max(1, record.TributaryCastleCount)) + "\n";
		}
		if (record.TributaryVillageCount > 0)
		{
			text += "村庄（" + record.TributaryVillageCount.ToString(CultureInfo.InvariantCulture) + "）：每村缴纳 户数 -"
				+ FormatTributaryPaymentNumber(record.TributaryVillageHearthLoss / Math.Max(1, record.TributaryVillageCount));
		}
		return text.TrimEnd('\n');
	}

	private static bool HasTributaryPaymentClassifiedNoticeData(TributaryPaymentNoticeRecord record)
	{
		return record != null
			&& (record.TownProsperityGainPerFief > 0
				|| record.TownFoodGainPerFief > 0
				|| record.CastleProsperityGainPerFief > 0
				|| record.CastleFoodGainPerFief > 0
				|| record.VillageHearthGainPerFief > 0
				|| record.PlayerTownProsperityGain > 0f
				|| record.PlayerTownFoodGain > 0f
				|| record.PlayerCastleProsperityGain > 0f
				|| record.PlayerCastleFoodGain > 0f
				|| record.PlayerVillageHearthGain > 0f
				|| record.TributaryTownProsperityLoss > 0f
				|| record.TributaryTownFoodLoss > 0f
				|| record.TributaryCastleProsperityLoss > 0f
				|| record.TributaryCastleFoodLoss > 0f
				|| record.TributaryVillageHearthLoss > 0f);
	}

	private static string BuildTributaryPaymentPlayerGainLine(string label, float actualGain, float plannedGain, float paymentRatio, string extraText, bool showPaymentRatio)
	{
		string text = label + "：+" + FormatTributaryPaymentNumber(actualGain);
		if (showPaymentRatio)
		{
			text += "（应得 +" + FormatTributaryPaymentNumber(plannedGain)
				+ "，实缴率 " + FormatTributaryPaymentPercent(paymentRatio);
			if (!string.IsNullOrWhiteSpace(extraText))
			{
				text += "；" + extraText;
			}
			return text + "）\n";
		}
		if (!string.IsNullOrWhiteSpace(extraText))
		{
			text += "（" + extraText + "）";
		}
		return text + "\n";
	}

	private static bool HasTributaryPaymentRatioInfo(TributaryPaymentNoticeRecord record)
	{
		return record != null
			&& (record.PlannedPlayerProsperityGain > 0f
				|| record.PlannedPlayerFoodGain > 0f
				|| record.PlannedPlayerHearthGain > 0f
				|| record.ProsperityPaymentRatio > 0f
				|| record.FoodPaymentRatio > 0f
				|| record.HearthPaymentRatio > 0f);
	}

	private static bool IsTributaryPaymentDiscounted(TributaryPaymentNoticeRecord record)
	{
		return HasTributaryPaymentRatioInfo(record)
			&& ((record.PlannedPlayerProsperityGain > 0f && record.ProsperityPaymentRatio < 0.999f)
				|| (record.PlannedPlayerFoodGain > 0f && record.FoodPaymentRatio < 0.999f)
				|| (record.PlannedPlayerHearthGain > 0f && record.HearthPaymentRatio < 0.999f));
	}

	private static string FormatTributaryPaymentPercent(float value)
	{
		return Math.Round(ClampTributaryPaymentRatio(value) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
	}

	private static string FormatTributaryPaymentNumber(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return "0";
		}
		return Math.Round(value).ToString(CultureInfo.InvariantCulture);
	}

	private static string FormatTributaryPaymentDiagnosticNumber(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return "0";
		}
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private static string GetVassalageTypeDisplayName(AfVassalageType type)
	{
		switch (NormalizeVassalageType(type))
		{
		case AfVassalageType.Tributary:
			return "朝贡国";
		case AfVassalageType.Garrison:
			return "卫戍国";
		default:
			return "附庸国";
		}
	}

	private static string FormatCampaignDate(int absoluteDay)
	{
		int daysPerSeason = GetDaysInSeasonSafe();
		int daysPerYear = GetDaysInYearSafe(daysPerSeason);
		int day = Math.Max(0, absoluteDay);
		int year = day / daysPerYear;
		int dayOfYear = day % daysPerYear;
		int seasonIndex = Math.Min(3, dayOfYear / daysPerSeason);
		int dayOfSeason = dayOfYear % daysPerSeason + 1;
		return year.ToString(CultureInfo.InvariantCulture) + "年 " + GetCampaignSeasonName(seasonIndex) + " "
			+ dayOfSeason.ToString(CultureInfo.InvariantCulture) + "日";
	}

	private static int GetDaysInSeasonSafe()
	{
		try
		{
			int daysInSeason = CampaignTime.DaysInSeason;
			if (daysInSeason > 0)
			{
				return daysInSeason;
			}
		}
		catch
		{
		}
		return 21;
	}

	private static int GetDaysInYearSafe(int daysInSeason)
	{
		try
		{
			int daysInYear = CampaignTime.DaysInYear;
			if (daysInYear > 0)
			{
				return daysInYear;
			}
		}
		catch
		{
		}
		return Math.Max(1, daysInSeason) * 4;
	}

	private static string GetCampaignSeasonName(int seasonIndex)
	{
		switch (seasonIndex)
		{
		case 0:
			return "春季";
		case 1:
			return "夏季";
		case 2:
			return "秋季";
		case 3:
			return "冬季";
		default:
			return "未知季节";
		}
	}

	private string BuildWarNoticeKingdomName(Kingdom kingdom)
	{
		VassalageAgreement agreement = GetPlayerVassalAgreement(kingdom);
		if (agreement != null)
		{
			return BuildPlayerSubjectWarNoticeName(kingdom, NormalizeVassalageType(agreement.Type));
		}
		return GetKingdomDisplayName(kingdom, "敌国");
	}

	private static string BuildPlayerSubjectWarNoticeName(Kingdom kingdom, AfVassalageType type)
	{
		string typeText = GetVassalageTypeDisplayName(type);
		return "你的" + typeText + GetKingdomDisplayName(kingdom, "该国");
	}

	private static string BuildTributaryPaymentFortificationGainLine(Settlement settlement, float prosperityGain, float foodGain)
	{
		return GetSettlementDisplayName(settlement)
			+ "：繁荣度 +" + FormatTributaryPaymentNumber(prosperityGain)
			+ "，粮食 +" + FormatTributaryPaymentNumber(foodGain);
	}

	private static string BuildTributaryPaymentVillageGainLine(Settlement settlement, float hearthGain)
	{
		return GetSettlementDisplayName(settlement)
			+ "：户数 +" + FormatTributaryPaymentNumber(hearthGain);
	}

	private static string GetSettlementDisplayName(Settlement settlement)
	{
		try
		{
			string text = settlement?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return settlement?.StringId ?? "未知定居点";
	}

	private static string GetKingdomDisplayName(Kingdom kingdom, string fallback)
	{
		try
		{
			string text = kingdom?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return kingdom?.StringId ?? fallback ?? "未知王国";
	}

	private static string GetHeroDisplayName(Hero hero, string fallback)
	{
		try
		{
			string text = hero?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return hero?.StringId ?? fallback ?? "对方";
	}
}

[HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
internal static class Patch_Vassalage_DeclareWarAction
{
	public static bool Prefix(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		return VassalageBehavior.Instance?.ShouldAllowDeclareWarAction(faction1, faction2, declareWarDetail) ?? true;
	}
}

[HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
internal static class Patch_Vassalage_MakePeaceAction
{
	public static bool Prefix(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration, MakePeaceAction.MakePeaceDetail detail)
	{
		return VassalageBehavior.Instance?.ShouldAllowMakePeaceAction(faction1, faction2, detail) ?? true;
	}
}

[HarmonyPatch(typeof(CampaignInformationManager), "NewLogEntryAdded")]
internal static class Patch_Vassalage_CampaignInformationManager_NewLogEntryAdded
{
	public static bool Prefix(LogEntry log)
	{
		return VassalageBehavior.ShouldAllowCampaignLogNotification(log);
	}
}

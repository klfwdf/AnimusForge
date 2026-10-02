using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

internal sealed class WorldDiplomacyMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string DocumentId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public WorldDiplomacyMapNotification(string documentId, string title, string description)
		: base(new TextObject(string.IsNullOrWhiteSpace(description) ? "点击查看外交宣言。" : description))
	{
		DocumentId = (documentId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(title) ? "新的外交宣言" : title);
	}

	public override bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(DocumentId);
	}
}

internal sealed class WorldDiplomacyMapNotificationItemVM : MapNotificationItemBaseVM
{
	public WorldDiplomacyMapNotificationItemVM(WorldDiplomacyMapNotification data)
		: base(data)
	{
		WorldDiplomacyUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = WorldDiplomacyUiSprites.NotificationIdentifier;
		_onInspect = delegate
		{
			if (WorldDiplomacyPresentation.OpenDocumentFromNotification(data.DocumentId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal static class WorldDiplomacyUiSprites
{
	public const string NotificationIdentifier = "af_world_diplomacy_notice";
	private const string Source = "WorldDiplomacyUiSprites";
	private const string Category = "af_world_diplomacy";
	private const string FileName = "af_world_diplomacy_notice_v2.png";
	private const string BrushName = "Map.Notification.Type.Circle.Image";
	private static readonly string SpriteName = Category + "\\" + NotificationIdentifier;
	private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static BannerlordUiSprite _runtimeSprite;
	private static bool _patched;
	private static bool _brushApplied;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.world.diplomacy.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		TryPatch(patcher, "RefreshBrushFactory", nameof(RefreshBrushFactoryPostfix));
		EnsureInstalledForNotificationUi();
	}

	public static void EnsureInstalledForNotificationUi()
	{
		TryInstallRuntimeSprite();
		TryApplyBrushLayerSprite();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprite();
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
			if (target != null)
			{
				harmony.Patch(target, postfix: new HarmonyMethod(typeof(WorldDiplomacyUiSprites), postfixName));
			}
		}
		catch (Exception ex)
		{
			LogOnce("patch-" + targetName, ex.Message);
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
			string filePath = Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "GUI", "SpriteParts", Category, FileName);
			if (!File.Exists(filePath))
			{
				LogOnce("file-missing", "file missing: " + filePath);
				return;
			}
			BannerlordEngineTexture engineTexture = null;
			try
			{
				engineTexture = BannerlordEngineTexture.CreateFromMemory(File.ReadAllBytes(filePath));
			}
			catch
			{
			}
			engineTexture ??= BannerlordEngineTexture.LoadTextureFromPath(Path.GetFileName(filePath), Path.GetDirectoryName(filePath));
			if (engineTexture == null)
			{
				LogOnce("texture-null", "native texture loader returned null");
				return;
			}
			try
			{
				engineTexture.Name = SpriteName;
				engineTexture.SetTextureAsAlwaysValid();
				engineTexture.PreloadTexture(true);
			}
			catch
			{
			}
			int width = engineTexture.Width > 0 ? engineTexture.Width : 2048;
			int height = engineTexture.Height > 0 ? engineTexture.Height : 2048;
			BannerlordUiTexture uiTexture = new BannerlordUiTexture(new EngineTexture(engineTexture));
			_runtimeSprite = new RuntimeTextureSprite(SpriteName, uiTexture, width, height);
			UIResourceManager.SpriteData.Sprites[SpriteName] = _runtimeSprite;
		}
		catch (Exception ex)
		{
			LogOnce("install", ex.Message);
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
			if (AnimusForgeRuntimeBrushSpriteGuard.TryApplyLayerStyle(brush, NotificationIdentifier, _runtimeSprite, out string failureReason))
			{
				Style style = brush.GetStyle(NotificationIdentifier);
				StyleLayer styleLayer = style?.GetLayer(NotificationIdentifier);
				if (styleLayer != null)
				{
					styleLayer.Sprite = _runtimeSprite;
					styleLayer.Color = TaleWorlds.Library.Color.White;
					styleLayer.ColorFactor = 1f;
					styleLayer.AlphaFactor = 1f;
					styleLayer.HueFactor = 0f;
					styleLayer.SaturationFactor = 0f;
					styleLayer.ValueFactor = 0f;
					styleLayer.ImageFitType = ImageFit.ImageFitTypes.Cover;
					styleLayer.ImageFitHorizontalAlignment = ImageFit.ImageHorizontalAlignments.Center;
					styleLayer.ImageFitVerticalAlignment = ImageFit.ImageVerticalAlignments.Center;
				}
				_brushApplied = true;
			}
			else if (!_brushApplied)
			{
				LogOnce("brush", failureReason);
			}
		}
		catch (Exception ex)
		{
			LogOnce("brush-exception", ex.Message);
		}
	}
	private static void LogOnce(string key, string message)
	{
		if (LoggedFailures.Add(key))
		{
			Logger.Log(Source, "[AF-WORLD-DIPLOMACY-UI] " + message);
		}
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

public sealed class WorldDiplomacyComposePopup
{
	private enum PendingCloseAction
	{
		None,
		Submit,
		Cancel
	}
	private static WorldDiplomacyComposePopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly WorldDiplomacyComposePopupVM _dataSource;
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private PendingCloseAction _pendingAction;
	private string _pendingBody = "";
	private bool _closed;

	public static bool IsOpen => _activePopup != null && !_activePopup._closed;

	private WorldDiplomacyComposePopup(ScreenBase screen, string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		_screen = screen;
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		_dataSource = new WorldDiplomacyComposePopupVM(title, subtitle, hint, HandleSubmit, HandleCancel);
		_layer = new GauntletLayer("WorldDiplomacyComposePopup", 4050, false);
	}

	public static bool Show(string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		ScreenBase screen = ScreenManager.TopScreen;
		if (screen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			WorldDiplomacyComposePopup popup = new WorldDiplomacyComposePopup(screen, title, subtitle, hint, onSubmit, onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("WorldDiplomacyComposePopup", "[ERROR] " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		WorldDiplomacyComposePopup popup = _activePopup;
		if (popup == null || popup._closed)
		{
			return;
		}
		try
		{
			if (popup._layer?.Input != null && (popup._layer.Input.IsHotKeyReleased("Exit") || popup._layer.Input.IsKeyReleased(InputKey.Escape)))
			{
				popup.HandleCancel();
			}
		}
		catch
		{
		}
		popup.ProcessPendingAction();
	}
	private void Open()
	{
		_layer.LoadMovie("WorldDiplomacyComposePopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}
	private void HandleSubmit(string body)
	{
		if (_pendingAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingBody = body ?? "";
		_pendingAction = PendingCloseAction.Submit;
	}
	private void HandleCancel()
	{
		if (_pendingAction == PendingCloseAction.None)
		{
			_pendingAction = PendingCloseAction.Cancel;
		}
	}
	private void ProcessPendingAction()
	{
		if (_pendingAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction action = _pendingAction;
		string body = _pendingBody;
		_pendingAction = PendingCloseAction.None;
		_pendingBody = "";
		Close(silent: true);
		if (action == PendingCloseAction.Submit)
		{
			_onSubmit?.Invoke(body);
		}
		else
		{
			_onCancel?.Invoke();
		}
	}
	private void Close(bool silent)
	{
		if (_closed)
		{
			return;
		}
		_closed = true;
		try
		{
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				Logger.Log("WorldDiplomacyComposePopup", "[WARN] " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class WorldDiplomacyComposePopupVM : ViewModel
{
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private string _titleText;
	private string _subtitleText;
	private string _hintText;
	private string _bodyText;
	private bool _canPublish;

	public WorldDiplomacyComposePopupVM(string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		TitleText = string.IsNullOrWhiteSpace(title) ? "撰写外交宣言" : title;
		SubtitleText = subtitle ?? "";
		HintText = hint ?? "";
		BodyText = "";
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	[DataSourceProperty]
	public string HintText
	{
		get => _hintText;
		set
		{
			if (value != _hintText)
			{
				_hintText = value;
				OnPropertyChangedWithValue(value, nameof(HintText));
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get => _bodyText;
		set
		{
			string clean = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, 6000);
			if (clean != _bodyText)
			{
				_bodyText = clean;
				OnPropertyChangedWithValue(clean, nameof(BodyText));
				CanPublish = !string.IsNullOrWhiteSpace(clean);
			}
		}
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		private set
		{
			if (value != _canPublish)
			{
				_canPublish = value;
				OnPropertyChangedWithValue(value, nameof(CanPublish));
			}
		}
	}

	public void ExecutePublish()
	{
		if (CanPublish)
		{
			_onSubmit?.Invoke(BodyText);
		}
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}

}

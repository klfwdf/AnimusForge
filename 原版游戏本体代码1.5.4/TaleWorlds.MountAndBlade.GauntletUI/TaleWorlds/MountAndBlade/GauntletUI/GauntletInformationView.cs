using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletInformationView : GlobalLayer
{
	private TooltipBaseVM _dataSource;

	private GauntletMovieIdentifier _movie;

	private GauntletLayer _layerAsGauntletLayer;

	private static GauntletInformationView _current;

	private const float _tooltipExtendTreshold = 0.18f;

	private float _gamepadTooltipExtendTimer;

	private GauntletInformationView()
	{
		_layerAsGauntletLayer = new GauntletLayer("Tooltip", 115000);
		InformationManager.OnShowTooltip += OnShowTooltip;
		InformationManager.OnHideTooltip += OnHideTooltip;
		InformationManager.IsAnyTooltipActiveInternal = (InformationManager.IsAnyTooltipActiveDelegate)Delegate.Combine(InformationManager.IsAnyTooltipActiveInternal, new InformationManager.IsAnyTooltipActiveDelegate(OnGetIsAnyTooltipActive));
		base.Layer = _layerAsGauntletLayer;
	}

	public static void Initialize()
	{
		if (_current == null)
		{
			_current = new GauntletInformationView();
			ScreenManager.AddGlobalLayer(_current, isFocusable: false);
			PropertyBasedTooltipVM.AddKeyType("MapClick", () => _current.GetKey("MapHotKeyCategory", "MapClick"));
			PropertyBasedTooltipVM.AddKeyType("FollowModifier", () => _current.GetKey("MapHotKeyCategory", "MapFollowModifier"));
			PropertyBasedTooltipVM.AddKeyType("ExtendModifier", () => _current.GetExtendTooltipKeyText());
		}
	}

	public static void OnFinalize()
	{
		if (_current != null)
		{
			InformationManager.OnShowTooltip -= _current.OnShowTooltip;
			InformationManager.OnHideTooltip -= _current.OnHideTooltip;
			InformationManager.IsAnyTooltipActiveInternal = (InformationManager.IsAnyTooltipActiveDelegate)Delegate.Remove(InformationManager.IsAnyTooltipActiveInternal, new InformationManager.IsAnyTooltipActiveDelegate(_current.OnGetIsAnyTooltipActive));
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_dataSource != null && (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt) || Input.IsKeyDown(InputKey.ControllerLBumper)))
		{
			_gamepadTooltipExtendTimer += dt;
		}
		else
		{
			_gamepadTooltipExtendTimer = 0f;
		}
		if (_dataSource != null)
		{
			_dataSource.Tick(dt);
			_dataSource.IsExtended = (Input.IsGamepadActive ? (_gamepadTooltipExtendTimer > 0.18f) : (_gamepadTooltipExtendTimer > 0f));
		}
	}

	private string GetExtendTooltipKeyText()
	{
		if (Input.IsGamepadActive)
		{
			return GetKey("MapHotKeyCategory", "MapFollowModifier");
		}
		return Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString();
	}

	private string GetKey(string categoryId, string keyId)
	{
		return Game.Current.GameTextManager.GetHotKeyGameText(categoryId, keyId).ToString();
	}

	private string GetKey(string categoryId, int keyId)
	{
		return Game.Current.GameTextManager.GetHotKeyGameText(categoryId, keyId).ToString();
	}

	private void OnShowTooltip(Type type, object[] args)
	{
		OnHideTooltip();
		if (InformationManager.RegisteredTypes.TryGetValue(type, out var value))
		{
			try
			{
				_dataSource = Activator.CreateInstance(value.TooltipType, type, args) as TooltipBaseVM;
				_movie = _layerAsGauntletLayer.LoadMovie(value.MovieName, _dataSource);
				return;
			}
			catch (Exception arg)
			{
				Debug.FailedAssert($"Failed to display tooltip of type: {type.FullName}. Exception: {arg}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletInformationView.cs", "OnShowTooltip", 112);
				return;
			}
		}
		Debug.FailedAssert("Unable to show tooltip. Either the given type or the corresponding tooltip type is not added to TooltipMappingProvider. Given type: " + type.FullName, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletInformationView.cs", "OnShowTooltip", 117);
	}

	private void OnHideTooltip()
	{
		_dataSource?.OnFinalize();
		if (_movie != null)
		{
			_layerAsGauntletLayer.ReleaseMovie(_movie);
		}
		_dataSource = null;
		_movie = null;
	}

	private void OnGetIsAnyTooltipActive(out bool isAnyTooltipActive, out bool isAnyTooltipExtended)
	{
		isAnyTooltipActive = _dataSource?.IsActive ?? false;
		isAnyTooltipExtended = _dataSource?.IsExtended ?? false;
	}
}

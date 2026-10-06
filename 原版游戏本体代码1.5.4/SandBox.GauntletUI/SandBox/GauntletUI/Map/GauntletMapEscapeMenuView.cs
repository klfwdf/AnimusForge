using System.Collections.Generic;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapEscapeMenuView))]
public class GauntletMapEscapeMenuView : MapView
{
	private const string StoryModeGameTypeStringId = "CampaignStoryMode";

	private GauntletLayer _layerAsGauntletLayer;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	private readonly List<EscapeMenuItemVM> _menuItems;

	public GauntletMapEscapeMenuView(List<EscapeMenuItemVM> items)
	{
		_menuItems = items;
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_escapeMenuDatasource = new EscapeMenuVM(_menuItems);
		InitializeCampaignStartingOptionsInfo();
		base.Layer = new GauntletLayer("MapEscapeMenu", 4400)
		{
			IsFocusLayer = true
		};
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_escapeMenuMovie = _layerAsGauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.MapScreen.AddLayer(base.Layer);
		base.MapScreen.PauseAmbientSounds();
		ScreenManager.TrySetFocus(base.Layer);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		HandleTick(dt);
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		HandleTick(dt);
	}

	private void HandleTick(float dt)
	{
		_escapeMenuDatasource.Tick(dt);
		if (base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu") || base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			MapScreen.Instance.CloseEscapeMenu();
		}
	}

	protected override bool IsEscaped()
	{
		return base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu");
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		base.Layer.InputRestrictions.ResetInputRestrictions();
		base.MapScreen.RemoveLayer(base.Layer);
		base.MapScreen.RestartAmbientSounds();
		ScreenManager.TryLoseFocus(base.Layer);
		base.Layer = null;
		_layerAsGauntletLayer = null;
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
	}

	protected override TutorialContexts GetTutorialContext()
	{
		return TutorialContexts.EscapeMenu;
	}

	private void InitializeCampaignStartingOptionsInfo()
	{
		Campaign current = Campaign.Current;
		if (current?.Options?.AdvancedStartOptionsData != null && !(current.GameTypeStringId == "CampaignStoryMode"))
		{
			AdvancedStartOptionsData advancedStartData = current.AdvancedStartData;
			List<TextObject> scenarioParameters = GetScenarioParameters(advancedStartData);
			string startScenario;
			if (scenarioParameters.Count > 0)
			{
				scenarioParameters.Insert(0, advancedStartData.GetSelectedScenarioName());
				startScenario = GameTexts.GameTextHelper.MergeTextObjectsWithComma(scenarioParameters, includeAnd: false).ToString();
			}
			else
			{
				startScenario = advancedStartData.GetSelectedScenarioName().ToString();
			}
			_escapeMenuDatasource.InitializeCampaignStartingOptionsInfo(startScenario, GetRawSeed(advancedStartData));
		}
	}

	private uint GetRawSeed(AdvancedStartOptionsData startOptions)
	{
		if (startOptions.HasValue("Seed"))
		{
			return startOptions.GetValue<uint>("Seed");
		}
		return Campaign.Current.Options.Seed;
	}

	private static List<TextObject> GetScenarioParameters(AdvancedStartOptionsData startOptions)
	{
		string scenario = startOptions.GetScenario();
		List<TextObject> list = new List<TextObject>();
		switch (scenario)
		{
		case "InvasionId":
			AddOptionValueTo(startOptions, list, "InvasionScenarioFactionId");
			break;
		case "unitedempire":
			AddOptionValueTo(startOptions, list, "UnitedEmpireUnifierKingdomId");
			break;
		case "LastStand":
			AddOptionValueTo(startOptions, list, "LastStandKingdomId");
			break;
		case "twofactionwar":
			AddOptionValueTo(startOptions, list, "TwoFactionWarFaction1Id");
			AddOptionValueTo(startOptions, list, "TwoFactionWarFaction2Id");
			break;
		case "alternativecalradia":
			AddOptionValueTo(startOptions, list, "AlternativeCalradiaVariantId");
			break;
		}
		return list;
	}

	private static void AddOptionValueTo(AdvancedStartOptionsData startOptions, List<TextObject> list, string optionId)
	{
		list.Add(startOptions.GetDisplayName(optionId));
	}
}

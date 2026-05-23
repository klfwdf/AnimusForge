using System.Collections.Generic;
using SandBox;
using SandBox.View;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using StoryMode.Extensions;
using StoryMode.View.Permissions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace StoryMode.View;

public class StoryModeViewSubModule : MBSubModuleBase
{
	private bool _startedStoryMode;

	public override void OnGameInitializationFinished(Game game)
	{
		base.OnGameInitializationFinished(game);
		StoryModePermissionsSystem.OnInitialize();
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		StoryModePermissionsSystem.OnUnload();
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		TextObject coreContentDisabledReason = new TextObject("{=V8BXjyYq}Disabled during installation.");
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("StoryModeNewGame", new TextObject("{=sf_menu_storymode_new_game}New Campaign"), 2, delegate
		{
			StartGame();
		}, () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason)));
		Module.CurrentModule.ImguiProfilerTick += OnImguiProfilerTick;
	}

	protected virtual void FillDataForCampaign()
	{
	}

	protected override void OnSubModuleUnloaded()
	{
		Module.CurrentModule.ImguiProfilerTick -= OnImguiProfilerTick;
		base.OnSubModuleUnloaded();
	}

	public override void OnSubModuleDeactivated()
	{
	}

	public override void OnSubModuleActivated()
	{
	}

	private void StartGame()
	{
		_startedStoryMode = true;
		MBGameManager.StartNewGame(new SandBoxGameManager(() => new CampaignStoryMode(CampaignGameMode.Campaign)));
		_startedStoryMode = false;
	}

	protected override void OnBeforeGameStart(MBGameManager mbGameManager, List<string> disabledModules)
	{
		if (mbGameManager is SandBoxGameManager sandBoxGameManager && (sandBoxGameManager.LoadingSavedGame ? (!sandBoxGameManager.MetaData.HasStoryMode()) : (!_startedStoryMode)))
		{
			disabledModules.Add("StoryMode");
		}
	}

	private void OnImguiProfilerTick()
	{
		if (Campaign.Current == null)
		{
			return;
		}
		MBReadOnlyList<MobileParty> all = MobileParty.All;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		List<EntityVisualManagerBase<PartyBase>> components = SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>();
		foreach (MobileParty item in all)
		{
			if (item.IsMilitia || item.IsGarrison)
			{
				continue;
			}
			if (item.IsVisible)
			{
				num++;
			}
			MapEntityVisual<PartyBase> mapEntityVisual = null;
			foreach (EntityVisualManagerBase<PartyBase> item2 in components)
			{
				MapEntityVisual<PartyBase> visualOfEntity = item2.GetVisualOfEntity(PartyBase.MainParty);
				if (visualOfEntity != null)
				{
					mapEntityVisual = visualOfEntity;
				}
			}
			if (mapEntityVisual == null)
			{
				continue;
			}
			if (mapEntityVisual is MobilePartyVisual mobilePartyVisual)
			{
				if (mobilePartyVisual.HumanAgentVisuals != null)
				{
					num2++;
				}
				if (mobilePartyVisual.MountAgentVisuals != null)
				{
					num2++;
				}
				if (mobilePartyVisual.CaravanMountAgentVisuals != null)
				{
					num2++;
				}
			}
			num3++;
		}
		Imgui.BeginMainThreadScope();
		Imgui.Begin("Bannerlord Campaign Statistics");
		Imgui.Columns(2);
		Imgui.Text("Name");
		Imgui.NextColumn();
		Imgui.Text("Count");
		Imgui.NextColumn();
		Imgui.Separator();
		Imgui.Text("Total Mobile Party");
		Imgui.NextColumn();
		Imgui.Text(num3.ToString());
		Imgui.NextColumn();
		Imgui.Text("Visible Mobile Party");
		Imgui.NextColumn();
		Imgui.Text(num.ToString());
		Imgui.NextColumn();
		Imgui.Text("Total Agent Visuals");
		Imgui.NextColumn();
		Imgui.Text(num2.ToString());
		Imgui.NextColumn();
		Imgui.End();
		Imgui.EndMainThreadScope();
	}
}

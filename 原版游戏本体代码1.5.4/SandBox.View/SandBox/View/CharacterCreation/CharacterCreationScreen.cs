using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.CharacterCreation;

[GameStateScreen(typeof(CharacterCreationState))]
public class CharacterCreationScreen : ScreenBase, ICharacterCreationStateHandler, IGameStateListener
{
	private const string CultureParameterId = "MissionCulture";

	private readonly CharacterCreationState _characterCreationStateState;

	private IEnumerable<ScreenLayer> _shownLayers;

	private CharacterCreationStageViewBase _currentStageView;

	private readonly Dictionary<Type, Type> _stageViews;

	private SoundEvent _cultureAmbientSoundEvent;

	private Scene _genericScene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	public CharacterCreationScreen(CharacterCreationState characterCreationState)
	{
		_characterCreationStateState = characterCreationState;
		characterCreationState.Handler = this;
		_stageViews = new Dictionary<Type, Type>();
		CollectUnorderedStages();
		_cultureAmbientSoundEvent = SoundEvent.CreateEventFromString("event:/mission/ambient/special/charactercreation", null);
		_cultureAmbientSoundEvent.Play();
		CreateGenericScene();
	}

	private void CreateGenericScene()
	{
		_genericScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		SceneInitializationData initData = new SceneInitializationData
		{
			InitPhysicsWorld = false
		};
		_genericScene.Read("character_menu_new", ref initData);
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_genericScene);
	}

	private void StopSound()
	{
		SoundManager.SetGlobalParameter("MissionCulture", 0f);
		_cultureAmbientSoundEvent?.Stop();
		_cultureAmbientSoundEvent = null;
	}

	void ICharacterCreationStateHandler.OnCharacterCreationFinalized()
	{
		LoadingWindow.EnableGlobalLoadingWindow();
	}

	void ICharacterCreationStateHandler.OnRefresh()
	{
		if (_shownLayers != null)
		{
			ScreenLayer[] array = _shownLayers.ToArray();
			foreach (ScreenLayer layer in array)
			{
				RemoveLayer(layer);
			}
		}
		if (_currentStageView == null)
		{
			return;
		}
		_shownLayers = _currentStageView.GetLayers();
		if (_shownLayers != null)
		{
			ScreenLayer[] array = _shownLayers.ToArray();
			foreach (ScreenLayer layer2 in array)
			{
				AddLayer(layer2);
			}
		}
	}

	void ICharacterCreationStateHandler.OnStageCreated(CharacterCreationStageBase stage)
	{
		if (_stageViews.TryGetValue(stage.GetType(), out var value))
		{
			_currentStageView = Activator.CreateInstance(value, _characterCreationStateState.CharacterCreationManager, new ControlCharacterCreationStage(_characterCreationStateState.CharacterCreationManager.NextStage), new TextObject("{=Rvr1bcu8}Next"), new ControlCharacterCreationStage(_characterCreationStateState.CharacterCreationManager.PreviousStage), new TextObject("{=WXAaWZVf}Previous"), new ControlCharacterCreationStage(_characterCreationStateState.Refresh), new ControlCharacterCreationStageReturnInt(_characterCreationStateState.CharacterCreationManager.GetIndexOfCurrentStage), new ControlCharacterCreationStageReturnInt(_characterCreationStateState.CharacterCreationManager.GetTotalStagesCount), new ControlCharacterCreationStageReturnInt(_characterCreationStateState.CharacterCreationManager.GetFurthestIndex), new ControlCharacterCreationStageWithInt(_characterCreationStateState.CharacterCreationManager.GoToStage)) as CharacterCreationStageViewBase;
			stage.Listener = _currentStageView;
			_currentStageView.SetGenericScene(_genericScene);
		}
		else
		{
			_currentStageView = null;
		}
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (LoadingWindow.IsLoadingWindowActive)
		{
			LoadingWindow.DisableGlobalLoadingWindow();
		}
		_currentStageView?.Tick(dt);
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
	}

	void IGameStateListener.OnInitialize()
	{
		base.OnInitialize();
	}

	void IGameStateListener.OnFinalize()
	{
		base.OnFinalize();
		StopSound();
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_genericScene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_genericScene.ClearAll();
		_genericScene.ManualInvalidate();
		_genericScene = null;
	}

	private void CollectUnorderedStages()
	{
		Assembly assembly = typeof(CharacterCreationStageViewAttribute).Assembly;
		Assembly[] activeReferencingGameAssembliesSafe = assembly.GetActiveReferencingGameAssembliesSafe();
		CollectStagesFromAssembly(assembly);
		Assembly[] array = activeReferencingGameAssembliesSafe;
		foreach (Assembly assembly2 in array)
		{
			CollectStagesFromAssembly(assembly2);
		}
	}

	private void CollectStagesFromAssembly(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			if (typeof(CharacterCreationStageViewBase).IsAssignableFrom(item) && item.GetCustomAttributesSafe(typeof(CharacterCreationStageViewAttribute), inherit: true).FirstOrDefault() is CharacterCreationStageViewAttribute characterCreationStageViewAttribute)
			{
				if (_stageViews.ContainsKey(characterCreationStageViewAttribute.StageType))
				{
					_stageViews[characterCreationStageViewAttribute.StageType] = item;
				}
				else
				{
					_stageViews.Add(characterCreationStageViewAttribute.StageType, item);
				}
			}
		}
	}
}

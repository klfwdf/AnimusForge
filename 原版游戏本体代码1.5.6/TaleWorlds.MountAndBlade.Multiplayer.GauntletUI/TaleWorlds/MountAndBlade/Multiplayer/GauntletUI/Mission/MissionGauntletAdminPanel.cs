using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerAdminPanelUIHandler))]
public class MissionGauntletAdminPanel : MissionView
{
	public delegate IAdminPanelOptionProvider CreateOptionProviderDelegeate();

	public delegate MultiplayerAdminPanelOptionBaseVM CreateOptionViewModelDelegate(IAdminPanelOption option);

	public delegate MultiplayerAdminPanelOptionBaseVM CreateActionViewModelDelegate(IAdminPanelAction action);

	private GauntletLayer _gauntletLayer;

	private MultiplayerAdminPanelVM _dataSource;

	private GauntletMovieIdentifier _movie;

	private bool _isActive;

	private MultiplayerAdminComponent _multiplayerAdminComponent;

	private MissionLobbyComponent _missionLobbyComponent;

	private readonly MBList<CreateOptionProviderDelegeate> _optionProviderCreators;

	private readonly MBList<CreateOptionViewModelDelegate> _optionViewModelCreators;

	private readonly MBList<CreateActionViewModelDelegate> _actionViewModelCreators;

	public MissionGauntletAdminPanel()
	{
		ViewOrderPriority = 45;
		_optionProviderCreators = new MBList<CreateOptionProviderDelegeate>();
		_optionViewModelCreators = new MBList<CreateOptionViewModelDelegate>();
		_actionViewModelCreators = new MBList<CreateActionViewModelDelegate>();
		AddOptionViewModelCreator(CreateDefaultOptionViewModels);
		AddActionViewModelCreator(CreateDefaultActionViewModels);
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_multiplayerAdminComponent = base.Mission.GetMissionBehavior<MultiplayerAdminComponent>();
		_multiplayerAdminComponent.OnSetAdminMenuActiveState += OnShowAdminPanel;
		AddOptionProviderCreator(CreateDefaultAdminPanelOptionProvider);
	}

	private IAdminPanelOptionProvider CreateDefaultAdminPanelOptionProvider()
	{
		return new DefaultAdminPanelOptionProvider(_multiplayerAdminComponent, _missionLobbyComponent);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isActive && base.Mission.CurrentState != TaleWorlds.MountAndBlade.Mission.State.Continuing)
		{
			OnExitAdminPanel();
		}
		if (_isActive)
		{
			_dataSource.OnTick(dt);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		_multiplayerAdminComponent.OnSetAdminMenuActiveState -= OnShowAdminPanel;
		OnEscapeMenuToggled(isOpened: false);
		_dataSource?.OnFinalize();
		_dataSource = null;
		_optionProviderCreators.Clear();
		base.OnMissionScreenFinalize();
	}

	public override bool OnEscape()
	{
		if (_isActive)
		{
			OnExitAdminPanel();
			return true;
		}
		return base.OnEscape();
	}

	public void AddOptionProviderCreator(CreateOptionProviderDelegeate creator)
	{
		if (creator != null)
		{
			_optionProviderCreators.Add(creator);
		}
	}

	private void OnExitAdminPanel()
	{
		OnEscapeMenuToggled(isOpened: false);
	}

	private void OnShowAdminPanel(bool show)
	{
		OnEscapeMenuToggled(show);
	}

	public void AddOptionViewModelCreator(CreateOptionViewModelDelegate creator)
	{
		_optionViewModelCreators.Add(creator);
	}

	public void AddActionViewModelCreator(CreateActionViewModelDelegate creator)
	{
		_actionViewModelCreators.Add(creator);
	}

	private MultiplayerAdminPanelOptionBaseVM CreateDefaultOptionViewModels(IAdminPanelOption option)
	{
		if (option is IAdminPanelMultiSelectionOption option2)
		{
			return new MultiplayerAdminPanelMultiSelectionOptionVM(option2);
		}
		if (option is IAdminPanelAction option3)
		{
			return new MultiplayerAdminPanelActionOptionVM(option3);
		}
		if (option is IAdminPanelOption<string> option4)
		{
			return new MultiplayerAdminPanelStringOptionVM(option4);
		}
		if (option is IAdminPanelNumericOption option5)
		{
			return new MultiplayerAdminPanelNumericOptionVM(option5);
		}
		if (option is IAdminPanelOption<bool> option6)
		{
			return new MultiplayerAdminPanelToggleOptionVM(option6);
		}
		return null;
	}

	private MultiplayerAdminPanelOptionBaseVM CreateDefaultActionViewModels(IAdminPanelAction action)
	{
		return new MultiplayerAdminPanelActionOptionVM(action);
	}

	private MultiplayerAdminPanelOptionBaseVM OnCreateOptionViewModel(IAdminPanelOption option)
	{
		for (int num = _optionViewModelCreators.Count - 1; num >= 0; num--)
		{
			MultiplayerAdminPanelOptionBaseVM multiplayerAdminPanelOptionBaseVM = _optionViewModelCreators[num]?.Invoke(option);
			if (multiplayerAdminPanelOptionBaseVM != null)
			{
				return multiplayerAdminPanelOptionBaseVM;
			}
		}
		return null;
	}

	private MultiplayerAdminPanelOptionBaseVM OnCreateActionViewModel(IAdminPanelAction action)
	{
		for (int num = _actionViewModelCreators.Count - 1; num >= 0; num--)
		{
			MultiplayerAdminPanelOptionBaseVM multiplayerAdminPanelOptionBaseVM = _actionViewModelCreators[num]?.Invoke(action);
			if (multiplayerAdminPanelOptionBaseVM != null)
			{
				return multiplayerAdminPanelOptionBaseVM;
			}
		}
		return null;
	}

	private void OnEscapeMenuToggled(bool isOpened)
	{
		if (isOpened == _isActive || !base.MissionScreen.SetDisplayDialog(isOpened))
		{
			return;
		}
		_isActive = isOpened;
		if (isOpened)
		{
			if (_dataSource == null)
			{
				MBList<IAdminPanelOptionProvider> mBList = new MBList<IAdminPanelOptionProvider>();
				for (int i = 0; i < _optionProviderCreators.Count; i++)
				{
					IAdminPanelOptionProvider adminPanelOptionProvider = _optionProviderCreators[i]?.Invoke();
					if (adminPanelOptionProvider != null)
					{
						mBList.Add(adminPanelOptionProvider);
					}
				}
				_dataSource = new MultiplayerAdminPanelVM(OnEscapeMenuToggled, mBList, OnCreateOptionViewModel, OnCreateActionViewModel);
			}
			_gauntletLayer = new GauntletLayer("MultiplayerAdminPanel", ViewOrderPriority);
			_movie = _gauntletLayer.LoadMovie("MultiplayerAdminPanel", _dataSource);
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			base.MissionScreen.AddLayer(_gauntletLayer);
		}
		else
		{
			_dataSource?.OnFinalize();
			_dataSource = null;
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_gauntletLayer = null;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionMainAgentCheerBarkControllerVM : ViewModel
{
	private const string CheerId = "cheer";

	private const string BarkId = "bark";

	private const string NoneId = "none";

	private readonly Action<int> _onSelectCheer;

	private readonly Action<int> _onSelectBark;

	private List<string> _ownedTauntCosmetics;

	private IEnumerable<TauntIndexData> _playerTauntsWithIndices;

	private bool _isActive;

	private bool _isNodesCategories;

	private string _disabledReasonText;

	private string _selectedNodeText;

	private MBBindingList<CheerBarkNodeItemVM> _nodes;

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
				if (_isActive)
				{
					PopulateList();
				}
			}
		}
	}

	[DataSourceProperty]
	public string DisabledReasonText
	{
		get
		{
			return _disabledReasonText;
		}
		set
		{
			if (value != _disabledReasonText)
			{
				_disabledReasonText = value;
				OnPropertyChangedWithValue(value, "DisabledReasonText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedNodeText
	{
		get
		{
			return _selectedNodeText;
		}
		set
		{
			if (value != _selectedNodeText)
			{
				_selectedNodeText = value;
				OnPropertyChangedWithValue(value, "SelectedNodeText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNodesCategories
	{
		get
		{
			return _isNodesCategories;
		}
		set
		{
			if (value != _isNodesCategories)
			{
				_isNodesCategories = value;
				OnPropertyChangedWithValue(value, "IsNodesCategories");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CheerBarkNodeItemVM> Nodes
	{
		get
		{
			return _nodes;
		}
		set
		{
			if (value != _nodes)
			{
				_nodes = value;
				OnPropertyChangedWithValue(value, "Nodes");
			}
		}
	}

	public MissionMainAgentCheerBarkControllerVM(Action<int> onSelectCheer, Action<int> onSelectBark)
	{
		_onSelectCheer = onSelectCheer;
		_onSelectBark = onSelectBark;
		Nodes = new MBBindingList<CheerBarkNodeItemVM>();
		if (GameNetwork.IsMultiplayer)
		{
			_ownedTauntCosmetics = NetworkMain.GameClient.OwnedCosmetics.ToList();
			UpdatePlayerTauntIndices();
		}
		CheerBarkNodeItemVM.OnSelection += OnNodeFocused;
		CheerBarkNodeItemVM.OnNodeFocused += OnNodeTooltipToggled;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Nodes.ApplyActionOnAllItems(delegate(CheerBarkNodeItemVM n)
		{
			n.OnFinalize();
		});
		CheerBarkNodeItemVM.OnSelection -= OnNodeFocused;
		CheerBarkNodeItemVM.OnNodeFocused -= OnNodeTooltipToggled;
	}

	private void PopulateList()
	{
		bool flag = (IsNodesCategories = GameNetwork.IsClient);
		Nodes.Clear();
		GameKeyContext category = HotKeyManager.GetCategory("CombatHotKeyCategory");
		HotKey hotKey = category.GetHotKey("CheerBarkCloseMenu");
		SkinVoiceManager.SkinVoiceType[] mpBarks = SkinVoiceManager.VoiceType.MpBarks;
		if (flag)
		{
			HotKey hotKey2 = category.GetHotKey("CheerBarkSelectFirstCategory");
			CheerBarkNodeItemVM cheerBarkNodeItemVM = new CheerBarkNodeItemVM(new TextObject("{=KxH4VVU3}Taunt"), "cheer", hotKey2);
			Nodes.Add(cheerBarkNodeItemVM);
			TauntCosmeticElement[] array = new TauntCosmeticElement[TauntCosmeticElement.MaxNumberOfTaunts];
			foreach (TauntIndexData playerTauntsWithIndex in _playerTauntsWithIndices)
			{
				string tauntId = playerTauntsWithIndex.TauntId;
				int tauntIndex = playerTauntsWithIndex.TauntIndex;
				TauntCosmeticElement tauntCosmeticElement = CosmeticsManager.GetCosmeticElement(tauntId) as TauntCosmeticElement;
				if (!tauntCosmeticElement.IsFree && !_ownedTauntCosmetics.Contains(tauntId))
				{
					Debug.FailedAssert("Taunt list have invalid taunt: " + tauntId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\HUD\\MissionMainAgentCheerBarkControllerVM.cs", "PopulateList", 86);
				}
				else if (tauntIndex >= 0 && tauntIndex < TauntCosmeticElement.MaxNumberOfTaunts)
				{
					array[tauntIndex] = tauntCosmeticElement;
				}
			}
			for (int i = 0; i < array.Length; i++)
			{
				TauntCosmeticElement tauntCosmeticElement2 = array[i];
				if (tauntCosmeticElement2 != null)
				{
					int indexOfAction = TauntUsageManager.Instance.GetIndexOfAction(tauntCosmeticElement2.Id);
					TauntUsageManager.TauntUsage.TauntUsageFlag actionNotUsableReason = CosmeticsManagerHelper.GetActionNotUsableReason(Agent.Main, indexOfAction);
					cheerBarkNodeItemVM.AddSubNode(new CheerBarkNodeItemVM(tauntCosmeticElement2.Id, new TextObject("{=!}" + tauntCosmeticElement2.Name), tauntCosmeticElement2.Id, GetCheerShortcut(i), consoleOnlyShortcut: true, actionNotUsableReason));
				}
				else
				{
					cheerBarkNodeItemVM.AddSubNode(new CheerBarkNodeItemVM(string.Empty, TextObject.GetEmpty(), string.Empty, null, consoleOnlyShortcut: true));
				}
			}
			HotKey hotKey3 = category.GetHotKey("CheerBarkSelectSecondCategory");
			CheerBarkNodeItemVM cheerBarkNodeItemVM2 = new CheerBarkNodeItemVM(new TextObject("{=5Xoilj6r}Shout"), "bark", hotKey3);
			Nodes.Add(cheerBarkNodeItemVM2);
			cheerBarkNodeItemVM2.AddSubNode(new CheerBarkNodeItemVM(new TextObject("{=koX9okuG}None"), "none", hotKey, consoleOnlyShortcut: true));
			for (int j = 0; j < mpBarks.Length; j++)
			{
				cheerBarkNodeItemVM2.AddSubNode(new CheerBarkNodeItemVM(mpBarks[j].GetName(), "bark" + j, GetCheerShortcut(j), consoleOnlyShortcut: true));
			}
		}
		else
		{
			ActionIndexCache[] array2 = Agent.DefaultTauntActions.ToArray();
			Nodes.Add(new CheerBarkNodeItemVM(new TextObject("{=koX9okuG}None"), "none", hotKey, consoleOnlyShortcut: true));
			for (int k = 0; k < array2.Length; k++)
			{
				Nodes.Add(new CheerBarkNodeItemVM(new TextObject("{=!}" + (k + 1)), array2[k].GetName(), GetCheerShortcut(k), consoleOnlyShortcut: true));
			}
		}
		DisabledReasonText = string.Empty;
	}

	private void UpdatePlayerTauntIndices()
	{
		if (NetworkMain.GameClient?.PlayerData != null)
		{
			string playerId = NetworkMain.GameClient.PlayerData.UserId.ToString();
			_playerTauntsWithIndices = MultiplayerLocalDataManager.Instance.TauntSlotData.GetTauntIndicesForPlayer(playerId);
		}
	}

	private HotKey GetCheerShortcut(int cheerIndex)
	{
		GameKeyContext category = HotKeyManager.GetCategory("CombatHotKeyCategory");
		return cheerIndex switch
		{
			0 => category.GetHotKey("CheerBarkItem1"), 
			1 => category.GetHotKey("CheerBarkItem2"), 
			2 => category.GetHotKey("CheerBarkItem3"), 
			3 => category.GetHotKey("CheerBarkItem4"), 
			_ => null, 
		};
	}

	public void SelectItem(int itemIndex, int subNodeIndex = -1)
	{
		if (subNodeIndex == -1)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				Nodes[i].IsSelected = itemIndex == i;
			}
		}
		else if (itemIndex >= 0 && itemIndex < Nodes.Count)
		{
			for (int j = 0; j < Nodes[itemIndex].SubNodes.Count; j++)
			{
				Nodes[itemIndex].SubNodes[j].IsSelected = subNodeIndex == j;
			}
		}
	}

	public void ExecuteActivate()
	{
		IsActive = true;
	}

	public void ExecuteDeactivate(bool applySelection)
	{
		if (applySelection)
		{
			CheerBarkNodeItemVM cheerBarkNodeItemVM = Nodes.FirstOrDefault((CheerBarkNodeItemVM c) => c.IsSelected);
			if (cheerBarkNodeItemVM != null)
			{
				if (IsNodesCategories)
				{
					bool flag = cheerBarkNodeItemVM.TypeAsString == "bark";
					CheerBarkNodeItemVM cheerBarkNodeItemVM2 = cheerBarkNodeItemVM?.SubNodes.FirstOrDefault((CheerBarkNodeItemVM c) => c.IsSelected);
					if (cheerBarkNodeItemVM2 != null && cheerBarkNodeItemVM2.TypeAsString != "none")
					{
						if (flag)
						{
							_onSelectBark?.Invoke(cheerBarkNodeItemVM.SubNodes.IndexOf(cheerBarkNodeItemVM2) - 1);
						}
						else
						{
							int indexOfAction = TauntUsageManager.Instance.GetIndexOfAction(cheerBarkNodeItemVM2.TypeAsString);
							_onSelectCheer?.Invoke(indexOfAction);
						}
					}
				}
				else if (cheerBarkNodeItemVM.TypeAsString != "none")
				{
					int num = TauntUsageManager.Instance.GetIndexOfAction(cheerBarkNodeItemVM.TypeAsString);
					if (num == -1)
					{
						ActionIndexCache[] defaultTauntActions = Agent.DefaultTauntActions;
						for (int num2 = 0; num2 < defaultTauntActions.Length; num2++)
						{
							string name = defaultTauntActions[num2].GetName();
							if (cheerBarkNodeItemVM.TypeAsString == name)
							{
								num = num2;
								break;
							}
						}
					}
					_onSelectCheer?.Invoke(num);
				}
			}
		}
		Nodes.ApplyActionOnAllItems(delegate(CheerBarkNodeItemVM n)
		{
			n.IsSelected = false;
		});
		IsActive = false;
	}

	public void OnNodeFocused(CheerBarkNodeItemVM focusedNode)
	{
		string selectedNodeText = focusedNode?.CheerNameText ?? string.Empty;
		if (IsNodesCategories)
		{
			bool flag = focusedNode?.TypeAsString.Contains("bark") ?? false;
			string typeId = (flag ? "bark" : "cheer");
			Nodes.First((CheerBarkNodeItemVM c) => c.TypeAsString == typeId).SelectedNodeText = selectedNodeText;
		}
		else
		{
			SelectedNodeText = selectedNodeText;
		}
	}

	public void OnNodeTooltipToggled(CheerBarkNodeItemVM node)
	{
		if (node != null && node.TauntUsageDisabledReason != TauntUsageManager.TauntUsage.TauntUsageFlag.None)
		{
			DisabledReasonText = TauntUsageManager.GetActionDisabledReasonText(node.TauntUsageDisabledReason);
		}
		else
		{
			DisabledReasonText = string.Empty;
		}
	}
}

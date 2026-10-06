using System;
using Helpers;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class InventoryState : PlayerGameState
{
	public override bool IsMenuState => true;

	public InventoryLogic InventoryLogic { get; set; }

	public InventoryScreenHelper.InventoryMode InventoryMode { get; set; }

	public Action DoneLogicExtrasDelegate { get; set; }

	public IInventoryStateHandler Handler { get; set; }
}

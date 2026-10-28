using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GameMenu;

public class SettlementMenuPartyCharacterListsButtonWidget : ButtonWidget
{
	private bool _initialized;

	private ListPanel _childCharactersList;

	private ListPanel _childPartiesList;

	public Brush PartyListButtonBrush { get; set; }

	public Brush CharacterListButtonBrush { get; set; }

	public ContainerPageControlWidget CharactersList { get; set; }

	public ContainerPageControlWidget PartiesList { get; set; }

	public int MaxNumOfVisuals { get; set; } = 5;

	public ListPanel ChildCharactersList
	{
		get
		{
			return _childCharactersList;
		}
		set
		{
			if (value != _childCharactersList)
			{
				_childCharactersList = value;
				_childCharactersList.ItemAddEventHandlers.Add(OnListItemAdded);
			}
		}
	}

	public ListPanel ChildPartiesList
	{
		get
		{
			return _childPartiesList;
		}
		set
		{
			if (value != _childPartiesList)
			{
				_childPartiesList = value;
				_childPartiesList.ItemAddEventHandlers.Add(OnListItemAdded);
			}
		}
	}

	public SettlementMenuPartyCharacterListsButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.Brush = (ChildPartiesList.IsVisible ? PartyListButtonBrush : (ChildCharactersList.IsVisible ? CharacterListButtonBrush : null));
		if (!_initialized)
		{
			if (CharactersList.IsVisible)
			{
				SetCharacterListVisible();
			}
			else if (PartiesList.IsVisible)
			{
				SetPartyListVisible();
			}
			_initialized = true;
		}
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (!PartiesList.IsVisible && CharactersList.IsVisible)
		{
			SetPartyListVisible();
		}
		else if (PartiesList.IsVisible && !CharactersList.IsVisible)
		{
			SetCharacterListVisible();
		}
	}

	private void SetCharacterListVisible()
	{
		CharactersList.IsVisible = true;
		PartiesList.IsVisible = false;
		ChildPartiesList.IsVisible = true;
		ChildCharactersList.IsVisible = false;
	}

	private void SetPartyListVisible()
	{
		CharactersList.IsVisible = false;
		PartiesList.IsVisible = true;
		ChildPartiesList.IsVisible = false;
		ChildCharactersList.IsVisible = true;
	}

	private void OnListItemAdded(Widget parent, Widget child)
	{
		if (parent.ChildCount > MaxNumOfVisuals)
		{
			child.IsVisible = false;
		}
	}
}

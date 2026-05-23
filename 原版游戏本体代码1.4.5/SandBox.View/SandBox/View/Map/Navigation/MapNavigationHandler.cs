using System.Linq;
using SandBox.View.Map.Navigation.NavigationElements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SandBox.View.Map.Navigation;

public class MapNavigationHandler : INavigationHandler
{
	protected readonly Game _game;

	private INavigationElement[] _elements;

	public bool IsNavigationLocked { get; set; }

	public bool IsEscapeMenuActive => _elements.Any((INavigationElement e) => e is EscapeMenuNavigationElement escapeMenuNavigationElement && escapeMenuNavigationElement.IsActive);

	public INavigationElement[] GetElements()
	{
		return _elements;
	}

	public MapNavigationHandler()
	{
		_game = Game.Current;
		_elements = OnCreateElements();
	}

	public bool IsAnyElementActive()
	{
		for (int i = 0; i < _elements.Length; i++)
		{
			if (_elements[i].IsActive)
			{
				return true;
			}
		}
		return false;
	}

	protected virtual INavigationElement[] OnCreateElements()
	{
		return new INavigationElement[7]
		{
			new EscapeMenuNavigationElement(this),
			new CharacterDeveloperNavigationElement(this),
			new InventoryNavigationElement(this),
			new PartyNavigationElement(this),
			new QuestsNavigationElement(this),
			new ClanNavigationElement(this),
			new KingdomNavigationElement(this)
		};
	}

	public INavigationElement GetElement(string id)
	{
		for (int i = 0; i < _elements.Length; i++)
		{
			if (_elements[i].StringId == id)
			{
				return _elements[i];
			}
		}
		return null;
	}
}

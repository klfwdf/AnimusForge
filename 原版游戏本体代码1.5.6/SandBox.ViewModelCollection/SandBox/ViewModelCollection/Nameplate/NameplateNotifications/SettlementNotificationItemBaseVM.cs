using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications;

public class SettlementNotificationItemBaseVM : ViewModel
{
	private readonly Action<SettlementNotificationItemBaseVM> _onRemove;

	private CharacterImageIdentifierVM _characterVisual;

	private string _text;

	private string _characterName;

	private int _relationType;

	public int CreatedTick { get; set; }

	public string CharacterName
	{
		get
		{
			return _characterName;
		}
		set
		{
			if (value != _characterName)
			{
				_characterName = value;
				OnPropertyChangedWithValue(value, "CharacterName");
			}
		}
	}

	public int RelationType
	{
		get
		{
			return _relationType;
		}
		set
		{
			if (value != _relationType)
			{
				_relationType = value;
				OnPropertyChangedWithValue(value, "RelationType");
			}
		}
	}

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
			}
		}
	}

	public CharacterImageIdentifierVM CharacterVisual
	{
		get
		{
			return _characterVisual;
		}
		set
		{
			if (value != _characterVisual)
			{
				_characterVisual = value;
				OnPropertyChangedWithValue(value, "CharacterVisual");
			}
		}
	}

	public SettlementNotificationItemBaseVM(Action<SettlementNotificationItemBaseVM> onRemove, int createdTick)
	{
		_onRemove = onRemove;
		RelationType = 0;
		CreatedTick = createdTick;
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}

using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MultiplayerAdminInformationVM : ViewModel
{
	private MBBindingList<StringItemWithActionVM> _messageQueue;

	[DataSourceProperty]
	public MBBindingList<StringItemWithActionVM> MessageQueue
	{
		get
		{
			return _messageQueue;
		}
		set
		{
			if (value != _messageQueue)
			{
				_messageQueue = value;
				OnPropertyChangedWithValue(value, "MessageQueue");
			}
		}
	}

	public MultiplayerAdminInformationVM()
	{
		MessageQueue = new MBBindingList<StringItemWithActionVM>();
	}

	public void OnNewMessageReceived(string message)
	{
		StringItemWithActionVM item = new StringItemWithActionVM(ExecuteRemoveMessage, message, message);
		MessageQueue.Add(item);
	}

	private void ExecuteRemoveMessage(object messageToRemove)
	{
		int index = MessageQueue.FindIndex((StringItemWithActionVM m) => m.ActionText == messageToRemove as string);
		MessageQueue.RemoveAt(index);
	}
}

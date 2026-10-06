using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerOptionsDefault : GameNetworkMessage
{
	private readonly List<MultiplayerOptions.OptionType> _optionList;

	public MultiplayerOptionsDefault()
	{
		_optionList = new List<MultiplayerOptions.OptionType>();
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			if (optionType.GetOptionProperty().Replication != MultiplayerOptionsProperty.ReplicationOccurrence.Never)
			{
				_optionList.Add(optionType);
			}
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		for (int i = 0; i < _optionList.Count; i++)
		{
			MultiplayerOptions.OptionType optionType = _optionList[i];
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			switch (optionProperty.OptionValueType)
			{
			case MultiplayerOptions.OptionValueType.Bool:
			{
				bool value3 = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
				optionType.SetValue(value3, MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions);
				break;
			}
			case MultiplayerOptions.OptionValueType.Integer:
			case MultiplayerOptions.OptionValueType.Enum:
			{
				int value2 = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, maximumValueGiven: true), ref bufferReadValid);
				optionType.SetValue(value2, MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions);
				break;
			}
			case MultiplayerOptions.OptionValueType.String:
			{
				string value = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
				optionType.SetValue(value, MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions);
				break;
			}
			}
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		for (int i = 0; i < _optionList.Count; i++)
		{
			MultiplayerOptions.OptionType optionType = _optionList[i];
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			switch (optionProperty.OptionValueType)
			{
			case MultiplayerOptions.OptionValueType.Bool:
				GameNetworkMessage.WriteBoolToPacket(optionType.GetBoolValue(MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions));
				break;
			case MultiplayerOptions.OptionValueType.Integer:
			case MultiplayerOptions.OptionValueType.Enum:
				GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions), new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, maximumValueGiven: true));
				break;
			case MultiplayerOptions.OptionValueType.String:
				GameNetworkMessage.WriteStringToPacket(optionType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions));
				break;
			}
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Receiving default multiplayer options.";
	}
}

using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerOptionsImmediate : GameNetworkMessage
{
	private List<MultiplayerOptions.MultiplayerOption> _optionList;

	public MultiplayerOptionsImmediate()
	{
		_optionList = new List<MultiplayerOptions.MultiplayerOption>();
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			if (optionType.GetOptionProperty().Replication == MultiplayerOptionsProperty.ReplicationOccurrence.Immediately)
			{
				_optionList.Add(MultiplayerOptions.Instance.GetOptionFromOptionType(optionType));
			}
		}
	}

	public MultiplayerOptions.MultiplayerOption GetOption(MultiplayerOptions.OptionType optionType)
	{
		return _optionList.First((MultiplayerOptions.MultiplayerOption x) => x.OptionType == optionType);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		_optionList = new List<MultiplayerOptions.MultiplayerOption>();
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			if (optionProperty.Replication == MultiplayerOptionsProperty.ReplicationOccurrence.Immediately)
			{
				MultiplayerOptions.MultiplayerOption multiplayerOption = MultiplayerOptions.MultiplayerOption.CreateMultiplayerOption(optionType);
				switch (optionProperty.OptionValueType)
				{
				case MultiplayerOptions.OptionValueType.Bool:
					multiplayerOption.UpdateValue(GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid));
					break;
				case MultiplayerOptions.OptionValueType.Integer:
				case MultiplayerOptions.OptionValueType.Enum:
					multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, maximumValueGiven: true), ref bufferReadValid));
					break;
				case MultiplayerOptions.OptionValueType.String:
					multiplayerOption.UpdateValue(GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid));
					break;
				}
				_optionList.Add(multiplayerOption);
			}
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		foreach (MultiplayerOptions.MultiplayerOption option in _optionList)
		{
			MultiplayerOptions.OptionType optionType = option.OptionType;
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			switch (optionProperty.OptionValueType)
			{
			case MultiplayerOptions.OptionValueType.Bool:
				GameNetworkMessage.WriteBoolToPacket(optionType.GetBoolValue());
				break;
			case MultiplayerOptions.OptionValueType.Integer:
			case MultiplayerOptions.OptionValueType.Enum:
				GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(), new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, maximumValueGiven: true));
				break;
			case MultiplayerOptions.OptionValueType.String:
				GameNetworkMessage.WriteStringToPacket(optionType.GetStrValue());
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
		return "Receiving runtime multiplayer options.";
	}
}

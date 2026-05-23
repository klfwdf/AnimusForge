using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class AdminUpdateMultiplayerOptions : GameNetworkMessage
{
	public class AdminMultiplayerOptionInfo
	{
		public MultiplayerOptions.OptionType OptionType { get; }

		public MultiplayerOptions.MultiplayerOptionsAccessMode AccessMode { get; }

		public string StringValue { get; private set; }

		public bool BoolValue { get; private set; }

		public int IntValue { get; private set; }

		public AdminMultiplayerOptionInfo(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode)
		{
			OptionType = optionType;
			AccessMode = accessMode;
		}

		internal void SetValue(string value)
		{
			StringValue = value;
		}

		internal void SetValue(bool value)
		{
			BoolValue = value;
		}

		internal void SetValue(int value)
		{
			IntValue = value;
		}
	}

	public List<AdminMultiplayerOptionInfo> Options { get; private set; }

	public int OptionCount { get; private set; }

	public AdminUpdateMultiplayerOptions()
	{
		Options = new List<AdminMultiplayerOptionInfo>();
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Admin requesting update multiplayer options on server";
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		OptionCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 43, maximumValueGiven: true), ref bufferReadValid);
		for (int i = 0; i < OptionCount; i++)
		{
			AdminMultiplayerOptionInfo item = ReadOptionInfoFromPacket(ref bufferReadValid);
			Options.Add(item);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(Options.Count, new CompressionInfo.Integer(0, 43, maximumValueGiven: true));
		for (int i = 0; i < Options.Count; i++)
		{
			WriteOptionInfoToPacket(Options[i]);
		}
	}

	public void AddMultiplayerOption(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode, bool value)
	{
		AdminMultiplayerOptionInfo adminMultiplayerOptionInfo = new AdminMultiplayerOptionInfo(optionType, accessMode);
		adminMultiplayerOptionInfo.SetValue(value);
		Options.Add(adminMultiplayerOptionInfo);
	}

	public void AddMultiplayerOption(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode, int value)
	{
		AdminMultiplayerOptionInfo adminMultiplayerOptionInfo = new AdminMultiplayerOptionInfo(optionType, accessMode);
		adminMultiplayerOptionInfo.SetValue(value);
		Options.Add(adminMultiplayerOptionInfo);
	}

	public void AddMultiplayerOption(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode, string value)
	{
		AdminMultiplayerOptionInfo adminMultiplayerOptionInfo = new AdminMultiplayerOptionInfo(optionType, accessMode);
		adminMultiplayerOptionInfo.SetValue(value);
		Options.Add(adminMultiplayerOptionInfo);
	}

	private AdminMultiplayerOptionInfo ReadOptionInfoFromPacket(ref bool bufferReadValid)
	{
		int optionType = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 43, maximumValueGiven: true), ref bufferReadValid);
		MultiplayerOptions.MultiplayerOptionsAccessMode accessMode = (MultiplayerOptions.MultiplayerOptionsAccessMode)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 3, maximumValueGiven: true), ref bufferReadValid);
		AdminMultiplayerOptionInfo adminMultiplayerOptionInfo = new AdminMultiplayerOptionInfo((MultiplayerOptions.OptionType)optionType, accessMode);
		MultiplayerOptionsProperty optionProperty = ((MultiplayerOptions.OptionType)optionType).GetOptionProperty();
		switch (optionProperty.OptionValueType)
		{
		case MultiplayerOptions.OptionValueType.Bool:
		{
			bool value3 = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			adminMultiplayerOptionInfo.SetValue(value3);
			break;
		}
		case MultiplayerOptions.OptionValueType.Integer:
		case MultiplayerOptions.OptionValueType.Enum:
		{
			int value2 = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, maximumValueGiven: true), ref bufferReadValid);
			adminMultiplayerOptionInfo.SetValue(value2);
			break;
		}
		case MultiplayerOptions.OptionValueType.String:
		{
			string value = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
			adminMultiplayerOptionInfo.SetValue(value);
			break;
		}
		}
		return adminMultiplayerOptionInfo;
	}

	private void WriteOptionInfoToPacket(AdminMultiplayerOptionInfo optionInfo)
	{
		GameNetworkMessage.WriteIntToPacket((int)optionInfo.OptionType, new CompressionInfo.Integer(0, 43, maximumValueGiven: true));
		GameNetworkMessage.WriteIntToPacket((int)optionInfo.AccessMode, new CompressionInfo.Integer(0, 3, maximumValueGiven: true));
		MultiplayerOptionsProperty optionProperty = optionInfo.OptionType.GetOptionProperty();
		switch (optionProperty.OptionValueType)
		{
		case MultiplayerOptions.OptionValueType.Bool:
			GameNetworkMessage.WriteBoolToPacket(optionInfo.BoolValue);
			break;
		case MultiplayerOptions.OptionValueType.Integer:
		case MultiplayerOptions.OptionValueType.Enum:
			GameNetworkMessage.WriteIntToPacket(optionInfo.IntValue, new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, maximumValueGiven: true));
			break;
		case MultiplayerOptions.OptionValueType.String:
			GameNetworkMessage.WriteStringToPacket(optionInfo.StringValue);
			break;
		}
	}
}

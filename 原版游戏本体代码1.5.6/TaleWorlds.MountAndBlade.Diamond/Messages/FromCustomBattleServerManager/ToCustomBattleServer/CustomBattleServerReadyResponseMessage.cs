using System;
using System.Runtime.Serialization;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServerManager.ToCustomBattleServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServer", true)]
[DataContract]
public class CustomBattleServerReadyResponseMessage : LoginResultObject
{
}

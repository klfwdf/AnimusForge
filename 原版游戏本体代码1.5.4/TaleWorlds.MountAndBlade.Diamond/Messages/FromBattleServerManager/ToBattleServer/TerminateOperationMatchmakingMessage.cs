using System;
using TaleWorlds.Diamond;

namespace Messages.FromBattleServerManager.ToBattleServer;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServer", true)]
public class TerminateOperationMatchmakingMessage : Message
{
}

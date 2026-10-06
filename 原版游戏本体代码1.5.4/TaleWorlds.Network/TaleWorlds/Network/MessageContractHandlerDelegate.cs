namespace TaleWorlds.Network;

public delegate void MessageContractHandlerDelegate<T>(T message) where T : MessageContract;

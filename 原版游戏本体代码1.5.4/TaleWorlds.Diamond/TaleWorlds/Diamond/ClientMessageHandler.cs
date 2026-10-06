namespace TaleWorlds.Diamond;

public delegate void ClientMessageHandler<TMessage>(TMessage message) where TMessage : Message;

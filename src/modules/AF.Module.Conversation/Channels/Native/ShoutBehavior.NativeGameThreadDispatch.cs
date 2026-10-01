using System;
namespace AnimusForge;
public partial class ShoutBehavior
{
    private ConversationGameThreadDispatcher _conversationGameThreadDispatchRuntime;
    private ConversationGameThreadDispatcher _conversationGameThreadDispatcher => _conversationGameThreadDispatchRuntime ??= new ConversationGameThreadDispatcher(_pendingMainThreadFunctions, action => _mainThreadActions.Enqueue(action), IsBannerlordMainThreadForNativeActions);
}

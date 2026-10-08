using System;
using System.Collections.Generic;
namespace AnimusForge;
internal sealed class SceneRevisitStateOwner
{
 internal readonly object Gate;
 internal Dictionary<string,int> Days = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 internal readonly HashSet<string> HandledThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 internal readonly HashSet<string> FirstMeetingShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 internal SceneRevisitStateOwner(object gate) { Gate=gate??throw new ArgumentNullException(nameof(gate)); }
}

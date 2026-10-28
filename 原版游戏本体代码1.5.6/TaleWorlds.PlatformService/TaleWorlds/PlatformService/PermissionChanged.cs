using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService;

public delegate void PermissionChanged(PlayerId TargetPlayerId, Permission permission, bool HasPermission);

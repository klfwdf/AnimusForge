using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService;

public delegate void PermissionResults((PlayerId PlayerId, Permission Permission, bool HasPermission)[] results);

using System;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000012 RID: 18
	public interface ILoginAccessProvider
	{
		// Token: 0x06000057 RID: 87
		void Initialize(string preferredUserName, PlatformInitParams initParams);

		// Token: 0x06000058 RID: 88
		string GetUserName();

		// Token: 0x06000059 RID: 89
		PlayerId GetPlayerId();

		// Token: 0x0600005A RID: 90
		AccessObjectResult CreateAccessObject();
	}
}

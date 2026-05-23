using System;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000B1 RID: 177
	public static class MBObjectManagerExtensions
	{
		// Token: 0x06000949 RID: 2377 RVA: 0x0001E48C File Offset: 0x0001C68C
		public static void LoadXML(this MBObjectManager objectManager, string id, bool skipXmlFilterForEditor = false)
		{
			Game game = Game.Current;
			bool isDevelopment = false;
			string gameType = "";
			if (game != null)
			{
				isDevelopment = game.GameType.IsDevelopment;
				gameType = game.GameType.GameTypeStringId;
			}
			objectManager.LoadXML(id, isDevelopment, gameType, skipXmlFilterForEditor);
		}
	}
}

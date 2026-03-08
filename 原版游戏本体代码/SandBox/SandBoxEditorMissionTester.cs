using System;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000022 RID: 34
	internal class SandBoxEditorMissionTester : IEditorMissionTester
	{
		// Token: 0x0600010B RID: 267 RVA: 0x000070D9 File Offset: 0x000052D9
		void IEditorMissionTester.StartMissionForEditor(string missionName, string sceneName, string levels)
		{
			MBGameManager.StartNewGame(new EditorSceneMissionManager(missionName, sceneName, levels, false, "", false, 0f, 0f));
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000070F9 File Offset: 0x000052F9
		void IEditorMissionTester.StartMissionForReplayEditor(string missionName, string sceneName, string levels, string fileName, bool record, float startTime, float endTime)
		{
			MBGameManager.StartNewGame(new EditorSceneMissionManager(missionName, sceneName, levels, true, fileName, record, startTime, endTime));
		}
	}
}

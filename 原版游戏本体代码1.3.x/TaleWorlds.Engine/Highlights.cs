using System;
using System.Collections.Generic;

namespace TaleWorlds.Engine
{
	// Token: 0x02000050 RID: 80
	public class Highlights
	{
		// Token: 0x06000860 RID: 2144 RVA: 0x00006921 File Offset: 0x00004B21
		public static void Initialize()
		{
			EngineApplicationInterface.IHighlights.Initialize();
		}

		// Token: 0x06000861 RID: 2145 RVA: 0x0000692D File Offset: 0x00004B2D
		public static void OpenGroup(string id)
		{
			EngineApplicationInterface.IHighlights.OpenGroup(id);
		}

		// Token: 0x06000862 RID: 2146 RVA: 0x0000693A File Offset: 0x00004B3A
		public static void CloseGroup(string id, bool destroy = false)
		{
			EngineApplicationInterface.IHighlights.CloseGroup(id, destroy);
		}

		// Token: 0x06000863 RID: 2147 RVA: 0x00006948 File Offset: 0x00004B48
		public static void SaveScreenshot(string highlightId, string groupId)
		{
			EngineApplicationInterface.IHighlights.SaveScreenshot(highlightId, groupId);
		}

		// Token: 0x06000864 RID: 2148 RVA: 0x00006956 File Offset: 0x00004B56
		public static void SaveVideo(string highlightId, string groupId, int startDelta, int endDelta)
		{
			EngineApplicationInterface.IHighlights.SaveVideo(highlightId, groupId, startDelta, endDelta);
		}

		// Token: 0x06000865 RID: 2149 RVA: 0x00006968 File Offset: 0x00004B68
		public static void OpenSummary(List<string> groups)
		{
			string groups2 = string.Join("::", groups);
			EngineApplicationInterface.IHighlights.OpenSummary(groups2);
		}

		// Token: 0x06000866 RID: 2150 RVA: 0x0000698C File Offset: 0x00004B8C
		public static void AddHighlight(string id, string name)
		{
			EngineApplicationInterface.IHighlights.AddHighlight(id, name);
		}

		// Token: 0x06000867 RID: 2151 RVA: 0x0000699A File Offset: 0x00004B9A
		public static void RemoveHighlight(string id)
		{
			EngineApplicationInterface.IHighlights.RemoveHighlight(id);
		}

		// Token: 0x020000BF RID: 191
		public enum Significance
		{
			// Token: 0x040003A7 RID: 935
			None,
			// Token: 0x040003A8 RID: 936
			ExtremelyBad,
			// Token: 0x040003A9 RID: 937
			VeryBad,
			// Token: 0x040003AA RID: 938
			Bad = 4,
			// Token: 0x040003AB RID: 939
			Neutral = 16,
			// Token: 0x040003AC RID: 940
			Good = 256,
			// Token: 0x040003AD RID: 941
			VeryGood = 512,
			// Token: 0x040003AE RID: 942
			ExtremelyGoods = 1024,
			// Token: 0x040003AF RID: 943
			Max = 2048
		}

		// Token: 0x020000C0 RID: 192
		public enum Type
		{
			// Token: 0x040003B1 RID: 945
			None,
			// Token: 0x040003B2 RID: 946
			Milestone,
			// Token: 0x040003B3 RID: 947
			Achievement,
			// Token: 0x040003B4 RID: 948
			Incident = 4,
			// Token: 0x040003B5 RID: 949
			StateChange = 8,
			// Token: 0x040003B6 RID: 950
			Unannounced = 16,
			// Token: 0x040003B7 RID: 951
			Max = 32
		}
	}
}

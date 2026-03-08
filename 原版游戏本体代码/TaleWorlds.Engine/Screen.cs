using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000085 RID: 133
	public static class Screen
	{
		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000C04 RID: 3076 RVA: 0x0000D2DF File Offset: 0x0000B4DF
		// (set) Token: 0x06000C05 RID: 3077 RVA: 0x0000D2E6 File Offset: 0x0000B4E6
		public static float RealScreenResolutionWidth { get; private set; }

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000C06 RID: 3078 RVA: 0x0000D2EE File Offset: 0x0000B4EE
		// (set) Token: 0x06000C07 RID: 3079 RVA: 0x0000D2F5 File Offset: 0x0000B4F5
		public static float RealScreenResolutionHeight { get; private set; }

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000C08 RID: 3080 RVA: 0x0000D2FD File Offset: 0x0000B4FD
		public static Vec2 RealScreenResolution
		{
			get
			{
				return new Vec2(Screen.RealScreenResolutionWidth, Screen.RealScreenResolutionHeight);
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000C09 RID: 3081 RVA: 0x0000D30E File Offset: 0x0000B50E
		// (set) Token: 0x06000C0A RID: 3082 RVA: 0x0000D315 File Offset: 0x0000B515
		public static float AspectRatio { get; private set; }

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x06000C0B RID: 3083 RVA: 0x0000D31D File Offset: 0x0000B51D
		// (set) Token: 0x06000C0C RID: 3084 RVA: 0x0000D324 File Offset: 0x0000B524
		public static Vec2 DesktopResolution { get; private set; }

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x06000C0D RID: 3085 RVA: 0x0000D32C File Offset: 0x0000B52C
		// (set) Token: 0x06000C0E RID: 3086 RVA: 0x0000D333 File Offset: 0x0000B533
		public static Vec2 ScreenScale { get; private set; }

		// Token: 0x06000C0F RID: 3087 RVA: 0x0000D33C File Offset: 0x0000B53C
		internal static void Update()
		{
			Screen.RealScreenResolutionWidth = EngineApplicationInterface.IScreen.GetRealScreenResolutionWidth();
			Screen.RealScreenResolutionHeight = EngineApplicationInterface.IScreen.GetRealScreenResolutionHeight();
			Screen.AspectRatio = EngineApplicationInterface.IScreen.GetAspectRatio();
			Screen.DesktopResolution = new Vec2(EngineApplicationInterface.IScreen.GetDesktopWidth(), EngineApplicationInterface.IScreen.GetDesktopHeight());
			Screen.ScreenScale = new Vec2(Screen.RealScreenResolutionWidth / Screen.DesktopResolution.x, Screen.RealScreenResolutionHeight / Screen.DesktopResolution.y);
		}

		// Token: 0x06000C10 RID: 3088 RVA: 0x0000D3BE File Offset: 0x0000B5BE
		public static bool GetMouseVisible()
		{
			return EngineApplicationInterface.IScreen.GetMouseVisible();
		}
	}
}

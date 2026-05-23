using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL
{
	// Token: 0x0200003B RID: 59
	public class OpenGlLoadException : Exception
	{
		// Token: 0x06000173 RID: 371 RVA: 0x000055FB File Offset: 0x000037FB
		public OpenGlLoadException()
		{
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00005603 File Offset: 0x00003803
		public OpenGlLoadException(string message)
			: base(message)
		{
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000560C File Offset: 0x0000380C
		public OpenGlLoadException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}

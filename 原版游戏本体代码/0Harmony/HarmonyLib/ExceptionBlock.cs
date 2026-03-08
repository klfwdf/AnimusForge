using System;

namespace HarmonyLib
{
	/// <summary>An exception block</summary>
	// Token: 0x0200007B RID: 123
	public class ExceptionBlock
	{
		/// <summary>Creates a new ExceptionBlock</summary>
		/// <param name="blockType">The <see cref="T:HarmonyLib.ExceptionBlockType" /></param>
		/// <param name="catchType">The catch type</param>
		// Token: 0x06000236 RID: 566 RVA: 0x0000E225 File Offset: 0x0000C425
		public ExceptionBlock(ExceptionBlockType blockType, Type catchType = null)
		{
			this.blockType = blockType;
			this.catchType = catchType ?? typeof(object);
		}

		/// <summary>Block type</summary>
		// Token: 0x0400018F RID: 399
		public ExceptionBlockType blockType;

		/// <summary>Catch type</summary>
		// Token: 0x04000190 RID: 400
		public Type catchType;
	}
}

using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D2 RID: 722
	internal sealed class UserStringHeapBuffer : StringHeapBuffer
	{
		// Token: 0x060012C3 RID: 4803 RVA: 0x0003B260 File Offset: 0x00039460
		public override uint GetStringIndex(string @string)
		{
			uint index;
			if (this.strings.TryGetValue(@string, out index))
			{
				return index;
			}
			index = (uint)this.position;
			this.WriteString(@string);
			this.strings.Add(@string, index);
			return index;
		}

		// Token: 0x060012C4 RID: 4804 RVA: 0x0003B29C File Offset: 0x0003949C
		protected override void WriteString(string @string)
		{
			base.WriteCompressedUInt32((uint)(@string.Length * 2 + 1));
			byte special = 0;
			foreach (char @char in @string)
			{
				base.WriteUInt16((ushort)@char);
				if (special != 1 && (@char < ' ' || @char > '~') && (@char > '~' || (@char >= '\u0001' && @char <= '\b') || (@char >= '\u000e' && @char <= '\u001f') || @char == '\'' || @char == '-'))
				{
					special = 1;
				}
			}
			base.WriteByte(special);
		}
	}
}

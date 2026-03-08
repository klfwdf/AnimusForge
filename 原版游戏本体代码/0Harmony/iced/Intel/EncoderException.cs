using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Iced.Intel
{
	// Token: 0x02000642 RID: 1602
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	internal class EncoderException : Exception
	{
		// Token: 0x17000779 RID: 1913
		// (get) Token: 0x06002177 RID: 8567 RVA: 0x0006D0BB File Offset: 0x0006B2BB
		public Instruction Instruction { get; }

		// Token: 0x06002178 RID: 8568 RVA: 0x0006D0C3 File Offset: 0x0006B2C3
		public EncoderException(string message, in Instruction instruction)
			: base(message)
		{
			this.Instruction = instruction;
		}

		// Token: 0x06002179 RID: 8569 RVA: 0x0002DA2D File Offset: 0x0002BC2D
		protected EncoderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}

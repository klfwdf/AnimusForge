using System;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Rocks
{
	// Token: 0x0200044F RID: 1103
	internal interface IILVisitor
	{
		// Token: 0x060017F4 RID: 6132
		void OnInlineNone(OpCode opcode);

		// Token: 0x060017F5 RID: 6133
		void OnInlineSByte(OpCode opcode, sbyte value);

		// Token: 0x060017F6 RID: 6134
		void OnInlineByte(OpCode opcode, byte value);

		// Token: 0x060017F7 RID: 6135
		void OnInlineInt32(OpCode opcode, int value);

		// Token: 0x060017F8 RID: 6136
		void OnInlineInt64(OpCode opcode, long value);

		// Token: 0x060017F9 RID: 6137
		void OnInlineSingle(OpCode opcode, float value);

		// Token: 0x060017FA RID: 6138
		void OnInlineDouble(OpCode opcode, double value);

		// Token: 0x060017FB RID: 6139
		void OnInlineString(OpCode opcode, string value);

		// Token: 0x060017FC RID: 6140
		void OnInlineBranch(OpCode opcode, int offset);

		// Token: 0x060017FD RID: 6141
		void OnInlineSwitch(OpCode opcode, int[] offsets);

		// Token: 0x060017FE RID: 6142
		void OnInlineVariable(OpCode opcode, VariableDefinition variable);

		// Token: 0x060017FF RID: 6143
		void OnInlineArgument(OpCode opcode, ParameterDefinition parameter);

		// Token: 0x06001800 RID: 6144
		void OnInlineSignature(OpCode opcode, CallSite callSite);

		// Token: 0x06001801 RID: 6145
		void OnInlineType(OpCode opcode, TypeReference type);

		// Token: 0x06001802 RID: 6146
		void OnInlineField(OpCode opcode, FieldReference field);

		// Token: 0x06001803 RID: 6147
		void OnInlineMethod(OpCode opcode, MethodReference method);
	}
}

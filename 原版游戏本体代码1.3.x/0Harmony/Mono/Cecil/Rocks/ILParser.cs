using System;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil.Rocks
{
	// Token: 0x02000450 RID: 1104
	internal static class ILParser
	{
		// Token: 0x06001804 RID: 6148 RVA: 0x0004B7F8 File Offset: 0x000499F8
		public static void Parse(MethodDefinition method, IILVisitor visitor)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			if (visitor == null)
			{
				throw new ArgumentNullException("visitor");
			}
			if (!method.HasBody || !method.HasImage)
			{
				throw new ArgumentException();
			}
			method.Module.Read<MethodDefinition, bool>(method, delegate(MethodDefinition m, MetadataReader _)
			{
				ILParser.ParseMethod(m, visitor);
				return true;
			});
		}

		// Token: 0x06001805 RID: 6149 RVA: 0x0004B864 File Offset: 0x00049A64
		private static void ParseMethod(MethodDefinition method, IILVisitor visitor)
		{
			ILParser.ParseContext context = ILParser.CreateContext(method, visitor);
			CodeReader code = context.Code;
			byte flags = code.ReadByte();
			int num = (int)(flags & 3);
			if (num != 2)
			{
				if (num != 3)
				{
					throw new NotSupportedException();
				}
				code.Advance(-1);
				ILParser.ParseFatMethod(context);
			}
			else
			{
				ILParser.ParseCode(flags >> 2, context);
			}
			code.MoveBackTo(context.Position);
		}

		// Token: 0x06001806 RID: 6150 RVA: 0x0004B8C4 File Offset: 0x00049AC4
		private static ILParser.ParseContext CreateContext(MethodDefinition method, IILVisitor visitor)
		{
			CodeReader code = method.Module.Read<MethodDefinition, CodeReader>(method, (MethodDefinition _, MetadataReader reader) => reader.code);
			int position = code.MoveTo(method);
			return new ILParser.ParseContext
			{
				Code = code,
				Position = position,
				Metadata = code.reader,
				Visitor = visitor
			};
		}

		// Token: 0x06001807 RID: 6151 RVA: 0x0004B92C File Offset: 0x00049B2C
		private static void ParseFatMethod(ILParser.ParseContext context)
		{
			CodeReader code = context.Code;
			code.Advance(4);
			int code_size = code.ReadInt32();
			MetadataToken local_var_token = code.ReadToken();
			if (local_var_token != MetadataToken.Zero)
			{
				context.Variables = code.ReadVariables(local_var_token);
			}
			ILParser.ParseCode(code_size, context);
		}

		// Token: 0x06001808 RID: 6152 RVA: 0x0004B974 File Offset: 0x00049B74
		private static void ParseCode(int code_size, ILParser.ParseContext context)
		{
			CodeReader code = context.Code;
			MetadataReader metadata = context.Metadata;
			IILVisitor visitor = context.Visitor;
			int end = code.Position + code_size;
			while (code.Position < end)
			{
				byte il_opcode = code.ReadByte();
				OpCode opcode = ((il_opcode != 254) ? OpCodes.OneByteOpCode[(int)il_opcode] : OpCodes.TwoBytesOpCode[(int)code.ReadByte()]);
				switch (opcode.OperandType)
				{
				case OperandType.InlineBrTarget:
					visitor.OnInlineBranch(opcode, code.ReadInt32());
					break;
				case OperandType.InlineField:
				case OperandType.InlineMethod:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				{
					IMetadataTokenProvider member = metadata.LookupToken(code.ReadToken());
					TokenType tokenType = member.MetadataToken.TokenType;
					if (tokenType > TokenType.Field)
					{
						if (tokenType <= TokenType.MemberRef)
						{
							if (tokenType != TokenType.Method)
							{
								if (tokenType != TokenType.MemberRef)
								{
									break;
								}
								FieldReference field_ref = member as FieldReference;
								if (field_ref != null)
								{
									visitor.OnInlineField(opcode, field_ref);
									break;
								}
								MethodReference method_ref = member as MethodReference;
								if (method_ref != null)
								{
									visitor.OnInlineMethod(opcode, method_ref);
									break;
								}
								throw new InvalidOperationException();
							}
						}
						else
						{
							if (tokenType == TokenType.TypeSpec)
							{
								goto IL_2B8;
							}
							if (tokenType != TokenType.MethodSpec)
							{
								break;
							}
						}
						visitor.OnInlineMethod(opcode, (MethodReference)member);
						break;
					}
					if (tokenType != TokenType.TypeRef && tokenType != TokenType.TypeDef)
					{
						if (tokenType != TokenType.Field)
						{
							break;
						}
						visitor.OnInlineField(opcode, (FieldReference)member);
						break;
					}
					IL_2B8:
					visitor.OnInlineType(opcode, (TypeReference)member);
					break;
				}
				case OperandType.InlineI:
					visitor.OnInlineInt32(opcode, code.ReadInt32());
					break;
				case OperandType.InlineI8:
					visitor.OnInlineInt64(opcode, code.ReadInt64());
					break;
				case OperandType.InlineNone:
					visitor.OnInlineNone(opcode);
					break;
				case OperandType.InlineR:
					visitor.OnInlineDouble(opcode, code.ReadDouble());
					break;
				case OperandType.InlineSig:
					visitor.OnInlineSignature(opcode, code.GetCallSite(code.ReadToken()));
					break;
				case OperandType.InlineString:
					visitor.OnInlineString(opcode, code.GetString(code.ReadToken()));
					break;
				case OperandType.InlineSwitch:
				{
					int length = code.ReadInt32();
					int[] branches = new int[length];
					for (int i = 0; i < length; i++)
					{
						branches[i] = code.ReadInt32();
					}
					visitor.OnInlineSwitch(opcode, branches);
					break;
				}
				case OperandType.InlineVar:
					visitor.OnInlineVariable(opcode, ILParser.GetVariable(context, (int)code.ReadInt16()));
					break;
				case OperandType.InlineArg:
					visitor.OnInlineArgument(opcode, code.GetParameter((int)code.ReadInt16()));
					break;
				case OperandType.ShortInlineBrTarget:
					visitor.OnInlineBranch(opcode, (int)code.ReadSByte());
					break;
				case OperandType.ShortInlineI:
					if (opcode == OpCodes.Ldc_I4_S)
					{
						visitor.OnInlineSByte(opcode, code.ReadSByte());
					}
					else
					{
						visitor.OnInlineByte(opcode, code.ReadByte());
					}
					break;
				case OperandType.ShortInlineR:
					visitor.OnInlineSingle(opcode, code.ReadSingle());
					break;
				case OperandType.ShortInlineVar:
					visitor.OnInlineVariable(opcode, ILParser.GetVariable(context, (int)code.ReadByte()));
					break;
				case OperandType.ShortInlineArg:
					visitor.OnInlineArgument(opcode, code.GetParameter((int)code.ReadByte()));
					break;
				}
			}
		}

		// Token: 0x06001809 RID: 6153 RVA: 0x0004BCB0 File Offset: 0x00049EB0
		private static VariableDefinition GetVariable(ILParser.ParseContext context, int index)
		{
			return context.Variables[index];
		}

		// Token: 0x02000451 RID: 1105
		private class ParseContext
		{
			// Token: 0x1700059D RID: 1437
			// (get) Token: 0x0600180A RID: 6154 RVA: 0x0004BCBE File Offset: 0x00049EBE
			// (set) Token: 0x0600180B RID: 6155 RVA: 0x0004BCC6 File Offset: 0x00049EC6
			public CodeReader Code { get; set; }

			// Token: 0x1700059E RID: 1438
			// (get) Token: 0x0600180C RID: 6156 RVA: 0x0004BCCF File Offset: 0x00049ECF
			// (set) Token: 0x0600180D RID: 6157 RVA: 0x0004BCD7 File Offset: 0x00049ED7
			public int Position { get; set; }

			// Token: 0x1700059F RID: 1439
			// (get) Token: 0x0600180E RID: 6158 RVA: 0x0004BCE0 File Offset: 0x00049EE0
			// (set) Token: 0x0600180F RID: 6159 RVA: 0x0004BCE8 File Offset: 0x00049EE8
			public MetadataReader Metadata { get; set; }

			// Token: 0x170005A0 RID: 1440
			// (get) Token: 0x06001810 RID: 6160 RVA: 0x0004BCF1 File Offset: 0x00049EF1
			// (set) Token: 0x06001811 RID: 6161 RVA: 0x0004BCF9 File Offset: 0x00049EF9
			public Collection<VariableDefinition> Variables { get; set; }

			// Token: 0x170005A1 RID: 1441
			// (get) Token: 0x06001812 RID: 6162 RVA: 0x0004BD02 File Offset: 0x00049F02
			// (set) Token: 0x06001813 RID: 6163 RVA: 0x0004BD0A File Offset: 0x00049F0A
			public IILVisitor Visitor { get; set; }
		}
	}
}

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct InstructionEncoder
{
	public BlobBuilder CodeBuilder { get; }

	public ControlFlowBuilder? ControlFlowBuilder { get; }

	public int Offset => CodeBuilder.Count;

	public InstructionEncoder(BlobBuilder codeBuilder, ControlFlowBuilder? controlFlowBuilder = null)
	{
		if (codeBuilder == null)
		{
			System.Reflection.Throw.BuilderArgumentNull();
		}
		CodeBuilder = codeBuilder;
		ControlFlowBuilder = controlFlowBuilder;
	}

	public void OpCode(ILOpCode code)
	{
		ControlFlowBuilder?.ValidateNotInSwitch();
		if ((uint)(byte)code == (uint)code)
		{
			CodeBuilder.WriteByte((byte)code);
		}
		else
		{
			CodeBuilder.WriteUInt16BE((ushort)code);
		}
	}

	public void Token(EntityHandle handle)
	{
		Token(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle));
	}

	public void Token(int token)
	{
		ControlFlowBuilder?.ValidateNotInSwitch();
		CodeBuilder.WriteInt32(token);
	}

	public void LoadString(UserStringHandle handle)
	{
		OpCode(ILOpCode.Ldstr);
		Token(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle));
	}

	public void Call(EntityHandle methodHandle)
	{
		if (methodHandle.Kind != System.Reflection.Metadata.HandleKind.MethodDefinition && methodHandle.Kind != System.Reflection.Metadata.HandleKind.MethodSpecification && methodHandle.Kind != System.Reflection.Metadata.HandleKind.MemberReference)
		{
			System.Reflection.Throw.InvalidArgument_Handle("methodHandle");
		}
		OpCode(ILOpCode.Call);
		Token(methodHandle);
	}

	public void Call(System.Reflection.Metadata.MethodDefinitionHandle methodHandle)
	{
		OpCode(ILOpCode.Call);
		Token(methodHandle);
	}

	public void Call(MethodSpecificationHandle methodHandle)
	{
		OpCode(ILOpCode.Call);
		Token(methodHandle);
	}

	public void Call(MemberReferenceHandle methodHandle)
	{
		OpCode(ILOpCode.Call);
		Token(methodHandle);
	}

	public void CallIndirect(StandaloneSignatureHandle signature)
	{
		OpCode(ILOpCode.Calli);
		Token(signature);
	}

	public void LoadConstantI4(int value)
	{
		ILOpCode code;
		switch (value)
		{
		case -1:
			code = ILOpCode.Ldc_i4_m1;
			break;
		case 0:
			code = ILOpCode.Ldc_i4_0;
			break;
		case 1:
			code = ILOpCode.Ldc_i4_1;
			break;
		case 2:
			code = ILOpCode.Ldc_i4_2;
			break;
		case 3:
			code = ILOpCode.Ldc_i4_3;
			break;
		case 4:
			code = ILOpCode.Ldc_i4_4;
			break;
		case 5:
			code = ILOpCode.Ldc_i4_5;
			break;
		case 6:
			code = ILOpCode.Ldc_i4_6;
			break;
		case 7:
			code = ILOpCode.Ldc_i4_7;
			break;
		case 8:
			code = ILOpCode.Ldc_i4_8;
			break;
		default:
			if ((sbyte)value == value)
			{
				OpCode(ILOpCode.Ldc_i4_s);
				CodeBuilder.WriteSByte((sbyte)value);
			}
			else
			{
				OpCode(ILOpCode.Ldc_i4);
				CodeBuilder.WriteInt32(value);
			}
			return;
		}
		OpCode(code);
	}

	public void LoadConstantI8(long value)
	{
		OpCode(ILOpCode.Ldc_i8);
		CodeBuilder.WriteInt64(value);
	}

	public void LoadConstantR4(float value)
	{
		OpCode(ILOpCode.Ldc_r4);
		CodeBuilder.WriteSingle(value);
	}

	public void LoadConstantR8(double value)
	{
		OpCode(ILOpCode.Ldc_r8);
		CodeBuilder.WriteDouble(value);
	}

	public void LoadLocal(int slotIndex)
	{
		switch (slotIndex)
		{
		case 0:
			OpCode(ILOpCode.Ldloc_0);
			return;
		case 1:
			OpCode(ILOpCode.Ldloc_1);
			return;
		case 2:
			OpCode(ILOpCode.Ldloc_2);
			return;
		case 3:
			OpCode(ILOpCode.Ldloc_3);
			return;
		}
		if ((uint)slotIndex <= 255u)
		{
			OpCode(ILOpCode.Ldloc_s);
			CodeBuilder.WriteByte((byte)slotIndex);
		}
		else if (slotIndex > 0)
		{
			OpCode(ILOpCode.Ldloc);
			CodeBuilder.WriteInt32(slotIndex);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("slotIndex");
		}
	}

	public void StoreLocal(int slotIndex)
	{
		switch (slotIndex)
		{
		case 0:
			OpCode(ILOpCode.Stloc_0);
			return;
		case 1:
			OpCode(ILOpCode.Stloc_1);
			return;
		case 2:
			OpCode(ILOpCode.Stloc_2);
			return;
		case 3:
			OpCode(ILOpCode.Stloc_3);
			return;
		}
		if ((uint)slotIndex <= 255u)
		{
			OpCode(ILOpCode.Stloc_s);
			CodeBuilder.WriteByte((byte)slotIndex);
		}
		else if (slotIndex > 0)
		{
			OpCode(ILOpCode.Stloc);
			CodeBuilder.WriteInt32(slotIndex);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("slotIndex");
		}
	}

	public void LoadLocalAddress(int slotIndex)
	{
		if ((uint)slotIndex <= 255u)
		{
			OpCode(ILOpCode.Ldloca_s);
			CodeBuilder.WriteByte((byte)slotIndex);
		}
		else if (slotIndex > 0)
		{
			OpCode(ILOpCode.Ldloca);
			CodeBuilder.WriteInt32(slotIndex);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("slotIndex");
		}
	}

	public void LoadArgument(int argumentIndex)
	{
		switch (argumentIndex)
		{
		case 0:
			OpCode(ILOpCode.Ldarg_0);
			return;
		case 1:
			OpCode(ILOpCode.Ldarg_1);
			return;
		case 2:
			OpCode(ILOpCode.Ldarg_2);
			return;
		case 3:
			OpCode(ILOpCode.Ldarg_3);
			return;
		}
		if ((uint)argumentIndex <= 255u)
		{
			OpCode(ILOpCode.Ldarg_s);
			CodeBuilder.WriteByte((byte)argumentIndex);
		}
		else if (argumentIndex > 0)
		{
			OpCode(ILOpCode.Ldarg);
			CodeBuilder.WriteInt32(argumentIndex);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("argumentIndex");
		}
	}

	public void LoadArgumentAddress(int argumentIndex)
	{
		if ((uint)argumentIndex <= 255u)
		{
			OpCode(ILOpCode.Ldarga_s);
			CodeBuilder.WriteByte((byte)argumentIndex);
		}
		else if (argumentIndex > 0)
		{
			OpCode(ILOpCode.Ldarga);
			CodeBuilder.WriteInt32(argumentIndex);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("argumentIndex");
		}
	}

	public void StoreArgument(int argumentIndex)
	{
		if ((uint)argumentIndex <= 255u)
		{
			OpCode(ILOpCode.Starg_s);
			CodeBuilder.WriteByte((byte)argumentIndex);
		}
		else if (argumentIndex > 0)
		{
			OpCode(ILOpCode.Starg);
			CodeBuilder.WriteInt32(argumentIndex);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("argumentIndex");
		}
	}

	public LabelHandle DefineLabel()
	{
		return GetBranchBuilder().AddLabel();
	}

	internal void LabelOperand(ILOpCode code, LabelHandle label, int instructionEndDisplacement, int ilOffset)
	{
		GetBranchBuilder().AddBranch(Offset, label, instructionEndDisplacement, ilOffset, code);
		if (instructionEndDisplacement == 1)
		{
			CodeBuilder.WriteSByte(-1);
		}
		else
		{
			CodeBuilder.WriteInt32(-1);
		}
	}

	public void Branch(ILOpCode code, LabelHandle label)
	{
		int branchOperandSize = code.GetBranchOperandSize();
		int offset = Offset;
		OpCode(code);
		LabelOperand(code, label, branchOperandSize, offset);
	}

	public SwitchInstructionEncoder Switch(int branchCount)
	{
		if (branchCount <= 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("branchCount");
		}
		ControlFlowBuilder branchBuilder = GetBranchBuilder();
		int offset = Offset;
		OpCode(ILOpCode.Switch);
		branchBuilder.RemainingSwitchBranches = branchCount;
		CodeBuilder.WriteUInt32((uint)branchCount);
		int instructionEnd = Offset + 4 * branchCount;
		return new SwitchInstructionEncoder(this, offset, instructionEnd);
	}

	public void MarkLabel(LabelHandle label)
	{
		GetBranchBuilder().MarkLabel(Offset, label);
	}

	private ControlFlowBuilder GetBranchBuilder()
	{
		if (ControlFlowBuilder == null)
		{
			System.Reflection.Throw.ControlFlowBuilderNotAvailable();
		}
		return ControlFlowBuilder;
	}
}

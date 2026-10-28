namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodBodyStreamEncoder
{
	public readonly struct MethodBody
	{
		public int Offset { get; }

		public Blob Instructions { get; }

		public ExceptionRegionEncoder ExceptionRegions { get; }

		internal MethodBody(int bodyOffset, Blob instructions, ExceptionRegionEncoder exceptionRegions)
		{
			Offset = bodyOffset;
			Instructions = instructions;
			ExceptionRegions = exceptionRegions;
		}
	}

	public BlobBuilder Builder { get; }

	public MethodBodyStreamEncoder(BlobBuilder builder)
	{
		if (builder == null)
		{
			System.Reflection.Throw.BuilderArgumentNull();
		}
		if (builder.Count % 4 != 0)
		{
			throw new ArgumentException(System.SR.BuilderMustAligned, "builder");
		}
		Builder = builder;
	}

	public MethodBody AddMethodBody(int codeSize, int maxStack, int exceptionRegionCount, bool hasSmallExceptionRegions, StandaloneSignatureHandle localVariablesSignature, MethodBodyAttributes attributes)
	{
		return AddMethodBody(codeSize, maxStack, exceptionRegionCount, hasSmallExceptionRegions, localVariablesSignature, attributes, false);
	}

	public MethodBody AddMethodBody(int codeSize, int maxStack = 8, int exceptionRegionCount = 0, bool hasSmallExceptionRegions = true, StandaloneSignatureHandle localVariablesSignature = default(StandaloneSignatureHandle), MethodBodyAttributes attributes = MethodBodyAttributes.InitLocals, bool hasDynamicStackAllocation = false)
	{
		if (codeSize < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("codeSize");
		}
		if ((uint)maxStack > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("maxStack");
		}
		if (!ExceptionRegionEncoder.IsExceptionRegionCountInBounds(exceptionRegionCount))
		{
			System.Reflection.Throw.ArgumentOutOfRange("exceptionRegionCount");
		}
		int bodyOffset = SerializeHeader(codeSize, (ushort)maxStack, exceptionRegionCount, attributes, localVariablesSignature, hasDynamicStackAllocation);
		Blob instructions = Builder.ReserveBytes(codeSize);
		ExceptionRegionEncoder exceptionRegions = ((exceptionRegionCount > 0) ? ExceptionRegionEncoder.SerializeTableHeader(Builder, exceptionRegionCount, hasSmallExceptionRegions) : default(ExceptionRegionEncoder));
		return new MethodBody(bodyOffset, instructions, exceptionRegions);
	}

	public int AddMethodBody(InstructionEncoder instructionEncoder, int maxStack, StandaloneSignatureHandle localVariablesSignature, MethodBodyAttributes attributes)
	{
		return AddMethodBody(instructionEncoder, maxStack, localVariablesSignature, attributes, false);
	}

	public int AddMethodBody(InstructionEncoder instructionEncoder, int maxStack = 8, StandaloneSignatureHandle localVariablesSignature = default(StandaloneSignatureHandle), MethodBodyAttributes attributes = MethodBodyAttributes.InitLocals, bool hasDynamicStackAllocation = false)
	{
		if ((uint)maxStack > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("maxStack");
		}
		BlobBuilder codeBuilder = instructionEncoder.CodeBuilder;
		ControlFlowBuilder controlFlowBuilder = instructionEncoder.ControlFlowBuilder;
		if (codeBuilder == null)
		{
			System.Reflection.Throw.ArgumentNull("instructionEncoder");
		}
		int exceptionRegionCount = controlFlowBuilder?.ExceptionHandlerCount ?? 0;
		if (!ExceptionRegionEncoder.IsExceptionRegionCountInBounds(exceptionRegionCount))
		{
			System.Reflection.Throw.ArgumentOutOfRange("instructionEncoder", System.SR.TooManyExceptionRegions);
		}
		controlFlowBuilder?.ValidateNotInSwitch();
		int result = SerializeHeader(codeBuilder.Count, (ushort)maxStack, exceptionRegionCount, attributes, localVariablesSignature, hasDynamicStackAllocation);
		if (controlFlowBuilder != null && controlFlowBuilder.BranchCount > 0)
		{
			controlFlowBuilder.CopyCodeAndFixupBranches(codeBuilder, Builder);
		}
		else
		{
			codeBuilder.WriteContentTo(Builder);
		}
		controlFlowBuilder?.SerializeExceptionTable(Builder);
		return result;
	}

	private int SerializeHeader(int codeSize, ushort maxStack, int exceptionRegionCount, MethodBodyAttributes attributes, StandaloneSignatureHandle localVariablesSignature, bool hasDynamicStackAllocation)
	{
		bool flag = (attributes & MethodBodyAttributes.InitLocals) != 0;
		int count;
		if (codeSize < 64 && maxStack <= 8 && localVariablesSignature.IsNil && (!hasDynamicStackAllocation || !flag) && exceptionRegionCount == 0)
		{
			count = Builder.Count;
			Builder.WriteByte((byte)((codeSize << 2) | 2));
		}
		else
		{
			Builder.Align(4);
			count = Builder.Count;
			ushort num = 12291;
			if (exceptionRegionCount > 0)
			{
				num |= 8;
			}
			if (flag)
			{
				num |= 0x10;
			}
			Builder.WriteUInt16((ushort)((uint)attributes | (uint)num));
			Builder.WriteUInt16(maxStack);
			Builder.WriteInt32(codeSize);
			Builder.WriteInt32((!localVariablesSignature.IsNil) ? System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(localVariablesSignature) : 0);
		}
		return count;
	}
}

using System.Collections.Generic;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class ControlFlowBuilder
{
	internal readonly struct BranchInfo
	{
		internal readonly int OperandOffset;

		internal readonly LabelHandle Label;

		private readonly int _instructionEndDisplacement;

		internal readonly int ILOffset;

		internal readonly ILOpCode OpCode;

		internal bool IsShortBranch => _instructionEndDisplacement == 1;

		internal int OperandSize => Math.Min(_instructionEndDisplacement, 4);

		internal BranchInfo(int operandOffset, LabelHandle label, int instructionEndDisplacement, int ilOffset, ILOpCode opCode)
		{
			OperandOffset = operandOffset;
			Label = label;
			_instructionEndDisplacement = instructionEndDisplacement;
			ILOffset = ilOffset;
			OpCode = opCode;
		}

		internal int GetBranchDistance(List<int> labels)
		{
			int num = labels[Label.Id - 1];
			if (num < 0)
			{
				System.Reflection.Throw.InvalidOperation_LabelNotMarked(Label.Id);
			}
			int num2 = num - (OperandOffset + _instructionEndDisplacement);
			if (IsShortBranch && (sbyte)num2 != num2)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.DistanceBetweenInstructionAndLabelTooBig, OpCode, ILOffset, num2));
			}
			return num2;
		}
	}

	internal readonly struct ExceptionHandlerInfo(ExceptionRegionKind kind, LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart, EntityHandle catchType)
	{
		public readonly ExceptionRegionKind Kind = kind;

		public readonly LabelHandle TryStart = tryStart;

		public readonly LabelHandle TryEnd = tryEnd;

		public readonly LabelHandle HandlerStart = handlerStart;

		public readonly LabelHandle HandlerEnd = handlerEnd;

		public readonly LabelHandle FilterStart = filterStart;

		public readonly EntityHandle CatchType = catchType;
	}

	private readonly List<BranchInfo> _branches;

	private readonly List<int> _labels;

	private List<ExceptionHandlerInfo> _lazyExceptionHandlers;

	internal IEnumerable<BranchInfo> Branches => _branches;

	internal IEnumerable<int> Labels => _labels;

	internal int BranchCount => _branches.Count;

	internal int ExceptionHandlerCount => _lazyExceptionHandlers?.Count ?? 0;

	internal int RemainingSwitchBranches { get; set; }

	public ControlFlowBuilder()
	{
		_branches = new List<BranchInfo>();
		_labels = new List<int>();
	}

	public void Clear()
	{
		_branches.Clear();
		_labels.Clear();
		_lazyExceptionHandlers?.Clear();
		RemainingSwitchBranches = 0;
	}

	internal LabelHandle AddLabel()
	{
		ValidateNotInSwitch();
		_labels.Add(-1);
		return new LabelHandle(_labels.Count);
	}

	internal void AddBranch(int operandOffset, LabelHandle label, int instructionEndDisplacement, int ilOffset, ILOpCode opCode)
	{
		ValidateLabel(label, "label");
		_branches.Add(new BranchInfo(operandOffset, label, instructionEndDisplacement, ilOffset, opCode));
	}

	internal void MarkLabel(int ilOffset, LabelHandle label)
	{
		ValidateNotInSwitch();
		ValidateLabel(label, "label");
		_labels[label.Id - 1] = ilOffset;
	}

	private int GetLabelOffsetChecked(LabelHandle label)
	{
		int num = _labels[label.Id - 1];
		if (num < 0)
		{
			System.Reflection.Throw.InvalidOperation_LabelNotMarked(label.Id);
		}
		return num;
	}

	private void ValidateLabel(LabelHandle label, string parameterName)
	{
		if (label.IsNil)
		{
			System.Reflection.Throw.ArgumentNull(parameterName);
		}
		if (label.Id > _labels.Count)
		{
			System.Reflection.Throw.LabelDoesntBelongToBuilder(parameterName);
		}
	}

	public void AddFinallyRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd)
	{
		AddExceptionRegion(ExceptionRegionKind.Finally, tryStart, tryEnd, handlerStart, handlerEnd);
	}

	public void AddFaultRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd)
	{
		AddExceptionRegion(ExceptionRegionKind.Fault, tryStart, tryEnd, handlerStart, handlerEnd);
	}

	public void AddCatchRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, EntityHandle catchType)
	{
		if (!ExceptionRegionEncoder.IsValidCatchTypeHandle(catchType))
		{
			System.Reflection.Throw.InvalidArgument_Handle("catchType");
		}
		AddExceptionRegion(ExceptionRegionKind.Catch, tryStart, tryEnd, handlerStart, handlerEnd, default(LabelHandle), catchType);
	}

	public void AddFilterRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart)
	{
		ValidateLabel(filterStart, "filterStart");
		AddExceptionRegion(ExceptionRegionKind.Filter, tryStart, tryEnd, handlerStart, handlerEnd, filterStart);
	}

	private void AddExceptionRegion(ExceptionRegionKind kind, LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart = default(LabelHandle), EntityHandle catchType = default(EntityHandle))
	{
		ValidateLabel(tryStart, "tryStart");
		ValidateLabel(tryEnd, "tryEnd");
		ValidateLabel(handlerStart, "handlerStart");
		ValidateLabel(handlerEnd, "handlerEnd");
		ValidateNotInSwitch();
		if (_lazyExceptionHandlers == null)
		{
			_lazyExceptionHandlers = new List<ExceptionHandlerInfo>();
		}
		_lazyExceptionHandlers.Add(new ExceptionHandlerInfo(kind, tryStart, tryEnd, handlerStart, handlerEnd, filterStart, catchType));
	}

	internal void ValidateNotInSwitch()
	{
		if (RemainingSwitchBranches > 0)
		{
			System.Reflection.Throw.InvalidOperation(System.SR.SwitchInstructionEncoderTooFewBranches);
		}
	}

	internal void SwitchBranchAdded()
	{
		if (RemainingSwitchBranches == 0)
		{
			System.Reflection.Throw.InvalidOperation(System.SR.SwitchInstructionEncoderTooManyBranches);
		}
		RemainingSwitchBranches--;
	}

	internal void CopyCodeAndFixupBranches(BlobBuilder srcBuilder, BlobBuilder dstBuilder)
	{
		BranchInfo branchInfo = _branches[0];
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Blob blob in srcBuilder.GetBlobs())
		{
			while (true)
			{
				int num4 = Math.Min(branchInfo.OperandOffset - num2, blob.Length - num3);
				dstBuilder.WriteBytes(blob.Buffer, num3, num4);
				num2 += num4;
				num3 += num4;
				if (num3 == blob.Length)
				{
					num3 = 0;
					break;
				}
				int operandSize = branchInfo.OperandSize;
				bool isShortBranch = branchInfo.IsShortBranch;
				int branchDistance = branchInfo.GetBranchDistance(_labels);
				if (isShortBranch)
				{
					dstBuilder.WriteSByte((sbyte)branchDistance);
				}
				else
				{
					dstBuilder.WriteInt32(branchDistance);
				}
				num2 += operandSize;
				num++;
				branchInfo = ((num != _branches.Count) ? _branches[num] : new BranchInfo(int.MaxValue, default(LabelHandle), 0, 0, ILOpCode.Nop));
				if (num3 == blob.Length - 1)
				{
					num3 = operandSize;
					break;
				}
				num3 += operandSize;
			}
		}
	}

	internal void SerializeExceptionTable(BlobBuilder builder)
	{
		if (_lazyExceptionHandlers == null || _lazyExceptionHandlers.Count == 0)
		{
			return;
		}
		ExceptionRegionEncoder exceptionRegionEncoder = ExceptionRegionEncoder.SerializeTableHeader(builder, _lazyExceptionHandlers.Count, HasSmallExceptionRegions());
		foreach (ExceptionHandlerInfo lazyExceptionHandler in _lazyExceptionHandlers)
		{
			int labelOffsetChecked = GetLabelOffsetChecked(lazyExceptionHandler.TryStart);
			int labelOffsetChecked2 = GetLabelOffsetChecked(lazyExceptionHandler.TryEnd);
			int labelOffsetChecked3 = GetLabelOffsetChecked(lazyExceptionHandler.HandlerStart);
			int labelOffsetChecked4 = GetLabelOffsetChecked(lazyExceptionHandler.HandlerEnd);
			if (labelOffsetChecked > labelOffsetChecked2)
			{
				System.Reflection.Throw.InvalidOperation(System.SR.Format(System.SR.InvalidExceptionRegionBounds, labelOffsetChecked, labelOffsetChecked2));
			}
			if (labelOffsetChecked3 > labelOffsetChecked4)
			{
				System.Reflection.Throw.InvalidOperation(System.SR.Format(System.SR.InvalidExceptionRegionBounds, labelOffsetChecked3, labelOffsetChecked4));
			}
			int catchTokenOrOffset = lazyExceptionHandler.Kind switch
			{
				ExceptionRegionKind.Catch => System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(lazyExceptionHandler.CatchType), 
				ExceptionRegionKind.Filter => GetLabelOffsetChecked(lazyExceptionHandler.FilterStart), 
				_ => 0, 
			};
			exceptionRegionEncoder.AddUnchecked(lazyExceptionHandler.Kind, labelOffsetChecked, labelOffsetChecked2 - labelOffsetChecked, labelOffsetChecked3, labelOffsetChecked4 - labelOffsetChecked3, catchTokenOrOffset);
		}
	}

	private bool HasSmallExceptionRegions()
	{
		if (!ExceptionRegionEncoder.IsSmallRegionCount(_lazyExceptionHandlers.Count))
		{
			return false;
		}
		foreach (ExceptionHandlerInfo lazyExceptionHandler in _lazyExceptionHandlers)
		{
			if (!ExceptionRegionEncoder.IsSmallExceptionRegionFromBounds(GetLabelOffsetChecked(lazyExceptionHandler.TryStart), GetLabelOffsetChecked(lazyExceptionHandler.TryEnd)) || !ExceptionRegionEncoder.IsSmallExceptionRegionFromBounds(GetLabelOffsetChecked(lazyExceptionHandler.HandlerStart), GetLabelOffsetChecked(lazyExceptionHandler.HandlerEnd)))
			{
				return false;
			}
		}
		return true;
	}
}

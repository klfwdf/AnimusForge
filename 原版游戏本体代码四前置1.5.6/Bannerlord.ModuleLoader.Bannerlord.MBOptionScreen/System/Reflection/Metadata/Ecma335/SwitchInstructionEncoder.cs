namespace System.Reflection.Metadata.Ecma335;

internal readonly struct SwitchInstructionEncoder
{
	private readonly InstructionEncoder _encoder;

	private readonly int _ilOffset;

	private readonly int _instructionEnd;

	internal SwitchInstructionEncoder(InstructionEncoder encoder, int ilOffset, int instructionEnd)
	{
		_encoder = encoder;
		_ilOffset = ilOffset;
		_instructionEnd = instructionEnd;
	}

	public void Branch(LabelHandle label)
	{
		_encoder.ControlFlowBuilder.SwitchBranchAdded();
		_encoder.LabelOperand(ILOpCode.Switch, label, _instructionEnd - _encoder.Offset, _ilOffset);
	}
}

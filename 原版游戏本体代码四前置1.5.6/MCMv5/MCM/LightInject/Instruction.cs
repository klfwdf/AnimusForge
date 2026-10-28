using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class Instruction
{
	public OpCode Code { get; private set; }

	public Action<ILGenerator> Emit { get; private set; }

	public Instruction(OpCode code, Action<ILGenerator> emitAction)
	{
		Code = code;
		Emit = emitAction;
	}

	public override string ToString()
	{
		return Code.ToString();
	}
}
[ExcludeFromCodeCoverage]
internal class Instruction<T> : Instruction
{
	public T Argument { get; private set; }

	public Instruction(OpCode code, T argument, Action<ILGenerator> emitAction)
		: base(code, emitAction)
	{
		Argument = argument;
	}

	public override string ToString()
	{
		return $"{base.ToString()} {Argument}";
	}
}

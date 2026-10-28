using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MCM.LightInject;

internal interface IEmitter
{
	Type StackType { get; }

	List<Instruction> Instructions { get; }

	void Emit(OpCode code);

	void Emit(OpCode code, string arg);

	void PushConstantValue(object arg, Type type);

	void Emit(OpCode code, int arg);

	void Emit(OpCode code, long arg);

	void Emit(OpCode code, sbyte arg);

	void Emit(OpCode code, byte arg);

	void Emit(OpCode code, Type type);

	void Emit(OpCode code, ConstructorInfo constructor);

	void Emit(OpCode code, LocalBuilder localBuilder);

	void Emit(OpCode code, MethodInfo methodInfo);

	LocalBuilder DeclareLocal(Type type);
}

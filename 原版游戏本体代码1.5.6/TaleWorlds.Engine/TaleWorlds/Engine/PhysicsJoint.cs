using System;

namespace TaleWorlds.Engine;

public sealed class PhysicsJoint
{
	private readonly UIntPtr _pointer;

	internal UIntPtr Pointer => _pointer;

	internal PhysicsJoint(UIntPtr ptr)
	{
		_pointer = ptr;
	}
}

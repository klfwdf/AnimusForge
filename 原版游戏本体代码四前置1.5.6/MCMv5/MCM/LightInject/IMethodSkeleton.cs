using System;

namespace MCM.LightInject;

internal interface IMethodSkeleton
{
	IEmitter GetEmitter();

	Delegate CreateDelegate(Type delegateType);
}

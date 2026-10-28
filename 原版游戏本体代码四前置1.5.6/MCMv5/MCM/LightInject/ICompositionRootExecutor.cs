using System;

namespace MCM.LightInject;

internal interface ICompositionRootExecutor
{
	void Execute(Type compositionRootType);

	void Execute<TCompositionRoot>(TCompositionRoot compositionRoot) where TCompositionRoot : ICompositionRoot;
}

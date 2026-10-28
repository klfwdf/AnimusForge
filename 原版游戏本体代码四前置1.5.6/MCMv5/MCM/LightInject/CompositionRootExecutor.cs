using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class CompositionRootExecutor : ICompositionRootExecutor
{
	private readonly IServiceRegistry serviceRegistry;

	private readonly Func<Type, ICompositionRoot> activator;

	private readonly IList<Type> executedCompositionRoots = new List<Type>();

	private readonly object syncRoot = new object();

	public CompositionRootExecutor(IServiceRegistry serviceRegistry, Func<Type, ICompositionRoot> activator)
	{
		this.serviceRegistry = serviceRegistry;
		this.activator = activator;
	}

	public void Execute(Type compositionRootType)
	{
		if (executedCompositionRoots.Contains(compositionRootType))
		{
			return;
		}
		lock (syncRoot)
		{
			if (!executedCompositionRoots.Contains(compositionRootType))
			{
				executedCompositionRoots.Add(compositionRootType);
				ICompositionRoot compositionRoot = activator(compositionRootType);
				compositionRoot.Compose(serviceRegistry);
			}
		}
	}

	public void Execute<TCompositionRoot>(TCompositionRoot compositionRoot) where TCompositionRoot : ICompositionRoot
	{
		if (executedCompositionRoots.Contains(typeof(TCompositionRoot)))
		{
			return;
		}
		lock (syncRoot)
		{
			if (!executedCompositionRoots.Contains(typeof(TCompositionRoot)))
			{
				executedCompositionRoots.Add(typeof(TCompositionRoot));
				compositionRoot.Compose(serviceRegistry);
			}
		}
	}
}

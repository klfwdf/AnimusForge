using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal abstract class ScopeManager : IScopeManager
{
	public abstract Scope CurrentScope { get; set; }

	public IServiceFactory ServiceFactory { get; }

	protected ScopeManager(IServiceFactory serviceFactory)
	{
		ServiceFactory = serviceFactory;
	}

	public Scope BeginScope()
	{
		Scope currentScope = CurrentScope;
		return CurrentScope = new Scope(this, currentScope);
	}

	public void EndScope(Scope scope)
	{
		Scope parentScope = scope.ParentScope;
		if (CurrentScope == scope)
		{
			CurrentScope = parentScope;
		}
	}

	protected Scope GetThisScopeOrFirstValidAncestor(Scope scope)
	{
		while (scope != null && scope.IsDisposed)
		{
			scope = scope.ParentScope;
		}
		CurrentScope = scope;
		return scope;
	}
}

namespace MCM.LightInject;

internal interface IScopeManager
{
	Scope CurrentScope { get; set; }

	IServiceFactory ServiceFactory { get; }

	Scope BeginScope();

	void EndScope(Scope scope);
}

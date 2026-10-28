namespace MCM.LightInject;

internal interface IScopeManagerProvider
{
	IScopeManager GetScopeManager(IServiceFactory serviceFactory);
}

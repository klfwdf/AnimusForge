namespace MCM.LightInject;

internal interface IConstructionInfoProvider
{
	ConstructionInfo GetConstructionInfo(Registration registration);
}

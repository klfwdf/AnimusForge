namespace MCM.LightInject;

internal interface IConstructionInfoBuilder
{
	ConstructionInfo Execute(Registration registration);
}

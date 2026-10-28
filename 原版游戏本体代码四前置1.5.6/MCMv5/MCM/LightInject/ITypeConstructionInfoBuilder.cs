namespace MCM.LightInject;

internal interface ITypeConstructionInfoBuilder
{
	ConstructionInfo Execute(Registration registration);
}

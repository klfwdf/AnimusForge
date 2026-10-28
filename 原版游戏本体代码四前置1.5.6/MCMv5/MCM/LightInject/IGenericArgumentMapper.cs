using System;

namespace MCM.LightInject;

internal interface IGenericArgumentMapper
{
	GenericMappingResult Map(Type genericServiceType, Type openGenericImplementingType);

	Type TryMakeGenericType(Type genericServiceType, Type openGenericImplementingType);
}

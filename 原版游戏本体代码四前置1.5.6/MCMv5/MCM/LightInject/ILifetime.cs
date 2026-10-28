using System;

namespace MCM.LightInject;

internal interface ILifetime
{
	object GetInstance(Func<object> createInstance, Scope scope);
}

using System;

namespace TaleWorlds.DotNet;

public interface IManagedComponent
{
	void OnCustomCallbackMethodPassed(string name, Delegate method);

	void OnStart();

	void OnApplicationTick(float dt);
}

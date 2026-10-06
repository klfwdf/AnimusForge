namespace TaleWorlds.Core;

public interface IEntityComponent
{
	void OnInitialize();

	void OnFinalize();
}

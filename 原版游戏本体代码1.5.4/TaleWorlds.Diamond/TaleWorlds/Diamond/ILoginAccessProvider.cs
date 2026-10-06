using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond;

public interface ILoginAccessProvider
{
	void Initialize(string preferredUserName, PlatformInitParams initParams);

	string GetUserName();

	PlayerId GetPlayerId();

	AccessObjectResult CreateAccessObject();
}

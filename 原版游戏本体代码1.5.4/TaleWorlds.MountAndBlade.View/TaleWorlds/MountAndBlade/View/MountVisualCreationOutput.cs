using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View;

public struct MountVisualCreationOutput
{
	public MetaMesh HorseManeMesh { get; private set; }

	public MetaMesh MountMesh { get; private set; }

	public MetaMesh ReinMesh { get; private set; }

	public MetaMesh MountHarnessMesh { get; private set; }

	public MountVisualCreationOutput(MetaMesh horseManeMesh, MetaMesh mountMesh, MetaMesh reinMesh, MetaMesh mountHarnessMesh)
	{
		HorseManeMesh = horseManeMesh;
		MountMesh = mountMesh;
		ReinMesh = reinMesh;
		MountHarnessMesh = mountHarnessMesh;
	}
}

namespace TaleWorlds.Library;

public enum TelemetryLevelMask : uint
{
	All = uint.MaxValue,
	Level_0 = 1u,
	Level_1 = 2u,
	Level_2 = 4u,
	Level_3 = 8u,
	Level_4 = 16u,
	Level_5 = 32u,
	Agent = 64u,
	Threading = 128u,
	Application = 256u,
	Graphics = 512u,
	Gui = 1024u,
	Agent_ai = 2048u,
	Mono_0 = 4096u,
	Mono_1 = 8192u,
	Mono_2 = 16384u,
	RenderThread = 32768u,
	Sound = 65536u,
	Idle = 131072u,
	AgentParallel = 262144u,
	AgentTest = 524288u,
	Network = 1048576u,
	Navmesh = 2097152u,
	Memory = 4194304u,
	LevelMaskCount = 24u
}

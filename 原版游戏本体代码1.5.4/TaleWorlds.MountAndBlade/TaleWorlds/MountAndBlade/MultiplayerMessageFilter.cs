using System;

namespace TaleWorlds.MountAndBlade;

[Flags]
public enum MultiplayerMessageFilter : ulong
{
	None = 0uL,
	Peers = 1uL,
	Messaging = 2uL,
	Items = 4uL,
	General = 8uL,
	Equipment = 0x10uL,
	EquipmentDetailed = 0x20uL,
	Formations = 0x40uL,
	Agents = 0x80uL,
	AgentsDetailed = 0x100uL,
	Mission = 0x200uL,
	MissionDetailed = 0x400uL,
	AgentAnimations = 0x800uL,
	SiegeWeapons = 0x1000uL,
	MissionObjects = 0x2000uL,
	MissionObjectsDetailed = 0x4000uL,
	SiegeWeaponsDetailed = 0x8000uL,
	Orders = 0x10000uL,
	GameMode = 0x20000uL,
	Administration = 0x40000uL,
	Particles = 0x80000uL,
	RPC = 0x100000uL,
	All = 0xFFFFFFFFuL,
	LightLogging = 0x22289uL,
	NormalLogging = 0x1E329DuL,
	AllWithoutDetails = 0x1F32DFuL
}

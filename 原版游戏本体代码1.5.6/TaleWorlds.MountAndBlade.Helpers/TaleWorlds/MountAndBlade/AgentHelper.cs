using System;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

internal static class AgentHelper
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static Vec3 GetAgentPosition(UIntPtr agentPositionPointer)
	{
		Vec3* ptr = (Vec3*)agentPositionPointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void SetAgentPosition(UIntPtr agentPositionPointer, ref Vec3 newPos)
	{
		Debug.FailedAssert("Do not use this!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Helpers\\Helper.cs", "SetAgentPosition", 20);
		Vec3* ptr = (Vec3*)agentPositionPointer.ToPointer();
		*ptr = newPos;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static int GetAgentIndex(UIntPtr indexPtr)
	{
		int* ptr = (int*)indexPtr.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static AgentFlag GetAgentFlags(UIntPtr flagsPtr)
	{
		AgentFlag* ptr = (AgentFlag*)flagsPtr.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static AgentState GetAgentState(UIntPtr statePtr)
	{
		AgentState* ptr = (AgentState*)statePtr.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static AgentMovementMode GetAgentMovementMode(UIntPtr movementModePointer)
	{
		AgentMovementMode* ptr = (AgentMovementMode*)movementModePointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static AgentControllerType GetAgentControllerType(UIntPtr controllerTypePointer)
	{
		AgentControllerType* ptr = (AgentControllerType*)controllerTypePointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static float GetAgentMovementDirectionAsAngle(UIntPtr movementDirectionPointer)
	{
		float* ptr = (float*)movementDirectionPointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static EquipmentIndex GetPrimaryWieldedItemIndex(UIntPtr primaryWieldedItemIndexPointer)
	{
		EquipmentIndex* ptr = (EquipmentIndex*)primaryWieldedItemIndexPointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static EquipmentIndex GetOffhandWieldedItemIndex(UIntPtr offhandWieldedItemIndexPointer)
	{
		EquipmentIndex* ptr = (EquipmentIndex*)offhandWieldedItemIndexPointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static int GetChannel0CurrentActionIndex(UIntPtr channel0CurrentActionPointer)
	{
		int* ptr = (int*)channel0CurrentActionPointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static int GetChannel1CurrentActionIndex(UIntPtr channel1CurrentActionPointer)
	{
		int* ptr = (int*)channel1CurrentActionPointer.ToPointer();
		return *ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static float GetMaximumForwardUnlimitedSpeed(UIntPtr maximumForwardUnlimitedSpeed)
	{
		float* ptr = (float*)maximumForwardUnlimitedSpeed.ToPointer();
		return *ptr;
	}
}

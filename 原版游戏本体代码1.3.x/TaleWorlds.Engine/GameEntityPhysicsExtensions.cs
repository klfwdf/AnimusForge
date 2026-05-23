using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200004E RID: 78
	public static class GameEntityPhysicsExtensions
	{
		// Token: 0x060007E3 RID: 2019 RVA: 0x00005CC2 File Offset: 0x00003EC2
		public static bool HasBody(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasBody(gameEntity.Pointer);
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x00005CD4 File Offset: 0x00003ED4
		public static bool HasBody(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasBody(gameEntity.Pointer);
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x00005CE7 File Offset: 0x00003EE7
		public static void AddSphereAsBody(this GameEntity gameEntity, Vec3 sphere, float radius, BodyFlags bodyFlags)
		{
			EngineApplicationInterface.IGameEntity.AddSphereAsBody(gameEntity.Pointer, sphere, radius, (uint)bodyFlags);
		}

		// Token: 0x060007E6 RID: 2022 RVA: 0x00005CFC File Offset: 0x00003EFC
		public static void AddCapsuleAsBody(this GameEntity gameEntity, Vec3 p1, Vec3 p2, float radius, BodyFlags bodyFlags, string physicsMaterialName = "")
		{
			EngineApplicationInterface.IGameEntity.AddCapsuleAsBody(gameEntity.Pointer, p1, p2, radius, (uint)bodyFlags, physicsMaterialName);
		}

		// Token: 0x060007E7 RID: 2023 RVA: 0x00005D15 File Offset: 0x00003F15
		public static void PushCapsuleShapeToEntityBody(this WeakGameEntity gameEntity, Vec3 p1, Vec3 p2, float radius, string physicsMaterialName)
		{
			EngineApplicationInterface.IGameEntity.PushCapsuleShapeToEntityBody(gameEntity.Pointer, p1, p2, radius, physicsMaterialName);
		}

		// Token: 0x060007E8 RID: 2024 RVA: 0x00005D2D File Offset: 0x00003F2D
		public static void AddSphereAsBody(this WeakGameEntity gameEntity, Vec3 sphere, float radius, BodyFlags bodyFlags)
		{
			EngineApplicationInterface.IGameEntity.AddSphereAsBody(gameEntity.Pointer, sphere, radius, (uint)bodyFlags);
		}

		// Token: 0x060007E9 RID: 2025 RVA: 0x00005D43 File Offset: 0x00003F43
		public static void AddCapsuleAsBody(this WeakGameEntity gameEntity, Vec3 p1, Vec3 p2, float radius, BodyFlags bodyFlags, string physicsMaterialName = "")
		{
			EngineApplicationInterface.IGameEntity.AddCapsuleAsBody(gameEntity.Pointer, p1, p2, radius, (uint)bodyFlags, physicsMaterialName);
		}

		// Token: 0x060007EA RID: 2026 RVA: 0x00005D5D File Offset: 0x00003F5D
		public static void PopCapsuleShapeFromEntityBody(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.PopCapsuleShapeFromEntityBody(gameEntity.Pointer);
		}

		// Token: 0x060007EB RID: 2027 RVA: 0x00005D70 File Offset: 0x00003F70
		public static void RemovePhysics(this GameEntity gameEntity, bool clearingTheScene = false)
		{
			EngineApplicationInterface.IGameEntity.RemovePhysics(gameEntity.Pointer, clearingTheScene);
		}

		// Token: 0x060007EC RID: 2028 RVA: 0x00005D83 File Offset: 0x00003F83
		public static void RemovePhysics(this WeakGameEntity gameEntity, bool clearingTheScene = false)
		{
			EngineApplicationInterface.IGameEntity.RemovePhysics(gameEntity.Pointer, clearingTheScene);
		}

		// Token: 0x060007ED RID: 2029 RVA: 0x00005D97 File Offset: 0x00003F97
		public static bool GetPhysicsState(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetPhysicsState(gameEntity.Pointer);
		}

		// Token: 0x060007EE RID: 2030 RVA: 0x00005DA9 File Offset: 0x00003FA9
		public static bool GetPhysicsState(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetPhysicsState(gameEntity.Pointer);
		}

		// Token: 0x060007EF RID: 2031 RVA: 0x00005DBC File Offset: 0x00003FBC
		public static int GetPhysicsTriangleCount(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetPhysicsTriangleCount(gameEntity.Pointer);
		}

		// Token: 0x060007F0 RID: 2032 RVA: 0x00005DCF File Offset: 0x00003FCF
		public static int GetPhysicsTriangleCount(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetPhysicsTriangleCount(gameEntity.Pointer);
		}

		// Token: 0x060007F1 RID: 2033 RVA: 0x00005DE1 File Offset: 0x00003FE1
		public static bool HasPhysicsDefinitionWithoutFlags(this GameEntity gameEntity, int excludeFlags)
		{
			return EngineApplicationInterface.IGameEntity.HasPhysicsDefinition(gameEntity.Pointer, excludeFlags);
		}

		// Token: 0x060007F2 RID: 2034 RVA: 0x00005DF4 File Offset: 0x00003FF4
		public static bool HasPhysicsDefinitionWithoutFlags(this WeakGameEntity gameEntity, int excludeFlags)
		{
			return EngineApplicationInterface.IGameEntity.HasPhysicsDefinition(gameEntity.Pointer, excludeFlags);
		}

		// Token: 0x060007F3 RID: 2035 RVA: 0x00005E08 File Offset: 0x00004008
		public static bool HasPhysicsBody(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasPhysicsBody(gameEntity.Pointer);
		}

		// Token: 0x060007F4 RID: 2036 RVA: 0x00005E1A File Offset: 0x0000401A
		public static bool HasPhysicsBody(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasPhysicsBody(gameEntity.Pointer);
		}

		// Token: 0x060007F5 RID: 2037 RVA: 0x00005E2D File Offset: 0x0000402D
		public static bool HasDynamicRigidBody(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasDynamicRigidBody(gameEntity.Pointer);
		}

		// Token: 0x060007F6 RID: 2038 RVA: 0x00005E3F File Offset: 0x0000403F
		public static bool HasDynamicRigidBody(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasDynamicRigidBody(gameEntity.Pointer);
		}

		// Token: 0x060007F7 RID: 2039 RVA: 0x00005E52 File Offset: 0x00004052
		public static bool HasKinematicRigidBody(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasKinematicRigidBody(gameEntity.Pointer);
		}

		// Token: 0x060007F8 RID: 2040 RVA: 0x00005E64 File Offset: 0x00004064
		public static bool HasKinematicRigidBody(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasKinematicRigidBody(gameEntity.Pointer);
		}

		// Token: 0x060007F9 RID: 2041 RVA: 0x00005E77 File Offset: 0x00004077
		public static bool HasStaticPhysicsBody(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasStaticPhysicsBody(gameEntity.Pointer);
		}

		// Token: 0x060007FA RID: 2042 RVA: 0x00005E89 File Offset: 0x00004089
		public static bool HasStaticPhysicsBody(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasStaticPhysicsBody(gameEntity.Pointer);
		}

		// Token: 0x060007FB RID: 2043 RVA: 0x00005E9C File Offset: 0x0000409C
		public static bool HasDynamicRigidBodyAndActiveSimulation(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasDynamicRigidBodyAndActiveSimulation(gameEntity.Pointer);
		}

		// Token: 0x060007FC RID: 2044 RVA: 0x00005EAE File Offset: 0x000040AE
		public static bool HasDynamicRigidBodyAndActiveSimulation(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.HasDynamicRigidBodyAndActiveSimulation(gameEntity.Pointer);
		}

		// Token: 0x060007FD RID: 2045 RVA: 0x00005EC1 File Offset: 0x000040C1
		public static void CreateVariableRatePhysics(this GameEntity gameEntity, bool forChildren)
		{
			EngineApplicationInterface.IGameEntity.CreateVariableRatePhysics(gameEntity.Pointer, forChildren);
		}

		// Token: 0x060007FE RID: 2046 RVA: 0x00005ED4 File Offset: 0x000040D4
		public static void CreateVariableRatePhysics(this WeakGameEntity gameEntity, bool forChildren)
		{
			EngineApplicationInterface.IGameEntity.CreateVariableRatePhysics(gameEntity.Pointer, forChildren);
		}

		// Token: 0x060007FF RID: 2047 RVA: 0x00005EE8 File Offset: 0x000040E8
		public static void SetPhysicsState(this GameEntity gameEntity, bool isEnabled, bool setChildren)
		{
			EngineApplicationInterface.IGameEntity.SetPhysicsState(gameEntity.Pointer, isEnabled, setChildren);
		}

		// Token: 0x06000800 RID: 2048 RVA: 0x00005EFC File Offset: 0x000040FC
		public static void SetPhysicsState(this WeakGameEntity gameEntity, bool isEnabled, bool setChildren)
		{
			EngineApplicationInterface.IGameEntity.SetPhysicsState(gameEntity.Pointer, isEnabled, setChildren);
		}

		// Token: 0x06000801 RID: 2049 RVA: 0x00005F11 File Offset: 0x00004111
		public static void SetPhysicsStateOnlyVariable(this GameEntity gameEntity, bool isEnabled, bool setChildren)
		{
			EngineApplicationInterface.IGameEntity.SetPhysicsStateOnlyVariable(gameEntity.Pointer, isEnabled, setChildren);
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x00005F25 File Offset: 0x00004125
		public static void SetPhysicsStateOnlyVariable(this WeakGameEntity gameEntity, bool isEnabled, bool setChildren)
		{
			EngineApplicationInterface.IGameEntity.SetPhysicsStateOnlyVariable(gameEntity.Pointer, isEnabled, setChildren);
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x00005F3A File Offset: 0x0000413A
		public static void RemoveEnginePhysics(this GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.RemoveEnginePhysics(gameEntity.Pointer);
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x00005F4C File Offset: 0x0000414C
		public static void RemoveEnginePhysics(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.RemoveEnginePhysics(gameEntity.Pointer);
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x00005F5F File Offset: 0x0000415F
		public static bool IsEngineBodySleeping(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsEngineBodySleeping(gameEntity.Pointer);
		}

		// Token: 0x06000806 RID: 2054 RVA: 0x00005F71 File Offset: 0x00004171
		public static bool IsEngineBodySleeping(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsEngineBodySleeping(gameEntity.Pointer);
		}

		// Token: 0x06000807 RID: 2055 RVA: 0x00005F84 File Offset: 0x00004184
		public static bool IsDynamicBodyStationary(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
		}

		// Token: 0x06000808 RID: 2056 RVA: 0x00005F96 File Offset: 0x00004196
		public static bool IsDynamicBodyStationary(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
		}

		// Token: 0x06000809 RID: 2057 RVA: 0x00005FA9 File Offset: 0x000041A9
		public static bool IsDynamicBodyStationaryMT(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
		}

		// Token: 0x0600080A RID: 2058 RVA: 0x00005FBB File Offset: 0x000041BB
		public static bool IsDynamicBodyStationaryMT(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
		}

		// Token: 0x0600080B RID: 2059 RVA: 0x00005FCE File Offset: 0x000041CE
		public static void ReplacePhysicsBodyWithQuadPhysicsBody(this GameEntity gameEntity, UIntPtr vertices, int numberOfVertices, PhysicsMaterial physicsMaterial, BodyFlags bodyFlags, UIntPtr indices, int numberOfIndices)
		{
			EngineApplicationInterface.IGameEntity.ReplacePhysicsBodyWithQuadPhysicsBody(gameEntity.Pointer, vertices, physicsMaterial.Index, bodyFlags, numberOfVertices, indices, numberOfIndices);
		}

		// Token: 0x0600080C RID: 2060 RVA: 0x00005FEE File Offset: 0x000041EE
		public static void ReplacePhysicsBodyWithQuadPhysicsBody(this WeakGameEntity gameEntity, UIntPtr vertices, int numberOfVertices, PhysicsMaterial physicsMaterial, BodyFlags bodyFlags, UIntPtr indices, int numberOfIndices)
		{
			EngineApplicationInterface.IGameEntity.ReplacePhysicsBodyWithQuadPhysicsBody(gameEntity.Pointer, vertices, physicsMaterial.Index, bodyFlags, numberOfVertices, indices, numberOfIndices);
		}

		// Token: 0x0600080D RID: 2061 RVA: 0x0000600F File Offset: 0x0000420F
		public static PhysicsShape GetBodyShape(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetBodyShape(gameEntity.Pointer);
		}

		// Token: 0x0600080E RID: 2062 RVA: 0x00006021 File Offset: 0x00004221
		public static PhysicsShape GetBodyShape(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetBodyShape(gameEntity.Pointer);
		}

		// Token: 0x0600080F RID: 2063 RVA: 0x00006034 File Offset: 0x00004234
		public static void SetBodyShape(this GameEntity gameEntity, PhysicsShape shape)
		{
			EngineApplicationInterface.IGameEntity.SetBodyShape(gameEntity.Pointer, (shape == null) ? ((UIntPtr)0UL) : shape.Pointer);
		}

		// Token: 0x06000810 RID: 2064 RVA: 0x0000605E File Offset: 0x0000425E
		public static void SetBodyShape(this WeakGameEntity gameEntity, PhysicsShape shape)
		{
			EngineApplicationInterface.IGameEntity.SetBodyShape(gameEntity.Pointer, (shape == null) ? ((UIntPtr)0UL) : shape.Pointer);
		}

		// Token: 0x06000811 RID: 2065 RVA: 0x0000608C File Offset: 0x0000428C
		public static void AddPhysics(this GameEntity gameEntity, float mass, Vec3 localCenterOfMass, PhysicsShape body, Vec3 initialGlobalVelocity, Vec3 angularGlobalVelocity, PhysicsMaterial physicsMaterial, bool isStatic, int collisionGroupID)
		{
			EngineApplicationInterface.IGameEntity.AddPhysics(gameEntity.Pointer, (body != null) ? body.Pointer : UIntPtr.Zero, mass, ref localCenterOfMass, ref initialGlobalVelocity, ref angularGlobalVelocity, physicsMaterial.Index, isStatic, collisionGroupID);
		}

		// Token: 0x06000812 RID: 2066 RVA: 0x000060D4 File Offset: 0x000042D4
		public static void AddPhysics(this WeakGameEntity gameEntity, float mass, Vec3 localCenterOfMass, PhysicsShape body, Vec3 initialVelocity, Vec3 angularVelocity, PhysicsMaterial physicsMaterial, bool isStatic, int collisionGroupID)
		{
			EngineApplicationInterface.IGameEntity.AddPhysics(gameEntity.Pointer, (body != null) ? body.Pointer : UIntPtr.Zero, mass, ref localCenterOfMass, ref initialVelocity, ref angularVelocity, physicsMaterial.Index, isStatic, collisionGroupID);
		}

		// Token: 0x06000813 RID: 2067 RVA: 0x0000611A File Offset: 0x0000431A
		public static void SetVelocityLimits(this GameEntity gameEntity, float maxLinearVelocity, float maxAngularVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetVelocityLimits(gameEntity.Pointer, maxLinearVelocity, maxAngularVelocity);
		}

		// Token: 0x06000814 RID: 2068 RVA: 0x0000612E File Offset: 0x0000432E
		public static void SetVelocityLimits(this WeakGameEntity gameEntity, float maxLinearVelocity, float maxAngularVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetVelocityLimits(gameEntity.Pointer, maxLinearVelocity, maxAngularVelocity);
		}

		// Token: 0x06000815 RID: 2069 RVA: 0x00006143 File Offset: 0x00004343
		public static void SetMaxDepenetrationVelocity(this GameEntity gameEntity, float maxDepenetrationVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetMaxDepenetrationVelocity(gameEntity.Pointer, maxDepenetrationVelocity);
		}

		// Token: 0x06000816 RID: 2070 RVA: 0x00006156 File Offset: 0x00004356
		public static void SetMaxDepenetrationVelocity(this WeakGameEntity gameEntity, float maxDepenetrationVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetMaxDepenetrationVelocity(gameEntity.Pointer, maxDepenetrationVelocity);
		}

		// Token: 0x06000817 RID: 2071 RVA: 0x0000616A File Offset: 0x0000436A
		public static void SetSolverIterationCounts(this GameEntity gameEntity, int positionIterationCount, int velocityIterationCount)
		{
			EngineApplicationInterface.IGameEntity.SetSolverIterationCounts(gameEntity.Pointer, positionIterationCount, velocityIterationCount);
		}

		// Token: 0x06000818 RID: 2072 RVA: 0x0000617E File Offset: 0x0000437E
		public static void SetSolverIterationCounts(this WeakGameEntity gameEntity, int positionIterationCount, int velocityIterationCount)
		{
			EngineApplicationInterface.IGameEntity.SetSolverIterationCounts(gameEntity.Pointer, positionIterationCount, velocityIterationCount);
		}

		// Token: 0x06000819 RID: 2073 RVA: 0x00006193 File Offset: 0x00004393
		public static void ApplyLocalImpulseToDynamicBody(this GameEntity gameEntity, Vec3 localPosition, Vec3 impulse)
		{
			EngineApplicationInterface.IGameEntity.ApplyLocalImpulseToDynamicBody(gameEntity.Pointer, ref localPosition, ref impulse);
		}

		// Token: 0x0600081A RID: 2074 RVA: 0x000061A9 File Offset: 0x000043A9
		public static void ApplyLocalImpulseToDynamicBody(this WeakGameEntity gameEntity, Vec3 localPosition, Vec3 impulse)
		{
			EngineApplicationInterface.IGameEntity.ApplyLocalImpulseToDynamicBody(gameEntity.Pointer, ref localPosition, ref impulse);
		}

		// Token: 0x0600081B RID: 2075 RVA: 0x000061C0 File Offset: 0x000043C0
		public static void ApplyForceToDynamicBody(this GameEntity gameEntity, Vec3 force, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyForceToDynamicBody(gameEntity.Pointer, ref force, forceMode);
		}

		// Token: 0x0600081C RID: 2076 RVA: 0x000061D5 File Offset: 0x000043D5
		public static void ApplyForceToDynamicBody(this WeakGameEntity gameEntity, Vec3 force, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyForceToDynamicBody(gameEntity.Pointer, ref force, forceMode);
		}

		// Token: 0x0600081D RID: 2077 RVA: 0x000061EB File Offset: 0x000043EB
		public static void ApplyGlobalForceAtLocalPosToDynamicBody(this GameEntity gameEntity, Vec3 localPosition, Vec3 globalForce, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref globalForce, forceMode);
		}

		// Token: 0x0600081E RID: 2078 RVA: 0x00006202 File Offset: 0x00004402
		public static void ApplyGlobalForceAtLocalPosToDynamicBody(this WeakGameEntity gameEntity, Vec3 localPosition, Vec3 globalForce, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref globalForce, forceMode);
		}

		// Token: 0x0600081F RID: 2079 RVA: 0x0000621A File Offset: 0x0000441A
		public static void ApplyTorqueToDynamicBody(this GameEntity gameEntity, Vec3 torque, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyTorqueToDynamicBody(gameEntity.Pointer, ref torque, forceMode);
		}

		// Token: 0x06000820 RID: 2080 RVA: 0x0000622F File Offset: 0x0000442F
		public static void ApplyTorqueToDynamicBody(this WeakGameEntity gameEntity, Vec3 torque, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyTorqueToDynamicBody(gameEntity.Pointer, ref torque, forceMode);
		}

		// Token: 0x06000821 RID: 2081 RVA: 0x00006245 File Offset: 0x00004445
		public static void ApplyLocalForceAtLocalPosToDynamicBody(this GameEntity gameEntity, Vec3 localPosition, Vec3 localForce, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyLocalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref localForce, forceMode);
		}

		// Token: 0x06000822 RID: 2082 RVA: 0x0000625C File Offset: 0x0000445C
		public static void ApplyLocalForceAtLocalPosToDynamicBody(this WeakGameEntity gameEntity, Vec3 localPosition, Vec3 localForce, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			EngineApplicationInterface.IGameEntity.ApplyLocalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref localForce, forceMode);
		}

		// Token: 0x06000823 RID: 2083 RVA: 0x00006274 File Offset: 0x00004474
		public static void ApplyAccelerationToDynamicBody(this GameEntity gameEntity, Vec3 acceleration)
		{
			EngineApplicationInterface.IGameEntity.ApplyAccelerationToDynamicBody(gameEntity.Pointer, ref acceleration);
		}

		// Token: 0x06000824 RID: 2084 RVA: 0x00006288 File Offset: 0x00004488
		public static void ApplyAccelerationToDynamicBody(this WeakGameEntity gameEntity, Vec3 acceleration)
		{
			EngineApplicationInterface.IGameEntity.ApplyAccelerationToDynamicBody(gameEntity.Pointer, ref acceleration);
		}

		// Token: 0x06000825 RID: 2085 RVA: 0x0000629D File Offset: 0x0000449D
		public static void DisableDynamicBodySimulation(this GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
		}

		// Token: 0x06000826 RID: 2086 RVA: 0x000062AF File Offset: 0x000044AF
		public static void DisableDynamicBodySimulation(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
		}

		// Token: 0x06000827 RID: 2087 RVA: 0x000062C2 File Offset: 0x000044C2
		public static void DisableDynamicBodySimulationMT(this GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
		}

		// Token: 0x06000828 RID: 2088 RVA: 0x000062D4 File Offset: 0x000044D4
		public static void DisableDynamicBodySimulationMT(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
		}

		// Token: 0x06000829 RID: 2089 RVA: 0x000062E7 File Offset: 0x000044E7
		public static void ConvertDynamicBodyToRayCast(this GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.ConvertDynamicBodyToRayCast(gameEntity.Pointer);
		}

		// Token: 0x0600082A RID: 2090 RVA: 0x000062F9 File Offset: 0x000044F9
		public static void ConvertDynamicBodyToRayCast(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.ConvertDynamicBodyToRayCast(gameEntity.Pointer);
		}

		// Token: 0x0600082B RID: 2091 RVA: 0x0000630C File Offset: 0x0000450C
		public static void SetPhysicsMoveToBatched(this GameEntity gameEntity, bool value)
		{
			EngineApplicationInterface.IGameEntity.SetPhysicsMoveToBatched(gameEntity.Pointer, value);
		}

		// Token: 0x0600082C RID: 2092 RVA: 0x0000631F File Offset: 0x0000451F
		public static void SetPhysicsMoveToBatched(this WeakGameEntity gameEntity, bool value)
		{
			EngineApplicationInterface.IGameEntity.SetPhysicsMoveToBatched(gameEntity.Pointer, value);
		}

		// Token: 0x0600082D RID: 2093 RVA: 0x00006333 File Offset: 0x00004533
		public static void EnableDynamicBody(this GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.EnableDynamicBody(gameEntity.Pointer);
		}

		// Token: 0x0600082E RID: 2094 RVA: 0x00006345 File Offset: 0x00004545
		public static void EnableDynamicBody(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.EnableDynamicBody(gameEntity.Pointer);
		}

		// Token: 0x0600082F RID: 2095 RVA: 0x00006358 File Offset: 0x00004558
		public static float GetMass(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetMass(gameEntity.Pointer);
		}

		// Token: 0x06000830 RID: 2096 RVA: 0x0000636A File Offset: 0x0000456A
		public static float GetMass(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetMass(gameEntity.Pointer);
		}

		// Token: 0x06000831 RID: 2097 RVA: 0x0000637D File Offset: 0x0000457D
		public static void SetMassAndUpdateInertiaAndCenterOfMass(this GameEntity gameEntity, float mass)
		{
			EngineApplicationInterface.IGameEntity.SetMassAndUpdateInertiaAndCenterOfMass(gameEntity.Pointer, mass);
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x00006390 File Offset: 0x00004590
		public static void SetMassAndUpdateInertiaAndCenterOfMass(this WeakGameEntity gameEntity, float mass)
		{
			EngineApplicationInterface.IGameEntity.SetMassAndUpdateInertiaAndCenterOfMass(gameEntity.Pointer, mass);
		}

		// Token: 0x06000833 RID: 2099 RVA: 0x000063A4 File Offset: 0x000045A4
		public static void SetCenterOfMass(this GameEntity gameEntity, Vec3 localCenterOfMass)
		{
			EngineApplicationInterface.IGameEntity.SetCenterOfMass(gameEntity.Pointer, ref localCenterOfMass);
		}

		// Token: 0x06000834 RID: 2100 RVA: 0x000063B8 File Offset: 0x000045B8
		public static void SetCenterOfMass(this WeakGameEntity gameEntity, Vec3 centerOfMass)
		{
			EngineApplicationInterface.IGameEntity.SetCenterOfMass(gameEntity.Pointer, ref centerOfMass);
		}

		// Token: 0x06000835 RID: 2101 RVA: 0x000063CD File Offset: 0x000045CD
		public static Vec3 GetMassSpaceInertia(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetMassSpaceInertia(gameEntity.Pointer);
		}

		// Token: 0x06000836 RID: 2102 RVA: 0x000063DF File Offset: 0x000045DF
		public static Vec3 GetMassSpaceInertia(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetMassSpaceInertia(gameEntity.Pointer);
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x000063F2 File Offset: 0x000045F2
		public static Vec3 GetMassSpaceInverseInertia(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetMassSpaceInverseInertia(gameEntity.Pointer);
		}

		// Token: 0x06000838 RID: 2104 RVA: 0x00006404 File Offset: 0x00004604
		public static Vec3 GetMassSpaceInverseInertia(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetMassSpaceInverseInertia(gameEntity.Pointer);
		}

		// Token: 0x06000839 RID: 2105 RVA: 0x00006417 File Offset: 0x00004617
		public static void SetMassSpaceInertia(this GameEntity gameEntity, Vec3 inertia)
		{
			EngineApplicationInterface.IGameEntity.SetMassSpaceInertia(gameEntity.Pointer, ref inertia);
		}

		// Token: 0x0600083A RID: 2106 RVA: 0x0000642B File Offset: 0x0000462B
		public static void SetMassSpaceInertia(this WeakGameEntity gameEntity, Vec3 inertia)
		{
			EngineApplicationInterface.IGameEntity.SetMassSpaceInertia(gameEntity.Pointer, ref inertia);
		}

		// Token: 0x0600083B RID: 2107 RVA: 0x00006440 File Offset: 0x00004640
		public static void SetDamping(this GameEntity gameEntity, float linearDamping, float angularDamping)
		{
			EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
		}

		// Token: 0x0600083C RID: 2108 RVA: 0x00006454 File Offset: 0x00004654
		public static void SetDamping(this WeakGameEntity gameEntity, float linearDamping, float angularDamping)
		{
			EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
		}

		// Token: 0x0600083D RID: 2109 RVA: 0x00006469 File Offset: 0x00004669
		public static void SetDampingMT(this GameEntity gameEntity, float linearDamping, float angularDamping)
		{
			EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
		}

		// Token: 0x0600083E RID: 2110 RVA: 0x0000647D File Offset: 0x0000467D
		public static void SetDampingMT(this WeakGameEntity gameEntity, float linearDamping, float angularDamping)
		{
			EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
		}

		// Token: 0x0600083F RID: 2111 RVA: 0x00006492 File Offset: 0x00004692
		public static void DisableGravity(this GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.DisableGravity(gameEntity.Pointer);
		}

		// Token: 0x06000840 RID: 2112 RVA: 0x000064A4 File Offset: 0x000046A4
		public static void DisableGravity(this WeakGameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.DisableGravity(gameEntity.Pointer);
		}

		// Token: 0x06000841 RID: 2113 RVA: 0x000064B7 File Offset: 0x000046B7
		public static bool IsGravityDisabled(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsGravityDisabled(gameEntity.Pointer);
		}

		// Token: 0x06000842 RID: 2114 RVA: 0x000064C9 File Offset: 0x000046C9
		public static bool IsGravityDisabled(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.IsGravityDisabled(gameEntity.Pointer);
		}

		// Token: 0x06000843 RID: 2115 RVA: 0x000064DC File Offset: 0x000046DC
		public static Vec3 GetLinearVelocity(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
		}

		// Token: 0x06000844 RID: 2116 RVA: 0x000064EE File Offset: 0x000046EE
		public static Vec3 GetLinearVelocity(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
		}

		// Token: 0x06000845 RID: 2117 RVA: 0x00006501 File Offset: 0x00004701
		public static void SetLinearVelocity(this GameEntity gameEntity, Vec3 newLinearVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetLinearVelocity(gameEntity.Pointer, newLinearVelocity);
		}

		// Token: 0x06000846 RID: 2118 RVA: 0x00006514 File Offset: 0x00004714
		public static void SetLinearVelocity(this WeakGameEntity gameEntity, Vec3 newLinearVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetLinearVelocity(gameEntity.Pointer, newLinearVelocity);
		}

		// Token: 0x06000847 RID: 2119 RVA: 0x00006528 File Offset: 0x00004728
		public static Vec3 GetLinearVelocityMT(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
		}

		// Token: 0x06000848 RID: 2120 RVA: 0x0000653A File Offset: 0x0000473A
		public static Vec3 GetLinearVelocityMT(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
		}

		// Token: 0x06000849 RID: 2121 RVA: 0x0000654D File Offset: 0x0000474D
		public static Vec3 GetAngularVelocity(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
		}

		// Token: 0x0600084A RID: 2122 RVA: 0x0000655F File Offset: 0x0000475F
		public static Vec3 GetAngularVelocity(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
		}

		// Token: 0x0600084B RID: 2123 RVA: 0x00006572 File Offset: 0x00004772
		public static Vec3 GetAngularVelocityMT(this GameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
		}

		// Token: 0x0600084C RID: 2124 RVA: 0x00006584 File Offset: 0x00004784
		public static Vec3 GetAngularVelocityMT(this WeakGameEntity gameEntity)
		{
			return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
		}

		// Token: 0x0600084D RID: 2125 RVA: 0x00006597 File Offset: 0x00004797
		public static void SetAngularVelocity(this GameEntity gameEntity, Vec3 newAngularVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetAngularVelocity(gameEntity.Pointer, newAngularVelocity);
		}

		// Token: 0x0600084E RID: 2126 RVA: 0x000065AB File Offset: 0x000047AB
		public static void SetAngularVelocity(this WeakGameEntity gameEntity, Vec3 newAngularVelocity)
		{
			EngineApplicationInterface.IGameEntity.SetAngularVelocity(gameEntity.Pointer, newAngularVelocity);
		}

		// Token: 0x0600084F RID: 2127 RVA: 0x000065C0 File Offset: 0x000047C0
		public static void GetPhysicsMinMax(this GameEntity gameEntity, bool includeChildren, out Vec3 bbmin, out Vec3 bbmax, bool returnLocal)
		{
			bbmin = Vec3.Zero;
			bbmax = Vec3.Zero;
			EngineApplicationInterface.IGameEntity.GetPhysicsMinMax(gameEntity.Pointer, includeChildren, ref bbmin, ref bbmax, returnLocal);
		}

		// Token: 0x06000850 RID: 2128 RVA: 0x000065ED File Offset: 0x000047ED
		public static void GetPhysicsMinMax(this WeakGameEntity gameEntity, bool includeChildren, out Vec3 bbmin, out Vec3 bbmax, bool returnLocal)
		{
			bbmin = Vec3.Zero;
			bbmax = Vec3.Zero;
			EngineApplicationInterface.IGameEntity.GetPhysicsMinMax(gameEntity.Pointer, includeChildren, ref bbmin, ref bbmax, returnLocal);
		}

		// Token: 0x06000851 RID: 2129 RVA: 0x0000661C File Offset: 0x0000481C
		public static BoundingBox GetLocalPhysicsBoundingBox(this GameEntity gameEntity, bool includeChildren)
		{
			BoundingBox result;
			EngineApplicationInterface.IGameEntity.GetLocalPhysicsBoundingBox(gameEntity.Pointer, includeChildren, out result);
			return result;
		}

		// Token: 0x06000852 RID: 2130 RVA: 0x00006640 File Offset: 0x00004840
		public static BoundingBox GetLocalPhysicsBoundingBox(this WeakGameEntity gameEntity, bool includeChildren)
		{
			BoundingBox result;
			EngineApplicationInterface.IGameEntity.GetLocalPhysicsBoundingBox(gameEntity.Pointer, includeChildren, out result);
			return result;
		}

		// Token: 0x06000853 RID: 2131 RVA: 0x00006664 File Offset: 0x00004864
		public static Vec3 GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(this WeakGameEntity entity, Vec3 globalPoint)
		{
			MatrixFrame bodyWorldTransform = entity.GetBodyWorldTransform();
			Vec3 centerOfMass = entity.CenterOfMass;
			Vec3 vb = globalPoint - bodyWorldTransform.TransformToParent(centerOfMass);
			Vec3 v = Vec3.CrossProduct(entity.GetAngularVelocity(), vb);
			return entity.GetLinearVelocity() + v;
		}

		// Token: 0x06000854 RID: 2132 RVA: 0x000066AC File Offset: 0x000048AC
		public static Vec3 GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(this GameEntity entity, Vec3 globalPoint)
		{
			MatrixFrame bodyWorldTransform = entity.GetBodyWorldTransform();
			Vec3 centerOfMass = entity.CenterOfMass;
			Vec3 vb = globalPoint - bodyWorldTransform.TransformToParent(centerOfMass);
			Vec3 v = Vec3.CrossProduct(entity.GetAngularVelocity(), vb);
			return entity.GetLinearVelocity() + v;
		}

		// Token: 0x06000855 RID: 2133 RVA: 0x000066F0 File Offset: 0x000048F0
		public static void ComputeVelocityDeltaFromImpulse(this WeakGameEntity gameEntity, in Vec3 impulseGlobal, in Vec3 impulsiveTorqueGlobal, out Vec3 deltaGlobalLinearVelocity, out Vec3 deltaGlobalAngularVelocity)
		{
			EngineApplicationInterface.IGameEntity.ComputeVelocityDeltaFromImpulse(gameEntity.Pointer, impulseGlobal, impulsiveTorqueGlobal, out deltaGlobalLinearVelocity, out deltaGlobalAngularVelocity);
		}

		// Token: 0x020000BE RID: 190
		[EngineStruct("rglPhysics_engine_body::Force_mode", false, null)]
		public enum ForceMode : sbyte
		{
			// Token: 0x040003A2 RID: 930
			Force,
			// Token: 0x040003A3 RID: 931
			Impulse,
			// Token: 0x040003A4 RID: 932
			VelocityChange,
			// Token: 0x040003A5 RID: 933
			Acceleration
		}
	}
}

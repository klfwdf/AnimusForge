using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class GameEntityPhysicsExtensions
{
	[EngineStruct("rglPhysics_engine_body::Force_mode", false, null)]
	public enum ForceMode : sbyte
	{
		Force,
		Impulse,
		VelocityChange,
		Acceleration
	}

	public static bool HasBody(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasBody(gameEntity.Pointer);
	}

	public static bool HasBody(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasBody(gameEntity.Pointer);
	}

	public static void AddSphereAsBody(this GameEntity gameEntity, Vec3 sphere, float radius, BodyFlags bodyFlags)
	{
		EngineApplicationInterface.IGameEntity.AddSphereAsBody(gameEntity.Pointer, sphere, radius, (uint)bodyFlags);
	}

	public static void AddCapsuleAsBody(this GameEntity gameEntity, Vec3 p1, Vec3 p2, float radius, BodyFlags bodyFlags, string physicsMaterialName = "")
	{
		EngineApplicationInterface.IGameEntity.AddCapsuleAsBody(gameEntity.Pointer, p1, p2, radius, (uint)bodyFlags, physicsMaterialName);
	}

	public static void UpdateBodyRestOffset(this WeakGameEntity gameEntity, float restOffset)
	{
		EngineApplicationInterface.IGameEntity.UpdateBodyRestOffset(gameEntity.Pointer, restOffset);
	}

	public static void PushCapsuleShapeToEntityBody(this WeakGameEntity gameEntity, Vec3 p1, Vec3 p2, float radius, string physicsMaterialName)
	{
		EngineApplicationInterface.IGameEntity.PushCapsuleShapeToEntityBody(gameEntity.Pointer, p1, p2, radius, physicsMaterialName);
	}

	public static void AddSphereAsBody(this WeakGameEntity gameEntity, Vec3 sphere, float radius, BodyFlags bodyFlags)
	{
		EngineApplicationInterface.IGameEntity.AddSphereAsBody(gameEntity.Pointer, sphere, radius, (uint)bodyFlags);
	}

	public static void AddCapsuleAsBody(this WeakGameEntity gameEntity, Vec3 p1, Vec3 p2, float radius, BodyFlags bodyFlags, string physicsMaterialName = "")
	{
		EngineApplicationInterface.IGameEntity.AddCapsuleAsBody(gameEntity.Pointer, p1, p2, radius, (uint)bodyFlags, physicsMaterialName);
	}

	public static void PopCapsuleShapeFromEntityBody(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.PopCapsuleShapeFromEntityBody(gameEntity.Pointer);
	}

	public static void RemovePhysics(this GameEntity gameEntity, bool clearingTheScene = false)
	{
		EngineApplicationInterface.IGameEntity.RemovePhysics(gameEntity.Pointer, clearingTheScene);
	}

	public static void RemovePhysics(this WeakGameEntity gameEntity, bool clearingTheScene = false)
	{
		EngineApplicationInterface.IGameEntity.RemovePhysics(gameEntity.Pointer, clearingTheScene);
	}

	public static bool GetPhysicsState(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetPhysicsState(gameEntity.Pointer);
	}

	public static bool GetPhysicsState(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetPhysicsState(gameEntity.Pointer);
	}

	public static int GetPhysicsTriangleCount(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetPhysicsTriangleCount(gameEntity.Pointer);
	}

	public static int GetPhysicsTriangleCount(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetPhysicsTriangleCount(gameEntity.Pointer);
	}

	public static bool HasPhysicsDefinitionWithoutFlags(this GameEntity gameEntity, int excludeFlags)
	{
		return EngineApplicationInterface.IGameEntity.HasPhysicsDefinition(gameEntity.Pointer, excludeFlags);
	}

	public static bool HasPhysicsDefinitionWithoutFlags(this WeakGameEntity gameEntity, int excludeFlags)
	{
		return EngineApplicationInterface.IGameEntity.HasPhysicsDefinition(gameEntity.Pointer, excludeFlags);
	}

	public static bool HasPhysicsBody(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasPhysicsBody(gameEntity.Pointer);
	}

	public static bool HasPhysicsBody(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasPhysicsBody(gameEntity.Pointer);
	}

	public static bool HasDynamicRigidBody(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasDynamicRigidBody(gameEntity.Pointer);
	}

	public static bool HasDynamicRigidBody(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasDynamicRigidBody(gameEntity.Pointer);
	}

	public static bool HasKinematicRigidBody(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasKinematicRigidBody(gameEntity.Pointer);
	}

	public static bool HasKinematicRigidBody(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasKinematicRigidBody(gameEntity.Pointer);
	}

	public static bool HasStaticPhysicsBody(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasStaticPhysicsBody(gameEntity.Pointer);
	}

	public static bool HasStaticPhysicsBody(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasStaticPhysicsBody(gameEntity.Pointer);
	}

	public static bool HasDynamicRigidBodyAndActiveSimulation(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasDynamicRigidBodyAndActiveSimulation(gameEntity.Pointer);
	}

	public static bool HasDynamicRigidBodyAndActiveSimulation(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.HasDynamicRigidBodyAndActiveSimulation(gameEntity.Pointer);
	}

	public static void CreateVariableRatePhysics(this GameEntity gameEntity, bool forChildren)
	{
		EngineApplicationInterface.IGameEntity.CreateVariableRatePhysics(gameEntity.Pointer, forChildren);
	}

	public static void CreateVariableRatePhysics(this WeakGameEntity gameEntity, bool forChildren)
	{
		EngineApplicationInterface.IGameEntity.CreateVariableRatePhysics(gameEntity.Pointer, forChildren);
	}

	public static void SetPhysicsState(this GameEntity gameEntity, bool isEnabled, bool setChildren)
	{
		EngineApplicationInterface.IGameEntity.SetPhysicsState(gameEntity.Pointer, isEnabled, setChildren);
	}

	public static void SetPhysicsState(this WeakGameEntity gameEntity, bool isEnabled, bool setChildren)
	{
		EngineApplicationInterface.IGameEntity.SetPhysicsState(gameEntity.Pointer, isEnabled, setChildren);
	}

	public static void SetPhysicsStateOnlyVariable(this GameEntity gameEntity, bool isEnabled, bool setChildren)
	{
		EngineApplicationInterface.IGameEntity.SetPhysicsStateOnlyVariable(gameEntity.Pointer, isEnabled, setChildren);
	}

	public static void SetPhysicsStateOnlyVariable(this WeakGameEntity gameEntity, bool isEnabled, bool setChildren)
	{
		EngineApplicationInterface.IGameEntity.SetPhysicsStateOnlyVariable(gameEntity.Pointer, isEnabled, setChildren);
	}

	public static void RemoveEnginePhysics(this GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.RemoveEnginePhysics(gameEntity.Pointer);
	}

	public static void RemoveEnginePhysics(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.RemoveEnginePhysics(gameEntity.Pointer);
	}

	public static bool IsEngineBodySleeping(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsEngineBodySleeping(gameEntity.Pointer);
	}

	public static bool IsEngineBodySleeping(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsEngineBodySleeping(gameEntity.Pointer);
	}

	public static bool IsDynamicBodyStationary(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
	}

	public static bool IsDynamicBodyStationary(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
	}

	public static bool IsDynamicBodyStationaryMT(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
	}

	public static bool IsDynamicBodyStationaryMT(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsDynamicBodyStationary(gameEntity.Pointer);
	}

	public static void ReplacePhysicsBodyWithQuadPhysicsBody(this GameEntity gameEntity, UIntPtr vertices, int numberOfVertices, PhysicsMaterial physicsMaterial, BodyFlags bodyFlags, UIntPtr indices, int numberOfIndices)
	{
		EngineApplicationInterface.IGameEntity.ReplacePhysicsBodyWithQuadPhysicsBody(gameEntity.Pointer, vertices, physicsMaterial.Index, bodyFlags, numberOfVertices, indices, numberOfIndices);
	}

	public static void ReplacePhysicsBodyWithQuadPhysicsBody(this WeakGameEntity gameEntity, UIntPtr vertices, int numberOfVertices, PhysicsMaterial physicsMaterial, BodyFlags bodyFlags, UIntPtr indices, int numberOfIndices)
	{
		EngineApplicationInterface.IGameEntity.ReplacePhysicsBodyWithQuadPhysicsBody(gameEntity.Pointer, vertices, physicsMaterial.Index, bodyFlags, numberOfVertices, indices, numberOfIndices);
	}

	public static PhysicsShape GetBodyShape(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetBodyShape(gameEntity.Pointer);
	}

	public static PhysicsShape GetBodyShape(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetBodyShape(gameEntity.Pointer);
	}

	public static void SetBodyShape(this GameEntity gameEntity, PhysicsShape shape)
	{
		EngineApplicationInterface.IGameEntity.SetBodyShape(gameEntity.Pointer, (shape == null) ? ((UIntPtr)0uL) : shape.Pointer);
	}

	public static void SetBodyShape(this WeakGameEntity gameEntity, PhysicsShape shape)
	{
		EngineApplicationInterface.IGameEntity.SetBodyShape(gameEntity.Pointer, (shape == null) ? ((UIntPtr)0uL) : shape.Pointer);
	}

	public static void AddPhysics(this GameEntity gameEntity, float mass, Vec3 localCenterOfMass, PhysicsShape body, Vec3 initialGlobalVelocity, Vec3 angularGlobalVelocity, PhysicsMaterial physicsMaterial, bool isStatic, int collisionGroupID)
	{
		EngineApplicationInterface.IGameEntity.AddPhysics(gameEntity.Pointer, (body != null) ? body.Pointer : UIntPtr.Zero, mass, ref localCenterOfMass, ref initialGlobalVelocity, ref angularGlobalVelocity, physicsMaterial.Index, isStatic, collisionGroupID);
	}

	public static void AddPhysics(this WeakGameEntity gameEntity, float mass, Vec3 localCenterOfMass, PhysicsShape body, Vec3 initialVelocity, Vec3 angularVelocity, PhysicsMaterial physicsMaterial, bool isStatic, int collisionGroupID)
	{
		EngineApplicationInterface.IGameEntity.AddPhysics(gameEntity.Pointer, (body != null) ? body.Pointer : UIntPtr.Zero, mass, ref localCenterOfMass, ref initialVelocity, ref angularVelocity, physicsMaterial.Index, isStatic, collisionGroupID);
	}

	public static void SetVelocityLimits(this GameEntity gameEntity, float maxLinearVelocity, float maxAngularVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetVelocityLimits(gameEntity.Pointer, maxLinearVelocity, maxAngularVelocity);
	}

	public static void SetVelocityLimits(this WeakGameEntity gameEntity, float maxLinearVelocity, float maxAngularVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetVelocityLimits(gameEntity.Pointer, maxLinearVelocity, maxAngularVelocity);
	}

	public static void SetMaxDepenetrationVelocity(this GameEntity gameEntity, float maxDepenetrationVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetMaxDepenetrationVelocity(gameEntity.Pointer, maxDepenetrationVelocity);
	}

	public static void SetMaxDepenetrationVelocity(this WeakGameEntity gameEntity, float maxDepenetrationVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetMaxDepenetrationVelocity(gameEntity.Pointer, maxDepenetrationVelocity);
	}

	public static void SetSolverIterationCounts(this GameEntity gameEntity, int positionIterationCount, int velocityIterationCount)
	{
		EngineApplicationInterface.IGameEntity.SetSolverIterationCounts(gameEntity.Pointer, positionIterationCount, velocityIterationCount);
	}

	public static void SetSolverIterationCounts(this WeakGameEntity gameEntity, int positionIterationCount, int velocityIterationCount)
	{
		EngineApplicationInterface.IGameEntity.SetSolverIterationCounts(gameEntity.Pointer, positionIterationCount, velocityIterationCount);
	}

	public static void ApplyLocalImpulseToDynamicBody(this GameEntity gameEntity, Vec3 localPosition, Vec3 impulse)
	{
		EngineApplicationInterface.IGameEntity.ApplyLocalImpulseToDynamicBody(gameEntity.Pointer, ref localPosition, ref impulse);
	}

	public static void ApplyLocalImpulseToDynamicBody(this WeakGameEntity gameEntity, Vec3 localPosition, Vec3 impulse)
	{
		EngineApplicationInterface.IGameEntity.ApplyLocalImpulseToDynamicBody(gameEntity.Pointer, ref localPosition, ref impulse);
	}

	public static void ApplyForceToDynamicBody(this GameEntity gameEntity, Vec3 force, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyForceToDynamicBody(gameEntity.Pointer, ref force, forceMode);
	}

	public static void ApplyForceToDynamicBody(this WeakGameEntity gameEntity, Vec3 force, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyForceToDynamicBody(gameEntity.Pointer, ref force, forceMode);
	}

	public static void ApplyGlobalForceAtLocalPosToDynamicBody(this GameEntity gameEntity, Vec3 localPosition, Vec3 globalForce, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref globalForce, forceMode);
	}

	public static void ApplyGlobalForceAtLocalPosToDynamicBody(this WeakGameEntity gameEntity, Vec3 localPosition, Vec3 globalForce, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref globalForce, forceMode);
	}

	public static void ApplyTorqueToDynamicBody(this GameEntity gameEntity, Vec3 torque, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyTorqueToDynamicBody(gameEntity.Pointer, ref torque, forceMode);
	}

	public static void ApplyTorqueToDynamicBody(this WeakGameEntity gameEntity, Vec3 torque, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyTorqueToDynamicBody(gameEntity.Pointer, ref torque, forceMode);
	}

	public static void ApplyLocalForceAtLocalPosToDynamicBody(this GameEntity gameEntity, Vec3 localPosition, Vec3 localForce, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyLocalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref localForce, forceMode);
	}

	public static void ApplyLocalForceAtLocalPosToDynamicBody(this WeakGameEntity gameEntity, Vec3 localPosition, Vec3 localForce, ForceMode forceMode)
	{
		EngineApplicationInterface.IGameEntity.ApplyLocalForceAtLocalPosToDynamicBody(gameEntity.Pointer, ref localPosition, ref localForce, forceMode);
	}

	public static void ApplyAccelerationToDynamicBody(this GameEntity gameEntity, Vec3 acceleration)
	{
		EngineApplicationInterface.IGameEntity.ApplyAccelerationToDynamicBody(gameEntity.Pointer, ref acceleration);
	}

	public static void ApplyAccelerationToDynamicBody(this WeakGameEntity gameEntity, Vec3 acceleration)
	{
		EngineApplicationInterface.IGameEntity.ApplyAccelerationToDynamicBody(gameEntity.Pointer, ref acceleration);
	}

	public static void DisableDynamicBodySimulation(this GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
	}

	public static void DisableDynamicBodySimulation(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
	}

	public static void DisableDynamicBodySimulationMT(this GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
	}

	public static void DisableDynamicBodySimulationMT(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.DisableDynamicBodySimulation(gameEntity.Pointer);
	}

	public static void ConvertDynamicBodyToRayCast(this GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.ConvertDynamicBodyToRayCast(gameEntity.Pointer);
	}

	public static void ConvertDynamicBodyToRayCast(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.ConvertDynamicBodyToRayCast(gameEntity.Pointer);
	}

	public static void SetPhysicsMoveToBatched(this GameEntity gameEntity, bool value)
	{
		EngineApplicationInterface.IGameEntity.SetPhysicsMoveToBatched(gameEntity.Pointer, value);
	}

	public static void SetPhysicsMoveToBatched(this WeakGameEntity gameEntity, bool value)
	{
		EngineApplicationInterface.IGameEntity.SetPhysicsMoveToBatched(gameEntity.Pointer, value);
	}

	public static void EnableDynamicBody(this GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.EnableDynamicBody(gameEntity.Pointer);
	}

	public static void EnableDynamicBody(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.EnableDynamicBody(gameEntity.Pointer);
	}

	public static float GetMass(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetMass(gameEntity.Pointer);
	}

	public static float GetMass(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetMass(gameEntity.Pointer);
	}

	public static void SetMassAndUpdateInertiaAndCenterOfMass(this GameEntity gameEntity, float mass)
	{
		EngineApplicationInterface.IGameEntity.SetMassAndUpdateInertiaAndCenterOfMass(gameEntity.Pointer, mass);
	}

	public static void SetMassAndUpdateInertiaAndCenterOfMass(this WeakGameEntity gameEntity, float mass)
	{
		EngineApplicationInterface.IGameEntity.SetMassAndUpdateInertiaAndCenterOfMass(gameEntity.Pointer, mass);
	}

	public static void SetCenterOfMass(this GameEntity gameEntity, Vec3 localCenterOfMass)
	{
		EngineApplicationInterface.IGameEntity.SetCenterOfMass(gameEntity.Pointer, ref localCenterOfMass);
	}

	public static void SetCenterOfMass(this WeakGameEntity gameEntity, Vec3 centerOfMass)
	{
		EngineApplicationInterface.IGameEntity.SetCenterOfMass(gameEntity.Pointer, ref centerOfMass);
	}

	public static Vec3 GetMassSpaceInertia(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetMassSpaceInertia(gameEntity.Pointer);
	}

	public static Vec3 GetMassSpaceInertia(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetMassSpaceInertia(gameEntity.Pointer);
	}

	public static Vec3 GetMassSpaceInverseInertia(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetMassSpaceInverseInertia(gameEntity.Pointer);
	}

	public static Vec3 GetMassSpaceInverseInertia(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetMassSpaceInverseInertia(gameEntity.Pointer);
	}

	public static void SetMassSpaceInertia(this GameEntity gameEntity, Vec3 inertia)
	{
		EngineApplicationInterface.IGameEntity.SetMassSpaceInertia(gameEntity.Pointer, ref inertia);
	}

	public static void SetMassSpaceInertia(this WeakGameEntity gameEntity, Vec3 inertia)
	{
		EngineApplicationInterface.IGameEntity.SetMassSpaceInertia(gameEntity.Pointer, ref inertia);
	}

	public static void SetDamping(this GameEntity gameEntity, float linearDamping, float angularDamping)
	{
		EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
	}

	public static void SetDamping(this WeakGameEntity gameEntity, float linearDamping, float angularDamping)
	{
		EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
	}

	public static void SetDampingMT(this GameEntity gameEntity, float linearDamping, float angularDamping)
	{
		EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
	}

	public static void SetDampingMT(this WeakGameEntity gameEntity, float linearDamping, float angularDamping)
	{
		EngineApplicationInterface.IGameEntity.SetDamping(gameEntity.Pointer, linearDamping, angularDamping);
	}

	public static void DisableGravity(this GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.DisableGravity(gameEntity.Pointer);
	}

	public static void DisableGravity(this WeakGameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.DisableGravity(gameEntity.Pointer);
	}

	public static bool IsGravityDisabled(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsGravityDisabled(gameEntity.Pointer);
	}

	public static bool IsGravityDisabled(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.IsGravityDisabled(gameEntity.Pointer);
	}

	public static Vec3 GetLinearVelocity(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
	}

	public static Vec3 GetLinearVelocity(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
	}

	public static void SetLinearVelocity(this GameEntity gameEntity, Vec3 newLinearVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetLinearVelocity(gameEntity.Pointer, newLinearVelocity);
	}

	public static void SetLinearVelocity(this WeakGameEntity gameEntity, Vec3 newLinearVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetLinearVelocity(gameEntity.Pointer, newLinearVelocity);
	}

	public static Vec3 GetLinearVelocityMT(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
	}

	public static Vec3 GetLinearVelocityMT(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetLinearVelocity(gameEntity.Pointer);
	}

	public static Vec3 GetAngularVelocity(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
	}

	public static Vec3 GetAngularVelocity(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
	}

	public static Vec3 GetAngularVelocityMT(this GameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
	}

	public static Vec3 GetAngularVelocityMT(this WeakGameEntity gameEntity)
	{
		return EngineApplicationInterface.IGameEntity.GetAngularVelocity(gameEntity.Pointer);
	}

	public static void SetAngularVelocity(this GameEntity gameEntity, Vec3 newAngularVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetAngularVelocity(gameEntity.Pointer, in newAngularVelocity);
	}

	public static void SetAngularVelocity(this WeakGameEntity gameEntity, Vec3 newAngularVelocity)
	{
		EngineApplicationInterface.IGameEntity.SetAngularVelocity(gameEntity.Pointer, in newAngularVelocity);
	}

	public static void GetPhysicsMinMax(this GameEntity gameEntity, bool includeChildren, out Vec3 bbmin, out Vec3 bbmax, bool returnLocal)
	{
		bbmin = Vec3.Zero;
		bbmax = Vec3.Zero;
		EngineApplicationInterface.IGameEntity.GetPhysicsMinMax(gameEntity.Pointer, includeChildren, ref bbmin, ref bbmax, returnLocal);
	}

	public static void GetPhysicsMinMax(this WeakGameEntity gameEntity, bool includeChildren, out Vec3 bbmin, out Vec3 bbmax, bool returnLocal)
	{
		bbmin = Vec3.Zero;
		bbmax = Vec3.Zero;
		EngineApplicationInterface.IGameEntity.GetPhysicsMinMax(gameEntity.Pointer, includeChildren, ref bbmin, ref bbmax, returnLocal);
	}

	public static BoundingBox GetLocalPhysicsBoundingBox(this GameEntity gameEntity, bool includeChildren)
	{
		EngineApplicationInterface.IGameEntity.GetLocalPhysicsBoundingBox(gameEntity.Pointer, includeChildren, out var outBoundingBox);
		return outBoundingBox;
	}

	public static BoundingBox GetLocalPhysicsBoundingBox(this WeakGameEntity gameEntity, bool includeChildren)
	{
		EngineApplicationInterface.IGameEntity.GetLocalPhysicsBoundingBox(gameEntity.Pointer, includeChildren, out var outBoundingBox);
		return outBoundingBox;
	}

	public static Vec3 GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(this WeakGameEntity entity, Vec3 globalPoint)
	{
		Vec3 vb = globalPoint - entity.GetBodyWorldTransform().TransformToParent(entity.CenterOfMass);
		Vec3 vec = Vec3.CrossProduct(entity.GetAngularVelocity(), vb);
		return entity.GetLinearVelocity() + vec;
	}

	public static Vec3 GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(this GameEntity entity, Vec3 globalPoint)
	{
		Vec3 vb = globalPoint - entity.GetBodyWorldTransform().TransformToParent(entity.CenterOfMass);
		Vec3 vec = Vec3.CrossProduct(entity.GetAngularVelocity(), vb);
		return entity.GetLinearVelocity() + vec;
	}

	public static void ComputeVelocityDeltaFromImpulse(this WeakGameEntity gameEntity, in Vec3 impulseGlobal, in Vec3 impulsiveTorqueGlobal, out Vec3 deltaGlobalLinearVelocity, out Vec3 deltaGlobalAngularVelocity)
	{
		EngineApplicationInterface.IGameEntity.ComputeVelocityDeltaFromImpulse(gameEntity.Pointer, in impulseGlobal, in impulsiveTorqueGlobal, out deltaGlobalLinearVelocity, out deltaGlobalAngularVelocity);
	}
}

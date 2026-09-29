using System;
using RichExecutions.Core;

namespace RichExecutions.Scene;

/// <summary>
/// Creates the isolated execution-method strategy for a method id. Each
/// strategy owns its own flow, death chain and after-death upkeep; the shared
/// host (TownExecutionMissionBehavior) only provides stage and agents.
/// </summary>
internal static class ExecutionMethodStrategyFactory
{
    public static IExecutionMethodStrategy Create(string methodId)
    {
        if (string.Equals(methodId, ExecutionMethodRules.Hanging, StringComparison.OrdinalIgnoreCase))
        {
            return new HangingExecutionStrategy();
        }

        if (string.Equals(methodId, ExecutionMethodRules.Burning, StringComparison.OrdinalIgnoreCase))
        {
            return new BurningExecutionStrategy();
        }

        if (string.Equals(methodId, ExecutionMethodRules.Beheading, StringComparison.OrdinalIgnoreCase))
        {
            return new BeheadingExecutionStrategy();
        }

        if (string.Equals(methodId, ExecutionMethodRules.BreakingWheel, StringComparison.OrdinalIgnoreCase))
        {
            return new BreakingWheelExecutionStrategy();
        }

        if (string.Equals(methodId, ExecutionMethodRules.Impalement, StringComparison.OrdinalIgnoreCase))
        {
            return new ImpalementExecutionStrategy();
        }

        if (string.Equals(methodId, ExecutionMethodRules.Stoning, StringComparison.OrdinalIgnoreCase))
        {
            return new StoningExecutionStrategy();
        }

        if (string.Equals(
                methodId,
                ExecutionMethodRules.CrossbowExecution,
                StringComparison.OrdinalIgnoreCase))
        {
            return new CrossbowExecutionStrategy();
        }

        throw new ArgumentException(
            $"No execution-method strategy exists for '{methodId}'.",
            nameof(methodId));
    }
}

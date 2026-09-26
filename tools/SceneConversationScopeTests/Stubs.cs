namespace TaleWorlds.Core
{
    public class BasicCharacterObject
    {
        public string StringId { get; set; } = string.Empty;

        public bool IsHero { get; set; }
    }
}

namespace TaleWorlds.Library
{
    public readonly struct Vec3
    {
        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float DistanceSquared(Vec3 other)
        {
            float x = X - other.X;
            float y = Y - other.Y;
            float z = Z - other.Z;
            return (x * x) + (y * y) + (z * z);
        }
    }
}

namespace TaleWorlds.MountAndBlade
{
    using TaleWorlds.Core;
    using TaleWorlds.Library;

    public enum AgentState
    {
        None = 0,
        Active = 1
    }

    public sealed class Mission
    {
    }

    public sealed class AgentOrigin
    {
        public BasicCharacterObject Troop { get; set; }
    }

    public sealed class Agent
    {
        public int Index { get; set; }

        public BasicCharacterObject Character { get; set; }

        public AgentOrigin Origin { get; set; }

        public Vec3 Position { get; set; }

        public Mission Mission { get; set; }

        public bool IsHuman { get; set; } = true;

        public AgentState State { get; set; } = AgentState.Active;

        public float Health { get; set; } = 100f;

        public bool Active { get; set; } = true;

        public bool IsActive() => Active;
    }
}

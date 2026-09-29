// Managed lifecycle/geometry fixtures. Harmony and the native renderer are not exercised.
using System;
using System.Reflection;
namespace HarmonyLib
{
    public class Harmony { public void Patch(MethodInfo method, HarmonyMethod postfix) { } }
    public class HarmonyMethod { public HarmonyMethod(Type type, string method) { } }
    public static class AccessTools
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static Type TypeByName(string name) => Type.GetType(name);
        public static FieldInfo Field(Type type, string name) => type?.GetField(name, Flags);
        public static MethodInfo Method(Type type, string name, Type[] args) => type.GetMethod(name, Flags, null, args, null);
    }
}
namespace TaleWorlds.Library
{
    public record struct Vec3(float x, float y, float z)
    {
        public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.x+b.x, a.y+b.y, a.z+b.z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.x-b.x, a.y-b.y, a.z-b.z);
        public static Vec3 operator *(Vec3 a, float b) => new(a.x*b, a.y*b, a.z*b);
    }
    public struct Mat3 { public Vec3 s, f, u; }
    public struct MatrixFrame { public Vec3 origin; public Mat3 rotation; }
}
namespace TaleWorlds.GauntletUI.BaseTypes
{
    public class TextureWidget
    {
        public object TextureProvider;
        private void SetTextureProviderProperties() { }
    }
}
namespace TaleWorlds.MountAndBlade.View
{
    public class AgentVisuals
    {
        public float Scale = 1f;
        public TaleWorlds.Library.Vec3 Eye;
        public int Samples;
        public float GetScale() => Scale;
        public TaleWorlds.Library.Vec3 GetGlobalStableEyePoint(bool human) { Samples++; return Eye; }
    }
}
namespace TaleWorlds.MountAndBlade.View.Tableaus
{
    public class CharacterTableau
    {
        public TaleWorlds.Library.MatrixFrame _camPos;
        public AgentVisuals _agentVisuals;
        public int Refreshes;
        public void SetStanceIndex(int stance) { Refreshes++; }
        private void AdjustCharacterForStanceIndex() { }
    }
}
namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders
{
    public class CharacterTableauTextureProvider
    {
        public TaleWorlds.MountAndBlade.View.Tableaus.CharacterTableau _characterTableau = new();
    }
}

using System;

namespace TaleWorlds.SaveSystem;

[AttributeUsage(AttributeTargets.Method)]
public class LateLoadInitializationCallback : Attribute
{
}

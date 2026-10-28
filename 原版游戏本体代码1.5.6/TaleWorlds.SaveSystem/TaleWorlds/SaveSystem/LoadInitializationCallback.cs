using System;

namespace TaleWorlds.SaveSystem;

[AttributeUsage(AttributeTargets.Method)]
public class LoadInitializationCallback : Attribute
{
}

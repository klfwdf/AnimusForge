using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class AspMvcPartialViewAttribute : PathReferenceAttribute
{
}

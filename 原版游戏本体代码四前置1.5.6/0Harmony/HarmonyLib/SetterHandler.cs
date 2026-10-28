using System;
using System.ComponentModel;

namespace HarmonyLib;

[Obsolete("Use AccessTools.FieldRefAccess<T, S> for fields and AccessTools.MethodDelegate<Action<T, S>> for property setters")]
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate void SetterHandler<in T, in S>(T source, S value);

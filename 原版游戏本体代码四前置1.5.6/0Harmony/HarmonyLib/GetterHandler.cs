using System;
using System.ComponentModel;

namespace HarmonyLib;

[Obsolete("Use AccessTools.FieldRefAccess<T, S> for fields and AccessTools.MethodDelegate<Func<T, S>> for property getters")]
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate S GetterHandler<in T, out S>(T source);

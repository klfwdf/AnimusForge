using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Threading;

[assembly: AssemblyVersion("1.1.2.0")]
[assembly: CLSCompliant(true)]
[assembly: AssemblyCompany("0x0ade, DaNike")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyCopyright("Copyright 2024 0x0ade, DaNike")]
[assembly: AssemblyDescription("A set of backports of new BCL features to all frameworks which MonoMod supports.")]
[assembly: AssemblyFileVersion("1.1.2.0")]
[assembly: AssemblyInformationalVersion("1.1.2+a1b82852b")]
[assembly: AssemblyProduct("MonoMod.Backports")]
[assembly: AssemblyTitle("MonoMod.Backports")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/MonoMod/MonoMod.git")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
// mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
[assembly: TypeForwardedTo(typeof(CallerFilePathAttribute))]
[assembly: TypeForwardedTo(typeof(CallerLineNumberAttribute))]
[assembly: TypeForwardedTo(typeof(CallerMemberNameAttribute))]
[assembly: TypeForwardedTo(typeof(ConcurrentDictionary<, >))]
[assembly: TypeForwardedTo(typeof(ConcurrentQueue<>))]
[assembly: TypeForwardedTo(typeof(ConcurrentStack<>))]
[assembly: TypeForwardedTo(typeof(ConditionalWeakTable<, >))]
[assembly: TypeForwardedTo(typeof(ConditionalWeakTable<, >.CreateValueCallback))]
[assembly: TypeForwardedTo(typeof(DefaultDllImportSearchPathsAttribute))]
[assembly: TypeForwardedTo(typeof(DllImportSearchPath))]
[assembly: TypeForwardedTo(typeof(EnumerablePartitionerOptions))]
[assembly: TypeForwardedTo(typeof(IntrospectionExtensions))]
[assembly: TypeForwardedTo(typeof(IProducerConsumerCollection<>))]
[assembly: TypeForwardedTo(typeof(IReadOnlyCollection<>))]
[assembly: TypeForwardedTo(typeof(IReadOnlyList<>))]
[assembly: TypeForwardedTo(typeof(IReflectableType))]
[assembly: TypeForwardedTo(typeof(IStructuralComparable))]
[assembly: TypeForwardedTo(typeof(IStructuralEquatable))]
[assembly: TypeForwardedTo(typeof(OrderablePartitioner<>))]
[assembly: TypeForwardedTo(typeof(Partitioner))]
[assembly: TypeForwardedTo(typeof(Partitioner<>))]
[assembly: TypeForwardedTo(typeof(SpinLock))]
[assembly: TypeForwardedTo(typeof(SpinWait))]
[assembly: TypeForwardedTo(typeof(SpinLock.SystemThreading_SpinLockDebugView))]
[assembly: TypeForwardedTo(typeof(ThreadLocal<>))]
[assembly: TypeForwardedTo(typeof(Tuple))]
[assembly: TypeForwardedTo(typeof(Tuple<>))]
[assembly: TypeForwardedTo(typeof(Tuple<, >))]
[assembly: TypeForwardedTo(typeof(Tuple<, , >))]
[assembly: TypeForwardedTo(typeof(Tuple<, , , >))]
[assembly: TypeForwardedTo(typeof(Tuple<, , , , >))]
[assembly: TypeForwardedTo(typeof(Tuple<, , , , , >))]
[assembly: TypeForwardedTo(typeof(Tuple<, , , , , , >))]
[assembly: TypeForwardedTo(typeof(Tuple<, , , , , , , >))]
[assembly: TypeForwardedTo(typeof(TypeDelegator))]
[assembly: TypeForwardedTo(typeof(TypeInfo))]
[assembly: TypeForwardedTo(typeof(Volatile))]
[assembly: TypeForwardedTo(typeof(WeakReference<>))]
// System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
[assembly: TypeForwardedTo(typeof(ConcurrentBag<>))]
[assembly: TypeForwardedTo(typeof(ConcurrentBag<>.ListOperation))]
[assembly: TypeForwardedTo(typeof(ConcurrentBag<>.Node))]
[assembly: TypeForwardedTo(typeof(ConcurrentBag<>.ThreadLocalList))]
// System.ValueTuple, Version=4.0.3.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
[assembly: TypeForwardedTo(typeof(TupleElementNamesAttribute))]
[assembly: TypeForwardedTo(typeof(ValueTuple))]
[assembly: TypeForwardedTo(typeof(ValueTuple<>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, >))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, , >))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, , , >))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, , , , >))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, , , , , >))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, , , , , , >))]
[assembly: TypeForwardedTo(typeof(ValueTuple<, , , , , , , >))]
[module: RefSafetyRules(11)]

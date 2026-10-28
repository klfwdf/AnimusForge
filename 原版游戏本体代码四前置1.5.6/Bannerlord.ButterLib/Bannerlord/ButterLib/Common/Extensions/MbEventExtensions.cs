using HarmonyLib.BUTR.Extensions;
using TaleWorlds.CampaignSystem;

namespace Bannerlord.ButterLib.Common.Extensions;

public static class MbEventExtensions
{
	internal static class MbEvent1InvokeHandler<T1>
	{
		private delegate void InvokeDelegate(MbEvent<T1> instance, T1 t1);

		private static InvokeDelegate? Delegate { get; } = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredDelegate<InvokeDelegate>("TaleWorlds.CampaignSystem.MbEvent`1:Invoke");

		public static void Invoke(MbEvent<T1> instance, T1 t1)
		{
			Delegate?.Invoke(instance, t1);
		}
	}

	internal static class MbEvent2InvokeHandler<T1, T2>
	{
		private delegate void InvokeDelegate(MbEvent<T1, T2> instance, T1 t1, T2 t2);

		private static InvokeDelegate? Delegate { get; } = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredDelegate<InvokeDelegate>("TaleWorlds.CampaignSystem.MbEvent`2:Invoke");

		public static void Invoke(MbEvent<T1, T2> instance, T1 t1, T2 t2)
		{
			Delegate?.Invoke(instance, t1, t2);
		}
	}

	internal static class MbEvent3InvokeHandler<T1, T2, T3>
	{
		private delegate void InvokeDelegate(MbEvent<T1, T2, T3> instance, T1 t1, T2 t2, T3 t3);

		private static readonly InvokeDelegate? Delegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredDelegate<InvokeDelegate>("TaleWorlds.CampaignSystem.MbEvent`3:Invoke");

		public static void Invoke(MbEvent<T1, T2, T3> instance, T1 t1, T2 t2, T3 t3)
		{
			Delegate?.Invoke(instance, t1, t2, t3);
		}
	}

	internal static class MbEvent4InvokeHandler<T1, T2, T3, T4>
	{
		private delegate void InvokeDelegate(MbEvent<T1, T2, T3, T4> instance, T1 t1, T2 t2, T3 t3, T4 t4);

		private static readonly InvokeDelegate? Delegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredDelegate<InvokeDelegate>("TaleWorlds.CampaignSystem.MbEvent`4:Invoke");

		public static void Invoke(MbEvent<T1, T2, T3, T4> instance, T1 t1, T2 t2, T3 t3, T4 t4)
		{
			Delegate?.Invoke(instance, t1, t2, t3, t4);
		}
	}

	internal static class MbEvent5InvokeHandler<T1, T2, T3, T4, T5>
	{
		private delegate void InvokeDelegate(MbEvent<T1, T2, T3, T4, T5> instance, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);

		private static readonly InvokeDelegate? Delegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredDelegate<InvokeDelegate>("TaleWorlds.CampaignSystem.MbEvent`5:Invoke");

		public static void Invoke(MbEvent<T1, T2, T3, T4, T5> instance, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			Delegate?.Invoke(instance, t1, t2, t3, t4, t5);
		}
	}

	internal static class MbEvent6InvokeHandler<T1, T2, T3, T4, T5, T6>
	{
		private delegate void InvokeDelegate(MbEvent<T1, T2, T3, T4, T5, T6> instance, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);

		private static readonly InvokeDelegate? Delegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredDelegate<InvokeDelegate>("TaleWorlds.CampaignSystem.MbEvent`6:Invoke");

		public static void Invoke(MbEvent<T1, T2, T3, T4, T5, T6> instance, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			Delegate?.Invoke(instance, t1, t2, t3, t4, t5, t6);
		}
	}

	private static void Invoke<T1>(this MbEvent<T1> instance, T1 arg1)
	{
		MbEvent1InvokeHandler<T1>.Invoke(instance, arg1);
	}

	public static void Invoke<T1, T2>(this MbEvent<T1, T2> instance, T1 arg1, T2 arg2)
	{
		MbEvent2InvokeHandler<T1, T2>.Invoke(instance, arg1, arg2);
	}

	public static void Invoke<T1, T2, T3>(this MbEvent<T1, T2, T3> instance, T1 arg1, T2 arg2, T3 arg3)
	{
		MbEvent3InvokeHandler<T1, T2, T3>.Invoke(instance, arg1, arg2, arg3);
	}

	public static void Invoke<T1, T2, T3, T4>(this MbEvent<T1, T2, T3, T4> instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		MbEvent4InvokeHandler<T1, T2, T3, T4>.Invoke(instance, arg1, arg2, arg3, arg4);
	}

	public static void Invoke<T1, T2, T3, T4, T5>(this MbEvent<T1, T2, T3, T4, T5> instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		MbEvent5InvokeHandler<T1, T2, T3, T4, T5>.Invoke(instance, arg1, arg2, arg3, arg4, arg5);
	}

	public static void Invoke<T1, T2, T3, T4, T5, T6>(this MbEvent<T1, T2, T3, T4, T5, T6> instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
	{
		MbEvent6InvokeHandler<T1, T2, T3, T4, T5, T6>.Invoke(instance, arg1, arg2, arg3, arg4, arg5, arg6);
	}
}

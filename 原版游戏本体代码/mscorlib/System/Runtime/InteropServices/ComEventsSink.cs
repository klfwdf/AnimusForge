using System;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009AF RID: 2479
	[SecurityCritical]
	internal class ComEventsSink : NativeMethods.IDispatch, ICustomQueryInterface
	{
		// Token: 0x06006323 RID: 25379 RVA: 0x0015201A File Offset: 0x0015021A
		internal ComEventsSink(object rcw, Guid iid)
		{
			this._iidSourceItf = iid;
			this.Advise(rcw);
		}

		// Token: 0x06006324 RID: 25380 RVA: 0x00152030 File Offset: 0x00150230
		internal static ComEventsSink Find(ComEventsSink sinks, ref Guid iid)
		{
			ComEventsSink comEventsSink = sinks;
			while (comEventsSink != null && comEventsSink._iidSourceItf != iid)
			{
				comEventsSink = comEventsSink._next;
			}
			return comEventsSink;
		}

		// Token: 0x06006325 RID: 25381 RVA: 0x0015205F File Offset: 0x0015025F
		internal static ComEventsSink Add(ComEventsSink sinks, ComEventsSink sink)
		{
			sink._next = sinks;
			return sink;
		}

		// Token: 0x06006326 RID: 25382 RVA: 0x00152069 File Offset: 0x00150269
		[SecurityCritical]
		internal static ComEventsSink RemoveAll(ComEventsSink sinks)
		{
			while (sinks != null)
			{
				sinks.Unadvise();
				sinks = sinks._next;
			}
			return null;
		}

		// Token: 0x06006327 RID: 25383 RVA: 0x00152080 File Offset: 0x00150280
		[SecurityCritical]
		internal static ComEventsSink Remove(ComEventsSink sinks, ComEventsSink sink)
		{
			if (sink == sinks)
			{
				sinks = sinks._next;
			}
			else
			{
				ComEventsSink comEventsSink = sinks;
				while (comEventsSink != null && comEventsSink._next != sink)
				{
					comEventsSink = comEventsSink._next;
				}
				if (comEventsSink != null)
				{
					comEventsSink._next = sink._next;
				}
			}
			sink.Unadvise();
			return sinks;
		}

		// Token: 0x06006328 RID: 25384 RVA: 0x001520C8 File Offset: 0x001502C8
		public ComEventsMethod RemoveMethod(ComEventsMethod method)
		{
			this._methods = ComEventsMethod.Remove(this._methods, method);
			return this._methods;
		}

		// Token: 0x06006329 RID: 25385 RVA: 0x001520E2 File Offset: 0x001502E2
		public ComEventsMethod FindMethod(int dispid)
		{
			return ComEventsMethod.Find(this._methods, dispid);
		}

		// Token: 0x0600632A RID: 25386 RVA: 0x001520F0 File Offset: 0x001502F0
		public ComEventsMethod AddMethod(int dispid)
		{
			ComEventsMethod comEventsMethod = new ComEventsMethod(dispid);
			this._methods = ComEventsMethod.Add(this._methods, comEventsMethod);
			return comEventsMethod;
		}

		// Token: 0x0600632B RID: 25387 RVA: 0x00152117 File Offset: 0x00150317
		[SecurityCritical]
		void NativeMethods.IDispatch.GetTypeInfoCount(out uint pctinfo)
		{
			pctinfo = 0U;
		}

		// Token: 0x0600632C RID: 25388 RVA: 0x0015211C File Offset: 0x0015031C
		[SecurityCritical]
		void NativeMethods.IDispatch.GetTypeInfo(uint iTInfo, int lcid, out IntPtr info)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600632D RID: 25389 RVA: 0x00152123 File Offset: 0x00150323
		[SecurityCritical]
		void NativeMethods.IDispatch.GetIDsOfNames(ref Guid iid, string[] names, uint cNames, int lcid, int[] rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600632E RID: 25390 RVA: 0x0015212C File Offset: 0x0015032C
		private unsafe static Variant* GetVariant(Variant* pSrc)
		{
			if (pSrc->VariantType == (VarEnum)16396)
			{
				Variant* ptr = (Variant*)(void*)pSrc->AsByRefVariant;
				if ((ptr->VariantType & (VarEnum)20479) == (VarEnum)16396)
				{
					return ptr;
				}
			}
			return pSrc;
		}

		// Token: 0x0600632F RID: 25391 RVA: 0x00152168 File Offset: 0x00150368
		[SecurityCritical]
		unsafe void NativeMethods.IDispatch.Invoke(int dispid, ref Guid riid, int lcid, INVOKEKIND wFlags, ref DISPPARAMS pDispParams, IntPtr pvarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			ComEventsMethod comEventsMethod = this.FindMethod(dispid);
			if (comEventsMethod == null)
			{
				return;
			}
			object[] array = new object[pDispParams.cArgs];
			int[] array2 = new int[pDispParams.cArgs];
			bool[] array3 = new bool[pDispParams.cArgs];
			Variant* ptr = (Variant*)(void*)pDispParams.rgvarg;
			int* ptr2 = (int*)(void*)pDispParams.rgdispidNamedArgs;
			int i;
			int num;
			for (i = 0; i < pDispParams.cNamedArgs; i++)
			{
				num = ptr2[i];
				Variant* variant = ComEventsSink.GetVariant(ptr + i);
				array[num] = variant->ToObject();
				array3[num] = true;
				if (variant->IsByRef)
				{
					array2[num] = i;
				}
				else
				{
					array2[num] = -1;
				}
			}
			num = 0;
			while (i < pDispParams.cArgs)
			{
				while (array3[num])
				{
					num++;
				}
				Variant* variant2 = ComEventsSink.GetVariant(ptr + (pDispParams.cArgs - 1 - i));
				array[num] = variant2->ToObject();
				if (variant2->IsByRef)
				{
					array2[num] = pDispParams.cArgs - 1 - i;
				}
				else
				{
					array2[num] = -1;
				}
				num++;
				i++;
			}
			object obj = comEventsMethod.Invoke(array);
			if (pvarResult != IntPtr.Zero)
			{
				Marshal.GetNativeVariantForObject(obj, pvarResult);
			}
			for (i = 0; i < pDispParams.cArgs; i++)
			{
				int num2 = array2[i];
				if (num2 != -1)
				{
					ComEventsSink.GetVariant(ptr + num2)->CopyFromIndirect(array[i]);
				}
			}
		}

		// Token: 0x06006330 RID: 25392 RVA: 0x001522F0 File Offset: 0x001504F0
		[SecurityCritical]
		CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
		{
			ppv = IntPtr.Zero;
			if (iid == this._iidSourceItf || iid == typeof(NativeMethods.IDispatch).GUID)
			{
				ppv = Marshal.GetComInterfaceForObject(this, typeof(NativeMethods.IDispatch), CustomQueryInterfaceMode.Ignore);
				return CustomQueryInterfaceResult.Handled;
			}
			if (iid == ComEventsSink.IID_IManagedObject)
			{
				return CustomQueryInterfaceResult.Failed;
			}
			return CustomQueryInterfaceResult.NotHandled;
		}

		// Token: 0x06006331 RID: 25393 RVA: 0x00152360 File Offset: 0x00150560
		private void Advise(object rcw)
		{
			IConnectionPointContainer connectionPointContainer = (IConnectionPointContainer)rcw;
			IConnectionPoint connectionPoint;
			connectionPointContainer.FindConnectionPoint(ref this._iidSourceItf, out connectionPoint);
			connectionPoint.Advise(this, out this._cookie);
			this._connectionPoint = connectionPoint;
		}

		// Token: 0x06006332 RID: 25394 RVA: 0x00152398 File Offset: 0x00150598
		[SecurityCritical]
		private void Unadvise()
		{
			try
			{
				this._connectionPoint.Unadvise(this._cookie);
				Marshal.ReleaseComObject(this._connectionPoint);
			}
			catch (Exception)
			{
			}
			finally
			{
				this._connectionPoint = null;
			}
		}

		// Token: 0x04002CBB RID: 11451
		private Guid _iidSourceItf;

		// Token: 0x04002CBC RID: 11452
		private IConnectionPoint _connectionPoint;

		// Token: 0x04002CBD RID: 11453
		private int _cookie;

		// Token: 0x04002CBE RID: 11454
		private ComEventsMethod _methods;

		// Token: 0x04002CBF RID: 11455
		private ComEventsSink _next;

		// Token: 0x04002CC0 RID: 11456
		private const VarEnum VT_BYREF_VARIANT = (VarEnum)16396;

		// Token: 0x04002CC1 RID: 11457
		private const VarEnum VT_TYPEMASK = (VarEnum)4095;

		// Token: 0x04002CC2 RID: 11458
		private const VarEnum VT_BYREF_TYPEMASK = (VarEnum)20479;

		// Token: 0x04002CC3 RID: 11459
		private static Guid IID_IManagedObject = new Guid("{C3FCC19E-A970-11D2-8B5A-00A0C9B7C9C4}");
	}
}

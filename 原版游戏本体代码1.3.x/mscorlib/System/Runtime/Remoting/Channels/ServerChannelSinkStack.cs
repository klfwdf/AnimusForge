using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000832 RID: 2098
	[SecurityCritical]
	[ComVisible(true)]
	public class ServerChannelSinkStack : IServerChannelSinkStack, IServerResponseChannelSinkStack
	{
		// Token: 0x060059B3 RID: 22963 RVA: 0x0013C2B0 File Offset: 0x0013A4B0
		[SecurityCritical]
		public void Push(IServerChannelSink sink, object state)
		{
			this._stack = new ServerChannelSinkStack.SinkStack
			{
				PrevStack = this._stack,
				Sink = sink,
				State = state
			};
		}

		// Token: 0x060059B4 RID: 22964 RVA: 0x0013C2E4 File Offset: 0x0013A4E4
		[SecurityCritical]
		public object Pop(IServerChannelSink sink)
		{
			if (this._stack == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_PopOnEmptySinkStack"));
			}
			while (this._stack.Sink != sink)
			{
				this._stack = this._stack.PrevStack;
				if (this._stack == null)
				{
					break;
				}
			}
			if (this._stack.Sink == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_PopFromSinkStackWithoutPush"));
			}
			object state = this._stack.State;
			this._stack = this._stack.PrevStack;
			return state;
		}

		// Token: 0x060059B5 RID: 22965 RVA: 0x0013C36C File Offset: 0x0013A56C
		[SecurityCritical]
		public void Store(IServerChannelSink sink, object state)
		{
			if (this._stack == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_StoreOnEmptySinkStack"));
			}
			while (this._stack.Sink != sink)
			{
				this._stack = this._stack.PrevStack;
				if (this._stack == null)
				{
					break;
				}
			}
			if (this._stack.Sink == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_StoreOnSinkStackWithoutPush"));
			}
			this._rememberedStack = new ServerChannelSinkStack.SinkStack
			{
				PrevStack = this._rememberedStack,
				Sink = sink,
				State = state
			};
			this.Pop(sink);
		}

		// Token: 0x060059B6 RID: 22966 RVA: 0x0013C404 File Offset: 0x0013A604
		[SecurityCritical]
		public void StoreAndDispatch(IServerChannelSink sink, object state)
		{
			this.Store(sink, state);
			this.FlipRememberedStack();
			CrossContextChannel.DoAsyncDispatch(this._asyncMsg, null);
		}

		// Token: 0x060059B7 RID: 22967 RVA: 0x0013C424 File Offset: 0x0013A624
		private void FlipRememberedStack()
		{
			if (this._stack != null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_CantCallFRSWhenStackEmtpy"));
			}
			while (this._rememberedStack != null)
			{
				this._stack = new ServerChannelSinkStack.SinkStack
				{
					PrevStack = this._stack,
					Sink = this._rememberedStack.Sink,
					State = this._rememberedStack.State
				};
				this._rememberedStack = this._rememberedStack.PrevStack;
			}
		}

		// Token: 0x060059B8 RID: 22968 RVA: 0x0013C4A0 File Offset: 0x0013A6A0
		[SecurityCritical]
		public void AsyncProcessResponse(IMessage msg, ITransportHeaders headers, Stream stream)
		{
			if (this._stack == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_CantCallAPRWhenStackEmpty"));
			}
			IServerChannelSink sink = this._stack.Sink;
			object state = this._stack.State;
			this._stack = this._stack.PrevStack;
			sink.AsyncProcessResponse(this, state, msg, headers, stream);
		}

		// Token: 0x060059B9 RID: 22969 RVA: 0x0013C4FC File Offset: 0x0013A6FC
		[SecurityCritical]
		public Stream GetResponseStream(IMessage msg, ITransportHeaders headers)
		{
			if (this._stack == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_CantCallGetResponseStreamWhenStackEmpty"));
			}
			IServerChannelSink sink = this._stack.Sink;
			object state = this._stack.State;
			this._stack = this._stack.PrevStack;
			Stream responseStream = sink.GetResponseStream(this, state, msg, headers);
			this.Push(sink, state);
			return responseStream;
		}

		// Token: 0x17000EDE RID: 3806
		// (set) Token: 0x060059BA RID: 22970 RVA: 0x0013C55E File Offset: 0x0013A75E
		internal object ServerObject
		{
			set
			{
				this._serverObject = value;
			}
		}

		// Token: 0x060059BB RID: 22971 RVA: 0x0013C568 File Offset: 0x0013A768
		[SecurityCritical]
		public void ServerCallback(IAsyncResult ar)
		{
			if (this._asyncEnd != null)
			{
				RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this._asyncEnd);
				MethodInfo mi = (MethodInfo)this._msg.MethodBase;
				RemotingMethodCachedData reflectionCachedData2 = InternalRemotingServices.GetReflectionCachedData(mi);
				ParameterInfo[] parameters = reflectionCachedData.Parameters;
				object[] array = new object[parameters.Length];
				array[parameters.Length - 1] = ar;
				object[] args = this._msg.Args;
				AsyncMessageHelper.GetOutArgs(reflectionCachedData2.Parameters, args, array);
				StackBuilderSink stackBuilderSink = new StackBuilderSink(this._serverObject);
				object[] array2;
				object ret = stackBuilderSink.PrivateProcessMessage(this._asyncEnd.MethodHandle, Message.CoerceArgs(this._asyncEnd, array, parameters), this._serverObject, out array2);
				if (array2 != null)
				{
					array2 = ArgMapper.ExpandAsyncEndArgsToSyncArgs(reflectionCachedData2, array2);
				}
				stackBuilderSink.CopyNonByrefOutArgsFromOriginalArgs(reflectionCachedData2, args, ref array2);
				IMessage msg = new ReturnMessage(ret, array2, this._msg.ArgCount, Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext, this._msg);
				this.AsyncProcessResponse(msg, null, null);
			}
		}

		// Token: 0x040028D6 RID: 10454
		private ServerChannelSinkStack.SinkStack _stack;

		// Token: 0x040028D7 RID: 10455
		private ServerChannelSinkStack.SinkStack _rememberedStack;

		// Token: 0x040028D8 RID: 10456
		private IMessage _asyncMsg;

		// Token: 0x040028D9 RID: 10457
		private MethodInfo _asyncEnd;

		// Token: 0x040028DA RID: 10458
		private object _serverObject;

		// Token: 0x040028DB RID: 10459
		private IMethodCallMessage _msg;

		// Token: 0x02000C78 RID: 3192
		private class SinkStack
		{
			// Token: 0x04003806 RID: 14342
			public ServerChannelSinkStack.SinkStack PrevStack;

			// Token: 0x04003807 RID: 14343
			public IServerChannelSink Sink;

			// Token: 0x04003808 RID: 14344
			public object State;
		}
	}
}

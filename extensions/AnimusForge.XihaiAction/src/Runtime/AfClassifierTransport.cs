using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.XihaiAction
{
    internal interface IAfClassifierTransport : IDisposable
    {
        Task<string> SendAsync(
            List<object> messages,
            int outputTokenLimit,
            CancellationToken cancellationToken);
    }

    internal sealed class AfV130CallApiTransport : IAfClassifierTransport
    {
        private readonly MethodInfo _callApiWithMessages;

        public AfV130CallApiTransport(MethodInfo callApiWithMessages)
        {
            _callApiWithMessages = callApiWithMessages ??
                                   throw new ArgumentNullException(nameof(callApiWithMessages));
        }

        public async Task<string> SendAsync(
            List<object> messages,
            int outputTokenLimit,
            CancellationToken cancellationToken)
        {
            object rawTask;
            try
            {
                rawTask = _callApiWithMessages.Invoke(null, new object[]
                {
                    messages,
                    outputTokenLimit,
                    false,
                    (int?)outputTokenLimit,
                    true,
                    false,
                    cancellationToken,
                    (float?)0f
                });
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    "AnimusForge classifier invocation failed.",
                    ex.InnerException ?? ex);
            }

            if (!(rawTask is Task<string> providerTask))
            {
                throw new InvalidOperationException(
                    "AnimusForge classifier returned an unexpected task type.");
            }

            string output = await providerTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return output;
        }

        public void Dispose()
        {
            // The reflected AF entrypoint owns its HTTP resources and returns a Task.
            // A future typed transport may own a client or a channel and can release it here.
        }
    }
}

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using RSocket;

namespace Demo.AspNetCore.RSocket.RSocket.Transports
{
    internal class ConnectionListenerTransport : IRSocketTransport
    {
        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public ConnectionListenerTransport(ConnectionContext connection)
        {
            Input = connection.Transport.Input;
            Output = connection.Transport.Output;
        }

        public Task StartAsync(CancellationToken cancel = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
}

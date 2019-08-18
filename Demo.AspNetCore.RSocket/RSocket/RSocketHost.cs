using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;
using RSocket;
using Demo.AspNetCore.RSocket.RSocket.Internals;
using Demo.AspNetCore.RSocket.RSocket.Transports;

namespace Demo.AspNetCore.RSocket.RSocket
{
    internal class RSocketHost : BackgroundService
    {
        private readonly IConnectionListenerFactory _connectionListenerFactory;
        private readonly ILogger<RSocketHost> _logger;

        private IConnectionListener _connectionListener;

        public RSocketHost(IConnectionListenerFactory connectionListenerFactory, ILogger<RSocketHost> logger)
        {
            _connectionListenerFactory = connectionListenerFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connectionListener = await _connectionListenerFactory.BindAsync(new IPEndPoint(IPAddress.Loopback, 6000), stoppingToken);

            while (true)
            {
                ConnectionContext connection = await _connectionListener.AcceptAsync(stoppingToken);

                // AcceptAsync will return null upon disposing the listener
                if (connection == null)
                {
                    break;
                }

                // In an actual server, ensure all accepted connections are disposed prior to completing
                _ = Accept(connection);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _connectionListener.DisposeAsync();
        }

        private async Task Accept(ConnectionContext connection)
        {
            try
            {
                IRSocketTransport rsocketTransport = new ConnectionListenerTransport(connection);

                RSocketServer rsocketServer = new EchoServer(rsocketTransport);
                await rsocketServer.ConnectAsync();

                _logger.LogInformation("Connection {ConnectionId} connected", connection.ConnectionId);

                await connection.ConnectionClosed.WaitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Connection {ConnectionId} threw an exception", connection.ConnectionId);
            }
            finally
            {
                await connection.DisposeAsync();

                _logger.LogInformation("Connection {ConnectionId} disconnected", connection.ConnectionId);
            }
        }
    }
}

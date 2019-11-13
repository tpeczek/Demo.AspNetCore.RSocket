using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;
using RSocket;
using Demo.AspNetCore.RSocket.RSocket.Internals;
using Demo.AspNetCore.RSocket.RSocket.Transports;
using System.Collections.Generic;

namespace Demo.AspNetCore.RSocket.RSocket
{
    internal class RSocketHost : BackgroundService
    {
        private readonly IConnectionListenerFactory _connectionListenerFactory;
        private readonly ConcurrentDictionary<string, (ConnectionContext Context, Task ExecutionTask)> _connections = new ConcurrentDictionary<string, (ConnectionContext, Task)>();
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
                ConnectionContext connectionContext = await _connectionListener.AcceptAsync(stoppingToken);

                // AcceptAsync will return null upon disposing the listener
                if (connectionContext == null)
                {
                    break;
                }

                _connections[connectionContext.ConnectionId] = (connectionContext, Accept(connectionContext));
            }

            List<Task> connectionsExecutionTasks = new List<Task>(_connections.Count);

            foreach (var connection in _connections)
            {
                connectionsExecutionTasks.Add(connection.Value.ExecutionTask);
                connection.Value.Context.Abort();
            }

            await Task.WhenAll(connectionsExecutionTasks);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _connectionListener.DisposeAsync();
        }

        private async Task Accept(ConnectionContext connectionContext)
        {
            try
            {
                await Task.Yield();

                IRSocketTransport rsocketTransport = new ConnectionListenerTransport(connectionContext);

                RSocketServer rsocketServer = new EchoServer(rsocketTransport);
                await rsocketServer.ConnectAsync();

                _logger.LogInformation("Connection {ConnectionId} connected", connectionContext.ConnectionId);

                await connectionContext.ConnectionClosed.WaitAsync();
            }
            catch (ConnectionResetException)
            { }
            catch (ConnectionAbortedException)
            { }
            catch (Exception e)
            {
                _logger.LogError(e, "Connection {ConnectionId} threw an exception", connectionContext.ConnectionId);
            }
            finally
            {
                await connectionContext.DisposeAsync();

                _connections.TryRemove(connectionContext.ConnectionId, out _);

                _logger.LogInformation("Connection {ConnectionId} disconnected", connectionContext.ConnectionId);
            }
        }
    }
}

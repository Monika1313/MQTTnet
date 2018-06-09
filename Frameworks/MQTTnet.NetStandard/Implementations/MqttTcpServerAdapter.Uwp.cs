#if WINDOWS_UWP
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public class MqttTcpServerAdapter : IMqttServerAdapter
    {
        private readonly IMqttNetChildLogger _logger;
        private StreamSocketListener _defaultEndpointSocket;

        public MqttTcpServerAdapter(IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateChildLogger(nameof(MqttTcpServerAdapter));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public async Task StartAsync(IMqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (_defaultEndpointSocket != null) throw new InvalidOperationException("Server is already started.");

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _defaultEndpointSocket = new StreamSocketListener();

                // This also affects the client sockets.
                _defaultEndpointSocket.Control.NoDelay = true;
                _defaultEndpointSocket.Control.KeepAlive = true;
                _defaultEndpointSocket.Control.QualityOfService = SocketQualityOfService.LowLatency;
                _defaultEndpointSocket.ConnectionReceived += AcceptDefaultEndpointConnectionsAsync;
                
                await _defaultEndpointSocket.BindServiceNameAsync(options.DefaultEndpointOptions.Port.ToString(), SocketProtectionLevel.PlainSocket);
                
            }

            if (options.TlsEndpointOptions.IsEnabled)
            {
                throw new NotSupportedException("TLS servers are not supported for UWP apps.");
            }
        }

        public Task StopAsync()
        {
            if (_defaultEndpointSocket != null)
            {
                _defaultEndpointSocket.ConnectionReceived -= AcceptDefaultEndpointConnectionsAsync;
            }

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private void AcceptDefaultEndpointConnectionsAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                var clientAdapter = new MqttChannelAdapter(new MqttTcpChannel(args.Socket), new MqttPacketSerializer(), _logger);
                ClientAccepted?.Invoke(this, new MqttServerAdapterClientAcceptedEventArgs(clientAdapter));
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while accepting connection at default endpoint.");
            }
        }
    }
}
#endif
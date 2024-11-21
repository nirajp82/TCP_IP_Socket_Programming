using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AsyncTCPServerSocket
{
    public class TcpServerListener
    {
        TcpListener? _listener;
        bool keepRunning = false;
        ICollection<TcpClient> _tcpClients = new List<TcpClient>();

        // Method to start listening on the given IP and port
        public async Task StartListeningAsync(IPAddress? ipAddr = null, int port = 23000)
        {
            try
            {
                // Validate IP address
                if (ipAddr == null)
                    ipAddr = IPAddress.Any;

                // Validate port
                if (port < 0 || port > 65535)
                    port = 23000;

                Console.WriteLine($"IP: {ipAddr}, Port: {port}. Starting to listen...");

                _listener = new TcpListener(ipAddr, port);
                _listener.Start();

                keepRunning = true;
                while (keepRunning)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _tcpClients.Add(tcpClient);
                    Console.WriteLine($"TCP client connected successfully: ClientEndPoint: {tcpClient.Client.RemoteEndPoint}, TotalCount:{_tcpClients.Count}");
                    // Start a new task to handle this client
                    var _ = Task.Run(async () => await ReadTcpClientDataAsync(tcpClient));
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Seems like listner was stopped: {ex}");
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            try
            {
                await ReadTcpClientDataAsync(tcpClient);
            }
            catch (Exception ex)
            {
                RemoveTcpClient(tcpClient); // Ensure removal on error
                Console.WriteLine($"Error in handling client: {ex}");
            }
        }

        private async Task ReadTcpClientDataAsync(TcpClient tcpClient)
        {
            NetworkStream? networkStream = null;
            networkStream = tcpClient.GetStream();
            var streamReader = new StreamReader(networkStream);
            var buffer = new char[64];
            while (keepRunning)
            {
                Console.WriteLine($"Server is ready to read data from the client.");

                var bytesReceived = await streamReader.ReadAsync(buffer, 0, buffer.Length);

                if (bytesReceived == 0)
                {
                    RemoveTcpClient(tcpClient);
                    break;
                }
                string? receivedData = new string(buffer);
                await EchoMessageToAllTcpClientAsync(tcpClient, receivedData);
                Console.WriteLine($"Data received, sent by client: {receivedData}");
                Array.Clear(buffer, 0, bytesReceived);
            }

            //Temporary code to invoke stop listner.
            if (!_tcpClients.Any())
            {
                StopListening();
            }
        }

        private void StopListening()
        {
            try
            {
                Console.WriteLine($"Stop Listener and close any connected client:");

                foreach (var tcpClient in _tcpClients)
                {
                    if (tcpClient.Connected)
                        tcpClient.Close();
                }

                if (_listener != null)
                    _listener.Stop();

                _tcpClients.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stop Listening Exception: {ex}");
            }
        }

        private async Task EchoMessageToAllTcpClientAsync(TcpClient tcpClientSender, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    return;

                byte[] buffMessage = Encoding.UTF8.GetBytes(message);
                //Echo message to all the TCPClient except the sender
                foreach (var tcpClient in _tcpClients.Where(c => c != tcpClientSender))
                {
                    NetworkStream networkStream = tcpClient.GetStream();
                    await networkStream.WriteAsync(buffMessage, 0, buffMessage.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }
        }

        private void RemoveTcpClient(TcpClient tcpClient)
        {
            if (_tcpClients.Contains(tcpClient))
            {
                _tcpClients.Remove(tcpClient);
            }
            Console.WriteLine($"Client disconnected: ClientEndPoint: {tcpClient.Client.RemoteEndPoint}, TotalCount:{_tcpClients.Count}");
        }

        private async Task SslReadTcpClientDataAsync(TcpClient tcpClient)
        {
            NetworkStream? networkStream = null;
            try
            {
                networkStream = tcpClient.GetStream();
                var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: true, (sender, cert, chain, sslPolicyErrors) =>
                {
                    Console.WriteLine($"{string.Concat(sslPolicyErrors)}");
                    return true;
                }, null);
                var buffer = new byte[64];
                int bytesReceived = 0;
                while (keepRunning)
                {
                    Console.WriteLine($"Server is ready to read data from the client.");

                    if (networkStream.CanRead && networkStream.DataAvailable)
                    {
                        bytesReceived = await sslStream.ReadAsync(buffer, 0, buffer.Length);
                        //Convert received bytes into a string
                        string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                        if (bytesReceived == 0)
                        {
                            RemoveTcpClient(tcpClient);
                        }
                        await EchoMessageToAllTcpClientAsync(tcpClient, receivedData);
                        Console.WriteLine($"Data received, sent by client: {receivedData}");
                        Array.Clear(buffer, 0, bytesReceived);
                    }
                }
            }
            catch (Exception ex)
            {
                RemoveTcpClient(tcpClient);
                Console.WriteLine($"{ex}");
            }
        }
    }
}
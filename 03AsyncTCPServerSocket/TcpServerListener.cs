using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace AsyncTCPServerSocket
{
    public class ClientConnectedArgs : EventArgs
    {
        public string ConnectedClient { get; set; }

        public ClientConnectedArgs(string connectedClient)
        {
            ConnectedClient = connectedClient;
        }
    }

    public class ClientDataReceivedArgs : EventArgs
    {
        public string ConnectedClient { get; set; }
        public string Data { get; set; }

        public ClientDataReceivedArgs(string connectedClient, string data)
        {
            ConnectedClient = connectedClient;
            Data = data;
        }
    }

    public class TcpServerListener
    {
        // TcpListener listens for incoming TCP connection requests
        TcpListener? _listener;

        // Collection to track active TcpClients
        ICollection<TcpClient> _tcpClients = new List<TcpClient>();

        public event EventHandler<ClientConnectedArgs> ClientConnected;
        public event EventHandler<ClientDataReceivedArgs> ClientDataReceived;

        // Method to start listening for client connections on the given IP address and port
        public async Task StartListeningAsync(IPAddress? ipAddr = null, int port = 23000)
        {
            try
            {
                if (ipAddr == null)
                    ipAddr = IPAddress.Any;

                if (port < 0 || port > 65535)
                    port = 23000;

                Console.WriteLine($"IP: {ipAddr}, Port: {port}. Starting to listen...");

                // Initialize the TcpListener with the specified IP address and port
                _listener = new TcpListener(ipAddr, port);
                _listener.Start();  // Start listening for incoming connections

                // Continuously accept incoming client connections until stopped
                while (true)
                {
                    // Accept a client connection asynchronously
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _tcpClients.Add(tcpClient);  // Add the connected client to the list of active clients

                    OnClientConnected(new ClientConnectedArgs(tcpClient.Client.RemoteEndPoint!.ToString()));

                    // Start a new task to handle the client's data asynchronously
                    var _ = Task.Run(async () => await ReadTcpClientDataAsync(tcpClient));
                }
            }
            catch (SocketException ex)
            {
                // Catch exception if listener stops or cannot start
                Console.WriteLine($"Seems like listener was stopped: {ex.Message}");
            }
        }

        protected virtual void OnClientConnected(ClientConnectedArgs e)
        {
            Console.WriteLine($"TCP client connected successfully: ClientEndPoint: " +
                $"{e.ConnectedClient}, TotalCount:{_tcpClients.Count}");
            ClientConnected?.Invoke(this, e);
        }

        protected virtual void OnClientDataReceived(ClientDataReceivedArgs e)
        {
            Console.WriteLine($"Client {e.ConnectedClient} ReceivedData: {e.Data}");
            ClientDataReceived?.Invoke(this, e);
        }

        // Method to handle client communication asynchronously
        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            try
            {
                // Attempt to read data from the client
                await ReadTcpClientDataAsync(tcpClient);
            }
            catch (Exception ex)
            {
                // In case of error, remove the client from the list and log the exception
                RemoveTcpClient(tcpClient);
                Console.WriteLine($"Error in handling client: {ex}");
            }
        }

        // Method to read data from the client asynchronously
        private async Task ReadTcpClientDataAsync(TcpClient tcpClient)
        {
            NetworkStream? networkStream = null;
            networkStream = tcpClient.GetStream();  // Obtain the NetworkStream for reading data
            var streamReader = new StreamReader(networkStream);  // StreamReader to read incoming data
            var buffer = new char[64];  // Buffer to store received data

            // Continuously read from the client as long as the server is running
            while (true)
            {
                Console.WriteLine($"Server is ready to read data from the client.");

                /*
                 ReadAsync: Use ReadAsync for reading raw, potentially binary data or when you need control 
                            over the number of bytes you read.
                 ReadLineAsync: Use ReadLineAsync when reading structured text data that is separated by lines 
                            (i.e., lines of text that are terminated by a newline character).
                 */
                // Asynchronously read data from the client
                var bytesReceived = await streamReader.ReadAsync(buffer, 0, buffer.Length);

                // If no data is received, the client has disconnected
                if (bytesReceived == 0)
                {
                    RemoveTcpClient(tcpClient);  // Remove the client from the list
                    break;
                }

                // Convert the received buffer to a string
                string? receivedData = new string(buffer);
                // Echo the received message to all other connected clients
                await EchoMessageToAllTcpClientAsync(tcpClient, receivedData);

                OnClientDataReceived(new ClientDataReceivedArgs(tcpClient.Client.RemoteEndPoint!.ToString(), receivedData));

                // Clear the buffer after processing
                Array.Clear(buffer, 0, bytesReceived);
            }

            // Temporary check: stop listening when no clients are connected, This is just to show how to stop the server.
            if (!_tcpClients.Any())
            {
                StopListening();  // Stop the server if there are no connected clients
            }
        }

        // Method to stop listening and disconnect all clients
        private void StopListening()
        {
            try
            {
                Console.WriteLine($"Stop Listener and close any connected client:");

                // Close all active client connections
                foreach (var tcpClient in _tcpClients)
                {
                    if (tcpClient.Connected)
                        tcpClient.Close();
                }

                // Stop the listener and clear the list of clients
                if (_listener != null)
                    _listener.Stop();

                _tcpClients.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stop Listening Exception: {ex}");
            }
        }

        // Method to broadcast the received message to all connected clients except the sender
        private async Task EchoMessageToAllTcpClientAsync(TcpClient tcpClientSender, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    return;

                byte[] buffMessage = Encoding.UTF8.GetBytes(message);  // Convert the message to a byte array

                // Loop through all connected clients, excluding the sender
                foreach (var tcpClient in _tcpClients)//.Where(c => c != tcpClientSender))
                {
                    // Get the NetworkStream for the client and send the message
                    NetworkStream networkStream = tcpClient.GetStream();
                    await networkStream.WriteAsync(buffMessage, 0, buffMessage.Length);  // Asynchronously send the message
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions encountered while broadcasting the message
                Console.WriteLine($"{ex}");
            }
        }

        // Method to remove a client from the list of active clients
        private void RemoveTcpClient(TcpClient tcpClient)
        {
            if (_tcpClients.Contains(tcpClient))
            {
                _tcpClients.Remove(tcpClient);  // Remove the client from the list
            }
            Console.WriteLine($"Client disconnected: ClientEndPoint: {tcpClient.Client.RemoteEndPoint}, TotalCount:{_tcpClients.Count}");
        }

        // Asynchronous method to read data from a TCP client over an SSL connection
        private async Task SslReadTcpClientDataAsync(TcpClient tcpClient)
        {
            NetworkStream? networkStream = null;
            try
            {
                // Get the network stream from the TcpClient
                networkStream = tcpClient.GetStream();

                // Create an SSL stream over the network stream for encrypted communication
                var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: true,
                    // Certificate validation callback (accepts all certificates here for simplicity)
                    (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // Log SSL certificate validation errors if any
                        Console.WriteLine($"{string.Concat(sslPolicyErrors)}");
                        return true;  // Accept all certificates, for testing purposes
                    }, null);

                // Buffer to store incoming data (64-byte size for simplicity)
                var buffer = new byte[64];
                int bytesReceived = 0;

                // Keep reading data from the client while the server is running
                while (true)
                {
                    Console.WriteLine($"Server is ready to read data from the client.");

                    // Check if the network stream is readable and has data available
                    if (networkStream.CanRead && networkStream.DataAvailable)
                    {
                        // Asynchronously read data from the SSL stream into the buffer
                        bytesReceived = await sslStream.ReadAsync(buffer, 0, buffer.Length);

                        // Convert the received bytes into a string (ASCII encoding)
                        string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

                        // If no data is received, assume the client has disconnected
                        if (bytesReceived == 0)
                        {
                            RemoveTcpClient(tcpClient);  // Remove the client from the list of active clients
                        }

                        // Echo the received message to all connected clients (except the sender)
                        await EchoMessageToAllTcpClientAsync(tcpClient, receivedData);

                        Console.WriteLine($"Data received, sent by client: {receivedData}");

                        // Clear the buffer after processing the received data
                        Array.Clear(buffer, 0, bytesReceived);
                    }
                }
            }
            catch (Exception ex)
            {
                // In case of any exception, remove the client and log the error
                RemoveTcpClient(tcpClient);
                Console.WriteLine($"{ex}");
            }
        }
    }
}
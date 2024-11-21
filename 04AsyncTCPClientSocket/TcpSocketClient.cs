using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace _04AsyncTCPClientSocket
{
    internal class TcpSocketClient
    {
        private readonly string _ipAddress; // IP address of the server to connect to
        private readonly int _port; // Port number of the server
        private TcpClient _tcpClient; // Instance of TcpClient to handle the connection

        // Constructor initializes the IP address and port number for the connection
        public TcpSocketClient(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        // Asynchronously connects to the server using the provided IP address and port
        public async Task ConnectToServer()
        {
            _tcpClient = new TcpClient(); // Initialize the TcpClient instance
            try
            {
                // Attempt to connect to the server asynchronously
                await _tcpClient.ConnectAsync(_ipAddress, _port);
                Console.WriteLine($"Connected to the server: {_ipAddress}, Port: {_port}");

                // Start a new task to handle receiving data from the server asynchronously
                var _ = Task.Run(async () => await ReadDataFromServerAsync());
            }
            catch (Exception ex)
            {
                // Handle exceptions related to the connection
                Console.WriteLine($"Exception while connecting to TCPServer: {ex}");
            }
        }

        // Asynchronously sends data to the connected server
        public async Task SendDataToServerAsync(string msgData)
        {
            // If the message is null or empty, return without sending anything
            if (string.IsNullOrEmpty(msgData))
            {
                Console.WriteLine("Empty string.");
                return;
            }

            // Check if the TcpClient is connected before attempting to send data
            if (_tcpClient != null && _tcpClient.Connected)
            {
                var nwStream = _tcpClient.GetStream(); // Get the network stream for communication
                StreamWriter clientStreamWriter = new StreamWriter(nwStream); // Create a StreamWriter for sending data
                clientStreamWriter.AutoFlush = true; // Ensure that data is sent immediately without buffering

                // Asynchronously send the message to the server
                await clientStreamWriter.WriteAsync(msgData);
                Console.WriteLine(); // Output a new line for readability after sending
            }
        }

        // Closes the connection to the server and cleans up resources
        internal void CloseAndDisconnect()
        {
            // Check if the TcpClient is connected before closing the connection
            if (_tcpClient != null && _tcpClient.Connected)
            {
                _tcpClient.Close(); // Close the connection to the server
            }
        }

        // Asynchronously reads data from the server
        private async Task ReadDataFromServerAsync()
        {
            try
            {
                // Retrieve the network stream for reading data from the server
                NetworkStream nwStream = _tcpClient.GetStream();
                StreamReader streamReader = new StreamReader(nwStream); // Create a StreamReader for reading data
                char[] buffer = new char[64]; // Buffer to store incoming data

                // Loop continuously to read data as long as the connection is alive
                while (true)
                {
                    var bytesReceived = await streamReader.ReadAsync(buffer, 0, buffer.Length); // Read data into the buffer

                    // If no data is received (i.e., the server has closed the connection), break the loop
                    if (bytesReceived == 0)
                    {
                        Console.WriteLine($"Disconnected from server.");
                        break;
                    }

                    // Output the received message along with the number of bytes received
                    Console.WriteLine("Received bytes: {0} - Message: {1}", bytesReceived, new string(buffer));

                    // Clear the buffer for the next read operation
                    Array.Clear(buffer, 0, bytesReceived);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions related to reading data from the server
                Console.WriteLine($"Read data from the server: {ex}");
            }
        }
    }
}

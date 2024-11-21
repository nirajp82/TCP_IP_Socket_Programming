using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace _04AsyncTCPClientSocket
{
    internal class TcpSocketClient
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private TcpClient _tcpClient;

        public TcpSocketClient(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public async Task ConnectToServer()
        {
            _tcpClient = new TcpClient();
            try
            {
                await _tcpClient.ConnectAsync(_ipAddress, _port);
                Console.WriteLine($"Connected to the server: {_ipAddress}, Port: {_port}");
                // Start a new task to handle the received data asynchronously
                var _ = Task.Run(async () => await ReadDataFromServerAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while connecting to TCPServer: {ex}");
            }
        }

        public async Task SendDataToServerAsync(string msgData)
        {
            if (string.IsNullOrEmpty(msgData))
            {
                Console.WriteLine("Empty string.");
                return;
            }

            if (_tcpClient != null && _tcpClient.Connected)
            {
                var nwStream = _tcpClient.GetStream();
                StreamWriter clientStreamWriter = new StreamWriter(nwStream);
                clientStreamWriter.AutoFlush = true;
                await clientStreamWriter.WriteAsync(msgData);
                Console.WriteLine();
            }
        }

        internal void CloseAndDisconnect()
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                _tcpClient.Close();
            }
        }

        private async Task ReadDataFromServerAsync()
        {
            try
            {
                NetworkStream nwStream = _tcpClient.GetStream();
                StreamReader streamReader = new StreamReader(nwStream);
                char[] buffer = new char[1024];
                while (true)
                {
                    var bytesReceived = await streamReader.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesReceived == 0)
                    {
                        Console.WriteLine($"Disconnected from server.");
                        break;
                    }
                    Console.WriteLine("Received bytes: {0} - Message: {1}", bytesReceived, new string(buffer));
                    Array.Clear(buffer, 0, bytesReceived);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read data from the server: {ex}");
            }
        }
    }
}
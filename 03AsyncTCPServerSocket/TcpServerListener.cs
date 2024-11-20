using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AsyncTCPServerSocket
{
    public class TcpServerListener
    {
        private TcpListener? _listener;
        bool keepRunning = false;

        // Method to start listening on the given IP and port
        public async Task StartListeningAsync(IPAddress? ipAddr = null, int port = 23000)
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
                Console.WriteLine($"TCP client connected successfully: {tcpClient}");
                await ReadTcpClientDataAsync(tcpClient);
            }
        }

        private async Task ReadTcpClientDataAsync(TcpClient tcpClient)
        {
            NetworkStream? networkStream = null;
            SslStream? sslStream = null;
            try
            {
                networkStream = tcpClient.GetStream();
                //sslStream = new SslStream(networkStream, leaveInnerStreamOpen: true, (sender, cert, chain, sslPolicyErrors) =>
                //{
                //    Console.WriteLine($"{string.Concat(sslPolicyErrors)}");
                //    return true;
                //}, null);
                //var buffer = new byte[64];
                var streamReader = new StreamReader(networkStream);
                var buffer = new char[1024];
                var sbData = new StringBuilder();
                while (keepRunning)
                {
                    //if (networkStream.CanRead && networkStream.DataAvailable)
                    //{
                    // bytesReceived = await streamReader.ReadAsync(buffer, 0, buffer.Length);
                    // Convert received bytes into a string
                    // string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                    //}
                    var bytesReceived = await streamReader.ReadAsync(buffer, 0, buffer.Length);
                    string? receivedData = new string(buffer);
                    sbData.Append(receivedData);
                    Console.WriteLine($"Data received, sent by client: {receivedData}");
                    Array.Clear(buffer, 0, bytesReceived);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }
        }
    }
}
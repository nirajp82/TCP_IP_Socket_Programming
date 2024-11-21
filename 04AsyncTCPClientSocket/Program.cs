
// Method to validate and return a valid IP address
using _04AsyncTCPClientSocket;
using System.Net;
using System.Text;

var ipAddress = GetValidIpAddress();
var port = GetValidPort();

var tcpSocketClient = new TcpSocketClient(ipAddress, port);
await tcpSocketClient.ConnectToServer();
string inputCommand = string.Empty;
while (true)
{
    Console.Write($"Type command and press enter to send it to the server. Type exit to the close. >> ");
    inputCommand = Console.ReadLine();
    // If the user types exit, break the loop and close the connection
    if (inputCommand!.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        tcpSocketClient.CloseAndDisconnect();
        break;
    }
    await tcpSocketClient.SendDataToServerAsync(inputCommand); 
}

string GetValidIpAddress()
{
    while (true)
    {
        Console.Write("Enter the IP address of the server: Default 127.0.0.1 >> ");
        string? ipAddress = Console.ReadLine();

        if (IPAddress.TryParse(ipAddress, out _))
        {
            return ipAddress;
        }
        else
        {
            Console.WriteLine($"Invalid IP address. Using local loopback 127.0.0.1 instead.");
            return "127.0.0.1";
        }
    }
}

// Method to validate and return a valid port number
int GetValidPort()
{
    int port;
    while (true)
    {
        Console.Write("Enter the port number: Default 23000 >> ");
        string? portInput = Console.ReadLine();

        if (int.TryParse(portInput, out port) && port >= 1 && port <= 65535)
        {
            return port;
        }
        else
        {
            Console.WriteLine("Invalid port number. Using default Port 23000.");
            return 23000;
        }
    }
}
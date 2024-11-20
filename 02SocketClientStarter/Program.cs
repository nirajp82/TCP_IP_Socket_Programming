using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        // Ask for IP address and port number
        string ipAddress = GetValidIpAddress();
        int port = GetValidPort();

        Socket? socketClient = null;
        try
        {
            // Create a Socket to establish the TCP connection
            // Using AddressFamily.InterNetwork (IPv4), SocketType.Stream (TCP), and ProtocolType.Tcp (TCP)
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Establish a TCP connection to the server at the specified IP and port
            // If successful, a connection is established. If the server is unreachable or the IP/port is incorrect,
            // a SocketException will be thrown.
            socketClient.Connect(ipAddress, port);
            Console.WriteLine($"Connected to {ipAddress} on port {port}.");

            string inputCommand = string.Empty;
            while (true)
            {
                Console.Write($"Type command and press enter to send it to the server. Type exit to the close. >> ");
                
                // Read command from user input
                inputCommand = Console.ReadLine();

                // If the user types exit, break the loop and close the connection
                if (inputCommand!.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // Convert the command string to bytes using ASCII encoding
                byte[] buffCommand = Encoding.ASCII.GetBytes(inputCommand);

                // Send the command to the server
                socketClient.Send(buffCommand);

                // Prepare a buffer to receive the server's response
                byte[] buffReceived = new byte[1024];
                int bytesRead = socketClient.Receive(buffReceived); // Receive data from the server

                // Display the received data
                Console.WriteLine($"Data Received from the server: {Encoding.ASCII.GetString(buffReceived, 0, bytesRead)}");
            }

            Console.WriteLine("\nConnection closed.");
        }
        catch (SocketException ex)
        {
            // Handle connection errors
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (socketClient != null)
            {
                if (socketClient.Connected)
                {
                    socketClient?.Shutdown(SocketShutdown.Both);
                }
                // Close the socket connection once the user exits the loop
                socketClient?.Close();
                socketClient?.Dispose();
            }
        }
    }

    // Method to validate and return a valid IP address
    static string GetValidIpAddress()
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
    static int GetValidPort()
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
}
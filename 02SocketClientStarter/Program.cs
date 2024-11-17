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
            Console.WriteLine($"Connected to {ipAddress} on port {port}. Type command and press enter to send it to the server. Type <EXIT> to the close.");

            string inputCommand = string.Empty;
            while (true) 
            {
                // Read command from user input
                inputCommand = Console.ReadLine();

                // If the user types <EXIT>, break the loop and close the connection
                if (inputCommand.Equals("<EXIT>", StringComparison.OrdinalIgnoreCase))
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
                Console.WriteLine($"Data Received: {Encoding.ASCII.GetString(buffReceived, 0, bytesRead)}");
            }

            // Close the socket connection once the user exits the loop
            socketClient.Close();
            Console.WriteLine("\nConnection closed.");
        }
        catch (SocketException ex)
        {
            // Handle connection errors
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            socketClient?.Shutdown(SocketShutdown.Both);
        }
    }

    // Method to validate and return a valid IP address
    static string GetValidIpAddress()
    {
        while (true)
        {
            Console.Write("Enter the IP address of the server: ");
            string? ipAddress = Console.ReadLine();

            if (IPAddress.TryParse(ipAddress, out _))
            {
                return ipAddress;
            }
            else
            {
                Console.WriteLine("Invalid IP address format. Please enter a valid IP address.");
            }
        }
    }

    // Method to validate and return a valid port number
    static int GetValidPort()
    {
        int port;
        while (true)
        {
            Console.Write("Enter the port number: ");
            string? portInput = Console.ReadLine();

            if (int.TryParse(portInput, out port) && port >= 1 && port <= 65535)
            {
                return port;
            }
            else
            {
                Console.WriteLine("Invalid port number. Please enter a valid port number between 1 and 65535.");
            }
        }
    }
}
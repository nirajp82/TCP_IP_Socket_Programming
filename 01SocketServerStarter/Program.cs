using System.Net;
using System.Net.Sockets;
using System.Text;

// Create a new TCP/IP socket to listen for incoming connections
Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

// Get the local IP address of the machine (here we use IPAddress.Any to listen on all available network interfaces for ex 127.0.0.1, localhost, or real IP Address)
IPAddress localIPAddress = IPAddress.Any;

// Define the endpoint to bind the socket to, using port 23000
IPEndPoint localEndPoint = new IPEndPoint(localIPAddress, 23000);

// Bind the server socket to the endpoint (this is where the server listens for incoming connections)
serverSocket.Bind(localEndPoint);

// Start listening for incoming connection requests. 
// The number 5 in the parentheses represents the backlog size (or the maximum number of pending connections that the server can queue up before it starts refusing new connections)
// If there is a backlog of 5, this means the server can have up to 5 connection requests in the queue waiting to be processed. Once the server begins processing a request (by accepting the connection with Accept()), it frees up space for the next request
serverSocket.Listen(backlog: 5);
Console.WriteLine("Server is listening on port 23000...");

// Accept an incoming connection (this blocks the thread until a connection is made)
Socket clientSocket = serverSocket.Accept();
Console.WriteLine($"Connection established with client. {clientSocket}, IPEndPoint: {clientSocket.RemoteEndPoint}");

byte[] buffer = new byte[1024];

StringBuilder dataBuilder = new StringBuilder();
while (true)
{
    // Read data from the client into the buffer
    int bytesReceived = clientSocket.Receive(buffer);

    // Convert received bytes into a string
    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
    Console.WriteLine($"Received data: {receivedData}");

    // Accumulate the data in the StringBuilder
    dataBuilder.Append(receivedData);

    //After receiving and processing the data, the clientSocket.Send(buffer) sends the exact same data back to the client that was stored in the buffer.
    clientSocket.Send(buffer);

    // If we detect a newline (Telnet sends \r\n), break out of the loop
    if (receivedData.Contains("\n"))
    {
        break;
    }
}
// Display the complete message received from the client
string fullMessage = dataBuilder.ToString().Trim();
Console.WriteLine($"Full message received: {fullMessage}");

// Close the client socket after the communication is done
clientSocket.Close();

// Optionally, you can close the server socket once you are done listening (although in a real-world scenario, the server would likely stay open)
serverSocket.Close();
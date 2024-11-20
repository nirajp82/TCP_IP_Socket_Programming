using AsyncTCPServerSocket;

try
{
    var tcpServerListener = new TcpServerListener();
    await tcpServerListener.StartListeningAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"{ex}");
}
Console.ReadLine();
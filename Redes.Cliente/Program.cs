using System.Net.Sockets;
using System.Text;

using (var client = new TcpClient())
{
    await client.ConnectAsync("localhost", 9000);

    var stream = client.GetStream();

    var listenTask = Task.Run(() => ListenForMessages(stream));
}

static async Task ListenForMessages(NetworkStream stream)
{
    byte[] buffer = new byte[1024];

    try
    {
        while (true)
        {
            // Read data from the server asynchronously
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                // The server has closed the connection
                Console.WriteLine("Server disconnected.");
                break;
            }

            // Decode and display the message
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"\nServer: {message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while listening to server: {ex.Message}");
    }
}
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Redes.Servidor.domain;

namespace Redes.Servidor;

public class ServidorTcpBombParty
{
    private static readonly ConcurrentDictionary<Guid, TcpClient> _jogadores = new();
    private static readonly ConcurrentDictionary<Guid, User> _usuarios = new();

    public async Task Iniciar()
    {
        Console.WriteLine("Carregando...");
        
        var dictionary = new DictionaryService().getDictionary();

        var listener = new TcpListener(IPAddress.Any, 9000);
        listener.Start();
        Console.WriteLine("Ouvindo na porta 9000...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            var clientId = Guid.NewGuid();

            _jogadores.TryAdd(clientId, client);
            Console.WriteLine($"Jogador {clientId} abriu conexão");

            _ = Task.Run(() => EscutarJogador(client, clientId));
        }
    }

    private async Task EscutarJogador(TcpClient client, Guid clientId)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024]; // Que tamanho usar?

        // var user = new User();

        try
        {

            var bd = await stream.ReadAsync(buffer, 0, buffer.Length);
            var name = Encoding.UTF8.GetString(buffer, 0, bd);

            _usuarios.TryAdd(clientId, new User(clientId, name));

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    // Jogador desconectou
                    _jogadores.TryRemove(clientId, out TcpClient clienteRemovido);
                    _usuarios.TryRemove(clientId, out User userRemovido);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Recebido do jogador {clientId}: {message}");

                // TODO fazer alguma coisa com a mensagem

                var response = Encoding.UTF8.GetBytes($"Resposta teste");
                await stream.WriteAsync(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with Client {clientId}: {ex.Message}");
        }
        finally
        {
            client.Close();
            _jogadores.TryRemove(clientId, out _);
            Console.WriteLine($"Jogador {clientId} desconectou");
        }
    }
}

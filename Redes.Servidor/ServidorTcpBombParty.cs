using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Redes.Servidor.domain;
using Redes.Servidor.service;

namespace Redes.Servidor;

public class ServidorTcpBombParty
{
    private readonly ConcurrentDictionary<Guid, TcpClient> _jogadores = new();
    private readonly List<Guid> _ordemJogadores = new();
    private readonly ConcurrentDictionary<Guid, User> _usuarios = new();

    private readonly HashSet<string> _palavras;
    private string _silabaAtual = string.Empty;
    private int _indiceJogadorAtual;

    public ServidorTcpBombParty()
    {
        _palavras = ServicoDePalavras.Carregar();
        ProximaSilaba();
    }

    public async Task Iniciar()
    {
        Console.WriteLine("Carregando...");
        
        var listener = new TcpListener(IPAddress.Any, 9000);
        listener.Start();
        Console.WriteLine("Ouvindo na porta 9000...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            var clientId = Guid.NewGuid();

            _jogadores.TryAdd(clientId, client);
            _ordemJogadores.Add(clientId);
            Console.WriteLine($"Jogador {clientId} abriu conexão");

            _ = Task.Run(() => EscutarJogador(client, clientId));
        }
    }

    private async Task EscutarJogador(TcpClient client, Guid clientId)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024]; // Que tamanho usar?

        try
        {
            var bd = await stream.ReadAsync(buffer);
            var name = Encoding.UTF8.GetString(buffer, 0, bd);

            _usuarios.TryAdd(clientId, new User(clientId, name));

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                var jogadorDesconectou = bytesRead == 0;
                if (jogadorDesconectou)
                {
                    RemoverJogador(clientId);
                    break;
                }

                var mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Recebido do jogador {clientId}: {mensagem}");

                await InterpretarMensagem(mensagem);
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

    private void RemoverJogador(Guid clientId)
    {
        _jogadores.TryRemove(clientId, out _);
        _usuarios.TryRemove(clientId, out _);
        _ordemJogadores.Remove(clientId);
    }

    private async Task  ProximoTurno()
    {
        ProximoJogador();
        ProximaSilaba();
        var mensagemProximoturno = ""; // TODO
        await TransmitirParaTodos(mensagemProximoturno);
    }

    private void ProximaSilaba()
    {
        _silabaAtual = ServicoDeSilabas.ObterSilabaAleatoria(_palavras);
    }

    private void ProximoJogador()
    {
        if (_indiceJogadorAtual == _ordemJogadores.Count - 1)
        {
            _indiceJogadorAtual = 0;
        }
        else
        {
            _indiceJogadorAtual++;
        }
    }

    private bool PalavraValida(string palavra)
    {
        return _palavras.Contains(palavra.ToLower());
    }

    private async Task InterpretarMensagem(string mensagem)
    {
        var tipoMensagem = TipoMensagemRecebida.TestarPalavra; // TODO identificar tipo de mensagem

        switch (tipoMensagem)
        {
            case TipoMensagemRecebida.TestarPalavra:
                await TestarPalavra(mensagem);
                break;
            case TipoMensagemRecebida.Digitar:
                await DigitarPalavra(mensagem);
                break;
        }
    }

    public async Task TestarPalavra(string mensagem)
    {
        var palavra = mensagem; // TODO extrair palavra da mensagem do jogador
        if (PalavraValida(palavra))
        {
            var respostaAcerto = "PALAVRA VALIDA"; // TODO montar mensagem de resposta
            await TransmitirParaTodos(respostaAcerto);
            await ProximoTurno();
        }
        else
        {
            var respostaErro = "PALAVRA INVALIDA"; // TODO montar mensagem de resposta
            await TransmitirParaTodos(respostaErro);
        }
    }

    private async Task DigitarPalavra(string mensagem)
    {
       // TODO enviar para todos os jogadores que a palavra digitada foi alterada
    }

    private async Task TransmitirParaTodos(string mensagem)
    {
        var mensagemRaw = Encoding.UTF8.GetBytes(mensagem);

        foreach (var jogador in _jogadores)
        {
            var stream = jogador.Value.GetStream();
            await stream.WriteAsync(mensagemRaw);
        }
    }
}

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Redes.Servidor.domain;
using Redes.Servidor.service;
using Redes.Common;

namespace Redes.Servidor;

public class ServidorTcpBombParty
{
    private readonly ConcurrentDictionary<Guid, TcpClient> _jogadores = new();
    private readonly List<Guid> _ordemJogadores = new();
    private readonly ConcurrentDictionary<Guid, User> _usuarios = new();

    private readonly HashSet<string> _palavras;
    private string _silabaAtual = string.Empty;
    private int _indiceJogadorAtual = -1;
    private string _textoSendoDigitado = string.Empty;

    public ServidorTcpBombParty()
    {
        _palavras = ServicoDePalavras.Carregar();
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
            await stream.ReadAsync(buffer);
            var mensagemInicial = Mensagem.From(buffer);

            if (mensagemInicial.TipoMensagem != TipoMensagem.EntrarNoJogo)
            {
                RemoverJogador(clientId, client);
                return;
            }

            var name = mensagemInicial.Conteudo ?? clientId.ToString();
            _usuarios.TryAdd(clientId, new User(clientId, name));

            var respostaInicial = new Mensagem(TipoMensagem.RespostaEntrarNoJogo, clientId.ToString());
            await Transmitir(client, respostaInicial);

            var atualizacaoGameState = new Mensagem(TipoMensagem.AtualizarGameState, $"{JogadorAtual} {_silabaAtual} {_textoSendoDigitado}");
            await Transmitir(client, atualizacaoGameState);

            var mensagemNovoJogador = new Mensagem(TipoMensagem.NovoJogadorEntrou, clientId + " " + name);
            await TransmitirParaTodos(mensagemNovoJogador);

            // Inicia o jogo de fato somente quando o segundo jogador entrar
            if (_jogadores.Count == 2)
            {
                await ProximoTurno();
            }

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                var jogadorDesconectou = bytesRead == 0;
                if (jogadorDesconectou)
                {
                    break;
                }

                await InterpretarMensagem(buffer, bytesRead);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with Client {clientId}: {ex.Message}");
        }
        finally
        {
            RemoverJogador(clientId, client);
            Console.WriteLine($"Jogador {clientId} desconectou");
        }
    }

    private void RemoverJogador(Guid clientId, TcpClient client)
    {
        client.Close();
        _jogadores.TryRemove(clientId, out _);
        _usuarios.TryRemove(clientId, out _);
        _ordemJogadores.Remove(clientId);
    }

    private async Task ProximoTurno()
    {
        var jogador = ProximoJogador();
        ProximaSilaba();
        var mensagemProximoturno = new Mensagem(TipoMensagem.ProximoTurno, $"{jogador} {_silabaAtual}");
        await TransmitirParaTodos(mensagemProximoturno);
    }

    private void ProximaSilaba()
    {
        _silabaAtual = ServicoDeSilabas.ObterSilabaAleatoria(_palavras);
    }

    private Guid ProximoJogador()
    {
        if (_indiceJogadorAtual == _ordemJogadores.Count - 1)
        {
            _indiceJogadorAtual = 0;
        }
        else
        {
            _indiceJogadorAtual++;
        }

        return JogadorAtual;
    }

    private Guid JogadorAtual => _indiceJogadorAtual >= 0 ? _ordemJogadores[_indiceJogadorAtual] : Guid.Empty;

    private bool PalavraValida(string palavra)
    {
        palavra = palavra.ToLower().Trim(' ').Trim('\n');
        return palavra.Contains(_silabaAtual) && _palavras.Contains(palavra);
    }

    private async Task InterpretarMensagem(byte[] mensagemRaw, int quantidadeBytes)
    {
        var mensagem = Mensagem.From(mensagemRaw, quantidadeBytes);

        switch (mensagem.TipoMensagem)
        {
            case TipoMensagem.TestarPalavra:
                await TestarPalavra(mensagem);
                break;
            case TipoMensagem.Digitar:
                await DigitarPalavra(mensagem);
                break;
        }
    }

    public async Task TestarPalavra(Mensagem mensagem)
    {
        var palavra = mensagem.Conteudo;
        Console.WriteLine("Testou palavra: " + palavra);
        if (palavra is not null && PalavraValida(palavra))
        {
            var respostaAcerto = new Mensagem(TipoMensagem.PalavraValida, null);
            await TransmitirParaTodos(respostaAcerto);
            await ProximoTurno();
        }
        else
        {
            var respostaErro = new Mensagem(TipoMensagem.PalavraInvalida, null);
            await TransmitirParaTodos(respostaErro);
        }
    }

    private async Task DigitarPalavra(Mensagem mensagem)
    {
        _textoSendoDigitado = mensagem.Conteudo ?? string.Empty;
        var atualizacaoGameState = new Mensagem(TipoMensagem.AtualizarGameState, $"{JogadorAtual} {_silabaAtual} {_textoSendoDigitado}");
        await TransmitirParaTodos(atualizacaoGameState);
    }

    private async Task TransmitirParaTodos(Mensagem mensagem)
    {
        foreach (var jogador in _jogadores)
        {
            await Transmitir(jogador.Value, mensagem);
        }
    }

    private async Task Transmitir(TcpClient jogador, Mensagem mensagem)
    {
        var stream = jogador.GetStream();
        await stream.WriteAsync(mensagem.Raw);
    }
}

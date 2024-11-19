using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using BombParty.Common;

namespace BombParty.Servidor;

public sealed class ServidorTcpBombParty : IDisposable
{
    private readonly ConcurrentDictionary<Guid, TcpClient> _conexoes = new();
    private readonly List<Guid> _ordemJogadores = new();
    private readonly ConcurrentDictionary<Guid, Jogador> _jogadores = new();

    private readonly HashSet<string> _palavras;
    private string _silabaAtual = string.Empty;
    private int _indiceJogadorAtual = -1;
    private string _textoSendoDigitado = string.Empty;

    private Guid _ganhador;

    public ServidorTcpBombParty()
    {
        _palavras = Palavras.Carregar();
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
            Console.WriteLine($"Jogador {clientId} abriu conexão");

            _ = Task.Run(() => EscutarJogador(client, clientId));
        }
    }

    private async Task EscutarJogador(TcpClient client, Guid clientId)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];

        try
        {
            await stream.ReadAsync(buffer);
            var mensagemInicial = Mensagem.From(buffer);

            if (mensagemInicial.TipoMensagem != TipoMensagem.EntrarNoJogo)
            {
                return;
            }

            var name = mensagemInicial.Conteudo ?? clientId.ToString();
            _jogadores.TryAdd(clientId, new Jogador(clientId, name));
            _conexoes.TryAdd(clientId, client);
            _ordemJogadores.Add(clientId);

            var respostaInicial = new Mensagem(TipoMensagem.RespostaEntrarNoJogo, clientId.ToString());
            await Transmitir(client, respostaInicial);

            var atualizacaoGameState = new Mensagem(TipoMensagem.AtualizarGameState, $"{JogadorAtual} {_silabaAtual} {_textoSendoDigitado}");
            await Transmitir(client, atualizacaoGameState);

            var mensagemNovoJogador = new Mensagem(TipoMensagem.NovoJogadorEntrou, clientId + " " + name);
            await TransmitirParaTodos(mensagemNovoJogador);

            // Inicia o jogo de fato somente quando o segundo jogador entrar
            if (_conexoes.Count == 2)
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

                await InterpretarMensagem(buffer, bytesRead, clientId);
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
        _conexoes.TryRemove(clientId, out _);
        _jogadores.TryRemove(clientId, out _);
        _ordemJogadores.Remove(clientId);
    }

    private async Task ProximoTurno()
    {
        var jogador = ProximoJogador();

        if (jogador != _ganhador)
        {
            ProximaSilaba();
            var mensagemProximoturno = new Mensagem(TipoMensagem.ProximoTurno, $"{jogador} {_silabaAtual}");
            await TransmitirParaTodos(mensagemProximoturno);
        }
        else
        {
            var mensagemProximoturno = new Mensagem(TipoMensagem.Ganhou, $"{_jogadores[_ganhador].Nome}");
            await TransmitirParaTodos(mensagemProximoturno);
        }
    }

    private void ProximaSilaba()
    {
        _silabaAtual = Silabas.ObterSilabaAleatoria(_palavras);
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

        _ganhador = ObterGanhador();

        var naoHaGanhador = _ganhador == Guid.Empty;
        if (naoHaGanhador)
        {
            while (_jogadores[JogadorAtual].Perdeu)
            {
                return ProximoJogador();
            }

            return JogadorAtual;
        }

        return _ganhador;
    }

    private Guid JogadorAtual => _indiceJogadorAtual >= 0 ? _ordemJogadores[_indiceJogadorAtual] : Guid.Empty;

    private Guid ObterGanhador()
    {
        var aindaNaoPerderam = _jogadores.Where(u => !u.Value.Perdeu);

        if (aindaNaoPerderam.Count() == 1)
        {
            return aindaNaoPerderam.First().Key;
        }

        return Guid.Empty;
    }

    private bool PalavraValida(string palavra)
    {
        palavra = palavra.ToLower().Trim(' ').Trim('\n');
        return palavra.Contains(_silabaAtual) && _palavras.Contains(palavra);
    }

    private async Task InterpretarMensagem(byte[] mensagemRaw, int quantidadeBytes, Guid clientId)
    {
        if (clientId != JogadorAtual)
        {
            return;
        }

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
            Console.WriteLine(JogadorAtual);
            var jogadorAtual = _jogadores[JogadorAtual];

            jogadorAtual.PerderVida();
            if (jogadorAtual.Perdeu)
            {
                var respostaPerdeu = new Mensagem(TipoMensagem.Perdeu, null);
                await Transmitir(_conexoes[JogadorAtual], respostaPerdeu);
                await ProximoTurno();
            }


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
        foreach (var conexaoJogador in _conexoes)
        {
            await Transmitir(conexaoJogador.Value, mensagem);
        }
    }

    private async Task Transmitir(TcpClient jogador, Mensagem mensagem)
    {
        var stream = jogador.GetStream();
        await stream.WriteAsync(mensagem.Raw);
    }

    public void Dispose()
    {
        foreach (var tcpClient in _conexoes.Values)
        {
            tcpClient.Close();
        }
    }
}

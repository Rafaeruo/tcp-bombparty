using Redes.Common;
using System.Net.Sockets;
using System.Text;

namespace Redes.Cliente
{
    public class ClienteTcpBombParty : IDisposable
    {
        private readonly TcpClient _client = new();
        private bool _conectado;
        private bool _atualizarInterface;

        private Guid _id;
        private string? _nome;
        private Mensagem? _ultimaMensagem;
        private Guid _jogadorAtual;
        private string? _silabaAtual;
        private string _textoSendoDigitado = string.Empty;
        private bool? _respostaTentativa;
        private bool _perdeu = false;
        private bool _ganhou = false;

        public async Task Iniciar(string host, int porta)
        {
            await _client.ConnectAsync(host, porta);
            _conectado = true;

            Console.Write("Nome do jogador: ");
            _nome = Console.ReadLine();

            var mensagemEntrarNoJogo = new Mensagem(TipoMensagem.EntrarNoJogo, _nome);
            var stream = _client.GetStream();
            await stream.WriteAsync(mensagemEntrarNoJogo.Raw);

            _ = Task.Run(EscutarServidor);

            while (_conectado)
            {
                if (!_atualizarInterface)
                {
                    continue;
                }

                AtualizarInterface();

                if (IsJogadorAtual())
                {
                    _textoSendoDigitado = string.Empty;
                    var palavra = new StringBuilder();
                    while(true)
                    {
                        AtualizarInterface();
                        var keyInfo = Console.ReadKey();

                        if (keyInfo.Key == ConsoleKey.Enter)
                        {
                            await Transmitir(new Mensagem(TipoMensagem.TestarPalavra, palavra.ToString()));

                            while(_respostaTentativa is null)
                            {
                            }

                            if (_respostaTentativa.Value)
                            {
                                _atualizarInterface = true;
                                _respostaTentativa = null;
                                _jogadorAtual = Guid.Empty;
                                break;
                            }
                            else
                            {
                                _respostaTentativa = null;
                                continue;
                            }
                        }
                        else if (keyInfo.Key == ConsoleKey.Backspace && palavra.Length >= 1)
                        {
                            palavra.Remove(palavra.Length - 1, 1);
                            _textoSendoDigitado = palavra.ToString();
                            await Transmitir(new Mensagem(TipoMensagem.Digitar, _textoSendoDigitado));
                        }
                        else if (!char.IsControl(keyInfo.KeyChar))
                        {
                            palavra.Append(keyInfo.KeyChar.ToString());
                            _textoSendoDigitado = palavra.ToString();
                            await Transmitir(new Mensagem(TipoMensagem.Digitar, _textoSendoDigitado));
                        }
                    }
                }
            }
        }

        private async Task EscutarServidor()
        {
            var stream = _client.GetStream();
            var buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        _conectado = false;
                        break;
                    }

                    InterpretarMensagem(buffer, bytesRead);

                    _atualizarInterface = true;
                }
            }
            catch (Exception ex)
            {
                _conectado = false;
                Console.WriteLine($"Error while listening to server: {ex.Message}");
            }
        }

        private async Task Transmitir(Mensagem mensagem)
        {
            var stream = _client.GetStream();
            await stream.WriteAsync(mensagem.Raw);
        }

        private void InterpretarMensagem(byte[] buffer, int bytesRead)
        {
            var mensagem = Mensagem.From(buffer, bytesRead);
            _ultimaMensagem = mensagem;

            switch (mensagem.TipoMensagem)
            {
                case TipoMensagem.RespostaEntrarNoJogo:
                    LerRespostaEntrarNoJogo();
                    break;
                case TipoMensagem.AtualizarGameState:
                    LerGameState();
                    break;
                case TipoMensagem.ProximoTurno:
                    ProximoTurno();
                    break;
                case TipoMensagem.PalavraValida:
                    if (IsJogadorAtual())
                    {
                        _respostaTentativa = true;
                    }
                    break;
                case TipoMensagem.PalavraInvalida:
                    if (IsJogadorAtual())
                    {
                        _respostaTentativa = false;
                    }
                    break;
                case TipoMensagem.Perdeu:
                    _perdeu = true;
                    _respostaTentativa = false;
                    break;
                case TipoMensagem.Ganhou:
                    _ganhou = true;
                    AtualizarInterface();
                    Finalizar();
                    break;
            }
        }

        private void LerRespostaEntrarNoJogo()
        {
            _id = Guid.Parse(_ultimaMensagem!.Conteudo!);
            _atualizarInterface = true;
        }

        private void LerGameState()
        {
            var partes = _ultimaMensagem!.Conteudo!.Split(' ');
            _jogadorAtual = Guid.Parse(partes[0]);
            _silabaAtual = partes[1];
            _textoSendoDigitado = partes[2];
        }

        private void ProximoTurno()
        {
            var partes = _ultimaMensagem!.Conteudo!.Split(' ');
            _jogadorAtual = Guid.Parse(partes[0]);
            _silabaAtual = partes[1];
            _textoSendoDigitado = string.Empty;
        }

        private void Finalizar()
        {
            _textoSendoDigitado = _ultimaMensagem.Conteudo != null ? _ultimaMensagem.Conteudo : "" ;
        }

        private void AtualizarInterface()
        {
            var seuTurno = IsJogadorAtual();
            var status = _perdeu
                ? "perdeu"
                : _ganhou
                    ? "ganhou"
                    : seuTurno ? "sim" : "não";

            Console.Clear();

            Console.WriteLine($"Última mensagem: {_ultimaMensagem?.TipoMensagem} | {_ultimaMensagem?.Conteudo}");
            Console.WriteLine($"Você: {_nome} ({_id})");
            Console.WriteLine("Jogador atual: " + _jogadorAtual);
            Console.WriteLine("Sílaba: " + _silabaAtual);
            
            Console.WriteLine($"Seu turno?: {status}");
            Console.WriteLine($"Texto sendo digitado: {_textoSendoDigitado}");

            _atualizarInterface = false;
        }

        private bool IsJogadorAtual()
        {
            return _jogadorAtual == _id && _jogadorAtual != Guid.Empty;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}

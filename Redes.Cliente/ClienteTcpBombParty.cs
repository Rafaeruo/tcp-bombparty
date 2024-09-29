using Redes.Common;
using System.Net.Sockets;

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

                    await InterpretarMensagem(buffer, bytesRead);

                    _atualizarInterface = true;
                }
            }
            catch (Exception ex)
            {
                _conectado = false;
                Console.WriteLine($"Error while listening to server: {ex.Message}");
            }
        }

        private async Task InterpretarMensagem(byte[] buffer, int bytesRead)
        {
            var mensagem = Mensagem.From(buffer, bytesRead);
            _ultimaMensagem = mensagem;

            if (mensagem.TipoMensagem == TipoMensagem.RespostaEntrarNoJogo)
            {
                _id = Guid.Parse(mensagem.Conteudo!);
            }
        }

        private void AtualizarInterface()
        {
            Console.Clear();

            Console.WriteLine($"Última mensagem: {_ultimaMensagem?.TipoMensagem} | {_ultimaMensagem?.Conteudo}");
            Console.WriteLine($"Você: {_nome} ({_id})");
            Console.WriteLine("Jogador atual: " + _jogadorAtual);
            Console.WriteLine("Sílaba: " + _silabaAtual);
            var seuTurno = _jogadorAtual == _id && _jogadorAtual != Guid.Empty;
            Console.WriteLine($"Seu turno?: {(seuTurno ? "sim" : "não")}");

            _atualizarInterface = false;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}

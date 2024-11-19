using System.Text;

namespace BombParty.Common
{
    public class Mensagem
    {
        private static readonly Encoding _encoding = Encoding.UTF8;
        public TipoMensagem TipoMensagem { get; set; }
        public string? Conteudo { get; set; }
        public Memory<byte> Raw => _encoding.GetBytes(ToString());

        public Mensagem(TipoMensagem tipoMensagem, string? conteudo)
        {
            TipoMensagem = tipoMensagem;
            Conteudo = conteudo;
        }

        public override string ToString()
        {
            return (int)TipoMensagem + "\n\n" + Conteudo + "\n\n";
        }

        public static Mensagem From(byte[] mensagemRaw)
        {
            return From(mensagemRaw, mensagemRaw.Length);
        }

        public static Mensagem From(byte[] mensagemRaw, int quantidadeBytes)
        {
            return From(_encoding.GetString(mensagemRaw, 0, quantidadeBytes));
        }

        public static Mensagem From(string mensagemRaw)
        {
            var partes = mensagemRaw.Split("\n\n");
            var tipo = Enum.Parse<TipoMensagem>(partes[0]);
            var conteudo = partes[1];

            return new Mensagem(tipo, conteudo);
        }
    }
}

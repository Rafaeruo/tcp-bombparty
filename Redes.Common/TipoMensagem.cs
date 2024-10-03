namespace Redes.Common
{
    public enum TipoMensagem
    {
        // Cliente -> Servidor
        TestarPalavra,
        Digitar,
        EntrarNoJogo,
        // Servidor -> Cliente
        PalavraValida,
        PalavraInvalida,
        ProximoTurno,
        NovoJogadorEntrou,
        RespostaEntrarNoJogo,
        AtualizarGameState
    }
}

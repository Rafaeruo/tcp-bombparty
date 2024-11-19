namespace BombParty.Servidor
{
    public static class Silabas
    {
        public static string ObterSilabaAleatoria(IEnumerable<string> palavras)
        {
            int tamanhoSilaba = Random.Shared.Next(2, 4);

            var palavrasComTamanhoMinimo = palavras.Where(palavra => palavra.Length >= tamanhoSilaba).ToArray();
            var indicePalavraAleatoria = Random.Shared.Next(0, palavrasComTamanhoMinimo.Length);
            var palavraAleatoria = palavrasComTamanhoMinimo[indicePalavraAleatoria];

            var indiceInicioPalavra = Random.Shared.Next(0, palavraAleatoria.Length - tamanhoSilaba);
            return palavraAleatoria.Substring(indiceInicioPalavra, tamanhoSilaba);
        }
    }
}

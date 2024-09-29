namespace Redes.Servidor.service
{
    public static class ServicoDeSilabas
    {
        public static string ObterSilabaAleatoria(IEnumerable<string> palavras)
        {
            var silabaTamanho3 = Random.Shared.Next(0, 2) == 0;
            int tamanhoSilaba = 2;

            if (silabaTamanho3)
            {
                tamanhoSilaba = 3;
            }

            var palavrasComTamanhoMinimo = palavras.Where(palavra => palavra.Length >= tamanhoSilaba).ToArray();
            var indicePalavraAleatoria = Random.Shared.Next(0, palavrasComTamanhoMinimo.Length);
            var palavraAleatoria = palavrasComTamanhoMinimo[indicePalavraAleatoria];

            var indiceInicioPalavra = Random.Shared.Next(0, palavraAleatoria.Length - tamanhoSilaba);
            return palavraAleatoria.Substring(indiceInicioPalavra, tamanhoSilaba);
        }
    }
}

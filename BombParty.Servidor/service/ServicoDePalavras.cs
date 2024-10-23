namespace BombParty.Servidor.service;

public static class ServicoDePalavras
{
    public static HashSet<string> Carregar()
    {
        const string path = "./palavras.txt";
        var palavras = new HashSet<string>();

        using var sr = new StreamReader(path);
        string? linha;

        while ((linha = sr.ReadLine()) != null)
        {
            palavras.Add(linha.ToLower());
        }

        return palavras;
    }
}
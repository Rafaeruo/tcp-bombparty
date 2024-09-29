using System.IO;

namespace Redes.Servidor;

public static class ServicoDePalavras
{
    public static HashSet<string> Carregar() 
    {
        const string path = "./br-utf8.txt";
        var palavras = new HashSet<string>();

        using var  sr = new StreamReader(path);
        string? linha;

        while ((linha = sr.ReadLine()) != null)
        {
            palavras.Add(linha.ToLower());
        }

        return palavras;
    }
}
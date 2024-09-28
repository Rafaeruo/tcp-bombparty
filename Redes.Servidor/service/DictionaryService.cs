using System.IO;

namespace Redes.Servidor;

public class DictionaryService
{
    public Dictionary<String, String> getDictionary() {
        string path = @"../br-utf8.txt";

        Dictionary<String, String> dic = new Dictionary<String, String>();

        try
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string linha;

                while ((linha = sr.ReadLine()) != null)
                {
                    dic[linha.ToLower()] = linha.ToLower();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ocorreu um erro: {e.Message}");
        }
        return dic;
    }
}
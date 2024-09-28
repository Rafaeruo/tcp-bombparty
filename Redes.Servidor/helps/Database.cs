using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Redes.Servidor.helps;

public class Database
{
    private static readonly Lazy<Database> instance = new Lazy<Database>(() => new Database());
    private ObjectCache cache = MemoryCache.Default;
    private CacheItemPolicy policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(1) };

    private Database() { }

    public static Database Instance => instance.Value;
    
    public void LoadWordsIntoCache(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("O arquivo especificado não foi encontrado.");
        }

        var words = new HashSet<string>(File.ReadAllLines(filePath));
        cache.Set("words", words, policy);
    }

    public bool IsWordInCache(string word)
    {
        if (cache.Contains("words"))
        {
            var words = (HashSet<string>)cache.Get("words");
            return words.Equals(word.ToLower());
        }
        return false;
    }
}

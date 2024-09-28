using Redes.Servidor;
using Redes.Servidor.helps;
using System.Text.Unicode;

Database database = Database.Instance;
 
try
{
    var filePath = "D:\\Projetos\\tcp-bombparty\\Redes.Servidor\\db\\br-utf8.txt";
    database.LoadWordsIntoCache(filePath);
}
catch
{

}

var servidor = new ServidorTcpBombParty();

await servidor.Iniciar();
namespace Redes.Cliente.Client;

public class User(Guid guid, string name)
{
    public Guid guid { get; set; } = guid;
    public string name { get; set; } = name;
    public int life { get; set; } = 3;
}
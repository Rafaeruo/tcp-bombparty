namespace Redes.Servidor.domain;

public class User(Guid guid, string name)
{
    public Guid guid { get; set; } = guid;
    public string name { get; set; } = name;
    public int life { get; set; } = 3;

    public void PerdeuVida() {
        _ = life != 0 ? life-- : 0;
    }

    public bool Perdeu() {
        return life == 0;
    }
}
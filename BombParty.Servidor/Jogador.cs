namespace BombParty.Servidor;

public class Jogador
{
    public Jogador(Guid id, string nome)
    {
        Id = id;
        Nome = nome;
        Vidas = 3;
    }

    public Guid Id { get; set; }
    public string Nome { get; set; }
    public int Vidas { get; set; }
    public bool Perdeu => Vidas == 0;

    public void PerderVida()
    {
        if (Vidas > 0)
        {
            Vidas--;
        }
    }
}
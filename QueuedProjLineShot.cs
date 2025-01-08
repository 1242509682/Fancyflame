using Microsoft.Xna.Framework;
using TShockAPI;
using static Fancyflame.FancyflamePlugin;

namespace Fancyflame;

public struct QueuedProjLineShot
{
    public PlayerModel Owner { get; }
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public Vector2 Velocity { get; set; }
    public int N { get; set; }
    public int Damage { get; set; }
    public float Knockback { get; set; }
    public int Counterdown { get; set; }

    public QueuedProjLineShot(PlayerModel owner)
    {
        Owner = owner;
        Start = Vector2.Zero;
        End = Vector2.Zero;
        Velocity = Vector2.Zero;
        N = 1;
        Damage = 0;
        Knockback = 0f;
        Counterdown = 10;
    }

    public bool Update()
    {
        if (Owner.UUID != TShock.Players[Owner.Index].UUID.GetHashCode())
        {
            return true;
        }
        if (Counterdown > 0)
        {
            Counterdown -= 5;
            return false;
        }
        if (Counterdown <= 0)
        {
            Owner.ProjLine(Start, End, Velocity, N, Config.FlamesProj, Damage, Knockback);
            return true;
        }
        return false;
    }
}


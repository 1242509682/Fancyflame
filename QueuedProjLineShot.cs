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
        if (Config.ItemList == null || Config.ItemList.Count <= 0) return false;

        var ownerUUID = Owner.UUID;
        var ownerIndex = Owner.Index;
        var heldItemType = Owner.TPlayer.HeldItem.type;

        if (ownerUUID != TShock.Players[ownerIndex].UUID.GetHashCode())
        {
            return true;
        }

        var items = Config.ItemList.FirstOrDefault(x => x != null && x.ItemID == heldItemType);
        if (items == null) return false;
        if (Counterdown > 0)
        {
            Counterdown -= items.FrameIntervals;
            return false;
        }
        if (Counterdown <= 0)
        {
            Owner.ProjLine(Start, End, Velocity, N, items.FlamesProj, Damage, Knockback);
            return true;
        }

        return false;
    }
}


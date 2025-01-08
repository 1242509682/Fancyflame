using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using static Fancyflame.FancyflamePlugin;

namespace Fancyflame;

public class PlayerModel
{
    private bool reverse; // 用于在火焰斩释放时切换方向
    public int Index { get; } // 玩家在游戏内的索引
    public int UUID { get; } // 唯一用户标识符（UUID）的哈希值，用于唯一识别玩家
    public Player TPlayer => Main.player[Index]; // 获取与当前PlayerModel关联的Terraria.Player实例
    public bool IsUseItem { get; set; } // 表示玩家是否正在使用物品
    public int FlamesCooldown { get; set; } // 火焰技能的冷却时间

    #region 物品使用角度
    public double ItemUseAngle
    {
        get
        {
            // 从玩家对象获取当前物品旋转角度
            double num = TPlayer.itemRotation;
            if (TPlayer.direction == -1)  // 如果玩家面朝左侧，则调整角度以确保正确的物品使用方向
            {
                num += Math.PI; // 增加π弧度（即180度）来反转角度
            }
            return num;  // 返回最终计算得到的物品使用角度
        }
    }
    #endregion

    #region 构造函数初始化PlayerModel实例，接收玩家索引作为参数
    public PlayerModel(int index)
    {
        Index = index;
        UUID = TShock.Players[index].UUID.GetHashCode();
    }
    #endregion

    #region 更新方法，主要用于减少技能冷却时间和检查是否应释放火焰斩技能。
    public void Update()
    {
        // 如果火焰斩技能处于冷却中，则减少冷却时间
        if (FlamesCooldown > 0)
        {
            FlamesCooldown -= 5;
        }

        // 检查是否满足释放火焰斩技能的条件：持有物品为3507（铜短剑） 不在冷却中 正在使用物品
        if (TPlayer.HeldItem.type == Config.ItemID && FlamesCooldown <= 0 && IsUseItem)
        {
            FlamesCooldown = Config.FlamesCooldown; // 设置火焰斩技能的冷却时间为120帧
            ReleaseFlameSlash(); // 释放火焰斩技能
        }
    }
    #endregion

    #region 当玩家正在使用物品时更新 IsUseItem 标识
    public void OnReceiveUpdate(GetDataHandlers.PlayerUpdateEventArgs args)
    {
        IsUseItem = args.Control.IsUsingItem;
    } 
    #endregion

    #region 释放火焰斩技能的方法
    public void ReleaseFlameSlash()
    {
        // 切换火焰斩的方向
        reverse = !reverse;
        var num = reverse ? 1 : -1;
        // 尝试找到最近的目标距离，默认800f
        var num2 = Math.Min(800f, TryFindNearestTargetDistance().GetValueOrDefault(800f));
        // 计算武器伤害并乘以系数
        var damage = TPlayer.GetWeaponDamage(TPlayer.HeldItem) * 5 / 2;

        // 根据目标距离决定使用远程攻击模式还是近距离攻击模式
        if (num2 > 160f)
        {
            // 远程攻击模式：发射一系列定向的火焰线
            // 计算要发射的火焰线条数，至少5条，最多33条
            var num3 = Math.Max(5, Math.Min(33, (int)Math.Ceiling(num2 / 800f * 33f)));

            // 定义火焰线的角度范围
            var num4 = ItemUseAngle + Math.PI / 3.0 * num; // 最大角度偏移
            var num5 = ItemUseAngle - Math.PI / 3.0 * num; // 最小角度偏移

            // 循环创建并发射每一条火焰线
            for (var i = 0; i < num3; i++)
            {
                // 计算当前火焰线的角度和长度
                var angle = num4 + (num5 - num4) * i / num3;
                var num6 = 16f + 224f * i / num3;
                var n = (int)(num6 / 16f);
                var counterdown = 60 * i / num3 + 2;
                // 确定火焰线的起点和终点位置
                var val = TPlayer.Center + FromPolar(angle, num2);
                // 创建并配置QueuedProjLineShot对象
                var qpls = new QueuedProjLineShot(this);
                qpls.Damage = damage;
                qpls.Knockback = 5f;
                qpls.Start = val + FromPolar(angle, (0f - num6) / 2f);
                qpls.End = val + FromPolar(angle, num6 / 2f);
                qpls.Counterdown = counterdown;
                qpls.N = n;
                var shot = qpls;
                // 添加到插件实例中处理
                FancyflamePlugin.Instance!.AddLineShot(shot);
            }
            return;
        }

        // 如果敌人在160f以内，则触发近距离攻击模式，增加冷却时间
        FlamesCooldown *= 2;
        // 初始化延迟计数器和增量
        var num7 = 2;
        var num8 = 5;
        num2 = 120f;

        // 创建环绕玩家的多层火焰圈
        for (var j = 0; j < 3; j++) // 外层循环控制层数
        {
            var num9 = 0.0;
            var num10 = Math.PI / 4.0;

            for (var k = 0; k < 8; k++) // 中层循环控制每层的扇区数
            {
                for (var l = 0; l < 3; l++) // 内层循环控制每个扇区内的射线数
                {
                    // 计算射线的起始和结束位置
                    var start = TPlayer.Center + FromPolar(num9, 80f);
                    var end = TPlayer.Center + FromPolar(num9, 160f);
                    // 创建并配置QueuedProjLineShot对象
                    var qpls = new QueuedProjLineShot(this);
                    qpls.Damage = damage;
                    qpls.Knockback = 5f;
                    qpls.Start = start;
                    qpls.End = end;
                    qpls.Counterdown = num7;
                    qpls.N = 5;
                    var shot2 = qpls;

                    // 更新角度准备下一次射线
                    num9 += num10 / 3.0;

                    // 添加到插件实例中处理
                    FancyflamePlugin.Instance!.AddLineShot(shot2);
                }
                // 增加延迟计数器以确保层次之间有时间间隔
                num7 += num8;
            }
        }
    } 
    #endregion

    #region 尝试查找最近的目标距离
    public float? TryFindNearestTargetDistance()
    {
        var npcs = (from npc in Main.npc
                    where npc.active && !npc.friendly
                    where npc.Distance(TPlayer.Center) < 1280f
                    orderby npc.Distance(TPlayer.Center)
                    select npc).FirstOrDefault()!;
        return (npcs != null) ? new float?(npcs.Distance(TPlayer.Center)) : null;
    }
    #endregion

    #region 创建新弹幕
    public int NewProj(Vector2 position, Vector2 velocity, int Type, int Damage, float KnockBack = 20f, int owner = 255, float ai0 = 0f, float ai1 = 0f, int extraUpdates = 0)
    {
        var tPlayer = TPlayer;
        var Source = tPlayer.GetProjectileSource_Item(tPlayer.HeldItem);
        var num = Projectile.NewProjectile(Source, position, velocity, Type, Damage, KnockBack, owner, ai0, ai1, 0f);

        //羽学注：NewProjectile方法已经能创建弹幕了，不需要发包
        //Main.projectile[num].extraUpdates = extraUpdates;
        //TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", num);
        return num;
    }
    #endregion

    #region 沿着一条线发射多个弹幕
    public void ProjLine(Vector2 start, Vector2 end, Vector2 velocity, int n, int projType, int damage, float knockback)
    {
        var val = (end - start) / n;
        for (var i = 0; i < n; i++)
        {
            NewProj(start + val * i, velocity, projType, damage, knockback);
        }
    }
    #endregion

    #region 将角度和长度转换为二维向量。
    /// <summary>
    /// 将角度和长度转换为二维向量。
    /// </summary>
    /// <param name="angle">角度（以弧度为单位）。</param>
    /// <param name="length">向量的长度。</param>
    /// <returns>一个表示新位置的Vector2对象。</returns>
    public static Vector2 FromPolar(double angle, float length)
    {
        // 计算X和Y坐标，并返回新的Vector2
        return new Vector2(
            (float)(Math.Cos(angle) * length), // X坐标
            (float)(Math.Sin(angle) * length)  // Y坐标
        );
    } 
    #endregion

}

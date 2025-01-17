﻿using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using static Fancyflame.FancyflamePlugin;

namespace Fancyflame;

public class PlayerModel
{
    // 玩家在游戏内的索引
    public int Index { get; }
    // 这样好判断新旧玩家
    public int UUID { get; }
    public Player TPlayer => Main.player[Index]; // 获取与当前PlayerModel关联的Terraria.Player实例

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
    public bool IsUseItem { get; set; } // 表示玩家是否正在使用物品
    public int FlamesCooldown { get; set; } // 火焰技能的冷却时间
    public void Update()
    {
        if (Config.ItemList == null || Config.ItemList.Count <= 0) return;

        foreach (var item in Config.ItemList)
        {
            if (item == null || item.ItemID == 0) continue;

            // 如果火焰斩技能处于冷却中，则减少冷却时间
            if (FlamesCooldown > 0)
            {
                FlamesCooldown -= item.FrameIntervals;
            }

            // 检查是否满足释放火焰斩技能的条件：持有物品为3507（铜短剑） 不在冷却中 正在使用物品
            if (item.ItemID == TPlayer.HeldItem.type && FlamesCooldown <= 0 && IsUseItem)
            {
                FlamesCooldown = item.FlamesCooldown; // 设置火焰斩技能的冷却时间为120帧
                ReleaseFlameSlash(item); // 释放火焰斩技能
            }
        }
    }

    //当玩家正在使用物品时更新 IsUseItem 标识
    public void OnReceiveUpdate(GetDataHandlers.PlayerUpdateEventArgs args)
    {
        IsUseItem = args.Control.IsUsingItem;
    }
    #endregion

    #region 释放火焰斩技能的方法
    private bool reverse; // 用于在火焰斩释放时切换方向
    internal void ReleaseFlameSlash(Configuration.ItemData item)
    {
        // 切换火焰斩的方向
        reverse ^= true;
        var sign = reverse ? 1 : -1;
        // 尝试找到最近的目标距离，默认50格
        var radius = Math.Min(item.RemoteRadius * 16, TryFindNearestTargetDistance(item) ?? item.RemoteRadius * 16);
        // 计算武器伤害并乘以系数
        var damage = TPlayer.GetWeaponDamage(TPlayer.HeldItem) * 5 / 2;

        // 根据目标距离决定使用远程攻击模式还是近距离攻击模式
        if (radius > item.RemoteRange * 16)
        {
            // 远程攻击模式：发射一系列定向的火焰线
            // 计算要发射的火焰线条数，至少5条，最多33条
            var steps = Math.Max(item.RemoteMin, Math.Min(item.RemoteMax, (int)Math.Ceiling(radius / 16 * item.RemoteRadius * item.RemoteMax)));

            // 定义火焰线的角度范围
            var angleStart = ItemUseAngle + Math.PI / item.RemoteAngleStart * sign; // 最大角度偏移
            var angleEnd = ItemUseAngle - Math.PI / item.RemoteAngleStop * sign; // 最小角度偏移

            // 循环创建并发射每一条火焰线
            for (var step = 0; step < steps; step++)
            {
                // 计算当前火焰线的角度和长度
                var angle = angleStart + (angleEnd - angleStart) * step / steps;
                var width = 16f + 224f * step / steps;
                var n = (int)(width / 16f);
                var timeToDelay = 60 * step / steps + 2;
                // 确定火焰线的起点和终点位置
                var lineCenter = TPlayer.Center + FromPolar(angle, radius);
                // 创建并配置QueuedProjLineShot对象
                var shot = new QueuedProjLineShot(this)
                {
                    Damage = damage,
                    Knockback = 5,
                    Start = lineCenter + FromPolar(angle, -width / 2),
                    End = lineCenter + FromPolar(angle, width / 2),
                    Counterdown = timeToDelay,
                    N = n,
                };
                FancyflamePlugin.Instance!.AddLineShot(shot);
            }
            return;
        }
        else
        {
            FlamesCooldown *= 2; // 如果敌人在10格以内，则触发近距离攻击模式，增加冷却时间(直接在身边扫2圈，时间翻倍

            // 初始化延迟计数器和增量
            var delay = 2;
            var deltaTime = 120 / 24;
            radius = 16 * item.CloseRadius;
            // 创建环绕玩家的多层火焰圈
            for (var i = 0; i < 3; i++) // 外层循环控制层数
            {
                var angle = 0.0;
                var delta = Math.PI / 4.0;
                for (var j = 0; j < 8; j++) // 中层循环控制每层的扇区数
                {
                    for (var k = 0; k < 3; k++) // 内层循环控制每个扇区内的射线数
                    {
                        // 计算射线的起始和结束位置
                        var lineStart = TPlayer.Center + FromPolar(angle, 5 * 16);
                        var lineEnd = TPlayer.Center + FromPolar(angle, 10 * 16);
                        // 创建并配置QueuedProjLineShot对象
                        var shot = new QueuedProjLineShot(this)
                        {
                            Damage = damage,
                            Knockback = 5,
                            Start = lineStart,
                            End = lineEnd,
                            Counterdown = delay,
                            N = 5,
                        };

                        // 更新角度准备下一次射线
                        angle += delta / 3.0;
                        // 添加到插件实例中处理
                        FancyflamePlugin.Instance!.AddLineShot(shot);
                    }
                    // 增加延迟计数器以确保层次之间有时间间隔
                    delay += deltaTime;
                }
            }
        }
    }
    #endregion

    #region 尝试查找最近的目标距离
    internal float? TryFindNearestTargetDistance(Configuration.ItemData item)
    {
        var target = Main.npc
            .Where(npc => npc.active && !npc.friendly)
            .Where(npc => npc.Distance(TPlayer.Center) < item.AutoFindRange * 16)
            .OrderBy(npc => npc.Distance(TPlayer.Center))
            .FirstOrDefault();
        return target?.Distance(TPlayer.Center);
    }
    #endregion

    #region 创建新弹幕
    public int NewProj(Vector2 position, Vector2 velocity, int Type, int Damage, float KnockBack = 20f, int owner = 255, float ai0 = 0f, float ai1 = 0f, int extraUpdates = 0)
    {
        var tPlayer = TPlayer;
        var Source = tPlayer.GetProjectileSource_Item(tPlayer.HeldItem);
        var index = Projectile.NewProjectile(Source, position, velocity, Type, Damage, KnockBack, owner, ai0, ai1, 0f);

        //羽学注：NewProjectile方法已经能创建弹幕了，不需要发包
        Main.projectile[index].extraUpdates = extraUpdates;
        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", index);
        return index;
    }
    #endregion

    #region 沿着一条线发射多个弹幕
    public void ProjLine(Vector2 start, Vector2 end, Vector2 velocity, int n, int projType, int damage, float knockback)
    {
        var delta = (end - start) / n;
        for (var i = 0; i < n; i++)
        {
            NewProj(start + delta * i, velocity, projType, damage, knockback);
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

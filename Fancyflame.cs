using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Fancyflame;

[ApiVersion(2, 1)]
public class FancyflamePlugin : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "火焰斩";
    public override string Author => "TOFOUT";
    public override string Description => "让指定武器能释放自动寻敌的火焰斩";
    public override Version Version => new Version(1, 0);
    #endregion

    #region 注册与释放
    public static FancyflamePlugin? Instance { get; private set; } // 单例模式实例 
    public FancyflamePlugin(Main game) : base(game)
    {
        players = new PlayerModel[255];
        queue = new LinkedList<QueuedProjLineShot>();
        queueBuffer = new List<QueuedProjLineShot>();
        Instance = this;
    }

    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
        PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
            PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
        }
        Instance = null;
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
    private static void ReloadConfig(ReloadEventArgs args = null!)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[火焰斩]重新加载配置完毕。");
    }
    #endregion

    #region 世界更新事件
    private PlayerModel?[] players; // 存储所有玩家的PlayerModel实例
    private LinkedList<QueuedProjLineShot> queue; // 用于处理线性射击任务的队列
    private List<QueuedProjLineShot> queueBuffer; // 缓存新的线性射击任务
    public void OnGameUpdate(object args)
    {
        // 遍历玩家数组，每隔5个元素调用一次Update方法来更新玩家状态。
        // 注意：这种遍历方式可能意味着并非所有玩家都会在每次世界更新时被更新，这取决于players数组的实际内容和长度。
        for (var i = 0; i < players.Length; i += Config.FrameIntervals)
        {
            // 使用null条件运算符安全地调用Update方法，避免因空引用引发异常。
            players[i]?.Update();
        }

        // 获取队列中的元素数量，并存储在一个局部变量中以确保循环过程中count值不变。
        var n = queue.Count;
        if (n > 0) // 如果队列中有待处理的元素，则进入此块逻辑进行处理。
        {
            // 获取队列的第一个节点。
            var node = queue.First;
            // 根据预先获取的count值迭代队列中的每个元素。
            for (var i = 0; i < n; i++)
            {
                // 提前保存下一个节点，因为当前节点可能会从队列中移除。
                var nodeNext = node!.Next;
                // 调用队列元素的Update方法，如果返回true则表示该元素已完成其工作并应从队列中移除。
                if (node.ValueRef.Update())
                {
                    queue.Remove(node);
                }

                // 移动到下一个节点。
                node = nodeNext;
            }
        }

        // 将queueBuffer中的所有元素添加到queue队列中，然后清空queueBuffer。
        // 这种做法通常用于将一批新的任务或对象加入到处理队列中，同时确保旧的任务先被处理。
        NuGet.Packaging.CollectionExtensions.AddRange(queue, queueBuffer);
        queueBuffer.Clear();
    }
    #endregion

    #region 添加线性射击
    public void AddLineShot(QueuedProjLineShot shot)
    {
        queueBuffer.Add(shot);
    } 
    #endregion

    #region 玩家登录事件
    private void OnPlayerPostLogin(PlayerPostLoginEventArgs e)
    {
        players[e.Player.Index] = new PlayerModel(e.Player.Index);
    }
    #endregion

    #region 玩家更新事件
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        players[args.Player.Index]?.OnReceiveUpdate(args);
    }
    #endregion

    #region 玩家离开服务器事件
    private void OnServerLeave(LeaveEventArgs args)
    {
        players[args.Who] = null;
    } 
    #endregion
}
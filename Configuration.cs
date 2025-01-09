﻿using Newtonsoft.Json;
using TShockAPI;

namespace Fancyflame;

internal class Configuration
{
    #region 实例变量
    [JsonProperty("物品ID", Order = 1)]
    public List<int> ItemID { get; set; } = new();
    [JsonProperty("弹幕ID", Order = 2)]
    public int FlamesProj = 85;
    [JsonProperty("火焰斩间隔", Order = 3)]
    public int FrameIntervals = 5; // 反正5帧也就83ms延迟
    [JsonProperty("火焰斩冷却帧数", Order = 3)]
    public int FlamesCooldown = 60 * 2;
    #endregion

    #region 预设参数方法
    public void SetDefault()
    {
        ItemID = new List<int>() { 3507 };
    }
    #endregion

    #region 读取与创建配置文件方法
    public static readonly string FilePath = Path.Combine(TShock.SavePath, "火焰斩.json");
    public void Write()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }

    public static Configuration Read()
    {
        if (!File.Exists(FilePath))
        {
            var NewConfig = new Configuration();
            NewConfig.SetDefault();
            new Configuration().Write();
            return NewConfig;
        }
        else
        {
            string jsonContent = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
        }
    }
    #endregion
}
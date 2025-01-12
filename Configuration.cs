using Newtonsoft.Json;
using TShockAPI;

namespace Fancyflame;

internal class Configuration
{
    [JsonProperty("物品数据表", Order = 1)]
    public List<ItemData> ItemList = new List<ItemData>();

    public class ItemData 
    {
        [JsonProperty("物品ID", Order = 1)]
        public int ItemID { get; set; } = new();
        [JsonProperty("弹幕ID", Order = 2)]
        public int FlamesProj { get; set; }
        [JsonProperty("间隔", Order = 3)]
        public int FrameIntervals { get; set; }
        [JsonProperty("冷却帧数", Order = 4)]
        public int FlamesCooldown { get; set; }
        [JsonProperty("远程攻击范围", Order = 5)]
        public int RemoteRadius { get; set; }
        [JsonProperty("近战攻击范围", Order = 6)]
        public float CloseRadius { get; set; }
        [JsonProperty("自动寻敌范围", Order = 7)]
        public int AutoFindRange { get; set; }
        [JsonProperty("近远切换范围", Order = 8)]
        public int RemoteRange { get; set; }
        [JsonProperty("远程弹幕最小数", Order = 9)]
        public int RemoteMin { get; set; }
        [JsonProperty("远程弹幕最大数", Order = 10)]
        public int RemoteMax { get; set; }
        [JsonProperty("远程角度最小偏移", Order = 11)]
        public double RemoteAngleStop { get; set; }
        [JsonProperty("远程角度最大偏移", Order = 12)]
        public double RemoteAngleStart { get; set; }


    }

    #region 预设参数方法
    public void SetDefault()
    {
        ItemList = new List<ItemData>() 
        {
           new ItemData()
           {
               ItemID = 3507,
               FlamesProj = 85,
               FrameIntervals = 5,
               FlamesCooldown = 60 * 2,
               RemoteRadius = 50,
               AutoFindRange = 80,
               RemoteRange = 10, RemoteMin = 5, RemoteMax = 33,
               RemoteAngleStart = 3.0, RemoteAngleStop = 3.0,
               CloseRadius = 7.5f
           },
           new ItemData()
           {
               ItemID = 4,
               FlamesProj = 700,
               FrameIntervals = 20,
               FlamesCooldown = 60 * 8,
               RemoteRadius = 50, AutoFindRange = 80,
               RemoteRange = 10, RemoteMin = 1, RemoteMax = 10,
               RemoteAngleStart = 1.0, RemoteAngleStop = 1.0,
               CloseRadius = 7.5f
           }
        };
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
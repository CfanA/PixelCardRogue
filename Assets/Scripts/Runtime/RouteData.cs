using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyCourier
{
    public enum RouteNodeKind
    {
        Skirmish,
        Elite,
        Hunt,
        Shop,
        Event,
        Rest,
        MidBoss,
        Boss
    }

    public enum AirspaceCondition
    {
        JetstreamCorridor,
        StaticFront,
        WreckageTide
    }

    public static class AirspaceRuleCatalog
    {
        public static string Name(AirspaceCondition condition) => condition switch
        {
            AirspaceCondition.JetstreamCorridor => "疾风走廊",
            AirspaceCondition.StaticFront => "静电锋面",
            _ => "残骸潮"
        };

        public static string Band(AirspaceCondition condition) => condition switch
        {
            AirspaceCondition.JetstreamCorridor => "高空",
            AirspaceCondition.StaticFront => "中层",
            _ => "低空"
        };

        public static string EncounterRule(AirspaceCondition condition) => condition switch
        {
            AirspaceCondition.JetstreamCorridor => "高速拦截与蓄力编队",
            AirspaceCondition.StaticFront => "灾变单位与协议干扰编队",
            _ => "机体拆解与信号封锁编队"
        };

        public static string RewardRule(AirspaceCondition condition) => condition switch
        {
            AirspaceCondition.JetstreamCorridor => "奖励偏向机动与低热循环",
            AirspaceCondition.StaticFront => "奖励偏向控制与资源调度",
            _ => "奖励偏向爆发与正面生存"
        };

        public static int EncounterVariant(AirspaceCondition condition, int encounterSeed)
        {
            int branch = (encounterSeed & int.MaxValue) % 4;
            return condition switch
            {
                AirspaceCondition.JetstreamCorridor => new[] { 0, 1, 6, 7 }[branch],
                AirspaceCondition.StaticFront => new[] { 1, 3, 8, 9 }[branch],
                _ => new[] { 2, 3, 10, 11 }[branch]
            };
        }
    }

    public sealed class RouteNodeDefinition
    {
        public int Id { get; }
        public int Column { get; }
        public int Lane { get; }
        public RouteNodeKind Kind { get; }
        public AirspaceCondition Airspace { get; }
        public string Title { get; }
        public string Description { get; }
        public int[] Next { get; }

        public RouteNodeDefinition(int id, int column, int lane, RouteNodeKind kind, string title,
            string description, params int[] next)
        {
            Id = id;
            Column = column;
            Lane = lane;
            Kind = kind;
            Airspace = lane switch
            {
                0 => AirspaceCondition.JetstreamCorridor,
                1 => AirspaceCondition.StaticFront,
                _ => AirspaceCondition.WreckageTide
            };
            Title = title;
            Description = description;
            Next = next ?? Array.Empty<int>();
        }

        public bool IsBattle => Kind == RouteNodeKind.Skirmish || Kind == RouteNodeKind.Elite ||
            Kind == RouteNodeKind.Hunt || Kind == RouteNodeKind.MidBoss || Kind == RouteNodeKind.Boss;

        public EncounterId Encounter => Kind switch
        {
            RouteNodeKind.Elite => EncounterId.Elite,
            RouteNodeKind.Hunt => EncounterId.Hunt,
            RouteNodeKind.MidBoss => EncounterId.MidBoss,
            RouteNodeKind.Boss => EncounterId.Boss,
            _ => EncounterId.Skirmish
        };
    }

    public sealed class RouteDefinition
    {
        private readonly Dictionary<int, RouteNodeDefinition> byId;

        public IReadOnlyList<RouteNodeDefinition> Nodes { get; }
        public int ColumnCount { get; }

        public RouteDefinition(params RouteNodeDefinition[] nodes)
        {
            Nodes = nodes;
            byId = nodes.ToDictionary(node => node.Id);
            ColumnCount = nodes.Max(node => node.Column) + 1;
            Validate();
        }

        public RouteNodeDefinition Get(int id) => byId[id];

        public IEnumerable<RouteNodeDefinition> AtColumn(int column) =>
            Nodes.Where(node => node.Column == column).OrderBy(node => node.Lane);

        private void Validate()
        {
            if (Nodes.Count == 0 || Nodes.Select(node => node.Id).Distinct().Count() != Nodes.Count)
                throw new InvalidOperationException("Route nodes must have unique ids.");
            foreach (RouteNodeDefinition node in Nodes)
            {
                foreach (int nextId in node.Next)
                {
                    if (!byId.TryGetValue(nextId, out RouteNodeDefinition next) || next.Column != node.Column + 1)
                        throw new InvalidOperationException($"Route edge {node.Id}->{nextId} must target the next column.");
                }
            }
        }
    }

    public static class RouteMapLayoutRules
    {
        public const float ColumnSpacing = 190f;
        public const float ContentPadding = 115f;

        public static float ContentWidth(int columnCount) =>
            ContentPadding * 2f + Math.Max(0, columnCount - 1) * ColumnSpacing;

        public static float MaximumScroll(int columnCount, float viewportWidth) =>
            Math.Max(0f, ContentWidth(columnCount) - viewportWidth);

        public static int RevealThrough(int currentColumn, int columnCount) =>
            Math.Min(Math.Max(0, columnCount - 1), Math.Max(0, currentColumn) + 2);
    }

    public static class RouteCatalog
    {
        public static readonly RouteDefinition WindmillArchipelago = new RouteDefinition(
            Node(0, 0, 1, RouteNodeKind.Skirmish, "废弃风标", "劫掠机封锁了离港航道。", 1, 2),

            Node(1, 1, 0, RouteNodeKind.Shop, "浮岛补给站", "购买卡牌、模块或修补机体。", 3, 4),
            Node(2, 1, 2, RouteNodeKind.Event, "失联求救信标", "合同信号与未知风险同时出现。", 4, 5),

            Node(3, 2, 0, RouteNodeKind.Skirmish, "云墙巡逻线", "轻型编队守住了稳定气流。", 6, 7),
            Node(4, 2, 1, RouteNodeKind.Elite, "雷暴封锁线", "高威胁编队携带稀有模块。", 6, 7, 8),
            Node(5, 2, 2, RouteNodeKind.Skirmish, "漂流炮艇群", "残骸之间埋伏着近距炮艇。", 7, 8),

            Node(6, 3, 0, RouteNodeKind.Rest, "云港维修坞", "停靠维修，或校准一张基础卡。", 9, 10),
            Node(7, 3, 1, RouteNodeKind.Event, "风暴残骸带", "残骸中仍有可回收的供能单元。", 9, 10, 11),
            Node(8, 3, 2, RouteNodeKind.Hunt, "追迹前哨", "敌方会根据连续换道进行锁定。", 10, 11),

            Node(9, 4, 0, RouteNodeKind.Elite, "磁针舰队", "磁针阵列会压缩安全航道。", 12, 13),
            Node(10, 4, 1, RouteNodeKind.Hunt, "追迹者空域", "航迹暴露将成为主要威胁。", 12, 13, 14),
            Node(11, 4, 2, RouteNodeKind.Shop, "黑市浮岛", "高价出售攻击组件与维修物资。", 13, 14),

            Node(12, 5, 0, RouteNodeKind.Event, "静电观测站", "观测站记录了首领的磁暴周期。", 15, 16),
            Node(13, 5, 1, RouteNodeKind.Skirmish, "断流峡谷", "狭窄气流迫使双方正面交火。", 15, 16, 17),
            Node(14, 5, 2, RouteNodeKind.Rest, "应急船坞", "终点前最后一次免费维护机会。", 16, 17),

            Node(15, 6, 0, RouteNodeKind.MidBoss, "万箱母巢", "拆解两侧功能舱，阻止母巢重组失踪货箱。", 20, 21),
            Node(16, 6, 1, RouteNodeKind.MidBoss, "三相气象钟", "赤、青、白三种天气相位轮转，考验临场适应。", 20, 21, 22),
            Node(17, 6, 2, RouteNodeKind.MidBoss, "莫比乌斯机库", "折叠机库持续旋转敌我航道与残骸坐标。", 21, 22),

            Node(20, 7, 0, RouteNodeKind.Elite, "雷鸣唱诗班", "三航道共鸣即将贯穿高压云墙。", 23, 24),
            Node(21, 7, 1, RouteNodeKind.Event, "风眼交换站", "可用货物、机体或邮票交换终局情报。", 23, 24, 25),
            Node(22, 7, 2, RouteNodeKind.Skirmish, "折光寄舱群", "折射炮记录受击航道并延迟还击。", 24, 25),

            Node(23, 8, 0, RouteNodeKind.Rest, "高空维修环", "中阶考核后的稳定维护窗口。", 26, 27),
            Node(24, 8, 1, RouteNodeKind.Elite, "债务清算所", "资源爆发会被换算为不断增长的航路债务。", 26, 27, 28),
            Node(25, 8, 2, RouteNodeKind.Hunt, "空白信标猎场", "支援信标不断改写追猎编队的防护协议。", 27, 28),

            Node(26, 9, 0, RouteNodeKind.Event, "零号投递记录", "最初邮差留下了一份仍在执行的错误指令。", 29, 30),
            Node(27, 9, 1, RouteNodeKind.Skirmish, "倒悬城外环", "云鲸背部的废城炮塔封锁进场航道。", 29, 30),
            Node(28, 9, 2, RouteNodeKind.Shop, "终局黑市浮岛", "最后一次购买、删牌与模块替换机会。", 29, 30),

            Node(29, 10, 0, RouteNodeKind.Rest, "风眼整备坞", "修复机体或校准终局核心卡。", 18, 31),
            Node(30, 10, 2, RouteNodeKind.Elite, "终局警戒网", "额外高压战斗换取最后一份稀有强化。", 19, 32),

            Node(18, 11, 0, RouteNodeKind.Boss, "磁暴鳐巢", "锁定航道与邻道溅射考验远距规避。"),
            Node(19, 11, 1, RouteNodeKind.Boss, "雷幕龙脊", "雷幕只留下唯一安全航道，逼迫精准换道。"),
            Node(31, 11, 1, RouteNodeKind.Boss, "零号邮局", "最初邮差将复制并审计你的构筑习惯。"),
            Node(32, 11, 2, RouteNodeKind.Boss, "倒悬天穹鲸", "环城航道翻转，鲸落倒计时已经开始。")
        );

        private static RouteNodeDefinition Node(int id, int column, int lane, RouteNodeKind kind,
            string title, string description, params int[] next) =>
            new RouteNodeDefinition(id, column, lane, kind, title, description, next);
    }
}

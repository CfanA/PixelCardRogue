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
            int branch = (encounterSeed & int.MaxValue) % 2;
            return condition switch
            {
                AirspaceCondition.JetstreamCorridor => branch,
                AirspaceCondition.StaticFront => branch == 0 ? 1 : 3,
                _ => branch == 0 ? 2 : 3
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
            Kind == RouteNodeKind.Hunt || Kind == RouteNodeKind.Boss;

        public EncounterId Encounter => Kind switch
        {
            RouteNodeKind.Elite => EncounterId.Elite,
            RouteNodeKind.Hunt => EncounterId.Hunt,
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

            Node(15, 6, 0, RouteNodeKind.Hunt, "雷幕先导追猎", "先导机正在演练唯一安全航道，击破后可截获雷幕密钥。", 19),
            Node(16, 6, 1, RouteNodeKind.Elite, "双频天穹封锁", "两类先遣机共同护航，可截获适配任一首领的双频解码器。", 18, 19),
            Node(17, 6, 2, RouteNodeKind.Skirmish, "磁针鳐卫伏击", "磁针扫掠覆盖目标邻道，击破后可校准偏航罗盘。", 18),

            Node(18, 7, 2, RouteNodeKind.Boss, "磁暴鳐巢", "锁定航道与邻道溅射考验远距规避。"),
            Node(19, 7, 0, RouteNodeKind.Boss, "雷幕龙脊", "雷幕只留下唯一安全航道，逼迫精准换道。")
        );

        private static RouteNodeDefinition Node(int id, int column, int lane, RouteNodeKind kind,
            string title, string description, params int[] next) =>
            new RouteNodeDefinition(id, column, lane, kind, title, description, next);
    }
}

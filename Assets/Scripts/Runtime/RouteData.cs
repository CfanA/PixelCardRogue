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

    public sealed class RouteNodeDefinition
    {
        public int Id { get; }
        public int Column { get; }
        public int Lane { get; }
        public RouteNodeKind Kind { get; }
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

            Node(15, 6, 0, RouteNodeKind.Hunt, "风眼追猎", "穿过风眼，同时甩掉追踪编队。", 18),
            Node(16, 6, 1, RouteNodeKind.Elite, "天穹封锁", "核心护航舰队守住终点入口。", 18),
            Node(17, 6, 2, RouteNodeKind.Skirmish, "残骸伏击", "低风险入口仍潜伏着拾荒者。", 18),

            Node(18, 7, 1, RouteNodeKind.Boss, "磁暴鳐巢", "完成配送前的最终障碍。")
        );

        private static RouteNodeDefinition Node(int id, int column, int lane, RouteNodeKind kind,
            string title, string description, params int[] next) =>
            new RouteNodeDefinition(id, column, lane, kind, title, description, next);
    }
}

using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    public enum MapNodeKind
    {
        Battle,
        Boss,
    }

    public sealed class MapNode
    {
        public MapNode(int id, int floor, int column, MapNodeKind kind, string enemyGroupId, IReadOnlyList<int> nextNodeIds)
        {
            Id = id;
            Floor = floor;
            Column = column;
            Kind = kind;
            EnemyGroupId = enemyGroupId;
            NextNodeIds = nextNodeIds;
        }

        public int Id { get; }

        /// <summary>1-based. The boss floor is one past the last battle floor.</summary>
        public int Floor { get; }

        /// <summary>Position within the floor, from 0.</summary>
        public int Column { get; }

        public MapNodeKind Kind { get; }
        public string EnemyGroupId { get; }

        /// <summary>Nodes on the next floor that can be chosen after this one. Empty for the boss.</summary>
        public IReadOnlyList<int> NextNodeIds { get; }
    }

    /// <summary>The node graph of one expedition. Node ids are indexes into <see cref="Nodes"/>.</summary>
    public sealed class NodeMap
    {
        public NodeMap(IReadOnlyList<MapNode> nodes, int floorCount)
        {
            Nodes = nodes;
            FloorCount = floorCount;
        }

        public IReadOnlyList<MapNode> Nodes { get; }

        /// <summary>Battle floors plus the boss floor.</summary>
        public int FloorCount { get; }

        public MapNode Get(int nodeId)
        {
            if (nodeId < 0 || nodeId >= Nodes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "Unknown map node.");
            }

            return Nodes[nodeId];
        }

        public List<MapNode> OnFloor(int floor)
        {
            var result = new List<MapNode>();
            foreach (MapNode node in Nodes)
            {
                if (node.Floor == floor)
                {
                    result.Add(node);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Builds the map of an expedition from its seed (Docs/Design/03_Dungeon_Structure.md §1).
    /// The same seed always gives the same map.
    /// </summary>
    public static class MapGenerator
    {
        public static NodeMap Generate(StaticData data, DungeonData dungeon, ulong expeditionSeed)
        {
            var rng = new Pcg32(SeedDeriver.Derive(expeditionSeed, "map", 0), RngStream.Map);

            // Decide the shape first: how many nodes each battle floor has.
            var widths = new int[dungeon.Floors];
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = rng.NextInt(dungeon.MapMinWidth, dungeon.MapMaxWidth);
            }

            var firstIdOfFloor = new int[dungeon.Floors + 1];
            int count = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                firstIdOfFloor[i] = count;
                count += widths[i];
            }

            int bossId = count;
            firstIdOfFloor[dungeon.Floors] = bossId;

            var nodes = new List<MapNode>();
            for (int i = 0; i < widths.Length; i++)
            {
                int floor = i + 1;
                List<EnemyGroupData> groups = data.GroupsFor(dungeon.Id, floor);
                bool lastBattleFloor = i == widths.Length - 1;
                List<int>[] edges = lastBattleFloor ? null : ConnectFloors(widths[i], widths[i + 1], data.Balance.MapBranchChancePercent, rng);

                for (int column = 0; column < widths[i]; column++)
                {
                    EnemyGroupData group = groups[rng.NextInt(groups.Count)];
                    var next = new List<int>();
                    if (lastBattleFloor)
                    {
                        next.Add(bossId);
                    }
                    else
                    {
                        foreach (int target in edges[column])
                        {
                            next.Add(firstIdOfFloor[i + 1] + target);
                        }
                    }

                    nodes.Add(new MapNode(firstIdOfFloor[i] + column, floor, column, MapNodeKind.Battle, group.Id, next));
                }
            }

            nodes.Add(new MapNode(
                bossId,
                dungeon.Floors + 1,
                0,
                MapNodeKind.Boss,
                data.BossGroupOf(dungeon.Id).Id,
                new List<int>()));

            return new NodeMap(nodes, dungeon.Floors + 1);
        }

        /// <summary>
        /// Edges from a floor of <paramref name="from"/> nodes to the next floor of <paramref name="to"/> nodes.
        /// Every node leads to the nearest node below it, sometimes also to a neighbour, and every
        /// node below is reachable.
        /// </summary>
        static List<int>[] ConnectFloors(int from, int to, int branchChancePercent, Pcg32 rng)
        {
            var edges = new List<int>[from];
            var reached = new bool[to];

            for (int i = 0; i < from; i++)
            {
                int primary = i * to / from;
                edges[i] = new List<int> { primary };
                reached[primary] = true;

                if (to > 1 && rng.NextInt(100) < branchChancePercent)
                {
                    int neighbour = primary + 1 < to ? primary + 1 : primary - 1;
                    edges[i].Add(neighbour);
                    reached[neighbour] = true;
                }
            }

            for (int j = 0; j < to; j++)
            {
                if (reached[j])
                {
                    continue;
                }

                int source = Math.Min(from - 1, j * from / to);
                edges[source].Add(j);
            }

            foreach (List<int> list in edges)
            {
                list.Sort();
            }

            return edges;
        }
    }
}

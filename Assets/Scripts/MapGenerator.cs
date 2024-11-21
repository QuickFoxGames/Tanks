using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public enum Type
    {
        Grass,
        Dirt,
        Sand,
        Water
    }
    [SerializeField] private int m_width;
    [SerializeField] private int m_height;
    [SerializeField] private float m_tileSize;
    [SerializeField] private Tile[] m_tiles;

    private Cell[,] m_grid;

    void Start()
    {
        RunWaveFunctionCollapse();
    }

    private void RunWaveFunctionCollapse()
    {
        InitializeGrid();

        // Keep collapsing until all cells are resolved
        while (true)
        {
            Cell cell = FindCellWithLeastEntropy();
            if (cell == null) // No more cells to process
                break;

            CollapseCell(cell);
            PropagateConstraints(cell);
        }

        InstantiateMap();
    }

    private void InitializeGrid()
    {
        m_grid = new Cell[m_width, m_height];

        for (int x = 0; x < m_width; x++)
        {
            for (int y = 0; y < m_height; y++)
            {
                m_grid[x, y] = new Cell
                {
                    Position = new Vector2Int(x, y),
                    PossibleTypes = m_tiles.Select(tile => tile.Type).ToList()
                };
            }
        }
    }
    private Cell FindCellWithLeastEntropy()
    {
        return m_grid.Cast<Cell>()
            .Where(cell => cell.PossibleTypes.Count > 1)
            .OrderBy(cell => cell.PossibleTypes.Count)
            .FirstOrDefault();
    }
    private void CollapseCell(Cell cell)
    {
        var weights = GetWeights(cell.PossibleTypes);
        var chosenType = WeightedRandomChoice(cell.PossibleTypes, weights);
        cell.PossibleTypes = new List<Type> { chosenType };
    }
    private List<float> GetWeights(List<Type> possibleTypes)
    {
        return possibleTypes.Select(type =>
        {
            var tile = m_tiles.First(t => t.Type == type);
            var index = tile.m_connectedTypes.ToList().IndexOf(type);
            return index >= 0 ? 1f / (index + 1) : 1f;
        }).ToList();
    }
    private Type WeightedRandomChoice(List<Type> possibleTypes, List<float> weights)
    {
        float totalWeight = weights.Sum();
        float randomValue = Random.Range(0, totalWeight);

        for (int i = 0; i < possibleTypes.Count; i++)
        {
            if (randomValue < weights[i])
                return possibleTypes[i];

            randomValue -= weights[i];
        }
        return possibleTypes.Last();
    }
    private void PropagateConstraints(Cell cell)
    {
        Queue<Cell> toProcess = new Queue<Cell>();
        toProcess.Enqueue(cell);

        while (toProcess.Count > 0)
        {
            Cell current = toProcess.Dequeue();

            foreach (var neighbor in GetNeighbors(current))
            {
                if (neighbor.PossibleTypes.Count == 1) // Already collapsed, skip
                    continue;

                var validTypes = GetValidNeighborTypes(current, neighbor);
                if (validTypes.Count < neighbor.PossibleTypes.Count) // Reduce possibilities
                {
                    neighbor.PossibleTypes = validTypes;

                    // Add affected neighbor to the queue
                    toProcess.Enqueue(neighbor);
                }
            }
        }
    }
    private List<Type> GetValidNeighborTypes(Cell current, Cell neighbor)
    {
        var validTypes = new List<Type>();

        foreach (var type in neighbor.PossibleTypes)
        {
            if (current.PossibleTypes.Any(t => CanConnect(t, type)))
            {
                validTypes.Add(type);
            }
        }

        validTypes = validTypes
            .OrderByDescending(type =>
            {
                var tile = m_tiles.First(t => t.Type == type);
                return 1f / (tile.m_connectedTypes.ToList().IndexOf(type) + 1);
            })
            .ToList();

        return validTypes;
    }
    private bool CanConnect(Type type1, Type type2)
    {
        Tile tile1 = m_tiles.First(t => t.Type == type1);
        return tile1.m_connectedTypes.Contains(type2);
    }
    private IEnumerable<Cell> GetNeighbors(Cell cell)
    {
        List<Vector2Int> directions = new()
    {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(1, 0),  // Right
        new Vector2Int(0, -1), // Down
        new Vector2Int(-1, 0)  // Left
    };

        foreach (var dir in directions)
        {
            var neighborPos = cell.Position + dir;

            if (neighborPos.x >= 0 && neighborPos.x < m_width &&
                neighborPos.y >= 0 && neighborPos.y < m_height)
            {
                yield return m_grid[neighborPos.x, neighborPos.y];
            }
        }
    }
    private void InstantiateMap()
    {
        for (int x = 0; x < m_width; x++)
        {
            for (int y = 0; y < m_height; y++)
            {
                Cell cell = m_grid[x, y];
                Tile tile = m_tiles.First(t => t.Type == cell.PossibleTypes[0]);
                Transform t = Instantiate(tile.m_prefab, new Vector3(x * m_tileSize, y * m_tileSize, 0f), Quaternion.identity, transform).transform;
                t.localScale = new(m_tileSize, m_tileSize, 1f);
            }
        }
    }
}

[System.Serializable]
public struct Tile
{
    public GameObject m_prefab;
    public MapGenerator.Type Type;
    public MapGenerator.Type[] m_connectedTypes;
}
public class Cell
{
    public Vector2Int Position;
    public List<MapGenerator.Type> PossibleTypes;
}

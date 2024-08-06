namespace AStarPathfindingTest
{
    public class Path
    {
        public bool IsWalkable { get; set; }
        public int Cost { get; set; }
    }

    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int GCost { get; set; } // Cost from start to this node
        public int HCost { get; set; } // Heuristic cost from this node to end
        public int FCost => GCost + HCost; // Total cost
        public Node? Parent { get; set; }

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class AStar
    {
        private static readonly int[] Dx = { 0, 1, 0, -1 };
        private static readonly int[] Dy = { 1, 0, -1, 0 };
        private readonly int _width;
        private readonly int _height;
        private readonly Path[,] _grid;
        private readonly List<Node> _traversedNodes = new List<Node>();

        public AStar(Path[,] grid)
        {
            _grid = grid;
            _width = grid.GetLength(0);
            _height = grid.GetLength(1);
        }

        private int Heuristic(int x1, int y1, int x2, int y2)
        {
            // Using Manhattan distance as heuristic
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public List<Node>? FindPath(int startX, int startY, int endX, int endY)
        {
            Node startNode = new Node(startX, startY);
            Node endNode = new Node(endX, endY);

            List<Node> openList = new List<Node> { startNode };
            HashSet<Node> closedList = new HashSet<Node>();

            Node[,] nodes = new Node[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    nodes[x, y] = new Node(x, y);
                }
            }

            startNode.GCost = 0;
            startNode.HCost = Heuristic(startX, startY, endX, endY);

            while (openList.Count > 0)
            {
                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < currentNode.FCost ||
                        (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                    {
                        currentNode = openList[i];
                    }
                }

                if (currentNode.X == endX && currentNode.Y == endY)
                {
                    List<Node> finalPath = RetracePath(startNode, currentNode);
                    PrintGrid(finalPath, endNode);
                    return finalPath;
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                for (int i = 0; i < 4; i++)
                {
                    int newX = currentNode.X + Dx[i];
                    int newY = currentNode.Y + Dy[i];

                    if (!IsInBounds(newX, newY) || !_grid[newX, newY].IsWalkable || closedList.Contains(nodes[newX, newY]))
                    {
                        continue;
                    }

                    int newGCost = currentNode.GCost + _grid[newX, newY].Cost;
                    Node neighbor = nodes[newX, newY];

                    if (newGCost < neighbor.GCost || !openList.Contains(neighbor))
                    {
                        neighbor.GCost = newGCost;
                        neighbor.HCost = Heuristic(newX, newY, endX, endY);
                        neighbor.Parent = currentNode;

                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }

                        _traversedNodes.Add(neighbor);
                        PrintGrid(RetracePath(startNode, neighbor), endNode);
                    }
                }
            }

            return null; // No path found
        }

        private List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node? currentNode = endNode;

            while (currentNode != null && currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private void PrintGrid(List<Node> path, Node endNode)
        {
            Console.Clear();
            Console.WriteLine($"Goal coordinates are {endNode.X}:{endNode.Y}");

            char[,] displayGrid = new char[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    displayGrid[x, y] = _grid[x, y].IsWalkable ? '.' : '#';
                }
            }

            if (_traversedNodes != null)
            {
                //set the traversed node characters
                foreach (Node node in _traversedNodes)
                {
                    displayGrid[node.X, node.Y] = 't';
                }
            }

            if (path != null)
            {
                //set the path node characters
                foreach (Node node in path)
                {
                    displayGrid[node.X, node.Y] = 'x';
                }
                //set the goal node character
                displayGrid[endNode.X, endNode.Y] = 'G';
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var pathItem = displayGrid[x, y];

                    if (pathItem.Equals('G')) Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (pathItem.Equals('x')) Console.ForegroundColor = ConsoleColor.Green;
                    else if (pathItem.Equals('t')) Console.ForegroundColor = ConsoleColor.Red;
                    else Console.ForegroundColor = ConsoleColor.White;

                    Console.Write(pathItem + " ");
                }
                Console.WriteLine();
            }

            Thread.Sleep(1); // Add delay for visualization purposes
        }
    }
}
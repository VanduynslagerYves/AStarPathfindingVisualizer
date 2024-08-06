using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace WPFFrontend
{
    public partial class MainWindow : Window
    {
        private int _width;
        private int _height;
        private Path[,] _grid;
        private AStar _aStar;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnStartPathfindingClick(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            GridDisplay.Children.Clear();

            var dispatcher = Dispatcher;
            var gridDisplay = GridDisplay;
            var costText = CostText;

            _width = gridDisplay.Columns;
            _height = gridDisplay.Rows;

            _grid = new Path[_width, _height];
            Random random = new Random();

            var walkableProbability = 0.7;

            // Initialize the grid with random walkable properties
            InitializeGrid(_grid, random, walkableProbability);

            _aStar = new AStar(_grid, gridDisplay, costText, dispatcher);

            var startX = random.Next(0, _width); // 0;
            var startY = random.Next(0, _height);// 0;
            var endX = random.Next(0, _width); //_width - 1;
            var endY = random.Next(0, _height); //_height - 1;

            // Ensure start and end are walkable
            if (!_grid[startX, startY].IsWalkable) _grid[startX, startY].IsWalkable = true;
            if (!_grid[endX, endY].IsWalkable) _grid[endX, endY].IsWalkable = true;

            await Task.Run(() => _aStar.FindPath(startX, startY, endX, endY));

            StartButton.IsEnabled = true;
        }

        private void InitializeGrid(Path[,] grid, Random random, double walkableProbability)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = new Path
                    {
                        IsWalkable = random.NextDouble() < walkableProbability,
                        Cost = random.Next(0, 5) //1
                    };
                }
            }
        }
    }

    public enum ElementType
    {
        Wall,
        Walkable,
        Goal,
        Traversed,
        Walked,
    }

    public class Path
    {
        public bool IsWalkable { get; set; }
        public int Cost { get; set; }
    }

    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }

        // GCost is the cost from the start node to this node.
        public int GCost { get; set; } = int.MaxValue; // Initialize to maximum value

        // HCost is the heuristic cost from this node to the end node.
        public int HCost { get; set; }

        // FCost is the total cost (GCost + HCost).
        public int FCost => GCost + HCost; // Total cost
        public Node Parent { get; set; }

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
        private UniformGrid _gridDisplay;
        private TextBox _costText;
        private Dispatcher _dispatcher;

        public AStar(Path[,] grid, UniformGrid gridDisplay, TextBox costText, Dispatcher dispatcher)
        {
            _grid = grid;
            _width = grid.GetLength(0);
            _height = grid.GetLength(1);
            _gridDisplay = gridDisplay;
            _costText = costText;
            _dispatcher = dispatcher;
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

        public List<Node> FindPath(int startX, int startY, int endX, int endY)
        {
            if (!IsInBounds(startX, startY) || !IsInBounds(endX, endY))
                return null;

            Node startNode = new Node(startX, startY) { GCost = 0 };
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

            startNode.HCost = Heuristic(startX, startY, endX, endY);

            while (openList.Count > 0)
            {
                Node currentNode = openList.OrderBy(node => node.FCost).ThenBy(node => node.HCost).First();

                if (currentNode.X == endX && currentNode.Y == endY)
                {
                    List<Node> finalPath = RetracePath(startNode, currentNode);
                    DrawGrid(finalPath, startNode, endNode);
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
                        DrawGrid(RetracePath(startNode, neighbor), startNode, endNode);

                        Thread.Sleep(50); // Add delay for visualization purposes
                    }
                }
            }

            return null; // No path found
        }

        private List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != null && currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private void DrawGrid(List<Node> path, Node startNode, Node endNode)
        {
            _dispatcher.Invoke(() =>
            {
                _gridDisplay.Children.Clear();

                ElementType[,] displayGrid = new ElementType[_width, _height];

                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        displayGrid[x, y] = _grid[x, y].IsWalkable ? ElementType.Walkable : ElementType.Wall;
                    }
                }

                if (_traversedNodes != null)
                {
                    foreach (Node node in _traversedNodes)
                    {
                        if (startNode.X == node.X && startNode.Y == node.Y) displayGrid[node.X, node.Y] = ElementType.Walked;
                        else displayGrid[node.X, node.Y] = ElementType.Traversed;
                    }
                }

                if (path != null)
                {
                    var totalCost = 0;
                    foreach (Node node in path)
                    {
                        displayGrid[node.X, node.Y] = ElementType.Walked;
                        totalCost += node.GCost;
                    }
                    displayGrid[endNode.X, endNode.Y] = ElementType.Goal;
                    displayGrid[startNode.X, startNode.Y] = ElementType.Walked;

                    _costText.Text = $"Total cost: {totalCost}";
                    //Debug.WriteLine($"Total cost: {totalCost}");
                }

                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        var pathItem = displayGrid[x, y];
                        TextBlock cell = new TextBlock
                        {
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 25,
                            Height = 25,
                        };

                        switch (pathItem)
                        {
                            case ElementType.Goal:
                                cell.Background = Brushes.Yellow;
                                break;
                            case ElementType.Walked:
                                cell.Background = Brushes.Green;
                                break;
                            case ElementType.Traversed:
                                cell.Background = Brushes.Red;
                                break;
                            case ElementType.Wall:
                                cell.Background = Brushes.Black;
                                break;
                            default:
                                cell.Background = Brushes.White;
                                break;
                        }

                        _gridDisplay.Children.Add(cell);
                    }
                }
            });
        }
    }
}

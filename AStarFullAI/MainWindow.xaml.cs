using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WPFFrontend.Pathfinding2
{
    public partial class MainWindow : Window
    {
        // Grid representation and parameters
        private int[,] grid;
        private int width, height;
        private Point start, end;
        private Random random = new Random();
        private const int CellSize = 20; // Size of each cell in pixels

        private Dictionary<Point, Rectangle> cellRectangles = new Dictionary<Point, Rectangle>();

        public MainWindow()
        {
            InitializeComponent();
        }

        // Event handler for the "Generate and Find Path" button
        private void GenerateAndFindPath_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            GenerateGrid();
            DrawGrid();
            FindPath();
        }

        // Validate user inputs
        private bool ValidateInputs()
        {
            // Try to parse all input values
            if (!int.TryParse(WidthTextBox.Text, out width) ||
                !int.TryParse(HeightTextBox.Text, out height) ||
                !double.TryParse(TraverseProbabilityTextBox.Text, out double traverseProbability) ||
                !int.TryParse(StartXTextBox.Text, out int startX) ||
                !int.TryParse(StartYTextBox.Text, out int startY) ||
                !int.TryParse(EndXTextBox.Text, out int endX) ||
                !int.TryParse(EndYTextBox.Text, out int endY))
            {
                MessageBox.Show("Invalid input. Please enter valid numbers.");
                return false;
            }

            start = new Point(startX, startY);
            end = new Point(endX, endY);

            // Check if start and end positions are within the grid
            if (startX < 0 || startX >= width || startY < 0 || startY >= height ||
                endX < 0 || endX >= width || endY < 0 || endY >= height)
            {
                MessageBox.Show("Start and end positions must be within the grid.");
                return false;
            }

            // Check if traverse probability is between 0 and 1
            if (traverseProbability < 0 || traverseProbability > 1)
            {
                MessageBox.Show("Traverse probability must be between 0 and 1.");
                return false;
            }

            return true;
        }

        // Generate the grid based on user inputs
        private void GenerateGrid()
        {
            grid = new int[width, height];
            double traverseProbability = double.Parse(TraverseProbabilityTextBox.Text);

            // Populate the grid with random costs or obstacles
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (random.NextDouble() < traverseProbability)
                    {
                        grid[x, y] = random.Next(1, 10); // Random cost between 1 and 9
                    }
                    else
                    {
                        grid[x, y] = -1; // Non-traversable cell (obstacle)
                    }
                }
            }

            // Ensure start and end are traversable
            grid[(int)start.X, (int)start.Y] = 1;
            grid[(int)end.X, (int)end.Y] = 1;
        }

        // Draw the grid on the canvas
        private void DrawGrid()
        {
            GridCanvas.Children.Clear();
            cellRectangles.Clear();

            // Set the canvas size based on the grid dimensions
            GridCanvas.Width = width * CellSize;
            GridCanvas.Height = height * CellSize;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    if (grid[x, y] == -1)
                    {
                        rect.Fill = Brushes.Black;
                    }
                    else
                    {
                        rect.Fill = Brushes.White;
                    }

                    Canvas.SetLeft(rect, x * CellSize);
                    Canvas.SetTop(rect, y * CellSize);
                    GridCanvas.Children.Add(rect);
                    cellRectangles[new Point(x, y)] = rect;
                }
            }
        }

        // Draw a path on the grid canvas
        private void DrawPath(List<Point> path, Brush color)
        {
            foreach (var point in path)
            {
                if (cellRectangles.TryGetValue(point, out Rectangle rect))
                {
                    // If the rectangle is not already the target color, update it
                    if (rect.Fill != color)
                    {
                        rect.Fill = color;
                        rect.Opacity = 0.5;
                    }
                }
            }
        }

        private void ResetGridColors()
        {
            foreach (var kvp in cellRectangles)
            {
                Point point = kvp.Key;
                Rectangle rect = kvp.Value;

                if (grid[(int)point.X, (int)point.Y] == -1)
                {
                    rect.Fill = Brushes.Black;
                }
                else
                {
                    rect.Fill = Brushes.White;
                }
                rect.Opacity = 1;
            }
        }

        // Initiate the A* pathfinding algorithm
        private async void FindPath()
        {
            ResetGridColors();
            var path = await Task.Run(() => AStarSearch());
            if (path != null)
            {
                DrawPath(path, Brushes.Green);
            }
            else
            {
                MessageBox.Show("No path found!");
            }
        }

        // A* Search algorithm implementation
        private async Task<List<Point>> AStarSearch()
        {
            var openSet = new List<Point> { start };
            var closedSet = new HashSet<Point>();
            var cameFrom = new Dictionary<Point, Point>();
            var gScore = new Dictionary<Point, double>();
            var fScore = new Dictionary<Point, double>();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            while (openSet.Count > 0)
            {
                // Get the node with the lowest fScore
                var current = openSet.OrderBy(p => fScore.ContainsKey(p) ? fScore[p] : double.MaxValue).First();

                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    var tentativeGScore = gScore[current] + grid[(int)neighbor.X, (int)neighbor.Y];

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                    else if (tentativeGScore >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : double.MaxValue))
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);

                    // Visualize the current path being considered
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DrawPath(ReconstructPath(cameFrom, neighbor), Brushes.Red);
                    });

                    await Task.Delay(1); // Slow down the visualization
                }
            }

            return null; // No path found
        }

        // Heuristic function for A* (Manhattan distance)
        private double Heuristic(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        // Get valid neighbors for a given point
        private List<Point> GetNeighbors(Point p)
        {
            var neighbors = new List<Point>();
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = (int)p.X + dx[i];
                int ny = (int)p.Y + dy[i];

                // Check if the neighbor is within bounds and traversable
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && grid[nx, ny] != -1)
                {
                    neighbors.Add(new Point(nx, ny));
                }
            }

            return neighbors;
        }

        // Reconstruct the path from start to end
        private List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
        {
            var path = new List<Point> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }


        //private void DrawPath(List<Point> path, Brush color)
        //{
        //    foreach (var point in path)
        //    {
        //        Rectangle rect = new Rectangle
        //        {
        //            Width = CellSize,
        //            Height = CellSize,
        //            Fill = color,
        //            Opacity = 0.5
        //        };

        //        Canvas.SetLeft(rect, point.X * CellSize);
        //        Canvas.SetTop(rect, point.Y * CellSize);
        //        GridCanvas.Children.Add(rect);
        //    }
        //}
    }
}
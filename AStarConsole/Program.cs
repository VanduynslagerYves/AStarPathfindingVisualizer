namespace AStarPathfindingTest
{
    public class Program
    {
        private static void InitializeGrid(Path[,] grid, Random random, double walkableProbability)
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
                        Cost = 1
                    };
                }
            }
        }

        public static void Main()
        {
            int width = 25;
            int height = 25;
            Path[,] grid = new Path[width, height];
            Random random = new Random();

            // Initialize the grid with random walkable properties
            InitializeGrid(grid, random, 0.9); // 80% chance of being walkable

            AStar aStar = new AStar(grid);
            var randomX = random.Next(0, width);
            var randomY = random.Next(0, height);

            Console.Write("Press a key to start");
            Console.ReadLine();

            List<Node>? path = aStar.FindPath(0, 0, randomX, randomY);

            //if (path != null)
            //{
            //    Console.WriteLine("Path found:");
            //    foreach (Node node in path)
            //    {
            //        Console.WriteLine($"X: {node.X}, Y: {node.Y}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("No path found");
            //}
            if (path == null)
            {
                Console.WriteLine("No path found");
            }

            Console.ReadLine();
        }
    }
}
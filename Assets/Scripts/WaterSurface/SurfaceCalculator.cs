using UnityEngine;

namespace WaterSurface
{
    public class SurfaceCalculator : ISurfaceCalculator
    {
        public (float[,], float[,]) Step(float[,] grid, float[,] speedGrid, float deltaTime)
        {
            float[,] newGrid = new float[grid.GetLength(0), grid.GetLength(1)];
            for (var i = 0; i < grid.GetLength(0); i++)
            {
                for (var j = 0; j < grid.GetLength(1); j++)
                {
                    var neighbourAverage = CalculateNeighboursAverage(grid, i, j);
                    speedGrid[i, j] = (speedGrid[i, j] + (neighbourAverage - grid[i, j]) * 20f) * 0.995f;
                    newGrid[i, j] = grid[i, j] + speedGrid[i, j] * deltaTime;
                }
            }

            return (newGrid, speedGrid);
        }

        private float CalculateNeighboursAverage(float[,] grid, int i, int j)
        {
            var neighbourAverage = 0f;
            var neighboursCount = 0f;
            if (i > 0)
            {
                neighbourAverage += grid[i - 1, j];
                neighboursCount++;
                if (j > 0)
                {
                    neighbourAverage += GetDiagonalValue(grid[i, j], grid[i - 1, j - 1]);
                    neighboursCount++;
                }
                if (j < grid.GetLength(1) - 1)
                {
                        
                    neighbourAverage += GetDiagonalValue(grid[i, j], grid[i - 1, j + 1]);
                    neighboursCount++;
                }
            }

            if (i < grid.GetLength(0) - 1)
            {
                neighbourAverage += grid[i + 1, j];
                neighboursCount++;

                if (j > 0)
                {
                    neighbourAverage += GetDiagonalValue(grid[i, j], grid[i + 1, j - 1]);
                    neighboursCount++;
                }

                if (j < grid.GetLength(1) - 1)
                {
                        
                    neighbourAverage += GetDiagonalValue(grid[i, j], grid[i + 1, j + 1]);
                    neighboursCount++;
                }
            }

            if (j > 0)
            {
                neighbourAverage += grid[i, j - 1];
                neighboursCount++;
            }

            if (j < grid.GetLength(1) - 1)
            {
                        
                neighbourAverage += grid[i, j + 1];
                neighboursCount++;
            }
            neighbourAverage /= neighboursCount;
            return neighbourAverage;
        }

        private float GetDiagonalValue(float val, float diagonalVal)
        {
            return val + (diagonalVal - val) / 1.41421356237f;
        }
    }
}

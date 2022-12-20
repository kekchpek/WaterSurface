using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterSurface
{
    [Obsolete]
    public class SurfaceCalculatorCPU
    {
        public (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) Step(float[] grid, float[] speedGrid, 
            float deltaTime, int gridW, int gridH)
        {
            var newGrid = new float[grid.Length];
            for (var i = 0; i < gridH; i++)
            {
                for (var j = 0; j < gridW; j++)
                {
                    var index = gridW * i + j;
                    var neighbourAverage = CalculateNeighboursAverage(grid, index, gridW, gridH);
                    speedGrid[index] = (speedGrid[index] + (neighbourAverage - grid[index]) * 20f) * 0.995f;
                    newGrid[index] = grid[index] + speedGrid[index] * deltaTime;
                }
            }

            var normals = new Vector3[grid.Length];
            
            for (var i = 1; i < gridH - 1; i++)
            {
                for (var j = 1; j < gridW -1; j++)
                {
                    var index = i * gridW + j;
                    var neighbours = new List<Vector3>();
                    neighbours.Add(new Vector3(i, newGrid[index + 1], j + 1));
                    neighbours.Add(new Vector3(i + 1, newGrid[index + gridW], j));
                    neighbours.Add(new Vector3(i, newGrid[index - 1], j - 1));
                    neighbours.Add(new Vector3(i - 1, newGrid[index - gridW], j));
                    normals[index] = FindNormalOfVertex(new Vector3(i, grid[index], j), neighbours.ToArray());
                }
            }

            return (newGrid, speedGrid, normals );
        }

        private Vector3 FindNormalOfVertex(Vector3 vertex, params Vector3[] neighbours)
        {
            var normalsSum = Vector3.zero;
            for (var i = 0; i < neighbours.Length; i++)
            {
                var nextIndex = i < neighbours.Length - 1 ? i + 1 : 0;
                normalsSum += FindNormalOfTriangle(vertex, neighbours[i], neighbours[nextIndex]);
            }
            return (normalsSum / neighbours.Length).normalized;
        }

        private Vector3 FindNormalOfTriangle(Vector3 baseVertex, Vector3 vertex1, Vector3 vertex2)
        {
            return Vector3.Cross(vertex1 - baseVertex, vertex2 - baseVertex).normalized;
        }

        private float CalculateNeighboursAverage(float[] grid, int index, int w, int h)
        {
            var j = index % w;
            var i = index / w;
            var neighbourAverage = 0f;
            var neighboursCount = 0f;
            if (i > 0)
            {
                neighbourAverage += grid[index - w];
                neighboursCount++;
                if (j > 0)
                {
                    neighbourAverage += GetDiagonalValue(grid[index], grid[index - w - 1]);
                    neighboursCount++;
                }
                if (j < w - 1)
                {
                        
                    neighbourAverage += GetDiagonalValue(grid[index], grid[index - w + 1]);
                    neighboursCount++;
                }
            }

            if (i < h - 1)
            {
                neighbourAverage += grid[index + w];
                neighboursCount++;

                if (j > 0)
                {
                    neighbourAverage += GetDiagonalValue(grid[index], grid[index + w - 1]);
                    neighboursCount++;
                }

                if (j < w - 1)
                {
                        
                    neighbourAverage += GetDiagonalValue(grid[index], grid[index + w + 1]);
                    neighboursCount++;
                }
            }

            if (j > 0)
            {
                neighbourAverage += grid[index - 1];
                neighboursCount++;
            }

            if (j < w - 1)
            {
                        
                neighbourAverage += grid[index + 1];
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

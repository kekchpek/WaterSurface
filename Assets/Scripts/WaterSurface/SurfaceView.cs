using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterSurface
{
    public class SurfaceView : MonoBehaviour, IRaycastable
    {

        [SerializeField] private int N;

        private Vector3[] _grid;
        
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        
        private float[,] _heightGrid;
        private float[,] _speedGrid;
        private Vector3[] _normals;

        private ISurfaceCalculator _surfaceCalculator = new SurfaceCalculator();
        
        [SerializeField] private float waveHeight;

        private void Awake()
        {
            _grid = new Vector3[N * N];
            _heightGrid = new float[N, N];
            _speedGrid = new float[N, N];
            _normals = new Vector3[N * N];
            
            _meshFilter = gameObject.GetComponent<MeshFilter>();
            _meshCollider = gameObject.GetComponent<MeshCollider>();
            _mesh = new Mesh();
            var triangles = new int[6 * (N - 1) * (N - 1)];
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < N; j++)
                {
                    int vertexIndex = i * N + j;
                    if (i < N - 1 && j < N - 1)
                    {
                        var triangleStartIndex = (i * (N - 1) + j) * 6;
                        triangles[triangleStartIndex] = vertexIndex;
                        triangles[triangleStartIndex + 1] = vertexIndex + 1;
                        triangles[triangleStartIndex + 2] = vertexIndex + N;
                        // second triangle
                        triangles[triangleStartIndex + 3] = vertexIndex + N + 1;
                        triangles[triangleStartIndex + 4] = vertexIndex + N;
                        triangles[triangleStartIndex + 5] = vertexIndex + 1;
                    }

                    _grid[vertexIndex].x = i;
                    _grid[vertexIndex].z = j;
                    _heightGrid[i, j] = 5f;
                }
            }
            _mesh.vertices = _grid;
            _mesh.triangles = triangles;
        }

        private void CreateWave(int x, int y)
        {
            for (var i = -10; i < 10; i++)
            {
                for (var j = -10; j < 10; j++)
                {
                    var indexX = x + i;
                    var indexY = y + j;
                    if (indexX < 0 || indexX >= N || indexY < 0 || indexY >= N)
                    {
                        continue;
                    }
                    _heightGrid[indexX, indexY] 
                        += Mathf.Max(0f, waveHeight * Mathf.Cos(Mathf.Sqrt(i * i + j * j) * Mathf.PI / 20f));
                }
            }
        }

        private void FixedUpdate()
        {
            var newData = _surfaceCalculator.Step(_heightGrid, _speedGrid, Time.fixedDeltaTime);
            _heightGrid = newData.Item1;
            _speedGrid = newData.Item2;
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < N; j++)
                {
                    var index = i * N + j;
                    _grid[index].y = _heightGrid[i, j];
                }
            }
            for (var i = 1; i < N - 1; i++)
            {
                for (var j = 1; j < N -1; j++)
                {
                    var index = i * N + j;
                    var neighbours = new List<Vector3>();
                    neighbours.Add(_grid[index + 1]);
                    neighbours.Add(_grid[index + N]);
                    neighbours.Add(_grid[index - 1]);
                    neighbours.Add(_grid[index - N]);
                    _normals[index] = FindNormalOfVertex(_grid[index], neighbours.ToArray());
                }
            }
            _mesh.vertices = _grid;
            _mesh.normals = _normals;
            _meshCollider.sharedMesh = _mesh;
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

        public void OnRaycast(RaycastHit hitInfo)
        {
            CreateWave(Mathf.RoundToInt(hitInfo.point.x), Mathf.RoundToInt(hitInfo.point.z));
        }
    }
}
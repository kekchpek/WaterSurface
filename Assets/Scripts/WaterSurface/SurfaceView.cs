using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaterSurface
{
    public class SurfaceView : MonoBehaviour, IRaycastable
    {

        [SerializeField] private float _realSize;
        [SerializeField] private int _gridSize;

        private Vector3[] _grid;
        
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        
        private float[] _heightGrid;
        private float[] _speedGrid;

        private ISurfaceCalculator _surfaceCalculator;
        
        [SerializeField] private float _timeScale;
        [SerializeField] private int _callsScale;

        [SerializeField] private float _fluidityFactor = 30f;
        [SerializeField] private float _absorbFactor = 0.997f;

        [SerializeField] private float _waveStrength;

        [SerializeField] private int _waveSize;

        [SerializeField] private Collider[] _obstaclesColliders;

        private bool _firstCall = true;
        private bool _gridChanged;

        private float? _lastFluidityFactor;
        private float? _lastAbsorbFactor;
        

        private void Awake()
        {
            _surfaceCalculator = GetComponent<SurfaceCalculatorGPU>();
            _grid = new Vector3[_gridSize * _gridSize];
            _heightGrid = new float[_gridSize * _gridSize];
            _speedGrid = new float[_gridSize * _gridSize];
            
            _meshFilter = gameObject.GetComponent<MeshFilter>();
            _mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };
            var triangles = new int[6 * (_gridSize - 1) * (_gridSize - 1)];
            _meshFilter.sharedMesh = _mesh;

            var step = _realSize / (_gridSize - 1);
            for (var i = 0; i < _gridSize; i++)
            {
                for (var j = 0; j < _gridSize; j++)
                {
                    var vertexIndex = i * _gridSize + j;
                    if (i < _gridSize - 1 && j < _gridSize - 1)
                    {
                        var triangleStartIndex = (i * (_gridSize - 1) + j) * 6;
                        triangles[triangleStartIndex] = vertexIndex;
                        triangles[triangleStartIndex + 1] = vertexIndex + 1;
                        triangles[triangleStartIndex + 2] = vertexIndex + _gridSize;
                        // second triangle
                        triangles[triangleStartIndex + 3] = vertexIndex + _gridSize + 1;
                        triangles[triangleStartIndex + 4] = vertexIndex + _gridSize;
                        triangles[triangleStartIndex + 5] = vertexIndex + 1;
                    }

                    _grid[vertexIndex].x = step * i;
                    _grid[vertexIndex].z = step * j;
                    _grid[vertexIndex].y = 15f;
                    _heightGrid[vertexIndex] = 15f; 
                }
            }
            _mesh.vertices = _grid;
            _mesh.triangles = triangles;
            
            _surfaceCalculator.Initialize(_grid.Length);

            var activeMask = new int[_grid.Length];
            for (var i = 0; i < _grid.Length; i++)
            {
                activeMask[i] = Convert.ToInt32(_obstaclesColliders.All(x => !IsInside(x, _grid[i])));
            }
            _surfaceCalculator.SetActiveGridMask(activeMask);
        }

        private void CreateWave(int x, int y)
        {
            var halfWaveSize = _waveSize / 2;
            for (var i = -halfWaveSize; i < halfWaveSize; i++)
            {
                for (var j = -halfWaveSize; j < halfWaveSize; j++)
                {
                    var indexY = x + i;
                    var indexX = y + j;
                    var index = indexY * _gridSize + indexX;
                    if (indexX < 0 || indexX >= _gridSize || indexY < 0 || indexY >= _gridSize)
                    {
                        continue;
                    }
                    _heightGrid[index] 
                        += _waveStrength * Mathf.Max(0f, Mathf.Cos(Mathf.Sqrt(i * i + j * j) * Mathf.PI / _waveSize));
                }
            }

            _gridChanged = true;
        }

        private void FixedUpdate()
        {
            if (_lastFluidityFactor != _fluidityFactor)
            {
                _surfaceCalculator.SetFluidityFactor(_fluidityFactor);
                _lastFluidityFactor = _fluidityFactor;
            }

            if (_lastAbsorbFactor != _absorbFactor)
            {
                _surfaceCalculator.SetAbsorbFactor(_absorbFactor);
                _lastAbsorbFactor = _absorbFactor;
            }
            
            var time = Time.fixedDeltaTime * _timeScale;
            for (var callN = 0; callN < _callsScale; callN++)
            {
                (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) newData;
                if (_firstCall)
                {
                    newData =
                        _surfaceCalculator.Step(time, _gridSize, _gridSize, _heightGrid, _speedGrid);
                    _firstCall = false;
                } 
                else if (_gridChanged)
                {
                    newData = _surfaceCalculator.Step(time, grid: _heightGrid);
                    _gridChanged = false;
                }
                else
                {
                    newData = _surfaceCalculator.Step(time);
                }

                _heightGrid = newData.newGrid;
                _speedGrid = newData.newSpeedGrid;
                for (var i = 0; i < _gridSize; i++)
                {
                    for (var j = 0; j < _gridSize; j++)
                    {
                        var index = i * _gridSize + j;
                        _grid[index].y = _heightGrid[index];
                    }
                }
                _mesh.vertices = _grid;
                _mesh.normals = newData.newGridNormals;
            }
        }
        
        private bool IsInside(Collider c, Vector3 point)
        {
            var closest = c.ClosestPoint(point);
            return closest == point;
        }

        public void OnRaycast(RaycastHit hitInfo)
        {
            var factor = _gridSize / _realSize;
            CreateWave(Mathf.RoundToInt(hitInfo.point.x * factor), Mathf.RoundToInt(hitInfo.point.z * factor));
        }
    }
}
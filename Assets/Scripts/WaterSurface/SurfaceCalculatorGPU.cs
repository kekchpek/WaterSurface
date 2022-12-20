using System;
using System.Linq;
using UnityEngine;

namespace WaterSurface
{
    public class SurfaceCalculatorGPU : MonoBehaviour, ISurfaceCalculator
    {
        private const int StripCount = 4;
        
        [SerializeField]
        private ComputeShader _waterComputeShader;

        private bool _buffersInited;
        private bool _factorBuffersInited;
        
        private ComputeBuffer _gridHeightBuffer;
        private ComputeBuffer _speedGridBuffer;
        private ComputeBuffer _gridSizeBuffer;
        private ComputeBuffer _deltaTimeBuffer;
        private ComputeBuffer _outputNormalsGridBuffer;
        private ComputeBuffer _newHeightGridBuffer;
        private ComputeBuffer _absorbBuffer;
        private ComputeBuffer _fluidityBuffer;
        private ComputeBuffer _gridActiveMaskBuffer;

        private int _gridSize;
        private int _gridH = -1;
        private int _gridW = -1;

        private float[] _cachedGrid;
        private float[] _cachedSpeeds;

        public void Initialize(int gridSize)
        {
            if (_buffersInited)
                return;

            int size = gridSize * 4;
            
            _gridHeightBuffer = new ComputeBuffer(size, 4);
            _speedGridBuffer = new ComputeBuffer(size, 4);
            _gridSizeBuffer = new ComputeBuffer(2, 4);
            _deltaTimeBuffer = new ComputeBuffer(1, 4);
            _outputNormalsGridBuffer = new ComputeBuffer(size, 12);
            _newHeightGridBuffer = new ComputeBuffer(size, 4);
            _gridActiveMaskBuffer = new ComputeBuffer(size, 4);
            
            _absorbBuffer = new ComputeBuffer(4, 4);
            _fluidityBuffer = new ComputeBuffer(4, 4);
            
            _waterComputeShader.SetBuffer(0, "HeightGridBuffer", _gridHeightBuffer);
            _waterComputeShader.SetBuffer(2, "HeightGridBuffer", _gridHeightBuffer);
            
            _waterComputeShader.SetBuffer(0, "GridActiveMaskBuffer", _gridActiveMaskBuffer);
            _waterComputeShader.SetBuffer(1, "GridActiveMaskBuffer", _gridActiveMaskBuffer);
            _waterComputeShader.SetBuffer(2, "GridActiveMaskBuffer", _gridActiveMaskBuffer);

            _waterComputeShader.SetBuffer(0, "SpeedGridBuffer", _speedGridBuffer);
            
            _waterComputeShader.SetBuffer(0, "GridSizeBuffer", _gridSizeBuffer);
            _waterComputeShader.SetBuffer(1, "GridSizeBuffer", _gridSizeBuffer);
            _waterComputeShader.SetBuffer(2, "GridSizeBuffer", _gridSizeBuffer);
            
            _waterComputeShader.SetBuffer(0, "DeltaTimeBuffer", _deltaTimeBuffer);
            _waterComputeShader.SetBuffer(1, "OutputNormalsGridBuffer", _outputNormalsGridBuffer);
            
            _waterComputeShader.SetBuffer(0, "NewHeightGrid", _newHeightGridBuffer);
            _waterComputeShader.SetBuffer(1, "NewHeightGrid", _newHeightGridBuffer);
            _waterComputeShader.SetBuffer(2, "NewHeightGrid", _newHeightGridBuffer);
        
            _waterComputeShader.SetBuffer(0, "AbsorbBuffer", _absorbBuffer);
        
            _waterComputeShader.SetBuffer(0, "FluidityBuffer", _fluidityBuffer);
            
            _buffersInited = true;
        }

        public (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) Step(float deltaTime,
            int? gridW = null, int? gridH = null, float[] grid = null, float[] speedGrid = null)
        {

            if (_gridH == -1 || _gridW == -1)
            {
                if (grid == null || speedGrid == null || gridH == null || gridW == null)
                    throw new InvalidOperationException("First time calculation started all arguments should be specified.");
                _gridH = gridH.Value;
                _gridW = gridW.Value;
                _gridSize = gridW!.Value * gridH!.Value;
            }
            else
            {
                if (gridH != null || gridW != null)
                {
                    throw new InvalidOperationException("Grid size can not be changed!");
                }
            }
            
            return CalculateBatch(deltaTime, grid, speedGrid);
        }

        public void SetFluidityFactor(float fluidityFactor)
        {
            _fluidityBuffer.SetData(new [] { fluidityFactor });
        }

        public void SetAbsorbFactor(float absorbFactor)
        {
            _absorbBuffer.SetData(new [] { absorbFactor });
        }

        public void SetActiveGridMask(int[] mask)
        {
            _gridActiveMaskBuffer.SetData(mask);
        }

        private (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) 
            CalculateBatch(float deltaTime, float[] grid = null, float[] speedGrid = null, int gridStartIndex = 0,
                int gridElementsCount = -1)
        {
            var gridSize = _gridSize;
            // INPUT
            if (grid != null)
            {
                if (gridElementsCount == -1)
                {
                    gridElementsCount = grid.Length;
                }
                gridSize = gridElementsCount;
                _gridSizeBuffer.SetData(new[] {_gridW, gridSize / _gridW});
                _gridHeightBuffer.SetData(grid, gridStartIndex, 0, gridElementsCount);
            }

            if (speedGrid != null)
            {
                _speedGridBuffer.SetData(speedGrid, gridStartIndex, 0, gridElementsCount);
            }
            
            _deltaTimeBuffer.SetData(new[] { deltaTime });
            
            // OUTPUT
            _waterComputeShader.Dispatch(0, gridSize / 1024 + 1, 1, 1);
            _waterComputeShader.Dispatch(1, gridSize / 1024 + 1, 1, 1);
            _waterComputeShader.Dispatch(2, gridSize / 1024 + 1, 1, 1);

            var newGridHeight = new float[gridSize];
            _gridHeightBuffer.GetData(newGridHeight);
            var newGridSpeed = new float[gridSize];
            _speedGridBuffer.GetData(newGridSpeed);
            var normals = new Vector3[gridSize];
            _outputNormalsGridBuffer.GetData(normals);
                
            return (newGridHeight, newGridSpeed, normals);
        }

        private void OnDestroy()
        {
            _gridHeightBuffer.Dispose();
            _speedGridBuffer.Dispose();
            _gridSizeBuffer.Dispose();
            _deltaTimeBuffer.Dispose();
            _outputNormalsGridBuffer.Dispose();
            _newHeightGridBuffer.Dispose();
        }
    }
}
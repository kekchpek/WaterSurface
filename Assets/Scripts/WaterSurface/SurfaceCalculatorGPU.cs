using System;
using UnityEngine;

namespace WaterSurface
{
    public class SurfaceCalculatorGPU : MonoBehaviour, ISurfaceCalculator
    {

        private const int MaxGridBatchSize = 256;
        private const int StripCount = 4;
        
        [SerializeField]
        private ComputeShader _waterComputeShader;

        private bool _buffersInited;
        
        private ComputeBuffer _gridHeightBuffer;
        private ComputeBuffer _speedGridBuffer;
        private ComputeBuffer _gridSizeBuffer;
        private ComputeBuffer _deltaTimeBuffer;
        private ComputeBuffer _outputNormalsGridBuffer;
        private ComputeBuffer _newHeightGridBuffer;

        private int _gridSize;
        private int _gridH;
        private int _gridW;

        private float[] _cachedGrid;
        private float[] _cachedSpeeds;

        private void InitBuffers(int size)
        {
            _gridHeightBuffer = new ComputeBuffer(size, 4);
            _speedGridBuffer = new ComputeBuffer(size, 4);
            _gridSizeBuffer = new ComputeBuffer(2, 4);
            _deltaTimeBuffer = new ComputeBuffer(1, 4);
            _outputNormalsGridBuffer = new ComputeBuffer(size, 12);
            _newHeightGridBuffer = new ComputeBuffer(size, 4);
            _waterComputeShader.SetBuffer(0, "HeightGridBuffer", _gridHeightBuffer);
            _waterComputeShader.SetBuffer(2, "HeightGridBuffer", _gridHeightBuffer);
            _waterComputeShader.SetBuffer(0, "SpeedGridBuffer", _speedGridBuffer);
            _waterComputeShader.SetBuffer(0, "GridSizeBuffer", _gridSizeBuffer);
            _waterComputeShader.SetBuffer(1, "GridSizeBuffer", _gridSizeBuffer);
            _waterComputeShader.SetBuffer(2, "GridSizeBuffer", _gridSizeBuffer);
            _waterComputeShader.SetBuffer(0, "DeltaTimeBuffer", _deltaTimeBuffer);
            _waterComputeShader.SetBuffer(1, "OutputNormalsGridBuffer", _outputNormalsGridBuffer);
            _waterComputeShader.SetBuffer(0, "NewHeightGrid", _newHeightGridBuffer);
            _waterComputeShader.SetBuffer(1, "NewHeightGrid", _newHeightGridBuffer);
            _waterComputeShader.SetBuffer(2, "NewHeightGrid", _newHeightGridBuffer);
            _buffersInited = true;
        }

        public (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) Step(float deltaTime,
            int? gridW = null, int? gridH = null, float[] grid = null, float[] speedGrid = null)
        {

            if (!_buffersInited)
            {
                if (grid == null || speedGrid == null || gridH == null || gridW == null)
                    throw new InvalidOperationException("First time calculation started all arguments should be specified.");
                
                
                _gridH = gridH.Value;
                _gridW = gridW.Value;
                _gridSize = gridW!.Value * gridH!.Value;

                if (gridW > MaxGridBatchSize || gridH > MaxGridBatchSize)
                {
                    InitBuffers(gridH.Value * gridW.Value / StripCount);
                }
                else
                {
                    InitBuffers(gridH.Value * gridW.Value);
                }
                
                
            }
            else
            {
                if (gridH != null || gridW != null)
                {
                    throw new InvalidOperationException("Grid size can not be changed!");
                }
            }

            if (_gridH > MaxGridBatchSize || gridH > MaxGridBatchSize)
            {
                return StripAndCalculate(deltaTime, grid, speedGrid);
            }
            else
            {
                return CalculateBatch(deltaTime, grid, speedGrid);
            }
        }

        private (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) StripAndCalculate(float deltaTime, float[] grid = null, float[] speedGrid = null)
        {
            if (grid != null)
            {
                _cachedGrid = grid;
            }

            if (speedGrid != null)
            {
                _cachedSpeeds = speedGrid;
            }

            var resultGrid = new float[_cachedGrid.Length];
            var resultSpeed = new float[_cachedGrid.Length];
            var resultNormals = new Vector3[_cachedGrid.Length];

            var batchGridHeight = _gridH / StripCount;
            var processingHeight = 0;
            while (processingHeight < _gridH)
            {
                var countToCalculate = _gridW * batchGridHeight;
                var startIndex = processingHeight * _gridW;
                var calculatedData = CalculateBatch(deltaTime, _cachedGrid, _cachedSpeeds,
                    startIndex,
                    Math.Min(countToCalculate, _cachedGrid.Length - startIndex));

                var copyHeight = processingHeight;
                var startCopyIndex = 0;
                if (processingHeight > 0)
                {
                    startCopyIndex = _gridW;
                    copyHeight++;
                }
                Array.Copy(calculatedData.newGrid, startCopyIndex,
                    resultGrid, copyHeight * _gridW,
                    calculatedData.newGrid.Length - startCopyIndex);
                Array.Copy(calculatedData.newSpeedGrid, startCopyIndex,
                    resultSpeed, copyHeight * _gridW,
                    calculatedData.newSpeedGrid.Length - startCopyIndex);
                Array.Copy(calculatedData.newGridNormals, startCopyIndex,
                    resultNormals, copyHeight * _gridW,
                    calculatedData.newGridNormals.Length - startCopyIndex);
                processingHeight += batchGridHeight - 2;
            }

            _cachedGrid = resultGrid;
            _cachedSpeeds = resultSpeed;
            
            return (resultGrid, resultSpeed, resultNormals);
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
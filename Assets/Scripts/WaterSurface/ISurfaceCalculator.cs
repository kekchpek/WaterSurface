using UnityEngine;

namespace WaterSurface
{
    public interface ISurfaceCalculator
    {

        void Initialize(int gridSize);
        
        (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) Step(float deltaTime,
            int? gridW = null, int? gridH = null, float[] grid = null, float[] speedGrid = null);

        void SetFluidityFactor(float fluidityFactor);

        void SetAbsorbFactor(float absorbFactor);

        void SetActiveGridMask(int[] mask);

    }
}
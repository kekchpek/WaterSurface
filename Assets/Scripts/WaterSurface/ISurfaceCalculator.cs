using UnityEngine;

namespace WaterSurface
{
    public interface ISurfaceCalculator
    {
        (float[] newGrid, float[] newSpeedGrid, Vector3[] newGridNormals) Step(float deltaTime,
            int? gridW = null, int? gridH = null, float[] grid = null, float[] speedGrid = null);
    }
}
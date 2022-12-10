namespace WaterSurface
{
    public interface ISurfaceCalculator
    {
        (float[,], float[,]) Step(float[,] grid, float[,] speedGrid, float deltaTime);
    }
}
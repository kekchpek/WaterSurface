using UnityEngine;

namespace WaterSurface
{
    public interface IRaycastable
    {
        void OnRaycast(RaycastHit hitInfo);
    }
}
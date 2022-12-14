using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace WaterSurface.RayTracingAccelerator
{
    public class RayTracingAccelerator : MonoBehaviour
    {
        private void Awake()
        {
            var settings = new RayTracingAccelerationStructure.RASSettings();
            settings.layerMask = -1; // all layers
            settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
            settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

            var accelerationStructure = new RayTracingAccelerationStructure(settings);
            
            accelerationStructure.Build(); 
        }
    }
}
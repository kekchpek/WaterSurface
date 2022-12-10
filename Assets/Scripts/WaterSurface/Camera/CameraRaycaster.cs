using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WaterSurface
{
    public class CameraRaycaster : MonoBehaviour
    {

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void FixedUpdate()
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Input.GetMouseButton(0) && Physics.Raycast(ray, out var hit, 100000f))
            {
                hit.transform.GetComponent<IRaycastable>()?.OnRaycast(hit);
            }
        }
    }
}
using UnityEngine;

namespace WaterSurface
{
    public class CameraController : MonoBehaviour
    {

        [SerializeField] private float _speed;

        private bool _rotating;

        private Vector3? _cachedMousePos;

        private void FixedUpdate()
        {
            var vert = Input.GetAxis("Vertical");
            var hor = Input.GetAxis("Horizontal");
            var upDown = Input.GetAxis("UpDown");
            var cachedTransform = transform;

            var speed = _speed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= 2;
            }
            
            cachedTransform.position +=  cachedTransform.rotation * new Vector3(hor, upDown, vert) * (speed * Time.fixedDeltaTime);

            if (Input.GetMouseButton(1))
            {
                _rotating = true;
                Cursor.visible = false;
                _cachedMousePos ??= Input.mousePosition;
            } else {
                _rotating = false;
                Cursor.visible = true;
                _cachedMousePos = null;
            }

            if (_rotating)
            {
                var delta = Input.mousePosition - _cachedMousePos!.Value;
                _cachedMousePos = Input.mousePosition;
                cachedTransform.Rotate(-Vector3.right, delta.y);
                cachedTransform.Rotate(Vector3.up, delta.x, Space.World);
            }
            
        }
    }
}
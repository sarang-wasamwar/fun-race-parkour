// BillboardLabel.cs
// Makes a world-space TMP label always face the main camera.

using UnityEngine;

namespace FunRaceParkour
{
    public class BillboardLabel : MonoBehaviour
    {
        private static Camera _cam;

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.forward = _cam.transform.forward;
        }
    }
}

using UnityEngine;

namespace HQ
{
    [ExecuteInEditMode]
    public class HqCameraFollow: MonoBehaviour
    {
        public ProjectedBody body;
        public HqRenderer hQCamera;
        public HqRearRenderer hQRearCamera;

        private void Update()
        {
#if UNITY_EDITOR
            Follow();
#else
            Follow();
#endif
        }

        private void Follow()
        {
            if (hQCamera != null)
            {
                hQCamera.cameraOffset = body.playerX;
                hQCamera.trip = body.trip;
            }
            if (hQRearCamera != null)
            {
                hQRearCamera.cameraOffset = body.playerX;
                hQRearCamera.trip = body.trip;
            }
        }
    }
}

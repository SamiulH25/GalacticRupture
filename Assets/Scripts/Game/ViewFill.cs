using UnityEngine;

namespace GalacticRupture.Game
{
    // A faint cool light from behind the camera, so the shadowed side of a hull still reads
    // as a shape. The key light (the star) does the drama; this keeps the game legible.
    [ExecuteAlways]
    public sealed class ViewFill : MonoBehaviour
    {
        public Transform Camera;
        public Vector3 Offset = new Vector3(18f, -25f, 0f);

        void LateUpdate()
        {
            if (Camera != null) transform.rotation = Camera.rotation * Quaternion.Euler(Offset);
        }
    }
}

using UnityEngine;

namespace HurricaneVR.Framework.Core.Utils
{
    public class HVRSetAllRBInterpolate : MonoBehaviour
    {
        public void Awake()
        {
            foreach (var rb in FindObjectsOfType<Rigidbody>())
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }
}
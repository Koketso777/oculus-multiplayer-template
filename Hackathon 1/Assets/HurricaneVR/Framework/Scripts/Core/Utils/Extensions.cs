using System.Collections;
using HurricaneVR.Framework.Shared;
using UnityEngine;

namespace HurricaneVR.Framework.Core.Utils
{
    public static class Extensions
    {

        public static Bounds GetRendererBounds(this Transform transform, bool requireEnabled = true)
        {
            var bounds = new Bounds(transform.position, Vector3.zero);
            var renderers = transform.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (!requireEnabled || r.enabled)
                    bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }

        public static void ResetLocalProps(this Transform transform, bool resetScale = true)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (resetScale)
                transform.localScale = Vector3.one;
        }

        public static void SetLayerRecursive(this Transform transform, HVRLayers layer, Transform except = null)
        {
            var newLayer = LayerMask.NameToLayer(layer.ToString());
            if (newLayer < 0)
                return;

            SetLayerRecursive(transform, newLayer, except);
        }

        public static void SetLayerRecursive(this Transform transform, int newLayer, Transform except = null)
        {
            if (!transform || transform == except)
                return;

            transform.gameObject.layer = newLayer;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursive(transform.GetChild(i), newLayer, except);
            }
        }

        public static IEnumerator SetLayerTimeout(Transform transform, HVRLayers layer, float timeout)
        {
            yield return new WaitForSeconds(timeout);
            transform.SetLayerRecursive(layer);
        }
    }
}


using System.Collections;
using UnityEngine;

namespace HurricaneVR.Framework.Core.Utils
{
    public static class PhysicsExtensions
    {
        public static IEnumerator IgnoreCollisionForSeconds(this Collider[] colliders, Collider[] otherColliders, float seconds)
        {
            foreach (var collider in colliders)
            {
                if (!collider)
                    continue;

                foreach (var otherCollider in otherColliders)
                {
                    if (!otherCollider)
                        continue;
                    Physics.IgnoreCollision(collider, otherCollider, true);
                }
            }

            yield return new WaitForSeconds(seconds);

            foreach (var collider in colliders)
            {
                if (!collider)
                    continue;

                foreach (var otherCollider in otherColliders)
                {
                    if (!otherCollider)
                        continue;
                    Physics.IgnoreCollision(collider, otherCollider, false);
                }
            }
        }
    }
}
using HurricaneVR.Framework.Core.Player;
using UnityEngine;

namespace HurricaneVR.TechDemo.Scripts
{
    public class DemoManualTeleport : MonoBehaviour
    {
        public Transform PositionOne;
        public Transform PositionTwo;
        public HVRTeleporter Teleporter;

        public void GoToOne()
        {
            if (Teleporter && PositionOne)
            {
                Teleporter.Teleport(PositionOne.position, PositionOne.forward);
            }
        }

        public void GoToTwo()
        {
            if (Teleporter && PositionTwo)
            {
                Teleporter.Teleport(PositionTwo.position, PositionTwo.forward);
            }
        }
    }
}

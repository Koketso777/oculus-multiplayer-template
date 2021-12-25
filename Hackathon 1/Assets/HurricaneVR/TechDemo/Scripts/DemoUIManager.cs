﻿using System.Linq;
using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Player;
using HurricaneVR.Framework.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HurricaneVR.TechDemo.Scripts
{
    public class DemoUIManager : MonoBehaviour
    {
        public HVRPlayerController Player;
        public HVRCameraRig CameraRig;
        public HVRPlayerInputs Inputs;

        public TextMeshProUGUI SitStandText;
        public TextMeshProUGUI ForceGrabText;
        public TextMeshProUGUI LeftForceText;
        public TextMeshProUGUI RightForceText;
        public Slider TurnRateSlider;
        public Slider SnapTurnSlider;
        public TextMeshProUGUI TurnRateText;
        public TextMeshProUGUI SnapRateText;
        public Toggle SmoothTurnToggle;
        public Toggle LineGrabTrigger;

        public HVRForceGrabber LeftForce;
        public HVRForceGrabber RightForce;

        void Start()
        {
            UpdateSitStandButton();
            UpdateForceGrabButton();
            TurnRateSlider.value = Player.SmoothTurnSpeed;
            SnapTurnSlider.value = Player.SnapAmount;

            TurnRateText.text = Player.SmoothTurnSpeed.ToString();
            SnapRateText.text = Player.SnapAmount.ToString();

            SmoothTurnToggle.isOn = Player.RotationType == RotationType.Smooth;
            LineGrabTrigger.isOn = HVRSettings.Instance.LineGrabTriggerLoose;

            TurnRateSlider.onValueChanged.AddListener(OnTurnRateChanged);
            SnapTurnSlider.onValueChanged.AddListener(OnSnapTurnRateChanged);
            SmoothTurnToggle.onValueChanged.AddListener(OnSmoothTurnChanged);
            LineGrabTrigger.onValueChanged.AddListener(OnLineGrabTriggerChanged);

            LeftForce = Player.transform.root.GetComponentsInChildren<HVRForceGrabber>().FirstOrDefault(e => e.HandSide == HVRHandSide.Left);
            RightForce = Player.transform.root.GetComponentsInChildren<HVRForceGrabber>().FirstOrDefault(e => e.HandSide == HVRHandSide.Right);

            UpdateLeftForceButton();
            UpdateRightForceButton();
        }

        private void OnLineGrabTriggerChanged(bool arg0)
        {
            HVRSettings.Instance.LineGrabTriggerLoose = arg0;
        }

        public void CalibrateHeight()
        {
            if (CameraRig)
                CameraRig.Calibrate();
        }

        public void OnSitStandClicked()
        {
            var index = (int)CameraRig.SitStanding;
            index++;
            if (index > 2)
            {
                index = 0;
            }

            CameraRig.SetSitStandMode((HVRSitStand)index);
            UpdateSitStandButton();
        }

        public void OnForceGrabClicked()
        {
            var index = (int)Inputs.ForceGrabActivation;
            index++;
            if (index > 1)
            {
                index = 0;
            }

            Inputs.ForceGrabActivation = (HVRForceGrabActivation)index;
            UpdateForceGrabButton();
        }

        private void UpdateForceGrabButton()
        {
            ForceGrabText.text = Inputs.ForceGrabActivation.ToString();
        }

        private void UpdateSitStandButton()
        {
            SitStandText.text = CameraRig.SitStanding.ToString();
        }

        public void OnTurnRateChanged(float rate)
        {
            Player.SmoothTurnSpeed = rate;
            TurnRateText.text = Player.SmoothTurnSpeed.ToString();
        }

        public void OnSnapTurnRateChanged(float rate)
        {
            Player.SnapAmount = rate;
            SnapRateText.text = Player.SnapAmount.ToString();
        }

        public void OnSmoothTurnChanged(bool smooth)
        {
            Player.RotationType = smooth ? RotationType.Smooth : RotationType.Snap;
        }

        public void OnLeftForceGrabModeClicked()
        {
            if (LeftForce)
            {
                if (LeftForce.GrabStyle == HVRForceGrabMode.ForcePull)
                {
                    LeftForce.GrabStyle = HVRForceGrabMode.GravityGloves;
                }
                else
                {
                    LeftForce.GrabStyle = HVRForceGrabMode.ForcePull;
                }

                UpdateLeftForceButton();
            }
        }

        public void OnRightForceGrabModeClicked()
        {
            if (RightForce)
            {
                if (RightForce.GrabStyle == HVRForceGrabMode.ForcePull)
                {
                    RightForce.GrabStyle = HVRForceGrabMode.GravityGloves;
                }
                else
                {
                    RightForce.GrabStyle = HVRForceGrabMode.ForcePull;
                }

                UpdateRightForceButton();
            }
        }

        private void UpdateLeftForceButton()
        {
            LeftForceText.text = LeftForce.GrabStyle.ToString();
        }

        private void UpdateRightForceButton()
        {
            RightForceText.text = RightForce.GrabStyle.ToString();
        }
    }
}

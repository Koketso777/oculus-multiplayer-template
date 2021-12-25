#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HurricaneVR.Framework.ControllerInput
{

    public class HVRInputSystemController : HVRController
    {
        public static HVRInputActions InputActions = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Cleanup()
        {
            if (InputActions != null)
            {
                InputActions.Disable();
                InputActions.Dispose();
                InputActions = null;
            }
        }

        public static void Init()
        {
            if (InputActions == null)
            {
                InputActions = new HVRInputActions();
                InputActions.Enable();
            }
        }

        protected override void Start()
        {
            base.Start();
            Init();
        }

        protected override void UpdateInput()
        {
            if (Side == HVRHandSide.Left)
            {
                JoystickAxis = InputActions.LeftHand.Primary2DAxis.ReadValue<Vector2>();

                SetBool(out JoystickClicked, InputActions.LeftHand.Primary2DAxisClick);
                SetBool(out TrackPadClicked, InputActions.LeftHand.Secondary2DAxisClick);

                TrackpadAxis = InputActions.LeftHand.Secondary2DAxis.ReadValue<Vector2>();

                Grip = InputActions.LeftHand.Grip.ReadValue<float>();
                GripForce = InputActions.LeftHand.GripForce.ReadValue<float>();
                Trigger = InputActions.LeftHand.Trigger.ReadValue<float>();

                SetBool(out PrimaryButton, InputActions.LeftHand.PrimaryButton);
                SetBool(out SecondaryButton, InputActions.LeftHand.SecondaryButton);

                SetBool(out PrimaryTouch, InputActions.LeftHand.PrimaryTouch);
                SetBool(out SecondaryTouch, InputActions.LeftHand.SecondaryTouch);

                SetBool(out JoystickTouch, InputActions.LeftHand.Primary2DAxisTouch);
                SetBool(out TrackPadTouch, InputActions.LeftHand.Secondary2DAxisTouch);

                SetBool(out TriggerTouch, InputActions.LeftHand.TriggerTouch);

                SetBool(out MenuButton, InputActions.LeftHand.Menu);

                SetBool(out GripButton, InputActions.LeftHand.GripPress);
                SetBool(out TriggerButton, InputActions.LeftHand.TriggerPress);
            }
            else
            {
                JoystickAxis = InputActions.RightHand.Primary2DAxis.ReadValue<Vector2>();

                SetBool(out JoystickClicked, InputActions.RightHand.Primary2DAxisClick);
                SetBool(out TrackPadClicked, InputActions.RightHand.Secondary2DAxisClick);

                TrackpadAxis = InputActions.RightHand.Secondary2DAxis.ReadValue<Vector2>();

                Grip = InputActions.RightHand.Grip.ReadValue<float>();
                GripForce = InputActions.RightHand.GripForce.ReadValue<float>();
                Trigger = InputActions.RightHand.Trigger.ReadValue<float>();

                SetBool(out PrimaryButton, InputActions.RightHand.PrimaryButton);
                SetBool(out SecondaryButton, InputActions.RightHand.SecondaryButton);

                SetBool(out PrimaryTouch, InputActions.RightHand.PrimaryTouch);
                SetBool(out SecondaryTouch, InputActions.RightHand.SecondaryTouch);

                SetBool(out JoystickTouch, InputActions.RightHand.Primary2DAxisTouch);
                SetBool(out TrackPadTouch, InputActions.RightHand.Secondary2DAxisTouch);

                SetBool(out TriggerTouch, InputActions.RightHand.TriggerTouch);

                SetBool(out MenuButton, InputActions.RightHand.Menu);

                SetBool(out GripButton, InputActions.RightHand.GripPress);
                SetBool(out TriggerButton, InputActions.RightHand.TriggerPress);
            }

        }

        private void SetBool(out bool val, InputAction action)
        {
            val = false;
            if (action.activeControl != null)
            {
                var type = action.activeControl.valueType;
                if (type == typeof(bool))
                {
                    val = action.ReadValue<bool>();
                }
                else if (type == typeof(float))
                {
                    val = action.ReadValue<float>() > .5f;
                }
            }
        }
    }
}

#endif
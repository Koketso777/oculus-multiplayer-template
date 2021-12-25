using System.Collections;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.ScriptableObjects;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace HurricaneVR.Framework.Core.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class HVRPhysicsHands : MonoBehaviour
    {
        [Tooltip("Target transform for position and rotation tracking")]
        public Transform Target;
        public HVRJointSettings JointSettings;
        public Rigidbody ParentRigidBody;

        public int SolverIterations = 10;
        public int SolverVelocityIterations = 10;

        [Header("Debug")]
        public HVRJointSettings CurrentSettings;

        public bool LogStrengthChanges;

        [Tooltip("If true will update the joint every update - useful for tweaking HVRJointSettings in play mode.")]
        public bool AlwaysUpdateJoint;

        public Rigidbody RigidBody { get; private set; }

        public ConfigurableJoint Joint { get; set; }

        public HVRJointSettings JointOverride { get; private set; }

        public HVRJointSettings HandGrabberOverride { get; private set; }


        public bool Stopped { get; private set; }

        private JointDrive _stoppedDrive;

        protected virtual void Awake()
        {
            RigidBody = GetComponent<Rigidbody>();
            //this joint needs to be created before any offsets are applied to the controller target
            //due to how joints snapshot their initial rotations on creation
            SetupJoint();
            _stoppedDrive = new JointDrive();
            _stoppedDrive.maximumForce = 0f;
            _stoppedDrive.positionSpring = 0f;
            _stoppedDrive.positionDamper = 0f;
            RigidBody.maxAngularVelocity = 150f;
            RigidBody.solverIterations = SolverIterations;
            RigidBody.solverVelocityIterations = SolverVelocityIterations;

            if (!JointSettings)
                Debug.LogError($"JointSettings field is empty, must be populated with HVRJointSettings scriptable object.");
        }



        protected virtual IEnumerator StopHandsRoutine()
        {
            var count = 0;
            while (count < 100)
            {
                yield return new WaitForFixedUpdate();
                RigidBody.velocity = Vector3.zero;
                RigidBody.angularVelocity = Vector3.zero;
                transform.position = Target.position;
                count++;
            }
        }

        protected virtual void Start()
        {

        }

        protected virtual void FixedUpdate()
        {
            if (AlwaysUpdateJoint)
            {
                UpdateJoint();
            }
        }
        public virtual void SetupJoint()
        {

        }

        protected virtual void UpdateJoint()
        {
            if (Stopped)
                return;

            if (HandGrabberOverride)
            {
                UpdateStrength(HandGrabberOverride);
            }
            else if (JointOverride)
            {
                UpdateStrength(JointOverride);
            }
            else if (JointSettings)
            {
                UpdateStrength(JointSettings);
            }
        }

        //todo make protected after next hexa integration release
        public virtual void UpdateStrength(HVRJointSettings settings)
        {
            if (settings)
                settings.ApplySettings(Joint);

            CurrentSettings = settings;

            if (LogStrengthChanges && settings)
            {
                Debug.Log($"{settings.name} applied.");
            }
        }

        //todo remove after next hexa integration release
        protected virtual void ResetStrength()
        {
            StopOverride();
        }

        public virtual void OverrideSettings(HVRJointSettings settings)
        {
            JointOverride = settings;
            UpdateJoint();
        }

        public virtual void OverrideHandSettings(HVRJointSettings settings)
        {
            HandGrabberOverride = settings;
            UpdateJoint();
        }

        public virtual void StopOverride()
        {
            JointOverride = null;
            UpdateJoint();
        }

        public virtual void Stop()
        {
            Stopped = true;
            Joint.xDrive = Joint.yDrive = Joint.zDrive = Joint.angularXDrive = Joint.angularYZDrive = Joint.slerpDrive = _stoppedDrive;
        }

        public virtual void Restart()
        {
            Stopped = false;
            UpdateJoint();
        }

        protected virtual void OnEnable()
        {

        }
    }



}
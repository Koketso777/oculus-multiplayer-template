﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HurricaneVR.Framework.Components;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.HandPoser;
using HurricaneVR.Framework.Core.ScriptableObjects;
using HurricaneVR.Framework.Core.Sockets;
using HurricaneVR.Framework.Core.Stabbing;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using HurricaneVR.Framework.Shared.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace HurricaneVR.Framework.Core
{


    public class HVRGrabbable : MonoBehaviour
    {
        #region Fields

        internal const int TrackedVelocityCount = 10;


        [Header("Grab Settings")]


        public HVRGrabType GrabType;
        public HVRGrabTracking TrackingType;
        public HVRHoldType HoldType = HVRHoldType.AllowSwap;
        public HVRGrabControls GrabControl = HVRGrabControls.GripOrTrigger;
        public bool OverrideGrabTrigger;
        [DrawIf("OverrideGrabTrigger", true)]
        public HVRGrabTrigger GrabTrigger = HVRGrabTrigger.Active;

        [Tooltip("Does this grabbable require line of sight to the hand grabber to be grabbed?")]
        public bool RequireLineOfSight = true;

        [Tooltip("If grab type is snap and a pose couldn't resolve, should we try dynamic grabbing.")]
        public bool PhysicsPoserFallback = true;

        [FormerlySerializedAs("ParentHandModelImmediately")]
        [Tooltip("Should the hand model pose immediately to this upon grabbing.")]
        public bool PoseImmediately;

        [Tooltip("Should the hand model parent to the grabbable once close enough? Required for posing.")]
        public bool ParentHandModel = true;

        [Tooltip("Released if the grabbable exceeds this distance from the grabber.")]
        public float BreakDistance = 1f;

        [Tooltip("If true the object remains kinematic")]
        public bool RemainsKinematic = true;

        [Tooltip("If true the object is static or attached to something else and shouldn't be pulled and rotated to the hand")]
        public bool Stationary;

        [Header("Throwing Settings")]
        [Tooltip("Factor to apply to the angular to linear calculation.")]
        public float ReleasedAngularConversionFactor;

        [Tooltip("Factor to apply to the linear throwing velocity.")]
        public float ReleasedVelocityFactor = 1.0f;

        [Tooltip("Factor to apply to the angular throwing velocity.")]
        public float ReleasedAngularFactor = 1f;

        [Header("Grab Indicators")]
        public HVRGrabbableHoverBase GrabIndicator;
        public HVRGrabbableHoverBase ForceGrabIndicator;
        public bool ShowGrabIndicator = true;
        public bool ShowTriggerGrabIndicator = true;
        public bool ShowForceGrabIndicator = true;


        [Header("Force Grabbing")]
        public bool ForceGrabbable = true;

        [Tooltip("Override for when using Force Pull style distance grabbing. Does not apply to gravity glove style.")]
        public HVRForcePullSettings ForcePullOverride;


        #region Joint

        [Header("Configurable Joint Override")]

        [Tooltip("If set it will override the default joint settings - recommended to override the hand settings instead.")]
        public HVRJointSettings JointOverride;

        [Header("Hand Joint Overrides")]

        [Tooltip("Applies the joint settings to the hand joint with one hand hold.")]
        public HVRJointSettings OneHandJointSettings;

        [Tooltip("Applies the joint settings to the hand joint with two hand hold.")]
        public HVRJointSettings TwoHandJointSettings;

        [Tooltip("Uses this to pull the object to the hand, overrides default settings")]
        public HVRJointSettings PullingSettingsOverride;


        [Header("Physics")]

        [Tooltip("If true the hand palm will become the center of mass on grab, midpoint for 2 handed grabs")]
        public bool PalmCenterOfMass;

        #endregion

        [Header("SFX")]
        [Tooltip("SFX played when grabbed by a hand.")]
        public AudioClip HandGrabbedClip;


        [Header("Sockets")]

        [Tooltip("Socket that this grabbable will start in.")]
        public HVRSocket StartingSocket;

        [Tooltip("If true this grabbable will be auto grabbed by the StartingSocket whenever it's dropped.")]
        public bool LinkStartingSocket;

        [Tooltip("If provided only these grab points will be considered when an object is removed from a socket, otherwise the closest grab point will be used.")]
        public HVRPosableGrabPoint[] SocketGrabPoints;

        [Header("Misc")]
        public bool AutoApplyLayer = true;

        [Tooltip("If true the hand must not overlap this any longer to re-enable collision")]
        public bool RequireOverlapClearance = true;

        [Tooltip("If not requiring overlap clearance, how long to wait to re-enable collision with the hand")]
        public float OverlapTimeout = .5f;

        [Tooltip("Must be below this angle delta from expected hand pose and current hand orientation to create the final joint.")]
        public float FinalJointMaxAngle = 40f;

        [Tooltip("If the joint target rotation doesn't need to be 0 you can turn this to true. Set to false for guns or items that you will apply force to.")]
        public bool FinalJointQuick = true;

        [Tooltip("If FinalJointQuick - how long do we try pulling into position before using the final joint settings.")]
        public float FinalJointTimeout = .5f;

        [Tooltip("If assigned, Colliders will populate from these transforms.")]
        public List<Transform> CollisionParents = new List<Transform>();

        [Tooltip("Additional transforms to ignore children colliders when grabbing, helpful for compound objects")]
        public List<Transform> ExtraIgnoreCollisionParents = new List<Transform>();

        [Tooltip("If populated, only these colliders will be used by the grab detection system.")]
        public Collider[] GrabColliders;

        [Tooltip("Should angle be compared when considering which grab point to choose, grab point should be close together")]
        public bool ConsiderGrabPointAngle = true;

        [Tooltip("Let the grab system know if it can use collider closest point for line of sight and distance checking for grab detection")]
        public bool UseColliderClosestPoint = true;

        [Tooltip("If true, grabbing this object will disable hand collision while held")]
        public bool DisableHandCollision;

        [Tooltip("If in a networked game, can someone take this object from your hand?")]
        public bool AllowMultiplayerSwap;

        [Tooltip("For non jointed grabs that get parented to the grab point, and CloneHandModel is disabled on the hand grabber. Only use is to keep the " +
                 "physics hand from clipping through objects until the object is released.")]
        public bool EnableInvisibleHand;

        public CollisionDetectionMode CollisionDetection = CollisionDetectionMode.ContinuousDynamic;

        [Header("Debug")] public bool ShowBoundingBox;
        public bool DrawCenterOfMass;

        public List<Transform> GrabPoints = new List<Transform>();

        internal HashSet<Collider> GrabCollidersSet { get; private set; }
        internal bool FilterGrabColliders { get; private set; }

        #endregion

        #region Events

        public VRGrabberEvent Deactivated = new VRGrabberEvent();
        public VRGrabberEvent Activated = new VRGrabberEvent();
        public VRGrabberEvent Grabbed = new VRGrabberEvent();
        public VRGrabberEvent Released = new VRGrabberEvent();
        public VRGrabberEvent HoverEnter = new VRGrabberEvent();
        public VRGrabberEvent HoverExit = new VRGrabberEvent();
        public VRGrabbableEvent Collided = new VRGrabbableEvent();
        public VRGrabbableEvent Destroyed = new VRGrabbableEvent();
        public VRHandGrabberEvent HandGrabbed = new VRHandGrabberEvent();
        public VRHandGrabberEvent HandReleased = new VRHandGrabberEvent();
        public VRHandGrabberEvent HandFullReleased = new VRHandGrabberEvent();
        public VRSocketEvent Socketed = new VRSocketEvent();
        public VRSocketEvent UnSocketed = new VRSocketEvent();


        #endregion

        #region Properties

        public virtual bool IsMine { get; set; } = true;

        public int GrabberCount => _distinctGrabbers.Count;

        public float ElapsedSinceReleased { get; private set; }
        public bool IsBeingHeld => _distinctGrabbers.Count > 0;
        public bool IsSocketed { get; private set; }
        public bool IsBeingForcedGrabbed { get; internal set; }

        public bool IsClimbable { get; private set; }

        public bool CanBeGrabbed { get; set; } = true;

        /// <summary>
        /// Used to line of sight checks when grabbing, as well as disabling collision between the hand
        /// and the this object while grabbing.
        /// </summary>
        public Collider[] Colliders { get; private set; }

        public Collider[] AdditionalIgnoreColliders { get; private set; }

        /// <summary>
        /// Used for line of sight checks when grabbing.
        /// </summary>
        public Collider[] Triggers { get; private set; }

        public CollisionDetectionMode OriginalCollisionMode { get; private set; }

        public float Drag { get; private set; }
        public bool WasGravity { get; private set; }

        public bool WasKinematic { get; private set; }

        public List<HVRPosableGrabPoint> GrabPointsMeta = new List<HVRPosableGrabPoint>();

        public HVRGrabberBase PrimaryGrabber { get; private set; }
        public HVRSocket SocketHoverer { get; internal set; }

        public Rigidbody Rigidbody { get; set; }

        public HVRSocketable Socketable { get; private set; }
        public HVRSocket LinkedSocket { get; private set; }

        public HVRSocket Socket { get; private set; }

        public HVRHandGrabber LeftHandGrabber { get; private set; }
        public HVRHandGrabber RightHandGrabber { get; private set; }

        public bool IsLeftHandGrabbed { get; private set; }
        public bool IsRightHandGrabbed { get; private set; }

        public bool IsHandGrabbed => HandGrabbers.Count > 0;


        public bool IsJointGrab => TrackingType == HVRGrabTracking.ConfigurableJoint || TrackingType == HVRGrabTracking.FixedJoint;

        public bool HasConcaveColliders { get; private set; }

        /// <summary>
        /// If true will force use the two hand settings regardless of the number of hand grabbers holding
        /// </summary>
        public bool ForceTwoHandSettings
        {
            get => _forceTwoHandSettings;
            set
            {
                _forceTwoHandSettings = value;
                UpdateHandSettings();
            }
        }

        public HVRRequireOtherGrabbable RequiredGrabbableComponent { get; set; }

        public HVRGrabbable RequiredGrabbable => !RequiredGrabbableComponent ? null : RequiredGrabbableComponent.Grabbable;

        public bool RequiresGrabbable => RequiredGrabbableComponent && RequiredGrabbableComponent.Grabbable;

        public bool DropOnRequiredReleased => RequiredGrabbableComponent && RequiredGrabbable && RequiredGrabbableComponent.DropIfReleased;

        public bool GrabRequiredIfReleased => RequiredGrabbableComponent && RequiredGrabbableComponent.GrabRequiredIfReleased;

        //serialized for debugging purposes, cleared on Start()
        public List<HVRGrabberBase> Grabbers = new List<HVRGrabberBase>();
        public List<HVRHandGrabber> HandGrabbers = new List<HVRHandGrabber>();

        public readonly HashSet<Transform> HeldGrabPoints = new HashSet<Transform>();

        public Bounds ModelBounds => transform.GetRendererBounds();

        public List<HVRStabber> Stabbers = new List<HVRStabber>();
        public HVRStabbable Stabbable;

        public bool IsStabbing { get; private set; }
        public bool IsStabbed => Stabbable && Stabbable.IsStabbed;

        public bool BeingDestroyed { get; set; }

        #endregion

        #region Private

        private Quaternion _previousRotation = Quaternion.identity;
        private readonly Dictionary<HVRGrabberBase, ConfigurableJoint> _joints = new Dictionary<HVRGrabberBase, ConfigurableJoint>();
        private readonly CircularBuffer<Vector3> _recentVelocities = new CircularBuffer<Vector3>(TrackedVelocityCount);
        private readonly CircularBuffer<Vector3> _recentAngularVelocities = new CircularBuffer<Vector3>(TrackedVelocityCount);
        private readonly HashSet<HVRGrabberBase> _distinctGrabbers = new HashSet<HVRGrabberBase>();
        private readonly List<HVRGrabberBase> _releaseGrabbers = new List<HVRGrabberBase>();

        private bool _forceTwoHandSettings;

        private Vector3 _centerOfMass;
        private Quaternion _inertiaRotation;
        private Vector3 _inertiaTensor;
        private RigidbodyInterpolation _rbInterpolation;
        private float _mass;
        private bool _waitingForColDetectionReset;
        private Coroutine _resetCollisionDetectionRoutine;
        private readonly HashSet<Collider> _ignoredColliders = new HashSet<Collider>();

        #endregion

        #region Unity Methods

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();

            SetupColliders();

            if (GrabPoints.Count == 0)
                PopulateGrabPoints();

            LoadGrabPoints();

            Socketable = GetComponent<HVRSocketable>();
            if (Socketable && !Socketable.SocketOrientation)
            {
                var orientation = new GameObject("SocketOrientation");
                orientation.transform.SetParent(this.transform);
                orientation.transform.localPosition = Vector3.zero;
                orientation.transform.localRotation = Quaternion.identity;
                orientation.transform.localScale = Vector3.zero;
                Socketable.SocketOrientation = orientation.transform;
            }

            if (HVRSettings.Instance.AutoApplyGrabbableLayer && AutoApplyLayer)
            {
                transform.SetLayerRecursive(HVRLayers.Grabbable);
            }

            ResetTrackedVelocities();

            RequiredGrabbableComponent = GetComponent<HVRRequireOtherGrabbable>();

            IsClimbable = GetComponent<HVRClimbable>() != null;

            Stabbers.AddRange(GetComponents<HVRStabber>());

            if (!Stabbable)
            {
                Stabbable = GetComponentInChildren<HVRStabbable>();
            }

            if (IsJointGrab && !Rigidbody)
            {
                Stationary = true;
            }

            if (GrabColliders != null)
            {
                GrabCollidersSet = new HashSet<Collider>(GrabColliders);
                FilterGrabColliders = GrabColliders.Length > 0;
            }
        }


        protected virtual void Start()
        {
            Grabbers.Clear();

            if (StartingSocket)
            {
                if (LinkStartingSocket)
                {
                    LinkedSocket = StartingSocket;
                    LinkedSocket.LinkedGrabbable = this;
                }
                //let all Starts() go off first
                StartCoroutine(AttachToStartingSocket());
            }


        }

        protected virtual void Update()
        {
            if (ShowBoundingBox)
            {
                DrawBoundingBox();
            }
            if (!IsBeingHeld)
                ElapsedSinceReleased += Time.deltaTime;

            CheckIfStabbing();

            ProcessUpdate();
        }

        private void CheckIfStabbing()
        {
            IsStabbing = false;

            for (var i = 0; i < Stabbers.Count; i++)
            {
                var stabber = Stabbers[i];
                if (stabber.IsStabbing)
                {
                    IsStabbing = true;
                    break;
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (HandGrabbers.Count > 0)
            {
                TrackVelocities();

            }

            ProcessFixedUpdate();
        }

        private void OnDestroy()
        {
            _distinctGrabbers.Clear();
            Destroyed.Invoke(this);

            Activated.RemoveAllListeners();
            Grabbed.RemoveAllListeners();
            Released.RemoveAllListeners();
            HoverEnter.RemoveAllListeners();
            HoverExit.RemoveAllListeners();
            Destroyed.RemoveAllListeners();
        }

        protected virtual void OnCollisionEnter(Collision other)
        {
            Collided.Invoke(this);
        }

        private void OnJointBreak(float breakForce)
        {
            //Debug.Log($"joint broken {breakForce}");
            StartCoroutine(HandleJointBreak());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (DrawCenterOfMass && Rigidbody)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(Rigidbody.worldCenterOfMass, .02f);
            }

            //Gizmos.color = Color.magenta;
            //foreach (var joint in _joints)
            //{
            //    if (joint.Value)
            //    {
            //        Gizmos.DrawWireSphere(transform.TransformPoint(joint.Value.anchor), .3f / 12);
            //    }
            //}
        }



#endif

        #endregion


        #region Public Methods

        /// <summary>
        /// Ignores collision with another grabbable
        /// </summary>
        /// <param name="other"></param>
        public void IgnoreCollision(HVRGrabbable other, bool ignore = true)
        {
            foreach (var otherCollider in other.Colliders)
            {
                foreach (var ourCollider in Colliders)
                {
                    Physics.IgnoreCollision(otherCollider, ourCollider, ignore);
                }
            }
        }

        protected virtual bool GrabPointValid(HVRHandGrabber hand, HVRPosableGrabPoint grabPoint, GrabpointFilter filter)
        {
            if (grabPoint.PoserIndex != hand.PoserIndex)
                return false;

            if (!grabPoint.gameObject.activeInHierarchy || !grabPoint.enabled)
            {
                return false;
            }

            if (hand.HandSide == HVRHandSide.Left && !grabPoint.LeftHand ||
                hand.HandSide == HVRHandSide.Right && !grabPoint.RightHand)
            {
                return false;
            }

            var poseRotation = grabPoint.GetPoseWorldRotation(hand.HandSide);

            var angleDelta = Quaternion.Angle(hand.HandWorldRotation, poseRotation);
            if (angleDelta > grabPoint.AllowedAngleDifference)
            {
                return true;
            }

            if (filter == GrabpointFilter.ForceGrab && !grabPoint.IsForceGrabbable)
                return false;

            if (grabPoint.OneHandOnly && HeldGrabPoints.Contains(grabPoint.transform))
                return false;

            if (filter == GrabpointFilter.Socket && SocketGrabPoints != null && SocketGrabPoints.Length > 0 && !SocketGrabPoints.Contains(grabPoint))
            {
                return false;
            }

            return true;
        }

        public HVRPosableGrabPoint GetGrabPoint(HVRHandGrabber hand, GrabpointFilter filter)
        {
            HVRPosableGrabPoint closest = null;
            var currentDistance = float.MaxValue;

            for (var i = 0; i < GrabPointsMeta.Count; i++)
            {
                var grabPoint = GrabPointsMeta[i];

                if (!GrabPointValid(hand, grabPoint, filter))
                    continue;

                var distance = Vector3.Distance(hand.HandModel.position, grabPoint.GetPoseWorldPosition(hand.HandSide));

                if (distance < currentDistance)
                {
                    closest = grabPoint;
                    currentDistance = distance;
                }
            }

            if (closest == null)
                return null;

            var currentAngle = Quaternion.Angle(hand.HandWorldRotation, closest.GetPoseWorldRotation(hand.HandSide));

            if (!ConsiderGrabPointAngle)
                return closest;

            for (var i = 0; i < closest.Others.Count; i++)
            {
                var gp = closest.Others[i];

                if (!GrabPointValid(hand, gp, filter))
                    continue;

                var angle = Quaternion.Angle(hand.HandWorldRotation, gp.GetPoseWorldRotation(hand.HandSide));
                if (angle < currentAngle)
                {
                    closest = gp;
                    currentAngle = angle;
                }
            }

            return closest;
        }

        internal Transform GetGrabPointTransform(HVRHandGrabber hand, GrabpointFilter forceGrab)
        {
            var gp = GetGrabPoint(hand, forceGrab);
            return gp == null ? null : gp.transform;
        }

        /// <summary>
        /// Gets the distance between this grabbable and the provided grabber
        /// </summary>
        public virtual float GetDistanceToGrabber(Vector3 point)
        {
            if (GrabPoints.Count > 0)
            {
                var distance = float.PositiveInfinity;

                for (var i = 0; i < GrabPoints.Count; i++)
                {
                    var gp = GrabPoints[i];
                    var delta = point - gp.transform.position;
                    if (delta.magnitude < distance)
                    {
                        distance = delta.magnitude;
                    }
                }

                return distance;
            }

            return Vector3.Distance(point, transform.position);
        }

        /// <summary>
        /// Gets the Squared Distance between this grabbable and the provided grabber
        /// </summary>
        public virtual float GetSquareDistanceToGrabber(Vector3 point)
        {
            if (GrabPoints.Count > 0)
            {
                var distance = float.PositiveInfinity;

                for (var i = 0; i < GrabPoints.Count; i++)
                {
                    var gp = GrabPoints[i];
                    var delta = point - gp.transform.position;
                    if (delta.sqrMagnitude < distance)
                    {
                        distance = delta.sqrMagnitude;
                    }
                }

                return distance;
            }

            return (point - transform.position).sqrMagnitude;
        }

        /// <summary>
        /// Disables all non trigger colliders 
        /// </summary>
        public void DisableCollision()
        {
            foreach (var c in Colliders)
            {
                if (c) c.enabled = false;
            }
        }

        /// <summary>
        /// Sets all colliders to trigger
        /// </summary>
        public void SetAllToTrigger()
        {
            foreach (var c in Colliders)
            {
                if (c) c.isTrigger = true;
            }
        }

        /// <summary>
        /// Sets all non trigger colliders back to non trigger
        /// </summary>
        public void ResetToNonTrigger()
        {
            foreach (var c in Colliders)
            {
                if (c) c.isTrigger = false;
            }
        }

        /// <summary>
        /// Enables all non trigger colliders
        /// </summary>
        public void EnableCollision()
        {
            foreach (var c in Colliders)
            {
                if (c) c.enabled = true;
            }
        }

        /// <summary>
        /// Loads grab points from the object with HVRGrabPoints component, if not found it we look
        /// for the first child object named "GrabPoints"
        /// </summary>
        public void PopulateGrabPoints()
        {
            var vrGrabPoints = GetComponentInChildren<HVRGrabPoints>();
            Transform grabPoints;
            if (vrGrabPoints != null)
            {
                grabPoints = vrGrabPoints.transform;
            }
            else
            {
                grabPoints = transform.FindChildRecursive("GrabPoints");
            }

            if (grabPoints != null)
            {
                GrabPoints.Clear();
                //Debug.Log("VRGrabbableBase: Reloading grab points.");

                foreach (Transform c in grabPoints)
                {
                    GrabPoints.Add(c);
                }
            }
        }

        public virtual void LoadGrabPoints()
        {
            GrabPointsMeta.Clear();

            foreach (var grabPoint in GrabPoints)
            {
                if (!grabPoint || !grabPoint.gameObject.activeInHierarchy)
                    continue;

                if (!grabPoint.TryGetComponent(out HVRPosableGrabPoint gp))
                    continue;

                GrabPointsMeta.Add(gp);
            }

            //caching grouped grab points within themselves
            foreach (var grabPoint in GrabPointsMeta)
            {
                foreach (var groupedPoint in GrabPointsMeta.Where(p => p.Group == grabPoint.Group && p != grabPoint))
                {
                    grabPoint.AddGroupedGrabPoint(groupedPoint);
                }
            }
        }


        /// <summary>
        /// Gets the average velocity of the grabbable for N frames into the past starting at start frames into the past.
        /// </summary>
        public Vector3 GetAverageVelocity(int frames, int start, bool takePeak = false, int nPeak = 3)
        {
            if (start + frames > TrackedVelocityCount)
                frames = TrackedVelocityCount - start;
            return HVRHandGrabber.GetAverageVelocity(frames, start, _recentVelocities, takePeak, nPeak);
        }

        /// <summary>
        /// Gets the average angular velocity of the grabbable for N frames into the past starting at start frames into the past.
        /// </summary>
        public Vector3 GetAverageAngularVelocity(int frames, int start)
        {
            if (start + frames > TrackedVelocityCount)
                frames = TrackedVelocityCount - start;
            return HVRHandGrabber.GetAverageVelocity(frames, start, _recentAngularVelocities);
        }

        /// <summary>
        /// Used for networked games, to determine if any grabber holding this object is not ours
        /// </summary>
        /// <returns></returns>
        public bool AnyGrabberNotMine()
        {
            for (var i = 0; i < Grabbers.Count; i++)
            {
                var e = Grabbers[i];
                if (e.IsHandGrabber)
                {
                    if (!e.IsMine)
                        return true;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Forces any held grabbers to release this grabbable.
        /// </summary>
        public void ForceRelease()
        {
            foreach (var grabber in _distinctGrabbers)
            {
                _releaseGrabbers.Add(grabber);
            }

            for (var i = 0; i < _releaseGrabbers.Count; i++)
            {
                var grabber = _releaseGrabbers[i];
                grabber.ForceRelease();
            }

            _releaseGrabbers.Clear();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Called at the end of the unity Update method.
        /// </summary>
        protected virtual void ProcessUpdate()
        {

        }

        /// <summary>
        /// Called at the end of the unity FixedUpdate Method;
        /// </summary>
        protected virtual void ProcessFixedUpdate()
        {

        }

        /// <summary>
        /// Recursively finds colliders and triggers, ignores children that are grabbables.
        /// </summary>
        protected virtual void FindColliders(Transform parent, List<Collider> colliders, List<Collider> triggers)
        {
            var grabbable = parent.GetComponent<HVRGrabbable>();
            if (grabbable && grabbable != this)
                return;

            foreach (var c in parent.GetComponents<Collider>())
            {
                if (c.isTrigger)
                {
                    triggers.Add(c);
                }
                else
                {
                    colliders.Add(c);
                }
            }

            foreach (Transform child in parent)
            {
                FindColliders(child, colliders, triggers);
            }
        }

        /// <summary>
        /// When the grabbable is deactivated, such as when the trigger is released by the held hand grabber
        /// </summary>
        protected virtual void OnDeactivate(HVRGrabberBase grabber)
        {
            Deactivated.Invoke(grabber, this);
        }

        /// <summary>
        /// When the grabbable is activated, such as when the trigger is pulled by the held hand grabber
        /// </summary>
        protected virtual void OnActivate(HVRGrabberBase grabber)
        {
            Activated.Invoke(grabber, this);
        }

        /// <summary>
        /// Fired before the OnGrabbed method
        /// </summary>
        protected virtual void OnBeforeGrabbed(HVRGrabberBase grabber)
        {
            if (HVRSettings.Instance.VerboseGrabbableEvents)
            {
                Debug.Log($"{name}:OnBeforeGrabbed");
            }

            if (GrabberCount == 0)
            {
                SaveRigidBodyState();
            }
            AddGrabber(grabber);
        }

        /// <summary>
        /// Fired if the grabber decided to cancel the grab
        /// </summary>
        protected virtual void OnGrabCanceled(HVRGrabberBase grabber)
        {
            if (HVRSettings.Instance.VerboseGrabbableEvents)
            {
                Debug.Log($"{name}:OnGrabCanceled");
            }
            ResetRigidBody();
            RemoveGrabber(grabber);
        }

        /// <summary>
        /// Fired upon a successful grab
        /// </summary>
        protected virtual void OnGrabbed(HVRGrabberBase grabber)
        {
            if (HVRSettings.Instance.VerboseGrabbableEvents)
            {
                Debug.Log($"{name}:OnGrabbed");
            }

            IsSocketed = _distinctGrabbers.Any(e => e is HVRSocket); //really should only be one if socketed...
            if (IsSocketed)
            {
                Socket = Grabbers[0] as HVRSocket;
            }

            Grabbed.Invoke(grabber, this);
            if (grabber.IsHandGrabber)
            {
                HandGrabbed.Invoke(grabber as HVRHandGrabber, this);
            }

            if (grabber.IsSocket)
            {
                Socketed.Invoke(grabber as HVRSocket, this);
            }

            if (DropOnRequiredReleased && RequiredGrabbable.IsBeingHeld)
            {
                RequiredGrabbable.Released.AddListener(OnRequiredGrabbableReleased);
            }
        }

        /// <summary>
        /// Fired after the grabber released this
        /// </summary>
        protected virtual void OnReleased(HVRGrabberBase grabber)
        {
            if (HVRSettings.Instance.VerboseGrabbableEvents)
            {
                Debug.Log($"{name}:OnReleased");
            }

            if (RequiredGrabbable)
            {
                RequiredGrabbable.Released.RemoveListener(OnRequiredGrabbableReleased);
            }

            RemoveGrabber(grabber);


            IsBeingForcedGrabbed = false;

            RemoveJoint(grabber);

            if (GrabberCount == 0)
            {
                ElapsedSinceReleased = 0f;
                if (Rigidbody)
                {
                    ResetRigidBody();
                    if (_resetCollisionDetectionRoutine != null)
                    {
                        StopCoroutine(_resetCollisionDetectionRoutine);
                    }
                    _resetCollisionDetectionRoutine = StartCoroutine(ResetCollisionMode());
                }
            }

            IsSocketed = _distinctGrabbers.Any(e => e is HVRSocket); //really should only be one if socketed...
            if (!IsSocketed)
            {
                Socket = null;
            }

            if (!PrimaryGrabber && LinkedSocket)
            {
                StartCoroutine(CheckLinkedSocket());
            }
        }

        /// <summary>
        /// Fired when a grabber is hovering this, most likely with their trigger collider
        /// </summary>
        protected virtual void OnHoverEnter(HVRGrabberBase grabber)
        {
        }

        /// <summary>
        /// Fired when a grabber is not longer hovering this, most likely with their trigger collider
        /// </summary>
        protected virtual void OnHoverExit(HVRGrabberBase grabber)
        {
        }

        /// <summary>
        /// Called before a hand grabber is removed from the HandGrabbers field.
        /// </summary>
        protected virtual void OnBeforeHandGrabberRemoved(HVRHandGrabber handGrabber)
        {

        }

        protected virtual void OnAfterHandGrabberRemoved(HVRHandGrabber handGrabber)
        {
            if (handGrabber.HandSide == HVRHandSide.Left)
            {
                LeftHandGrabber = null;
                IsLeftHandGrabbed = false;
            }
            else
            {
                IsRightHandGrabbed = false;
                RightHandGrabber = null;
            }
            handGrabber.OverrideHandSettings(null);
            handGrabber.UpdateGrabbableCOM(this);
            UpdateHandSettings();
        }

        /// <summary>
        /// Called after a hand grabs this and is added to the HandGrabbers field.
        /// </summary>
        protected virtual void OnAfterHandGrabberAdded(HVRHandGrabber handGrabber)
        {
            if (handGrabber.HandSide == HVRHandSide.Left)
            {
                LeftHandGrabber = handGrabber;
                IsLeftHandGrabbed = true;
            }
            else
            {
                IsRightHandGrabbed = true;
                RightHandGrabber = handGrabber;
            }
            UpdateHandSettings();
        }

        /// <summary>
        /// If provided, will update the hand joint settings depending on one or two handed grabs
        /// </summary>
        protected virtual void UpdateHandSettings()
        {
            if ((HandGrabbers.Count >= 2 || ForceTwoHandSettings) && TwoHandJointSettings)
            {
                //Debug.Log($"{name} Update two hand : {TwoHandJointSettings?.name} [{ForceTwoHandSettings}]");
                for (int i = 0; i < HandGrabbers.Count; i++)
                {
                    HandGrabbers[i].OverrideHandSettings(TwoHandJointSettings);
                }
            }
            else if (HandGrabbers.Count > 0)
            {
                //Debug.Log($"{name} Update one hand : {OneHandJointSettings?.name}");
                HandGrabbers[0].OverrideHandSettings(OneHandJointSettings);
            }
        }


        #endregion

        #region Private Methods

        private void ResetTrackedVelocities()
        {
            for (var i = 0; i < TrackedVelocityCount; i++)
            {
                _recentVelocities.Enqueue(Vector3.zero);
                _recentAngularVelocities.Enqueue(Vector3.zero);
            }
        }



        /// <summary>
        /// Locates colliders that are used for line of sight checking and for collision disabling with the grabbing hand.
        /// </summary>
        private void SetupColliders()
        {
            var colliders = new List<Collider>();
            var extraColliders = new List<Collider>();
            var triggers = new List<Collider>();

            if (CollisionParents.Count > 0)
            {
                foreach (var collisionParent in CollisionParents)
                {
                    if (collisionParent)
                    {
                        colliders.AddRange(collisionParent.gameObject.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger));
                        triggers.AddRange(collisionParent.gameObject.GetComponentsInChildren<Collider>().Where(c => c.isTrigger));
                    }
                }
            }
            else
            {
                FindColliders(transform, colliders, triggers);
            }

            if (ExtraIgnoreCollisionParents.Count > 0)
            {
                foreach (var collisionParent in ExtraIgnoreCollisionParents)
                {
                    if (collisionParent)
                    {
                        extraColliders.AddRange(collisionParent.gameObject.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger));
                    }
                }
            }

            Triggers = triggers.ToArray();
            Colliders = colliders.ToArray();
            AdditionalIgnoreColliders = extraColliders.ToArray();
            UpdateIgnoreColliders();
            HasConcaveColliders = triggers.Any(e => { var mesh = e as MeshCollider; return mesh != null && !mesh.convex; });
            HasConcaveColliders = HasConcaveColliders || colliders.Any(e => { var mesh = e as MeshCollider; return mesh && !mesh.convex; });
        }

        public void UpdateIgnoreColliders()
        {
            _ignoredColliders.Clear();

            foreach (var c in Colliders)
            {
                _ignoredColliders.Add(c);
            }

            foreach (var c in AdditionalIgnoreColliders)
            {
                _ignoredColliders.Add(c);
            }
        }

        public bool IsIgnoreCollider(Collider col)
        {
            return _ignoredColliders.Contains(col);
        }

        private IEnumerator AttachToStartingSocket()
        {
            yield return null;
            StartingSocket.TryGrab(this, false, true);
        }

        private void TrackVelocities()
        {
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_previousRotation);
            deltaRotation.ToAngleAxis(out var angle, out var axis);
            angle *= Mathf.Deg2Rad;
            var angularVelocity = axis * (angle * (1.0f / Time.fixedDeltaTime));

            if (Rigidbody)
            {
                _recentVelocities.Enqueue(Rigidbody.velocity);
            }
            _recentAngularVelocities.Enqueue(angularVelocity);

            _previousRotation = transform.rotation;
        }

        private void OnRequiredGrabbableReleased(HVRGrabberBase arg0, HVRGrabbable grabbable)
        {
            if (RequiredGrabbable)
            {
                RequiredGrabbable.Released.RemoveListener(OnRequiredGrabbableReleased);
            }

            if (IsBeingHeld)
            {
                var grabber = PrimaryGrabber;
                ForceRelease();
                if (GrabRequiredIfReleased)
                {
                    grabber.TryGrab(grabbable);
                }
            }
        }

        private IEnumerator CheckLinkedSocket()
        {
            yield return null;

            if (!PrimaryGrabber && LinkedSocket)
            {
                LinkedSocket.TryGrab(this, true);
            }
        }

        private void SaveRigidBodyState()
        {
            if (!Rigidbody)
                return;

            Drag = Rigidbody.drag;
            WasGravity = Rigidbody.useGravity;
            WasKinematic = Rigidbody.isKinematic;
            if (!_waitingForColDetectionReset)
            {
                OriginalCollisionMode = Rigidbody.collisionDetectionMode;
            }

            _inertiaRotation = Rigidbody.inertiaTensorRotation;
            _centerOfMass = Rigidbody.centerOfMass;
            _inertiaTensor = Rigidbody.inertiaTensor;
            _mass = Rigidbody.mass;
            _rbInterpolation = Rigidbody.interpolation;
        }

        /// <summary>
        /// Resets the rigid body state to what it was before it was grabbed
        /// </summary>
        public virtual void ResetRigidBody()
        {
            if (!Rigidbody)
                return;

            Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Rigidbody.isKinematic = WasKinematic;

            if (Rigidbody.isKinematic)
            {
                Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            else if (!Rigidbody.isKinematic)
            {
                Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            Rigidbody.useGravity = WasGravity;
            Rigidbody.drag = Drag;
            Rigidbody.centerOfMass = _centerOfMass;
            Rigidbody.inertiaTensorRotation = _inertiaRotation;

            if (Rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX))
            {
                _inertiaTensor.x = 1f;
            }

            if (Rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY))
            {
                _inertiaTensor.y = 1f;
            }

            if (Rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationZ))
            {
                _inertiaTensor.z = 1f;
            }

            Rigidbody.inertiaTensor = _inertiaTensor;
            Rigidbody.mass = _mass;
            Rigidbody.interpolation = _rbInterpolation;
        }

        /// <summary>
        /// Destroys and cleanups reference to the configurable joint attached to this grabber
        /// </summary>
        public void RemoveJoint(HVRGrabberBase grabber)
        {
            if (_joints.TryGetValue(grabber, out var joint))
            {
                if (joint)
                    Destroy(joint);
                _joints.Remove(grabber);
            }
        }

        private IEnumerator ResetCollisionMode()
        {
            try
            {
                if (!Rigidbody)
                    yield break;
                _waitingForColDetectionReset = true;
                yield return new WaitForSeconds(10f);
                _waitingForColDetectionReset = false;

                if (!IsBeingHeld)
                {
                    Rigidbody.collisionDetectionMode = OriginalCollisionMode;
                }
            }
            finally
            {
                _resetCollisionDetectionRoutine = null;
            }
        }

        private IEnumerator HandleJointBreak()
        {
            yield return new WaitForFixedUpdate();

            foreach (var grabber in _joints.Keys.ToList())
            {
                _joints.TryGetValue(grabber, out var joint);
                if (!joint)
                    grabber.ForceRelease();
            }
        }



        #endregion

        #region Internal Methods

        internal void AddJoint(ConfigurableJoint joint, HVRGrabberBase grabber)
        {
            _joints[grabber] = joint;
        }

        /// <summary>
        /// When the grabbable is deactivated, such as when the trigger is released by the held hand grabber
        /// </summary>
        protected internal virtual void InternalOnDeactivate(HVRGrabberBase grabber)
        {
            OnDeactivate(grabber);
        }

        /// <summary>
        /// When the grabbable is activated, such as when the trigger is pulled by the held hand grabber
        /// </summary>
        protected internal virtual void InternalOnActivate(HVRGrabberBase grabber)
        {
            OnActivate(grabber);
        }

        internal void InternalOnGrabbed(HVRGrabberBase grabber)
        {
            OnGrabbed(grabber);
        }

        internal void InternalOnBeforeGrabbed(HVRGrabberBase grabber)
        {
            OnBeforeGrabbed(grabber);
        }

        internal void InternalOnGrabCanceled(HVRGrabberBase grabber)
        {
            OnGrabCanceled(grabber);
        }

        internal virtual void InternalOnReleased(HVRGrabberBase grabber)
        {
            OnReleased(grabber);
        }

        internal void AddGrabber(HVRGrabberBase grabber)
        {
            if (_distinctGrabbers.Count == 0)
            {
                PrimaryGrabber = grabber;
            }

            if (!_distinctGrabbers.Contains(grabber))
            {
                _distinctGrabbers.Add(grabber);
                Grabbers.Add(grabber);

                if (grabber is HVRHandGrabber hand)
                {
                    HandGrabbers.Add(hand);
                    OnAfterHandGrabberAdded(hand);
                }
            }

            if (HVRSettings.Instance.VerboseGrabbableEvents)
            {
                Debug.Log($"{name}:AddGrabber [{_distinctGrabbers.Count}]");
            }
        }

        internal void RemoveGrabber(HVRGrabberBase grabber)
        {
            if (_distinctGrabbers.Contains(grabber))
            {
                _distinctGrabbers.Remove(grabber);
                Grabbers.Remove(grabber);

                if (grabber is HVRHandGrabber hand)
                {
                    OnBeforeHandGrabberRemoved(hand);
                    HandGrabbers.Remove(hand);
                    OnAfterHandGrabberRemoved(hand);
                }
            }

            if (_distinctGrabbers.Count == 0)
            {
                PrimaryGrabber = null;
            }
            else if (_distinctGrabbers.Count == 1)
            {
                PrimaryGrabber = _distinctGrabbers.First();
            }

            if (HVRSettings.Instance.VerboseGrabbableEvents)
            {
                Debug.Log($"{name}:RemoveGrabber [{_distinctGrabbers.Count}]");
            }
        }

        internal void InternalOnHoverEnter(HVRGrabberBase grabber)
        {
            OnHoverEnter(grabber);
        }

        protected internal virtual void InternalOnHoverExit(HVRGrabberBase grabber)
        {
            OnHoverExit(grabber);
        }

        #endregion


        #region Debugging

        //public Vector3 TargetRotation;
        private Vector3 v3FrontTopLeft;
        private Vector3 v3FrontTopRight;
        private Vector3 v3FrontBottomLeft;
        private Vector3 v3FrontBottomRight;
        private Vector3 v3BackTopLeft;
        private Vector3 v3BackTopRight;
        private Vector3 v3BackBottomLeft;
        private Vector3 v3BackBottomRight;



        void DrawBoundingBox()
        {
            Bounds bounds = transform.GetRendererBounds(gameObject);

            Vector3 v3Center = bounds.center;
            Vector3 v3Extents = bounds.extents;

            v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
            v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
            v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
            v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
            v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
            v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
            v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
            v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner


            var color = Color.magenta;
            Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, color);
            Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, color);
            Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, color);
            Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, color);

            Debug.DrawLine(v3BackTopLeft, v3BackTopRight, color);
            Debug.DrawLine(v3BackTopRight, v3BackBottomRight, color);
            Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, color);
            Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, color);

            Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, color);
            Debug.DrawLine(v3FrontTopRight, v3BackTopRight, color);
            Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, color);
            Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, color);
        }

        #endregion
    }

    public enum GrabpointFilter
    {
        Normal, ForceGrab, Socket
    }
}
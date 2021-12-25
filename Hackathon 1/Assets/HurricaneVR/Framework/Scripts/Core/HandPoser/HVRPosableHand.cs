using System;
using System.Collections.Generic;
using HurricaneVR.Framework.Core.HandPoser.Data;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using UnityEditor;
using UnityEngine;

namespace HurricaneVR.Framework.Core.HandPoser
{
    public class HVRPosableHand : MonoBehaviour
    {
        #region EditorState 

        [SerializeField]
        protected GameObject Preview;

        [HideInInspector]
        [SerializeField]
        protected HVRHandPose SelectedPose;

        [HideInInspector]
        public bool DoPreview;

        #endregion

        public bool IsLeft;

        [Tooltip("Used to match up with grab points to allowed objects to have grab points that can be grabbed by different hands.")]
        public int PoserIndex;

        [Header("Mirroring")]
        public MirrorAxis MirrorAxis = MirrorAxis.X;

        [Header("Hand orientation adjustments if necessary for VRIK mirroring")]
        public bool UseMatchRotation;

        public HVRAxis Forward = HVRAxis.Y;
        public HVRAxis Up = HVRAxis.Z;

        public HVRAxis OtherForward = HVRAxis.Y;
        public HVRAxis OtherUp = HVRAxis.Z;


        [Header("Bone Information")]

        public Transform ThumbRoot;
        public Transform ThumbTip;

        public Transform IndexRoot;
        public Transform IndexTip;

        public Transform MiddleRoot;
        public Transform MiddleTip;

        public Transform RingRoot;
        public Transform RingTip;

        public Transform PinkyRoot;
        public Transform PinkyTip;


        public HVRPosableFinger Thumb;
        public HVRPosableFinger Index;
        public HVRPosableFinger Middle;
        public HVRPosableFinger Ring;
        public HVRPosableFinger Pinky;

        public HVRHandMirrorSettings MirrorSettings;
        public float CapsuleRadius = .01f;
        public HVRAxis CapsuleDirection;
        public bool CapsuleAddRadius = true;


        public HVRHandSide Side => IsLeft ? HVRHandSide.Left : HVRHandSide.Right;

        public bool IsRight => !IsLeft;

        private HVRPosableFinger[] _fingers;
        public HVRPosableFinger[] Fingers
        {
            get
            {
                if (_fingers == null || _fingers.Length == 0)
                {
                    var fingers = new List<HVRPosableFinger>();

                    if (Thumb != null)
                    {
                        fingers.Add(Thumb);
                    }

                    if (Index != null)
                    {
                        fingers.Add(Index);
                    }

                    if (Middle != null)
                    {
                        fingers.Add(Middle);
                    }

                    if (Ring != null)
                    {
                        fingers.Add(Ring);
                    }

                    if (Pinky != null)
                    {
                        fingers.Add(Pinky);
                    }

                    _fingers = fingers.ToArray();
                }

                return _fingers;
            }
        }

        private void Awake()
        {

        }

        public void Pose(HVRHandPose pose)
        {
            Pose(pose.GetPose(this.IsLeft));
        }

        public void PoseFingers(HVRHandPose pose)
        {
            PoseFingers(pose.GetPose(this.IsLeft));
        }

        public void PoseFingers(HVRHandPoseData pose)
        {
            PoseFinger(Thumb, pose.Thumb);
            PoseFinger(Index, pose.Index);
            PoseFinger(Middle, pose.Middle);
            PoseFinger(Ring, pose.Ring);
            PoseFinger(Pinky, pose.Pinky);
        }

        public void Pose(HVRHandPoseData pose, bool poseHand = true)
        {
            if (poseHand)
            {
                transform.localPosition = pose.Position;
                transform.localRotation = pose.Rotation;
            }

            PoseFinger(Thumb, pose.Thumb);
            PoseFinger(Index, pose.Index);
            PoseFinger(Middle, pose.Middle);
            PoseFinger(Ring, pose.Ring);
            PoseFinger(Pinky, pose.Pinky);
        }

        private void PoseFinger(HVRPosableFinger finger, HVRPosableFingerData data)
        {
            if (finger == null || data == null)
            {
                return;
            }

            if (finger.Bones == null || data.Bones == null)
            {
                return;
            }

            for (int i = 0; i < finger.Bones.Count; i++)
            {
                var bone = finger.Bones[i];
                if (data.Bones.Count - 1 >= i)
                {
                    var boneData = data.Bones[i];
                    bone.Transform.localPosition = boneData.Position;
                    bone.Transform.localRotation = boneData.Rotation;
                }
            }
        }

#if UNITY_EDITOR

        [InspectorButton("FingerSetup")] public string SetupFingers;
        public void FingerSetup()
        {
            _fingers = null;

            if (ThumbRoot)
            {
                Thumb = new HVRPosableFinger();
                Thumb.Root = ThumbRoot;
                Thumb.Tip = ThumbTip;
                FindBonesForFinger(ThumbRoot, ThumbTip, Thumb);
            }

            if (IndexRoot)
            {
                Index = new HVRPosableFinger();
                Index.Root = IndexRoot;
                Index.Tip = IndexTip;
                FindBonesForFinger(IndexRoot, IndexTip, Index);
            }

            if (MiddleRoot)
            {
                Middle = new HVRPosableFinger();
                Middle.Root = MiddleRoot;
                Middle.Tip = MiddleTip;
                FindBonesForFinger(MiddleRoot, MiddleTip, Middle);
            }

            if (RingRoot)
            {
                Ring = new HVRPosableFinger();
                Ring.Root = RingRoot;
                Ring.Tip = RingTip;
                FindBonesForFinger(RingRoot, RingTip, Ring);
            }

            if (PinkyRoot)
            {
                Pinky = new HVRPosableFinger();
                Pinky.Root = PinkyRoot;
                Pinky.Tip = PinkyTip;
                FindBonesForFinger(PinkyRoot, PinkyTip, Pinky);
            }

            EditorUtility.SetDirty(this);
        }

        [InspectorButton("AddThumbCapsulesPrivate")]
        public string AddThumbCapsules;
        [InspectorButton("AddIndexCapsulesPrivate")]
        public string AddIndexCapsules;
        [InspectorButton("AddMiddleCapsulesPrivate")]
        public string AddMiddleCapsules;
        [InspectorButton("AddRingCapsulesPrivate")]
        public string AddRingCapsules;
        [InspectorButton("AddPinkyCapsulesPrivate")]
        public string AddPinkyCapsules;


        private void AddThumbCapsulesPrivate()
        {
            AddFingerCapsules(Thumb);
        }

        private void AddIndexCapsulesPrivate()
        {
            AddFingerCapsules(Index);
        }

        private void AddMiddleCapsulesPrivate()
        {
            AddFingerCapsules(Middle);
        }

        private void AddRingCapsulesPrivate()
        {
            AddFingerCapsules(Ring);
        }

        private void AddPinkyCapsulesPrivate()
        {
            AddFingerCapsules(Pinky);
        }

        private void AddFingerCapsules(HVRPosableFinger finger)
        {
            if (finger == null || finger.Bones == null)
                return;

            Undo.SetCurrentGroupName("AddFingerCapsules");

            for (var i = 0; i < finger.Bones.Count; i++)
            {
                var bone = finger.Bones[i];
                var go = GameObject.Find("coll_" + bone.Transform.name);
                if (go == null || go.transform.parent != bone.Transform)
                {
                    go = new GameObject("coll_" + bone.Transform.name);
                }
                go.transform.parent = bone.Transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(go, "AddGO");
                var capsule = go.GetComponent<CapsuleCollider>();
                if (!capsule)
                {
                    capsule = go.AddComponent<CapsuleCollider>();
                }
                Transform next;
                if (i < finger.Bones.Count - 1)
                {
                    next = finger.Bones[i + 1].Transform;
                }
                else
                {
                    next = finger.Tip;
                }

                var jointDelta = next.position - bone.Transform.position;
                var length = jointDelta.magnitude;
                if (CapsuleAddRadius && i == finger.Bones.Count - 1) length -= CapsuleRadius;
                go.transform.position += jointDelta.normalized * (length * 0.5f);
                capsule.height = length;
                if (CapsuleAddRadius)
                {
                    capsule.height += CapsuleRadius * 2f;
                }

                capsule.radius = CapsuleRadius;
                capsule.direction = (int)CapsuleDirection;

            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

#endif



        private void FindBonesForFinger(Transform bone, Transform tip, HVRPosableFinger finger)
        {
            finger.Bones.Add(new HVRPosableBone() { Transform = bone.transform });

            if (tip.parent == bone)
            {
                return;
            }

            if (bone.childCount > 0)
            {
                FindBonesForFinger(bone.GetChild(0), tip, finger);
            }
        }

        public HVRHandPoseData CreateHandPose(Transform transformOverride = null)
        {
            var t = transformOverride ?? transform;

            var data = new HVRHandPoseData
            {
                Position = t.localPosition,
                Rotation = t.localRotation,
            };

            data.Thumb = Thumb?.GetFingerData();
            data.Index = Index?.GetFingerData();
            data.Middle = Middle?.GetFingerData();
            data.Ring = Ring?.GetFingerData();
            data.Pinky = Pinky?.GetFingerData();

            return data;
        }

        public void CopyHandData(HVRHandPoseData data)
        {
            data.Position = transform.localPosition;
            data.Rotation = transform.localRotation;

            for (var i = 0; i < Fingers.Length; i++)
            {
                var finger = Fingers[i];
                for (var j = 0; j < finger.Bones.Count; j++)
                {
                    var bone = finger.Bones[j];

                    data.Fingers[i].Bones[j].Position = bone.Transform.localPosition;
                    data.Fingers[i].Bones[j].Rotation = bone.Transform.localRotation;
                }
            }
        }

        public HVRHandPose CreateFullHandPose()
        {
            return CreateFullHandPose(MirrorAxis);
        }

        public HVRHandPose CreateFullHandPoseWorld(MirrorAxis axis)
        {
            var hand = CreateHandPose();
            hand.Position = transform.position;
            hand.Rotation = transform.rotation;

            //var handMirror = hand.Mirror(axis, transform);
            var handMirror = Mirror(axis);

            var left = IsLeft ? hand : handMirror;
            var right = IsLeft ? handMirror : hand;

            var handPose = ScriptableObject.CreateInstance<HVRHandPose>();
            handPose.SnappedLeft = Side == HVRHandSide.Left;
            handPose.LeftHand = left;
            handPose.RightHand = right;
            return handPose;
        }

        public HVRHandPose CreateFullHandPose(MirrorAxis axis, Transform transformOverride = null)
        {
            var hand = CreateHandPose(transformOverride);

            //var handMirror = hand.Mirror(axis, transform);
            var handMirror = Mirror(axis);

            var left = IsLeft ? hand : handMirror;
            var right = IsLeft ? handMirror : hand;

            var handPose = ScriptableObject.CreateInstance<HVRHandPose>();
            handPose.SnappedLeft = Side == HVRHandSide.Left;
            handPose.LeftHand = left;
            handPose.RightHand = right;
            return handPose;
        }

        public HVRHandPoseData Mirror(MirrorAxis handMirrorAxis, Transform transformOverride = null)
        {
            var t = transformOverride ?? transform;

            var clone = new HVRHandPoseData();
            clone.Position = t.localPosition;

            Vector3 direction;

            switch (handMirrorAxis)
            {
                case MirrorAxis.X:
                    clone.Position.x *= -1f;
                    direction = Vector3.right;
                    break;
                case MirrorAxis.Y:
                    clone.Position.y *= -1;
                    direction = Vector3.up;
                    break;
                case MirrorAxis.Z:
                    clone.Position.z *= -1;
                    direction = Vector3.forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(handMirrorAxis), handMirrorAxis, null);
            }

            Vector3 forward;
            Vector3 up;

            if (t.parent != null)
            {
                forward = t.parent.InverseTransformDirection(t.forward);
                up = t.parent.InverseTransformDirection(t.up);
            }
            else
            {
                forward = t.forward;
                up = t.up;
            }

            var mirror = Vector3.Reflect(forward, direction);
            var upMirror = Vector3.Reflect(up, direction);
            clone.Rotation = Quaternion.LookRotation(mirror, upMirror);

            if (UseMatchRotation)
            {
                //  clone.Rotation = MatchRotation(clone.Rotation, Forward.GetVector(), Up.GetVector(), OtherForward.GetVector(), OtherUp.GetVector());
                clone.Rotation = MatchRotation(clone.Rotation, OtherForward.GetVector(), OtherUp.GetVector(), Forward.GetVector(), Up.GetVector());
            }

            HVRJointMirrorSetting thumbOverride = null;
            HVRJointMirrorSetting indexMirror = null;
            HVRJointMirrorSetting middleMirror = null;
            HVRJointMirrorSetting ringMirror = null;
            HVRJointMirrorSetting pinkyMirror = null;

            if (MirrorSettings)
            {
                thumbOverride = MirrorSettings.UseThumbSetting ? MirrorSettings.ThumbSetting : MirrorSettings.AllSetting;
                indexMirror = MirrorSettings.UseIndexSetting ? MirrorSettings.IndexSetting : MirrorSettings.AllSetting;
                middleMirror = MirrorSettings.UseMiddleSetting ? MirrorSettings.MiddleSetting : MirrorSettings.AllSetting;
                ringMirror = MirrorSettings.UseRingSetting ? MirrorSettings.RingSetting : MirrorSettings.AllSetting;
                pinkyMirror = MirrorSettings.UsePinkySetting ? MirrorSettings.PinkySetting : MirrorSettings.AllSetting;
            }


            if (Thumb != null)
            {
                clone.Thumb = MirrorFinger(Thumb, thumbOverride, MirrorSettings?.ThumbSettings);
            }

            if (Index != null)
            {
                clone.Index = MirrorFinger(Index, indexMirror, MirrorSettings?.IndexSettings);
            }

            if (Middle != null)
            {
                clone.Middle = MirrorFinger(Middle, middleMirror, MirrorSettings?.MiddleSettings);
            }

            if (Ring != null)
            {
                clone.Ring = MirrorFinger(Ring, ringMirror, MirrorSettings?.RingSettings);
            }

            if (Pinky != null)
            {
                clone.Pinky = MirrorFinger(Pinky, pinkyMirror, MirrorSettings?.PinkySettings);
            }

            return clone;
        }

        public static Quaternion MatchRotation(Quaternion targetRotation, Vector3 targetforwardAxis, Vector3 targetUpAxis, Vector3 forwardAxis, Vector3 upAxis)
        {
            Quaternion f = Quaternion.LookRotation(forwardAxis, upAxis);
            Quaternion fTarget = Quaternion.LookRotation(targetforwardAxis, targetUpAxis);

            Quaternion d = targetRotation * fTarget;
            return d * Quaternion.Inverse(f);
        }

        private HVRPosableFingerData MirrorFinger(HVRPosableFinger finger, HVRJointMirrorSetting mirrorOverride, List<HVRJointMirrorSetting> settings)
        {
            var fingerData = new HVRPosableFingerData();

            for (var i = 0; i < finger.Bones.Count; i++)
            {
                var bone = finger.Bones[i];
                var boneData = new HVRPosableBoneData();



                HVRJointMirrorSetting mirror = null;


                if (settings != null && i < settings.Count)
                {
                    mirror = settings[i];
                }
                else if (mirrorOverride != null)
                {
                    mirror = mirrorOverride;
                }

                if (mirror != null)
                {
                    var euler = bone.Transform.localRotation.eulerAngles;

                    var xAngle = euler.x;
                    var yAngle = euler.y;
                    var zAngle = euler.z;

                    MirrorRotation(mirror.XRotation, ref xAngle);
                    MirrorRotation(mirror.YRotation, ref yAngle);
                    MirrorRotation(mirror.ZRotation, ref zAngle);

                    boneData.Position = bone.Transform.localPosition;
                    boneData.Rotation = Quaternion.Euler(xAngle, yAngle, zAngle);

                    if (mirror.XPosition == FingerMirrorPosition.Opposite)
                    {
                        boneData.Position.x *= -1f;
                    }

                    if (mirror.YPosition == FingerMirrorPosition.Opposite)
                    {
                        boneData.Position.y *= -1f;
                    }

                    if (mirror.ZPosition == FingerMirrorPosition.Opposite)
                    {
                        boneData.Position.z *= -1f;
                    }

                }
                else
                {
                    boneData.Position = bone.Transform.localPosition * -1;
                    boneData.Rotation = bone.Transform.localRotation;
                }

                fingerData.Bones.Add(boneData);
            }

            return fingerData;
        }

        private void MirrorRotation(FingerMirrorRotation option, ref float angle)
        {
            switch (option)
            {
                case FingerMirrorRotation.Minus180:
                    angle -= 180f;
                    break;
                case FingerMirrorRotation.Plus180:
                    angle += 180f;
                    break;
                case FingerMirrorRotation.Same:
                    break;
                case FingerMirrorRotation.Opposite:
                    angle *= -1f;
                    break;
                case FingerMirrorRotation.Neg180Minus:
                    angle = -180f - angle;
                    break;
                case FingerMirrorRotation.P180Minus:
                    angle = 180 - angle;
                    break;

            }
        }
    }

    [Serializable]
    public class HVRPosableFinger
    {
        public Transform Root;
        public Transform Tip;
        public List<HVRPosableBone> Bones = new List<HVRPosableBone>();

        public HVRPosableFingerData GetFingerData()
        {
            var finger = new HVRPosableFingerData
            {
                Bones = new List<HVRPosableBoneData>()
            };

            foreach (var bone in Bones)
            {
                finger.Bones.Add(bone.GetBoneData());
            }

            return finger;
        }
    }

    [Serializable]
    public class HVRPosableBone
    {
        public Transform Transform;

        public HVRPosableBoneData GetBoneData()
        {
            var bone = new HVRPosableBoneData();
            bone.Position = Transform.localPosition;
            bone.Rotation = Transform.localRotation;
            return bone;
        }
    }
}
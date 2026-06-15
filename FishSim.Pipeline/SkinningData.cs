using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FishSim.Animation
{
    // Egyetlen csonthoz tartozo kulcskocka: idopont + lokalis (szulohoz viszonyitott) transform.
    public struct Keyframe
    {
        public int Bone;
        public TimeSpan Time;
        public Matrix Transform;

        public Keyframe(int bone, TimeSpan time, Matrix transform)
        {
            Bone = bone;
            Time = time;
            Transform = transform;
        }
    }

    // Egy animacios klip: idotartam + idorendben rendezett kulcskockak (tobb csontra vegyesen).
    public class AnimationClip
    {
        public TimeSpan Duration;
        public List<Keyframe> Keyframes;

        public AnimationClip(TimeSpan duration, List<Keyframe> keyframes)
        {
            Duration = duration;
            Keyframes = keyframes;
        }
    }

    // A modell csontvazanak es animacioinak runtime adatai (Model.Tag-ben utazik).
    public class SkinningData
    {
        public Dictionary<string, AnimationClip> AnimationClips { get; }
        public Matrix[] BindPose { get; }
        public Matrix[] InverseBindPose { get; }
        public int[] SkeletonHierarchy { get; }

        public SkinningData(Dictionary<string, AnimationClip> animationClips, Matrix[] bindPose, Matrix[] inverseBindPose, int[] skeletonHierarchy)
        {
            AnimationClips = animationClips;
            BindPose = bindPose;
            InverseBindPose = inverseBindPose;
            SkeletonHierarchy = skeletonHierarchy;
        }
    }
}

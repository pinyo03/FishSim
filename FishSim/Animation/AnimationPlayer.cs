using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FishSim.Animation
{
    // Egyszeru, kulcskocka-lepteto animacio-lejatszo (nincs interpolacio, a "Skinning Sample"
    // mintajat koveti): minden Update-nel a currentTime-nal nem nagyobb idobelyegu
    // kulcskockakat alkalmazza, majd kiszamolja a vilag- es skin-transformokat.
    public class AnimationPlayer
    {
        readonly SkinningData skinningData;

        AnimationClip currentClip;
        TimeSpan currentTime;
        int currentKeyframe;

        readonly Matrix[] boneTransforms;
        readonly Matrix[] worldTransforms;
        readonly Matrix[] skinTransforms;

        public Matrix[] Bones => skinTransforms;

        public AnimationPlayer(SkinningData skinningData)
        {
            this.skinningData = skinningData;

            int boneCount = skinningData.BindPose.Length;
            boneTransforms = new Matrix[boneCount];
            worldTransforms = new Matrix[boneCount];
            skinTransforms = new Matrix[boneCount];

            Array.Copy(skinningData.BindPose, boneTransforms, boneCount);
            UpdateWorldTransforms(Matrix.Identity);
            UpdateSkinTransforms();
        }

        public void StartClip(AnimationClip clip)
        {
            currentClip = clip;
            currentTime = TimeSpan.Zero;
            currentKeyframe = 0;

            Array.Copy(skinningData.BindPose, boneTransforms, boneTransforms.Length);
        }

        public void Update(TimeSpan elapsed, bool loop, Matrix rootTransform)
        {
            UpdateBoneTransforms(elapsed, loop);
            UpdateWorldTransforms(rootTransform);
            UpdateSkinTransforms();
        }

        void UpdateBoneTransforms(TimeSpan elapsed, bool loop)
        {
            if (currentClip == null)
                return;

            TimeSpan time = currentTime + elapsed;

            if (currentClip.Duration > TimeSpan.Zero)
            {
                if (loop)
                {
                    while (time >= currentClip.Duration)
                        time -= currentClip.Duration;
                    while (time < TimeSpan.Zero)
                        time += currentClip.Duration;
                }
                else
                {
                    if (time > currentClip.Duration)
                        time = currentClip.Duration;
                    if (time < TimeSpan.Zero)
                        time = TimeSpan.Zero;
                }
            }

            // Ha visszaugrottunk az idoben (loop), kezdjuk ujra a bindpose-bol.
            if (time < currentTime)
            {
                currentKeyframe = 0;
                Array.Copy(skinningData.BindPose, boneTransforms, boneTransforms.Length);
            }

            currentTime = time;

            List<Keyframe> keyframes = currentClip.Keyframes;
            while (currentKeyframe < keyframes.Count && keyframes[currentKeyframe].Time <= currentTime)
            {
                Keyframe keyframe = keyframes[currentKeyframe];
                boneTransforms[keyframe.Bone] = keyframe.Transform;
                currentKeyframe++;
            }
        }

        void UpdateWorldTransforms(Matrix rootTransform)
        {
            worldTransforms[0] = boneTransforms[0] * rootTransform;

            for (int bone = 1; bone < worldTransforms.Length; bone++)
            {
                int parentBone = skinningData.SkeletonHierarchy[bone];
                worldTransforms[bone] = boneTransforms[bone] * worldTransforms[parentBone];
            }
        }

        void UpdateSkinTransforms()
        {
            for (int bone = 0; bone < skinTransforms.Length; bone++)
                skinTransforms[bone] = skinningData.InverseBindPose[bone] * worldTransforms[bone];
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FishSim.Animation
{
    public class SkinningDataReader : ContentTypeReader<SkinningData>
    {
        protected override SkinningData Read(ContentReader input, SkinningData existingInstance)
        {
            int boneCount = input.ReadInt32();
            var bindPose = new Matrix[boneCount];
            var inverseBindPose = new Matrix[boneCount];
            var skeletonHierarchy = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
                bindPose[i] = input.ReadMatrix();
            for (int i = 0; i < boneCount; i++)
                inverseBindPose[i] = input.ReadMatrix();
            for (int i = 0; i < boneCount; i++)
                skeletonHierarchy[i] = input.ReadInt32();

            int clipCount = input.ReadInt32();
            var clips = new Dictionary<string, AnimationClip>(clipCount);
            for (int c = 0; c < clipCount; c++)
            {
                string name = input.ReadString();
                long durationTicks = input.ReadInt64();
                int kfCount = input.ReadInt32();
                var keyframes = new List<Keyframe>(kfCount);
                for (int k = 0; k < kfCount; k++)
                {
                    int bone = input.ReadInt32();
                    long time = input.ReadInt64();
                    Matrix transform = input.ReadMatrix();
                    keyframes.Add(new Keyframe(bone, TimeSpan.FromTicks(time), transform));
                }
                clips.Add(name, new AnimationClip(TimeSpan.FromTicks(durationTicks), keyframes));
            }

            return new SkinningData(clips, bindPose, inverseBindPose, skeletonHierarchy);
        }
    }
}

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using FishSim.Animation;

namespace FishSim.Pipeline
{
    [ContentTypeWriter]
    public class SkinningDataWriter : ContentTypeWriter<SkinningData>
    {
        protected override void Write(ContentWriter output, SkinningData value)
        {
            output.Write(value.BindPose.Length);
            foreach (var m in value.BindPose)
                output.Write(m);
            foreach (var m in value.InverseBindPose)
                output.Write(m);
            foreach (var parent in value.SkeletonHierarchy)
                output.Write(parent);

            output.Write(value.AnimationClips.Count);
            foreach (var kvp in value.AnimationClips)
            {
                output.Write(kvp.Key);
                output.Write(kvp.Value.Duration.Ticks);
                output.Write(kvp.Value.Keyframes.Count);
                foreach (var kf in kvp.Value.Keyframes)
                {
                    output.Write(kf.Bone);
                    output.Write(kf.Time.Ticks);
                    output.Write(kf.Transform);
                }
            }
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "FishSim.Animation.SkinningData, FishSim";
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "FishSim.Animation.SkinningDataReader, FishSim";
        }
    }
}

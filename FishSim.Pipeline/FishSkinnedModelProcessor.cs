using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using FishSim.Animation; // Győződj meg róla, hogy ez a hivatkozás él a SkinningData miatt

namespace FishSim.Pipeline
{
    /// <summary>
    /// Tiszta, standard ModelProcessor a csontvázas animációk feldolgozásához.
    /// A kulcskockákat a "Skinning Sample" mintája szerint, módosítás nélkül tárolja el.
    /// </summary>
    [ContentProcessor(DisplayName = "Fish Skinned Model Processor")]
    public class FishSkinnedModelProcessor : ModelProcessor
    {
        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            // 1. Lefuttatjuk az alapértelmezett modell-feldolgozót (ez csinálja meg a mesht, materialokat)
            ModelContent model = base.Process(input, context);

            // 2. Kinyerjük a csontvázat és az animációkat az FBX node-fából
            ProcessSkeleton(input, out Matrix[] bindPose, out Matrix[] inverseBindPose, out int[] skeletonHierarchy);
            Dictionary<string, AnimationClip> animationClips = ProcessAnimations(input, bindPose, context);

            // 3. Becsomagoljuk a SkinningData objektumba, és beletesszük a Model.Tag-be
            model.Tag = new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);

            return model;
        }

        private void ProcessSkeleton(NodeContent input, out Matrix[] bindPose, out Matrix[] inverseBindPose, out int[] skeletonHierarchy)
        {
            // A csontokat a MeshHelper segitsegevel kell kilapitani, mert a base.Process()
            // (MeshProcessor) is ezt hasznalja a BLENDINDICES0 vertex-adat generalasahoz.
            // Egy sajat (pl. egyszeru rekurziv) kilistazas mas sorrendet/halmazt adhat,
            // ami azt eredmenyezi, hogy a shaderben Bones[blendIndices.x] egy MASIK
            // csont matrixat kapja - ez okozza a hatalmas, "tuskes" torzulasokat.
            List<BoneContent> bones = FlattenSkeleton(input);

            if (bones.Count == 0)
                throw new InvalidContentException("Nem talalhato csontvaz (Armature) a modellben!");

            bindPose = new Matrix[bones.Count];
            inverseBindPose = new Matrix[bones.Count];
            skeletonHierarchy = new int[bones.Count];

            for (int i = 0; i < bones.Count; i++)
            {
                BoneContent bone = bones[i];

                // A lokális transzformáció a szülőhöz képest (ezt adta a Blender)
                bindPose[i] = bone.Transform;

                // Az abszolút transzformáció inverze (ez kell a vertexek helyrehúzásához a shaderben)
                inverseBindPose[i] = Matrix.Invert(bone.AbsoluteTransform);

                // Szülő indexének megkeresése (ha van)
                skeletonHierarchy[i] = -1;
                if (bone.Parent is BoneContent parentBone)
                {
                    skeletonHierarchy[i] = bones.IndexOf(parentBone);
                }
            }
        }

        private List<BoneContent> FlattenSkeleton(NodeContent node)
        {
            BoneContent skeleton = MeshHelper.FindSkeleton(node);
            if (skeleton == null)
                return new List<BoneContent>();

            // MeshHelper.FlattenSkeleton ugyanazt a sorrendet adja, mint amit a
            // base.Process() (MeshProcessor) hasznal a BLENDINDICES0 generalasahoz.
            return new List<BoneContent>(MeshHelper.FlattenSkeleton(skeleton));
        }

        private Dictionary<string, AnimationClip> ProcessAnimations(NodeContent input, Matrix[] bindPose, ContentProcessorContext context)
        {
            var clips = new Dictionary<string, AnimationClip>();
            List<BoneContent> bones = FlattenSkeleton(input);

            // Az FBX-importalas soran az animacios csatornak nem a root node-on,
            // hanem az egyes csont-node-okon jelennek meg, ezert vegig kell jarni
            // a teljes node-fat es minden node sajat AnimationContent-jeit osszegyujteni.
            foreach (KeyValuePair<string, AnimationContent> animation in CollectAnimations(input))
            {
                if (!clips.TryGetValue(animation.Key, out AnimationClip clip))
                {
                    clip = new AnimationClip(animation.Value.Duration, new List<Keyframe>());
                    clips.Add(animation.Key, clip);
                }
                var keyframes = clip.Keyframes;

                if (animation.Value.Duration > clip.Duration)
                    clip.Duration = animation.Value.Duration;

                // Végigmegyünk az animáció csatornáin (minden csatorna egy csontot mozgat)
                foreach (KeyValuePair<string, AnimationChannel> channel in animation.Value.Channels)
                {
                    // Megkeressük, melyik csontra vonatkozik ez a csatorna
                    int boneIndex = bones.FindIndex(b => b.Name == channel.Key);
                    if (boneIndex == -1) continue;
                    if (channel.Value.Count == 0) continue;

                    // A kulcskocka transzformaciot kozvetlenul, valtozatlanul hasznaljuk
                    // (ahogy a XNA "Skinning Sample" is teszi): ez mar a csonthoz tartozo
                    // szulo-relativ lokalis transzformacio, ugyanabban a referenciakeretben,
                    // mint a bindPose[boneIndex] (bone.Transform).
                    foreach (AnimationKeyframe keyframe in channel.Value)
                        keyframes.Add(new Keyframe(boneIndex, keyframe.Time, keyframe.Transform));
                }
            }

            // Időrendbe állítjuk a kulcskockákat
            foreach (var clip in clips.Values)
                clip.Keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));

            return clips;
        }

        private IEnumerable<KeyValuePair<string, AnimationContent>> CollectAnimations(NodeContent node)
        {
            foreach (var animation in node.Animations)
                yield return animation;

            foreach (NodeContent child in node.Children)
                foreach (var animation in CollectAnimations(child))
                    yield return animation;
        }
    }
}
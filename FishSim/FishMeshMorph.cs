using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace FishSim
{
    // CPU vertex-morphing: minden mesh-vertexet bind-pose-ban a 3 legkozelebbi verlethez
    // ("csonthoz") kotunk, inverz-negyzetes tavolsag szerint sulyozva (linear blend skinning).
    // Per frame minden vertexet a hozzarendelt verletek bind/aktualis pozicio-kulonbsegenek
    // sulyozott osszegevel eltoljuk, model-local (a localTransform utani, de WorldTransform
    // elotti) terben. A rigid World matrix vegzi a fo mozgast/forgatast, ez csak a relativ
    // "rezgest"/uszomozgast adja hozza.
    class FishMeshMorph
    {
        struct BlendInfo
        {
            public int K0, K1, K2;
            public float W0, W1, W2;
        }

        class PartData
        {
            public DynamicVertexBuffer Buffer;
            public byte[] template;
            public byte[] scratch;
            public Vector3[] basePositions;
            public BlendInfo[] blend;
            public int stride;
            public int posOffset;
            public int vertexCount;
        }

        readonly PartData[] parts;
        readonly Vector3[] verletBindLocal;
        readonly int boneStart, boneEnd;
        readonly float maxInfluenceRadius;

        const float Epsilon = 1e-4f;

        public FishMeshMorph(Model model, Matrix bindWorld, Verlet[] verlets, int boneStart, int boneEnd)
        {
            this.boneStart = boneStart;
            this.boneEnd = boneEnd;

            Matrix invBindWorld = Matrix.Invert(bindWorld);
            verletBindLocal = new Vector3[verlets.Length];
            for (int i = boneStart; i <= boneEnd; i++)
                verletBindLocal[i] = Vector3.Transform(verlets[i].Pos, invBindWorld);

            // Hal-test hossza (gerinc farok-fej) a bind-local terben, ez adja az influence-radius alapjat.
            float fishBodyLength = (verletBindLocal[boneEnd] - verletBindLocal[boneStart]).Length();
            maxInfluenceRadius = 0.8f * fishBodyLength;

            var partList = new List<PartData>();
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    var decl = part.VertexBuffer.VertexDeclaration;
                    int stride = decl.VertexStride;
                    int vertexCount = part.VertexBuffer.VertexCount;
                    int posOffset = 0;
                    foreach (var elem in decl.GetVertexElements())
                        if (elem.VertexElementUsage == VertexElementUsage.Position && elem.UsageIndex == 0)
                            posOffset = elem.Offset;

                    var template = new byte[stride * vertexCount];
                    part.VertexBuffer.GetData(template);

                    var basePositions = new Vector3[vertexCount];
                    var blend = new BlendInfo[vertexCount];
                    for (int v = 0; v < vertexCount; v++)
                    {
                        int off = v * stride + posOffset;
                        var p = new Vector3(
                            BitConverter.ToSingle(template, off),
                            BitConverter.ToSingle(template, off + 4),
                            BitConverter.ToSingle(template, off + 8));
                        basePositions[v] = p;
                        blend[v] = ComputeBlend(p);
                    }

                    var buffer = new DynamicVertexBuffer(part.VertexBuffer.GraphicsDevice, decl, vertexCount, BufferUsage.WriteOnly);
                    buffer.SetData(template);
                    part.VertexBuffer = buffer;

                    partList.Add(new PartData
                    {
                        Buffer = buffer,
                        template = template,
                        scratch = new byte[template.Length],
                        basePositions = basePositions,
                        blend = blend,
                        stride = stride,
                        posOffset = posOffset,
                        vertexCount = vertexCount,
                    });
                }
            }
            parts = partList.ToArray();
        }

        // A 3 legkozelebbi verlet (bind-local terben) es az inverz-negyzetes tavolsag-sulyaik.
        BlendInfo ComputeBlend(Vector3 p)
        {
            // Top-3 legkozelebbi verlet kivalasztasa (egyszeru beillesztes 3 elemes rendezett tombbe)
            int[] bestK = { -1, -1, -1 };
            float[] bestD = { float.MaxValue, float.MaxValue, float.MaxValue };
            for (int k = boneStart; k <= boneEnd; k++)
            {
                float d = (p - verletBindLocal[k]).Length();
                if (d < bestD[0])
                {
                    bestD[2] = bestD[1]; bestK[2] = bestK[1];
                    bestD[1] = bestD[0]; bestK[1] = bestK[0];
                    bestD[0] = d; bestK[0] = k;
                }
                else if (d < bestD[1])
                {
                    bestD[2] = bestD[1]; bestK[2] = bestK[1];
                    bestD[1] = d; bestK[1] = k;
                }
                else if (d < bestD[2])
                {
                    bestD[2] = d; bestK[2] = k;
                }
            }

            var info = new BlendInfo { K0 = bestK[0], K1 = bestK[1], K2 = bestK[2] };

            if (bestD[0] < Epsilon)
            {
                // Pontos talalat: csak az egyetlen legkozelebbi verlet szamit, teljes sullyal.
                info.W0 = 1f; info.W1 = 0f; info.W2 = 0f;
                return info;
            }

            if (bestD[0] > maxInfluenceRadius)
            {
                // Tul tavoli vertex (uszo-/farokvegek hegye): ne extrapolaljunk vadul, csak a
                // legkozelebbi verlet hat ra, csillapitott (falloff) merteket.
                float falloff = MathHelper.Clamp(maxInfluenceRadius / bestD[0], 0f, 1f);
                info.W0 = falloff; info.W1 = 0f; info.W2 = 0f;
                return info;
            }

            // Inverz-negyzetes tavolsag-sulyok, normalizalva.
            float w0 = 1f / (bestD[0] * bestD[0]);
            float w1 = 1f / (bestD[1] * bestD[1]);
            float w2 = 1f / (bestD[2] * bestD[2]);
            float sum = w0 + w1 + w2;
            info.W0 = w0 / sum; info.W1 = w1 / sum; info.W2 = w2 / sum;
            return info;
        }

        // Frissiti a vertex-buffereket az aktualis verlet-pozicioknak megfeleloen.
        // world: a jelenlegi localTransform * WorldTransform (a Draw-ban hasznalt rigid matrix).
        public void Update(Matrix world, Verlet[] verlets)
        {
            Matrix invWorld = Matrix.Invert(world);
            var verletNowLocal = new Vector3[verlets.Length];
            for (int i = boneStart; i <= boneEnd; i++)
                verletNowLocal[i] = Vector3.Transform(verlets[i].Pos, invWorld);

            foreach (var pd in parts)
            {
                Array.Copy(pd.template, pd.scratch, pd.template.Length);
                for (int v = 0; v < pd.vertexCount; v++)
                {
                    var b = pd.blend[v];
                    Vector3 delta =
                        b.W0 * (verletNowLocal[b.K0] - verletBindLocal[b.K0]) +
                        b.W1 * (verletNowLocal[b.K1] - verletBindLocal[b.K1]) +
                        b.W2 * (verletNowLocal[b.K2] - verletBindLocal[b.K2]);
                    Vector3 p = pd.basePositions[v] + delta;
                    int off = v * pd.stride + pd.posOffset;
                    BitConverter.TryWriteBytes(new Span<byte>(pd.scratch, off, 4), p.X);
                    BitConverter.TryWriteBytes(new Span<byte>(pd.scratch, off + 4, 4), p.Y);
                    BitConverter.TryWriteBytes(new Span<byte>(pd.scratch, off + 8, 4), p.Z);
                }
                pd.Buffer.SetData(pd.scratch);
            }
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FishSim.Particles
{
    struct Particle
    {
        public Vector3 Position;
        public float Size;
        public Color Color;
        public float DriftSpeed;
        public float WobblePhase;
        public float WobbleAmplitude;
        public float WobbleFrequency;
    }

    class ParticleLayer
    {
        readonly ParticleLayerSettings settings;
        readonly Particle[] particles;
        readonly VertexPositionColor[] vertices;
        readonly DynamicVertexBuffer vertexBuffer;
        readonly IndexBuffer indexBuffer;
        readonly BasicEffect effect;

        public ParticleLayer(GraphicsDevice device, ParticleLayerSettings settings)
        {
            this.settings = settings;

            var rng = new Random(settings.Seed);
            particles = new Particle[settings.Count];
            for (int i = 0; i < particles.Length; i++)
            {
                var ext = settings.VolumeHalfExtents;
                var pos = new Vector3(
                    (float)(rng.NextDouble() * 2 - 1) * ext.X,
                    (float)(rng.NextDouble() * 2 - 1) * ext.Y,
                    (float)(rng.NextDouble() * 2 - 1) * ext.Z);

                float t = (float)rng.NextDouble();

                particles[i] = new Particle
                {
                    Position = pos,
                    Size = MathHelper.Lerp(settings.MinSize, settings.MaxSize, (float)rng.NextDouble()),
                    Color = Color.Lerp(settings.ColorA, settings.ColorB, t),
                    DriftSpeed = MathHelper.Lerp(settings.MinDriftSpeed, settings.MaxDriftSpeed, (float)rng.NextDouble()),
                    WobblePhase = (float)rng.NextDouble() * MathHelper.TwoPi,
                    WobbleAmplitude = MathHelper.Lerp(settings.MinWobbleAmplitude, settings.MaxWobbleAmplitude, (float)rng.NextDouble()),
                    WobbleFrequency = MathHelper.Lerp(settings.MinWobbleFrequency, settings.MaxWobbleFrequency, (float)rng.NextDouble()),
                };
            }

            vertices = new VertexPositionColor[particles.Length * 4];
            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);

            // 32-bites index buffer, mert nagy Count esetén a vertex-szám (Count*4) túlléphet a 65535-ös 16-bites határt
            var indices = new int[particles.Length * 6];
            for (int i = 0; i < particles.Length; i++)
            {
                int v = i * 4;
                int idx = i * 6;
                indices[idx + 0] = v;
                indices[idx + 1] = v + 1;
                indices[idx + 2] = v + 2;
                indices[idx + 3] = v;
                indices[idx + 4] = v + 2;
                indices[idx + 5] = v + 3;
            }
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);

            effect = new BasicEffect(device);
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;
            effect.TextureEnabled = false;
            effect.World = Matrix.Identity;
        }

        public void Update(GameTime gameTime, Vector3 cameraPosition)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var ext = settings.VolumeHalfExtents;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                p.Position.Y += p.DriftSpeed * dt;
                p.WobblePhase += p.WobbleFrequency * dt;

                var rel = p.Position - cameraPosition;
                if (rel.X > ext.X) rel.X -= 2 * ext.X;
                else if (rel.X < -ext.X) rel.X += 2 * ext.X;
                if (rel.Y > ext.Y) rel.Y -= 2 * ext.Y;
                else if (rel.Y < -ext.Y) rel.Y += 2 * ext.Y;
                if (rel.Z > ext.Z) rel.Z -= 2 * ext.Z;
                else if (rel.Z < -ext.Z) rel.Z += 2 * ext.Z;
                p.Position = cameraPosition + rel;
            }
        }

        public void Draw(Camera cam)
        {
            var device = vertexBuffer.GraphicsDevice;

            var forward = Vector3.Normalize(cam.Direction);
            var right = Vector3.Normalize(Vector3.Cross(cam.Up, forward));
            var up = Vector3.Cross(forward, right);

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                var wobble = new Vector3(
                    MathF.Sin(p.WobblePhase) * p.WobbleAmplitude,
                    0,
                    MathF.Cos(p.WobblePhase * 0.7f) * p.WobbleAmplitude);
                var center = p.Position + wobble;

                var rightOffset = right * p.Size;
                var upOffset = up * p.Size;

                int vi = i * 4;
                vertices[vi + 0] = new VertexPositionColor(center - rightOffset - upOffset, p.Color);
                vertices[vi + 1] = new VertexPositionColor(center + rightOffset - upOffset, p.Color);
                vertices[vi + 2] = new VertexPositionColor(center + rightOffset + upOffset, p.Color);
                vertices[vi + 3] = new VertexPositionColor(center - rightOffset + upOffset, p.Color);
            }

            vertexBuffer.SetData(vertices);

            device.BlendState = settings.Blend;
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            effect.View = cam.View;
            effect.Projection = cam.Projection;
            effect.CurrentTechnique.Passes[0].Apply();

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, particles.Length * 2);
        }
    }
}

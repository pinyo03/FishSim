using Game1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FishSim
{
    class Fish : Body
    {
        public bool ctrlW, ctrlS, ctrlA, ctrlD;
        public Vector3[] posErrors;
        public Vector3 Position => (verlets[0].Pos + verlets[1].Pos + verlets[2].Pos + verlets[3].Pos) * 0.25f;
        public Vector3 Direction => verlets[0].Pos - verlets[3].Pos;
        public Vector3 Right => verlets[1].Pos - verlets[0].Pos;
        public Vector3 Up => 2 * verlets[4].Pos - verlets[0].Pos - verlets[2].Pos;
        public Matrix WorldTransform => Matrix.CreateWorld(Position, Vector3.Normalize(Direction), Vector3.Normalize(Up));
        Model model;
        Matrix localTransform = Matrix.CreateScale(0.3f) * Matrix.CreateRotationY(MathHelper.Pi);
        public Fish(Fish fish) : base(fish)
        {
            model = fish.model;
            posErrors = new Vector3[verlets.Length];
        }
        public Fish(GraphicsDevice dev, Model model, Vector3 sunDir)
        {
            this.model = model;
            int meshCount = 0;
            foreach (var mesh in model.Meshes)
            {
                if (meshCount++ > 3)
                    break;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = true;
                    effect.PreferPerPixelLighting = true;
                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight0.Direction = -sunDir;
                    effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                    effect.DirectionalLight0.SpecularColor = new Vector3(1, 1, 1);
                    effect.DirectionalLight1.Enabled = true;
                    effect.DirectionalLight1.Direction = Vector3.Down;
                    effect.DirectionalLight1.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
                    effect.PreferPerPixelLighting = true;
                }
            }
            float w = 0.2f, l = 1;
            var rng = new Random();
            var pos = new Vector3(
                (float)rng.NextDouble() * 20, 5,
                (float)rng.NextDouble() * 20);
            verlets = new Verlet[] {
                new Verlet(pos + new Vector3(l, 0, -w)),
                new Verlet(pos + new Vector3(l, 0, w)),
                new Verlet(pos + new Vector3(-l, 0, w)),
                new Verlet(pos + new Vector3(-l, 0, -w)),
                new Verlet(pos + new Vector3(0, 0.5f, 0)),
                new Verlet(pos + new Vector3(-l, -0.8f, 0))
            };
            GenerateFullyConnectedBody();
        }
        public void Draw(Camera cam)
        {
            int meshCount = 0;
            foreach (var mesh in model.Meshes)
            {
                if (meshCount++ > 3)
                    break;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = localTransform * WorldTransform;
                    effect.View = cam.View;
                    effect.Projection = cam.Projection;
                }
                mesh.Draw();
            }
        }
        public void Step()
        {
            ApplyForces();
            for (int i = 0; i < verlets.Length; i++)
            {
                verlets[i].Step();
                if (posErrors != null)
                {
                    verlets[i].Pos += posErrors[i] * 0.01f;
                    verlets[i].pPos += posErrors[i] * 0.01f;
                }
            }
            ApplyConstraints();
        }
        private void ApplyForces()
        {
            Vector3 g = new Vector3(0, -9.81f, 0);
            for (int i = 0; i < verlets.Length; i++)
                verlets[i].Acc = g;
            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);
            for (int i = 0; i < 4; i++)
            {
                float height = verlets[i].Pos.Y;
                if (height < 0)
                {
                    verlets[i].Acc += Vector3.Up * Math.Min(-height * 50, 30);
                    verlets[i].AddSqFriction(Vector3.Up, 10);
                }
                float fHeight = Math.Max(0.5f - verlets[i].Pos.Y, 0);
                verlets[i].AddSqFriction(d, 0.05f * fHeight);
                verlets[i].AddSqFriction(r, 0.5f * fHeight);
                verlets[i].AddSqFriction(u, 5 * fHeight);
            }
            if (ctrlW)
                verlets[5].Acc += d * 10;
            if (ctrlS)
                verlets[5].Acc -= d * 5;
            if (ctrlA)
                verlets[5].Acc += r * (verlets[5].Velocity.Length() + 2) * 0.2f;
            if (ctrlD)
                verlets[5].Acc -= r * (verlets[5].Velocity.Length() + 2) * 0.2f;
        }
    }
}

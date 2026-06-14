using Game1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq.Expressions;

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
        Matrix localTransform = Matrix.CreateScale(0.1f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateRotationX(MathHelper.PiOver2);
        public Fish(Fish fish) : base(fish)
        {
            model = fish.model;
            posErrors = new Vector3[verlets.Length];
        }
        public Fish(GraphicsDevice dev, Model model, Texture2D texture, Texture2D normalTexture, Effect fishEffect, Vector3 sunDir)
        {
            this.model = model;
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    var effect = fishEffect.Clone();
                    effect.Parameters["Texture"].SetValue(texture);
                    effect.Parameters["NormalTexture"].SetValue(normalTexture);
                    effect.Parameters["AmbientColor"].SetValue(new Vector3(0.3f, 0.3f, 0.3f));
                    effect.Parameters["Light0Dir"].SetValue(Vector3.Normalize(-sunDir));
                    effect.Parameters["Light0Color"].SetValue(new Vector3(0.7f, 0.7f, 0.7f));
                    effect.Parameters["Light1Dir"].SetValue(Vector3.Down);
                    effect.Parameters["Light1Color"].SetValue(new Vector3(0.2f, 0.2f, 0.2f));
                    part.Effect = effect;
                }
            }
            float w = 0.2f, l = 1;
            var rng = new Random();
            var pos = new Vector3(
                (float)rng.NextDouble() * 20, 5,
                (float)rng.NextDouble() * 20);
            verlets = new Verlet[] {
                // messzi keretezés nem lesz része a halnak csak ha kiveszem minden másik pont elcsúszik
                new Verlet(pos + new Vector3(5*l, 0, -5*w)),
                new Verlet(pos + new Vector3(5 * l, 0, 5*w)),
                new Verlet(pos + new Vector3(-5*l, 0, 5*w)),
                new Verlet(pos + new Vector3(-5*l, 0, -5*w)),

                new Verlet(pos + new Vector3(-1.16f, 0.08f, 0)),                            //farok alsó vége
                new Verlet(pos + new Vector3(-0.76f, 0.25f, 0)),                            //farok alsó töve
                new Verlet(pos + new Vector3(-1.16f, 0.85f, 0)),                            //farok felső vége
                new Verlet(pos + new Vector3(-0.76f, 0.65f, 0)),                            //farok felső töve
                        
                new Verlet(pos + new Vector3(1.1f, -0.25f, -0.075f)),                       //bal melső úszó alsó vége
                new Verlet(pos + new Vector3(1, -0.02f, -0.084f)),                          //bal melső úszó hátsó vége
                new Verlet(pos + new Vector3(1.17f, 0.08f, -0.074f)),                       //bal melső úszó töve

                new Verlet(pos + new Vector3(1.1f, -0.25f, 0.075f)),                        //jobb melső úszó alsó vége
                new Verlet(pos + new Vector3(1, -0.02f, 0.084f)),                           //jobb melső úszó hátsó vége
                new Verlet(pos + new Vector3(1.17f, 0.08f, 0.074f)),                        //jobb melső úszó töve

                new Verlet(pos + new Vector3(0, -0.16f, 0)),                                //has középi úszó alsó vége
                new Verlet(pos + new Vector3(-0.26f, 0.1f, 0)),                             //has középi úszó felső vége
                new Verlet(pos + new Vector3(0.17f, 0.16f, 0)),                             //has középi úszó töve

                new Verlet(pos + new Vector3(0.15f, 1.1f, 0)),                              //hát középi úszó első vége
                new Verlet(pos + new Vector3(0.45f, 0.8f, 0)),                              //hát középi úszó első töve
                new Verlet(pos + new Vector3(0.1f, 0.7f, 0)),                               //hát középi úszó hátsó vége
                new Verlet(pos + new Vector3(-0.19f, 0.8f, 0)),                             //hát középi úszó hátsó töve

                new Verlet(pos + new Vector3(1.3f, 0.3f, 0.3f)),                            //jobb első úszó töve
                new Verlet(pos + new Vector3(1.03f, 0.43f, 0.6f)),                          //jobb első úszó felső vége
                new Verlet(pos + new Vector3(1.03f, 0.23f, 0.6f)),                          //jobb első úszó alsó vége

                new Verlet(pos + new Vector3(1.3f, 0.3f, -0.3f)),                           //bal első úszó töve
                new Verlet(pos + new Vector3(1.21f, 0.43f, -0.6f)),                         //bal első úszó felső vége
                new Verlet(pos + new Vector3(1.09f, 0.23f, -0.6f)),                         //bal első úszó alsó vége

                new Verlet(pos + new Vector3(-0.5f, 0.45f, 0)),                            //7. csigolya a faroknál
                new Verlet(pos + new Vector3(-0.1f, 0.45f, 0)),                            //6. csigolya
                new Verlet(pos + new Vector3(0.3f, 0.45f, 0)),                             //5. csigolya 
                new Verlet(pos + new Vector3(0.7f, 0.45f, 0)),                             //4. csigolya
                new Verlet(pos + new Vector3(1.1f, 0.45f, 0)),                             //3. csigolya
                new Verlet(pos + new Vector3(1.5f, 0.45f, 0)),                             //2. csigolya 
                new Verlet(pos + new Vector3(1.9f, 0.45f, 0)),                             //1. csigolya a fejnél
                new Verlet(pos + new Vector3(2.25f, 0.45f, 0)),                             //fej legeleje
            };
            GenerateFullyConnectedBody();
        }
        public void UpdateCaustics(float time)
        {
            // Kekes ambient a hal aktualis magassagahoz tartozo viz-szin alapjan.
            var ambient = WaterColorSettings.GetColorAtHeight(Position.Y) * 0.6f;
            foreach (var mesh in model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    CausticsSettings.Apply(part.Effect, time);
                    part.Effect.Parameters["AmbientColor"]?.SetValue(ambient);
                }
        }
        public void Draw(Camera cam)
        {
            var world = localTransform * WorldTransform;
            var worldIT = Matrix.Transpose(Matrix.Invert(world));
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    var effect = part.Effect;
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["View"].SetValue(cam.View);
                    effect.Parameters["Projection"].SetValue(cam.Projection);
                    effect.Parameters["WorldIT"].SetValue(worldIT);
                    WaterColorSettings.Apply(effect, cam.Position);
                }
                mesh.Draw();
            }
        }
        public void DrawDebugVerlets(Camera cam, BasicGeometry sphere)
        {
            foreach (var v in verlets)
            {
                var world = Matrix.CreateScale(0.05f) * Matrix.CreateTranslation(v.Pos);
                sphere.Draw(world, cam.View, cam.Projection);
            }
        }
        public void Step()
        {
            // TEMP: hal lefagyasztva a tomegpontok pozicionalasahoz - eredeti logika kikommentelve
            return;
            /*
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
            */
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

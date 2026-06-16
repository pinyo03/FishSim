using Game1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Linq.Expressions;
using FishSim.Animation;

namespace FishSim
{
    // Egy mesh PBR textura-keszlete (BaseColor + Normal + Metallic + Roughness, AO opcionalis).
    class FishMaterial
    {
        public Texture2D BaseColor, Normal, Metallic, Roughness, AO;
    }

    class Fish : Body
    {
        public bool ctrlW, ctrlS, ctrlA, ctrlD, ctrlQ, ctrlE;
        public float SizeScale = 1.0f;
        public Vector3 BoidSteerDir = Vector3.Zero;  // ha non-zero: AI proporcióalis irányítás
        public Vector3[] posErrors;
        // Az utkozesi ellipszoid felmeretei a hal lokalis tengelyei menten (elore/Direction, oldalra/Right, fel/Up).
        public float CollisionRadiusForward = 1.2f;
        public float CollisionRadiusRight = 0.5f;
        public float CollisionRadiusUp = 0.6f;
        public Vector3 Position => (verlets[0].Pos + verlets[1].Pos + verlets[2].Pos + verlets[3].Pos) * 0.25f;
        public Vector3 Direction => verlets[0].Pos - verlets[3].Pos;
        public Vector3 Right => verlets[1].Pos - verlets[0].Pos;
        public Vector3 Up => Vector3.Cross(Right, Direction);
        public Matrix WorldTransform => Matrix.CreateWorld(Position, Vector3.Normalize(Direction), Vector3.Normalize(Up));

        // Az utkozesi ellipszoidot a halhoz igazito (forgatott + nem-uniform skalazott) vilagmatrix.
        public Matrix CollisionWorldTransform
        {
            get
            {
                Vector3 d = Vector3.Normalize(Direction);
                Vector3 r = Vector3.Normalize(Right);
                Vector3 u = Vector3.Normalize(Up);
                return new Matrix(
                    d.X * CollisionRadiusForward, d.Y * CollisionRadiusForward, d.Z * CollisionRadiusForward, 0,
                    u.X * CollisionRadiusUp, u.Y * CollisionRadiusUp, u.Z * CollisionRadiusUp, 0,
                    r.X * CollisionRadiusRight, r.Y * CollisionRadiusRight, r.Z * CollisionRadiusRight, 0,
                    Position.X, Position.Y, Position.Z, 1);
            }
        }
        Model model;
        GraphicsDevice graphicsDevice;
        AnimationPlayer animationPlayer;
        Matrix localTransform = Matrix.CreateScale(0.4f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateRotationX(MathHelper.PiOver2);
        public Fish(Fish fish) : base(fish)
        {
            model = fish.model;
            graphicsDevice = fish.graphicsDevice;
            posErrors = new Vector3[verlets.Length];
        }
        public Fish(GraphicsDevice dev, Model model, FishMaterial bodyMaterial, FishMaterial eyesMaterial, Effect fishEffect, Vector3 sunDir, Vector3? spawnPos = null)
        {
            this.model = model;
            this.graphicsDevice = dev;
            var whiteAO = new Texture2D(dev, 1, 1);
            whiteAO.SetData(new[] { Color.White });

            // A SkinnedModelProcessor a Model.Tag-ben adja vissza a csontvazat es az animacios klipeket
            // (a sztenderd ModelProcessor ezt eldobja). Ha jelen van, elindítjuk az elso (egyetlen) klipet.
            if (model.Tag is SkinningData skinningData)
            {
                // Az uszas-animacio jelenleg hibas csontvaz-torzulast okoz ("tuskes csillag"),
                // ezert csak a bind pose-t hasznaljuk (nincs StartClip), a hal mozdulatlan marad.
                animationPlayer = new AnimationPlayer(skinningData);
                Console.WriteLine($"Fish: {skinningData.AnimationClips.Count} animacios klip, {skinningData.BindPose.Length} csont (animacio kikapcsolva).");
            }

            foreach (var mesh in model.Meshes)
            {
                // "EyesC" es "CorneaC" mesh-ek egyutt alkotjak a szemet, mindketto az EYS_Material textura-keszletet hasznalja.
                bool isEyes = mesh.Name.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0
                    || mesh.Name.IndexOf("cornea", StringComparison.OrdinalIgnoreCase) >= 0;
                var material = isEyes ? eyesMaterial : bodyMaterial;
                foreach (var part in mesh.MeshParts)
                {
                    Console.WriteLine($"Fish mesh '{mesh.Name}' -> {(isEyes ? "Eyes" : "Body")} material");

                    var effect = fishEffect.Clone();
                    effect.Parameters["Texture"].SetValue(material.BaseColor);
                    effect.Parameters["NormalTexture"].SetValue(material.Normal);
                    effect.Parameters["MetallicTexture"].SetValue(material.Metallic);
                    effect.Parameters["RoughnessTexture"].SetValue(material.Roughness);
                    effect.Parameters["AOTexture"].SetValue(material.AO ?? whiteAO);
                    effect.Parameters["HasAO"].SetValue(material.AO != null);
                    effect.Parameters["AmbientColor"].SetValue(new Vector3(0.12f, 0.22f, 0.45f));
                    effect.Parameters["Light0Dir"].SetValue(Vector3.Normalize(-sunDir));
                    effect.Parameters["Light0Color"].SetValue(new Vector3(0.45f, 0.60f, 0.90f));
                    effect.Parameters["Light1Dir"].SetValue(Vector3.Down);
                    effect.Parameters["Light1Color"].SetValue(new Vector3(0.08f, 0.14f, 0.30f));

                    // A Body mesh (csontvazas, animalt) a skinning technique-et hasznalja; a szem rideg marad.
                    if (!isEyes && animationPlayer != null)
                        effect.CurrentTechnique = effect.Techniques["FishLitSkinned"];
                    
                    part.Effect = effect;
                }
            }
            float w = 0.2f, l = 1;
            Vector3 pos;
            if (spawnPos.HasValue)
                pos = spawnPos.Value;
            else
            {
                var rng = new Random();
                pos = new Vector3((float)rng.NextDouble() * 20, -30, (float)rng.NextDouble() * 20);
            }
            verlets = new Verlet[] {
                new Verlet(pos + new Vector3(l, 0, -2*w)),
                new Verlet(pos + new Vector3(l, 0, 2*w)),
                new Verlet(pos + new Vector3(-l, 0, 2*w)),
                new Verlet(pos + new Vector3(-l, 0, -2*w)),
            };
            GenerateFullyConnectedBody();
        }
        public void UpdateAnimation(GameTime gameTime)
        {
            animationPlayer?.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);
        }
        public void UpdateCaustics(float time)
        {
            // Kekes ambient a hal aktualis magassagahoz tartozo viz-szin alapjan.
            var ambient = WaterColorSettings.GetColorAtHeight(Position.Y) * 2.1f;
            foreach (var mesh in model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    CausticsSettings.Apply(part.Effect, time);
                    part.Effect.Parameters["AmbientColor"]?.SetValue(ambient);
                }
        }
        public void Draw(Camera cam)
        {
            var world = Matrix.CreateScale(SizeScale) * localTransform * WorldTransform;
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
                    if (animationPlayer != null)
                        effect.Parameters["Bones"]?.SetValue(animationPlayer.Bones);
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
        public void Step(Seabed seabed, Matrix seabedTransform)
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

            // Seabed-collision: az utkozesi ellipszoid legalsó pontja ne mehessen
            // a terep felulete ala. Az ellipszoid lokalis tengelyei (d,r,u) menten
            // vett fel-le kiterjedese: sqrt((rF*d.Y)^2 + (rR*r.Y)^2 + (rU*u.Y)^2).
            Vector3 d2 = Vector3.Normalize(Direction);
            Vector3 r2 = Vector3.Normalize(Right);
            Vector3 u2 = Vector3.Normalize(Up);
            float verticalExtent = MathF.Sqrt(
                MathF.Pow(CollisionRadiusForward * d2.Y, 2) +
                MathF.Pow(CollisionRadiusRight * r2.Y, 2) +
                MathF.Pow(CollisionRadiusUp * u2.Y, 2));

            float seabedY = seabed.GetHeightAt(Position.X, Position.Z, seabedTransform);
            float penetration = (seabedY + verticalExtent) - Position.Y;
            if (penetration > 0)
            {
                // A pozicio-korrekcio pPos-t is elcsusztatja, hogy ne injektaljon extra
                // (a korrekciobol szarmazo) sebesseget; csak a tenyleges lefele-sebesseg
                // egy kis resze (restitution) pattan vissza.
                const float restitution = 0.675f;
                for (int i = 0; i < verlets.Length; i++)
                {
                    verlets[i].Pos.Y += penetration;
                    verlets[i].pPos.Y += penetration;

                    float velY = verlets[i].Pos.Y - verlets[i].pPos.Y;
                    if (velY < 0)
                        verlets[i].pPos.Y = verlets[i].Pos.Y + velY * restitution;
                }
            }
        }
        private void ApplyForces()
        {
            for (int i = 0; i < verlets.Length; i++)
                verlets[i].Acc = Vector3.Zero;

            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);

            // Aramvonalas (anizotrop) kozegellenallas: elore alig lassul, oldalra/fel-le erosen.
            for (int i = 0; i < verlets.Length; i++)
            {
                Vector3 vel = verlets[i].Pos - verlets[i].pPos;

                float vForward = Vector3.Dot(vel, d);
                float vRight = Vector3.Dot(vel, r);
                float vUp = Vector3.Dot(vel, u);

                verlets[i].Acc -= d * vForward * 2.0f;
                verlets[i].Acc -= r * vRight * 40.0f;
                verlets[i].Acc -= u * vUp * 40.0f;
            }

            // Tokesuly / auto-stabilizacio: a hal "fel" iranya (u) terjen vissza a globalis
            // fel irany hala-dolesi sikra projektalt verziojahoz, fuggetlenul attol, hogy
            // eppen oldalara dolt vagy teljesen a hasan/hatan uszik (negativ feedback mindket esetben).
            Vector3 worldUp = Vector3.Up;
            Vector3 desiredUp = worldUp - d * Vector3.Dot(worldUp, d);
            if (desiredUp.LengthSquared() > 1e-6f)
            {
                desiredUp = Vector3.Normalize(desiredUp);
                float stabK = 15.0f;
                float magnitude = Vector3.Dot(r, desiredUp) * stabK;
                verlets[0].Acc += magnitude * u;
                verlets[3].Acc += magnitude * u;
                verlets[1].Acc -= magnitude * u;
                verlets[2].Acc -= magnitude * u;
            }

            // Jatekos iranyitas
            float speed = 2.0f;
            float turnSpeed = 0.6f;

            if (ctrlW)
            {
                for (int i = 0; i < 4; i++) verlets[i].Acc += d * speed;
            }
            if (ctrlS)
            {
                // Fek: kiterjesztett uszok, sebessegfuggo fekezo ero.
                for (int i = 0; i < 4; i++)
                {
                    Vector3 vel = verlets[i].Pos - verlets[i].pPos;
                    verlets[i].Acc -= vel * 80f;
                }
            }

            // Yaw (kanyarodas balra/jobbra): orr (0,1) es farok (2,3) ellentetes iranyba.
            if (ctrlA)
            {
                verlets[0].Acc -= r * turnSpeed; verlets[1].Acc -= r * turnSpeed;
                verlets[2].Acc += r * turnSpeed; verlets[3].Acc += r * turnSpeed;
            }
            if (ctrlD)
            {
                verlets[0].Acc += r * turnSpeed; verlets[1].Acc += r * turnSpeed;
                verlets[2].Acc -= r * turnSpeed; verlets[3].Acc -= r * turnSpeed;
            }

            // Pitch (bolintas fel/le): orr (0,1) es farok (2,3) ellentetes iranyba.
            if (ctrlQ) // orr le
            {
                verlets[0].Acc -= u * turnSpeed; verlets[1].Acc -= u * turnSpeed;
                verlets[2].Acc += u * turnSpeed; verlets[3].Acc += u * turnSpeed;
            }
            if (ctrlE) // orr fel
            {
                verlets[0].Acc += u * turnSpeed; verlets[1].Acc += u * turnSpeed;
                verlets[2].Acc -= u * turnSpeed; verlets[3].Acc -= u * turnSpeed;
            }

            // AI boid iranyitas: proporcialis nyomatek + elore tolero ero (nem binalris ctrl-flagek).
            // BoidSteerDir a kivant vilagter-beli irany (normalizalt). A hal lokalis tengelyeire
            // vetitve yaw/pitch nyomatekot kapunk; az ores tengelyen nincs kiegyensulyozatlan ero.
            if (BoidSteerDir.LengthSquared() > 0.0001f)
            {
                const float aiSpeed  = 2.8f;  // > player speed (2.0f) hogy utolérjék a leadert
                const float steerK   = 1.5f;
                for (int i = 0; i < 4; i++) verlets[i].Acc += d * aiSpeed;
                float yaw   = Vector3.Dot(BoidSteerDir, r) * steerK;
                float pitch = Vector3.Dot(BoidSteerDir, u) * steerK;
                // yaw>0 → orr jobra (r iranyba), pitch>0 → orr fel (u iranyba)
                Vector3 torque = r * yaw + u * pitch;
                verlets[0].Acc += torque; verlets[1].Acc += torque;
                verlets[2].Acc -= torque; verlets[3].Acc -= torque;
            }
        }
    }
}

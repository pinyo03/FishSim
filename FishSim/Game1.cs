using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FishSim.Particles;
using System;
using System.Collections.Generic;
using Game1;


namespace FishSim
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        Sky sky;
        Fish fish;
        List<Fish> fishes = new List<Fish>();
        Seabed seabed;
        ParticleSystem particles;

        public bool fishCam = true;
        bool tabReleased = true;

        BasicGeometry debugSphere;
        bool showDebugVerlets = false;
        bool f1Released = true;

        BasicGeometry collisionSphere;
        bool showCollisionSphere = false;
        bool f2Released = true;

        Matrix seabedHeightTransform;
        Flock flock;

        Model coralModel;
        List<Matrix> coralTransforms = new();
        List<(Vector3 center, float rxz, float ry)> coralColliders = new();
        BasicGeometry coralCollisionEllipsoid;
        bool showCoralCollision = false;
        bool f3Released = true;

        Model tableCoralModel;
        List<(Matrix transform, int colorIdx)> tableCoralInstances = new();
        List<Matrix> tableCoralColliderMatrices = new();
        Effect[][] tableCoralEffects;
        int tableCoralPartCount;

        // Oktáns-alapú collision bucket-ek: index = (X≥0?4:0)|(Y≥0?2:0)|(Z≥0?1:0)
        List<(Vector3 center, float rxz, float ry)>[] _coralBuckets;
        List<Matrix>[] _tableCoralBuckets;

        float _yaw;
        float _pitch;
        const float Sensitivity = 0.003f;
        const float PitchLimit  = 1.45f;   // ~83 degrees, avoids gimbal lock at ±90

        int _previousScrollValue;
        float _cameraZoom = 5f;
        bool _wasLeftButtonPressed = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.GraphicsProfile = GraphicsProfile.HiDef; // ← ezt add hozzá
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            this.Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (s, e) => {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();
            };
            base.Initialize();
        }

        protected override void LoadContent()
        {
            sky = new Sky(GraphicsDevice, Content.Load<Texture2D>("fishsky1"), Content.Load<Effect>("Sky"));
            var fishEffect = Content.Load<Effect>("FishLit");

            var bodyMaterial = new FishMaterial
            {
                BaseColor = Content.Load<Texture2D>("tuna-fish/textures/TunaHLW_Material_BaseColor"),
                Normal = Content.Load<Texture2D>("tuna-fish/textures/TunaHLW_Material_Normal"),
                Metallic = Content.Load<Texture2D>("tuna-fish/textures/TunaHLW_Material_Metallic"),
                Roughness = Content.Load<Texture2D>("tuna-fish/textures/TunaHLW_Material_Roughness"),
                AO = Content.Load<Texture2D>("tuna-fish/textures/Material_ambient_occlusion"),
            };
            var greyEye = new Texture2D(GraphicsDevice, 1, 1);
            greyEye.SetData(new[] { new Color(15, 15, 15) });
            var silverMetallic = new Texture2D(GraphicsDevice, 1, 1);
            silverMetallic.SetData(new[] { Color.White }); // fully metallic
            var silverRoughness = new Texture2D(GraphicsDevice, 1, 1);
            silverRoughness.SetData(new[] { Color.Black }); // roughness = 0, mirror-sharp highlight
            var eyesMaterial = new FishMaterial
            {
                BaseColor = greyEye,
                Normal = Content.Load<Texture2D>("tuna-fish/textures/EYS_Material_Normal"),
                Metallic = silverMetallic,
                Roughness = silverRoughness,
            };
            fish = new Fish(GraphicsDevice, Content.Load<Model>("tuna-fish/source/TunaFish"), bodyMaterial, eyesMaterial, fishEffect, sky.SunDir);
            fishes.Add(fish);

            var flockModel = Content.Load<Model>("tuna-fish/source/TunaFish");
            var flockBoids = new System.Collections.Generic.List<Fish>();
            var spawnRng   = new Random(42);
            for (int i = 0; i < 150; i++)
            {
                Vector3 candidate = Vector3.Zero;
                bool placed = false;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    candidate = fish.Position + RandomOnUnitSphere(spawnRng) * Flock.SpawnRadius;
                    candidate.Y = MathHelper.Clamp(candidate.Y, -55f, -5f);

                    bool overlaps = false;
                    foreach (var f in fishes)
                    {
                        float minDist = f == fish
                            ? Flock.LeaderExclusionRadius + 1.0f
                            : Flock.MinSpawnGap;
                        if ((f.Position - candidate).Length() < minDist)
                        { overlaps = true; break; }
                    }

                    if (!overlaps) { placed = true; break; }
                }
                if (!placed) continue;

                var ai = new Fish(GraphicsDevice, flockModel, bodyMaterial, eyesMaterial, fishEffect, sky.SunDir, candidate);
                fishes.Add(ai);
                flockBoids.Add(ai);
            }
            flock = new Flock(fish, flockBoids);

            seabed = new Seabed(GraphicsDevice, Content.Load<Texture2D>("seabedColor"),
                sky.SunDir, Content.Load<Effect>("Seabed"));

            // A rétegek a ParticleLayerSettings alapértékeit használják (méret, szín, drift, wobble stb.
            // mind ott állítható); itt csak a seedet és a blend módot különböztetjük meg réteg szerint.
            particles = new ParticleSystem(GraphicsDevice,
                new ParticleLayerSettings { Seed = 1001, Blend = BlendState.NonPremultiplied },
                new ParticleLayerSettings { Seed = 2002, Blend = BlendState.Additive },
                new ParticleLayerSettingsBubbles());

            // Initialise look angles to match the fish's starting heading
            var flatDir = Vector3.Normalize(new Vector3(fish.Direction.X, 0, fish.Direction.Z));
            _yaw   = MathF.Atan2(flatDir.X, flatDir.Z);
            _pitch = -0.15f;

            // Pre-centre mouse so the first frame delta is zero
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

            debugSphere = BasicGeometry.CreateSphere(GraphicsDevice);
            debugSphere.Effect.DiffuseColor = Color.Red.ToVector3();

            collisionSphere = BasicGeometry.CreateSphere(GraphicsDevice);
            collisionSphere.Effect.DiffuseColor = Color.Yellow.ToVector3();
            collisionSphere.Effect.Alpha = 0.5f;

            // A seabed magassagteret ugyanide kell transzformalni, mint a Draw-ban a heightfield meshet.
            seabedHeightTransform = Matrix.CreateTranslation(0, -0.05f, 0)
                * Matrix.CreateScale(5000, 40f, 5000)
                * Matrix.CreateTranslation(-2500, -60, -2500);

            coralModel = Content.Load<Model>("coral-piece/source/coral fbx finished");
            var coralColorTex    = Content.Load<Texture2D>("coral-piece/Textures/coral_fbx_lambert1_BaseColor");
            var coralMetallicTex = Content.Load<Texture2D>("coral-piece/Textures/coral_fbx_lambert1_Metallic");
            var coralRoughTex    = Content.Load<Texture2D>("coral-piece/Textures/coral_fbx_lambert1_Roughness");
            var flatNormal = new Texture2D(GraphicsDevice, 1, 1);
            flatNormal.SetData(new[] { new Color(128, 128, 255) });
            var whiteAO = new Texture2D(GraphicsDevice, 1, 1);
            whiteAO.SetData(new[] { Color.White });
            var coralEffect = Content.Load<Effect>("FishLit");
            foreach (var mesh in coralModel.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    var eff = coralEffect.Clone();
                    eff.Parameters["Texture"].SetValue(coralColorTex);
                    eff.Parameters["NormalTexture"].SetValue(flatNormal);
                    eff.Parameters["MetallicTexture"].SetValue(coralMetallicTex);
                    eff.Parameters["RoughnessTexture"].SetValue(coralRoughTex);
                    eff.Parameters["AOTexture"].SetValue(whiteAO);
                    eff.Parameters["HasAO"].SetValue(false);
                    eff.Parameters["AmbientColor"].SetValue(new Vector3(0.30f, 0.30f, 0.32f));
                    eff.Parameters["Light0Dir"].SetValue(Vector3.Normalize(-sky.SunDir));
                    eff.Parameters["Light0Color"].SetValue(new Vector3(0.75f, 0.78f, 0.80f));
                    eff.Parameters["Light1Dir"].SetValue(Vector3.Down);
                    eff.Parameters["Light1Color"].SetValue(new Vector3(0.12f, 0.13f, 0.15f));
                    part.Effect = eff;
                }

            var coralRng = new Random(123);
            var coralPositions = new List<Vector2>();
            for (int i = 0; i < 150; i++)
            {
                float px = 0, pz = 0;
                bool placed = false;
                for (int attempt = 0; attempt < 300; attempt++)
                {
                    float angle = (float)(coralRng.NextDouble() * MathHelper.TwoPi);
                    float r = (float)(Math.Sqrt(coralRng.NextDouble()) * 250f);
                    px = r * MathF.Cos(angle);
                    pz = r * MathF.Sin(angle);
                    bool overlap = false;
                    foreach (var cp in coralPositions)
                        if ((new Vector2(cp.X - px, cp.Y - pz)).Length() < 12f)
                        { overlap = true; break; }
                    if (!overlap) { placed = true; break; }
                }
                if (!placed) continue;

                coralPositions.Add(new Vector2(px, pz));
                float py = seabed.GetHeightAt(px, pz, seabedHeightTransform);
                float scale = 1.0f + (float)coralRng.NextDouble() * 4.0f;
                float rotY = (float)(coralRng.NextDouble() * MathHelper.TwoPi);
                coralTransforms.Add(
                    Matrix.CreateScale(scale)
                    * Matrix.CreateRotationY(rotY)
                    * Matrix.CreateTranslation(px, py, pz));
                float rxz = scale * 1.2f;
                float ry  = scale * 2.5f * 0.7f;
                var offsetWorld = Vector3.TransformNormal(new Vector3(-rxz * 0.4f, 0, rxz * 0.3f), Matrix.CreateRotationY(rotY));
                coralColliders.Add((new Vector3(px, py + ry, pz) + offsetWorld, rxz, ry));
            }
            
            coralCollisionEllipsoid = BasicGeometry.CreateSphere(GraphicsDevice);
            coralCollisionEllipsoid.Effect.DiffuseColor = Color.Orange.ToVector3();
            coralCollisionEllipsoid.Effect.Alpha = 0.5f;

            tableCoralModel = Content.Load<Model>("table-coral/source/jeweled disk");
            var tcColorTex = new Texture2D[]
            {
                Content.Load<Texture2D>("table-coral/textures/Coral_reef_jeweled_disk_01_blue"),
                Content.Load<Texture2D>("table-coral/textures/Coral_reef_jeweled_disk_01_green"),
                Content.Load<Texture2D>("table-coral/textures/Coral_reef_jeweled_disk_01_purple"),
                Content.Load<Texture2D>("table-coral/textures/Coral_reef_jeweled_disk_01_red"),
            };
            var tcNormalTex  = Content.Load<Texture2D>("table-coral/textures/Coral_reef_jeweled_disk_01_normal");
            var tcSpecTex    = Content.Load<Texture2D>("table-coral/textures/Coral_reef_jeweled_disk_01_spec");
            var tcBlackMetal = new Texture2D(GraphicsDevice, 1, 1);
            tcBlackMetal.SetData(new[] { Color.Black });

            tableCoralPartCount = 0;
            foreach (var mesh in tableCoralModel.Meshes) tableCoralPartCount += mesh.MeshParts.Count;

            var tcBaseEffect = Content.Load<Effect>("FishLit");
            tableCoralEffects = new Effect[4][];
            for (int ci = 0; ci < 4; ci++)
            {
                tableCoralEffects[ci] = new Effect[tableCoralPartCount];
                int pi = 0;
                foreach (var mesh in tableCoralModel.Meshes)
                    foreach (var part in mesh.MeshParts)
                    {
                        var eff = tcBaseEffect.Clone();
                        eff.Parameters["Texture"].SetValue(tcColorTex[ci]);
                        eff.Parameters["NormalTexture"].SetValue(tcNormalTex);
                        eff.Parameters["MetallicTexture"].SetValue(tcBlackMetal);
                        eff.Parameters["RoughnessTexture"].SetValue(tcSpecTex);
                        eff.Parameters["AOTexture"].SetValue(whiteAO);
                        eff.Parameters["HasAO"].SetValue(false);
                        eff.Parameters["AmbientColor"].SetValue(new Vector3(0.30f, 0.30f, 0.32f));
                        eff.Parameters["Light0Dir"].SetValue(Vector3.Normalize(-sky.SunDir));
                        eff.Parameters["Light0Color"].SetValue(new Vector3(0.75f, 0.78f, 0.80f));
                        eff.Parameters["Light1Dir"].SetValue(Vector3.Down);
                        eff.Parameters["Light1Color"].SetValue(new Vector3(0.12f, 0.13f, 0.15f));
                        tableCoralEffects[ci][pi++] = eff;
                    }
            }

            var tcRng       = new Random(456);
            var tcPositions = new List<Vector2>();
            for (int i = 0; i < 120; i++)
            {
                float px = 0, pz = 0;
                bool placed = false;
                for (int attempt = 0; attempt < 300; attempt++)
                {
                    float angle = (float)(tcRng.NextDouble() * MathHelper.TwoPi);
                    float r     = (float)(Math.Sqrt(tcRng.NextDouble()) * 150f);
                    px = r * MathF.Cos(angle);
                    pz = r * MathF.Sin(angle);
                    bool overlap = false;
                    foreach (var cp in tcPositions)
                        if ((new Vector2(cp.X - px, cp.Y - pz)).Length() < 10f)
                        { overlap = true; break; }
                    if (!overlap) { placed = true; break; }
                }
                if (!placed) continue;

                tcPositions.Add(new Vector2(px, pz));
                float py      = seabed.GetHeightAt(px, pz, seabedHeightTransform);
                float scale   = 2.0f + (float)tcRng.NextDouble() * 3.25f;
                float rotY    = (float)(tcRng.NextDouble() * MathHelper.TwoPi);
                int   colorIdx = tcRng.Next(4);
                tableCoralInstances.Add((
                    Matrix.CreateScale(scale)
                    * Matrix.CreateRotationX(-MathHelper.PiOver2)
                    * Matrix.CreateRotationY(rotY)
                    * Matrix.CreateTranslation(px, py, pz),
                    colorIdx));
                var colliderMatrix = Matrix.CreateScale(scale * 0.6f, scale * 0.28f, scale * 0.9f)
                    * Matrix.CreateRotationX(-MathHelper.PiOver2)
                    * Matrix.CreateRotationY(rotY)
                    * Matrix.CreateTranslation(px, py, pz);
                tableCoralColliderMatrices.Add(colliderMatrix);
            }

            // Bucket-ek feltöltése – határt átlógó korallok több oktánsba is bekerülnek
            _coralBuckets = new List<(Vector3, float, float)>[8];
            for (int i = 0; i < 8; i++) _coralBuckets[i] = new();
            foreach (var col in coralColliders)
            {
                for (int oct = 0; oct < 8; oct++)
                {
                    if (((oct & 4) != 0 ? col.center.X + col.rxz > 0 : col.center.X - col.rxz < 0) &&
                        ((oct & 2) != 0 ? col.center.Y + col.ry  > 0 : col.center.Y - col.ry  < 0) &&
                        ((oct & 1) != 0 ? col.center.Z + col.rxz > 0 : col.center.Z - col.rxz < 0))
                        _coralBuckets[oct].Add(col);
                }
            }

            _tableCoralBuckets = new List<Matrix>[8];
            for (int i = 0; i < 8; i++) _tableCoralBuckets[i] = new();
            foreach (var mat in tableCoralColliderMatrices)
            {
                Vector3 pos = mat.Translation;
                float bx = mat.Right.Length();
                float by = mat.Up.Length();
                float bz = mat.Backward.Length();
                for (int oct = 0; oct < 8; oct++)
                {
                    if (((oct & 4) != 0 ? pos.X + bx > 0 : pos.X - bx < 0) &&
                        ((oct & 2) != 0 ? pos.Y + by > 0 : pos.Y - by < 0) &&
                        ((oct & 1) != 0 ? pos.Z + bz > 0 : pos.Z - bz < 0))
                        _tableCoralBuckets[oct].Add(mat);
                }
            }
        }

        static int Octant(Vector3 pos) =>
            (pos.X >= 0 ? 4 : 0) | (pos.Y >= 0 ? 2 : 0) | (pos.Z >= 0 ? 1 : 0);

        static Vector3 RandomOnUnitSphere(Random rng)
        {
            float theta = (float)(rng.NextDouble() * MathHelper.TwoPi);
            float phi   = MathF.Acos((float)(2.0 * rng.NextDouble() - 1.0));
            return new Vector3(
                MathF.Sin(phi) * MathF.Cos(theta),
                MathF.Sin(phi) * MathF.Sin(theta),
                MathF.Cos(phi));
        }

        // Converts yaw (horizontal) and pitch (vertical) angles into a unit direction vector.
        // Convention: yaw=0 looks toward +Z; positive yaw rotates toward +X.
        private static Vector3 DirectionFromAngles(float yaw, float pitch)
        {
            return new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * MathF.Cos(yaw));
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var ks    = Keyboard.GetState();
            var tab   = ks.IsKeyDown(Keys.Tab);
            var shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);
            var w     = ks.IsKeyDown(Keys.W) || ks.IsKeyDown(Keys.Up);
            var s     = ks.IsKeyDown(Keys.S) || ks.IsKeyDown(Keys.Down);
            var a     = ks.IsKeyDown(Keys.A) || ks.IsKeyDown(Keys.Left);
            var d     = ks.IsKeyDown(Keys.D) || ks.IsKeyDown(Keys.Right);
            var up    = ks.IsKeyDown(Keys.Q);
            var down  = ks.IsKeyDown(Keys.E);

            if (tab && tabReleased)
            {
                fishCam = !fishCam;
                tabReleased = false;
                if (!fishCam)
                {
                    // Átváltáskor _yaw/_pitch-et a kamera tényleges nézési irányából számolom,
                    // különben 180°-os ugrás lenne (fish-cam orbit iránya az ellentéte a look iránynak)
                    var dir = Camera.Main.Direction;
                    _yaw   = MathF.Atan2(dir.X, dir.Z);
                    _pitch = MathF.Asin(MathHelper.Clamp(dir.Y, -1f, 1f));
                }
            }
            else if (!tab) tabReleased = true;

            var f1 = ks.IsKeyDown(Keys.F1);
            if (f1 && f1Released) { showDebugVerlets = !showDebugVerlets; f1Released = false; }
            else if (!f1) f1Released = true;

            var f2 = ks.IsKeyDown(Keys.F2);
            if (f2 && f2Released) { showCollisionSphere = !showCollisionSphere; f2Released = false; }
            else if (!f2) f2Released = true;

            var f3 = ks.IsKeyDown(Keys.F3);
            if (f3 && f3Released) { showCoralCollision = !showCoralCollision; f3Released = false; }
            else if (!f3) f3Released = true;

            var ms = Mouse.GetState();

            // --- Zoom (scroll wheel) ---
            int scrollDelta = ms.ScrollWheelValue - _previousScrollValue;
            if (scrollDelta != 0)
            {
                _cameraZoom -= (scrollDelta / 120f) * 1.5f;
                _cameraZoom = MathHelper.Clamp(_cameraZoom, 1.95f, 40f);
            }
            _previousScrollValue = ms.ScrollWheelValue;

            if (fishCam)
            {
                fish.ctrlW = w;
                fish.ctrlA = a;
                fish.ctrlS = s;
                fish.ctrlD = d;
                fish.ctrlQ = up;
                fish.ctrlE = down;

                if (IsActive)
                {
                    int cx = GraphicsDevice.Viewport.Width  / 2;
                    int cy = GraphicsDevice.Viewport.Height / 2;

                    if (ms.LeftButton == ButtonState.Pressed)
                    {
                        // Orbit mode: free rotation around the fish while the button is held
                        if (!_wasLeftButtonPressed)
                        {
                            Mouse.SetPosition(cx, cy);
                            _wasLeftButtonPressed = true;
                        }
                        else
                        {
                            _yaw   -= (ms.X - cx) * Sensitivity;
                            _pitch += (ms.Y - cy) * Sensitivity;
                            _pitch  = MathHelper.Clamp(_pitch, -PitchLimit, PitchLimit);
                            Mouse.SetPosition(cx, cy);
                        }

                        Vector3 offset = DirectionFromAngles(_yaw, _pitch) * _cameraZoom;
                        Camera.Main.Position  = fish.Position + offset;
                        Camera.Main.Direction = Vector3.Normalize(fish.Position - Camera.Main.Position);
                    }
                    else
                    {
                        // Chase mode: smoothly settle back behind the fish
                        _wasLeftButtonPressed = false;

                        Vector3 fishFacing = Vector3.Normalize(fish.Direction);
                        Vector3 fishUp     = Vector3.Normalize(fish.Up);

                        Vector3 targetPosition = fish.Position - fishFacing * _cameraZoom + fishUp * (_cameraZoom * 0.2f);
                        Vector3 targetFocus    = fish.Position + fishFacing * 2f;

                        Camera.Main.Position = Vector3.Lerp(Camera.Main.Position, targetPosition, 0.08f);

                        Vector3 currentFocus = Camera.Main.Position + Camera.Main.Direction;
                        Camera.Main.Direction = Vector3.Normalize(Vector3.Lerp(currentFocus, targetFocus, 0.1f) - Camera.Main.Position);

                        // Keep yaw/pitch in sync (fish -> camera direction) so orbit mode
                        // starts exactly from the current chase position, no jump.
                        var toCam = Vector3.Normalize(Camera.Main.Position - fish.Position);
                        _yaw   = MathF.Atan2(toCam.X, toCam.Z);
                        _pitch = MathF.Asin(MathHelper.Clamp(toCam.Y, -1f, 1f));
                    }
                }
            }
            else
            {
                // --- Mouse look (only while window is focused) ---
                if (IsActive)
                {
                    int cx = GraphicsDevice.Viewport.Width  / 2;
                    int cy = GraphicsDevice.Viewport.Height / 2;
                    _yaw   -= (ms.X - cx) * Sensitivity;
                    _pitch -= (ms.Y - cy) * Sensitivity;
                    _pitch  = MathHelper.Clamp(_pitch, -PitchLimit, PitchLimit);
                    Mouse.SetPosition(cx, cy);
                }

                var lookDir = DirectionFromAngles(_yaw, _pitch);
                Camera.Main.Direction = lookDir;

                var speed = shift ? 1.0f : 0.1f;

                // Horizontal forward vector — no vertical drift when looking up/down
                var flat = new Vector3(lookDir.X, 0, lookDir.Z);
                if (flat.LengthSquared() > 0.0001f) flat = Vector3.Normalize(flat);

                // Right vector derived from yaw only so it stays horizontal
                var right = new Vector3(MathF.Cos(_yaw), 0, -MathF.Sin(_yaw));

                if (w)    Camera.Main.Position += flat  * speed;
                if (s)    Camera.Main.Position -= flat  * speed;
                if (a)    Camera.Main.Position -= right * speed;
                if (d)    Camera.Main.Position += right * speed;
                if (up)   Camera.Main.Position += new Vector3(0, speed, 0);
                if (down) Camera.Main.Position -= new Vector3(0, speed, 0);
            }

            flock.Update(gameTime);

            foreach (var f in fishes)
            {
                f.Step(seabed, seabedHeightTransform);
                f.UpdateAnimation(gameTime);

                int fishOct = Octant(f.Position);
                foreach (var (center, rxz, ry) in _coralBuckets[fishOct])
                {
                    Vector3 delta = f.Position - center;
                    const float fishR = 1.0f;
                    float ex = delta.X / (rxz + fishR);
                    float ey = delta.Y / (ry  + fishR);
                    float ez = delta.Z / (rxz + fishR);
                    float distUnit = MathF.Sqrt(ex * ex + ey * ey + ez * ez);
                    if (distUnit < 1f && distUnit > 0.0001f)
                    {
                        Vector3 grad = Vector3.Normalize(new Vector3(
                            delta.X / (rxz * rxz),
                            delta.Y / (ry  * ry),
                            delta.Z / (rxz * rxz)));
                        float penetration = MathF.Min((1f - distUnit) * MathF.Min(rxz, ry), 0.15f);
                        f.ApplyCollisionPush(grad, penetration);
                    }
                }

                foreach (var collMat in _tableCoralBuckets[fishOct])
                {
                    Matrix collMat2 = collMat;
                    Matrix.Invert(ref collMat2, out Matrix collInv);
                    Vector3 localPos = Vector3.Transform(f.Position, collInv);
                    float minScale = MathF.Min(collMat.Right.Length(), MathF.Min(collMat.Up.Length(), collMat.Backward.Length()));
                    float distUnit = localPos.Length();
                    if (distUnit < 1f && distUnit > 0.0001f)
                    {
                        Vector3 localGrad = Vector3.Normalize(localPos);
                        Vector3 worldGrad = Vector3.Normalize(Vector3.TransformNormal(localGrad, Matrix.Transpose(collInv)));
                        float penetration = MathF.Min((1f - distUnit) * minScale, 0.15f);
                        f.ApplyCollisionPush(worldGrad, penetration);
                    }
                }
            }

            flock.ApplyLeaderExclusion();
            flock.ApplyHardSeparation();

            particles.Update(gameTime, Camera.Main.Position);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Camera.Main.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            var seabedTransforms = new Matrix[] { seabedHeightTransform };

            GraphicsDevice.Clear(Color.Black);

            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            sky.UpdateCaustics(time);
            foreach (var f in fishes)
                f.UpdateCaustics(time);
            seabed.UpdateCaustics(time);

            sky.Draw(Camera.Main);
            foreach (var f in fishes)
                f.Draw(Camera.Main);

            if (showDebugVerlets)
                foreach (var f in fishes)
                    f.DrawDebugVerlets(Camera.Main, debugSphere);

            if (showCollisionSphere)
            {
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                foreach (var f in fishes)
                    collisionSphere.Draw(f.CollisionWorldTransform, Camera.Main.View, Camera.Main.Projection);
                GraphicsDevice.BlendState = BlendState.Opaque;
            }

            if (showCoralCollision)
            {
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                foreach (var (center, rxz, ry) in coralColliders)
                {
                    var ellipsoidWorld = Matrix.CreateScale(rxz, ry, rxz)
                        * Matrix.CreateTranslation(center);
                    coralCollisionEllipsoid.Draw(ellipsoidWorld, Camera.Main.View, Camera.Main.Projection);
                }
                foreach (var collMat in tableCoralColliderMatrices)
                    coralCollisionEllipsoid.Draw(collMat, Camera.Main.View, Camera.Main.Projection);
                GraphicsDevice.BlendState = BlendState.Opaque;
            }

            foreach (var m in seabedTransforms)
                seabed.Draw(m, Camera.Main);

            seabed.DrawSandPlane(Camera.Main);

            foreach (var coralWorld in coralTransforms)
            {
                var worldIT = Matrix.Transpose(Matrix.Invert(coralWorld));
                var waterAmbient = WaterColorSettings.GetColorAtHeight(coralWorld.Translation.Y);
                var ambient = Vector3.Lerp(new Vector3(0.30f, 0.30f, 0.32f), waterAmbient, 0.25f);
                for (int mi = 1; mi < coralModel.Meshes.Count; mi++)
                {
                    var mesh = coralModel.Meshes[mi];
                    foreach (var part in mesh.MeshParts)
                    {
                        var eff = part.Effect;
                        eff.Parameters["World"].SetValue(coralWorld);
                        eff.Parameters["View"].SetValue(Camera.Main.View);
                        eff.Parameters["Projection"].SetValue(Camera.Main.Projection);
                        eff.Parameters["WorldIT"].SetValue(worldIT);
                        eff.Parameters["AmbientColor"]?.SetValue(ambient);
                        CausticsSettings.Apply(eff, time);
                        WaterColorSettings.Apply(eff, Camera.Main.Position);
                    }
                    mesh.Draw();
                }
            }

            foreach (var (tcWorld, colorIdx) in tableCoralInstances)
            {
                int pi = 0;
                foreach (var mesh in tableCoralModel.Meshes)
                    foreach (var part in mesh.MeshParts)
                        part.Effect = tableCoralEffects[colorIdx][pi++];

                var worldIT      = Matrix.Transpose(Matrix.Invert(tcWorld));
                var waterAmbient = WaterColorSettings.GetColorAtHeight(tcWorld.Translation.Y);
                var ambient      = Vector3.Lerp(new Vector3(0.30f, 0.30f, 0.32f), waterAmbient, 0.25f);
                foreach (var mesh in tableCoralModel.Meshes)
                {
                    foreach (var part in mesh.MeshParts)
                    {
                        var eff = part.Effect;
                        eff.Parameters["World"].SetValue(tcWorld);
                        eff.Parameters["View"].SetValue(Camera.Main.View);
                        eff.Parameters["Projection"].SetValue(Camera.Main.Projection);
                        eff.Parameters["WorldIT"].SetValue(worldIT);
                        eff.Parameters["AmbientColor"]?.SetValue(ambient);
                        CausticsSettings.Apply(eff, time);
                        WaterColorSettings.Apply(eff, Camera.Main.Position);
                    }
                    mesh.Draw();
                }
            }

            particles.Draw(Camera.Main);

            base.Draw(gameTime);
        }
    }
}
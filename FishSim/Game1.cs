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
        BasicGeometry debugCylinder;
        bool showDebugVerlets = true;
        bool showDebugConstraints = true;
        bool f1Released = true;
        bool f2Released = true;

        float _yaw;
        float _pitch;
        const float Sensitivity = 0.003f;
        const float PitchLimit  = 1.45f;   // ~83 degrees, avoids gimbal lock at ±90

        // Hal-kamera: orbit nezet (bal egergomb), kozeppontban a hallal
        float orbitYaw;
        float orbitPitch;
        float orbitDistance = 6f;
        const float OrbitMinDistance = 1.5f;
        const float OrbitMaxDistance = 20f;
        const float ZoomSensitivity = 0.0015f;
        int prevScrollValue;
        bool wasOrbiting;
        // Egerrel allitott cel-dontes erzekenysege (hal pitchTarget-jehez)
        const float PitchSteerSensitivity = 0.002f;

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
            fish = new Fish(GraphicsDevice, Content.Load<Model>("fish1"), Content.Load<Texture2D>("relebook-export-ggach148215-001"), Content.Load<Texture2D>("relebook-export-ggach148215-002"), fishEffect, sky.SunDir);
            fishes.Add(fish);
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
            prevScrollValue = Mouse.GetState().ScrollWheelValue;

            debugSphere = BasicGeometry.CreateSphere(GraphicsDevice);
            debugSphere.Effect.DiffuseColor = Color.Red.ToVector3();

            debugCylinder = BasicGeometry.CreateCylinder(GraphicsDevice);
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

            if (tab && tabReleased) { fishCam = !fishCam; tabReleased = false; }
            else if (!tab) tabReleased = true;

            var f1 = ks.IsKeyDown(Keys.F1);
            if (f1 && f1Released) { showDebugVerlets = !showDebugVerlets; f1Released = false; }
            else if (!f1) f1Released = true;

            var f2 = ks.IsKeyDown(Keys.F2);
            if (f2 && f2Released) { showDebugConstraints = !showDebugConstraints; f2Released = false; }
            else if (!f2) f2Released = true;

            var ms = Mouse.GetState();
            bool orbiting = fishCam && ms.LeftButton == ButtonState.Pressed;

            // --- Mouse look (only while window is focused) ---
            if (IsActive)
            {
                int cx = GraphicsDevice.Viewport.Width  / 2;
                int cy = GraphicsDevice.Viewport.Height / 2;
                float dx = ms.X - cx;
                float dy = ms.Y - cy;

                if (orbiting)
                {
                    if (!wasOrbiting)
                    {
                        // Atvetel a kovetokamera iranyabol, hogy ne ugorjon a nezet
                        var fishDir = Vector3.Normalize(fish.Direction);
                        orbitPitch = MathF.Asin(MathHelper.Clamp(fishDir.Y, -1f, 1f));
                        orbitYaw   = MathF.Atan2(fishDir.X, fishDir.Z);
                    }
                    orbitYaw   -= dx * Sensitivity;
                    orbitPitch -= dy * Sensitivity;
                    orbitPitch  = MathHelper.Clamp(orbitPitch, -PitchLimit, PitchLimit);

                    int scrollDelta = ms.ScrollWheelValue - prevScrollValue;
                    orbitDistance -= scrollDelta * ZoomSensitivity;
                    orbitDistance  = MathHelper.Clamp(orbitDistance, OrbitMinDistance, OrbitMaxDistance);
                }
                else if (fishCam)
                {
                    // Eger fel/le: a hal cel-dontese (pitch) - "ragado" ertek, elorehajtassal
                    // ez adja a sullyedest/emelkedest. (dy<0 = eger felfele = hal orra
                    // felfele dol -> emelkedes, ezert + elojel, nem -.)
                    fish.pitchTarget = MathHelper.Clamp(fish.pitchTarget + dy * PitchSteerSensitivity, -1f, 1f);
                }
                else
                {
                    _yaw   -= dx * Sensitivity;
                    _pitch -= dy * Sensitivity;
                    _pitch  = MathHelper.Clamp(_pitch, -PitchLimit, PitchLimit);
                }

                prevScrollValue = ms.ScrollWheelValue;
                Mouse.SetPosition(cx, cy);
            }
            wasOrbiting = orbiting;

            var lookDir = DirectionFromAngles(_yaw, _pitch);

            if (fishCam)
            {
                if (orbiting)
                {
                    // Korbenezes a hal korul, gorgovel zoomolva
                    var orbitDir = DirectionFromAngles(orbitYaw, orbitPitch);
                    Camera.Main.Position  = fish.Position - orbitDir * orbitDistance;
                    Camera.Main.Direction = orbitDir;
                }
                else
                {
                    // Kovetokamera a hal mogott, a hal iranya/dolese szerint
                    var fishDir = Vector3.Normalize(fish.Direction);
                    var fishUp  = Vector3.Normalize(fish.Up);
                    Camera.Main.Position  = fish.Position - fishDir * 6f + fishUp * 2f;
                    Camera.Main.Direction = fishDir;
                }

                fish.ctrlW = w;
                fish.ctrlA = a;
                fish.ctrlS = s;
                fish.ctrlD = d;
            }
            else
            {
                Camera.Main.Direction = lookDir;

                var speed = shift ? 0.1f : 0.01f;

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

            foreach (var f in fishes)
                f.Step();

            particles.Update(gameTime, Camera.Main.Position);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Camera.Main.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            var seabedBedrockLevel = Matrix.CreateTranslation(0, -0.05f, 0);
            var seabedTransforms = new Matrix[]
            {
                seabedBedrockLevel * Matrix.CreateScale(5000, 40f, 5000) * Matrix.CreateTranslation(-2500, -60, -2500)
            };

            GraphicsDevice.Clear(Color.Black);

            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            sky.UpdateCaustics(time);
            foreach (var f in fishes)
                f.UpdateCaustics(time);
            seabed.UpdateCaustics(time);

            sky.Draw(Camera.Main);
            foreach (var f in fishes)
                f.Draw(Camera.Main);

            // TEMP: a kontrollpontok altal hajtott vertex-morph (es az ezt mutato debug-nezetek)
            // kikommentezve - a mesh most rigid (bind-pose), igy csak a hal teljes
            // mozgasat/iranyitasat lehet figyelni a fizikai "jitter" nelkul.
            // if (showDebugVerlets)
            //     foreach (var f in fishes)
            //         f.DrawDebugVerlets(Camera.Main, debugSphere);

            // if (showDebugConstraints)
            //     foreach (var f in fishes)
            //         f.DrawDebugConstraints(Camera.Main, debugCylinder);

            foreach (var m in seabedTransforms)
                seabed.Draw(m, Camera.Main);

            seabed.DrawSandPlane(Camera.Main);

            particles.Draw(Camera.Main);

            base.Draw(gameTime);
        }
    }
}
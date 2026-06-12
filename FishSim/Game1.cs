using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;


namespace FishSim
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        Sky sky;
        Fish fish;
        List<Fish> fishes = new List<Fish>();
        Seabed seabed;
        RenderTarget2D heightMap, waterRefraction, waterReflection;
        Water water;

        public bool fishCam = true;
        bool tabReleased = true;

        float _yaw;
        float _pitch;
        const float Sensitivity = 0.003f;
        const float PitchLimit  = 1.45f;   // ~83 degrees, avoids gimbal lock at ±90

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
            waterReflection = new RenderTarget2D(GraphicsDevice,
                GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth16); Window.ClientSizeChanged += (s, e) => {
                    _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                    _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                    _graphics.ApplyChanges();
                    waterReflection?.Dispose();
                    waterRefraction?.Dispose();
                    waterReflection = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
                        GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
                    waterRefraction = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
                        GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
                };
            heightMap = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
                        GraphicsDevice.Viewport.Height, false, SurfaceFormat.Single, DepthFormat.Depth16);
            waterRefraction = new RenderTarget2D(GraphicsDevice,
                GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth16);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            sky = new Sky(GraphicsDevice, Content.Load<Texture2D>("fishsky1"));
            fish = new Fish(GraphicsDevice, Content.Load<Model>("fishClown"), sky.SunDir);
            fishes.Add(fish);
            seabed = new Seabed(GraphicsDevice, Content.Load<Texture2D>("seabedColor"),
                Content.Load<Texture2D>("seabedHeight"), sky.SunDir, Content.Load<Effect>("Seabed"));

            var waterEffect = Content.Load<Effect>("Water");
            var normalMap = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            normalMap.SetData(new[] { new Color(128, 128, 255) });
            water = new Water(GraphicsDevice, normalMap, waterEffect);
            // Initialise look angles to match the fish's starting heading
            var flatDir = Vector3.Normalize(new Vector3(fish.Direction.X, 0, fish.Direction.Z));
            _yaw   = MathF.Atan2(flatDir.X, flatDir.Z);
            _pitch = -0.15f;

            // Pre-centre mouse so the first frame delta is zero
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
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

            // --- Mouse look (only while window is focused) ---
            if (IsActive)
            {
                int cx = GraphicsDevice.Viewport.Width  / 2;
                int cy = GraphicsDevice.Viewport.Height / 2;
                var ms = Mouse.GetState();
                _yaw   -= (ms.X - cx) * Sensitivity;
                _pitch -= (ms.Y - cy) * Sensitivity;
                _pitch  = MathHelper.Clamp(_pitch, -PitchLimit, PitchLimit);
                Mouse.SetPosition(cx, cy);
            }

            var lookDir = DirectionFromAngles(_yaw, _pitch);

            if (fishCam)
            {
                // Position anchored behind the fish's XZ heading; view direction is free
                var fishFacing = Vector3.Normalize(new Vector3(fish.Direction.X, 0, fish.Direction.Z));
                Camera.Main.Position  = fish.Position - fishFacing * 6 + new Vector3(0, 2f, 0);
                Camera.Main.Direction = lookDir;

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
                seabedBedrockLevel * Matrix.CreateScale(250, 2f, 250) * Matrix.CreateTranslation(-125, -3, -125)
            };

            // 1. Height map
            GraphicsDevice.SetRenderTarget(heightMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer,
                new Vector4(-100.0f, 0, 0, 0), 1, 0);
            foreach (var m in seabedTransforms)
                seabed.DrawHeight(m, Camera.Main);

            // 2. Refraction map – amit a vízen ÁT látunk (homok, halak)
            GraphicsDevice.SetRenderTarget(waterRefraction);
            GraphicsDevice.Clear(new Color(0, 20, 40)); // sötétkék alap ha nincs homok
            foreach (var m in seabedTransforms)
                seabed.DrawRefraction(m, Camera.Main);
            foreach (var f in fishes)
                f.Draw(Camera.Main);

            // 3. Reflection map – amit a vízben tükröződik (ég)
            GraphicsDevice.SetRenderTarget(waterReflection);
            GraphicsDevice.Clear(Color.Black);
            var reflCam = Camera.Main.GetReflection(Vector3.Up);
            reflCam.AspectRatio = Camera.Main.AspectRatio;
            sky.Draw(reflCam);

            // 4. Normál render a back bufferre
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            GraphicsDevice.BlendState = BlendState.Opaque;

            sky.Draw(Camera.Main);
            foreach (var f in fishes)
                f.Draw(Camera.Main);
            foreach (var m in seabedTransforms)
                seabed.Draw(m, Camera.Main);

            seabed.DrawSandPlane(Camera.Main);

            // Víz – legutoljára, alpha blend-del
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            water.Draw(Camera.Main, waterReflection, waterRefraction, heightMap, gameTime, sky.SunDir);
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            base.Draw(gameTime);
        }
    }
}
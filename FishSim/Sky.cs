using Game1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim
{
    class Sky
    {
        public Vector3 SunDir = Vector3.Normalize(new Vector3(1, 1, 5));
        BasicGeometry model;
        public Sky(GraphicsDevice dev, Texture2D tex)
        {
            model = BasicGeometry.CreateHalfSphere(dev, 15, 7,
                v => new VertexPositionTexture(v.Position,
                new Vector2(v.TextureCoordinate.X, 1 - v.TextureCoordinate.Y)));
            model.Effect.Texture = tex;
            model.Effect.LightingEnabled = false;
            model.Effect.TextureEnabled = true;
        }
        public void Draw(Camera cam)
        {
            model.Draw(Matrix.CreateScale(1000) *
                Matrix.CreateTranslation(new Vector3(cam.Position.X, 0, cam.Position.Z)),
                cam.View, cam.Projection);
        }
    }
}
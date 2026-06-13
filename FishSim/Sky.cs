using Game1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim
{
    class Sky
    {
        public Vector3 SunDir = Vector3.Normalize(new Vector3(1, 1, 5));
        BasicGeometry model;
        Effect effect;
        public Sky(GraphicsDevice dev, Texture2D tex, Effect skyEffect)
        {
            model = BasicGeometry.CreateHalfSphere(dev, 15, 7,
                v => new VertexPositionTexture(v.Position,
                new Vector2(v.TextureCoordinate.X, 1 - v.TextureCoordinate.Y)));
            effect = skyEffect.Clone();
            effect.Parameters["Texture"].SetValue(tex);
        }
        public void UpdateCaustics(float time)
        {
            CausticsSettings.Apply(effect, time);
        }
        public void Draw(Camera cam)
        {
            var world = Matrix.CreateScale(1000) *
                Matrix.CreateTranslation(new Vector3(cam.Position.X, 0, cam.Position.Z));
            effect.Parameters["World"].SetValue(world);
            effect.Parameters["View"].SetValue(cam.View);
            effect.Parameters["Projection"].SetValue(cam.Projection);
            effect.Parameters["CameraPosition"].SetValue(cam.Position);
            effect.CurrentTechnique.Passes[0].Apply();
            model.DrawWithoutEffect();
        }
    }
}

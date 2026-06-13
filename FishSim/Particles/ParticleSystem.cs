using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim.Particles
{
    class ParticleSystem
    {
        readonly GraphicsDevice device;
        readonly ParticleLayer[] layers;

        public ParticleSystem(GraphicsDevice device, params ParticleLayerSettings[] layerSettings)
        {
            this.device = device;
            layers = new ParticleLayer[layerSettings.Length];
            for (int i = 0; i < layerSettings.Length; i++)
                layers[i] = new ParticleLayer(device, layerSettings[i]);
        }

        public void Update(GameTime gameTime, Vector3 cameraPosition)
        {
            foreach (var layer in layers)
                layer.Update(gameTime, cameraPosition);
        }

        public void Draw(Camera cam)
        {
            var prevBlend = device.BlendState;
            var prevDepth = device.DepthStencilState;

            device.DepthStencilState = DepthStencilState.DepthRead;

            foreach (var layer in layers)
                layer.Draw(cam);

            device.BlendState = prevBlend;
            device.DepthStencilState = prevDepth;
        }
    }
}

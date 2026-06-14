using FishSim;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishSim
{
    class Seabed
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;
        Effect effect;

        VertexBuffer flatPlaneVB;
        IndexBuffer flatPlaneIB;

        public Seabed(GraphicsDevice dev, Texture2D tex, Vector3 sunDir, Effect effect, SeabedGenerationSettings settings = null)
        {
            settings ??= new SeabedGenerationSettings();
            int w = settings.Resolution;
            int h = settings.Resolution;
            var noise = new PerlinNoise(settings.Seed);

            const float TileCount = 100f; // a shaderben használt diffúz tiling
            var texData = new Color[tex.Width * tex.Height];
            tex.GetData(texData);

            var vbData = new VertexPositionNormalTexture[w * h];
            for (int i = 0; i < vbData.Length; i++)
            {
                int x = i % w; int y = i / w;
                float u = x / (float)(w - 1);
                float v = y / (float)(h - 1);
                float n = noise.Fbm(u * settings.Frequency, v * settings.Frequency, settings.Octaves, settings.Persistence, settings.Lacunarity);
                float height = (n + 1f) * 0.5f * settings.HeightScale;

                // A diffúz textúra fényessége alapján kis mértékű magasságeltolás (ugyanazzal a tilinggel, mint a shader).
                float tu = (u * TileCount) % 1f;
                float tv = (v * TileCount) % 1f;
                int tx = (int)(tu * tex.Width) % tex.Width;
                int ty = (int)(tv * tex.Height) % tex.Height;
                var color = texData[ty * tex.Width + tx];
                float luminance = (color.R + color.G + color.B) / (3f * 255f);
                height += (luminance - 0.5f) * settings.ColorHeightInfluence;

                height = MathHelper.Clamp(height, 0f, 1f);
                var pos = new Vector3(u, height, v);
                vbData[i] = new VertexPositionNormalTexture(pos, Vector3.Up, new Vector2(pos.X, pos.Z));
            }
            for (int i = 0; i < vbData.Length; i++)
            {
                int x = i % w;
                int y = i / w;
                if (x != 0 && x != w - 1 && y != 0 && y != h - 1)
                {
                    var xdir = vbData[y * w + x + 1].Position - vbData[y * w + x - 1].Position;
                    var zdir = vbData[(y + 1) * w + x].Position - vbData[(y - 1) * w + x].Position;
                    vbData[i].Normal = Vector3.Normalize(Vector3.Cross(zdir, xdir));
                }
            }
            vertexBuffer = new VertexBuffer(dev, VertexPositionNormalTexture.VertexDeclaration, vbData.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vbData);

            var indices = new int[(h - 1) * (w * 2 + 1)];
            int idx = 0;
            int dir = 1;
            for (int j = 1; j < w; j++)
            {
                int i = dir > 0 ? 0 : w - 1;
                for (; i >= 0 && i < w; i += dir)
                {
                    indices[idx++] = j * w + i;
                    indices[idx++] = (j - 1) * w + i;
                }
                indices[idx++] = (j) * w + i - dir;
                dir = -dir;
            }
            indexBuffer = new IndexBuffer(dev, typeof(int), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);

            this.effect = effect;
            effect.Parameters["DiffuseMap"].SetValue(tex);
            effect.Parameters["SunDir"].SetValue(sunDir);
            effect.Parameters["TileCount"].SetValue(100f);           // 100x ismétlés
            effect.Parameters["BlendWidth"].SetValue(10f / 1024f);  // 10px átmenet 1024px textúrán

            // A konstruktor végére, az effect paraméterek után:
            float ext = 50000f;
            var flatVerts = new VertexPosition[]
            {
                new(new Vector3(-ext, 0f, -ext)),
                new(new Vector3( ext, 0f, -ext)),
                new(new Vector3(-ext, 0f,  ext)),
                new(new Vector3( ext, 0f,  ext)),
            };
            flatPlaneVB = new VertexBuffer(dev, VertexPosition.VertexDeclaration, 4, BufferUsage.WriteOnly);
            flatPlaneVB.SetData(flatVerts);

            flatPlaneIB = new IndexBuffer(dev, typeof(ushort), 4, BufferUsage.WriteOnly);
            flatPlaneIB.SetData(new ushort[] { 0, 1, 2, 3 });

            effect.Parameters["SandColor"].SetValue(new Vector3(123f / 255f, 112f / 255f, 105f / 255f));
        }
        public void UpdateCaustics(float time)
        {
            CausticsSettings.Apply(effect, time);
        }

        public void Draw(Matrix world, Camera cam)
        {
            var dev = vertexBuffer.GraphicsDevice;
            dev.SetVertexBuffer(vertexBuffer);
            dev.Indices = indexBuffer;
            effect.Parameters["WorldViewProj"].SetValue(world * cam.View * cam.Projection);
            effect.Parameters["WorldIT"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
            effect.Parameters["World"].SetValue(world);
            WaterColorSettings.Apply(effect, cam.Position);
            effect.CurrentTechnique.Passes[0].Apply();
            dev.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, indexBuffer.IndexCount - 2);
        }
        public void DrawSandPlane(Camera cam)
        {
            var dev = flatPlaneVB.GraphicsDevice;
            dev.SetVertexBuffer(flatPlaneVB);
            dev.Indices = flatPlaneIB;
            var sandWorld = Matrix.CreateTranslation(0, -60, 0);
            effect.Parameters["WorldViewProj"].SetValue(sandWorld * cam.View * cam.Projection);
            effect.Parameters["World"].SetValue(sandWorld);
            WaterColorSettings.Apply(effect, cam.Position);
            effect.Techniques["SandPlane"].Passes[0].Apply();
            dev.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 2);
        }
    }
}

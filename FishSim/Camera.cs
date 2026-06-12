using Microsoft.Xna.Framework;

namespace FishSim
{
    class Camera
    {
        public Vector3 Position = new(5, 2, 3);
        public Vector3 Direction = new(-5, -2, -3);
        public Vector3 Up = Vector3.Up; public float AspectRatio = 1;
        public Matrix View => Matrix.CreateLookAt(Position, Position + Direction, Up);
        public Matrix Projection => Matrix.CreatePerspectiveFieldOfView(1, AspectRatio, 1, 1200);
        public static readonly Camera Main = new Camera();

        public Camera GetReflection(Vector3 normal)
        {
            return new Camera
            {
                Position = Vector3.Reflect(Position, normal),
                Direction = Vector3.Reflect(Direction, normal),
                Up = Up,
                AspectRatio = AspectRatio
            };
        }
    }
}
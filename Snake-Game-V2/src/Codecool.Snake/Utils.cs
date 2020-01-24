using System;
using Perlin.Geom;

namespace Snake_Game_V2
{
    public static class Utils
    {
        /// <summary>
        /// Converts a direction in degrees (0...360) to x and y coordinates.
        /// The length of this vector is the second parameter
        /// </summary>
        public static Point DirectionToVector(float directionInDegrees, float length)
        {
            float directionInRadians = directionInDegrees / 180 * (float)Math.PI;
            Point heading = new Point(length * (float)Math.Sin(directionInRadians), -length * (float)Math.Cos(directionInRadians));
            return heading;
        }
    }
}
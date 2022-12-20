using UnityEngine;

namespace DistantLands.Cozy {
    public class CozyUtilities
    {


        public Color DoubleGradient(Gradient start, Gradient target, float depth, float time)
        {
            return Color.Lerp(start.Evaluate(time), target.Evaluate(time), depth);
        }

    }
}
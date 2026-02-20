namespace Code.Services.Random
{
    /// <summary>
    /// Production implementation of IRandomProvider that wraps Unity's Random API.
    /// This is the default implementation used in runtime.
    /// </summary>
    public class UnityRandomProvider : IRandomProvider
    {
        /// <inheritdoc />
        public int Range(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        /// <inheritdoc />
        public float Range(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }
    }
}

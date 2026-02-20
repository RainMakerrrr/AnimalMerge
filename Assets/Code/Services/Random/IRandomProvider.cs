namespace Code.Services.Random
{
    /// <summary>
    /// Service interface for random number generation.
    /// Provides testable abstraction over Unity's Random API.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a random integer number between min (inclusive) and max (exclusive).
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns>A random integer between min and max.</returns>
        int Range(int min, int max);

        /// <summary>
        /// Returns a random float number between min (inclusive) and max (inclusive).
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        /// <returns>A random float between min and max.</returns>
        float Range(float min, float max);
    }
}

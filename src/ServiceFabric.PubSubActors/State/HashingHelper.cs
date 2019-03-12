using System.Text;

namespace ServiceFabric.PubSubActors.State
{
    public class HashingHelper : IHashingHelper
    {
        /// <inheritdoc />
        /// <summary>
        /// Returns FNV hash.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public long HashString(string input)
        {
            input = input.ToUpperInvariant();
            var value = Encoding.UTF8.GetBytes(input);
            ulong hash = 14695981039346656037;
            for (int i = 0; i < value.Length; ++i)
            {
                hash ^= value[i];
                hash *= 1099511628211;
            }
            return (long)hash;
        }
    }

    public interface IHashingHelper
    {
        /// <summary>
        /// Uses a hashing algorithm with a good distribution, to generate an int64 from a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        long HashString(string input);
    }
}
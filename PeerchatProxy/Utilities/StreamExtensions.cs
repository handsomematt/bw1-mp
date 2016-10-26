using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PeerchatProxy
{
    internal static class StreamExtensions
    {
        public static async Task<string> ReadLineAsync(this StreamReader sr, CancellationToken cancellationToken)
        {
            return await sr.ReadLineAsync().WithCancellation(cancellationToken);
        }

        public static async Task WriteLineAsync(this StreamWriter sw, string data, CancellationToken cancellationToken)
        {
            await sw.WriteLineAsync(data).WithCancellation(cancellationToken);
        }
    }
}

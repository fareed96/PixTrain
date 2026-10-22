using System;

namespace PixTrain.Core.Utils
{
    /// <summary>Hat geliri gibi gerçek zamana bağlı kurallar için saat soyutlaması (testte sabitlenir).</summary>
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

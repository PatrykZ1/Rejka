namespace backend.Infrastructure.Analysis;

public sealed class VideoDurationLimitExceededException(TimeSpan duration, TimeSpan maximumDuration)
    : Exception($"Video duration {duration} exceeds the maximum allowed duration of {maximumDuration}.");

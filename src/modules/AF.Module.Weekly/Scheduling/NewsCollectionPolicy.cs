using System;

namespace AnimusForge;

internal static class NewsCollectionPolicy
{
    internal static bool IsWeeklyDue(double startHour, double now) => startHour >= 0 && now - startHour >= 168.0;
    internal static bool Includes(int sequence, int startSequence, int endSequence) =>
        startSequence < 0 || (sequence > startSequence && (endSequence < 0 || sequence <= endSequence));
}

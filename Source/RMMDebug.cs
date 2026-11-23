using Verse;

namespace RimworldMilkingMachine
{
    internal static class RMMDebug
    {
        internal const int DEBUG = 0;

        private static bool Enabled => DEBUG > 0;

        internal static void Message(string message)
        {
            if (Enabled)
            {
                Log.Message(message);
            }
        }

        internal static void Warning(string message)
        {
            if (Enabled)
            {
                Log.Warning(message);
            }
        }

        internal static void Error(string message)
        {
            if (Enabled)
            {
                Log.Error(message);
            }
        }
    }
}

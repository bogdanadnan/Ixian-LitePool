using System;
namespace LP.Helpers
{
    public static class Utils
    {
        public static string processLastSeen(DateTime dt)
        {
            TimeSpan ts = DateTime.Now - dt;

            if (ts.TotalHours >= 1)
            {
                return String.Format("{0} hour{1} ago", Math.Floor(ts.TotalHours), Math.Floor(ts.TotalHours) > 1 ? "s" : "");
            }
            if (ts.TotalMinutes >= 1)
            {
                return String.Format("{0} minute{1} ago", Math.Floor(ts.TotalMinutes), Math.Floor(ts.TotalMinutes) > 1 ? "s" : "");
            }
            if (ts.TotalSeconds >= 1)
            {
                return String.Format("{0} second{1} ago", Math.Floor(ts.TotalSeconds), Math.Floor(ts.TotalSeconds) > 1 ? "s" : "");
            }

            return "1 second ago";
        }

        public static string processDateTime(DateTime dt)
        {
            return dt.ToString("G");
        }
    }
}

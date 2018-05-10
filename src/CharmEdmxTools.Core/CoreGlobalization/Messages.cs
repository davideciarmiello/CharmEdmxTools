using System;
using System.Threading;

namespace CharmEdmxTools.Core.CoreGlobalization
{
    public static class Messages
    {
        static Messages()
        {
            SetCurrent(Thread.CurrentThread.CurrentCulture.LCID.ToString());
        }

        public static void SetCurrent(string local)
        {
            if (local.Equals("it", StringComparison.OrdinalIgnoreCase) || local == "1040")
                Current = new MessagesIt();
            else
                Current = new MessagesEn();
        }

        public static IMessages Current { get; private set; }
    }
}

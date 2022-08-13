using Gress;
using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public static class ProgressExtensions
    {
        public static void Report(this IProgress<Percentage> progress, int current, int total)
        {
            if (total == 0)
            {
                progress.Report(Percentage.FromValue(100));
                return;
            }

            progress.Report(new Percentage(current * 100d / total));
        }
    }
}

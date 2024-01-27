namespace Metalhead.Examples.SyncAsyncParallel.Core.Models
{
    public class ProgressReport
    {
        public int PercentageComplete { get; set; } = 0;
        public Download Download { get; set; } = new Download();
    }
}

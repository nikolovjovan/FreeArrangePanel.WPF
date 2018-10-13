namespace FreeArrangePanel.Helpers
{
    /// <summary>
    ///     Used for storing delta limits for selected controls.
    /// </summary>
    internal class DeltaLimit
    {
        public double Left = double.MaxValue;
        public double Right = double.MaxValue;
        public double Top = double.MaxValue;
        public double Bottom = double.MaxValue;
    }
}
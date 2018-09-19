namespace FreeArrangePanel.Helpers
{
    internal class DeltaLimit
    {
        public double Left = double.MaxValue;
        public double Right = double.MaxValue;
        public double Top = double.MaxValue;
        public double Bottom = double.MaxValue;

        public override string ToString()
        {
            return "Left: " + Left + " Top: " + Top + " Right: " + Right + " Bottom: " + Bottom;
        }
    }
}
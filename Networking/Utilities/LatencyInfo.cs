namespace Networking.Utilities
{
    public class LatencyInfo
    {
        public double Last { get; private set; }

        public double Minimum { get; private set; }
        public double Maximum { get; private set; }
        public double Average { get; private set; }

        public bool Pinged { get; private set; }

        public void Update(double latest)
        {
            Last = latest;

            if (!Pinged || latest < Minimum)
                Minimum = latest;

            if (!Pinged || latest > Maximum)
                Maximum = latest;

            Average = (Minimum + Maximum + Last) / 3;

            Pinged = true;
        }
    }
}
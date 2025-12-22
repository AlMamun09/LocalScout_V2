namespace LocalScout.Application.Utilities
{
    public static class DistanceCalculator
    {
        private const double EarthRadiusKm = 6371.0;

        // Calculates the distance between two geographical points using the Haversine formula
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        // Calculates the distance between two geographical points.
        // Returns null if any coordinate is missing.
        public static double? CalculateDistance(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return null;

            return CalculateDistance(lat1.Value, lon1.Value, lat2.Value, lon2.Value);
        }

        // Converts degrees to radians
        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}

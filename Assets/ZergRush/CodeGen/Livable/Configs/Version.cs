namespace ZergRush.Alive
{
    [GenInLocalFolder]
    public struct Version
    {
        public int major, middle, minor;

        public Version(string version)
        {
            string[] versionStrings = version.Split('.');
            major = int.Parse(versionStrings[0]);
            middle = int.Parse(versionStrings[1]);
            minor = int.Parse(versionStrings[2]);
        }

        public static bool operator <  (Version ver1, Version ver2) => Comparison(ver1, ver2) < 0;
        public static bool operator >  (Version ver1, Version ver2) => Comparison(ver1, ver2) > 0;
        public static bool operator == (Version ver1, Version ver2) => Comparison(ver1, ver2) == 0;
        public static bool operator != (Version ver1, Version ver2) => Comparison(ver1, ver2) != 0;
        public static bool operator <= (Version ver1, Version ver2) => Comparison(ver1, ver2) <= 0;
        public static bool operator >= (Version ver1, Version ver2) => Comparison(ver1, ver2) >= 0;
        public override int GetHashCode() => major * 1000000 + middle * 1000 + minor;
        public static int Comparison(Version ver1, Version ver2) => ver1.GetHashCode().CompareTo(ver2.GetHashCode());

        public override bool Equals(object obj)
        {
            if (!(obj is Version)) return false;
            return Comparison(this, (Version) obj) == 0;
        }

        public override string ToString()
        {
            return $"{major}.{middle}.{minor}";
        }
    }
}
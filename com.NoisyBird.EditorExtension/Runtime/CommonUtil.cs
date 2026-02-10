namespace NoisyBird.EditorExtension
{
    public static partial class CommonUtil
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return str == null || str.Equals(string.Empty);
        }
    }
}
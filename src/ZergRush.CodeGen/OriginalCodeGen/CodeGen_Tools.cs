namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string AddOrCondition(string prev, string newCondition)
        {
            return string.IsNullOrEmpty(prev) ? newCondition : prev + " || " + newCondition;
        }
    }
}

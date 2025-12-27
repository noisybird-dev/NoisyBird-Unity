using System;

namespace NoisyBird.Debug
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class NBCheatAttribute : Attribute
    {
        public object Category { get; private set; }
        public int Group { get; private set; }

        public NBCheatAttribute(object category, int group = int.MaxValue)
        {
            Category = category;
            Group = group;
        }
    }
}

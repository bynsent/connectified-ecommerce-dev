using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Unleash.Common
{
    public static class HelperMethod
    {
        public static int GetEnumIntFromString(Type enumType, string enumString)
        {
            if (!Enum.IsDefined(enumType, enumString))
            {
                throw new ArgumentException($"Value '{enumString}' is not defined in enum '{enumType.Name}'.");
            }

            return (int)Enum.Parse(enumType, enumString);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guardian {
    public class Utility {
        // geting names from enum values (used in list box)
        public static IEnumerable<string> GetNames(Type type) {
            if( !type.IsEnum )
                throw new ArgumentException("Type is not enum");

            return (
                from field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                where field.IsLiteral
                select field.Name
            ).ToList<string>();
        }
    }
}

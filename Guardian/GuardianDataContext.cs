using Guardian.Model;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Guardian {
    public class GuardianDataContext : DataContext {
        public GuardianDataContext(string connection) : base(connection) { }

        // Table for items.
        public Table<Item> Items;

        // Table for users.
        public Table<User> Users;
    }
}

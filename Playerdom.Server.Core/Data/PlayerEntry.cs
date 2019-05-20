using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Server.Core.Data
{
    public class PlayerEntry
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; } //SQLite doesn't support ulong
        //[PrimaryKey] //Multiple primary keys not supported in this library
        public Guid Token { get; set; }
    }
}

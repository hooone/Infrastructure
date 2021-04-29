using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demo2
{
    public class Setting
    {
        [DbConnectionString("127.0.0.1", "RFID", "RFID")]
        public string conn { get; set; } = "a";
        string a { get; set; }
    }
}

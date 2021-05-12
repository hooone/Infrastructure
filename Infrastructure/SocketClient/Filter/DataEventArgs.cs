using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public class DataEventArgs : EventArgs
    {
        public byte[] Data { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }
    }
}

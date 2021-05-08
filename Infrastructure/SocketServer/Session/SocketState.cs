using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    static class SocketState
    {
        public const int Normal = 0;//0000 0000
        public const int InClosing = 16;//0001 0000  >= 16
        public const int Closed = 16777216;//256 * 256 * 256; 0x01 0x00 0x00 0x00
        public const int InSending = 1;//0000 0001  > 1
        public const int InReceiving = 2;//0000 0010 > 2
        public const int InSendingReceivingMask = -4;// ~(InSending | InReceiving); 0xf0 0xff 0xff 0xff
    }
}

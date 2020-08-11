using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFT_Order
{
    class Symbol
    {
        public int OrderBookId { get; set; }
        public int OrderPrice{ get; set; }
        public double OrderLot { get; set; }
        public int CustomerNo { get; set; }

    }
}

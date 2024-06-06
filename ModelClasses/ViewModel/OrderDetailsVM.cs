using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelClasses.ViewModel
{
    public class OrderDetailsVM
    {
        public UserOrderHeader orderHeader { get; set; }
        public IEnumerable<OrderDetails> userProductList { get; set; }
    }
}
    
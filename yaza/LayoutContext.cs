using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public class LayoutContext
    {
        public int ip { get; private set; }
        public int? minAddress;
        public int? maxAddress;

        public void SetOrg(int value)
        {
            ip = value;
        }

        public void ReserveBytes(int amount)
        {
            if (!minAddress.HasValue || ip < minAddress.Value)
                minAddress = ip;

            ip += amount;

            if (!maxAddress.HasValue || ip > maxAddress.Value)
                maxAddress = ip;
        }
    }
}

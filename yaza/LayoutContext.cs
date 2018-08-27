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
        public int op { get; private set; }

        public void SetOrg(int value)
        {
            ip = value;
        }

        public void Seek(int value)
        {
            op = value;
        }

        public void ReserveBytes(int amount)
        {
            ip += amount;
            op += amount;
        }
    }
}


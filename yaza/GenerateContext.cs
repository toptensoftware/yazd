using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public class GenerateContext
    {
        public GenerateContext(LayoutContext layoutContext)
        {
            _layoutContext = layoutContext;
        }

        LayoutContext _layoutContext;

        public void SetOrg(int address)
        {
        }

        public void EmitByte(int value)
        {

        }

        public void EmitWord(int value)
        {
        }

        public void EmitBytes(byte[] bytes)
        {
        }


    }
}

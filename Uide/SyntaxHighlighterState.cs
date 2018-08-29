using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uide
{
    enum SyntaxHighlighterState
    {
        Normal,
        LineComment,
        BlockComment,
        SingleQuoteString,
        DoubleQuoteString,
        NumericLiteral,
    }
}

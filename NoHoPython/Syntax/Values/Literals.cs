using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.Syntax.Values
{
    public sealed class IntegerLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }
        
        public long Number { get; private set; }

        public IntegerLiteral(long number, SourceLocation sourceLocation)
        {
            this.Number = number;
            SourceLocation = sourceLocation;
        }
    }

    public sealed partial class DecimalLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public decimal Number { get; private set; }

        public DecimalLiteral(decimal number, SourceLocation sourceLocation)
        {
            this.Number = number;
            SourceLocation = sourceLocation;
        }
    }

    public sealed partial class CharacterLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public char Character { get; private set; }

        public CharacterLiteral(char character, SourceLocation sourceLocation)
        {
            this.Character = character;
            SourceLocation = sourceLocation;
        }
    }

    public sealed partial class ArrayLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly List<IAstValue> Elements;

        public ArrayLiteral(List<IAstValue> elements, SourceLocation sourceLocation)
        {
            Elements = elements;
            SourceLocation = sourceLocation;
        }

        public ArrayLiteral(string stringLiteral, SourceLocation sourceLocation) : this(stringLiteral.ToList().ConvertAll((char c) => (IAstValue)(new CharacterLiteral(c, sourceLocation))), sourceLocation)
        {

        }
    }
}

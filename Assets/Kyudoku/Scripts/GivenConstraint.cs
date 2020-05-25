using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    public sealed class GivenConstraint : Constraint
    {
        public int Location { get; private set; }
        public int Value { get; private set; }

        public GivenConstraint(int location, int value) : base(new[] { location }) { Location = location; Value = value; }

        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            for (var i = 0; i < takens[Location].Length; i++)
                if (i + minValue != Value)
                    takens[Location][i] = true;
            return Enumerable.Empty<Constraint>();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    public abstract class Constraint
    {
        public int[] AffectedCells { get; private set; }

        protected Constraint(IEnumerable<int> affectedCells)
        {
            AffectedCells = affectedCells.ToArray();
        }

        public abstract IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue);
    }
}

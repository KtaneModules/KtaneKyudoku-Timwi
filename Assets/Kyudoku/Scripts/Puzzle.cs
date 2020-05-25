using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    public sealed class Puzzle
    {
        public int Size { get; private set; }
        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }
        public List<Constraint> Constraints { get; private set; }

        public Puzzle(int size, int minValue, int maxValue)
        {
            Size = size;
            MinValue = minValue;
            MaxValue = maxValue;
            Constraints = new List<Constraint>();
        }

        public Puzzle(int size, int minValue, int maxValue, params Constraint[] constraints)
        {
            Size = size;
            MinValue = minValue;
            MaxValue = maxValue;
            Constraints = constraints.ToList();
        }

        public Puzzle(int size, int minValue, int maxValue, IEnumerable<Constraint> constraints)
        {
            Size = size;
            MinValue = minValue;
            MaxValue = maxValue;
            Constraints = constraints.ToList();
        }

        public Puzzle(int size, int minValue, int maxValue, params IEnumerable<Constraint>[] constraints)
        {
            Size = size;
            MinValue = minValue;
            MaxValue = maxValue;
            Constraints = constraints.Where(cs => cs != null).SelectMany(x => x.Where(c => c != null)).ToList();
        }

        private int _numVals;

        public IEnumerable<int[]> Solve()
        {
            _numVals = MaxValue - MinValue + 1;
            var cells = new int?[Size];
            var takens = new bool[Size][];
            for (var i = 0; i < Size; i++)
                takens[i] = new bool[_numVals];

            var newConstraints = new List<Constraint>();
            foreach (var constraint in Constraints)
            {
                var cs = constraint.MarkTakens(takens, cells, null, MinValue, MaxValue);
                if (cs == null)
                    newConstraints.Add(constraint);
                else
                    newConstraints.AddRange(cs);
            }

            var numConstraintsPerCell = new int[Size];
            foreach (var constraint in newConstraints)
                foreach (var cell in constraint.AffectedCells ?? Enumerable.Range(0, Size))
                    numConstraintsPerCell[cell]++;

            return solve(cells, takens, newConstraints, numConstraintsPerCell).Select(solution => solution.Select(val => val + MinValue).ToArray());
        }

        private IEnumerable<int[]> solve(int?[] filledInValues, bool[][] takens, List<Constraint> constraints, int[] numConstraintsPerCell)
        {
            var fewestPossibleValues = int.MaxValue;
            var ix = -1;
            for (var cell = 0; cell < Size; cell++)
            {
                if (filledInValues[cell] != null)
                    continue;
                var count = 0;
                for (var v = 0; v < takens[cell].Length; v++)
                    if (!takens[cell][v])
                        count++;
                if (count == 0)
                    yield break;
                count -= numConstraintsPerCell[cell];
                if (count < fewestPossibleValues)
                {
                    ix = cell;
                    fewestPossibleValues = count;
                }
            }

            if (ix == -1)
            {
                yield return filledInValues.Select(val => val.Value).ToArray();
                yield break;
            }

            for (var tVal = 0; tVal < takens[ix].Length; tVal++)
            {
                var val = tVal % takens[ix].Length;
                if (takens[ix][val])
                    continue;

                filledInValues[ix] = val;
                var takensCopy = takens.Select(arr => arr.ToArray()).ToArray();

                List<Constraint> constraintsCopy = null;

                for (var i = 0; i < constraints.Count; i++)
                {
                    var constraint = constraints[i];
                    var newConstraints = constraint.MarkTakens(takensCopy, filledInValues, ix, MinValue, MaxValue);
                    if (newConstraints != null)
                    {

                        if (constraintsCopy == null)
                            constraintsCopy = new List<Constraint>(constraints.Take(i));
                        var constraintIx = constraintsCopy.Count;
                        constraintsCopy.AddRange(newConstraints);

                        for (var cIx = constraintIx; cIx < constraintsCopy.Count; cIx++)
                        {
                            var yetMoreConstraints = constraintsCopy[cIx].MarkTakens(takensCopy, filledInValues, null, MinValue, MaxValue);
                            if (yetMoreConstraints != null)
                                throw new NotImplementedException(string.Format(
                                    @"While entering a {0} in cell {1}, a {2} returned a {3}. When calling MarkTakens on this new constraint, it returned yet further constraints. This scenario is not currently supported by the algorithm. The constraints returned from MarkTaken() must not themselves return further constraints when MarkTaken() is called on them for values already placed in the grid.",
                                    val + MinValue, ix, constraint.GetType().Name, constraintsCopy[cIx].GetType().Name));
                        }
                    }
                    else if (constraintsCopy != null)
                        constraintsCopy.Add(constraint);
                }

                foreach (var solution in solve(filledInValues, takensCopy, constraintsCopy ?? constraints, numConstraintsPerCell))
                    yield return solution;
            }
            filledInValues[ix] = null;
        }
    }
}

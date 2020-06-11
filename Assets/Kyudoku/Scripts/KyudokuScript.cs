using System.Collections;
using UnityEngine;
using System.Linq;
using Rnd = UnityEngine.Random;
using PuzzleSolvers;
using System.Text.RegularExpressions;

public class KyudokuScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] Grid;
    public KMSelectable Reset;
    public TextMesh[] Digits;
    public GameObject[] Xs;
    public GameObject[] Os;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    readonly Cell[] Cells = new Cell[36];
    PuzzleInfo puzzleInfo;
    bool forcedSolve = false;

    struct PuzzleInfo
    {
        public int Given;
        public int[] NumberGrid;
        public bool[] Solution;
    }

    struct Cell
    {
        public KMSelectable GridPoint;
        public GameObject XObj;
        public GameObject OObj;
        public TextMesh Digit;
        public bool X;
        public bool O;
    }

    KMSelectable.OnInteractHandler GridPress(int gp)
    {
        return delegate
        {
            if (gp == puzzleInfo.Given)
                return false;

            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Grid[gp].transform);

            if (!Cells[gp].X && !Cells[gp].O)
            {
                Cells[gp].X = true;
                Cells[gp].XObj.SetActive(true);
            }
            else if (Cells[gp].X && !Cells[gp].O)
            {
                Cells[gp].X = false;
                Cells[gp].XObj.SetActive(false);
                Cells[gp].O = true;
                Cells[gp].OObj.SetActive(true);
            }
            else if (Cells[gp].O)
            {
                Cells[gp].O = false;
                Cells[gp].OObj.SetActive(false);
            }
            for (int i = 0; i < puzzleInfo.Solution.Length; i++)
                if (Cells[i].O != puzzleInfo.Solution[i])
                    return false;
            moduleSolved = true;
            Module.HandlePass();
            if (!forcedSolve)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!puzzleInfo.Solution[i])
                        Cells[i].XObj.SetActive(true);
                }
            }
            return false;
        };
    }

    void Start()
    {
        moduleId = moduleIdCounter++;

        for (int i = 0; i < Grid.Length; i++)
        {
            Grid[i].OnInteract += GridPress(i);
        }

        for (int i = 0; i < 36; i++)
        {
            Cells[i].GridPoint = Grid[i];
            Cells[i].XObj = Xs[i];
            Cells[i].OObj = Os[i];
            Cells[i].Digit = Digits[i];
            Cells[i].X = false;
            Cells[i].O = false;
        }

        puzzleInfo = GeneratePuzzle();
        for (int row = 0; row < 6; row++)
            Debug.LogFormat(@"[Kyudoku #{0}] {1}", moduleId, Enumerable.Range(0, 6).Select(col => puzzleInfo.Solution[6 * row + col] ? "[" + puzzleInfo.NumberGrid[6 * row + col] + "]" : " # ").Join(""));

        Cells[puzzleInfo.Given].OObj.SetActive(true);
        Cells[puzzleInfo.Given].O = true;

        for (int i = 0; i < puzzleInfo.NumberGrid.Length; i++)
            Cells[i].Digit.text = puzzleInfo.NumberGrid[i].ToString();

        Reset.OnInteract += delegate
        {
            Reset.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Reset.transform);
            if (moduleSolved)
                return false;
            for (int i = 0; i < 36; i++)
            {
                Cells[i].O = false;
                Cells[i].X = false;
                Cells[i].OObj.SetActive(false);
                Cells[i].XObj.SetActive(false);
            }
            Cells[puzzleInfo.Given].O = true;
            Cells[puzzleInfo.Given].OObj.SetActive(true);
            return false;
        };
    }

    PuzzleInfo GeneratePuzzle()
    {
        while (true)
        {
            var numberGrid = new int[36];
            var ixs = Enumerable.Range(0, 36).ToList().Shuffle();
            for (var i = 0; i < 9; i++)
                numberGrid[ixs[i]] = i + 1;
            for (var i = 0; i < 36; i++)
                if (numberGrid[i] == 0)
                    numberGrid[i] = Rnd.Range(1, 10);

            var given = Rnd.Range(0, 36);

            // 0 = circled, 1 = shaded
            var puzzle = new Puzzle(36, 0, 1);
            puzzle.Constraints.Add(new GivenConstraint(given, 0));
            puzzle.Constraints.Add(new Kyudoku6x6Constraint(numberGrid));

            var solutions = puzzle.Solve().Take(2).ToArray();
            if (solutions.Length == 1)
                return new PuzzleInfo { Given = given, NumberGrid = numberGrid, Solution = solutions[0].Select(val => val == 0).ToArray() };
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} A1 C3 D5 [Toggle cells A1, C3 and D5] | !{0} set A1 C3 D5 [Circle cells A1, C3 and D5] | !{0} reset [Resets the module]";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if (moduleSolved)
        {
            yield return "sendtochaterror The module is already solved.";
            yield break;
        }
        else if ((m = Regex.Match(command, @"^\s*([ABCDEF123456 ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            var cell = m.Groups[0].Value.Split(' ');
            for (int i = 0; i < cell.Length; i++)
            {
                if (Regex.IsMatch(cell[i], @"^\s*[ABCDEF][123456]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    continue;
                else
                {
                    yield return "sendtochaterror Incorrect syntax.";
                    yield break;
                }
            }
            for (int i = 0; i < cell.Length; i++)
            {
                var row = int.Parse(cell[i].Substring(1, 1));
                var col = cell[i].ToUpperInvariant()[0] - 'A' + 1;
                Grid[6 * row - 1 + col - 6].OnInteract();
                yield return new WaitForSeconds(.05f);
            }
            yield break;
        }
        else if ((m = Regex.Match(command, @"^\s*set\s*([ABCDEF123456 ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            var cell = m.Groups[1].Value.Split(' ').ToArray();
            for (int i = 0; i < cell.Length; i++)
            {
                if (Regex.IsMatch(cell[i], @"^\s*[ABCDEF][123456]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    continue;
                else
                {
                    yield return "sendtochaterror Incorrect syntax.";
                    yield break;
                }
            }
            for (int i = 0; i < cell.Length; i++)
            {
                var row = int.Parse(cell[i].Substring(1, 1));
                var col = cell[i].ToUpperInvariant()[0] - 'A' + 1;
                if (Cells[6 * row - 1 + col - 6].O)
                    continue;
                else if (Cells[(6 * row - 1 + col) - 6].X)
                    Grid[6 * row + col - 6].OnInteract();
                else
                {
                    Grid[6 * row - 1 + col - 6].OnInteract();
                    yield return new WaitForSeconds(.05f);
                    Grid[6 * row - 1 + col - 6].OnInteract();
                }
                yield return new WaitForSeconds(.05f);

            }
            yield break;
        }
        else if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Reset.OnInteract();
            yield break;
        }
        else
        {
            yield return "sendtochaterror Invalid Command";
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat(@"[Kyudoku #{0}] Module was force solved by TP", moduleId);
        forcedSolve = true;
        for (int i = 0; i < Cells.Length; i++)
        {
            while (Cells[i].X != !puzzleInfo.Solution[i])
            {
                Cells[i].GridPoint.OnInteract();
                yield return new WaitForSeconds(.05f);
            }
            while (Cells[i].O != puzzleInfo.Solution[i])
            {
                Cells[i].GridPoint.OnInteract();
                yield return new WaitForSeconds(.05f);
            }
        }
    }
}
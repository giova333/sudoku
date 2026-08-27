using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Sudoku.Editor.Baking
{
    /// <summary>
    /// The editor front end for the puzzle bake. All the work happens in
    /// <see cref="BankBaker"/>; this only picks counts and paths, shows
    /// progress, and writes the files.
    ///
    /// The same bake is available headlessly via tools/bake.sh, which is what
    /// CI uses - both call identical Core code, so the banks come out the same.
    /// </summary>
    public sealed class PuzzleBankBakeWindow : EditorWindow
    {
        const string OutputDir = "Assets/_Project/Resources/Banks";

        int _mainCount = 2000;
        int _dailyCount = 750;
        Vector2 _scroll;
        string _report = "";

        [MenuItem("Sudoku/Bake Puzzle Banks...")]
        static void Open() =>
            GetWindow<PuzzleBankBakeWindow>(true, "Bake Puzzle Banks").minSize = new Vector2(460, 320);

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Generates the graded puzzle banks. A full bake takes several minutes and " +
                "blocks the editor. Banks are reproducible: the same counts and profile " +
                "always produce byte-identical files.",
                MessageType.Info);

            _mainCount = EditorGUILayout.IntField("Main puzzles / tier", _mainCount);
            _dailyCount = EditorGUILayout.IntField("Daily puzzles / tier", _dailyCount);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_mainCount <= 0 || _dailyCount <= 0))
            {
                if (GUILayout.Button("Bake", GUILayout.Height(30)))
                    Bake();
            }

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void Bake()
        {
            var log = new StringBuilder();
            var watch = Stopwatch.StartNew();
            var fellShort = false;

            try
            {
                Directory.CreateDirectory(OutputDir);
                fellShort |= BakeSet("main", _mainCount, 1_000_000, log);
                fellShort |= BakeSet("daily", _dailyCount, 9_000_000, log);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            watch.Stop();
            log.AppendLine();
            log.AppendLine($"Done in {watch.Elapsed.TotalMinutes:F1} min.");
            if (fellShort)
                log.AppendLine("*** One or more banks fell short. Check the difficulty profile. ***");

            _report = log.ToString();
            AssetDatabase.Refresh();

            if (fellShort) Debug.LogError(_report);
            else Debug.Log(_report);
        }

        bool BakeSet(string setName, int count, int seedBase, StringBuilder log)
        {
            var fellShort = false;

            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
            {
                var cancelled = false;

                var request = new BakeRequest(tier, count, seedBase + (int)tier * 100_000);
                var result = BankBaker.Bake(request, DifficultyProfile.Default, ConstraintSet.Classic,
                    (done, total) =>
                    {
                        if (cancelled) return;
                        if (done % 25 != 0 && done != total) return;

                        cancelled = EditorUtility.DisplayCancelableProgressBar(
                            "Baking puzzle banks", $"{setName} / {tier}: {done} of {total}", done / (float)total);
                    });

                var path = Path.Combine(OutputDir, $"{setName}-{tier}.bytes");
                File.WriteAllBytes(path, PuzzleBankSerializer.Write(tier, result.Puzzles));

                log.AppendLine($"{setName}/{result.Summary}  [{new FileInfo(path).Length / 1024}KB]");
                fellShort |= result.FellShort;

                if (cancelled)
                {
                    log.AppendLine("Cancelled by user - remaining banks were not baked.");
                    return true;
                }
            }

            return fellShort;
        }
    }
}

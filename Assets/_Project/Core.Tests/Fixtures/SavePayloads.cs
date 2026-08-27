namespace Sudoku.Core.Tests.Fixtures
{
    /// <summary>
    /// Save payloads written by hand, never by the code under test. A migration
    /// test is only worth anything if the older payload is a real artefact of
    /// the older format rather than something the current serializer produced,
    /// so these are transcribed and checked by eye.
    /// </summary>
    public static class SavePayloads
    {
        /// <summary>
        /// A schema-1 save: the greybox format, which held a single in-progress
        /// puzzle under "slot" and left played-puzzle counts in engine
        /// preferences, so it carries no "slots" list and no "progress".
        ///
        /// The puzzle is the classic grid. The player has placed a correct 4 at
        /// cell 2 and a wrong 9 at cell 3 - the solution there is 6 - which is
        /// why one heart is gone and one mistake is recorded. Two cells carry
        /// pencil marks: cell 5 holds 1 and 2 (mask 3), cell 8 holds 3 and 5
        /// (mask 20).
        /// </summary>
        public const string SchemaVersionOne = @"{
  ""schemaVersion"": 1,
  ""slot"": {
    ""id"": ""Easy"",
    ""tier"": 0,
    ""date"": """",
    ""bank"": ""main-Easy"",
    ""bankIndex"": 137,
    ""clues"": ""530070000600195000098000060800060003400803001700020006060000280000419005000080079"",
    ""solution"": ""534678912672195348198342567859761423426853791713924856961537284287419635345286179"",
    ""rules"": [3, 1, 3, 1],
    ""values"": ""534970000600195000098000060800060003400803001700020006060000280000419005000080079"",
    ""notes"": [
      0, 0, 0, 0, 0, 3, 0, 0, 20,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0
    ],
    ""elapsed"": 91.5,
    ""hearts"": 2,
    ""hints"": 3,
    ""hintsUsed"": 0,
    ""mistakes"": 1,
    ""status"": 0,
    ""paused"": false,
    ""started"": true,
    ""history"": [
      0, 2, 1, 2, 0, 4, 0, 0,
      0, 3, 1, 3, 0, 9, 0, 0
    ]
  }
}";
    }
}

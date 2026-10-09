using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Editor.Data
{
    public sealed class DataTransformException : Exception
    {
        public DataTransformException(IReadOnlyList<string> errors)
            : base("Static data transform failed:\n" + string.Join("\n", errors))
        {
            Errors = errors;
        }

        public IReadOnlyList<string> Errors { get; }
    }

    public sealed class TransformResult
    {
        public TransformResult(StaticData data, IReadOnlyDictionary<string, string> generatedFiles)
        {
            Data = data;
            GeneratedFiles = generatedFiles;
        }

        public StaticData Data { get; }

        /// <summary>Generated file name -> JSON text, for every definition.</summary>
        public IReadOnlyDictionary<string, string> GeneratedFiles { get; }
    }

    /// <summary>
    /// CSV -> validated definitions -> JSON text. Pure: it reads and writes no files, so the Unity
    /// Editor and Tools/Sim run exactly the same code. Either every definition converts or nothing does.
    /// </summary>
    public static class StaticDataTransformer
    {
        /// <param name="readSource">Returns the CSV text of a source file name, or null when the file is missing.</param>
        public static TransformResult Transform(Func<string, string> readSource)
        {
            if (readSource == null)
            {
                throw new ArgumentNullException(nameof(readSource));
            }

            var errors = new List<string>();
            var parts = new StaticDataParts
            {
                Balance = Map(StaticDataFiles.Balance, readSource, BalanceMapper.Map, errors),
                Jobs = Map(StaticDataFiles.Job, readSource, JobMapper.Map, errors),
                Items = Map(StaticDataFiles.Item, readSource, ItemMapper.Map, errors),
                Bags = Map(StaticDataFiles.Bag, readSource, BagMapper.Map, errors),
                Potions = Map(StaticDataFiles.Potion, readSource, PotionMapper.Map, errors),
                Enemies = Map(StaticDataFiles.Enemy, readSource, EnemyMapper.Map, errors),
                EnemyGroups = Map(StaticDataFiles.EnemyGroup, readSource, EnemyGroupMapper.Map, errors),
                Affinities = Map(StaticDataFiles.Affinity, readSource, AffinityMapper.Map, errors),
                Dungeons = Map(StaticDataFiles.Dungeon, readSource, DungeonMapper.Map, errors),
                Mercenaries = Map(StaticDataFiles.Mercenary, readSource, MercenaryMapper.Map, errors),
                FatigueStates = Map(StaticDataFiles.FatigueState, readSource, FatigueStateMapper.Map, errors),
            };

            // Row errors are reported before data set checks: a broken row would only cause
            // misleading "does not exist" errors further down.
            if (errors.Count > 0)
            {
                throw new DataTransformException(errors);
            }

            StaticData data;
            try
            {
                data = new StaticData(parts);
            }
            catch (DataValidationException exception)
            {
                throw new DataTransformException(exception.Problems);
            }

            return new TransformResult(data, StaticDataLoader.Serialize(data));
        }

        static List<T> Map<T>(
            StaticDataFiles.Entry entry,
            Func<string, string> readSource,
            Func<CsvTable, List<string>, List<T>> map,
            List<string> errors)
        {
            try
            {
                CsvTable table = CsvTable.Parse(readSource(entry.SourceFileName), entry.SourceFileName);
                return map(table, errors);
            }
            catch (DataException exception)
            {
                errors.Add(exception.Message);
                return new List<T>();
            }
        }
    }
}

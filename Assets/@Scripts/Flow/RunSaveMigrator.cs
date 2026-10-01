using System.Collections.Generic;
using F1.Save;

namespace F1.Flow
{
    /// <summary>
    /// Brings an older run save up to the current schema, one version at a time. A newer file than
    /// this build knows is refused.
    /// </summary>
    public static class RunSaveMigrator
    {
        /// <summary>
        /// The oldest schema this build can still read. Version 1 had two rows (front, rear); a run
        /// saved under those rules is not converted to numbered rows.
        /// </summary>
        public const int OldestReadableVersion = 2;

        public static void Migrate(RunSaveData save)
        {
            if (save.SchemaVersion < OldestReadableVersion || save.SchemaVersion > RunSaveData.CurrentSchemaVersion)
            {
                throw new RunSaveException($"Schema version {save.SchemaVersion} cannot be read.");
            }

            // Each step edits the DTO in place and sets SchemaVersion to the next number.
            if (save.SchemaVersion == 2)
            {
                From2To3(save);
            }
        }

        /// <summary>
        /// Version 2 stored a board as one entry per slot, null for an empty one, and had no inventory.
        /// The entries that hold an item become the board in order; the inventory starts empty.
        /// Whether the board still fits its cells, now that items have sizes, is for the validation.
        /// </summary>
        static void From2To3(RunSaveData save)
        {
            if (save.Expedition != null)
            {
                if (save.Expedition.Members != null)
                {
                    foreach (MemberRecord member in save.Expedition.Members)
                    {
                        member?.Items?.RemoveAll(item => item == null);
                    }
                }

                save.Expedition.Inventory = new List<ItemRecord>();
            }

            save.SchemaVersion = 3;
        }
    }
}

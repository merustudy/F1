using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The story's dialogues in the lobby (Slice D stage 55, Docs/Design/33_Black_Lily_Story_Summary.md §12): which one is due, which was
    /// heard last, and the weeks the run has left. They touch no battle, no economy and no random stream; the simulator never asks.
    /// </summary>
    public static class StoryRules
    {
        /// <summary>
        /// The dialogue the lobby should play now, or null: in the dialogues' order, the first not heard in this run whose trigger stands —
        /// the run's start always, a dungeon's unlocking once that dungeon is open. An ended run plays none.
        /// </summary>
        public static DialogueData DueDialogue(StaticData data, RunState state)
        {
            if (state.IsOver)
            {
                return null;
            }

            foreach (DialogueData dialogue in data.DialoguesInOrder)
            {
                if (state.SeenDialogues.Contains(dialogue.Id))
                {
                    continue;
                }

                if (dialogue.Trigger == DialogueTrigger.RunStart
                    || (dialogue.Trigger == DialogueTrigger.DungeonUnlocked && RunRules.IsUnlocked(state, data.Dungeons.Get(dialogue.Dungeon))))
                {
                    return dialogue;
                }
            }

            return null;
        }

        /// <summary>Records a dialogue as heard (once).</summary>
        public static void MarkSeen(StaticData data, RunState state, string dialogueId)
        {
            if (!data.Dialogues.Contains(dialogueId))
            {
                throw new ArgumentException($"Dialogue '{dialogueId}' does not exist.", nameof(dialogueId));
            }

            if (!state.SeenDialogues.Contains(dialogueId))
            {
                state.SeenDialogues.Add(dialogueId);
            }
        }

        /// <summary>The heard dialogue that comes last in the dialogues' order (the one the speaker in the town repeats), or null.</summary>
        public static DialogueData LastHeard(StaticData data, RunState state)
        {
            DialogueData last = null;
            foreach (DialogueData dialogue in data.DialoguesInOrder)
            {
                if (state.SeenDialogues.Contains(dialogue.Id))
                {
                    last = dialogue;
                }
            }

            return last;
        }

        /// <summary>The weeks left to the run's end (the eclipse, Design/33 §7): the last week counts, so the first week has all of them; never below 0.</summary>
        public static int WeeksLeft(StaticData data, RunState state)
        {
            return WeeksLeftOn(data, state.Day);
        }

        /// <summary>The weeks left on a given week (an expedition's return, a settlement's new week); 0 is the day of the eclipse.</summary>
        public static int WeeksLeftOn(StaticData data, int day)
        {
            return Math.Max(0, data.Balance.TotalDays - day + 1);
        }
    }
}

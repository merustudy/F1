using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>How the camera moves over a scene's picture (Slice D stage 55, the Round 98 motion mockup).</summary>
    public enum StoryEffect
    {
        None,

        /// <summary>A small shake that dies away over the first part of the scene (the massacre).</summary>
        Shake,

        /// <summary>The dark at the edges tightens with a heartbeat (the bitten wrist).</summary>
        Heartbeat,
    }

    /// <summary>How a scene gives way to the next.</summary>
    public enum StoryTransition
    {
        /// <summary>The two scenes cross over a second around the cut.</summary>
        Cross,

        /// <summary>The scene sinks into black at its end and the next comes up out of it.</summary>
        Black,
    }

    /// <summary>
    /// Where the camera looks at one end of a scene's motion: the centre of the view in thousandths of the picture's width and height, and the
    /// zoom in percent (100 shows the picture's full width through the 16:9 screen). A view that would leave the picture is pushed back in.
    /// </summary>
    public sealed class StoryCamera
    {
        [JsonConstructor]
        public StoryCamera(int x, int y, int zoom)
        {
            if (x < 0 || x > 1000 || y < 0 || y > 1000)
            {
                throw new DataException($"A story camera's centre ({x}:{y}) must be within 0..1000.");
            }

            if (zoom < 100 || zoom > 400)
            {
                throw new DataException($"A story camera's zoom {zoom} must be within 100..400.");
            }

            X = x;
            Y = y;
            Zoom = zoom;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public int X { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Y { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Zoom { get; }
    }

    /// <summary>
    /// One scene of a told story (Slice D stage 55, Docs/Design/33_Black_Lily_Story_Summary.md §9): the intro's scenes, in Order within their
    /// Sequence. Everything the story says — the picture, the time, the lines, the motion — is here and not in code, so a scene is added,
    /// removed or reworded in the CSV alone. A scene without a picture is the run's first lobby itself: its lines stand over the lobby as a
    /// banner (only the last scene of a sequence may be one). A line in quotation marks (“…”) is someone else's voice; the screen colours it.
    /// </summary>
    public sealed class StorySceneData
    {
        public const string DefinitionName = "StoryScene";

        /// <summary>The lines one screen holds (Design/33 §9).</summary>
        public const int MaxLines = 3;

        [JsonConstructor]
        public StorySceneData(
            string id,
            string sequence,
            int order,
            string image,
            int durationMs,
            int shortDurationMs,
            List<int> shortLines,
            StoryCamera cameraFrom,
            StoryCamera cameraTo,
            StoryEffect effect,
            StoryTransition transition,
            bool countdown,
            List<LocalizedText> lines,
            string soundNote)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Sequence = DataId.Require(sequence, $"{DefinitionName} '{id}' Sequence");
            Image = ArtAddress.Optional(image, $"{DefinitionName} '{id}'", "Image");
            if (durationMs < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': DurationMs must be at least 1.");
            }

            if (shortDurationMs < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': ShortDurationMs cannot be negative (0 leaves the scene out of the short cut).");
            }

            Lines = lines ?? new List<LocalizedText>();
            if (Lines.Count > MaxLines || Lines.Contains(null))
            {
                throw new DataException($"{DefinitionName} '{id}': one to {MaxLines} lines, filled from the first.");
            }

            foreach (LocalizedText line in Lines)
            {
                foreach (string text in line.Values.Values)
                {
                    if (text.IndexOf('<') >= 0 || text.IndexOf('>') >= 0)
                    {
                        throw new DataException($"{DefinitionName} '{id}': a line cannot hold '<' or '>' (the screen draws rich text).");
                    }
                }
            }

            ShortLines = shortLines ?? new List<int>();
            if ((shortDurationMs == 0) != (ShortLines.Count == 0))
            {
                throw new DataException($"{DefinitionName} '{id}': a scene in the short cut keeps at least one line, and one left out keeps none.");
            }

            for (int i = 0; i < ShortLines.Count; i++)
            {
                if (ShortLines[i] < 1 || ShortLines[i] > Lines.Count || (i > 0 && ShortLines[i] <= ShortLines[i - 1]))
                {
                    throw new DataException($"{DefinitionName} '{id}': ShortLines names the scene's lines (1..{Lines.Count}) in order, each once.");
                }
            }

            if (Image != null && (cameraFrom == null || cameraTo == null))
            {
                throw new DataException($"{DefinitionName} '{id}': a scene with a picture needs CameraFrom and CameraTo.");
            }

            if (Image == null && (cameraFrom != null || cameraTo != null || effect != StoryEffect.None))
            {
                throw new DataException($"{DefinitionName} '{id}': a scene without a picture (the lobby's banner) has no camera and no effect.");
            }

            Order = order;
            DurationMs = durationMs;
            ShortDurationMs = shortDurationMs;
            CameraFrom = cameraFrom;
            CameraTo = cameraTo;
            Effect = effect;
            Transition = transition;
            Countdown = countdown;
            SoundNote = soundNote;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        /// <summary>The story the scene belongs to ("intro").</summary>
        [JsonProperty(Order = 2, Required = Required.Always)]
        public string Sequence { get; }

        /// <summary>The scene's place in its sequence (the JSON is in id order).</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Order { get; }

        /// <summary>The picture's logical address ("story/intro/mural"), or null for the lobby's banner.</summary>
        [JsonProperty(Order = 4, Required = Required.AllowNull)]
        public string Image { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int DurationMs { get; }

        /// <summary>The scene's time in the short cut; 0 leaves it out.</summary>
        [JsonProperty(Order = 6, Required = Required.Always)]
        public int ShortDurationMs { get; }

        /// <summary>The lines the short cut keeps, 1 being the first.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public List<int> ShortLines { get; }

        [JsonProperty(Order = 8, Required = Required.AllowNull)]
        public StoryCamera CameraFrom { get; }

        [JsonProperty(Order = 9, Required = Required.AllowNull)]
        public StoryCamera CameraTo { get; }

        [JsonProperty(Order = 10, Required = Required.Always)]
        public StoryEffect Effect { get; }

        [JsonProperty(Order = 11, Required = Required.Always)]
        public StoryTransition Transition { get; }

        /// <summary>The scene shows the weeks left to the end of the run (the screen's text and the run's days).</summary>
        [JsonProperty(Order = 12, Required = Required.Always)]
        public bool Countdown { get; }

        [JsonProperty(Order = 13, Required = Required.Always)]
        public List<LocalizedText> Lines { get; }

        /// <summary>A note for the sound round (not translated, not read by the game).</summary>
        [JsonProperty(Order = 14, Required = Required.AllowNull)]
        public string SoundNote { get; }

        /// <summary>The scene stands as a banner over the lobby rather than as a picture.</summary>
        [JsonIgnore]
        public bool IsLobbyBanner => Image == null;

        /// <summary>The scene's time in a cut.</summary>
        public int DurationOf(bool shortCut)
        {
            return shortCut ? ShortDurationMs : DurationMs;
        }

        /// <summary>The lines a cut shows, in order.</summary>
        public List<LocalizedText> LinesOf(bool shortCut)
        {
            if (!shortCut)
            {
                return new List<LocalizedText>(Lines);
            }

            var kept = new List<LocalizedText>();
            foreach (int number in ShortLines)
            {
                kept.Add(Lines[number - 1]);
            }

            return kept;
        }
    }

    /// <summary>One face of a speaker: the expression's id and the dialogue picture's address.</summary>
    public sealed class NpcPortrait
    {
        [JsonConstructor]
        public NpcPortrait(string expression, string image)
        {
            Expression = DataId.Require(expression, "NpcPortrait Expression");
            Image = ArtAddress.Optional(image, $"NpcPortrait '{expression}'", "Image") ?? throw new DataException($"NpcPortrait '{expression}': Image is missing.");
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Expression { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public string Image { get; }
    }

    /// <summary>
    /// Someone of the story who is not a mercenary (Slice D stage 55): Linette. The figure that stands in the town, and a dialogue picture per
    /// expression. Expressions are ids, not an enum: one is added with a picture and a cell here. The first is the usual face.
    /// </summary>
    public sealed class NpcData
    {
        public const string DefinitionName = "Npc";

        [JsonConstructor]
        public NpcData(string id, LocalizedText name, string figure, List<NpcPortrait> portraits)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            Figure = ArtAddress.Optional(figure, $"{DefinitionName} '{id}'", "Figure");
            Portraits = portraits ?? new List<NpcPortrait>();
            if (Portraits.Count == 0 || Portraits.Contains(null))
            {
                throw new DataException($"{DefinitionName} '{id}': at least one portrait (expression:address).");
            }

            var seen = new HashSet<string>();
            foreach (NpcPortrait portrait in Portraits)
            {
                if (!seen.Add(portrait.Expression))
                {
                    throw new DataException($"{DefinitionName} '{id}': expression '{portrait.Expression}' is listed twice.");
                }
            }
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        /// <summary>The figure that stands in the town ("unit/npc/linette"), or null.</summary>
        [JsonProperty(Order = 3, Required = Required.AllowNull)]
        public string Figure { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public List<NpcPortrait> Portraits { get; }

        /// <summary>The picture of an expression, or null when the speaker has none by that name.</summary>
        public string PortraitOf(string expression)
        {
            foreach (NpcPortrait portrait in Portraits)
            {
                if (portrait.Expression == expression)
                {
                    return portrait.Image;
                }
            }

            return null;
        }
    }

    /// <summary>When a dialogue comes, in the lobby (Slice D stage 55).</summary>
    public enum DialogueTrigger
    {
        /// <summary>The run's first lobby, after the intro.</summary>
        RunStart,

        /// <summary>The first lobby after the dungeon of the row is unlocked.</summary>
        DungeonUnlocked,
    }

    /// <summary>A dialogue of the story (Slice D stage 55, Design/33 §12): when it comes and its place among the others. Its lines are DialogueLineData.</summary>
    public sealed class DialogueData
    {
        public const string DefinitionName = "Dialogue";

        [JsonConstructor]
        public DialogueData(string id, int order, DialogueTrigger trigger, string dungeon)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            if (trigger == DialogueTrigger.DungeonUnlocked)
            {
                Dungeon = DataId.Require(dungeon, $"{DefinitionName} '{id}' Dungeon");
            }
            else if (dungeon != null)
            {
                throw new DataException($"{DefinitionName} '{id}': only a DungeonUnlocked dialogue names a Dungeon.");
            }

            Order = order;
            Trigger = trigger;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        /// <summary>The dialogues' order: the first due one comes first, and the last heard one is the one Linette repeats.</summary>
        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Order { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public DialogueTrigger Trigger { get; }

        /// <summary>The dungeon whose unlocking brings the dialogue; null for another trigger.</summary>
        [JsonProperty(Order = 4, Required = Required.AllowNull)]
        public string Dungeon { get; }
    }

    /// <summary>One line of a dialogue (Slice D stage 55): who says it, with which face, in Order within the dialogue.</summary>
    public sealed class DialogueLineData
    {
        public const string DefinitionName = "DialogueLine";

        [JsonConstructor]
        public DialogueLineData(string id, string dialogue, int order, string speaker, string expression, LocalizedText text)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Dialogue = DataId.Require(dialogue, $"{DefinitionName} '{id}' Dialogue");
            Speaker = DataId.Require(speaker, $"{DefinitionName} '{id}' Speaker");
            Expression = DataId.Require(expression, $"{DefinitionName} '{id}' Expression");
            Text = text ?? throw new DataException($"{DefinitionName} '{id}': Text is missing.");
            Order = order;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public string Dialogue { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Order { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public string Speaker { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public string Expression { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public LocalizedText Text { get; }
    }
}

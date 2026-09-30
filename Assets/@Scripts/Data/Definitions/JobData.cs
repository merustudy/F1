namespace F1.Data
{
    /// <summary>A mercenary job. Immutable; the constructor enforces its own rules.</summary>
    public sealed class JobData
    {
        public const string DefinitionName = "Job";

        public JobData(string id, LocalizedText name)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
        }

        public string Id { get; }
        public LocalizedText Name { get; }
    }
}

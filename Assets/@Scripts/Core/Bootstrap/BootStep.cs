namespace F1.Core
{
    /// <summary>
    /// One step of the boot sequence. The error code is stable: it is what the boot screen shows
    /// when the step fails, so it must not change when the step is renamed.
    /// </summary>
    public readonly struct BootStep
    {
        public static readonly BootStep CreateManagers = new BootStep("Create managers", "BOOT-01");
        public static readonly BootStep ConfigureManagers = new BootStep("Configure managers", "BOOT-02");
        public static readonly BootStep LoadMainScene = new BootStep("Load main scene", "BOOT-10");
        public static readonly BootStep BindMainScene = new BootStep("Bind main scene", "BOOT-11");

        public BootStep(string name, string errorCode)
        {
            Name = name;
            ErrorCode = errorCode;
        }

        public string Name { get; }
        public string ErrorCode { get; }
    }
}

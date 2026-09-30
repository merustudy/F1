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
        public static readonly BootStep InitializeSaveStorage = new BootStep("Initialize save storage", "BOOT-03");
        public static readonly BootStep LoadSettings = new BootStep("Load settings", "BOOT-04");
        public static readonly BootStep InitializeResources = new BootStep("Initialize resources", "BOOT-05");
        public static readonly BootStep LoadStaticData = new BootStep("Load static data", "BOOT-06");
        public static readonly BootStep InitializeLocalization = new BootStep("Initialize localization", "BOOT-07");
        public static readonly BootStep ApplyLocale = new BootStep("Apply locale", "BOOT-08");
        public static readonly BootStep LoadRun = new BootStep("Load run", "BOOT-09");
        public static readonly BootStep LoadMainScene = new BootStep("Load main scene", "BOOT-10");
        public static readonly BootStep BindMainScene = new BootStep("Bind main scene", "BOOT-11");
        public static readonly BootStep ShowMainUi = new BootStep("Show main UI", "BOOT-12");

        public BootStep(string name, string errorCode)
        {
            Name = name;
            ErrorCode = errorCode;
        }

        public string Name { get; }
        public string ErrorCode { get; }
    }
}

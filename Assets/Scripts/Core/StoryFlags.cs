namespace Boruhan.Core
{
    /// <summary>
    /// Story flag names used by the prologue. Flags are plain strings stored in the save, so these
    /// are only here for reference and for code; in the Inspector you type the same strings.
    /// </summary>
    public static class StoryFlags
    {
        public const string YurtIntroSeen = "otag_giris_goruldu";
        public const string TalismanTaken = "tilsim_alindi";
        public const string TalismanGlowSeen = "tilsim_parladi";
        public const string StairwayAppeared = "merdiven_belirdi";
        public const string SkyArrivalSeen = "gok_varis_goruldu";
        public const string KutSpark = "kut_kivilcimi";
        public const string ReturnedToEarth = "yeryuzune_dondu";
        public const string RiftOpened = "yarik_acildi";
    }
}

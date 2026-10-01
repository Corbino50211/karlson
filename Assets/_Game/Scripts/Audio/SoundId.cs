namespace Momentum.Audio
{
    /// <summary>Every sound cue in the game. Clips can be assigned per cue in the AudioLibrary; missing
    /// cues are synthesized procedurally by ProceduralAudio so the game always has audio.</summary>
    public enum SoundId
    {
        None = 0,

        // UI
        UIClick = 1,
        UIHover = 2,
        UIBack = 3,

        // Movement
        Jump = 10,
        Land = 11,
        Footstep = 12,
        SlideStart = 13,
        WallJump = 14,
        WallRunStep = 15,
        Vault = 16,
        GrappleFire = 17,
        GrappleAttach = 18,
        GrappleRelease = 19,
        LaunchPad = 20,
        SpeedPad = 21,

        // Weapons
        Pistol = 30,
        SMG = 31,
        Shotgun = 32,
        Rifle = 33,
        Revolver = 34,
        RocketLaunch = 35,
        GrenadeLaunch = 36,
        Railgun = 37,
        Reload = 38,
        DryFire = 39,
        WeaponSwitch = 40,

        // Pickups
        WeaponPickup = 50,
        AmmoPickup = 51,
        HealthPickup = 52,

        // Combat feedback
        HitMarker = 60,
        KillConfirm = 61,
        Explosion = 62,
        BulletImpact = 63,
        PlayerHurt = 64,
        PlayerDeath = 65,

        // Enemies
        EnemyShot = 70,
        EnemyAlert = 71,
        EnemyDeath = 72,
        LaserCharge = 73,
        LaserFire = 74,
        ChargerRoar = 75,

        // Bosses
        BossRoar = 80,
        BossSlam = 81,
        Shockwave = 82,
        BossDeath = 83,
        ShieldDown = 84,

        // World
        Checkpoint = 90,
        Finish = 91,
        GlassBreak = 92,
        CrateBreak = 93,
        DoorOpen = 94,
        Secret = 95
    }

    public enum MusicTrack
    {
        None = 0,
        Menu = 1,
        Level = 2,
        Boss = 3,
        Editor = 4,
        Intense = 5
    }
}

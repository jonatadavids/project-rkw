using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RKW.Physics.EditMode.Tests")]
// Round 36: KartPhysicsPrototypeBootstrap.OnTrackSelected needs to be
// callable directly from KartPhysicsPrototypeTests (PlayMode) now that
// track/kart creation moved out of Awake() into a menu-driven
// BeginRace() (see TrackSelectMenu) -- a real button click cannot be
// simulated in a headless PlayMode test, so the test needs internal
// access to trigger the same path a player tap would.
[assembly: InternalsVisibleTo("RKW.PlayMode.Tests")]

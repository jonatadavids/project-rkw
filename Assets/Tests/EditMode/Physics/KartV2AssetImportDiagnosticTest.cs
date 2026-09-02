using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// RECOVERY etapa, rodada seguinte (2026-09-01) -- KartV2's model still
    /// falls back to the generic primitive box in PlayMode
    /// (KartV2CosmeticVisualWiringTest logs "2 transforms total") even after
    /// the founder did a full Library-folder wipe and clean reimport. That
    /// rules out a stale-cache explanation, so this test asks Unity's own
    /// AssetDatabase directly -- something only possible in an EditMode test,
    /// since PlayMode tests cannot use UnityEditor APIs -- exactly what it
    /// has registered for KartV2.obj's path, compared side by side against
    /// RacingKart.obj (the "13 HP" model, confirmed working). This tells us
    /// whether the import itself is silently failing/incomplete at the
    /// AssetDatabase level (before Resources.Load ever gets involved), or
    /// whether the import is fine and something else is going wrong only in
    /// PlayMode/Resources.Load specifically.
    /// </summary>
    public sealed class KartV2AssetImportDiagnosticTest
    {
        private const string RacingKartPath = "Assets/RKW/Physics/Resources/KartPhysics/Models/RacingKart.obj";
        private const string KartV2Path = "Assets/RKW/Physics/Resources/KartPhysics/Models/KartV2.obj";

        [Test]
        public void KartV2Obj_AssetDatabaseContents_ComparedToKnownWorkingRacingKart()
        {
            LogAllSubAssets("RacingKart (kart que funciona)", RacingKartPath);
            LogAllSubAssets("KartV2 (kart com problema)", KartV2Path);

            var racingKartMain = AssetDatabase.LoadMainAssetAtPath(RacingKartPath) as GameObject;
            var kartV2Main = AssetDatabase.LoadMainAssetAtPath(KartV2Path) as GameObject;

            var racingKartResourcesLoad = Resources.Load<GameObject>("KartPhysics/Models/RacingKart");
            var kartV2ResourcesLoad = Resources.Load<GameObject>("KartPhysics/Models/KartV2");

            Debug.Log($"[DIAG4] AssetDatabase.LoadMainAssetAtPath: RacingKart={(racingKartMain != null ? racingKartMain.name : "NULL")}, " +
                      $"KartV2={(kartV2Main != null ? kartV2Main.name : "NULL")}");
            Debug.Log($"[DIAG4] Resources.Load<GameObject>: RacingKart={(racingKartResourcesLoad != null ? racingKartResourcesLoad.name : "NULL")}, " +
                      $"KartV2={(kartV2ResourcesLoad != null ? kartV2ResourcesLoad.name : "NULL")}");

            var importer = AssetImporter.GetAtPath(KartV2Path);
            Debug.Log($"[DIAG4] AssetImporter.GetAtPath(KartV2): {(importer != null ? importer.GetType().Name : "NULL")}, " +
                      $"importSettingsMissing={(importer != null ? importer.importSettingsMissing.ToString() : "n/a")}");

            var kartV2Guid = AssetDatabase.AssetPathToGUID(KartV2Path);
            var kartV2PathFromGuid = AssetDatabase.GUIDToAssetPath(kartV2Guid);
            Debug.Log($"[DIAG4] KartV2 GUID roundtrip: path->guid='{kartV2Guid}', guid->path='{kartV2PathFromGuid}' " +
                      $"(should equal '{KartV2Path}')");

            // Sanity check first: if RacingKart itself somehow fails this
            // check too, the problem is broader than KartV2 specifically and
            // the assertions below about KartV2 would be misleading.
            Assert.That(racingKartMain, Is.Not.Null,
                "Sanity check failed: RacingKart.obj's main asset is not a GameObject either -- " +
                "if this fails too, the problem is with the test/environment, not specific to KartV2.");

            Assert.That(kartV2Main, Is.Not.Null,
                "AssetDatabase itself has NO GameObject registered as the main asset for KartV2.obj -- " +
                "the import is failing/incomplete at the AssetDatabase level, not just at Resources.Load. " +
                "Check the [DIAG4] log lines above for exactly what sub-assets (if any) DO exist for this path.");

            Assert.That(kartV2ResourcesLoad, Is.Not.Null,
                "AssetDatabase has a main GameObject for KartV2.obj, but Resources.Load<GameObject> still " +
                "returns null -- this would point at a Resources-folder-specific issue rather than the import itself.");
        }

        private static void LogAllSubAssets(string label, string assetPath)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Debug.Log($"[DIAG4] {label} ({assetPath}): {all.Length} sub-asset(s) found via AssetDatabase.LoadAllAssetsAtPath.");
            foreach (var obj in all)
            {
                if (obj == null)
                {
                    Debug.Log("[DIAG4]   - <null entry>");
                    continue;
                }
                Debug.Log($"[DIAG4]   - type={obj.GetType().Name}, name='{obj.name}'");
            }
        }
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RKW.Editor
{
    /// <summary>
    /// Editor script that assembles a simple kart track from Kenney Racing Kit pieces.
    /// Run via menu: RKW > Build Track MVP
    /// </summary>
    public static class TrackBuilder
    {
        private const string RacingKitPath = "Assets/Art/Kenney/RacingKit/";
        private const float TileScale = 4f; // Each tile becomes 4m x 4m
        private const float TileSize = 1f * TileScale; // World size per tile

        [MenuItem("RKW/Build Track MVP")]
        public static void BuildTrackMVP()
        {
            var trackRoot = new GameObject("Track_MVP");

            // Circuit layout: a simple but interesting kartódromo
            // Using tile-based placement with rotation
            // Pieces: S = Straight, CL = Corner Large (90° turn), CS = Corner Small
            // Layout forms a closed circuit ~300m

            var cursor = Vector3.zero;
            var heading = 0f; // degrees, 0 = +Z

            // Start/finish straight (5 tiles)
            PlaceTiles(trackRoot, "roadStartPositions", ref cursor, ref heading, 1);
            PlaceTiles(trackRoot, "roadStraightLong", ref cursor, ref heading, 4);

            // Turn 1 - large right
            PlaceCorner(trackRoot, "roadCornerLarge", ref cursor, ref heading, false);

            // Short straight
            PlaceTiles(trackRoot, "roadStraightLong", ref cursor, ref heading, 2);

            // Turn 2 - large right
            PlaceCorner(trackRoot, "roadCornerLarge", ref cursor, ref heading, false);

            // Back straight (5 tiles)
            PlaceTiles(trackRoot, "roadStraightLong", ref cursor, ref heading, 3);
            PlaceTiles(trackRoot, "roadStraightLongBump", ref cursor, ref heading, 1); // zebra!
            PlaceTiles(trackRoot, "roadStraightLong", ref cursor, ref heading, 1);

            // Turn 3 - large right
            PlaceCorner(trackRoot, "roadCornerLarge", ref cursor, ref heading, false);

            // Short straight with pit entry
            PlaceTiles(trackRoot, "roadStraightLong", ref cursor, ref heading, 2);

            // Turn 4 - large right (back to start)
            PlaceCorner(trackRoot, "roadCornerLarge", ref cursor, ref heading, false);

            // Final straight back to start
            PlaceTiles(trackRoot, "roadStraightLong", ref cursor, ref heading, 1);

            // Add decorations
            AddBarriers(trackRoot);
            AddEnvironment(trackRoot);

            // Scale the entire track
            trackRoot.transform.localScale = Vector3.one * TileScale;

            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(trackRoot.transform);
            ground.transform.localPosition = new Vector3(5f, -0.01f, 5f);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.3f, 0.5f, 0.2f) // grass green
            };

            Undo.RegisterCreatedObjectUndo(trackRoot, "Build Track MVP");
            Selection.activeGameObject = trackRoot;
            Debug.Log($"Track MVP built with {trackRoot.transform.childCount} children at scale {TileScale}x");
        }

        private static void PlaceTiles(GameObject parent, string pieceName, ref Vector3 cursor, ref float heading, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingKitPath + pieceName + ".dae");
                if (prefab == null)
                {
                    Debug.LogWarning($"Missing piece: {pieceName}");
                    // Advance cursor anyway
                    cursor += Quaternion.Euler(0, heading, 0) * Vector3.forward * TileSize;
                    return;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(parent.transform);
                instance.transform.localPosition = cursor;
                instance.transform.localRotation = Quaternion.Euler(0, heading, 0);
                instance.name = $"{pieceName}_{parent.transform.childCount}";

                // Advance cursor in current heading direction (tiles are 1 unit in Z)
                cursor += Quaternion.Euler(0, heading, 0) * Vector3.forward * 1f;
            }
        }

        private static void PlaceCorner(GameObject parent, string pieceName, ref Vector3 cursor, ref float heading, bool left)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingKitPath + pieceName + ".dae");
            if (prefab == null)
            {
                Debug.LogWarning($"Missing corner piece: {pieceName}");
                heading += left ? -90f : 90f;
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent.transform);
            instance.transform.localPosition = cursor;
            instance.transform.localRotation = Quaternion.Euler(0, heading + (left ? 0 : 0), 0);
            instance.name = $"{pieceName}_{(left ? "L" : "R")}_{parent.transform.childCount}";

            // Corner pieces: advance position and change heading
            // Large corner is 1x1 tile, turns 90 degrees
            var turnDirection = left ? -90f : 90f;
            cursor += Quaternion.Euler(0, heading, 0) * Vector3.forward * 1f;
            heading += turnDirection;
        }

        private static void AddBarriers(GameObject parent)
        {
            // Add some barriers around dangerous points
            var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingKitPath + "barrierRed.dae");
            if (barrierPrefab == null) return;

            // Place a few at corners (approximate positions)
            var positions = new[] {
                new Vector3(5f, 0f, 0.5f),
                new Vector3(5f, 0f, -0.5f),
                new Vector3(-1f, 0f, 5f),
                new Vector3(11f, 0f, 5f),
            };

            foreach (var pos in positions)
            {
                var barrier = (GameObject)PrefabUtility.InstantiatePrefab(barrierPrefab);
                barrier.transform.SetParent(parent.transform);
                barrier.transform.localPosition = pos;
                barrier.name = $"barrier_{parent.transform.childCount}";
            }
        }

        private static void AddEnvironment(GameObject parent)
        {
            // Add trees and grandstand
            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingKitPath + "treeLarge.dae");
            var standPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingKitPath + "grandStand.dae");

            if (treePrefab != null)
            {
                var treePositions = new[] {
                    new Vector3(-2f, 0f, 3f), new Vector3(-2f, 0f, 7f),
                    new Vector3(12f, 0f, 3f), new Vector3(12f, 0f, 7f),
                    new Vector3(3f, 0f, -2f), new Vector3(7f, 0f, -2f),
                };
                foreach (var pos in treePositions)
                {
                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                    tree.transform.SetParent(parent.transform);
                    tree.transform.localPosition = pos;
                    tree.name = $"tree_{parent.transform.childCount}";
                }
            }

            if (standPrefab != null)
            {
                var stand = (GameObject)PrefabUtility.InstantiatePrefab(standPrefab);
                stand.transform.SetParent(parent.transform);
                stand.transform.localPosition = new Vector3(3f, 0f, -2.5f);
                stand.name = "grandStand";
            }
        }
    }
}
#endif

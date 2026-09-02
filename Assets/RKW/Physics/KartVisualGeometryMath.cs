using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 43 (2026-09-01): pure geometry helpers for orienting cosmetic
    /// visual pivots (like the cockpit steering wheel) to match the ACTUAL
    /// tilt of the modeled part, instead of assuming it sits flat in a
    /// fixed local plane. See KartPhysicsPrototypeBootstrap.CreateWheelSteeringPivot
    /// for why this matters: measured directly from KartV2's own source
    /// geometry, the steering wheel disc's real face normal is roughly
    /// (0, 0.61, -0.79) -- tilted back like a real kart's wheel, NOT
    /// pointing straight along the kart's forward axis. Simply copying the
    /// kart body's own rotation onto the pivot (the old behaviour) made
    /// KartSteeringVisual spin the wheel around the wrong axis, which
    /// looks like the wheel sitting "off to the side, at an angle" (the
    /// founder's exact complaint) even though it WAS turning.
    /// </summary>
    internal static class KartVisualGeometryMath
    {
        /// <summary>
        /// Fits a plane to <paramref name="points"/> (least-squares) and
        /// returns its unit normal. Uses the standard PCA approach: the
        /// normal is the eigenvector of the points' covariance matrix with
        /// the SMALLEST eigenvalue (the direction the points vary the
        /// least along -- i.e. "through" the plane, not along it). Needs
        /// at least 3 non-collinear points; returns Vector3.zero if the
        /// input can't determine a plane.
        /// </summary>
        public static Vector3 ComputeBestFitPlaneNormal(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count < 3)
            {
                return Vector3.zero;
            }

            // Round 45 (2026-09-01) founder feedback: "o volante continua
            // torto" -- more specifically, an earlier report that it came
            // up tilted a different way on separate app launches ("a
            // primeira vez ficou para direita, fechei e abri novamente
            // ficou para esquerda") despite the source geometry being
            // completely static. Floating-point addition is not
            // associative, so summing the SAME set of points in a
            // DIFFERENT order can produce tiny differences in the
            // computed covariance matrix -- and when a nearly-flat disc
            // like this steering wheel has two close-to-equal small
            // eigenvalues, a tiny difference like that can flip which one
            // gets picked as "smallest", swinging the resulting normal to
            // a completely different direction. The upstream traversal
            // order feeding this method (GetComponentsInChildren over a
            // freshly-imported model, on Mono, where string-keyed
            // container enumeration order is not guaranteed identical
            // across separate process launches) was never guaranteed
            // stable to begin with. Sorting points into a fixed,
            // value-only order HERE -- independent of whatever order they
            // arrived in -- removes that source of run-to-run drift
            // entirely: the same physical points always sum in the same
            // order, so the result is now provably identical no matter
            // how the caller's collection was ordered (see
            // KartVisualGeometryMathTests.ComputeBestFitPlaneNormal_IsIndependentOfInputPointOrder).
            var sortedPoints = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                sortedPoints[i] = points[i];
            }
            System.Array.Sort(sortedPoints, ComparePointsLexicographically);

            var centroid = Vector3.zero;
            for (var i = 0; i < sortedPoints.Length; i++)
            {
                centroid += sortedPoints[i];
            }
            centroid /= sortedPoints.Length;

            double cxx = 0, cyy = 0, czz = 0, cxy = 0, cxz = 0, cyz = 0;
            for (var i = 0; i < sortedPoints.Length; i++)
            {
                var d = sortedPoints[i] - centroid;
                cxx += (double)d.x * d.x;
                cyy += (double)d.y * d.y;
                czz += (double)d.z * d.z;
                cxy += (double)d.x * d.y;
                cxz += (double)d.x * d.z;
                cyz += (double)d.y * d.z;
            }

            var n = sortedPoints.Length;
            cxx /= n; cyy /= n; czz /= n; cxy /= n; cxz /= n; cyz /= n;

            var normal = SmallestEigenvectorOfSymmetric3x3(cxx, cxy, cxz, cyy, cyz, czz);
            return normal.sqrMagnitude > 1e-12f ? normal.normalized : Vector3.zero;
        }

        /// <summary>
        /// Fixed, value-only ordering (x, then y, then z) -- two calls with
        /// the exact same set of points, in ANY input order, always sort
        /// to the identical sequence. See ComputeBestFitPlaneNormal's own
        /// comment for why this matters.
        /// </summary>
        private static int ComparePointsLexicographically(Vector3 a, Vector3 b)
        {
            var byX = a.x.CompareTo(b.x);
            if (byX != 0)
            {
                return byX;
            }

            var byY = a.y.CompareTo(b.y);
            return byY != 0 ? byY : a.z.CompareTo(b.z);
        }

        /// <summary>
        /// Closed-form eigen-decomposition of a symmetric 3x3 matrix
        /// [[a,d,e],[d,b,f],[e,f,c]], returning the eigenvector for its
        /// SMALLEST eigenvalue. Uses the standard trigonometric solution
        /// for the characteristic cubic of a real symmetric matrix (always
        /// has 3 real eigenvalues, so no iteration is needed) -- see e.g.
        /// Smith, O.K., "Eigenvalues of a symmetric 3x3 matrix" (1961).
        /// </summary>
        private static Vector3 SmallestEigenvectorOfSymmetric3x3(
            double a, double d, double e,
            double b, double f,
            double c)
        {
            var p1 = d * d + e * e + f * f;
            double eig1, eig2, eig3;

            if (p1 < 1e-18)
            {
                eig1 = a;
                eig2 = b;
                eig3 = c;
            }
            else
            {
                var q = (a + b + c) / 3.0;
                var p2 = (a - q) * (a - q) + (b - q) * (b - q) + (c - q) * (c - q) + 2.0 * p1;
                var p = System.Math.Sqrt(p2 / 6.0);

                var bxx = (a - q) / p;
                var byy = (b - q) / p;
                var bzz = (c - q) / p;
                var bxy = d / p;
                var bxz = e / p;
                var byz = f / p;

                var detB = bxx * (byy * bzz - byz * byz)
                           - bxy * (bxy * bzz - byz * bxz)
                           + bxz * (bxy * byz - byy * bxz);

                var r = detB / 2.0;
                r = System.Math.Max(-1.0, System.Math.Min(1.0, r));
                var phi = System.Math.Acos(r) / 3.0;

                eig1 = q + 2.0 * p * System.Math.Cos(phi);
                eig3 = q + 2.0 * p * System.Math.Cos(phi + 2.0 * System.Math.PI / 3.0);
                eig2 = 3.0 * q - eig1 - eig3;
            }

            var smallest = System.Math.Min(eig1, System.Math.Min(eig2, eig3));

            var m00 = a - smallest;
            var m01 = d;
            var m02 = e;
            var m11 = b - smallest;
            var m12 = f;
            var m22 = c - smallest;

            var row0 = new Vector3((float)m00, (float)m01, (float)m02);
            var row1 = new Vector3((float)m01, (float)m11, (float)m12);
            var row2 = new Vector3((float)m02, (float)m12, (float)m22);

            var candidate = Vector3.Cross(row0, row1);
            if (candidate.sqrMagnitude < 1e-12f)
            {
                candidate = Vector3.Cross(row0, row2);
            }
            if (candidate.sqrMagnitude < 1e-12f)
            {
                candidate = Vector3.Cross(row1, row2);
            }

            return candidate;
        }
    }
}

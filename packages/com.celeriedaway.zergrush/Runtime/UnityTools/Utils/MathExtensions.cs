using System;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush
{
    public static class MathExtensions
    {
        public static int Clamp(this int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
        
        public static int LoopAdd(this int i, int val, int cycle)
        {
            return Loop(i + val, cycle);
        }
        
        public static int Loop(this int i, int cycle)
        {
            int r = i % cycle;
            if (r < 0) r += cycle;
            return r;
        }
        
        public static Matrix4x4 ClearScale(this Matrix4x4 matrix)
        {
            var scale = matrix.ExtractScale();
            var res = matrix * Matrix4x4.Scale(new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z));
            var resScale = res.ExtractScale();
            return res;
            //return Matrix4x4.TRS(matrix.ExtractPosition(), matrix.ExtractRotation(), Vector3.one);
        }

        public static Vector3 Up(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m01;
            position.y = matrix.m11;
            position.z = matrix.m21;
            return position;
        }

        public static Vector3 Forward(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m02;
            position.y = matrix.m12;
            position.z = matrix.m22;
            return position;
        }

        public static Vector3 Right(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m00;
            position.y = matrix.m10;
            position.z = matrix.m20;
            return position;
        }

        public static Vector3 ExtractPosition(this ref Matrix4x4 mat)
        {
            Vector3 vector3;
            vector3.x = mat.m03;
            vector3.y = mat.m13;
            vector3.z = mat.m23;
            return vector3;
        }

        public static Vector3 ExtractScale(this Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }

        public static Quaternion ExtractRotation(this ref Matrix4x4 mat)
        {
            Vector3 vector3_1;
            vector3_1.x = mat.m02;
            vector3_1.y = mat.m12;
            vector3_1.z = mat.m22;
            Vector3 vector3_2;
            vector3_2.x = mat.m01;
            vector3_2.y = mat.m11;
            vector3_2.z = mat.m21;
            if ((double) vector3_1.sqrMagnitude < 9.99999974737875E-06 ||
                (double) vector3_2.sqrMagnitude < 9.99999974737875E-06)
                return Quaternion.identity;
            Quaternion quaternion = (Quaternion) UnityEngine.Quaternion.LookRotation((UnityEngine.Vector3) vector3_1,
                (UnityEngine.Vector3) vector3_2);
            if ((double) Math.Abs(quaternion.x) + (double) Math.Abs(quaternion.y) + (double) Math.Abs(quaternion.z) +
                (double) Math.Abs(quaternion.w) < 9.99999997475243E-07)
                Debug.Log("hi");
            return quaternion;
        }
        
        public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
        {
            transform.localScale = matrix.ExtractScale();
            transform.rotation = matrix.ExtractRotation();
            transform.position = matrix.ExtractPosition();
        }
        public static void FromMatrixLocal(this Transform transform, Matrix4x4 matrix)
        {
            transform.localScale = matrix.ExtractScale();
            transform.localRotation = matrix.ExtractRotation();
            transform.localPosition = matrix.ExtractPosition();
        }

        public static (bool, Vector3) PointOnLineProjection(Vector3 pt, Vector3 pt1, Vector3 pt2)
        {
            bool isValid = false;

            var r = new Vector3(0, 0);
            if (pt1.y == pt2.y && pt1.x == pt2.x)
            {
                pt1.y -= 0.00001f;
            }

            var U = (pt.y - pt1.y) * (pt2.y - pt1.y) + (pt.x - pt1.x) * (pt2.x - pt1.x) +
                    (pt.z - pt1.z) * (pt2.z - pt1.z);

            var Udenom = Math.Pow(pt2.y - pt1.y, 2) + Math.Pow(pt2.x - pt1.x, 2) + Math.Pow(pt2.z - pt1.z, 2);

            U /= (float) Udenom;

            r.z = pt1.z + (U * (pt2.z - pt1.z));
            r.y = pt1.y + (U * (pt2.y - pt1.y));
            r.x = pt1.x + (U * (pt2.x - pt1.x));

            double minx, maxx, miny, maxy, minz, maxz;

            minx = Math.Min(pt1.x, pt2.x);
            maxx = Math.Max(pt1.x, pt2.x);

            miny = Math.Min(pt1.y, pt2.y);
            maxy = Math.Max(pt1.y, pt2.y);

            minz = Math.Min(pt1.z, pt2.z);
            maxz = Math.Max(pt1.z, pt2.z);

            isValid = (r.x >= minx && r.x <= maxx) && (r.y >= miny && r.y <= maxy) && (r.z >= minz && r.z <= maxz);

            return isValid ? (true, r) : (false, new Vector3());
        }

        public static (bool, Vector2) PointOnLineProjection(Vector2 pt, Vector2 pt1, Vector2 pt2)
        {
            bool isValid = false;

            var r = new Vector2(0, 0);
            if (pt1.y == pt2.y && pt1.x == pt2.x)
            {
                pt1.y -= 0.00001f;
            }

            var U = ((pt.y - pt1.y) * (pt2.y - pt1.y)) + ((pt.x - pt1.x) * (pt2.x - pt1.x));

            var Udenom = Math.Pow(pt2.y - pt1.y, 2) + Math.Pow(pt2.x - pt1.x, 2);

            U /= (float) Udenom;

            r.y = pt1.y + (U * (pt2.y - pt1.y));
            r.x = pt1.x + (U * (pt2.x - pt1.x));

            double minx, maxx, miny, maxy;

            minx = Math.Min(pt1.x, pt2.x);
            maxx = Math.Max(pt1.x, pt2.x);

            miny = Math.Min(pt1.y, pt2.y);
            maxy = Math.Max(pt1.y, pt2.y);

            isValid = (r.x >= minx && r.x <= maxx) && (r.y >= miny && r.y <= maxy);

            return isValid ? (true, r) : (false, new Vector2());
        }

        public static void SafeAdd(this Cell<int> cell, int val, int max)
        {
            cell.value = Math.Min(max, cell.value + val);
        }

        public static void SafeSubstract(this Cell<int> cell, int val, int min = 0)
        {
            cell.value = Math.Max(min, cell.value - val);
        }

        public static void SafeSubstract(this Cell<float> cell, float val, float min = 0)
        {
            cell.value = Math.Max(min, cell.value - val);
        }

        public static void SafeSubstract(this Cell<ushort> cell, int val)
        {
            cell.value = (ushort) Math.Max(0, cell.value - val);
        }

        public static void SafeSubstract(this Cell<byte> cell, int val)
        {
            cell.value = (byte) Math.Max(0, cell.value - val);
        }

        public static void AddLooped(this Cell<float> cell, float val, float loop)
        {
            cell.value += val;
            if (cell.value >= loop) cell.value -= loop;
        }
    }
}
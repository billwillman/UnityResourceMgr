#if UNITY_2018_1_OR_NEWER
#define _USE_NATIVE_ARRAY
#endif

using System;
using System.Runtime.InteropServices;
using UnityEngine;

#if _USE_NATIVE_ARRAY
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

public static class UnsafeUtil {
    public static float UintToFloat(uint u) {
        unsafe {
            return *(float*)&u;
        }
    }

    public static uint FloatToUint(float f) {
        unsafe {
            return *(uint*)&f;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ArrayHeader {
        internal IntPtr type;
        internal int length;
    }

    private unsafe static void HackArraySizeCall<TA>(TA[] array, ArrayHeader* header, int size, Action<TA[]> func) {
        int oriLen = header->length;
        header->length = size;
        try {
            func(array);
        } finally {
            header->length = oriLen;
        }
    }

    public unsafe static void IntegerHackArraySizeCall(int[] array, int size, Action<int[]> func) {
        if (array != null && size < array.Length) {
            fixed (void* p = array) {
                HackArraySizeCall(array, ((ArrayHeader*)p) - 1, size, func);
                return;
            }
        }
        func(array);
    }
    public unsafe static void Vector2HackArraySizeCall(Vector2[] array, int size, Action<Vector2[]> func) {
        if (array != null && size < array.Length) {
            fixed (void* p = array) {
                HackArraySizeCall(array, ((ArrayHeader*)p) - 1, size, func);
                return;
            }
        }
        func(array);
    }
    public unsafe static void Vector3HackArraySizeCall(Vector3[] array, int size, Action<Vector3[]> func) {
        if (array != null && size < array.Length) {
            fixed (void* p = array) {
                HackArraySizeCall(array, ((ArrayHeader*)p) - 1, size, func);
                return;
            }
        }
        func(array);
    }
    public unsafe static void Vector4HackArraySizeCall(Vector4[] array, int size, Action<Vector4[]> func) {
        if (array != null && size < array.Length) {
            fixed (void* p = array) {
                HackArraySizeCall(array, ((ArrayHeader*)p) - 1, size, func);
                return;
            }
        }
        func(array);
    }
    public unsafe static void Color32HackArraySizeCall(Color32[] array, int size, Action<Color32[]> func) {
        if (array != null && size < array.Length) {
            fixed (void* p = array) {
                HackArraySizeCall(array, ((ArrayHeader*)p) - 1, size, func);
                return;
            }
        }
        func(array);
    }
    public unsafe static void BoneWeightHackArraySizeCall(BoneWeight[] array, int size, Action<BoneWeight[]> func) {
        if (array != null && size < array.Length) {
            fixed (void* p = array) {
                HackArraySizeCall(array, ((ArrayHeader*)p) - 1, size, func);
                return;
            }
        }
        func(array);
    }

#if _USE_NATIVE_ARRAY
    public unsafe static T UncheckReadArrayElement<T>(NativeArray<T> narr, int idx) where T : struct {
        void* ptr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<T>(narr);
        return UnsafeUtility.ReadArrayElement<T>(ptr, idx);
    }
#endif
}
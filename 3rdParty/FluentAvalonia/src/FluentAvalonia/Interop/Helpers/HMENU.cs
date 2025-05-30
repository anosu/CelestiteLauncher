﻿// Adapted from TerraFX.Interop.Windows, MIT license

namespace FluentAvalonia.Interop.Win32;

internal readonly unsafe struct HMENU : IComparable, IEquatable<HMENU>
{
    public readonly void* Value;

    public HMENU(void* value)
    {
        Value = value;
    }

    public static HMENU INVALID_VALUE => new HMENU((void*)(-1));

    public static HMENU NULL => new HMENU(null);

    public static bool operator ==(HMENU left, HMENU right) => left.Value == right.Value;

    public static bool operator !=(HMENU left, HMENU right) => left.Value != right.Value;

    public static bool operator <(HMENU left, HMENU right) => left.Value < right.Value;

    public static bool operator <=(HMENU left, HMENU right) => left.Value <= right.Value;

    public static bool operator >(HMENU left, HMENU right) => left.Value > right.Value;

    public static bool operator >=(HMENU left, HMENU right) => left.Value >= right.Value;

    public static explicit operator HMENU(void* value) => new HMENU(value);

    public static implicit operator void*(HMENU value) => value.Value;

    public static explicit operator HMENU(byte value) => new HMENU((void*)(value));

    public static explicit operator byte(HMENU value) => (byte)(value.Value);

    public static explicit operator HMENU(short value) => new HMENU((void*)(value));

    public static explicit operator short(HMENU value) => (short)(value.Value);

    public static explicit operator HMENU(int value) => new HMENU((void*)(value));

    public static explicit operator int(HMENU value) => (int)(value.Value);

    public static explicit operator HMENU(long value) => new HMENU((void*)(value));

    public static explicit operator long(HMENU value) => (long)(value.Value);

    public static explicit operator HMENU(nint value) => new HMENU((void*)(value));

    public static implicit operator nint(HMENU value) => (nint)(value.Value);

    public static explicit operator HMENU(sbyte value) => new HMENU((void*)(value));

    public static explicit operator sbyte(HMENU value) => (sbyte)(value.Value);

    public static explicit operator HMENU(ushort value) => new HMENU((void*)(value));

    public static explicit operator ushort(HMENU value) => (ushort)(value.Value);

    public static explicit operator HMENU(uint value) => new HMENU((void*)(value));

    public static explicit operator uint(HMENU value) => (uint)(value.Value);

    public static explicit operator HMENU(ulong value) => new HMENU((void*)(value));

    public static explicit operator ulong(HMENU value) => (ulong)(value.Value);

    public static explicit operator HMENU(nuint value) => new HMENU((void*)(value));

    public static implicit operator nuint(HMENU value) => (nuint)(value.Value);

    public int CompareTo(object obj)
    {
        if (obj is HMENU other)
        {
            return CompareTo(other);
        }

        return (obj is null) ? 1 : throw new ArgumentException("obj is not an instance of HMENU.");
    }

    //public int CompareTo(HMENU other) => ((nuint)(Value)).CompareTo((nuint)(other.Value));

    public override bool Equals(object obj) => (obj is HMENU other) && Equals(other);

    public bool Equals(HMENU other) => ((nuint)(Value)).Equals((nuint)(other.Value));

    public override int GetHashCode() => ((nuint)(Value)).GetHashCode();

    // public override string ToString() => ((nuint)(Value)).ToString((sizeof(nint) == 4) ? "X8" : "X16");

    //public string ToString(string? format, IFormatProvider? formatProvider) => ((nuint)(Value)).ToString(format, formatProvider);
}








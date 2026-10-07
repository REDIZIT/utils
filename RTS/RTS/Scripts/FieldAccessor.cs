using System;
using System.IO;
using System.Reflection;

namespace RTS
{
	public class FieldAccessor
	{
		private readonly FieldInfo field;
		private readonly Type fieldType;
		private readonly bool isPayload;
		private readonly bool isEnum;

		public FieldAccessor(FieldInfo field)
		{
			this.field = field;
			fieldType = field.FieldType;
			isPayload = typeof(IPayload).IsAssignableFrom(fieldType);
			isEnum = fieldType.IsEnum;
		}

		public void Write(object target, MessageWriter w)
		{
			object? value = field.GetValue(target);

			if (isPayload)
			{
				w.WritePayload((IPayload)value!);
				return;
			}

			if (isEnum)
			{
				w.Write(Convert.ToInt32(value));
				return;
			}

			WritePrimitive(w, value, fieldType);
		}

		public void Read(object target, MessageReader r)
		{
			if (isPayload)
			{
				field.SetValue(target, r.ReadPayload());
				return;
			}

			if (isEnum)
			{
				int enumVal = r.ReadInt32();
				field.SetValue(target, Enum.ToObject(fieldType, enumVal));
				return;
			}

			object value = ReadPrimitive(r, fieldType);
			field.SetValue(target, value);
		}

		private static void WritePrimitive(BinaryWriter w, object? val, Type type)
		{
			if (type == typeof(string))
			{
				w.Write((string)(val ?? string.Empty));
			}
			else if (type == typeof(int)) w.Write((int)(val ?? 0));
			else if (type == typeof(long)) w.Write((long)(val ?? 0L));
			else if (type == typeof(bool)) w.Write((bool)(val ?? false));
			else if (type == typeof(float)) w.Write((float)(val ?? 0.0f));
			else if (type == typeof(double)) w.Write((double)(val ?? 0.0));
			else if (type == typeof(byte)) w.Write((byte)(val ?? (byte)0));
			else if (type == typeof(short)) w.Write((short)(val ?? (short)0));
			else if (type == typeof(byte[]))
			{
				byte[] bytes = (byte[])val!;
				if (bytes == null)
				{
					w.Write((int)-1);
				}
				else
				{
					w.Write(bytes.Length);
					w.Write(bytes);
				}
			}
			else
			{
				throw new NotSupportedException($"Type '{type.FullName}' is not supported by Serializer primitive mapping.");
			}
		}

		private static object ReadPrimitive(BinaryReader r, Type type)
		{
			if (type == typeof(string)) return r.ReadString();
			if (type == typeof(int)) return r.ReadInt32();
			if (type == typeof(long)) return r.ReadInt64();
			if (type == typeof(bool)) return r.ReadBoolean();
			if (type == typeof(float)) return r.ReadSingle();
			if (type == typeof(double)) return r.ReadDouble();
			if (type == typeof(byte)) return r.ReadByte();
			if (type == typeof(short)) return r.ReadInt16();
			if (type == typeof(byte[]))
			{
				int length = r.ReadInt32();
				return length == -1 ? Array.Empty<byte>() : r.ReadBytes(length);
			}

			throw new NotSupportedException($"Type '{type.FullName}' is not supported by Serializer primitive mapping.");
		}
	}
}
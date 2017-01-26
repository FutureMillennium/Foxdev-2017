using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uide
{
	class ByteToHex
	{
		private static readonly uint[] _lookup32 = CreateLookup32();

		private static uint[] CreateLookup32()
		{
			var result = new uint[256];
			for (int i = 0; i < 256; i++)
			{
				string s = i.ToString("x2");
				result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
			}
			return result;
		}

		public static string ByteArrayToHexViaLookup32(byte[] bytes, int start = 0, int length = 0)
		{
			uint[] lookup32 = _lookup32;

			if (length == 0)
				length = bytes.Length - start;

			char[] result = new char[length * 2];
			for (int i = 0; i < length; i++)
			{
				uint val = lookup32[bytes[i + start]];
				result[2 * i] = (char)val;
				result[2 * i + 1] = (char)(val >> 16);
			}
			return new string(result);
		}
	}
}

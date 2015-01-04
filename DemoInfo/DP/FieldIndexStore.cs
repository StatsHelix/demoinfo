using System;
using System.Collections.Generic;

namespace DemoInfo
{
	public struct FieldIndexQueue
	{
		public int Count;

		private long value1; //These are the first 9 numbers
		private long value2; //These are the numbers 9 ... 18

		private List<long> backup;

		const int maskbits = 12;
		const int mask = (1 << maskbits) - 1; //4095 (== max value)
		const int numsPerLong = 64 / maskbits; //5
		const int numsPerLong2 = 2 * numsPerLong; //10


		public void PushValue(long value)
		{
			value &= mask;

			if (Count < numsPerLong)
				value1 |= value << (maskbits * Count);
			else if (Count < numsPerLong2)
				value2 |= value << (maskbits * (Count - numsPerLong));
			else if (Count == numsPerLong2)
				backup = new List<long> () { value };
			else
				backup.Add (value);

			Count++;
		}

		public long GetValue(int index)
		{
			if (index < numsPerLong)
				return ((value1 >> (maskbits * index)) & mask);
			else if (index < numsPerLong2)
				return ((value2 >> (maskbits * (index - numsPerLong))) & mask);
			else
				return backup[index - numsPerLong2];
		}

		public override string ToString ()
		{
			string res = "";
			for (int i = 0; i < this.Count; i++) {
				res += GetValue (i) + ",";
			}

			return res;
		}
	}
}


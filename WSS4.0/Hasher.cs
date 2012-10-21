using System;
using System.Text;
using System.Security;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace zmeng.SharePoint
{
	interface IHashing
	{
		string CalculateHash(object seed);
		bool ValidateHash(object seed, string hash);
	}

	public class HasherType1 : IHashing
	{
		#region enum OptionsFlags
		public enum OptionsFlags
		{
			HashUserLogins		= 1,
			HashItemId			= 2,
			HashTimestamp		= 4,
		}
		#endregion

		private OptionsFlags options = OptionsFlags.HashUserLogins
			| OptionsFlags.HashItemId
			| OptionsFlags.HashTimestamp;

		public HasherType1()
		{
			// Initialise with default options
		}

		public OptionsFlags Options
		{
			get { return options; }
			set { options = value; }
		}

		public string CalculateHash(object seed)
		{
			DateTime timestamp = DateTime.Now;

			List<byte> bin = new List<byte>();

			bin.Add((byte)1);			// Version
			bin.Add((byte)options);	// Options
			bin.AddRange(BitConverter.GetBytes(timestamp.Ticks));
			bin.AddRange(CalculateRawHash(seed, timestamp, options));

			return Convert.ToBase64String(bin.ToArray());
		}
		public bool ValidateHash(object seed, string hash)
		{
			byte[] bin = Convert.FromBase64String(hash);

			if (bin[0] != 1)
				throw new Exception("Unknown hash version");

			OptionsFlags instanceOptions = (OptionsFlags)bin[1];
			DateTime timestamp = new DateTime(BitConverter.ToInt64(bin, 2));

			byte[] computedHash = CalculateRawHash(seed, timestamp, instanceOptions);

			for (int i = 0; i < computedHash.Length; i++)
			{
				if (computedHash[i] != bin[bin.Length - computedHash.Length + i])
					return false;
			}

			return true;
		}
		private byte[] CalculateRawHash(object seed, DateTime timestamp, OptionsFlags instanceOptions)
		{
			if (!(seed is Seed))
				throw new Exception("Unknown seed object");

			Seed castSeed = (Seed)seed;

			List<byte> data = new List<byte>();

			if ((instanceOptions & OptionsFlags.HashUserLogins) == OptionsFlags.HashUserLogins)
				data.AddRange(Encoding.ASCII.GetBytes(castSeed.userLogins));

			if ((instanceOptions & OptionsFlags.HashItemId) == OptionsFlags.HashItemId)
				data.AddRange(castSeed.itemId.ToByteArray());

			if ((instanceOptions & OptionsFlags.HashTimestamp) == OptionsFlags.HashTimestamp)
				data.AddRange(BitConverter.GetBytes(timestamp.Ticks));

			MD5 md5 = MD5.Create();
			return md5.ComputeHash(data.ToArray());
		}

		#region struct Seed
		public struct Seed
		{
			public Seed (string userLogins, Guid itemId)
			{
				this.userLogins = userLogins;
				this.itemId = itemId;
			}

			public string userLogins;
			public Guid itemId;
		}
		#endregion
	}
}

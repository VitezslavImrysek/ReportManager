using System.Security.Cryptography;
using System.Text;

namespace ReportAdmin.Core.Utils;

public static class GuidUtil
{
	private static readonly Guid NamespaceGuid = Guid.Parse("6b84c6a7-8e3b-4b5a-9f4b-1e2a52b40c3a");

	public static Guid FromPresetKey(string presetKey)
		=> CreateDeterministicGuid(NamespaceGuid, presetKey);

	private static Guid CreateDeterministicGuid(Guid ns, string name)
	{
		byte[] nsBytes = ns.ToByteArray();
		SwapByteOrder(nsBytes);

		byte[] nameBytes = Encoding.UTF8.GetBytes(name);

		byte[] hash;
		using (var sha1 = SHA1.Create())
		{
			sha1.TransformBlock(nsBytes, 0, nsBytes.Length, null, 0);
			sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
			hash = sha1.Hash!;
		}

		var newGuid = new byte[16];
		Array.Copy(hash, 0, newGuid, 0, 16);

		newGuid[6] = (byte)((newGuid[6] & 0x0F) | 0x50); // v5
		newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80); // rfc4122

		SwapByteOrder(newGuid);
		return new Guid(newGuid);
	}

	private static void SwapByteOrder(byte[] guid)
	{
		void Swap(int a, int b) { (guid[a], guid[b]) = (guid[b], guid[a]); }
		Swap(0, 3); Swap(1, 2); Swap(4, 5); Swap(6, 7);
	}
}

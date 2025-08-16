using System.Security.Cryptography;
using System.Text;

namespace Xams.Core.Utils;

public class GuidUtil
{
    public static Guid FromString(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashedBytes = sha256.ComputeHash(inputBytes);

            // Truncate the hash to 16 bytes to fit the size of a Guid.
            byte[] truncatedBytes = new byte[16];
            Array.Copy(hashedBytes, truncatedBytes, 16);

            return new Guid(truncatedBytes);
        }
    }
}
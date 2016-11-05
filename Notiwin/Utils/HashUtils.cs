using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Notiwin.Utils {
    class HashUtils {

        private static MD5 md5;

        public static string ComputeHash(byte[] data) {
            if (md5 == null) {
                md5 = MD5.Create();
            }

            byte[] hash = md5.ComputeHash(data);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}

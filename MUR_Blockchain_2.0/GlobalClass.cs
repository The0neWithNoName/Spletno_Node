using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MUR_Blockchain_2._0
{
    static public class GlobalClass
    {
        public static Blockchain blockchain = new Blockchain();
        public static string id;
        public static RSAParameters publicKey;

        public static RSAParameters privateKey;
        public static string xmlPublicKey; 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MUR_Blockchain_2._0
{
    public class Block
    {
        public int index { get; set; }
        public DateTime timestamp { get; set; }
        public string data { get; set; }
        public string prevHash { get; set; }
        public string hash { get; set; }
        public int nonece { get; set; }


        public Block(int index, string data, string prevHash)
        {
            this.index = index;
            this.timestamp = DateTime.Now;
            this.data = data;
            this.prevHash = prevHash;
            this.nonece = 0;
            this.hash = this.calculateHash();

        }



        public string calculateHash()
        {
            string block = this.index.ToString() + this.prevHash + this.timestamp + this.data + this.nonece;
            using (var sha256 = new SHA256Managed())
            {
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(block))).Replace("-", "");
            }
        }

        public void mineBlock(int difficulty)
        {
            string zeroString = "";
            for (int i = 0; i < difficulty; i++)
                zeroString += "0";

            
            while (this.hash.Substring(0, difficulty) != zeroString)
            {
                this.hash = this.calculateHash();
                this.nonece++;
                
            }
        }

        public string toString()
        {
            string output =  "-----------------------------------\n" +
                    "Block: " + this.index + "\n" +
                    "Time stamp: " + this.timestamp.ToString() + "\n" +
                    "Data: " + this.data + "\n"+
                    "Hash: " + this.hash + "\n" +
                    "-----------------------------------\n";
            return output;
        }   


        public string blockToJson()
        {
            return JsonConvert.SerializeObject(this);
        }



    }

}

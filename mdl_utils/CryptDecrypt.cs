using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace mdl_utils {
    public class CryptDecrypt {
        

        /// <summary>
        /// Convert a string like a='2';b=#3#;c='12'.. into a string hashtable
        /// </summary>
        /// <param name="S1"></param>
        /// <returns></returns>
        public static Hashtable GetHashFromString(string S1) {
            var HH = new Hashtable();

            byte[] B1 = Quoting.StringToByteArray(S1);
            string S = CryptDecrypt.DecryptString(B1);

            int i = 0;
            while (i < S.Length) {
                //prende l'identificatore all'inizio di S, fino all'uguale
                int poseq = S.IndexOf("=");
                if (poseq <= 0) break;
                string myfield = S.Substring(0, poseq).Trim();
                S = S.Substring(poseq + 1);
                char SEP = S[0];
                if (SEP != '\'' && SEP != '#') break;
                int index = 1;
                while (index < S.Length) {
                    //ad ogni iterazione index è la posizione da cui partire (inclusa) per la ricerca del prossimo apice
                    index = S.IndexOf(SEP, index);
                    if (index <= 0) break;
                    if ((index + 1) >= S.Length) break; //ha trovato l'apice (finale)
                    if (S[index + 1] != SEP) break; //ha trovato l'apice (non è seguito da un altro apice)
                    index += 2;
                }
                if ((index < 1) || (index >= S.Length)) break; //non ha trovato l'apice
                if (S[index] != SEP) break;  //non ha trovato l'apice
                string val = S.Substring(1, index - 1);
                if (SEP == '\'') {
                    val = val.Replace("''", "'"); //toglie il doppio apice in tutto val
                }
                else {
                    val = "#" + val + "#";
                }
                try {
                    HH[myfield] = val;
                }
                catch { }
                if (index + 2 >= S.Length) break; //Se S è finita esci
                S = S.Substring(index + 2);
            }
            return HH;
        }

        /// <summary>
        /// Convert an hashtable into a string like a='2';b=#3#;c='12'.. 
        /// </summary>
        /// <param name="H"></param>
        /// <returns></returns>
        public static string GetStringFromHashTable(Hashtable H) {
            
            string S = "";
            foreach (object key in H.Keys) {
                if (S != "") S += ";";
                S = S + key.ToString() + "=" + Quoting.quote(H[key]);
            }

            byte[] B2 = CryptString(S); //dati criptati
            string SS = Quoting.ByteArrayToString(B2); //stringa dei dati criptati
            return SS;
        }


        /// <summary>
        /// Crypts a string with 3-des
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] CryptString(string key) {
            if (key == null) return null;
            byte[] A = Encoding.Default.GetBytes(key);

            var MS = new MemoryStream(1000);
            var CryptoS = new CryptoStream(MS,
                new TripleDESCryptoServiceProvider().CreateEncryptor(
                new byte[] { 75, 12, 0, 215, 93, 89, 45, 11, 171, 96, 4, 64, 13, 158, 36, 190 },
                new byte[] { 61, 12, 99, 78, 149, 123, 147, 48, 81, 20, 238, 57, 125, 38, 13, 4 }
                ), CryptoStreamMode.Write);
            CryptoS.Write(A, 0, A.Length);
            CryptoS.FlushFinalBlock();
            byte[] B = MS.ToArray();
            return B;
        }


        /// <summary>
        /// Decrypts a string with 3-des
        /// </summary>
        /// <param name="B"></param>
        /// <returns></returns>
		public static string DecryptString(byte[] B) {
            if (B == null) return null;
            var MS = new MemoryStream();
            var CryptoS = new CryptoStream(MS,
                new TripleDESCryptoServiceProvider().CreateDecryptor(
                new byte[] { 75, 12, 0, 215, 93, 89, 45, 11, 171, 96, 4, 64, 13, 158, 36, 190 },
                new byte[] { 61, 12, 99, 78, 149, 123, 147, 48, 81, 20, 238, 57, 125, 38, 13, 4 }
                ), CryptoStreamMode.Write);
            CryptoS.Write(B, 0, B.Length);
            CryptoS.FlushFinalBlock();
            string key = Encoding.Default.GetString(MS.ToArray()).TrimEnd();
            return key;
        }

        /// <summary>
        /// Crypts an array of bytes  with 3-des
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static byte[] CryptBytes(byte[] A) {
            var MS = new MemoryStream(1000);
            var CryptoS = new CryptoStream(MS,
                new TripleDESCryptoServiceProvider().CreateEncryptor(
                new byte[] { 75, 12, 0, 215, 93, 89, 45, 11, 171, 96, 4, 64, 13, 158, 36, 190 },
                new byte[] { 61, 12, 99, 78, 149, 123, 147, 48, 81, 20, 238, 57, 125, 38, 13, 4 }
                ), CryptoStreamMode.Write);
            CryptoS.Write(A, 0, A.Length);
            CryptoS.FlushFinalBlock();
            byte[] B = MS.ToArray();
            return B;
        }


        /// <summary>
        /// Decryps an array of bytes   with 3-des
        /// </summary>
        /// <param name="B"></param>
        /// <returns></returns>
		public static byte[] DecryptBytes(byte[] B) {
            if (B == null) return null;
            var MS = new MemoryStream();
            var CryptoS = new CryptoStream(MS,
                new TripleDESCryptoServiceProvider().CreateDecryptor(
                new byte[] { 75, 12, 0, 215, 93, 89, 45, 11, 171, 96, 4, 64, 13, 158, 36, 190 },
                new byte[] { 61, 12, 99, 78, 149, 123, 147, 48, 81, 20, 238, 57, 125, 38, 13, 4 }
                ), CryptoStreamMode.Write);
            CryptoS.Write(B, 0, B.Length);
            CryptoS.FlushFinalBlock();
            return MS.ToArray();
        }

    }
}

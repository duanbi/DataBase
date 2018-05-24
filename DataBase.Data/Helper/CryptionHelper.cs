using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DataBase.Data
{
    /// <summary>
    /// 加密通用类
    /// </summary>
    public class CryptionHelper
    {
        #region DES

        /// <summary>
        /// DES加密数据
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="sKey"></param>
        /// <returns></returns>
        public static string DesEncrypt(string Text, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray;
            inputByteArray = Encoding.Default.GetBytes(Text);
            des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
            MemoryStream ms = new MemoryStream();
            des.Mode = CipherMode.CBC;

            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            StringBuilder ret = new StringBuilder();
            foreach (byte b in ms.ToArray())
            {
                ret.AppendFormat("{0:x2}", b);
            }
            return ret.ToString();
        }

        /// <summary>
        /// DES解密数据
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="sKey"></param>
        /// <returns></returns>
        public static string DesDecrypt(string Text, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            int len;
            len = Text.Length / 2;
            byte[] inputByteArray = new byte[len];
            int x, i;
            for (x = 0; x < len; x++)
            {
                i = Convert.ToInt32(Text.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte)i;
            }
            des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.Default.GetString(ms.ToArray());
        }

        #endregion

        #region AES加密

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="text">密文</param>
        /// <param name="key">密钥,长度为16的字符串</param>
        /// <param name="iv">偏移量,长度为16的字符串</param>
        /// <returns>明文</returns>
        public static string AESDecrypt(string text, string key, string iv)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                KeySize = 128,
                BlockSize = 128
            };
            byte[] encryptedData = HexStrToByte(text);
            byte[] pwdBytes = UTF8Encoding.UTF8.GetBytes(key);
            byte[] keyBytes = new byte[16];
            int len = pwdBytes.Length;
            if (len > keyBytes.Length)
                len = keyBytes.Length;
            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;

            byte[] ivBytes = UTF8Encoding.UTF8.GetBytes(iv); //偏移向量
            byte[] ivBytesNew = new byte[16];
            len = ivBytes.Length;
            if (len > 16)
                len = ivBytesNew.Length;
            Array.Copy(ivBytes, ivBytesNew, len);  //复制IV16位作为密钥
            rijndaelCipher.IV = ivBytesNew;

            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();
            byte[] plainText = transform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            return UTF8Encoding.UTF8.GetString(plainText).Replace("\0", "");
        }

        /// <summary>
        /// AES加密方法 128位加密
        /// </summary>
        /// <param name="text">明文</param>
        /// <param name="key">密钥,长度为16的字符串</param>
        /// <param name="iv">偏移量,长度为16的字符串</param>
        /// <returns>密文</returns>
        public static string AESEncrypt(string text, string key, string iv)
        {
            try
            {
                RijndaelManaged rijndaelCipher = new RijndaelManaged
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.Zeros,
                    KeySize = 128,
                    BlockSize = 128
                };

                //密码模式
                //填充模式
                //密钥为128位
                byte[] pwdBytes = UTF8Encoding.UTF8.GetBytes(key);
                byte[] keyBytes = new byte[16];
                int len = pwdBytes.Length;
                if (len > 16)
                    len = keyBytes.Length;
                Array.Copy(pwdBytes, keyBytes, len);  //复制key16位作为密钥

                rijndaelCipher.Key = keyBytes;               //加密密钥

                byte[] ivBytes = UTF8Encoding.UTF8.GetBytes(iv); //偏移向量
                byte[] ivBytesNew = new byte[16];
                len = ivBytes.Length;
                if (len > 16)
                    len = ivBytesNew.Length;
                Array.Copy(ivBytes, ivBytesNew, len);  //复制IV16位作为密钥
                rijndaelCipher.IV = ivBytesNew;

                ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
                byte[] plainText = UTF8Encoding.UTF8.GetBytes(text);
                byte[] cipherBytes = transform.TransformFinalBlock(plainText, 0, plainText.Length);

                return ByteToHexStr(cipherBytes);
            }
            catch (IOException ex) { throw ex; }
            catch (CryptographicException ex) { throw ex; }
            catch (ArgumentException ex) { throw ex; }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// 字符串转16进制字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private static byte[] HexStrToByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string ByteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("x2");
                }
            }
            return returnStr;
        }

        #endregion

        #region Md5

        /// <summary>
        /// Md5加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Md5(string str, bool isLower = false)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            b = new MD5CryptoServiceProvider().ComputeHash(b);
            string ret = "";
            for (int i = 0; i < b.Length; i++)
                ret += b[i].ToString(isLower ? "x" : "X").PadLeft(2, '0');
            return ret;
        }

        #endregion
    }
    
}
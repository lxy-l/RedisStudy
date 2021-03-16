using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    /// <summary>
    /// 加解密工具类
    /// </summary>
    public class EncyptHelper
    {
        #region MD5加密
        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <returns>MD5结果</returns>
        public static string MD5(string value)
        {
            byte[] result = Encoding.UTF8.GetBytes(value);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            return BitConverter.ToString(output).Replace("-", "");
        }
        #endregion

        #region SHA256加密
        /// <summary>
        /// SHA256加密
        /// </summary>
        /// /// <param name="value">原始字符串</param>
        /// <returns>SHA256加密结果</returns>
        public static string SHA256(string value)
        {
            byte[] SHA256Data = Encoding.UTF8.GetBytes(value);
            SHA256Managed Sha256 = new SHA256Managed();
            byte[] Result = Sha256.ComputeHash(SHA256Data);
            return Convert.ToBase64String(Result);
        }
        #endregion

        #region Base64加解密

        /// <summary>
        /// base64编码
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <returns></returns>
        public static string Base64Encode(string value)
        {
            string result = Convert.ToBase64String(Encoding.Default.GetBytes(value));
            return result;
        }
        /// <summary>
        /// base64解码
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <returns></returns>
        public static string Base64Decode(string value)
        {
            string result = Encoding.Default.GetString(Convert.FromBase64String(value));
            return result;
        }
        #endregion

        #region RSA加解密
        public static (string publicKey, string privateKey) CreateKey()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                string publicKey = rsa.ToXmlString(false); // 公钥
                string privateKey = rsa.ToXmlString(true); // 私钥
                return (publicKey: publicKey, privateKey: privateKey);
            }
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string RSADecrypt(string value, string privateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            var cipherbytes = rsa.Decrypt(Convert.FromBase64String(value), true);
            return Encoding.UTF8.GetString(cipherbytes);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string RSAEncrypt(string value, string publicKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            var cipherbytes = rsa.Encrypt(Encoding.UTF8.GetBytes(value), true);
            return Convert.ToBase64String(cipherbytes);

        }
        #endregion
    }
}

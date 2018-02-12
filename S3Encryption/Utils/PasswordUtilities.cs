using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace S3Encryption.Utils
{
    static class PasswordUtilities
    {
        const int DEFAULT_LENGTH = 8;
        const int MIN_PASSWORD_LENGTH = 8;
        const int MAX_PASSWORD_LENGTH = 32;
        const string POSSIBLE_PASSWORD_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890~!$%^*()-+[]{},.<>?";
        const string PASSWORD_PATTERN = "((?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[~!$%^*\\(\\)\\-+\\[\\]\\{\\},.<>?]).{8,32})";


        public static byte[] GenerateSecuredPassword(int len)
        {
            int length = GetLength(len);
            string s;
            do
            {
                s = RandomStringUtils.Random(length, 0, POSSIBLE_PASSWORD_CHARS.Length - 1, true, true, POSSIBLE_PASSWORD_CHARS.ToCharArray());

            } while (!Regex.IsMatch(s, PASSWORD_PATTERN));


            return ASCIIEncoding.UTF8.GetBytes(s);
        }


        private static int GetLength(int length)
        {
            if (length == 0)
            {
                return DEFAULT_LENGTH;
            }
            if (length > MAX_PASSWORD_LENGTH || length < MIN_PASSWORD_LENGTH)
            {
                throw new ArgumentOutOfRangeException("length", "Invalid password length specified. Password length must be between 8 and 32");
            }
            return length;
        }
    }
}

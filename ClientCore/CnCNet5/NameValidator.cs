using System;
using System.Linq;

namespace ClientCore.CnCNet5
{
    public static class NameValidator
    {
        /// <summary>
        /// Checks if the player's nickname is valid for CnCNet.
        /// </summary>
        /// <returns>Null if the nickname is valid, otherwise a string that tells
        /// what is wrong with the name.</returns>
        public static string IsNameValid(string name)
        {
            var profanityFilter = new ProfanityFilter();

            if (string.IsNullOrEmpty(name))
                return "请输入您的游戏名";

            if (profanityFilter.IsOffensive(name))
                return "请不要口吐芬芳";

            int number = -1;
            if (int.TryParse(name.Substring(0, 1), out number))
                return "游戏名的第一位不能是数字";

            if (name[0] == '-')
                return "游戏名的第一位不能是横线 ( - )";

            // Check that there are no invalid chars
            char[] allowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_[]|\\{}^`".ToCharArray();
            char[] nicknameChars = name.ToCharArray();

            foreach (char nickChar in nicknameChars)
            {
                if (!allowedCharacters.Contains(nickChar))
                {
                    return "你的游戏名包含无效字符" + Environment.NewLine +
                    "有效字符为英文字母和数字";
                }
            }

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
                return "你的游戏名太长了";

            return null;
        }
    }
}

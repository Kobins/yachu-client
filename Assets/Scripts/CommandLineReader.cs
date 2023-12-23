namespace Yachu.Client
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    #endregion

    public class CommandLineReader
    {
        //Config
        private const string Prefix = "-";

        public static string[] GetCommandLineArgs()
        {
            return Environment.GetCommandLineArgs();
        }

        public static Dictionary<string, string> GetArguments()
        {
            static void AddDict(Dictionary<string, string> dict, string key, string value)
            {
                if (!dict.TryAdd(key, value ?? "1"))
                {
                    string result = key+(value != null ? $"={value}" : "");
                    Debug.LogWarning($"{result} 삽입 실패");
                }
            }
            
            var dict = new Dictionary<string, string>();
            var args = GetCommandLineArgs();
            string key = null;
            for (int i = 0; i < args.Length; i++)
            {
                var raw = args[i];
                // -로 시작하면 키
                if (raw.StartsWith(Prefix))
                {
                    if (key != null)
                    {
                        AddDict(dict, key, null);
                    }
                    key = raw.Substring(Prefix.Length);
                }
                // - 다음에 일반 스트링은 값으로 묶임
                else if(key != null)
                {
                    AddDict(dict, key, raw);
                    key = null;
                }
            }

#if UNITY_EDITOR
            dict.Add("host", "3.39.24.233");
#endif

            return dict;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TwoLadles.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public const string LANG_EN = "en";
        public const string LANG_VI = "vi";
        public const string LANG_KO = "ko";

        private const string PREF_KEY = "TwoLadles_SelectedLanguage";

        public string CurrentLanguage { get; private set; } = LANG_EN;

        public event Action OnLanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _table = 
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("LocalizationManager");
                Instance = go.AddComponent<LocalizationManager>();
                DontDestroyOnLoad(go);
                Instance.Init();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Init();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Init()
        {
            LoadLocalizationTable();

            if (PlayerPrefs.HasKey(PREF_KEY))
            {
                string saved = PlayerPrefs.GetString(PREF_KEY, LANG_EN);
                SetLanguage(saved, false);
            }
            else
            {
                string detected = LANG_EN;
                if (Application.systemLanguage == SystemLanguage.Vietnamese)
                {
                    detected = LANG_VI;
                }
                else if (Application.systemLanguage == SystemLanguage.Korean)
                {
                    detected = LANG_KO;
                }
                SetLanguage(detected, true);
            }
        }

        private void LoadLocalizationTable()
        {
            _table.Clear();
            _table[LANG_EN] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _table[LANG_VI] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _table[LANG_KO] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            TextAsset csvAsset = Resources.Load<TextAsset>("Localization");
            if (csvAsset == null)
            {
                Debug.LogWarning("[LocalizationManager] Localization.csv not found in Resources!");
                return;
            }

            using (StringReader reader = new StringReader(csvAsset.text))
            {
                string header = reader.ReadLine();
                if (string.IsNullOrEmpty(header)) return;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = ParseCsvLine(line);
                    if (parts.Count < 2) continue;

                    string key = parts[0].Trim();
                    string en = parts.Count > 1 ? parts[1].Replace("\\n", "\n") : key;
                    string vi = parts.Count > 2 ? parts[2].Replace("\\n", "\n") : en;
                    string ko = parts.Count > 3 ? parts[3].Replace("\\n", "\n") : en;

                    _table[LANG_EN][key] = en;
                    _table[LANG_VI][key] = vi;
                    _table[LANG_KO][key] = ko;
                }
            }
            Debug.Log($"[LocalizationManager] Loaded {_table[LANG_EN].Count} localization keys successfully.");
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        public void SetLanguage(string langCode, bool saveToPrefs = true)
        {
            if (langCode != LANG_EN && langCode != LANG_VI && langCode != LANG_KO)
            {
                langCode = LANG_EN;
            }

            CurrentLanguage = langCode;
            if (saveToPrefs)
            {
                PlayerPrefs.SetString(PREF_KEY, CurrentLanguage);
                PlayerPrefs.Save();
            }

            Debug.Log($"[LocalizationManager] Language switched to: {CurrentLanguage.ToUpper()}");
            OnLanguageChanged?.Invoke();
        }

        public string Get(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

            if (_table.TryGetValue(CurrentLanguage, out var langDict) && langDict.TryGetValue(key, out var val))
            {
                if (!string.IsNullOrEmpty(val)) return val;
            }

            if (_table.TryGetValue(LANG_EN, out var enDict) && enDict.TryGetValue(key, out var enVal))
            {
                if (!string.IsNullOrEmpty(enVal)) return enVal;
            }

            return fallback ?? key;
        }

        public string Format(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationManager] Format failed for key '{key}': {ex.Message}");
                return template;
            }
        }
    }
}

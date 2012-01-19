﻿// Copyright (c) 2012 Blue Onion Software, All rights reserved
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BlueOnionSoftware
{
    public class Settings
    {
        private const string RegExPatternsKey = "RegExPatterns";
        private const string StopOnBuildErrorKey = "StopOnBuildError";
        private const string RegistryPath = @"DialogPage\BlueOnionSoftware.VsColorOutputOptions";
        public static IRegistryKey OverrideRegistryKey { get; set; }

        public RegExClassification[] Patterns { get; set; }
        public bool EnableStopOnBuildError { get; set; }

        public void Load()
        {
            using (var key = OpenRegistry(false))
            {
                var json = (key != null) ? key.GetValue(RegExPatternsKey) as string : null;
                Patterns = (string.IsNullOrEmpty(json) || json == "[]") ? DefaultPatterns() : LoadPatternsFromJson(json);
                var value = key.GetValue(StopOnBuildErrorKey) as string;
                EnableStopOnBuildError = string.IsNullOrEmpty(value) == false && value == bool.TrueString;
            }
        }

        public void Save()
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(RegExClassification[]));
                serializer.WriteObject(ms, Patterns);
                var json = Encoding.Default.GetString(ms.ToArray());
                using (var key = OpenRegistry(true))
                {
                    key.SetValue(RegExPatternsKey, json);
                    key.SetValue(StopOnBuildErrorKey, EnableStopOnBuildError.ToString());
                }
                if (OutputClassifierProvider.OutputClassifier != null)
                {
                    OutputClassifierProvider.OutputClassifier.ClearSettings();
                }
            }
        }

        private static IRegistryKey OpenRegistry(bool writeable)
        {
            if (OverrideRegistryKey != null)
            {
                return OverrideRegistryKey;
            }
            var root = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_UserSettings, writeable);
            return new RegistryKeyImpl(root.OpenSubKey(RegistryPath, writeable));
        }

        private static RegExClassification[] DefaultPatterns()
        {
            return new[]
            {
                new RegExClassification {RegExPattern = @"\+\+\+\>", ClassificationType = ClassificationTypes.LogCustom1, IgnoreCase = false},
                new RegExClassification {RegExPattern = @"(=====|-----)", ClassificationType = ClassificationTypes.BuildHead, IgnoreCase = false},
                new RegExClassification {RegExPattern = @"0 failed", ClassificationType = ClassificationTypes.BuildHead, IgnoreCase = true},
                new RegExClassification {RegExPattern = @"(\W|^)(error|fail|failed|exception)\W", ClassificationType = ClassificationTypes.LogError, IgnoreCase = true},
                new RegExClassification {RegExPattern = @"(exception:|stack trace:)", ClassificationType = ClassificationTypes.LogError, IgnoreCase = true},
                new RegExClassification {RegExPattern = @"^\s+at\s", ClassificationType = ClassificationTypes.LogError, IgnoreCase = true},
                new RegExClassification {RegExPattern = @"(\W|^)warning\W", ClassificationType = ClassificationTypes.LogWarning, IgnoreCase = true},
                new RegExClassification {RegExPattern = @"(\W|^)information\W", ClassificationType = ClassificationTypes.LogInformation, IgnoreCase = true}
            };
        }

        private static RegExClassification[] LoadPatternsFromJson(string json)
        {
            try
            {
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(RegExClassification[]));
                    var patterns = serializer.ReadObject(ms) as RegExClassification[];
                    return patterns ?? DefaultPatterns();
                }
            }
            catch (Exception)
            {
                return DefaultPatterns();
            }
        }
    }
}
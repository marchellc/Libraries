using Common.Extensions;
using Common.IO.Collections;
using Common.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Http
{
    public class HttpAuthentificator
    {
        private readonly LockedDictionary<string, string[]> httpKeys = new LockedDictionary<string, string[]>();

        public string OverridePermId = "ovv";
        public int KeyLength = 64;

        public event Action<string, string[]> OnKeyGenerated;

        public event Action<string, string[]> OnKeyPermissionsRemoved;
        public event Action<string, string[]> OnKeyPermissionsAdded;

        public event Action<string> OnKeyRemoved;

        public Dictionary<string, string[]> GetKeys()
            => new Dictionary<string, string[]>(httpKeys);

        public bool Remove(string keyId)
        {
            if (!httpKeys.Remove(keyId))
                return false;

            OnKeyRemoved.Call(keyId);
            return true;
        }

        public bool RemovePermissions(string keyId, params string[] permIds)
        {
            if (!httpKeys.TryGetValue(keyId, out var keyPerms))
                return false;

            var curLength = keyPerms.Length;

            keyPerms = keyPerms.Where(p => !permIds.Contains(p)).ToArray();

            httpKeys[keyId] = keyPerms;

            if (curLength != keyPerms.Length)
            {
                OnKeyPermissionsRemoved.Call(keyId, permIds);
                return true;
            }

            return false;
        }

        public bool AddPermissions(string keyId, params string[] permIds)
        {
            if (!httpKeys.TryGetValue(keyId, out var keyPerms))
                return false;

            var curLength = keyPerms.Length;

            keyPerms = keyPerms.Concat(permIds.Where(p => !keyPerms.Contains(p))).ToArray();

            httpKeys[keyId] = keyPerms;

            if (curLength != keyPerms.Length)
            {
                OnKeyPermissionsAdded.Call(keyId, permIds);
                return true;
            }

            return false;
        }

        public string GenerateNew(params string[] perms)
        {
            var keyId = Generator.Instance.GetString(KeyLength, true);

            while (httpKeys.ContainsKey(keyId))
                keyId = Generator.Instance.GetString(KeyLength, true);

            httpKeys[keyId] = perms;
            OnKeyGenerated.Call(keyId, perms);
            return keyId;
        }

        public void LoadJson(string fileContent)
            => httpKeys.AddRange(fileContent.Deserialize<Dictionary<string, string[]>>());

        public string SaveJson()
            => GetKeys().Serialize();

        public HttpAuthentificationResult Authentificate(string keyId, string permId)
        {
            if (string.IsNullOrWhiteSpace(keyId) || keyId.Length != KeyLength)
                return HttpAuthentificationResult.InvalidKey;

            if (!httpKeys.TryGetValue(keyId, out var keyPerms))
                return HttpAuthentificationResult.InvalidKey;

            if (string.IsNullOrWhiteSpace(permId) || keyPerms.Contains(permId) || (!string.IsNullOrWhiteSpace(OverridePermId) && keyPerms.Contains(OverridePermId)))
                return HttpAuthentificationResult.Authorized;

            if (keyPerms.Length == 0)
                return HttpAuthentificationResult.Unauthorized;

            if (permId.Contains("."))
            {
                var permSplits = permId.Split('.');

                for (int i = 0; i < permSplits.Length; i++)
                {
                    if (permId.Contains($"*.{permSplits[i]}")
                        || permId.Contains($"{permSplits[i]}.*"))
                        return HttpAuthentificationResult.Authorized;
                }
            }

            return HttpAuthentificationResult.Unauthorized;
        }

        public void ClearKeys()
            => httpKeys.Clear();
    }
}
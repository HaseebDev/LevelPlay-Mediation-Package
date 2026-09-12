#if LEVELPLAY_INSTALLED
using System;
using UnityEngine;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Manages persistence of RemoveAds status with encryption support
    /// </summary>
    public class AdPersistenceManager
    {
        private const string FallbackDeviceIdPrefKey = "Autech.LevelPlay.RemoveAds.DeviceGuid";

        private bool useEncryptedStorage = true;
        private string removeAdsKey = "RemoveAds_Status";
        private string encryptionKey;
        private string legacyXorKey;

        public AdPersistenceManager()
        {
            // Generate device-unique encryption key on first use
            encryptionKey = GenerateDeviceUniqueKey();
            legacyXorKey = encryptionKey;
        }

        public bool UseEncryptedStorage
        {
            get => useEncryptedStorage;
            set => useEncryptedStorage = value;
        }

        public string RemoveAdsKey
        {
            get => removeAdsKey;
            set => removeAdsKey = value;
        }

        /// <summary>
        /// Optional legacy XOR key for migrating data from versions prior to AES encryption.
        /// Supply this before calling MigrateLegacyEncryption() if the legacy key differs from
        /// the current encryption key.
        /// </summary>
        public string LegacyXorKey
        {
            get => legacyXorKey;
            set => legacyXorKey = value;
        }

        /// <summary>
        /// Set custom encryption key for data protection.
        ///
        /// ⚠️ CRITICAL DATA LOSS WARNING:
        /// - Changing this key after data has been saved will make that data UNRECOVERABLE
        /// - Users will LOSE their RemoveAds purchase status if the key changes
        /// - Store this key SECURELY and CONSISTENTLY across app versions
        /// - Never change this key in production unless you handle data migration
        /// - Recommended: Use a consistent, hardcoded key specific to your app
        ///
        /// SECURITY REQUIREMENTS:
        /// - Minimum 32 characters recommended for AES-256 security
        /// - Use a unique key per application
        /// - Never expose this key in public repositories
        ///
        /// Example: "YourAppName_SecureKey_2024_DoNotChange_v1"
        /// </summary>
        /// <param name="customKey">The encryption key (minimum 8 characters, 32+ recommended)</param>
        public void SetEncryptionKey(string customKey)
        {
            if (string.IsNullOrWhiteSpace(customKey))
            {
                AdLog.Error("[AdPersistenceManager] Cannot set empty encryption key");
                return;
            }

            if (customKey.Length < 32)
            {
                AdLog.Warn("[AdPersistenceManager] Encryption key should be at least 32 characters for AES-256 security");
                AdLog.Warn("[AdPersistenceManager] Current length: " + customKey.Length);
            }

            // Check if key is being changed after initialization (potential data loss)
            string previousKey = encryptionKey;
            bool isKeyChange = !string.IsNullOrEmpty(previousKey) && previousKey != customKey;
            bool hasEncryptedData = useEncryptedStorage && SecureStorage.HasEncryptedData(removeAdsKey);

            if (isKeyChange)
            {
                if (hasEncryptedData)
                {
                    AdLog.Error("========================================");
                    AdLog.Error("[AdPersistenceManager] ⚠️ ENCRYPTION KEY CHANGED ⚠️");
                    AdLog.Error("[AdPersistenceManager] Previously encrypted data will be UNREADABLE!");
                    AdLog.Error("[AdPersistenceManager] Users will LOSE their RemoveAds status!");
                    AdLog.Error("[AdPersistenceManager] Ensure this is intentional and handle data migration!");
                    AdLog.Error("========================================");
                }
                else
                {
                    AdLog.Warn("[AdPersistenceManager] Encryption key changed before encrypted data was stored. Set LegacyXorKey if migrating older data.");
                }
            }

            encryptionKey = customKey;
            if (isKeyChange && (string.IsNullOrEmpty(legacyXorKey) || legacyXorKey == previousKey))
            {
                // Preserve previous key so developers can migrate legacy data if required
                legacyXorKey = previousKey;
            }
            AdLog.Info("[AdPersistenceManager] Custom encryption key set");
        }

        /// <summary>
        /// Generates a device-unique encryption key for data protection.
        ///
        /// PRIVACY COMPLIANCE NOTICE:
        /// - Uses SystemInfo.deviceUniqueIdentifier (deprecated but functional)
        /// - Used ONLY for local encryption key generation, NOT sent to servers
        /// - Data never leaves the device and is not used for tracking
        /// - Complies with GDPR/CCPA as data is not shared or transmitted
        /// - For stricter privacy requirements, use SetEncryptionKey() with a custom key
        ///
        /// DEVELOPER NOTE:
        /// - Consider reviewing your app's privacy policy
        /// - Ensure compliance with platform-specific privacy guidelines (iOS App Tracking, Android permissions)
        /// - This identifier is used solely for encrypting local RemoveAds status
        /// </summary>
        private string GenerateDeviceUniqueKey()
        {
            // Generate a unique key based on device identifiers
            // PRIVACY: This identifier is used ONLY for local encryption, never transmitted
            string deviceId;
            try
            {
#pragma warning disable CS0618 // SystemInfo.deviceUniqueIdentifier is deprecated but still functional
                deviceId = SystemInfo.deviceUniqueIdentifier;
#pragma warning restore CS0618

                // Fallback if deviceId is empty or unavailable
                if (string.IsNullOrEmpty(deviceId))
                {
                    AdLog.Warn("[AdPersistenceManager] deviceUniqueIdentifier not available, using privacy-safe fallback");
                    deviceId = GenerateFallbackDeviceId();
                }
            }
            catch (System.Exception ex)
            {
                AdLog.Warn($"[AdPersistenceManager] Failed to get deviceUniqueIdentifier: {ex.Message}. Using privacy-safe fallback.");
                deviceId = GenerateFallbackDeviceId();
            }

            string appId = Application.identifier;
            return $"{appId}_{deviceId}_RemoveAdsEncryption_v2";
        }

        private string GenerateFallbackDeviceId()
        {
            // Persist a GUID so OS updates do not invalidate the encryption key
            string storedGuid = PlayerPrefs.GetString(FallbackDeviceIdPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(storedGuid))
            {
                return storedGuid;
            }

            string generatedGuid = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(FallbackDeviceIdPrefKey, generatedGuid);
            PlayerPrefs.Save();

            AdLog.Warn("[AdPersistenceManager] Generated persistent fallback device identifier. Ensure Remove Ads purchases can be restored via your IAP flow.");
            return generatedGuid;
        }

        public event Action<bool> OnRemoveAdsLoadedFromStorage;

        public bool LoadRemoveAdsStatus()
        {
            bool savedValue = false;

            try
            {
                if (useEncryptedStorage)
                {
                    savedValue = SecureStorage.LoadEncryptedBool(removeAdsKey, false, encryptionKey);
                }
                else
                {
                    savedValue = PlayerPrefs.GetInt(removeAdsKey, 0) == 1;
                    // Note: PlayerPrefs.Save() not needed for Load operations
                }

                OnRemoveAdsLoadedFromStorage?.Invoke(savedValue);
                AdLog.Info($"[AdPersistenceManager] Loaded Remove Ads status: {savedValue}");
            }
            catch (System.Exception ex)
            {
                AdLog.Error($"[AdPersistenceManager] Failed to load Remove Ads status: {ex.Message}");
                AdLog.Exception(ex);
                savedValue = false; // Safe default - don't give free RemoveAds on error
                OnRemoveAdsLoadedFromStorage?.Invoke(false);
            }

            return savedValue;
        }

        public void SaveRemoveAdsStatus(bool value)
        {
            try
            {
                if (useEncryptedStorage)
                {
                    bool saveSuccess = SecureStorage.SaveEncryptedBool(removeAdsKey, value, encryptionKey);
                    if (!saveSuccess)
                    {
                        throw new InvalidOperationException("[AdPersistenceManager] Failed to save encrypted Remove Ads status");
                    }
                }
                else
                {
                    PlayerPrefs.SetInt(removeAdsKey, value ? 1 : 0);
                    PlayerPrefs.Save();
                }

                AdLog.Info($"[AdPersistenceManager] Saved Remove Ads status: {value}");
            }
            catch (System.Exception ex)
            {
                AdLog.Error($"[AdPersistenceManager] Failed to save Remove Ads status: {ex.Message}");
                AdLog.Exception(ex);
                throw; // Re-throw so caller knows save failed (important for IAP)
            }
        }

        public void ClearRemoveAdsData()
        {
            try
            {
                if (useEncryptedStorage)
                {
                    SecureStorage.DeleteEncryptedData(removeAdsKey);
                }
                else
                {
                    PlayerPrefs.DeleteKey(removeAdsKey);
                    PlayerPrefs.Save();
                }

                AdLog.Info("[AdPersistenceManager] Remove Ads data cleared");
            }
            catch (System.Exception ex)
            {
                AdLog.Error($"[AdPersistenceManager] Failed to clear Remove Ads data: {ex.Message}");
                AdLog.Exception(ex);
            }
        }

        public bool HasRemoveAdsDataInStorage()
        {
            try
            {
                if (useEncryptedStorage)
                {
                    return SecureStorage.HasEncryptedData(removeAdsKey);
                }
                else
                {
                    return PlayerPrefs.HasKey(removeAdsKey);
                }
            }
            catch (System.Exception ex)
            {
                AdLog.Error($"[AdPersistenceManager] Failed to check Remove Ads data: {ex.Message}");
                AdLog.Exception(ex);
                return false; // Safe default
            }
        }

        public bool MigrateLegacyEncryption()
        {
            if (!useEncryptedStorage)
            {
                AdLog.Info("[AdPersistenceManager] Encryption not enabled - migration not needed");
                return true;
            }

            if (!SecureStorage.HasLegacyData(removeAdsKey))
            {
                AdLog.Info("[AdPersistenceManager] No legacy data found");
                return true;
            }

            AdLog.Warn("[AdPersistenceManager] Migrating legacy data...");

            string xorKeyToUse = !string.IsNullOrEmpty(legacyXorKey) ? legacyXorKey : encryptionKey;
            if (string.IsNullOrEmpty(xorKeyToUse))
            {
                AdLog.Error("[AdPersistenceManager] Migration failed: Legacy XOR key not provided.");
                return false;
            }

            bool success = SecureStorage.MigrateLegacyData(removeAdsKey, xorKeyToUse, encryptionKey);

            if (success)
            {
                AdLog.Warn("[AdPersistenceManager] Migration completed successfully");
            }
            else
            {
                AdLog.Error("[AdPersistenceManager] Migration failed");
            }

            return success;
        }

        public bool NeedsLegacyMigration()
        {
            return useEncryptedStorage && SecureStorage.HasLegacyData(removeAdsKey);
        }

        public void LogEncryptionInfo()
        {
            AdLog.Info("=== [AdPersistenceManager] ENCRYPTION INFO ===");
            AdLog.Info($"Encrypted Storage Enabled: {useEncryptedStorage}");
            AdLog.Info($"Has Stored Data: {HasRemoveAdsDataInStorage()}");

            if (useEncryptedStorage)
            {
                AdLog.Info($"Encryption Method: AES-256-CBC");
                AdLog.Info($"{SecureStorage.GetEncryptionInfo()}");
                AdLog.Info("Using device-unique encryption key");
            }
            else
            {
                AdLog.Info("Encryption Method: None");
                AdLog.Warn("WARNING: Data is not encrypted!");
            }

            AdLog.Info("=======================================");
        }
    }
}
#endif // LEVELPLAY_INSTALLED

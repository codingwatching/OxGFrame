﻿using MyBox;
using OxGKit.LoggingSystem;
using System.Collections.Generic;
using UnityEngine;

namespace OxGFrame.AssetLoader.Bundle
{
    [DisallowMultipleComponent]
    internal class PatchLauncher : MonoBehaviour
    {
        [Separator("Patch Options")]
        public BundleConfig.PlayMode playMode = BundleConfig.PlayMode.EditorSimulateMode;
        [Tooltip("If checked, the patch field will compare whole version."), ConditionalField(nameof(playMode), false, BundleConfig.PlayMode.HostMode, BundleConfig.PlayMode.WebGLRemoteMode)]
        public BundleConfig.SemanticRule semanticRule = new BundleConfig.SemanticRule();
        [Tooltip("If checked, will skip preset packages download step of the patch (force download while playing)."), ConditionalField(nameof(playMode), false, BundleConfig.PlayMode.HostMode)]
        public bool skipMainDownload = false;
        [Tooltip("If checked, will check disk space is it enough while patch checking."), ConditionalField(new string[] { nameof(playMode), nameof(skipMainDownload) }, new bool[] { false, true }, BundleConfig.PlayMode.HostMode)]
        public bool checkDiskSpace = true;

        [Separator("Preset App Packages")]
        [Tooltip("The first element will be default app package.\n\nNote: The presets will combine in main download of the patch.")]
        public List<AppPackageInfoWithBuild> listAppPackages = new List<AppPackageInfoWithBuild>() { new AppPackageInfoWithBuild() { packageName = "DefaultPackage" } };

        [Separator("Preset DLC Packages"), Tooltip("The preset DLC packages must be fixed versions.\n\nNote: The presets will combine in main download of the patch.")]
        public List<DlcPackageInfoWithBuild> listDlcPackages = new List<DlcPackageInfoWithBuild>();

        [Separator("Download Options")]
        public int maxConcurrencyDownloadCount = BundleConfig.maxConcurrencyDownloadCount;
        public int failedRetryCount = BundleConfig.failedRetryCount;
        [Tooltip("If file size >= [BreakpointFileSizeThreshold] that file will enable breakpoint mechanism (for all downloaders).")]
        public uint breakpointFileSizeThreshold = BundleConfig.breakpointFileSizeThreshold;

        [Separator("Cryptogram Options")]
        [SerializeField] private DecryptInfo _decryptInfo = new DecryptInfo();

        private async void Awake()
        {
            string newName = $"[{nameof(PatchLauncher)}]";
            this.gameObject.name = newName;
            if (this.gameObject.transform.root.name == newName)
            {
                var container = GameObject.Find(nameof(OxGFrame));
                if (container == null)
                    container = new GameObject(nameof(OxGFrame));
                this.gameObject.transform.SetParent(container.transform);
                DontDestroyOnLoad(container);
            }
            else
                DontDestroyOnLoad(this.gameObject.transform.root);

            #region Patch Options
#if !UNITY_EDITOR && OXGFRAME_OFFLINE_MODE
            this.playMode = BundleConfig.PlayMode.OfflineMode;
#elif !UNITY_EDITOR && OXGFRAME_HOST_MODE
            this.playMode = BundleConfig.PlayMode.HostMode;
#elif !UNITY_EDITOR && OXGFRAME_WEBGL_MODE
            this.playMode = BundleConfig.PlayMode.WebGLMode;
#elif !UNITY_EDITOR && OXGFRAME_WEBGL_REMOTE_MODE
            this.playMode = BundleConfig.PlayMode.WebGLRemoteMode;
#endif
            BundleConfig.playMode = this.playMode;
            // For Host Mode
            if (this.playMode == BundleConfig.PlayMode.HostMode)
            {
                BundleConfig.semanticRule = this.semanticRule;
                BundleConfig.skipMainDownload = this.skipMainDownload;
                BundleConfig.checkDiskSpace = this.checkDiskSpace;
            }
            // For WebGL Mode
            else if (this.playMode == BundleConfig.PlayMode.WebGLMode ||
                     BundleConfig.playMode == BundleConfig.PlayMode.WebGLRemoteMode)
            {
                BundleConfig.semanticRule = this.semanticRule;
            }
            #endregion

            #region Package List
            BundleConfig.listAppPackages = this.listAppPackages;
            BundleConfig.listDlcPackages = this.listDlcPackages;
            #endregion

            #region Download Options
            BundleConfig.maxConcurrencyDownloadCount = this.maxConcurrencyDownloadCount <= 0 ? BundleConfig.DEFAULT_MAX_CONCURRENCY_MAX_DOWNLOAD_COUNT : this.maxConcurrencyDownloadCount;
            BundleConfig.failedRetryCount = this.failedRetryCount <= 0 ? BundleConfig.DEFAULT_FAILED_RETRY_COUNT : this.failedRetryCount;
            // Set download breakpoint size threshold
            BundleConfig.breakpointFileSizeThreshold = this.breakpointFileSizeThreshold;
            #endregion

            #region Cryptogram Options
            BundleConfig.InitDecryptInfo(this._decryptInfo.GetDecryptArgs(), this._decryptInfo.scuredStringType, this._decryptInfo.GetSaltSize(), this._decryptInfo.GetDummySize());
            this._decryptInfo.Dispose();
            #endregion

            // Init Settings and Setup Preset App Packages
            await PackageManager.InitSetup();

            if (PackageManager.isInitialized)
            {
                Logging.Print<Logger>($"<color=#32ff94>(Powered by YooAsset) Initialized Play Mode: {BundleConfig.playMode}</color>");
                Logging.Print<Logger>("<color=#b5ff00>(Powered by YooAsset) PatchLauncher Setup Completes.</color>");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
#if UNITY_WEBGL
            switch (this.playMode)
            {
                case BundleConfig.PlayMode.OfflineMode:
                    this.playMode = BundleConfig.PlayMode.EditorSimulateMode;
                    Debug.Log($"<color=#ff1f4c>[Offline Mode] is not supported on {UnityEditor.EditorUserBuildSettings.activeBuildTarget}.</color>");
                    break;
                case BundleConfig.PlayMode.HostMode:
                    this.playMode = BundleConfig.PlayMode.EditorSimulateMode;
                    Debug.Log($"<color=#ff1f4c>[Host Mode] is not supported on {UnityEditor.EditorUserBuildSettings.activeBuildTarget}.</color>");
                    break;
            }
#else
            switch (this.playMode)
            {
                case BundleConfig.PlayMode.WebGLMode:
                case BundleConfig.PlayMode.WebGLRemoteMode:
                    this.playMode = BundleConfig.PlayMode.EditorSimulateMode;
                    Debug.Log($"<color=#ff1f4c>[WebGL Mode] is not supported on {UnityEditor.EditorUserBuildSettings.activeBuildTarget}.</color>");
                    break;
            }
#endif
        }
#endif
    }
}
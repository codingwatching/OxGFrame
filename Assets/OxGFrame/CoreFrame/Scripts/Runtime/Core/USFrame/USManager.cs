﻿using Cysharp.Threading.Tasks;
using OxGFrame.AssetLoader;
using OxGFrame.AssetLoader.Cacher;
using OxGKit.LoggingSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OxGFrame.CoreFrame.USFrame
{
    public struct AdditiveSceneInfo
    {
        public string sceneName;
        public bool activeRootGameObjects;
    }

    internal class USManager
    {
        public static int sceneCount { get { return SceneManager.sceneCount; } }

        private float _currentCount;
        private float _totalCount;

        private static readonly object _locker = new object();
        private static USManager _instance = null;
        public static USManager GetInstance()
        {
            if (_instance == null)
            {
                lock (_locker)
                {
                    _instance = new USManager();
                }
            }
            return _instance;
        }

        public Scene GetSceneAt(int index)
        {
            return SceneManager.GetSceneAt(index);
        }

        public Scene GetSceneByName(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName);
        }

        public Scene GetSceneByBuildIndex(int buildIndex)
        {
            return SceneManager.GetSceneByBuildIndex(buildIndex);
        }

        public Scene[] GetAllScenes(params string[] sceneNames)
        {
            List<Scene> scenes = new List<Scene>();
            if (sceneNames.Length > 0)
            {
                for (int i = 0; i < sceneCount; i++)
                {
                    foreach (string sceneName in sceneNames)
                    {
                        if (sceneName == this.GetSceneAt(i).name)
                        {
                            scenes.Add(this.GetSceneAt(i));
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < sceneCount; i++)
                {
                    scenes.Add(this.GetSceneAt(i));
                }
            }

            return scenes.ToArray();
        }

        public Scene[] GetAllScenes(params int[] buildIndexes)
        {
            List<Scene> scenes = new List<Scene>();
            if (buildIndexes.Length > 0)
            {
                for (int i = 0; i < sceneCount; i++)
                {
                    foreach (int buildIndex in buildIndexes)
                    {
                        if (buildIndex == this.GetSceneAt(i).buildIndex)
                        {
                            scenes.Add(this.GetSceneAt(i));
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < sceneCount; i++)
                {
                    scenes.Add(this.GetSceneAt(i));
                }
            }

            return scenes.ToArray();
        }

        public void SetActiveSceneRootGameObjects(string sceneName, bool active, string[] withoutRootGameObjectNames = null)
        {
            Scene[] filterScenes = this.GetAllScenes(sceneName);
            foreach (var scene in filterScenes)
            {
                this.SetActiveSceneRootGameObjects(scene, active, withoutRootGameObjectNames);
            }
        }

        public void SetActiveSceneRootGameObjects(Scene scene, bool active, string[] withoutRootGameObjectNames = null)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                if (scene.isLoaded)
                {
                    foreach (var go in scene.GetRootGameObjects())
                    {
                        if (withoutRootGameObjectNames != null && withoutRootGameObjectNames.Length > 0)
                        {
                            bool without = false;
                            for (int i = 0; i < withoutRootGameObjectNames.Length; i++)
                            {
                                if (go.name == withoutRootGameObjectNames[i])
                                {
                                    without = true;
                                    break;
                                }
                            }
                            if (!without) go.SetActive(active);
                        }
                        else go.SetActive(active);
                    }
                }
                else Logging.Print<Logger>($"<color=#ff8233>Set active objects of the scene failed!!! Scene Name: {scene.name}. The scene is loding...</color>");
            }
            else Logging.Print<Logger>($"<color=#ff33ae>Set active objects of the scene failed!!! Scene Name: {scene.name}. The scene not is valid!!!</color>");
        }

        #region Bundle
        public async UniTask<BundlePack> LoadFromBundleAsync(string packageName, string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, bool activateOnLoad = true, uint priority = 100, Progression progression = null)
        {
            var scene = this.GetSceneByName(sceneName);
            if (!string.IsNullOrEmpty(scene.name) &&
                scene.isLoaded &&
                loadSceneMode == LoadSceneMode.Single)
            {
                Logging.PrintWarning<Logger>($"【US】Single Scene => {sceneName} already exists!!!");
                return null;
            }

            var pack = await AssetLoaders.LoadSceneAsync(packageName, sceneName, loadSceneMode, activateOnLoad, priority, progression);
            if (pack != null)
            {
                Logging.Print<Logger>($"<color=#4affc2>Load Scene From <color=#ffc04a>Bundle</color> => sceneName: {sceneName}, mode: {loadSceneMode}</color>");
                return pack;
            }

            return null;
        }

        public BundlePack LoadFromBundle(string packageName, string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Progression progression = null)
        {
            var scene = this.GetSceneByName(sceneName);
            if (!string.IsNullOrEmpty(scene.name) &&
                scene.isLoaded &&
                loadSceneMode == LoadSceneMode.Single)
            {
                Logging.PrintWarning<Logger>($"【US】Single Scene => {sceneName} already exists!!!");
                return null;
            }

            var pack = AssetLoaders.LoadScene(packageName, sceneName, loadSceneMode, progression);
            if (pack != null)
            {
                Logging.Print<Logger>($"<color=#4affc2>Load Scene From <color=#ffc04a>Bundle</color> => sceneName: {sceneName}, mode: {loadSceneMode}</color>");
                return pack;
            }

            return null;
        }

        public void UnloadFromBundle(bool recursively, params string[] sceneNames)
        {
            if (sceneNames != null && sceneNames.Length > 0)
            {
                foreach (string sceneName in sceneNames)
                {
                    AssetLoaders.UnloadScene(sceneName, recursively);
                }
            }
        }
        #endregion

        #region Build
        public async UniTask<AsyncOperation> LoadFromBuildAsync(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Progression progression = null)
        {
            this._currentCount = 0;
            this._totalCount = 1; // 初始 1 = 必有一場景

            var scene = this.GetSceneByName(sceneName);
            if (!string.IsNullOrEmpty(scene.name) &&
                scene.isLoaded &&
                loadSceneMode == LoadSceneMode.Single)
            {
                Logging.PrintWarning<Logger>($"【US】Single Scene => {sceneName} already exists!!!");
                return null;
            }

            var req = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (req != null)
            {
                req.allowSceneActivation = false;

                float lastCount = 0;
                while (!req.isDone)
                {
                    if (progression != null)
                    {
                        this._currentCount += (req.progress - lastCount);
                        lastCount = req.progress;
                        if (this._currentCount >= 0.9f) this._currentCount = 1f;
                        progression.Invoke(this._currentCount / this._totalCount, this._currentCount, this._totalCount);
                    }
                    if (req.progress >= 0.9f) req.allowSceneActivation = true;
                    await UniTask.Yield();
                }

                Logging.Print<Logger>($"<color=#4affc2>Load Scene From <color=#ffc04a>Build</color> => sceneName: {sceneName}, mode: {loadSceneMode}</color>");
            }

            return req;
        }

        public Scene LoadFromBuild(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Progression progression = null)
        {
            this._currentCount = 0;
            this._totalCount = 1; // 初始 1 = 必有一場景

            var scene = this.GetSceneByName(sceneName);
            if (!string.IsNullOrEmpty(scene.name) &&
                scene.isLoaded &&
                loadSceneMode == LoadSceneMode.Single)
            {
                Logging.PrintWarning<Logger>($"【US】Single Scene => {sceneName} already exists!!!");
                return default;
            }

            scene = SceneManager.LoadScene(sceneName, new LoadSceneParameters(loadSceneMode));
            if (progression != null)
            {
                this._currentCount++;
                progression.Invoke(this._currentCount / this._totalCount, this._currentCount, this._totalCount);
            }
            Logging.Print<Logger>($"<color=#4affc2>Load Scene From <color=#ffc04a>Build</color> => sceneName: {sceneName}, mode: {loadSceneMode}</color>");
            // (Caution) If use sync to load scene.isLoaded return false -> Why??
            return scene;
        }

        public async UniTask<AsyncOperation> LoadFromBuildAsync(int buildIndex, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Progression progression = null)
        {
            this._currentCount = 0;
            this._totalCount = 1; // 初始 1 = 必有一場景

            var scene = this.GetSceneByBuildIndex(buildIndex);
            string sceneName = scene.name;
            if (!string.IsNullOrEmpty(sceneName) && scene.isLoaded && loadSceneMode == LoadSceneMode.Single)
            {
                Logging.PrintWarning<Logger>($"【US】Single Scene => {sceneName} already exists!!!");
                return null;
            }

            var req = SceneManager.LoadSceneAsync(buildIndex, loadSceneMode);
            if (req != null)
            {
                req.allowSceneActivation = false;

                float lastCount = 0;
                while (!req.isDone)
                {
                    if (progression != null)
                    {
                        this._currentCount += (req.progress - lastCount);
                        lastCount = req.progress;
                        if (this._currentCount >= 0.9f) this._currentCount = 1f;
                        progression.Invoke(this._currentCount / this._totalCount, this._currentCount, this._totalCount);
                    }
                    if (req.progress >= 0.9f) req.allowSceneActivation = true;
                    await UniTask.Yield();
                }

                Logging.Print<Logger>($"<color=#4affc2>Load Scene From <color=#ffc04a>Build</color> => idx: {buildIndex}, mode: {loadSceneMode}</color>");
            }

            return req;
        }

        public Scene LoadFromBuild(int buildIndex, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Progression progression = null)
        {
            this._currentCount = 0;
            this._totalCount = 1; // 初始 1 = 必有一場景

            var scene = this.GetSceneByBuildIndex(buildIndex);
            string sceneName = scene.name;
            if (!string.IsNullOrEmpty(sceneName) &&
                scene.isLoaded &&
                loadSceneMode == LoadSceneMode.Single)
            {
                Logging.PrintWarning<Logger>($"【US】Single Scene => {sceneName} already exists!!!");
                return default;
            }

            scene = SceneManager.LoadScene(sceneName, new LoadSceneParameters(loadSceneMode));
            if (progression != null)
            {
                this._currentCount++;
                progression.Invoke(this._currentCount / this._totalCount, this._currentCount, this._totalCount);
            }
            Logging.Print<Logger>($"<color=#4affc2>Load Scene From <color=#ffc04a>Build</color> => idx: {buildIndex}, mode: {loadSceneMode}</color>");
            // (Caution) If use sync to load scene.isLoaded return false -> Why??
            return scene;
        }

        public void UnloadFromBuild(bool recursively, params string[] sceneNames)
        {
            if (sceneCount == 1)
            {
                Logging.PrintWarning<Logger>("Cannot unload last scene!!!");
                return;
            }

            if (sceneNames != null && sceneNames.Length > 0)
            {
                foreach (string sceneName in sceneNames)
                {
                    if (recursively)
                    {
                        for (int i = 0; i < sceneCount; i++)
                        {
                            if (sceneName == this.GetSceneAt(i).name)
                            {
                                SceneManager.UnloadSceneAsync(this.GetSceneAt(i));
                                Logging.Print<Logger>($"<color=#ff4ae0>Unload Scene <color=#ffc04a>(Build)</color> => sceneName: {sceneName}</color>");
                            }
                        }
                    }
                    else
                    {
                        for (int i = sceneCount - 1; i >= 0; --i)
                        {
                            if (sceneName == this.GetSceneAt(i).name)
                            {
                                SceneManager.UnloadSceneAsync(this.GetSceneAt(i));
                                Logging.Print<Logger>($"<color=#ff4ae0>Unload Scene <color=#ffc04a>(Build)</color> => sceneName: {sceneName}</color>");
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void UnloadFromBuild(bool recursively, params int[] buildIndexes)
        {
            if (sceneCount == 1)
            {
                Logging.PrintWarning<Logger>("Cannot unload last scene!!!");
                return;
            }

            if (buildIndexes.Length > 0)
            {
                if (recursively)
                {
                    foreach (int buildIndex in buildIndexes)
                    {
                        for (int i = 0; i < sceneCount; i++)
                        {
                            if (buildIndex == this.GetSceneAt(i).buildIndex)
                            {
                                SceneManager.UnloadSceneAsync(this.GetSceneAt(i));
                                Logging.Print<Logger>($"<color=#ff4ae0>Unload Scene <color=#ffc04a>(Build)</color> => sceneName: {this.GetSceneAt(i).name}, buildIdx: {this.GetSceneAt(i).buildIndex}</color>");
                            }
                        }
                    }
                }
                else
                {
                    foreach (int buildIndex in buildIndexes)
                    {
                        for (int i = sceneCount - 1; i >= 0; --i)
                        {
                            if (buildIndex == this.GetSceneAt(i).buildIndex)
                            {
                                SceneManager.UnloadSceneAsync(this.GetSceneAt(i));
                                Logging.Print<Logger>($"<color=#ff4ae0>Unload Scene <color=#ffc04a>(Build)</color> => sceneName: {this.GetSceneAt(i).name}, buildIdx: {this.GetSceneAt(i).buildIndex}</color>");
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
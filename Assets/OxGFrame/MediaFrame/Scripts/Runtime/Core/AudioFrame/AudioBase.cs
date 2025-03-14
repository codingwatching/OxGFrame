﻿using Cysharp.Threading.Tasks;
using MyBox;
using OxGKit.LoggingSystem;
using UnityEngine;
using UnityEngine.Audio;

namespace OxGFrame.MediaFrame.AudioFrame
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioBase : MediaBase
    {
        protected AudioSource _audioSource = null;

        public AudioType audioType = new AudioType();
        public UnityEngine.AudioType audioFileType = UnityEngine.AudioType.MPEG;
        public SourceType sourceType = SourceType.Audio;
        // SourceType => AudioClip
        [Tooltip("Drag audio clip. This is not supports [WebGL]"), ConditionalField(nameof(sourceType), false, SourceType.Audio)]
        public AudioClip audioClip = null;
        // SourceType => StreamingAssets, Url
        [Tooltip("Can select the \"CacheType\" from the AudioManager's inspector."), ConditionalField(nameof(sourceType), true, SourceType.Audio)]
        public bool requestCached = true;
        // SourceType => StreamingAssets
        [Tooltip("Default path is [StreamingAssets]. Just set that inside path and file name, Don't forget file name must with extension, ex: Audio/example.mp3"), ConditionalField(nameof(sourceType), false, SourceType.StreamingAssets)]
        public string fullPathName = "";
        // SourceType => Url
        [ConditionalField(nameof(sourceType), false, SourceType.Url)]
        public UrlSet urlSet = new UrlSet();

        [HideInInspector, Tooltip("Manual to set audio length or press preload button to set [Unity has a bug in WebGL, Get an audio via UnityWebRequest cannot return length value]")]
        public float audioLength = 0;

        [SerializeField]
        protected MixerGroupSourceType _mixerGroupSourceType = MixerGroupSourceType.Assign;
        [SerializeField, ConditionalField(nameof(_mixerGroupSourceType), false, MixerGroupSourceType.Assign)]
        protected AudioMixerGroup _mixerGroup = null;
        [SerializeField, ConditionalField(nameof(_mixerGroupSourceType), false, MixerGroupSourceType.Find)]
        protected string _mixerName = "MasterMixer";
        [SerializeField, ConditionalField(nameof(_mixerGroupSourceType), false, MixerGroupSourceType.Find)]
        protected string _mixerGroupName = "";

        internal override async UniTask<bool> Init()
        {
            this._audioSource = this.GetComponent<AudioSource>();
            bool isInitialized = await this._InitAudio();

            if (isInitialized)
                this._isInit = true; // Mark all init is finished.

            return this._isInit;
        }

        private async UniTask<bool> _InitAudio()
        {
            this.isPrepared = false;

            if (this._audioSource == null)
                return false;

            // Get Audio
            switch (this.sourceType)
            {
                case SourceType.StreamingAssets:
                    this.audioClip = await this.GetAudioFromStreamingAssets(this.requestCached);
                    break;
                case SourceType.Url:
                    this.audioClip = await this.GetAudioFromURL(this.requestCached);
                    break;
            }

            if (this.audioClip == null)
            {
                Logging.Print<Logger>($"<color=#FF0000>Cannot find AudioClip: {this.mediaName}</color>");
                return false;
            }

            // Get Mixer Group
            switch (this._mixerGroupSourceType)
            {
                case MixerGroupSourceType.Assign:
                    break;
                case MixerGroupSourceType.Find:
                    var masterMixer = AudioManager.GetInstance().GetMixerByName(this._mixerName);
                    var mixerGroup = masterMixer?.FindMatchingGroups(this._mixerGroupName)[0];
                    this._mixerGroup = mixerGroup;
                    break;
            }

            this._audioSource.clip = this.audioClip;
            this._audioSource.mute = true;
            this._audioSource.playOnAwake = false;
            this._audioSource.priority = this.audioType.priority;
            this._audioSource.outputAudioMixerGroup = this._mixerGroup;
            this._audioSource.loop = (this.loops == -1) ? true : false;
            this._mediaLength = this._currentRemainingLength = (this.audioLength > 0) ? this.audioLength : this.audioClip.length;

            this.isPrepared = true;

            Logging.Print<Logger>($"<color=#00EEFF>【Init Once】 Audio length: {this._mediaLength} (s)</color>");

            return this.isPrepared;
        }

        public async UniTask<AudioClip> GetAudioFromStreamingAssets(bool cached)
        {
            string pathName = System.IO.Path.Combine(GetRequestStreamingAssetsPath(), this.fullPathName);
            var audioClip = await AudioManager.GetInstance().RequestAudio(pathName, this.audioFileType, null, null, null, cached);
            return audioClip;
        }

        public async UniTask<AudioClip> GetAudioFromURL(bool cached)
        {
            string urlCfg = await this.urlSet.urlCfg.GetFileText();
            string urlSet = this.urlSet.getUrlPathFromCfg ? GetValueFromUrlCfg(urlCfg, AUDIO_URLSET) : string.Empty;
            string url = (!string.IsNullOrEmpty(urlSet)) ? $"{urlSet.Trim()}{this.urlSet.url.Trim()}" : this.urlSet.url.Trim();
            var audioClip = await AudioManager.GetInstance().RequestAudio(url, this.audioFileType, null, null, null, cached);
            return audioClip;
        }

        protected override void OnFixedUpdate(float dt = 0f)
        {
            if (this._audioSource == null)
                return;

            if (!this.isPrepared)
                return;

            if (this.IsPaused())
                return;

            if (this.CurrentRemainingLength() > 0f)
            {
                this._currentRemainingLength -= dt;
                if (this.CurrentRemainingLength() <= 0f)
                {
                    if (this._loops >= 0)
                    {
                        this._audioSource.Stop();

                        this._loops--;
                        if (this._loops <= 0)
                        {
                            this._currentRemainingLength = 0;
                            if (this.autoEndToStop)
                                this.StopSelf();
                        }
                        else
                            this._audioSource.Play();
                    }
                    this._currentRemainingLength = this.Length();
                }
            }
        }

        internal override void Play(int loops, float volume)
        {
            if (this._audioSource == null ||
                this._audioSource.clip == null)
                return;

            this.gameObject.SetActive(true);

            this._audioSource.mute = false;

            if (!this.IsPaused())
                this._loops = (loops == -1 || loops > 0) ? loops : this.loops;

            if (this._loops == -1)
                this._audioSource.loop = true;

            if (!this._audioSource.clip.preloadAudioData)
            {
                this._audioSource.clip.LoadAudioData();
                Logging.Print<Logger>($"Load AudioName: {this.mediaName}, AudioSource => Time: {this._audioSource.time}, TimeSamples: {this._audioSource.timeSamples}; AudioClip => Time: {this._audioSource.clip.length}, Samples: {this._audioSource.clip.samples}, Freq: {this._audioSource.clip.frequency}");
            }

            volume = (volume > 0f) ? volume : this._audioSource.volume;
            this._audioSource.volume = volume;

            if (!this.IsPaused())
                this._audioSource.Play();
            else
                this._audioSource.UnPause();

            this._isPaused = false; // 取消暫停標記
        }

        internal override void Stop()
        {
            if (this._audioSource == null)
                return;

            this._audioSource.Stop();
            this.ResetLength();
            this.ResetLoops();

            this._endEvent?.Invoke();
            this._endEvent = null;

            this.gameObject.SetActive(false);
        }

        internal override void Pause()
        {
            if (this._audioSource == null)
                return;

            this._isPaused = true; // 標記暫停
            this._audioSource.Pause();
        }

        public override bool IsPlaying()
        {
            if (this._audioSource == null)
                return false;
            return this._audioSource.isPlaying;
        }

        public override bool IsPaused()
        {
            return this._isPaused;
        }

        public override bool IsLooping()
        {
            if (this._audioSource == null)
                return false;
            return this._audioSource.loop;
        }

        protected override void StopSelf()
        {
            AudioManager.GetInstance().Stop(this);
        }

        public override float Length()
        {
            return this._mediaLength;
        }

        public override float CurrentLength()
        {
            return this._mediaLength - this._currentRemainingLength;
        }

        public override float CurrentRemainingLength()
        {
            return this._currentRemainingLength;
        }

        public override void OnRelease()
        {
            this._endEvent?.Invoke();

            base.OnRelease();
            this._audioSource = null;
            this._mixerGroup = null;
            this.audioClip = null;
            this.audioType = null;
            this.fullPathName = null;
            this.urlSet = null;
        }

        public AudioSource GetAudioSource()
        {
            return this._audioSource;
        }

        private void OnDestroy()
        {
            if (Time.frameCount == 0 ||
                !Application.isPlaying)
                return;

            try
            {
                if (!this.isDestroying)
                    AudioManager.GetInstance().Stop(this, true, true);
            }
            catch
            {
                /* Nothing to do */
            }
        }
    }
}
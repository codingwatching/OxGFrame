using System;
using UnityEngine;

namespace OxGFrame.MediaFrame.Editor
{
    [Serializable]
    public class MediaUrlPlan
    {
        [SerializeField]
        public string planName = string.Empty;
        [SerializeField]
        public string audioUrlset = "http://127.0.0.1/audio/";
        [SerializeField]
        public string videoUrlset = "http://127.0.0.1/video/";

        public MediaUrlPlan()
        {
            this.planName = "Media Url Plan";
        }
    }
}
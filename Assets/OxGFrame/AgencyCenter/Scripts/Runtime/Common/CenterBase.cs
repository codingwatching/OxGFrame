﻿using System.Collections.Generic;
using UnityEngine;

namespace OxGFrame.AgencyCenter
{
    public class CenterBase<TCenter, TClass> where TCenter : CenterBase<TCenter, TClass>, new()
    {
        private Dictionary<int, TClass> _cache = new Dictionary<int, TClass>();

        private static readonly object _locker = new object();
        private static TCenter _instance = null;
        protected static TCenter GetInstance()
        {
            if (_instance == null)
            {
                lock (_locker)
                {
                    _instance = new TCenter();
                }
            }
            return _instance;
        }

        public UClass Get<UClass>() where UClass : TClass
        {
            System.Type type = typeof(UClass);
            int hashCode = type.GetHashCode();

            return this.Get<UClass>(hashCode);
        }

        public UClass Get<UClass>(int eventId) where UClass : TClass
        {
            return (UClass)this.GetFromCache(eventId);
        }

        public bool Has<UClass>() where UClass : TClass
        {
            System.Type type = typeof(UClass);
            int hashCode = type.GetHashCode();

            return this.Has<UClass>(hashCode);
        }

        public bool Has<UClass>(int id) where UClass : TClass
        {
            return this.HasInCache(id);
        }

        public void Register<UClass>() where UClass : TClass, new()
        {
            System.Type type = typeof(UClass);
            int hashCode = type.GetHashCode();

            UClass @new = new UClass();

            this.Register(hashCode, @new);
        }

        public void Register<UClass>(int id) where UClass : TClass, new()
        {
            UClass @new = new UClass();

            this.Register(id, @new);
        }

        public void Register(int id, TClass @class)
        {
            if (this.HasInCache(id))
            {
                Debug.Log(string.Format("<color=#FF0000>Repeat registration. Id: {0}, Reg: {1}</color>", id, @class.GetType().Name));
                return;
            }

            this._cache.Add(id, @class);
        }

        protected TClass GetFromCache(int id)
        {
            if (!this.HasInCache(id))
            {
                Debug.Log(string.Format("<color=#FF0000>Cannot found. Id: {0}</color>", id));
                return default;
            }

            return this._cache[id];
        }

        protected bool HasInCache(int id)
        {
            return this._cache.ContainsKey(id);
        }
    }
}
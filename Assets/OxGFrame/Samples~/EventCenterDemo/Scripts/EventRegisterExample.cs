﻿using Cysharp.Threading.Tasks;
using OxGFrame.CenterFrame.EventCenter;
using System;
using UnityEngine;

public class EventMsgTest : EventBase
{
    private int _valueInt;
    private string _valueString;

    // Listener
    public event Action<int, string> eventHandler = null;

    public void Emit(int valueInt, string valueString)
    {
        this._valueInt = valueInt;
        this._valueString = valueString;

        this.HandleEvent().Forget();
    }

    public async UniTask EmitAsync(int valueInt, string valueString)
    {
        this._valueInt = valueInt;
        this._valueString = valueString;

        await this.HandleEventAsync();

    }

    public async override UniTaskVoid HandleEvent()
    {
        Debug.Log(string.Format("<color=#FFC078>【Handle Event】 -> {0}</color>", nameof(EventMsgTest)));

        int getValueInt = this._valueInt;
        string getValueString = this._valueString;

        this.eventHandler?.Invoke(getValueInt, getValueString);

        getValueString = string.IsNullOrEmpty(getValueString) ? "null" : getValueString;
        Debug.Log($"<color=#00ff8e>[{nameof(HandleEvent)}] Get Values: {getValueInt}, {getValueString}</color>");

        this.Release();
    }

    public async override UniTask HandleEventAsync()
    {
        Debug.Log(string.Format("<color=#FFC078>【Handle Event】 -> {0}</color>", nameof(EventMsgTest)));

        int getValueInt = this._valueInt;
        string getValueString = this._valueString;

        this.eventHandler?.Invoke(getValueInt, getValueString);

        getValueString = string.IsNullOrEmpty(getValueString) ? "null" : getValueString;
        Debug.Log($"<color=#00ff8e>[{nameof(HandleEvent)}] Get Values: {getValueInt}, {getValueString}</color>");

        await UniTask.Yield();

        this.Release();
    }

    protected override void Release()
    {
        this._valueInt = 0;
        this._valueString = null;
    }
}
using UnityEngine;
using UnityEngine.Video;
using System;

[Serializable]
public class SlideContent
{
    public enum ContentType { Image, Video }

    public ContentType contentType;
    public Sprite image;
    public VideoClip videoClip;
    public bool loopVideo = true;
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class AnimationTimeLineHelper
    {
        //
        // Static Methods
        //
        public static void AddEvent<T>(AnimationClip anim, string funcName, float delayTime, T param) where T : IConvertible
        {
            if (anim == null || string.IsNullOrEmpty(funcName))
            {
                return;
            }
            AnimationEvent animationEvent = new AnimationEvent();
            animationEvent.functionName = funcName;
            animationEvent.time = delayTime;
            animationEvent.messageOptions = SendMessageOptions.DontRequireReceiver;
            Type typeFromHandle = typeof(T);
            if (typeFromHandle == typeof(int) || typeFromHandle == typeof(ushort) || typeFromHandle == typeof(uint))
            {
                animationEvent.intParameter = param.ToInt32(AnimationTimeLineHelper.m_NumberFormater);
            }
            else
            {
                if (typeFromHandle == typeof(float))
                {
                    animationEvent.floatParameter = param.ToSingle(AnimationTimeLineHelper.m_NumberFormater);
                }
                else
                {
                    if (typeFromHandle == typeof(string))
                    {
                        animationEvent.stringParameter = param.ToString();
                    }
                    else
                    {
                        if (typeFromHandle == typeof(bool))
                        {
                            animationEvent.intParameter = param.ToInt32(AnimationTimeLineHelper.m_NumberFormater);
                        }
                        else
                        {
                            UnityEngine.Object @object = param as UnityEngine.Object;
                            if (@object == null)
                            {
                                throw new Exception("[AnimationTimeLineHelper] AddEvent: not support!");
                            }
                            animationEvent.objectReferenceParameter = @object;
                        }
                    }
                }
            }
            anim.AddEvent(animationEvent);
        }

        public static void AddEvent(AnimationClip anim, string funcName, float delayTime)
        {
            if (anim == null)
            {
                return;
            }
            anim.AddEvent(new AnimationEvent
            {
                functionName = funcName,
                time = delayTime,
                messageOptions = SendMessageOptions.DontRequireReceiver
            });
        }

        public static bool GetAnimationClipCurves(AnimationClip clip, out AnimationTimeLineHelper.AniVec3Frames posFrames, out AnimationTimeLineHelper.AniQuatFrames rotFrames, out AnimationTimeLineHelper.AniVec3Frames scaleFrames, params string[] bonePaths)
        {
            posFrames = AnimationTimeLineHelper.AniVec3Frames.Create();
            rotFrames = AnimationTimeLineHelper.AniQuatFrames.Create();
            scaleFrames = AnimationTimeLineHelper.AniVec3Frames.Create();
            return (clip == null || bonePaths == null || bonePaths.Length <= 0) && false;
        }

        public static bool GetAnimatorCurrClipFrame(Animator animator, out int frame, int layer = 0, int clipIndex = 0)
        {
            frame = -1;
            if (animator == null || layer < 0 || clipIndex < 0)
            {
                return false;
            }
            AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorClipInfo[] currentAnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(layer);
            if (currentAnimatorClipInfo == null || currentAnimatorClipInfo.Length <= 0 || clipIndex >= currentAnimatorClipInfo.Length)
            {
                return false;
            }
            if (currentAnimatorClipInfo[clipIndex].clip == null)
            {
                return false;
            }
            frame = Mathf.RoundToInt(currentAnimatorClipInfo[clipIndex].clip.frameRate * currentAnimatorStateInfo.length * (currentAnimatorStateInfo.normalizedTime % 1f));
            return true;
        }

        public static bool GetAnimatorCurrClipMaxFrame(Animator animator, out int maxFrame, int layer = 0, int clipIndex = 0)
        {
            maxFrame = 0;
            if (animator == null || layer < 0 || clipIndex < 0)
            {
                return false;
            }
            AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorClipInfo[] currentAnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(layer);
            if (currentAnimatorClipInfo == null || currentAnimatorClipInfo.Length <= 0 || clipIndex >= currentAnimatorClipInfo.Length)
            {
                return false;
            }
            if (currentAnimatorClipInfo[clipIndex].clip == null)
            {
                return false;
            }
            maxFrame = Mathf.RoundToInt(currentAnimatorClipInfo[clipIndex].clip.frameRate * currentAnimatorStateInfo.length);
            return true;
        }

        public static bool GetAnimatorCurrNormalTime(Animator animator, bool isClip0_1, out float clipTime, int layer = 0)
        {
            clipTime = 0f;
            if (animator == null || layer < 0)
            {
                return false;
            }
            AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            if (isClip0_1)
            {
                clipTime = currentAnimatorStateInfo.normalizedTime % 1f;
            }
            else
            {
                clipTime = currentAnimatorStateInfo.normalizedTime;
            }
            return true;
        }

        public static bool GetAnimatorCurrTime(Animator animator, out float clipTime, int layer = 0)
        {
            clipTime = 0f;
            if (animator == null || layer < 0)
            {
                return false;
            }
            AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            clipTime = currentAnimatorStateInfo.normalizedTime % 1f * currentAnimatorStateInfo.length;
            return true;
        }

        public static bool GetAnimatorCurrTimeLength(Animator animator, out float clipTime, int layer = 0)
        {
            clipTime = 0f;
            if (animator == null || layer < 0)
            {
                return false;
            }
            clipTime = animator.GetCurrentAnimatorStateInfo(layer).length;
            return true;
        }

        public static void RemoveFunction(AnimationClip clip, string funcName, bool isAll)
        {
            if (clip == null || string.IsNullOrEmpty(funcName))
            {
                return;
            }
            AnimationEvent[] events = clip.events;
            if (events == null || events.Length <= 0)
            {
                return;
            }
            List<AnimationEvent> list = new List<AnimationEvent>();
            list.AddRange(events);
            bool flag = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                AnimationEvent animationEvent = list[i];
                if (animationEvent != null && string.Compare(animationEvent.functionName, funcName) == 0)
                {
                    list.RemoveAt(i);
                    flag = true;
                    if (!isAll)
                    {
                        break;
                    }
                }
            }
            if (flag)
            {
                clip.events = list.ToArray();
            }
        }

        //
        // Nested Types
        //
        public struct AniQuatFrames
        {
            public AnimationCurve[] curve;
            public static AnimationTimeLineHelper.AniQuatFrames Create()
            {
                return new AnimationTimeLineHelper.AniQuatFrames
                {
                    curve = new AnimationCurve[4]
                };
            }
        }

        public struct AniVec3Frames
        {
            public AnimationCurve[] curve;
            public static AnimationTimeLineHelper.AniVec3Frames Create()
            {
                return new AnimationTimeLineHelper.AniVec3Frames
                {
                    curve = new AnimationCurve[3]
                };
            }
        }

        public static IFormatProvider m_NumberFormater = System.Globalization.CultureInfo.CurrentCulture;
    }
}
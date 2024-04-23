/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-09-08 10:18:21
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using LoopScrollRect.Core;

namespace fScrollRect.Core
{
    [Serializable]
    public class Snap
    {
        public bool Enable;
        public float VelocityThreshold;
        public float Duration;
        public EaseType Easing;
    }
}
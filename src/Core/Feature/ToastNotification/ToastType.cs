﻿using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    [Flags]
    public enum ToastType
    {
        None = 0,
        Success = 1 << 0,
        Error = 1 << 1,
        Warning = 1 << 2,
        Information = 1 << 3
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pici.HabboHotel.Users.UserDataManagement
{
    class UserDataNotFoundException : Exception
    {
        public UserDataNotFoundException(string reason)
            : base(reason)
        { }
    }
}

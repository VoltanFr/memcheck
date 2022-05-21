﻿using Microsoft.AspNetCore.Identity;
using System;

namespace MemCheck.Domain;

//It's here that we're going to manage the Admin role
public sealed class MemCheckUserRole : IdentityRole<Guid>
{
}

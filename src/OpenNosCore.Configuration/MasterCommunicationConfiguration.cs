﻿using System;
using System.ComponentModel.DataAnnotations;

namespace OpenNosCore.Configuration
{
    public class MasterCommunicationConfiguration : ServerConfiguration
    {
        public string Password { get; set; }
        public ServerConfiguration WebApi { get; set; }
    }
}

﻿using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Download
{
    public class DownloadClientItem
    {
        public String DownloadClient { get; set; }
        public String DownloadClientId { get; set; }
        public String Category { get; set; }
        public String Title { get; set; }

        public Int64 TotalSize { get; set; }
        public Int64 RemainingSize { get; set; }
        public TimeSpan? DownloadTime { get; set; }
        public TimeSpan? RemainingTime { get; set; }

        public String OutputPath { get; set; }
        public String Message { get; set; }

        public DownloadItemStatus Status { get; set; }
        public Boolean IsEncrypted { get; set; }
        public Boolean IsReadOnly { get; set; }

        public RemoteEpisode RemoteEpisode { get; set; }
    }
}

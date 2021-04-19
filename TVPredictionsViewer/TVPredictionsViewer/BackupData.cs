using System;
using System.Collections.Generic;
using System.Text;

namespace TVPredictionsViewer
{
    [Serializable]
    class BackupData
    {
        public Dictionary<int, string> ShowDescriptions, IMDBList, ShowImages;
        public Dictionary<string, int> ShowIDs;
    }
}

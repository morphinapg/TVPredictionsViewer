using System;
using System.Collections.Generic;
using System.Text;

namespace TVPredictionsViewer
{
    public class TVSearchResult
    {
        public string SeriesName;
        public int Id;
        public List<string> Networks;
        public List<string> Aliases;
        public TMDbLib.Objects.TvShows.TvShow Show;

        public TVSearchResult(string name, int id, List<string> networks, List<string> aliases, TMDbLib.Objects.TvShows.TvShow show)
        {
            SeriesName = name;
            Id = id;
            Networks = networks;
            Aliases = aliases;
            Show = show;
        }
    }
}

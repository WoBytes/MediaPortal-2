#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace MediaPortal.Plugins.WifiRemote
{
  public class NowPlayingRadio : IAdditionalNowPlayingInfo
  {
    private string mediaType = "radio";

    public string MediaType
    {
      get { return mediaType; }
    }

    public string MpExtId
    {
      get { return ChannelId.ToString(); } //????
    }

    public int MpExtMediaType
    {
      get { return (int)MpExtendedMediaTypes.Tv; }
    }

    public int MpExtProviderId
    {
      get { return 0; } //no radio providers yet
    }

    /// <summary>
    /// ID of the current channel
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Name of the current channel
    /// </summary>
    public string ChannelName { get; set; }

    /// <summary>
    /// Name of the current artits
    /// </summary>
    public string ArtistName { get; set; }

    /// <summary>
    /// Id of current program
    /// </summary>
    public int CurrentProgramId { get; set; }

    /// <summary>
    /// Name of current program
    /// </summary>
    public string CurrentProgramName { get; set; }

    /// <summary>
    /// Description of current program
    /// </summary>
    public string CurrentProgramDescription { get; set; }

    /// <summary>
    /// Start date of current program
    /// </summary>
    public DateTime CurrentProgramBegin { get; set; }

    /// <summary>
    /// End date of current program
    /// </summary>
    public DateTime CurrentProgramEnd { get; set; }

    /// <summary>
    /// Id of next program
    /// </summary>
    public int NextProgramId { get; set; }

    /// <summary>
    /// Name of next program
    /// </summary>
    public string NextProgramName { get; set; }

    /// <summary>
    /// Description of next program
    /// </summary>
    public string NextProgramDescription { get; set; }

    /// <summary>
    /// Start date of next program
    /// </summary>
    public DateTime NextProgramBegin { get; set; }

    /// <summary>
    /// End date of next program
    /// </summary>
    public DateTime NextProgramEnd { get; set; }

    /// <summary>
    /// <code>true</code> if the program is a web stream
    /// </summary>
    public bool IsWebStream { get; set; }

    /// <summary>
    /// Url of the program
    /// </summary>
    public string CurrentUrl { get; set; }

    // TODO: reimplement
    /// <summary>
    /// Constructor
    /// </summary>
    public NowPlayingRadio()
    {
      /*TvPlugin.TVHome.Navigator.UpdateCurrentChannel();
      TvDatabase.Channel current = TvPlugin.Radio.CurrentChannel;

      if (current != null && current.IsWebstream())
      {
        if (current.ReferringTuningDetail() != null && current.ReferringTuningDetail().Count > 0)
        {
          IList<TuningDetail> details = current.ReferringTuningDetail();
          TuningDetail detail = details[0];
          CurrentProgramName = detail.Name;
          CurrentProgramId = detail.IdChannel;
          CurrentUrl = detail.Url;
          ChannelName = GUIPropertyManager.GetProperty("#Play.Current.Album");
          ArtistName = GUIPropertyManager.GetProperty("#Play.Current.Artist");
        }
      }
      else if (current != null && !current.IsWebstream())
      {
        ChannelId = current.IdChannel;
        ChannelName = current.DisplayName;
        ArtistName = GUIPropertyManager.GetProperty("#Play.Current.Artist");

        if (current.CurrentProgram != null)
        {
          CurrentProgramId = current.CurrentProgram.IdProgram;
          CurrentProgramName = current.CurrentProgram.Title;
          CurrentProgramDescription = current.CurrentProgram.Description;
          CurrentProgramBegin = current.CurrentProgram.StartTime;
          CurrentProgramEnd = current.CurrentProgram.EndTime;
        }

        if (current.NextProgram != null)
        {
          NextProgramId = current.NextProgram.IdProgram;
          NextProgramName = current.NextProgram.Title;
          NextProgramDescription = current.NextProgram.Description;
          NextProgramBegin = current.NextProgram.StartTime;
          NextProgramEnd = current.NextProgram.EndTime;
        }
      }*/
    }
  }
}
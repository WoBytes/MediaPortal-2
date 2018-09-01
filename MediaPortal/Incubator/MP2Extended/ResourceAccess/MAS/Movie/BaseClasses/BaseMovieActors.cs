﻿// Source from: http://madreflection.originalcoder.com/2009/12/generic-tryparse.html

#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieActors
  {
    internal List<WebActor> MovieActors(MediaItem item)
    {
      List<WebActor> output = new List<WebActor>();
      
      var movieActors = (HashSet<object>)item.GetAspect(VideoAspect.Metadata)[VideoAspect.ATTR_ACTORS];
      if (movieActors != null)
      {
        output.AddRange(movieActors.Select(actor => new WebActor
        {
          Title = actor.ToString(), PID = 0
        }));
      }

      return output;
    }
  }
}
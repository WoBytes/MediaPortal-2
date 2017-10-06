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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Backend.Database;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;

namespace MediaPortal.Backend.Services.UserProfileDataManagement
{
  public class UserProfileDataManagement : IUserProfileDataManagement
  {
    #region Public methods

    public void Startup()
    {
      DatabaseSubSchemaManager updater = new DatabaseSubSchemaManager(UserProfileDataManagement_SubSchema.SUBSCHEMA_NAME);
      updater.AddDirectory(UserProfileDataManagement_SubSchema.SubSchemaScriptDirectory);
      int curVersionMajor;
      int curVersionMinor;
      if (!updater.UpdateSubSchema(out curVersionMajor, out curVersionMinor) ||
          curVersionMajor != UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR ||
          curVersionMinor != UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR)
        throw new IllegalCallException(string.Format(
            "Unable to update the UserProfileDataManagement's subschema version to expected version {0}.{1}",
            UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR, UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR));
    }

    public void Shutdown()
    {
      // Nothing to do, yet
    }

    #endregion

    #region Protected methods

    protected ICollection<UserProfile> GetProfiles(Guid? profileId, string name, bool loadData = true)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        int profileIdIndex;
        int nameIndex;
        int idIndex;
        int dataIndex;
        int lastLoginIndex;
        int imageIndex;
        ICollection<UserProfile> result = new List<UserProfile>();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserProfilesCommand(transaction, profileId, name,
            out profileIdIndex, out nameIndex, out idIndex, out dataIndex, out lastLoginIndex, out imageIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              result.Add(new UserProfile(
                database.ReadDBValue<Guid>(reader, profileIdIndex),
                database.ReadDBValue<string>(reader, nameIndex),
                database.ReadDBValue<int>(reader, idIndex),
                database.ReadDBValue<string>(reader, dataIndex),
                database.ReadDBValue<DateTime?>(reader, lastLoginIndex),
                database.ReadDBValue<byte[]>(reader, imageIndex))
              );
            }
          }
        }

        if (loadData)
        {
          foreach (var user in result)
          {
            using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataListCommand(transaction, user.ProfileId, null, false,
                out nameIndex, out profileIdIndex, out dataIndex))
            {
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                {
                  string key = database.ReadDBValue<string>(reader, nameIndex);
                  if (!user.AdditionalData.ContainsKey(key))
                    user.AdditionalData.Add(key, new Dictionary<int, string>());
                  user.AdditionalData[key].Add(database.ReadDBValue<int>(reader, profileIdIndex), database.ReadDBValue<string>(reader, dataIndex));
                }
              }
            }
          }
        }

        return result;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    #endregion

    #region IUserProfileDataManagement implementation

    #region User profiles management

    public ICollection<UserProfile> GetProfiles()
    {
      return GetProfiles(null, null);
    }

    public bool GetProfile(Guid profileId, out UserProfile userProfile)
    {
      ICollection<UserProfile> profiles = GetProfiles(profileId, null);
      userProfile = profiles.FirstOrDefault();
      return userProfile != null;
    }

    public bool GetProfileByName(string profileName, out UserProfile userProfile)
    {
      ICollection<UserProfile> profiles = GetProfiles(null, profileName);
      userProfile = profiles.FirstOrDefault();
      return userProfile != null;
    }

    public Guid CreateProfile(string profileName)
    {
      //Profile might already exist.
      UserProfile existingProfile;
      if (GetProfileByName(profileName, out existingProfile))
        return existingProfile.ProfileId;

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      Guid profileId = Guid.NewGuid();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserProfileCommand(transaction, profileId, profileName))
          command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileName);
        transaction.Rollback();
        throw;
      }
      return profileId;
    }

    public Guid CreateProfile(string profileName, int profileType, string profilePassword)
    {
      //Profile might already exist.
      UserProfile existingProfile;
      if (GetProfileByName(profileName, out existingProfile))
        return existingProfile.ProfileId;

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      Guid profileId = Guid.NewGuid();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserProfileCommand(transaction, profileId, profileName, profileType, profilePassword))
          command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileName);
        transaction.Rollback();
        throw;
      }
      return profileId;
    }

    public bool UpdateProfile(Guid profileId, string profileName, int profileType, string profilePassword)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.UpdateUserProfileCommand(transaction, profileId, profileName, profileType, profilePassword))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileName);
        transaction.Rollback();
        throw;
      }
    }

    public bool SetProfileImage(Guid profileId, byte[] profileImage)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SetUserProfileImageCommand(transaction, profileId, profileImage))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public bool RenameProfile(Guid profileId, string newName)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.UpdateUserProfileNameCommand(transaction, profileId, newName))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error renaming profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public bool DeleteProfile(Guid profileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserProfileCommand(transaction, profileId))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error deleting profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public bool LoginProfile(Guid profileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.LoginUserProfileCommand(transaction, profileId))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error logging in profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #region User playlist data

    public bool GetUserPlaylistData(Guid profileId, Guid playlistId, string key, out string data)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        int dataIndex;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserPlaylistDataCommand(transaction, profileId, playlistId,
            key, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              data = database.ReadDBValue<string>(reader, dataIndex);
              return true;
            }
          }
        }
        data = null;
        return false;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public bool SetUserPlaylistData(Guid profileId, Guid playlistId, string key, string data)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserPlaylistDataCommand(transaction, profileId, playlistId, key))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserPlaylistDataCommand(transaction, profileId, playlistId, key, data))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();
        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error setting playlist data '{0}' for playlist '{1}' in profile '{2}'", e, key, playlistId, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #region User media item data

    public bool GetUserMediaItemData(Guid profileId, Guid mediaItemId, string key, out string data)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        int dataIndex;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserMediaItemDataCommand(transaction, profileId, mediaItemId,
            key, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              data = database.ReadDBValue<string>(reader, dataIndex);
              return true;
            }
          }
        }
        data = null;
        return false;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public bool SetUserMediaItemData(Guid profileId, Guid mediaItemId, string key, string data)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, profileId, mediaItemId, key))
          command.ExecuteNonQuery();

        // Allow "delete only", if new data is null. This is used to delete no longer required data.
        if (!string.IsNullOrEmpty(data))
        {
          using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserMediaItemDataCommand(transaction, profileId, mediaItemId, key, data))
            result = command.ExecuteNonQuery() > 0;
        }
        else
          result = true;

        transaction.Commit();
        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error setting media item data '{0}' for media item '{1}' in profile '{2}'", e, key, mediaItemId, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #region User additional data

    public bool GetUserAdditionalData(Guid profileId, string key, out string data, int dataNo = 0)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        int dataIndex;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataCommand(transaction, profileId,
            key, dataNo, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              data = database.ReadDBValue<string>(reader, dataIndex);
              return true;
            }
          }
        }
        data = null;
        return false;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public bool SetUserAdditionalData(Guid profileId, string key, string data, int dataNo = 0)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserAdditionalDataCommand(transaction, profileId, dataNo, key))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserAdditionalDataCommand(transaction, profileId, key, dataNo, data))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error setting additional data '{0}' in profile '{1}'", e, key, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public bool GetUserAdditionalDataList(Guid profileId, string key, out IEnumerable<Tuple<int, string>> data, bool orderByKey = false, uint? offset = null, uint? limit = null)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        int dataNoIndex;
        int dataIndex;
        List<Tuple<int, string>> list = new List<Tuple<int, string>>();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataListCommand(transaction, profileId,
            key, orderByKey, out dataNoIndex, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            var records = reader.AsEnumerable();
            if (offset.HasValue)
              records = records.Skip((int)offset.Value);
            if (limit.HasValue)
              records = records.Take((int)limit.Value);
            foreach (var record in records)
            {
              list.Add(new Tuple<int, string>(database.ReadDBValue<int>(record, dataNoIndex), database.ReadDBValue<string>(record, dataIndex)));
            }
          }
        }
        data = null;
        if (list.Count > 0)
          data = list;
        return data != null;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public bool GetUserSelectedAdditionalDataList(Guid profileId, string[] keys, out IEnumerable<Tuple<string, int, string>> data, bool orderByKey = false, uint? offset = null, uint? limit = null)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        int dataNoIndex;
        int dataIndex;
        int keyIndex;
        List<Tuple<string, int, string>> list = new List<Tuple<string, int, string>>();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataListCommand(transaction, profileId,
            keys, orderByKey, out keyIndex, out dataNoIndex, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            var records = reader.AsEnumerable();
            if (offset.HasValue)
              records = records.Skip((int)offset.Value);
            if (limit.HasValue)
              records = records.Take((int)limit.Value);
            foreach (var record in records)
            {
              list.Add(new Tuple<string, int, string>(database.ReadDBValue<string>(record, keyIndex), database.ReadDBValue<int>(record, dataNoIndex),
                database.ReadDBValue<string>(record, dataIndex)));
            }
          }
        }
        data = null;
        if (list.Count > 0)
          data = list;
        return data != null;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    #endregion

    #region Cleanup user data

    public bool ClearAllUserData(Guid profileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserPlaylistDataCommand(transaction, profileId, null, null))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, profileId, null, null))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserAdditionalDataCommand(transaction, profileId, null, null))
          command.ExecuteNonQuery();
        transaction.Commit();

        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error clearing user data for profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public bool ClearUserMediaItemDataKey(Guid profileId, string key)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, profileId, null, key))
          command.ExecuteNonQuery();
        transaction.Commit();
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error clearing user media item data for profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public bool ClearUserAdditionalDataKey(Guid profileId, string key)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserAdditionalDataCommand(transaction, profileId, null, key))
          command.ExecuteNonQuery();
        transaction.Commit();

        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error clearing user additional data for profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #endregion
  }
}

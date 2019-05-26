using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Playerdom.Server.Core.Data;
using SQLite;

namespace Playerdom.Server.Core
{
    public class LocalDatabase : IDisposable
    {
        private SQLiteConnection db;


        public LocalDatabase(string path)
        {
            string directoryPath = Path.Combine(path, "Data");

            if(!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);

            }

            if (File.Exists(Path.Combine(directoryPath, "Playerdom.db")))
            {
                db = new SQLiteConnection(Path.Combine(directoryPath, "Playerdom.db"), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
            }
            else
            {
                db = new SQLiteConnection(Path.Combine(directoryPath, "Playerdom.db"), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                db.CreateTable<PlayerEntry>();
            }
        }

        public long? GetPlayerID(Guid token)
        {
            List<PlayerEntry> matches = db.Query<PlayerEntry>("select * from PlayerEntry where Token = ?", token);

            if (matches == null || matches.Count == 0) return null;

            return matches[0].Id;
        }

        public PlayerEntry GetPlayer(long id)
        {
            List<PlayerEntry> matches = db.Query<PlayerEntry>("select * from PlayerEntry where Id = ?", id);

            if (matches == null || matches.Count == 0) return null;

            return matches[0];
        }

        public bool CheckUsernameExistance(string username)
        {
            List<PlayerEntry> matches = db.Query<PlayerEntry>("select * from PlayerEntry where Username = ?", username);

            if (matches.Count == 0) return false;
            return true;
        }

        public void UpdatePlayerUsername(long id, string newUsername)
        {
            db.Execute("update PlayerEntry set Username = ? where id = ?", newUsername, id);
        }

        public void UpdatePlayerToken(long id, Guid newToken)
        {
            db.Execute("update PlayerEntry set Token = ? where id = ?", newToken, id);
        }

        public long CreateNewPlayer(Guid token)
        {
            db.Insert(new PlayerEntry() { Token = token });

            long newId = GetPlayerID(token).Value;
            UpdatePlayerUsername(newId, "Player" + newId);

            return newId;
        }
        public bool? GetPlayerAdminStatus(long id)
        {
            List<bool> matches = db.Query<bool>("select IsAdmin from PlayerEntry where Id = ?", id);

            if (matches == null || matches.Count == 0) return null;

            return matches[0];
        }


        public void Dispose()
        {
            db.Dispose();
        }
    }
}
